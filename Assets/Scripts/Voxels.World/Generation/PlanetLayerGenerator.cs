using System;
using Unity.Mathematics;
using Voxels.Core.Blocks;
using Voxels.Core.Sphere;

namespace Voxels.World.Generation
{
    public sealed class PlanetLayerGenerator : IWorldGenerator
    {
        readonly PlanetSettings settings;
        readonly uint seed;

        public PlanetLayerGenerator(PlanetSettings settings)
        {
            this.settings = settings;
            seed = (uint)math.max(1, settings.Seed);
        }

        public void Generate(PlanetWorld world)
        {
            BiomeDefinition biome = settings.Biome;
            if (biome == null)
            {
                throw new InvalidOperationException("PlanetSettings.Biome is not assigned.");
            }

            BiomeBlockIds blocks = BiomeBlockIds.FromBiome(biome);
            IcosphereHexGrid grid = world.Grid;
            PlanetColumnStorage storage = world.Columns;

            int coreEnd = settings.CoreLayerCount;
            int mantleEnd = coreEnd + settings.MantleLayerCount;
            int crustTop = settings.CrustTopLayer;
            int seaLevel = settings.SeaLevelLayer;

            for (int i = 0; i < grid.CellCount; i++)
            {
                ref readonly SphereHexCell cell = ref grid.GetCell(i);
                BlockColumn column = storage.GetColumn(i);

                int surfaceHeight = SampleSurfaceHeight(cell.Normal, biome, crustTop, seaLevel);
                column.SetSurfaceHeight(surfaceHeight);

                FillGeology(column, coreEnd, mantleEnd, crustTop, blocks.Core, blocks.Mantle, blocks.Bedrock);
                FillTerrainColumn(column, crustTop, surfaceHeight, blocks.Bedrock);
                CarveCaves(column, cell.Normal, biome, coreEnd, surfaceHeight);
                ApplySurfaceBlocks(column, surfaceHeight, seaLevel, crustTop, biome.DirtDepth, blocks.Grass, blocks.Dirt, blocks.Sand, blocks.Bedrock);
                FillWater(column, surfaceHeight, seaLevel, blocks.Water);
            }
        }

        int SampleSurfaceHeight(float3 normal, BiomeDefinition biome, int crustTop, int seaLevel)
        {
            float continent = noise.snoise(normal * biome.ContinentalFrequency + SeedOffset(11));
            int seaOffset = seaLevel - crustTop;

            if (continent < biome.ContinentalThreshold)
            {
                float blendRange = math.max(0.05f, biome.ContinentalBlendWidth);
                float oceanBlend = math.saturate((biome.ContinentalThreshold - continent) / blendRange);
                int oceanDepth = (int)math.round(math.lerp(biome.OceanDepthMin, biome.OceanDepthMax, oceanBlend));
                int offset = seaOffset - oceanDepth;
                return math.max(crustTop + 1, crustTop + offset);
            }

            float detailNoise = noise.snoise(normal * biome.DetailFrequency + SeedOffset(53));
            detailNoise = (detailNoise * 0.5f + 0.5f) * 2f - 1f;

            float terrainType = noise.snoise(normal * biome.TerrainTypeFrequency + SeedOffset(23));
            float elevation = seaOffset;

            if (terrainType < biome.PlainsUpperThreshold)
            {
                elevation += 1f + detailNoise * biome.PlainsRoughness;
            }
            else if (terrainType < biome.HillsUpperThreshold)
            {
                float hills = noise.snoise(normal * biome.MountainFrequency + SeedOffset(17));
                hills = hills * 0.5f + 0.5f;
                elevation += 2f
                    + hills * biome.HillsAmplitude
                    + detailNoise * (biome.DetailAmplitude * 0.35f);
            }
            else
            {
                float mountainNoise = noise.snoise(normal * biome.MountainFrequency + SeedOffset(17));
                mountainNoise = math.saturate(mountainNoise * 0.5f + 0.5f);

                float ridgeNoise = math.abs(noise.snoise(normal * biome.RidgeFrequency + SeedOffset(29)));
                ridgeNoise = math.pow(ridgeNoise, 1.6f);

                elevation += 3f
                    + mountainNoise * biome.MountainAmplitude
                    + ridgeNoise * biome.RidgeAmplitude
                    + detailNoise * biome.DetailAmplitude;
            }

            return crustTop + math.max(1, (int)math.round(elevation));
        }

        static void FillGeology(
            BlockColumn column,
            int coreEnd,
            int mantleEnd,
            int crustTop,
            BlockId core,
            BlockId mantle,
            BlockId bedrock)
        {
            for (int layer = 0; layer < crustTop; layer++)
            {
                BlockId blockId = layer < coreEnd ? core
                    : layer < mantleEnd ? mantle
                    : bedrock;
                column.SetBlock(layer, blockId);
            }
        }

        static void FillTerrainColumn(BlockColumn column, int crustTop, int surfaceHeight, BlockId bedrock)
        {
            for (int layer = crustTop; layer <= surfaceHeight; layer++)
            {
                column.SetBlock(layer, bedrock);
            }
        }

        void CarveCaves(
            BlockColumn column,
            float3 normal,
            BiomeDefinition biome,
            int coreEnd,
            int surfaceHeight)
        {
            int caveTop = surfaceHeight - biome.CaveMaxDepthBelowSurface;
            int caveBottom = coreEnd + biome.CaveMinLayerAboveCore;

            for (int layer = caveBottom; layer <= caveTop; layer++)
            {
                if (IsCave(normal, layer, biome))
                {
                    column.SetBlock(layer, BlockId.Air);
                }
            }
        }

        bool IsCave(float3 normal, int layer, BiomeDefinition biome)
        {
            float3 sample = normal * biome.CaveFrequency + new float3(layer * 0.11f, layer * 0.07f, layer * 0.13f);
            float caveNoise = noise.snoise(sample + SeedOffset(91));
            return caveNoise > biome.CaveThreshold;
        }

        static void ApplySurfaceBlocks(
            BlockColumn column,
            int surfaceHeight,
            int seaLevel,
            int crustTop,
            int dirtDepth,
            BlockId grass,
            BlockId dirt,
            BlockId sand,
            BlockId bedrock)
        {
            bool underwater = surfaceHeight < seaLevel;

            for (int layer = crustTop; layer <= surfaceHeight; layer++)
            {
                if (column.IsAir(layer))
                {
                    continue;
                }

                BlockId blockId;
                if (layer == surfaceHeight)
                {
                    blockId = underwater ? sand : grass;
                }
                else if (layer > surfaceHeight - dirtDepth)
                {
                    blockId = dirt;
                }
                else
                {
                    blockId = bedrock;
                }

                column.SetBlock(layer, blockId);
            }
        }

        static void FillWater(BlockColumn column, int surfaceHeight, int seaLevel, BlockId water)
        {
            if (surfaceHeight >= seaLevel)
            {
                return;
            }

            for (int layer = surfaceHeight + 1; layer <= seaLevel; layer++)
            {
                column.SetBlock(layer, water);
            }
        }

        float3 SeedOffset(int salt)
        {
            uint hash = math.hash(new int3((int)seed, salt, 0));
            return new float3(
                (hash & 0xFF) / 255f * 100f,
                ((hash >> 8) & 0xFF) / 255f * 100f,
                ((hash >> 16) & 0xFF) / 255f * 100f);
        }
    }
}
