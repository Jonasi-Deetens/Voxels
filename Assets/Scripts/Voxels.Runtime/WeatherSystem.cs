using Unity.Mathematics;
using UnityEngine;
using Voxels.Core.Hex;
using Voxels.World;
using Voxels.World.Climate;

namespace Voxels.Runtime
{
    [DefaultExecutionOrder(50)]
    public sealed class WeatherSystem : MonoBehaviour
    {
        public static WeatherSystem Instance { get; private set; }

        [SerializeField] bool enableWeather = true;
        [SerializeField] Vector2 changeIntervalSeconds = new Vector2(90f, 240f);
        [SerializeField] float transitionSeconds = 45f;

        WorldScroller scroller;
        WorldSettings settings;
        CelestialSystem celestial;
        HexSkyCloudController skyClouds;
        ProceduralSkyController skyController;
        BiomeAmbienceController biomeAmbience;
        WeatherParticleController particles;
        FlatClimateSampler climateSampler;

        WeatherKind targetKind = WeatherKind.Clear;
        WeatherKind currentKind = WeatherKind.Clear;
        WeatherSnapshot snapshot = WeatherSnapshot.Clear;
        float changeTimer;
        float transitionProgress = 1f;
        BiomeDefinition lastBiome;
        Random weatherRandom;

        public WeatherSnapshot Snapshot => snapshot;

        public void Initialize(
            WorldScroller worldScroller,
            WorldSettings worldSettings,
            CelestialSystem celestialSystem,
            HexSkyCloudController clouds,
            ProceduralSkyController proceduralSky,
            BiomeAmbienceController ambience,
            WeatherParticleController weatherParticles)
        {
            Instance = this;
            scroller = worldScroller;
            settings = worldSettings;
            celestial = celestialSystem;
            skyClouds = clouds;
            skyController = proceduralSky;
            biomeAmbience = ambience;
            particles = weatherParticles;
            climateSampler = new FlatClimateSampler(worldSettings);
            weatherRandom = new Random((uint)math.max(1, worldSettings.Seed) ^ 0x9E47A1C5u);
            changeTimer = weatherRandom.NextFloat(changeIntervalSeconds.x, changeIntervalSeconds.y);
            PickNewTargetWeather();
            transitionProgress = 1f;
            ApplySnapshot(true);
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        void Update()
        {
            if (!enableWeather || scroller?.HexWorld == null || settings == null)
            {
                snapshot = WeatherSnapshot.Clear;
                return;
            }

            climateTimer -= Time.deltaTime;
            if (climateTimer <= 0f)
            {
                    PickNewTargetWeather();
            }

            changeTimer -= Time.deltaTime;
            if (changeTimer <= 0f)
            {
                changeTimer = weatherRandom.NextFloat(changeIntervalSeconds.x, changeIntervalSeconds.y);
                PickNewTargetWeather();
            }

            if (transitionProgress < 1f)
            {
                transitionProgress = math.min(1f, transitionProgress + Time.deltaTime / math.max(1f, transitionSeconds));
            }
            else if (currentKind != targetKind)
            {
                currentKind = targetKind;
            }

            ApplySnapshot(false);
        }

        void PickNewTargetWeather()
        {
            HexCoord hex = scroller.PlayerWorldHex;
            HexWorld world = scroller.HexWorld;
            BiomeDefinition biome = world.GetBiome(hex);
            int seaLevel = settings.SeaLevelLayer;
            int surfaceHeight = seaLevel;
            if (world.TryGetColumn(hex, out BlockColumn column))
            {
                surfaceHeight = column.SurfaceHeight;
            }

            int coastDistance = EstimateCoastDistance(world, hex, seaLevel);
            ClimateSample climate = climateSampler.Sample(hex, surfaceHeight, seaLevel, coastDistance);
            WeatherKind picked = WeatherRules.PickTarget(biome, climate, ref weatherRandom);
            if (picked == targetKind)
            {
                return;
            }

            targetKind = picked;
            transitionProgress = 0f;
        }

        void ApplySnapshot(bool instant)
        {
            WeatherProfile target = WeatherRules.GetProfile(targetKind);
            WeatherProfile current = WeatherRules.GetProfile(currentKind);
            float t = instant ? 1f : SmoothStep(transitionProgress);

            float cloudCoverage = math.lerp(current.CloudCoverage, target.CloudCoverage, t);
            float precipitation = math.lerp(current.Precipitation, target.Precipitation, t);
            float fogMultiplier = math.lerp(current.FogMultiplier, target.FogMultiplier, t);
            float windMultiplier = math.lerp(current.WindMultiplier, target.WindMultiplier, t);
            float skyDimming = math.lerp(current.SkyDimming, target.SkyDimming, t);
            float ambientVolume = math.lerp(current.AmbientVolumeMultiplier, target.AmbientVolumeMultiplier, t);

            WeatherKind displayKind = t > 0.5f ? targetKind : currentKind;
            snapshot = new WeatherSnapshot(
                displayKind,
                cloudCoverage,
                precipitation,
                fogMultiplier,
                windMultiplier,
                skyDimming,
                ambientVolume);

            skyClouds?.ApplyWeather(snapshot);
            skyController?.ApplyWeather(snapshot);
            particles?.ApplyWeather(snapshot);
            biomeAmbience?.ApplyWeatherVolume(ambientVolume);
        }

        static float SmoothStep(float t) => t * t * (3f - 2f * t);

        static int EstimateCoastDistance(HexWorld world, in HexCoord hex, int seaLevel)
        {
            if (!world.TryGetColumn(hex, out BlockColumn column))
            {
                return 12;
            }

            if (column.SurfaceHeight <= seaLevel)
            {
                return 0;
            }

            for (int i = 0; i < HexCoord.NeighborOffsets.Length; i++)
            {
                HexCoord neighbor = hex.Add(HexCoord.NeighborOffsets[i]);
                if (world.TryGetColumn(neighbor, out BlockColumn neighborColumn) &&
                    neighborColumn.SurfaceHeight <= seaLevel)
                {
                    return 1;
                }
            }

            return 12;
        }
    }
}
