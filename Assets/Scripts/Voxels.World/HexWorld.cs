using System.Collections.Generic;
using Voxels.Core.Hex;
using Voxels.World.Generation;

namespace Voxels.World
{
    public sealed class HexWorld
    {
        readonly WorldSettings settings;
        readonly HexWorldDataCache dataCache;
        readonly BlockRegistry blockRegistry;

        public WorldSettings Settings => settings;
        public HexWorldDataCache DataCache => dataCache;
        public BlockRegistry BlockRegistry => blockRegistry;
        public float BlockSize => settings.BlockSize;
        public int WorldHexRadius => settings.WorldHexRadius;

        public HexWorld(WorldSettings settings, BlockRegistry blockRegistry)
        {
            this.settings = settings;
            this.blockRegistry = blockRegistry;
            dataCache = new HexWorldDataCache(settings.ColumnCapacity, settings.ColumnCacheMaxCells);
        }

        public void Generate(IWorldGenerator generator)
        {
            generator.Generate(this);
        }

        public bool TryGetColumn(in HexCoord hex, out BlockColumn column) => dataCache.TryGetColumn(hex, out column);

        public BlockColumn GetOrCreateColumn(in HexCoord hex) => dataCache.GetOrCreateColumn(hex);

        public void SetBiome(in HexCoord hex, BiomeDefinition biome) => dataCache.SetBiome(hex, biome);

        public BiomeDefinition GetBiome(in HexCoord hex) => dataCache.GetBiome(hex);

        public HexCoord WorldToLocal(in HexCoord worldHex, in HexCoord playerHex) => worldHex.Subtract(playerHex);

        public bool IsInsideWorld(in HexCoord worldHex) =>
            HexagonMask.IsInsideWorld(worldHex, settings.WorldHexRadius);

        public int DistanceToEdge(in HexCoord worldHex) =>
            settings.WorldHexRadius - HexagonMask.DistanceFromCenter(worldHex);

        public float LayerToWorldY(int layer) => layer * settings.BlockSize;

        public float GetSurfaceWorldY(in HexCoord worldHex)
        {
            if (!dataCache.TryGetColumn(worldHex, out BlockColumn column))
            {
                return settings.SeaLevelLayer * settings.BlockSize;
            }

            return LayerToWorldY(column.SurfaceHeight + 1);
        }

        public void TrimCache(HashSet<HexCoord> protectedHexes) => dataCache.TrimUnprotected(protectedHexes);
    }
}
