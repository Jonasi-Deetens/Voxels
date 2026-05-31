using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using Voxels.World;

namespace Voxels.Runtime
{
    public sealed class CelestialSystem : MonoBehaviour
    {
        const string UrpUnlitShaderName = "Universal Render Pipeline/Unlit";
        const string UrpLitShaderName = "Universal Render Pipeline/Lit";
        const float MoonAngularSizeFactor = 0.45f;
        const float SunEmissionIntensity = 6f;

        [SerializeField] Transform planetTransform;

        Transform skyRoot;
        Transform sunVisual;
        Transform moonVisual;

        PlanetSettings settings;
        PlanetWorld planetWorld;
        Light sunLight;
        PlayerAnchor playerAnchor;
        float orbitPlaneAngle;
        float timeOfDay;

        public float TimeOfDay => timeOfDay;

        public void Initialize(
            PlanetSettings planetSettings,
            PlanetWorld world,
            Light directionalLight,
            PlayerAnchor anchor)
        {
            settings = planetSettings;
            planetWorld = world;
            sunLight = directionalLight != null ? directionalLight : FindSunLight();
            playerAnchor = anchor;
            planetTransform = planetTransform != null ? planetTransform : transform;
            orbitPlaneAngle = (settings.Seed * 0.314159f) % (math.PI * 2f);

            EnsureSkyRoot();
            ApplyCelestial(0f);
        }

        void Update()
        {
            if (settings == null || planetWorld == null)
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
            float spinAngle = normalizedTime * 360f;
            float3 spinAxis = settings.ResolveSpinAxis();
            if (planetTransform != null)
            {
                planetTransform.rotation = Quaternion.AngleAxis(spinAngle, (Vector3)spinAxis);
            }

            float sunAngle = normalizedTime * math.PI * 2f;
            float sunDistance = planetWorld.ApproximateOuterRadius * settings.SunDistanceMultiplier;
            float3 orbitX = math.normalize(new float3(math.cos(orbitPlaneAngle), 0f, math.sin(orbitPlaneAngle)));
            float3 orbitZ = math.normalize(math.cross(spinAxis, orbitX));
            if (math.lengthsq(orbitZ) < 0.001f)
            {
                orbitZ = new float3(0f, 0f, 1f);
            }

            float3 sunDirection = math.cos(sunAngle) * orbitX + math.sin(sunAngle) * orbitZ;
            Vector3 sunPosition = (Vector3)(sunDirection * sunDistance);
            float sunDiameter = AngularDiameterToWorldSize(sunDistance, settings.SunAngularSize);
            float moonDiameter = AngularDiameterToWorldSize(
                sunDistance * 0.95f,
                settings.SunAngularSize * MoonAngularSizeFactor);

            if (sunVisual != null)
            {
                sunVisual.position = sunPosition;
                sunVisual.localScale = Vector3.one * sunDiameter;
            }

            if (moonVisual != null)
            {
                moonVisual.position = (Vector3)(-sunDirection * sunDistance * 0.95f);
                moonVisual.localScale = Vector3.one * moonDiameter;
            }

            if (sunLight != null)
            {
                Vector3 lightOrigin = playerAnchor != null ? playerAnchor.transform.position : Vector3.zero;
                Vector3 lightDirection = (sunPosition - lightOrigin).normalized;
                sunLight.transform.rotation = Quaternion.LookRotation(-lightDirection, Vector3.up);
                RenderSettings.sun = sunLight;
            }
        }

        void EnsureSkyRoot()
        {
            if (skyRoot != null)
            {
                return;
            }

            var skyObject = new GameObject("Sky");
            skyRoot = skyObject.transform;

            sunVisual = CreateSkySphere(
                "Sun",
                CreateSunMaterial(),
                ShadowCastingMode.Off);

            moonVisual = CreateSkySphere(
                "Moon",
                CreateMoonMaterial(),
                ShadowCastingMode.Off);
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
            Color sunTint = new Color(1f, 0.92f, 0.55f, 1f);

            if (shader.name == UrpLitShaderName)
            {
                material.SetColor("_BaseColor", Color.black);
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", sunTint * SunEmissionIntensity);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                // HDR unlit color drives bloom when post-processing is enabled.
                material.SetColor("_BaseColor", sunTint * SunEmissionIntensity);
            }

            return material;
        }

        static Material CreateMoonMaterial()
        {
            Shader shader = Shader.Find(UrpUnlitShaderName);
            if (shader == null)
            {
                return null;
            }

            var material = new Material(shader);
            material.SetColor("_BaseColor", new Color(1.15f, 1.18f, 1.28f, 1f));
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
