using UnityEngine;
using Voxels.World;

namespace Voxels.Runtime
{
    /// <summary>
    /// Drives procedural skybox tint/exposure and optional distance fog from sun height.
    /// </summary>
    public sealed class ProceduralSkyController : MonoBehaviour
    {
        static readonly int SkyTintId = Shader.PropertyToID("_SkyTint");
        static readonly int ExposureId = Shader.PropertyToID("_Exposure");
        static readonly int AtmosphereThicknessId = Shader.PropertyToID("_AtmosphereThickness");
        static readonly int SunSizeId = Shader.PropertyToID("_SunSize");

        [SerializeField] Color daySkyTint = new Color(0.52f, 0.72f, 1f, 1f);
        [SerializeField] Color nightSkyTint = new Color(0.02f, 0.04f, 0.12f, 1f);
        [SerializeField] Color dayFogColor = new Color(0.62f, 0.78f, 0.95f, 1f);
        [SerializeField] Color nightFogColor = new Color(0.03f, 0.05f, 0.1f, 1f);
        [SerializeField] float dayExposure = 1.15f;
        [SerializeField] float nightExposure = 0.35f;
        [SerializeField] float dayFogDensity = 0.0018f;
        [SerializeField] float nightFogDensity = 0.0035f;

        Material skyMaterial;
        CelestialSystem celestial;
        WorldSettings settings;
        WeatherSnapshot weatherSnapshot = WeatherSnapshot.Clear;

        public void Initialize(CelestialSystem celestialSystem, WorldSettings worldSettings)
        {
            celestial = celestialSystem;
            settings = worldSettings;
            EnsureSkyMaterial();
        }

        void LateUpdate()
        {
            if (skyMaterial == null || celestial == null)
            {
                return;
            }

            float sunHeight = celestial.SunHeight;
            float skyDim = 1f - weatherSnapshot.SkyDimming;
            skyMaterial.SetColor(SkyTintId, Color.Lerp(nightSkyTint, daySkyTint, sunHeight) * skyDim);
            skyMaterial.SetFloat(ExposureId, Mathf.Lerp(nightExposure, dayExposure, sunHeight));
            skyMaterial.SetFloat(AtmosphereThicknessId, Mathf.Lerp(0.65f, 1.05f, sunHeight));
            skyMaterial.SetFloat(SunSizeId, Mathf.Lerp(0.02f, 0.05f, sunHeight));

            if (settings != null && settings.EnableDistanceFog)
            {
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.ExponentialSquared;
                RenderSettings.fogColor = Color.Lerp(nightFogColor, dayFogColor, sunHeight);
                float baseFog = Mathf.Lerp(nightFogDensity, dayFogDensity, sunHeight);
                RenderSettings.fogDensity = baseFog * weatherSnapshot.FogMultiplier;
            }
        }

        void EnsureSkyMaterial()
        {
            if (skyMaterial != null)
            {
                return;
            }

            Shader shader = Shader.Find("Skybox/Procedural");
            if (shader == null)
            {
                shader = Shader.Find("Skybox/Gradient");
            }

            if (shader == null)
            {
                return;
            }

            skyMaterial = new Material(shader);
            RenderSettings.skybox = skyMaterial;
        }

        void OnDestroy()
        {
            if (skyMaterial != null)
            {
                Destroy(skyMaterial);
            }
        }
    }
}


        public void ApplyWeather(in WeatherSnapshot snapshot) => weatherSnapshot = snapshot;
