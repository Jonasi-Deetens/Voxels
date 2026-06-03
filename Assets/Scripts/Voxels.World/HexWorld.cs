using Voxels.Core.Hex;
using Voxels.World.Generation;

namespace Voxels.World
{
    public sealed class HexWorld
    {
        readonly WorldSettings settings;
        readonly HexColumnStorage columns;
        readonly HexBiomeMap biomeMap;
        readonly BlockRegistry blockRegistry;

        public WorldSettings Settings => settings;
        public HexColumnStorage Columns => columns;
        public HexBiomeMap BiomeMap => biomeMap;
        public BlockRegistry BlockRegistry => blockRegistry;
        public float BlockSize => settings.BlockSize;
        public int WorldHexRadius => settings.WorldHexRadius;

        public HexWorld(WorldSettings settings, BlockRegistry blockRegistry)
        {
            this.settings = settings;
            this.blockRegistry = blockRegistry;
            columns = new HexColumnStorage(settings.ColumnCapacity);
            biomeMap = new HexBiomeMap();
        }

        public void Generate(IWorldGenerator generator)
        {
            generator.Generate(this);
        }

        public HexCoord WorldToLocal(in HexCoord worldHex, in HexCoord playerHex) => worldHex.Subtract(playerHex);

        public bool IsInsideWorld(in HexCoord worldHex) =>
            HexagonMask.IsInsideWorld(worldHex, settings.WorldHexRadius);

        public float LayerToWorldY(int layer) => layer * settings.BlockSize;

        public float GetSurfaceWorldY(in HexCoord worldHex)
        {
            if (!columns.TryGetColumn(worldHex, out BlockColumn column))
            {
                return settings.SeaLevelLayer * settings.BlockSize;
            }

            return LayerToWorldY(column.SurfaceHeight + 1);
        }
    }
}
