using Unity.Mathematics;
using Voxels.Core.Blocks;
using Voxels.Core.Hex;
using Voxels.World.Modding;

namespace Voxels.World.Generation
{
    public static class MultiHexStructurePlacer
    {
        static readonly HexCoord[] AxialNeighbors =
        {
            new HexCoord(1, 0),
            new HexCoord(1, -1),
            new HexCoord(0, -1),
            new HexCoord(-1, 0),
            new HexCoord(-1, 1),
            new HexCoord(0, 1),
        };

        public static bool TryPlaceStoneRuin(HexWorld world, in HexCoord anchor, WorldSettings settings, uint seed)
        {
            if (!CanPlaceCluster(world, anchor, settings, seed, 1301, 0.018f))
            {
                return false;
            }

            BlockId stone = new BlockId(3);
            BlockId gravel = new BlockId(12);
            BlockId crystal = new BlockId(13);

            if (!TryReserveCluster(world, anchor))
            {
                return false;
            }

            PlaceColumnStack(world, anchor, settings, stone, 3);
            for (int i = 0; i < AxialNeighbors.Length; i++)
            {
                HexCoord hex = anchor.Add(AxialNeighbors[i]);
                BlockId ringBlock = i % 3 == 0 ? gravel : stone;
                PlaceSurfaceBlock(world, hex, settings, ringBlock, 1);
            }

            HexCoord crystalHex = anchor.Add(AxialNeighbors[HashIndex(anchor, seed, 1302) % AxialNeighbors.Length]);
            PlaceColumnStack(world, crystalHex, settings, crystal, 2);
            return true;
        }

        public static bool TryPlaceWandererCamp(HexWorld world, in HexCoord anchor, WorldSettings settings, uint seed)
        {
            if (!CanPlaceCluster(world, anchor, settings, seed, 1401, 0.022f))
            {
                return false;
            }

            if (!TryReserveCluster(world, anchor))
            {
                return false;
            }

            BlockId ash = new BlockId(15);
            BlockId core = new BlockId(4);
            BlockId dirt = new BlockId(2);

            PlaceSurfaceBlock(world, anchor, settings, ash, 1);
            PlaceSurfaceBlock(world, anchor, settings, core, 2);
            int tentA = HashIndex(anchor, seed, 1402) % AxialNeighbors.Length;
            int tentB = (tentA + 2) % AxialNeighbors.Length;
            PlaceSurfaceBlock(world, anchor.Add(AxialNeighbors[tentA]), settings, dirt, 1);
            PlaceSurfaceBlock(world, anchor.Add(AxialNeighbors[tentB]), settings, ash, 1);
            return true;
        }

        static bool CanPlaceCluster(HexWorld world, in HexCoord anchor, WorldSettings settings, uint seed, int salt, float chance)
        {
            if (Hash01(anchor, seed, salt) > chance)
            {
                return false;
            }

            int sea = settings.SeaLevelLayer;
            if (!world.TryGetColumn(anchor, out BlockColumn center) || center.SurfaceHeight < sea)
            {
                return false;
            }

            for (int i = 0; i < AxialNeighbors.Length; i++)
            {
                HexCoord hex = anchor.Add(AxialNeighbors[i]);
                if (!world.IsInsideWorld(hex) || world.IsColumnDirty(hex))
                {
                    return false;
                }

                if (!world.TryGetColumn(hex, out BlockColumn column) || column.SurfaceHeight < sea)
                {
                    return false;
                }

                if (world.IsStructureReserved(hex))
                {
                    return false;
                }
            }

            return !world.IsStructureReserved(anchor);
        }

        static bool TryReserveCluster(HexWorld world, in HexCoord anchor)
        {
            if (!world.TryReserveStructureHex(anchor))
            {
                return false;
            }

            for (int i = 0; i < AxialNeighbors.Length; i++)
            {
                if (!world.TryReserveStructureHex(anchor.Add(AxialNeighbors[i])))
                {
                    return false;
                }
            }

            return true;
        }

        static void PlaceColumnStack(HexWorld world, in HexCoord hex, WorldSettings settings, BlockId block, int height)
        {
            if (!world.TryGetColumn(hex, out BlockColumn column))
            {
                return;
            }

            int surface = column.SurfaceHeight;
            for (int i = 1; i <= height; i++)
            {
                int layer = surface + i;
                if (layer < settings.ColumnCapacity)
                {
                    column.SetBlock(layer, block);
                }
            }
        }

        static void PlaceSurfaceBlock(HexWorld world, in HexCoord hex, WorldSettings settings, BlockId block, int heightAboveSurface)
        {
            if (!world.TryGetColumn(hex, out BlockColumn column))
            {
                return;
            }

            int layer = column.SurfaceHeight + heightAboveSurface;
            if (layer < settings.ColumnCapacity)
            {
                column.SetBlock(layer, block);
            }
        }

        static int HashIndex(in HexCoord hex, uint seed, int salt)
        {
            uint hash = math.hash(new int3(hex.Q, hex.R, (int)seed + salt));
            return (int)(hash & 0xFFFF);
        }

        static float Hash01(in HexCoord hex, uint seed, int salt)
        {
            return HashIndex(hex, seed, salt) / 65535f;
        }
    }
}
