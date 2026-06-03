using UnityEngine;
using Voxels.World;

namespace Voxels.Runtime
{
    /// <summary>
    /// Drives a procedural skybox tint/exposure from the celestial sun height.
    /// </summary>
    public sealed class ProceduralSkyController : MonoBehaviour
    {
        static readonly int SkyTintId = Shader.PropertyToID("_SkyTint");
        static readonly int ExposureId = Shader.PropertyToID("_Exposure");
        static readonly int AtmosphereThicknessId = Shader.PropertyToID("_AtmosphereThickness");
        static readonly int SunSizeId = Shader.PropertyToID("_SunSize");

        [SerializeField] Color daySkyTint = new Color(0.52f, 0.72f, 1f, 1f);
        [SerializeField] Color nightSkyTint = new Color(0.02f, 0.04f, 0.12f, 1f);
        [SerializeField] float dayExposure = 1.15f;
        [SerializeField] float nightExposure = 0.35f;

        Material skyMaterial;
        CelestialSystem celestial;

        public void Initialize(CelestialSystem celestialSystem)
        {
            celestial = celestialSystem;
            EnsureSkyMaterial();
        }

        void LateUpdate()
        {
            if (skyMaterial == null || celestial == null)
            {
                return;
            }

            float sunHeight = celestial.SunHeight;
            skyMaterial.SetColor(SkyTintId, Color.Lerp(nightSkyTint, daySkyTint, sunHeight));
            skyMaterial.SetFloat(ExposureId, Mathf.Lerp(nightExposure, dayExposure, sunHeight));
            skyMaterial.SetFloat(AtmosphereThicknessId, Mathf.Lerp(0.65f, 1.05f, sunHeight));
            skyMaterial.SetFloat(SunSizeId, Mathf.Lerp(0.02f, 0.05f, sunHeight));
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
