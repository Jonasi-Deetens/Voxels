using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using Voxels.Rendering;
using Voxels.World;

namespace Voxels.Runtime
{
    /// <summary>
    /// Drifting hexagon cloud cards on a sky dome that follow the player and fade with the day/night cycle.
    /// </summary>
    public sealed class HexSkyCloudController : MonoBehaviour
    {
        const string UrpUnlitShaderName = "Universal Render Pipeline/Unlit";

        [SerializeField] int cloudCount = 20;
        [SerializeField] float skyRadiusMultiplier = 0.92f;
        [SerializeField] float minElevationDegrees = 14f;
        [SerializeField] float maxElevationDegrees = 48f;
        [SerializeField] float driftDegreesPerSecond = 3.5f;
        [SerializeField] float bobAmplitude = 6f;
        [SerializeField] float bobSpeed = 0.35f;
        [SerializeField] Vector2 scaleRange = new Vector2(28f, 72f);
        [SerializeField] Color dayCloudColor = new Color(1f, 1f, 1f, 0.72f);
        [SerializeField] Color nightCloudColor = new Color(0.55f, 0.62f, 0.78f, 0.22f);

        sealed class CloudInstance
        {
            public Transform Transform;
            public float Azimuth;
            public float Elevation;
            public float DriftMultiplier;
            public float BobPhase;
        }

        readonly CloudInstance[] clouds = new CloudInstance[64];

        Transform cloudRoot;
        Material cloudMaterial;
        Mesh cloudMesh;
        CelestialSystem celestial;
        WorldSettings settings;
        Transform playerTransform;
        float windOffset;
        int activeCloudCount;
        int baseCloudCount;
        float densityScale = 1f;
        WeatherSnapshot weatherSnapshot = WeatherSnapshot.Clear;
        float weatherWindMultiplier = 1f;
        float weatherAlphaMultiplier = 1f;

        public void Initialize(CelestialSystem celestialSystem, WorldSettings worldSettings, Transform player)
        {
            celestial = celestialSystem;
            settings = worldSettings;
            playerTransform = player;
            windOffset = (worldSettings != null ? worldSettings.Seed : 0) * 0.017f;

            EnsureCloudRoot();
            EnsureCloudAssets();
            SpawnClouds();
        }

        void LateUpdate()
        {
            if (activeCloudCount == 0 || celestial == null || cloudMaterial == null)
            {
                return;
            }

            Vector3 origin = playerTransform != null ? playerTransform.position : Vector3.zero;
            float skyRadius = ResolveSkyRadius();
            float sunHeight = celestial.SunHeight;
            Color cloudColor = Color.Lerp(nightCloudColor, dayCloudColor, sunHeight);
            cloudColor.a *= weatherAlphaMultiplier;
            cloudMaterial.SetColor("_BaseColor", cloudColor);

            float time = Time.time;
            float timeOfDayBoost = 0.85f + 0.3f * Mathf.Sin(celestial.TimeOfDay * Mathf.PI * 2f);
            float drift = driftDegreesPerSecond * Mathf.Deg2Rad * Time.deltaTime * timeOfDayBoost * weatherWindMultiplier;

            int visibleClouds = Mathf.CeilToInt(activeCloudCount * Mathf.Clamp01(weatherSnapshot.CloudCoverage));
            for (int i = 0; i < activeCloudCount; i++)
            {
                CloudInstance cloud = clouds[i];
                if (cloud?.Transform == null)
                {
                    continue;
                }

                bool visible = i < visibleClouds;
                if (cloud.Transform.gameObject.activeSelf != visible)
                {
                    cloud.Transform.gameObject.SetActive(visible);
                }

                if (!visible)
                {
                    continue;
                }

                cloud.Azimuth += drift * cloud.DriftMultiplier;
                if (cloud.Azimuth > math.PI * 2f)
                {
                    cloud.Azimuth -= math.PI * 2f;
                }

                float bob = math.sin(time * bobSpeed + cloud.BobPhase) * bobAmplitude;
                Vector3 direction = DirectionFromAngles(cloud.Azimuth, cloud.Elevation);
                Vector3 position = origin + direction * (skyRadius + bob);
                cloud.Transform.SetPositionAndRotation(
                    position,
                    Quaternion.FromToRotation(Vector3.up, direction));
            }

            if (cloudRoot != null)
            {
                cloudRoot.position = origin;
            }
        }

        void EnsureCloudRoot()
        {
            if (cloudRoot != null)
            {
                return;
            }

            var rootObject = new GameObject("HexSkyClouds");
            cloudRoot = rootObject.transform;
            cloudRoot.SetParent(transform, false);
        }

        void EnsureCloudAssets()
        {
            if (cloudMesh == null)
            {
                cloudMesh = HexCloudMeshUtility.GetSharedMesh(1f);
            }

            if (cloudMaterial != null)
            {
                return;
            }

            Shader shader = Shader.Find(UrpUnlitShaderName);
            if (shader == null)
            {
                return;
            }

            cloudMaterial = new Material(shader);
            cloudMaterial.SetColor("_BaseColor", dayCloudColor);
            BlockMaterialUtility.ConfigureTransparent(cloudMaterial);
        }

        void SpawnClouds()
        {
            ClearClouds();

            if (cloudMesh == null || cloudMaterial == null || settings == null)
            {
                return;
            }

            baseCloudCount = Mathf.Clamp(cloudCount, 0, clouds.Length);
            activeCloudCount = baseCloudCount;
            var random = new Unity.Mathematics.Random((uint)math.max(1, settings.Seed) ^ 0xC10Du);

            int visibleClouds = Mathf.CeilToInt(activeCloudCount * Mathf.Clamp01(weatherSnapshot.CloudCoverage));
            for (int i = 0; i < activeCloudCount; i++)
            {
                float azimuth = random.NextFloat(0f, math.PI * 2f);
                float elevation = math.radians(random.NextFloat(minElevationDegrees, maxElevationDegrees));
                float scale = random.NextFloat(scaleRange.x, scaleRange.y);
                float driftMultiplier = random.NextFloat(0.65f, 1.35f);
                float bobPhase = random.NextFloat(0f, math.PI * 2f);

                var cloudObject = new GameObject($"HexCloud_{i}");
                cloudObject.transform.SetParent(cloudRoot, false);
                cloudObject.transform.localScale = Vector3.one * scale;

                var filter = cloudObject.AddComponent<MeshFilter>();
                filter.sharedMesh = cloudMesh;

                var renderer = cloudObject.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = cloudMaterial;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;

                clouds[i] = new CloudInstance
                {
                    Transform = cloudObject.transform,
                    Azimuth = azimuth + windOffset,
                    Elevation = elevation,
                    DriftMultiplier = driftMultiplier,
                    BobPhase = bobPhase,
                };
            }
        }

        float ResolveSkyRadius()
        {
            if (settings == null)
            {
                return 400f;
            }

            return settings.ResolveOrbitRadius() * skyRadiusMultiplier;
        }

        static Vector3 DirectionFromAngles(float azimuth, float elevation)
        {
            float cosElev = math.cos(elevation);
            return new Vector3(
                math.sin(azimuth) * cosElev,
                math.sin(elevation),
                math.cos(azimuth) * cosElev);
        }

        void ClearClouds()
        {
            for (int i = 0; i < clouds.Length; i++)
            {
                if (clouds[i]?.Transform != null)
                {
                    Destroy(clouds[i].Transform.gameObject);
                }

                clouds[i] = null;
            }

            activeCloudCount = 0;
        }

        void OnDestroy()
        {
            ClearClouds();
            if (cloudMaterial != null)
            {
                Destroy(cloudMaterial);
            }
        }

        public void ApplyWeather(in WeatherSnapshot snapshot)
        {
            weatherSnapshot = snapshot;
            weatherWindMultiplier = snapshot.WindMultiplier;
            weatherAlphaMultiplier = Mathf.Lerp(0.35f, 1f, snapshot.CloudCoverage);
        }

        public void SetCloudDensityScale(float scale)
        {
            densityScale = Mathf.Clamp(scale, 0.35f, 1.5f);
            int target = Mathf.Clamp(Mathf.RoundToInt(baseCloudCount * densityScale), 0, clouds.Length);
            if (target == activeCloudCount)
            {
                return;
            }

            ClearClouds();
            cloudCount = target;
            SpawnClouds();
        }
    }
}
