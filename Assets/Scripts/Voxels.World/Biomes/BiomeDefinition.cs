using UnityEngine;

namespace Voxels.World
{
    [CreateAssetMenu(menuName = "Voxels/Biome Definition", fileName = "Biome_")]
    public sealed class BiomeDefinition : ScriptableObject
    {
        [Header("Surface")]
        [SerializeField] BlockDefinition surfaceBlock;
        [SerializeField] BlockDefinition subsoilBlock;
        [SerializeField] BlockDefinition underwaterSurfaceBlock;
        [SerializeField] BlockDefinition waterBlock;
        [SerializeField] int dirtDepth = 4;

        [Header("Geology")]
        [SerializeField] BlockDefinition coreBlock;
        [SerializeField] BlockDefinition mantleBlock;
        [SerializeField] BlockDefinition bedrockBlock;

        [Header("Continental (oceans vs land)")]
        [SerializeField] float continentalFrequency = 0.45f;
        [SerializeField] float continentalThreshold = 0.2f;
        [SerializeField] float continentalBlendWidth = 0.3f;
        [SerializeField] int oceanDepthMin = 6;
        [SerializeField] int oceanDepthMax = 14;

        [Header("Terrain diversity (land)")]
        [SerializeField] float terrainTypeFrequency = 0.75f;
        [SerializeField] float plainsUpperThreshold = -0.1f;
        [SerializeField] float hillsUpperThreshold = 0.25f;
        [SerializeField] float plainsRoughness = 2f;
        [SerializeField] float hillsAmplitude = 6f;

        [Header("Mountains (peaks only)")]
        [SerializeField] float mountainAmplitude = 18f;
        [SerializeField] float mountainFrequency = 1.8f;
        [SerializeField] float detailAmplitude = 6f;
        [SerializeField] float detailFrequency = 5.5f;
        [SerializeField] float ridgeFrequency = 3.2f;
        [SerializeField] float ridgeAmplitude = 10f;

        [Header("Caves")]
        [SerializeField] float caveFrequency = 2.5f;
        [SerializeField] float caveThreshold = 0.62f;
        [SerializeField] int caveMinLayerAboveCore = 6;
        [SerializeField] int caveMaxDepthBelowSurface = 5;

        public BlockDefinition SurfaceBlock => surfaceBlock;
        public BlockDefinition SubsoilBlock => subsoilBlock;
        public BlockDefinition UnderwaterSurfaceBlock => underwaterSurfaceBlock;
        public BlockDefinition WaterBlock => waterBlock;
        public BlockDefinition CoreBlock => coreBlock;
        public BlockDefinition MantleBlock => mantleBlock;
        public BlockDefinition BedrockBlock => bedrockBlock;
        public int DirtDepth => dirtDepth;
        public float ContinentalFrequency => continentalFrequency;
        public float ContinentalThreshold => continentalThreshold;
        public float ContinentalBlendWidth => continentalBlendWidth;
        public int OceanDepthMin => oceanDepthMin;
        public int OceanDepthMax => oceanDepthMax;
        public float TerrainTypeFrequency => terrainTypeFrequency;
        public float PlainsUpperThreshold => plainsUpperThreshold;
        public float HillsUpperThreshold => hillsUpperThreshold;
        public float PlainsRoughness => plainsRoughness;
        public float HillsAmplitude => hillsAmplitude;
        public float MountainAmplitude => mountainAmplitude;
        public float MountainFrequency => mountainFrequency;
        public float DetailAmplitude => detailAmplitude;
        public float DetailFrequency => detailFrequency;
        public float RidgeFrequency => ridgeFrequency;
        public float RidgeAmplitude => ridgeAmplitude;
        public float CaveFrequency => caveFrequency;
        public float CaveThreshold => caveThreshold;
        public int CaveMinLayerAboveCore => caveMinLayerAboveCore;
        public int CaveMaxDepthBelowSurface => caveMaxDepthBelowSurface;
    }
}
