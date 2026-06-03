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

            float roll = Hash01(worldHex, seed, 701);
            if (roll > settings.StructureDensity)
            {
                return;
            }

            BiomeDefinition biome = world.GetBiome(worldHex);
            StructureKind kind = ResolveStructureKind(biome);
            switch (kind)
            {
                case StructureKind.Cactus:
                    PlaceCactus(column, surfaceHeight, settings, biome);
                    break;
                case StructureKind.Rock:
                    PlaceRock(column, surfaceHeight, settings, biome);
                    break;
                case StructureKind.Pine:
                    PlacePine(column, surfaceHeight, settings, biome);
                    break;
                default:
                    PlaceOak(column, surfaceHeight, settings, biome);
                    break;
            }
        }

        enum StructureKind
        {
            Oak,
            Pine,
            Cactus,
            Rock,
        }

        static StructureKind ResolveStructureKind(BiomeDefinition biome)
        {
            if (biome == null)
            {
                return StructureKind.Oak;
            }

            string name = biome.name.ToLowerInvariant();
            if (name.Contains("desert") || name.Contains("savanna"))
            {
                return StructureKind.Cactus;
            }

            if (name.Contains("alpine") || name.Contains("tundra") || name.Contains("crystal"))
            {
                return StructureKind.Rock;
            }

            if (name.Contains("taiga") || name.Contains("forest") || name.Contains("swamp"))
            {
                return StructureKind.Pine;
            }

            return StructureKind.Oak;
        }

        static void PlaceOak(BlockColumn column, int surfaceHeight, WorldSettings settings, BiomeDefinition biome)
        {
            BlockId trunk = ResolveTrunk(biome, new BlockId(2));
            BlockId leaves = ResolveLeaves(biome, new BlockId(1));
            int trunkHeight = math.clamp(settings.TreeTrunkHeight, 2, settings.MaxHeightAboveSurface - 2);
            PlaceTrunk(column, surfaceHeight, trunkHeight, trunk, settings);
            PlaceLeafDisc(column, surfaceHeight + trunkHeight, leaves, settings, radius: 1);
        }

        static void PlacePine(BlockColumn column, int surfaceHeight, WorldSettings settings, BiomeDefinition biome)
        {
            BlockId trunk = ResolveTrunk(biome, new BlockId(2));
            BlockId leaves = ResolveLeaves(biome, new BlockId(10));
            int trunkHeight = math.clamp(settings.TreeTrunkHeight + 1, 3, settings.MaxHeightAboveSurface - 1);
            PlaceTrunk(column, surfaceHeight, trunkHeight, trunk, settings);
            for (int i = 0; i < 3; i++)
            {
                PlaceLeafDisc(column, surfaceHeight + trunkHeight - i, leaves, settings, radius: 2 - i);
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
            int height = 2;
            for (int layer = surfaceHeight + 1; layer <= surfaceHeight + height; layer++)
            {
                if (layer >= settings.ColumnCapacity)
                {
                    break;
                }

                column.SetBlock(layer, stone);
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
                if (layer >= settings.ColumnCapacity)
                {
                    break;
                }

                column.SetBlock(layer, trunk);
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
            if (radius <= 0 || layer + 1 >= settings.ColumnCapacity)
            {
                return;
            }

            column.SetBlock(layer + 1, leaves);
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
