using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Voxels.Core.Blocks;
using Voxels.Core.Hex;
using Voxels.World;

namespace Voxels.Runtime
{
    public sealed class WorldRegionLoader : MonoBehaviour
    {
        readonly HashSet<ChunkCoord> loadedRegions = new HashSet<ChunkCoord>();
        string saveFolder;
        HexWorld hexWorld;
        WorldSettings settings;

        public void Initialize(HexWorld world, WorldSettings worldSettings)
        {
            hexWorld = world;
            settings = worldSettings;
            saveFolder = Path.Combine(Application.persistentDataPath, "voxels_world_save");
            loadedRegions.Clear();
        }

        public bool TryLoadRegionForChunk(in ChunkCoord chunk)
        {
            if (!settings.InfiniteWorld || hexWorld == null)
            {
                return false;
            }

            if (loadedRegions.Contains(chunk))
            {
                return false;
            }

            string path = Path.Combine(saveFolder, WorldRegionSaveUtility.RegionsFolderName, $"chunk_{chunk.Q}_{chunk.R}.json");
            if (!File.Exists(path))
            {
                loadedRegions.Add(chunk);
                return false;
            }

            WorldSaveData region = JsonUtility.FromJson<WorldSaveData>(File.ReadAllText(path));
            if (region?.columns == null)
            {
                loadedRegions.Add(chunk);
                return false;
            }

            for (int i = 0; i < region.columns.Count; i++)
            {
                ApplyColumn(region.columns[i]);
            }

            for (int i = 0; i < region.biomes.Count; i++)
            {
                SavedBiome savedBiome = region.biomes[i];
                var hex = new HexCoord(savedBiome.q, savedBiome.r);
                if (settings.BiomeCatalog != null &&
                    settings.BiomeCatalog.TryGetBiomeByAssetName(savedBiome.biomeName, out BiomeDefinition biome))
                {
                    hexWorld.SetBiome(hex, biome);
                }
            }

            loadedRegions.Add(chunk);
            return true;
        }

        void ApplyColumn(SavedColumn saved)
        {
            var hex = new HexCoord(saved.q, saved.r);
            BlockColumn column = hexWorld.GetOrCreateColumn(hex);
            column.SetSurfaceHeight(saved.surfaceHeight);

            for (int layer = 0; layer < settings.ColumnCapacity; layer++)
            {
                column.SetBlock(layer, BlockId.Air);
            }

            for (int b = 0; b < saved.blocks.Count; b++)
            {
                column.SetBlock(saved.blocks[b].layer, new BlockId(saved.blocks[b].blockId));
            }

            hexWorld.MarkColumnDirty(hex);
        }
    }
}
