using System;
using System.Collections;
using Unity.Mathematics;
using Voxels.Core.Blocks;
using Voxels.Core.Sphere;
using Voxels.World;
using Voxels.World.Climate;

namespace Voxels.World.Generation
{
    public sealed class PlanetLayerGenerator : IWorldGenerator
    {
        public const int DefaultBatchSize = 4096;

        readonly PlanetSettings settings;
        readonly uint seed;

        public PlanetLayerGenerator(PlanetSettings settings)
        {
            this.settings = settings;
            seed = (uint)math.max(1, settings.Seed);
        }

        public void Generate(PlanetWorld world)
        {
            RunGeneration(world, DefaultBatchSize, null);
        }

        public IEnumerator GenerateBatched(PlanetWorld world, int batchSize, Action<float, string> reportProgress)
        {
            int cellCount = world.Grid.CellCount;
            var frameBudget = new BuildFrameBudget(settings.BuildFrameBudgetMs);
            int progressStride = math.max(1, cellCount / 128);

            BiomeCatalog catalog = RequireCatalog();
            BiomeDefinition terrainProfile = catalog.TerrainProfile;
            BiomeBlockIds geologyBlocks = BiomeBlockIds.FromBiome(terrainProfile);

            int coreEnd = settings.CoreLayerCount;
            int mantleEnd = coreEnd + settings.MantleLayerCount;
            int crustTop = settings.CrustTopLayer;
            int seaLevel = settings.SeaLevelLayer;

            IcosphereHexGrid grid = world.Grid;
            PlanetColumnStorage storage = world.Columns;
            PlanetBiomeMap biomeMap = world.BiomeMap;
            int[] surfaceHeights = new int[cellCount];

            reportProgress?.Invoke(0.02f, "Generating height map…");
            for (int i = 0; i < cellCount; i++)
            {
                SphereHexCell cell = grid.GetCell(i);
                surfaceHeights[i] = SampleSurfaceHeight(cell.Normal, terrainProfile, crustTop, seaLevel);
                storage.GetColumn(i).SetSurfaceHeight(surfaceHeights[i]);

                if (i % progressStride == 0 || i == cellCount - 1)
                {
                    reportProgress?.Invoke(0.02f + 0.28f * (i + 1) / cellCount, "Generating height map…");
                }

                if (frameBudget.ShouldYield())
                {
                    yield return null;
                    frameBudget.MarkYield();
                }
            }

            reportProgress?.Invoke(0.32f, "Computing climate…");
            yield return null;

            var coastField = new OceanDistanceField(grid, seaLevel, storage);
            var climateSampler = new ClimateSampler(settings);
            var biomeSelector = new BiomeSelector(catalog);
            var climateSamples = new ClimateSample[cellCount];

            for (int i = 0; i < cellCount; i++)
            {
                SphereHexCell cell = grid.GetCell(i);
                climateSamples[i] = climateSampler.Sample(
                    in cell,
                    surfaceHeights[i],
                    seaLevel,
                    crustTop,
                    coastField);
                BiomeDefinition selected = biomeSelector.Select(climateSamples[i]);
                biomeMap.SetBiome(i, selected);

                if (i % progressStride == 0 || i == cellCount - 1)
                {
                    reportProgress?.Invoke(0.32f + 0.18f * (i + 1) / cellCount, "Computing climate…");
                }

                if (frameBudget.ShouldYield())
                {
                    yield return null;
                    frameBudget.MarkYield();
                }
            }

            reportProgress?.Invoke(0.52f, "Filling columns…");
            var geologyTemplate = new BlockColumn(crustTop);
            FillGeology(geologyTemplate, coreEnd, mantleEnd, crustTop, geologyBlocks.Core, geologyBlocks.Mantle, geologyBlocks.Bedrock);

            for (int i = 0; i < cellCount; i++)
            {
                SphereHexCell cell = grid.GetCell(i);
                BlockColumn column = storage.GetColumn(i);
                int surfaceHeight = surfaceHeights[i];
                BiomeDefinition cellBiome = biomeMap.GetBiome(i) ?? terrainProfile;
                BiomeBlockIds blocks = BiomeBlockIds.FromBiome(cellBiome);

                column.CopyLowerLayersFrom(geologyTemplate, crustTop);
                FillTerrainColumn(column, crustTop, surfaceHeight, blocks.Bedrock);
                CarveCaves(column, cell.Normal, cellBiome, coreEnd, surfaceHeight);
                ApplySurfaceBlocks(
                    column,
                    surfaceHeight,
                    seaLevel,
                    crustTop,
                    cellBiome.DirtDepth,
                    blocks.Grass,
                    blocks.Dirt,
                    blocks.Sand,
                    blocks.Bedrock);
                FillWater(column, surfaceHeight, seaLevel, blocks.Water);

                if (i % progressStride == 0 || i == cellCount - 1)
                {
                    reportProgress?.Invoke(0.52f + 0.38f * (i + 1) / cellCount, "Filling columns…");
                }

                if (frameBudget.ShouldYield())
                {
                    yield return null;
                    frameBudget.MarkYield();
                }
            }

            reportProgress?.Invoke(0.92f, "Terrain generation complete.");
        }

        void RunGeneration(PlanetWorld world, int batchSize, Action<float, string> reportProgress)
        {
            IEnumerator routine = GenerateBatched(world, batchSize, reportProgress);
            while (routine.MoveNext())
            {
            }
        }

        BiomeCatalog RequireCatalog()
        {
            BiomeCatalog catalog = settings.BiomeCatalog;
            if (catalog == null)
            {
                throw new InvalidOperationException("PlanetSettings.BiomeCatalog is not assigned.");
            }

            if (catalog.TerrainProfile == null)
            {
                throw new InvalidOperationException("BiomeCatalog.TerrainProfile is not assigned.");
            }

            return catalog;
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
