using Unity.Mathematics;
using UnityEngine;

namespace Voxels.World
{
    [CreateAssetMenu(menuName = "Voxels/Planet Settings", fileName = "Planet_")]
    public sealed class PlanetSettings : ScriptableObject
    {
        [SerializeField] int subdivisionLevel = 6;
        [SerializeField] int seed = 42;
        [SerializeField] BiomeCatalog biomeCatalog;
        [SerializeField] BiomeDefinition biome;
        [SerializeField] int cellsPerChunk = 512;
        [Tooltip("Scales planet radius while keeping 1-block hex spacing (2 = double radius, adds one icosphere subdivision step).")]
        [SerializeField] float planetRadiusScale = 4f;

        [Header("Player Scale")]
        [SerializeField] float blockSize = 1f;
        [Tooltip("Eye height in block units while standing on the surface.")]
        [SerializeField] float playerEyeHeight = 1.7f;
        [Tooltip("Player height in block units.")]
        [SerializeField] float playerHeight = 2f;

        [Header("Geological Shell (radial block layers from base radius)")]
        [SerializeField] int coreLayerCount = 4;
        [SerializeField] int mantleLayerCount = 48;
        [SerializeField] int crustLayerCount = 28;
        [SerializeField] int seaLevelOffsetFromCrust = 6;

        [Header("Celestial")]
        [SerializeField] float dayLengthSeconds = 900f;
        [SerializeField] float axisTiltDegrees = 23.5f;
        [SerializeField] float sunDistanceMultiplier = 50f;
        [Tooltip("Apparent diameter of the sun disc in degrees (real sun ≈ 0.5°).")]
        [SerializeField] float sunAngularSize = 1.2f;

        public int SubdivisionLevel => subdivisionLevel;
        public float PlanetRadiusScale => planetRadiusScale;
        public int Seed => seed;
        public BiomeCatalog BiomeCatalog => biomeCatalog;
        public BiomeDefinition Biome => biomeCatalog != null ? biomeCatalog.TerrainProfile : biome;
        public int CellsPerChunk => cellsPerChunk;
        public float BlockSize => blockSize;
        public float PlayerEyeHeight => playerEyeHeight;
        public float PlayerHeight => playerHeight;
        public int CoreLayerCount => coreLayerCount;
        public int MantleLayerCount => mantleLayerCount;
        public int CrustLayerCount => crustLayerCount;
        public int SeaLevelOffsetFromCrust => seaLevelOffsetFromCrust;
        public float DayLengthSeconds => dayLengthSeconds;
        public float AxisTiltDegrees => axisTiltDegrees;
        public float SunDistanceMultiplier => sunDistanceMultiplier;
        public float SunAngularSize => sunAngularSize;

        public int CrustTopLayer => coreLayerCount + mantleLayerCount + crustLayerCount;

        public int SeaLevelLayer => CrustTopLayer + seaLevelOffsetFromCrust;

        public int ResolveSubdivisionLevel()
        {
            int extra = 0;
            float scale = planetRadiusScale;
            while (scale >= 2f)
            {
                extra++;
                scale *= 0.5f;
            }

            return subdivisionLevel + extra;
        }

        public float ResolveShellRadius(float averageNeighborArc) => blockSize / averageNeighborArc;

        public float LayerToWorldRadius(float shellRadius, int layer) => shellRadius + layer * blockSize;

        public float ApproximateOuterRadius(float shellRadius, BiomeDefinition biomeDefinition)
        {
            float amplitude = biomeDefinition != null
                ? biomeDefinition.MountainAmplitude + biomeDefinition.DetailAmplitude
                : 24f;

            if (biomeCatalog != null)
            {
                amplitude = biomeCatalog.MaxMountainAmplitude + 8f;
            }

            float maxLayer = SeaLevelLayer + amplitude + 8f;
            return LayerToWorldRadius(shellRadius, (int)maxLayer);
        }

        public float3 ResolveSpinAxis()
        {
            float angle = (seed * 0.6180339887f) % (math.PI * 2f);
            float3 tiltDir = math.normalize(new float3(math.cos(angle), 0f, math.sin(angle)));
            float tilt = math.radians(axisTiltDegrees);
            quaternion rotation = quaternion.AxisAngle(tiltDir, tilt);
            return math.normalize(math.mul(rotation, new float3(0f, 1f, 0f)));
        }
    }
}
