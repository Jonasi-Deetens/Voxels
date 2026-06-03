using Unity.Mathematics;
using UnityEngine;

namespace Voxels.World
{
    [CreateAssetMenu(menuName = "Voxels/World Settings", fileName = "World_")]
    public sealed class WorldSettings : ScriptableObject
    {
        [SerializeField] int seed = 42;
        [SerializeField] BiomeCatalog biomeCatalog;
        [SerializeField] BiomeDefinition biome;

        [Header("Map")]
        [SerializeField] int worldHexRadius = 80;
        [SerializeField] float blockSize = 1f;

        [Header("Columns")]
        [SerializeField] int maxDepthBelowSurface = 80;
        [SerializeField] int maxHeightAboveSurface = 30;
        [SerializeField] int seaLevelLayer = 72;

        [Header("Streaming")]
        [SerializeField] int chunkSizeHex = 16;
        [SerializeField] int viewRadiusChunks = 3;

        [Header("Player")]
        [SerializeField] float playerEyeHeight = 1.7f;
        [SerializeField] float playerHeight = 2f;

        [Header("Celestial")]
        [SerializeField] float dayLengthSeconds = 900f;
        [SerializeField] float orbitRadiusMultiplier = 4f;
        [SerializeField] float sunAngularSize = 2.4f;
        [SerializeField] float moonAngularSize = 1.08f;
        [SerializeField] float moonOrbitPhaseOffset = 0.45f;

        [Header("Build Performance")]
        [SerializeField] float buildFrameBudgetMs = 16f;
        [SerializeField] bool createTerrainColliders = true;
        [SerializeField] float floatingOriginRecenterDistance = 1000f;
        [SerializeField] int columnCacheMaxCells = 8192;
        [SerializeField] int chunkMeshPadding = 1;
        [SerializeField] bool showWorldBoundary = true;
        [SerializeField] float boundaryWallHeight = 96f;
        [SerializeField] bool useBackgroundMeshBuild = true;

        public int Seed => seed;
        public BiomeCatalog BiomeCatalog => biomeCatalog;
        public BiomeDefinition Biome => biomeCatalog != null ? biomeCatalog.TerrainProfile : biome;
        public int WorldHexRadius => worldHexRadius;
        public float BlockSize => blockSize;
        public int MaxDepthBelowSurface => maxDepthBelowSurface;
        public int MaxHeightAboveSurface => maxHeightAboveSurface;
        public int SeaLevelLayer => seaLevelLayer;
        public int ColumnCapacity => maxDepthBelowSurface + 1 + maxHeightAboveSurface;
        public int ChunkSizeHex => chunkSizeHex;
        public int ViewRadiusChunks => viewRadiusChunks;
        public float PlayerEyeHeight => playerEyeHeight;
        public float PlayerHeight => playerHeight;
        public float DayLengthSeconds => dayLengthSeconds;
        public float OrbitRadiusMultiplier => orbitRadiusMultiplier;
        public float SunAngularSize => sunAngularSize;
        public float MoonAngularSize => moonAngularSize;
        public float MoonOrbitPhaseOffset => moonOrbitPhaseOffset;
        public float BuildFrameBudgetMs => buildFrameBudgetMs;
        public bool CreateTerrainColliders => createTerrainColliders;
        public float FloatingOriginRecenterDistance => floatingOriginRecenterDistance;
        public int ColumnCacheMaxCells => columnCacheMaxCells;
        public int ChunkMeshPadding => chunkMeshPadding;
        public bool ShowWorldBoundary => showWorldBoundary;
        public float BoundaryWallHeight => boundaryWallHeight;
        public bool UseBackgroundMeshBuild => useBackgroundMeshBuild;

        public float ResolveOrbitRadius()
        {
            return worldHexRadius * blockSize * orbitRadiusMultiplier;
        }

        public float3 ResolveOrbitAxisTilt()
        {
            float angle = (seed * 0.6180339887f) % (math.PI * 2f);
            return math.normalize(new float3(math.cos(angle), 0.35f, math.sin(angle)));
        }
    }
}
