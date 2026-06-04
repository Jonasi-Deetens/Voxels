using System.Collections.Generic;
using Unity.Mathematics;
using Voxels.Core.Blocks;
using Voxels.Core.Hex;
using Voxels.World;
using Voxels.World.Climate;

namespace Voxels.World.Generation
{
    public sealed class FlatTerrainGenerator
    {
        readonly WorldSettings settings;
        readonly uint seed;

        public FlatTerrainGenerator(WorldSettings settings)
        {
            this.settings = settings;
            seed = (uint)math.max(1, settings.Seed);
        }

        public void GenerateChunk(HexWorld world, in ChunkCoord chunkCoord, int padding = 1)
        {
            int size = settings.ChunkSizeHex;
            HexCoord origin = chunkCoord.ToHexOrigin(size);
            var cells = new List<HexCoord>((size + padding * 2) * (size + padding * 2));

            for (int dq = -padding; dq < size + padding; dq++)
            {
                for (int dr = -padding; dr < size + padding; dr++)
                {
                    HexCoord worldHex = origin.Add(new HexCoord(dq, dr));
                    if (world.IsInsideWorld(worldHex))
                    {
                        cells.Add(worldHex);
                    }
                }
            }

            BiomeCatalog catalog = RequireCatalog();
            BiomeDefinition terrainProfile = catalog.TerrainProfile;
            int seaLevel = settings.SeaLevelLayer;
            int minSurfaceLayer = 8;

            var surfaceHeights = new Dictionary<HexCoord, int>(cells.Count);
            for (int i = 0; i < cells.Count; i++)
            {
                HexCoord hex = cells[i];
                if (world.IsColumnDirty(hex) && world.TryGetColumn(hex, out BlockColumn existing))
                {
                    surfaceHeights[hex] = existing.SurfaceHeight;
                    continue;
                }

                int surfaceHeight = SampleSurfaceHeight(world, hex, terrainProfile, seaLevel, minSurfaceLayer);
                surfaceHeights[hex] = surfaceHeight;
                world.GetOrCreateColumn(hex).SetSurfaceHeight(surfaceHeight);
            }

            var coastField = new FlatOceanDistanceField(cells, seaLevel, world.DataCache);
            var climateSampler = new FlatClimateSampler(settings);
            var biomeSelector = new BiomeSelector(catalog);

            foreach (HexCoord hex in cells)
            {
                if (world.IsColumnDirty(hex))
                {
                    continue;
                }

                int surfaceHeight = surfaceHeights[hex];
                BlockColumn column = world.GetOrCreateColumn(hex);
                ClimateSample climate = climateSampler.Sample(
                    hex,
                    surfaceHeight,
                    seaLevel,
                    coastField.GetCoastDistance(hex));
                BiomeDefinition cellBiome = biomeSelector.Select(climate) ?? terrainProfile;
                world.SetBiome(hex, cellBiome);
                BiomeBlockIds blocks = BiomeBlockIds.FromBiome(cellBiome);

                int columnBottom = math.max(0, surfaceHeight - settings.MaxDepthBelowSurface);
                FillColumn(world, column, columnBottom, surfaceHeight, blocks, hex, cellBiome, seaLevel);
                StructureGenerator.TryPlaceStructure(world, hex, column, settings, seed);
            }

            WaterSpreadUtility.ApplyToCells(world, cells, settings);
        }

        void FillColumn(
            HexWorld world,
            BlockColumn column,
            int columnBottom,
            int surfaceHeight,
            BiomeBlockIds blocks,
            in HexCoord worldHex,
            BiomeDefinition biome,
            int seaLevel)
        {
            for (int layer = columnBottom; layer < surfaceHeight; layer++)
            {
                column.SetBlock(layer, blocks.Bedrock);
            }

            CarveCaves(world, column, worldHex, biome, columnBottom, surfaceHeight);
            ApplySurfaceBlocks(
                column,
                columnBottom,
                surfaceHeight,
                seaLevel,
                biome.DirtDepth,
                blocks.Grass,
                blocks.Dirt,
                blocks.Sand,
                blocks.Bedrock);

            if (surfaceHeight >= seaLevel && !column.IsAir(surfaceHeight))
            {
                BlockId blendedSurface = BiomeBorderBlender.ResolveSurfaceBlock(world, worldHex, biome);
                column.SetBlock(surfaceHeight, blendedSurface);
            }

            FillWater(column, surfaceHeight, seaLevel, blocks.Water);

            int skyTop = math.min(
                surfaceHeight + settings.MaxHeightAboveSurface,
                settings.ColumnCapacity - 1);
            for (int layer = surfaceHeight + 1; layer <= skyTop; layer++)
            {
                if (column.GetBlock(layer).IsAir)
                {
                    column.SetBlock(layer, BlockId.Air);
                }
            }
        }

        BiomeCatalog RequireCatalog()
        {
            BiomeCatalog catalog = settings.BiomeCatalog;
            if (catalog == null || catalog.TerrainProfile == null)
            {
                throw new System.InvalidOperationException("WorldSettings.BiomeCatalog is not assigned.");
            }

            return catalog;
        }

        int SampleSurfaceHeight(
            HexWorld world,
            in HexCoord worldHex,
            BiomeDefinition biome,
            int seaLevel,
            int minSurfaceLayer)
        {
            float2 xz = GetNoiseXZ(world, worldHex);
            float continent = noise.snoise(xz * biome.ContinentalFrequency + SeedOffset(11).xy);

            if (continent < biome.ContinentalThreshold)
            {
                float blendRange = math.max(0.05f, biome.ContinentalBlendWidth);
                float oceanBlend = math.saturate((biome.ContinentalThreshold - continent) / blendRange);
                int oceanDepth = (int)math.round(math.lerp(biome.OceanDepthMin, biome.OceanDepthMax, oceanBlend));
                return math.max(minSurfaceLayer, seaLevel - oceanDepth);
            }

            float detailNoise = noise.snoise(xz * biome.DetailFrequency + SeedOffset(53).xy);
            detailNoise = (detailNoise * 0.5f + 0.5f) * 2f - 1f;

            float terrainType = noise.snoise(xz * biome.TerrainTypeFrequency + SeedOffset(23).xy);
            float elevation = seaLevel;

            if (terrainType < biome.PlainsUpperThreshold)
            {
                elevation += 1f + detailNoise * biome.PlainsRoughness;
            }
            else if (terrainType < biome.HillsUpperThreshold)
            {
                float hills = noise.snoise(xz * biome.MountainFrequency + SeedOffset(17).xy);
                hills = hills * 0.5f + 0.5f;
                elevation += 2f
                    + hills * biome.HillsAmplitude
                    + detailNoise * (biome.DetailAmplitude * 0.35f);
            }
            else
            {
                float mountainNoise = noise.snoise(xz * biome.MountainFrequency + SeedOffset(17).xy);
                mountainNoise = math.saturate(mountainNoise * 0.5f + 0.5f);

                float ridgeNoise = math.abs(noise.snoise(xz * biome.RidgeFrequency + SeedOffset(29).xy));
                ridgeNoise = math.pow(ridgeNoise, 1.6f);

                elevation += 3f
                    + mountainNoise * biome.MountainAmplitude
                    + ridgeNoise * biome.RidgeAmplitude
                    + detailNoise * biome.DetailAmplitude;
            }

            int surface = math.max(minSurfaceLayer, (int)math.round(elevation));
            int maxSurface = settings.ColumnCapacity - settings.MaxHeightAboveSurface - 1;
            return math.clamp(surface, minSurfaceLayer + 1, maxSurface);
        }

        void CarveCaves(
            HexWorld world,
            BlockColumn column,
            in HexCoord worldHex,
            BiomeDefinition biome,
            int columnBottom,
            int surfaceHeight)
        {
            int caveTop = surfaceHeight - biome.CaveMaxDepthBelowSurface;
            int caveBottom = columnBottom + biome.CaveMinLayerAboveCore;

            float2 xz = GetNoiseXZ(world, worldHex);
            for (int layer = caveBottom; layer <= caveTop; layer++)
            {
                float3 sample = new float3(xz.x, layer * 0.11f, xz.y) * biome.CaveFrequency;
                float caveNoise = noise.snoise(sample + SeedOffset(91));
                float worm = noise.snoise(sample * 1.7f + SeedOffset(97));
                if (caveNoise > biome.CaveThreshold && worm > -0.15f)
                {
                    column.SetBlock(layer, BlockId.Air);
                }
            }
        }

        static void ApplySurfaceBlocks(
            BlockColumn column,
            int columnBottom,
            int surfaceHeight,
            int seaLevel,
            int dirtDepth,
            BlockId grass,
            BlockId dirt,
            BlockId sand,
            BlockId bedrock)
        {
            bool underwater = surfaceHeight < seaLevel;

            for (int layer = columnBottom; layer <= surfaceHeight; layer++)
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

        static float2 GetNoiseXZ(HexWorld world, in HexCoord worldHex)
        {
            if (world.Settings.InfiniteWorld)
            {
                return WorldNoiseSampling.SampleXZ(worldHex, world.BlockSize, world.NoiseOriginHex);
            }

            return FlatHexGrid.AxialToWorld(worldHex, world.BlockSize).xz;
        }

        float2 SeedOffset(int salt)
        {
            uint hash = math.hash(new int3((int)seed, salt, 0));
            return new float2(
                (hash & 0xFF) / 255f * 100f,
                ((hash >> 8) & 0xFF) / 255f * 100f);
        }
    }
}
