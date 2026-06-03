using UnityEngine;
using Voxels.World;
using Voxels.World.Climate;

namespace Voxels.Runtime
{
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
        [SerializeField] float lightningFlashInterval = 7f;

        Material skyMaterial;
        CelestialSystem celestial;
        WorldSettings settings;
        WeatherSnapshot weatherSnapshot = WeatherSnapshot.Clear;
        float lightningTimer;
        float flashStrength;
        bool fogEnabled = true;

        public void Initialize(CelestialSystem celestialSystem, WorldSettings worldSettings)
        {
            celestial = celestialSystem;
            settings = worldSettings;
            EnsureSkyMaterial();
        }

        public void ApplyWeather(in WeatherSnapshot snapshot) => weatherSnapshot = snapshot;

        public void SetFogEnabled(bool enabled) => fogEnabled = enabled;

        void LateUpdate()
        {
            if (skyMaterial == null || celestial == null)
            {
                return;
            }

            float sunHeight = celestial.SunHeight;
            UpdateLightning();

            float skyDim = (1f - weatherSnapshot.SkyDimming) * (1f - flashStrength * 0.35f);
            Color skyTint = Color.Lerp(nightSkyTint, daySkyTint, sunHeight) * skyDim;
            skyMaterial.SetColor(SkyTintId, skyTint);
            skyMaterial.SetFloat(ExposureId, Mathf.Lerp(nightExposure, dayExposure, sunHeight) + flashStrength * 0.4f);
            skyMaterial.SetFloat(AtmosphereThicknessId, Mathf.Lerp(0.65f, 1.05f, sunHeight));
            skyMaterial.SetFloat(SunSizeId, Mathf.Lerp(0.02f, 0.05f, sunHeight));

            if (settings != null && settings.EnableDistanceFog && fogEnabled)
            {
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.ExponentialSquared;
                RenderSettings.fogColor = Color.Lerp(nightFogColor, dayFogColor, sunHeight);
                float baseFog = Mathf.Lerp(nightFogDensity, dayFogDensity, sunHeight);
                RenderSettings.fogDensity = baseFog * weatherSnapshot.FogMultiplier;
            }

            flashStrength = Mathf.MoveTowards(flashStrength, 0f, Time.deltaTime * 3.5f);
        }

        void UpdateLightning()
        {
            if (weatherSnapshot.Kind != WeatherKind.Storm)
            {
                return;
            }

            lightningTimer -= Time.deltaTime;
            if (lightningTimer > 0f)
            {
                return;
            }

            lightningTimer = lightningFlashInterval * Random.Range(0.45f, 1.1f);
            flashStrength = Random.Range(0.55f, 1f);
        }

        void EnsureSkyMaterial()
        {
            if (skyMaterial != null)
            {
                return;
            }

            Shader shader = Shader.Find("Skybox/Procedural") ?? Shader.Find("Skybox/Gradient");
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
