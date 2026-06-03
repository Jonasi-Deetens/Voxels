using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using Voxels.World;

namespace Voxels.Runtime
{
    [DefaultExecutionOrder(-10)]
    public sealed class CelestialSystem : MonoBehaviour
    {
        const string UrpUnlitShaderName = "Universal Render Pipeline/Unlit";
        const string UrpLitShaderName = "Universal Render Pipeline/Lit";
        const float SunEmissionIntensity = 6f;
        const float MoonEmissionIntensity = 1.2f;

        Transform skyRoot;
        Transform sunVisual;
        Transform moonVisual;

        WorldSettings settings;
        Light sunLight;
        Light moonLight;
        Transform playerTransform;
        float orbitPlaneAngle;
        float timeOfDay;
        Color defaultAmbient;

        public float TimeOfDay => timeOfDay;

        public void Initialize(WorldSettings worldSettings, Light directionalLight, Transform player)
        {
            settings = worldSettings;
            sunLight = directionalLight != null ? directionalLight : FindSunLight();
            playerTransform = player;
            defaultAmbient = RenderSettings.ambientLight;
            orbitPlaneAngle = (settings.Seed * 0.314159f) % (math.PI * 2f);

            EnsureSkyRoot();
            EnsureMoonLight();
            ApplyCelestial(0f);
        }

        void Update()
        {
            if (settings == null)
            {
                return;
            }

            float dayLength = math.max(1f, settings.DayLengthSeconds);
            timeOfDay += Time.deltaTime / dayLength;
            if (timeOfDay > 1f)
            {
                timeOfDay -= 1f;
            }

            ApplyCelestial(timeOfDay);
        }

        void ApplyCelestial(float normalizedTime)
        {
            float orbitRadius = settings.ResolveOrbitRadius();
            float3 orbitAxis = settings.ResolveOrbitAxisTilt();
            float3 orbitX = math.normalize(new float3(math.cos(orbitPlaneAngle), 0f, math.sin(orbitPlaneAngle)));
            float3 orbitZ = math.normalize(math.cross(orbitAxis, orbitX));
            if (math.lengthsq(orbitZ) < 0.001f)
            {
                orbitZ = new float3(0f, 0f, 1f);
            }

            float sunAngle = normalizedTime * math.PI * 2f;
            float moonAngle = sunAngle + math.PI + settings.MoonOrbitPhaseOffset * math.PI * 2f;

            float3 sunDirection = math.cos(sunAngle) * orbitX + math.sin(sunAngle) * orbitZ;
            float3 moonDirection = math.cos(moonAngle) * orbitX + math.sin(moonAngle) * orbitZ;

            Vector3 origin = playerTransform != null ? playerTransform.position : Vector3.zero;
            Vector3 sunPosition = origin + (Vector3)(sunDirection * orbitRadius);
            Vector3 moonPosition = origin + (Vector3)(moonDirection * orbitRadius * 0.98f);

            float sunDiameter = AngularDiameterToWorldSize(orbitRadius, settings.SunAngularSize);
            float moonDiameter = AngularDiameterToWorldSize(orbitRadius, settings.MoonAngularSize);

            if (sunVisual != null)
            {
                sunVisual.position = sunPosition;
                sunVisual.localScale = Vector3.one * sunDiameter;
            }

            if (moonVisual != null)
            {
                moonVisual.position = moonPosition;
                moonVisual.localScale = Vector3.one * moonDiameter;
            }

            float sunHeight = math.saturate(sunDirection.y * 0.5f + 0.5f);
            float moonHeight = math.saturate(moonDirection.y * 0.5f + 0.5f);

            if (sunLight != null)
            {
                Vector3 lightDirection = (sunPosition - origin).normalized;
                sunLight.transform.rotation = Quaternion.LookRotation(-lightDirection, Vector3.up);
                sunLight.intensity = math.lerp(0.04f, 1.15f, sunHeight);
                sunLight.color = Color.Lerp(new Color(0.45f, 0.5f, 0.65f), new Color(1f, 0.95f, 0.85f), sunHeight);
                RenderSettings.sun = sunLight;
            }

            if (moonLight != null)
            {
                Vector3 moonLightDirection = (moonPosition - origin).normalized;
                moonLight.transform.rotation = Quaternion.LookRotation(-moonLightDirection, Vector3.up);
                moonLight.intensity = math.lerp(0f, 0.22f, moonHeight) * (1f - sunHeight * 0.85f);
                moonLight.enabled = moonLight.intensity > 0.01f;
            }

            RenderSettings.ambientLight = Color.Lerp(
                defaultAmbient * 0.35f,
                defaultAmbient,
                math.saturate(sunHeight * 1.1f));
        }

        void EnsureMoonLight()
        {
            if (moonLight != null)
            {
                return;
            }

            var moonLightObject = new GameObject("Moon Light");
            moonLightObject.transform.SetParent(transform, false);
            moonLight = moonLightObject.AddComponent<Light>();
            moonLight.type = LightType.Directional;
            moonLight.color = new Color(0.65f, 0.72f, 0.9f);
            moonLight.shadows = LightShadows.None;
        }

        void EnsureSkyRoot()
        {
            if (skyRoot != null)
            {
                return;
            }

            var skyObject = new GameObject("Sky");
            skyRoot = skyObject.transform;

            sunVisual = CreateSkySphere("Sun", CreateSunMaterial(), ShadowCastingMode.Off);
            moonVisual = CreateSkySphere("Moon", CreateMoonMaterial(), ShadowCastingMode.Off);
        }

        Transform CreateSkySphere(string objectName, Material material, ShadowCastingMode shadowMode)
        {
            var sphereObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphereObject.name = objectName;
            sphereObject.transform.SetParent(skyRoot, false);
            Destroy(sphereObject.GetComponent<Collider>());

            var renderer = sphereObject.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = shadowMode;
            renderer.receiveShadows = false;

            return sphereObject.transform;
        }

        Material CreateSunMaterial()
        {
            Shader shader = Shader.Find(UrpLitShaderName) ?? Shader.Find(UrpUnlitShaderName);
            if (shader == null)
            {
                return null;
            }

            var material = new Material(shader);
            Color sunTint = new Color(1f, 0.55f, 0.15f, 1f);

            if (shader.name == UrpLitShaderName)
            {
                material.SetColor("_BaseColor", Color.black);
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", sunTint * SunEmissionIntensity);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                material.SetColor("_BaseColor", sunTint * SunEmissionIntensity);
            }

            return material;
        }

        Material CreateMoonMaterial()
        {
            Shader shader = Shader.Find(UrpLitShaderName) ?? Shader.Find(UrpUnlitShaderName);
            if (shader == null)
            {
                return null;
            }

            var material = new Material(shader);
            Color moonTint = new Color(0.88f, 0.9f, 0.92f, 1f);

            if (shader.name == UrpLitShaderName)
            {
                material.SetColor("_BaseColor", moonTint * 0.35f);
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", moonTint * MoonEmissionIntensity);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                material.SetColor("_BaseColor", moonTint * MoonEmissionIntensity);
            }

            return material;
        }

        static float AngularDiameterToWorldSize(float distance, float angularDiameterDegrees)
        {
            float halfAngleRad = angularDiameterDegrees * 0.5f * Mathf.Deg2Rad;
            return Mathf.Max(0.5f, distance * Mathf.Tan(halfAngleRad) * 2f);
        }

        static Light FindSunLight()
        {
            Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i].type == LightType.Directional)
                {
                    return lights[i];
                }
            }

            return null;
        }
    }
}
