using Unity.Mathematics;
using Voxels.Core.Blocks;
using Voxels.Core.Hex;
using Voxels.World;

namespace Voxels.World.Generation
{
    public static class StructureGenerator
    {
        public static void TryPlaceStructure(
            HexWorld world,
            in HexCoord worldHex,
            BlockColumn column,
            WorldSettings settings,
            uint seed)
        {
            if (settings.StructureDensity <= 0f)
            {
                return;
            }

            int seaLevel = settings.SeaLevelLayer;
            int surfaceHeight = column.SurfaceHeight;
            if (surfaceHeight < seaLevel)
            {
                return;
            }

            BiomeDefinition biome = world.GetBiome(worldHex);
            if (!StructurePlacementPlanner.CanPlaceStructure(world, worldHex, biome, seed, settings))
            {
                return;
            }

            PoiKind poi = ResolvePoiKind(biome, worldHex, seed);
            switch (poi)
            {
                case PoiKind.MushroomCluster:
                    PlaceMushroomCluster(column, surfaceHeight, settings, biome);
                    break;
                case PoiKind.DeadTree:
                    PlaceDeadTree(column, surfaceHeight, settings, biome);
                    break;
                case PoiKind.BoulderPatch:
                    PlaceBoulderPatch(column, surfaceHeight, settings, biome);
                    break;
                case PoiKind.Cactus:
                    PlaceCactus(column, surfaceHeight, settings, biome);
                    break;
                case PoiKind.Rock:
                    PlaceRock(column, surfaceHeight, settings, biome);
                    break;
                case PoiKind.Pine:
                    PlacePine(column, surfaceHeight, settings, biome);
                    break;
                default:
                    PlaceOak(column, surfaceHeight, settings, biome);
                    break;
            }
        }

        enum PoiKind
        {
            Oak,
            Pine,
            Cactus,
            Rock,
            MushroomCluster,
            DeadTree,
            BoulderPatch,
        }

        static PoiKind ResolvePoiKind(BiomeDefinition biome, in HexCoord hex, uint seed)
        {
            string name = biome != null ? biome.name.ToLowerInvariant() : string.Empty;
            float variant = Hash01(hex, seed, 811);

            if (name.Contains("fungal") || name.Contains("swamp"))
            {
                return variant < 0.55f ? PoiKind.MushroomCluster : PoiKind.DeadTree;
            }

            if (name.Contains("desert") || name.Contains("savanna"))
            {
                return variant < 0.7f ? PoiKind.Cactus : PoiKind.BoulderPatch;
            }

            if (name.Contains("alpine") || name.Contains("tundra") || name.Contains("crystal"))
            {
                return variant < 0.65f ? PoiKind.Rock : PoiKind.BoulderPatch;
            }

            if (name.Contains("taiga") || name.Contains("forest"))
            {
                return variant < 0.15f ? PoiKind.DeadTree : PoiKind.Pine;
            }

            if (name.Contains("ash"))
            {
                return PoiKind.DeadTree;
            }

            return variant < 0.12f ? PoiKind.BoulderPatch : PoiKind.Oak;
        }

        static void PlaceMushroomCluster(BlockColumn column, int surfaceHeight, WorldSettings settings, BiomeDefinition biome)
        {
            BlockId stem = ResolveTrunk(biome, new BlockId(2));
            BlockId cap = biome?.SurfaceBlock != null ? biome.SurfaceBlock.BlockId : new BlockId(14);
            column.SetBlock(surfaceHeight + 1, stem);
            for (int layer = surfaceHeight + 2; layer <= surfaceHeight + 3; layer++)
            {
                if (layer < settings.ColumnCapacity)
                {
                    column.SetBlock(layer, cap);
                }
            }
        }

        static void PlaceDeadTree(BlockColumn column, int surfaceHeight, WorldSettings settings, BiomeDefinition biome)
        {
            BlockId wood = ResolveTrunk(biome, new BlockId(2));
            int height = math.clamp(2, 2, settings.MaxHeightAboveSurface - 1);
            for (int layer = surfaceHeight + 1; layer <= surfaceHeight + height; layer++)
            {
                if (layer < settings.ColumnCapacity)
                {
                    column.SetBlock(layer, wood);
                }
            }
        }

        static void PlaceBoulderPatch(BlockColumn column, int surfaceHeight, WorldSettings settings, BiomeDefinition biome)
        {
            BlockId stone = biome?.BedrockBlock != null ? biome.BedrockBlock.BlockId : new BlockId(3);
            column.SetBlock(surfaceHeight + 1, stone);
            if (surfaceHeight + 2 < settings.ColumnCapacity)
            {
                column.SetBlock(surfaceHeight + 2, stone);
            }
        }

        static void PlaceOak(BlockColumn column, int surfaceHeight, WorldSettings settings, BiomeDefinition biome)
        {
            BlockId trunk = ResolveTrunk(biome, new BlockId(2));
            BlockId leaves = ResolveLeaves(biome, new BlockId(1));
            int trunkHeight = math.clamp(settings.TreeTrunkHeight, 2, settings.MaxHeightAboveSurface - 2);
            PlaceTrunk(column, surfaceHeight, trunkHeight, trunk, settings);
            PlaceLeafDisc(column, surfaceHeight + trunkHeight, leaves, settings, 1);
        }

        static void PlacePine(BlockColumn column, int surfaceHeight, WorldSettings settings, BiomeDefinition biome)
        {
            BlockId trunk = ResolveTrunk(biome, new BlockId(2));
            BlockId leaves = ResolveLeaves(biome, new BlockId(10));
            int trunkHeight = math.clamp(settings.TreeTrunkHeight + 1, 3, settings.MaxHeightAboveSurface - 1);
            PlaceTrunk(column, surfaceHeight, trunkHeight, trunk, settings);
            for (int i = 0; i < 3; i++)
            {
                PlaceLeafDisc(column, surfaceHeight + trunkHeight - i, leaves, settings, 2 - i);
            }
        }

        static void PlaceCactus(BlockColumn column, int surfaceHeight, WorldSettings settings, BiomeDefinition biome)
        {
            BlockId trunk = ResolveTrunk(biome, new BlockId(7));
            int trunkHeight = math.clamp(settings.TreeTrunkHeight - 1, 2, 4);
            PlaceTrunk(column, surfaceHeight, trunkHeight, trunk, settings);
        }

        static void PlaceRock(BlockColumn column, int surfaceHeight, WorldSettings settings, BiomeDefinition biome)
        {
            BlockId stone = biome?.BedrockBlock != null ? biome.BedrockBlock.BlockId : new BlockId(3);
            for (int layer = surfaceHeight + 1; layer <= surfaceHeight + 2; layer++)
            {
                if (layer < settings.ColumnCapacity)
                {
                    column.SetBlock(layer, stone);
                }
            }
        }

        static void PlaceTrunk(
            BlockColumn column,
            int surfaceHeight,
            int trunkHeight,
            BlockId trunk,
            WorldSettings settings)
        {
            for (int layer = surfaceHeight + 1; layer <= surfaceHeight + trunkHeight; layer++)
            {
                if (layer < settings.ColumnCapacity)
                {
                    column.SetBlock(layer, trunk);
                }
            }
        }

        static void PlaceLeafDisc(
            BlockColumn column,
            int layer,
            BlockId leaves,
            WorldSettings settings,
            int radius)
        {
            if (layer >= settings.ColumnCapacity)
            {
                return;
            }

            column.SetBlock(layer, leaves);
            if (radius > 0 && layer + 1 < settings.ColumnCapacity)
            {
                column.SetBlock(layer + 1, leaves);
            }
        }

        static BlockId ResolveTrunk(BiomeDefinition biome, BlockId fallback) =>
            biome?.SubsoilBlock != null ? biome.SubsoilBlock.BlockId : fallback;

        static BlockId ResolveLeaves(BiomeDefinition biome, BlockId fallback) =>
            biome?.SurfaceBlock != null ? biome.SurfaceBlock.BlockId : fallback;

        static float Hash01(in HexCoord hex, uint seed, int salt)
        {
            uint hash = math.hash(new int3(hex.Q, hex.R, (int)seed + salt));
            return (hash & 0xFFFF) / 65535f;
        }
    }
}
