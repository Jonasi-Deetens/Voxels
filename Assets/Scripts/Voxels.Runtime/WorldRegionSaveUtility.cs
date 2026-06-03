using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Voxels.Core.Blocks;
using Voxels.Core.Hex;
using Voxels.World;

namespace Voxels.Runtime
{
    public static class WorldRegionSaveUtility
    {
        public const string RegionsFolderName = "regions";

        public static void WriteRegionFiles(string saveFolder, WorldSaveData manifest, WorldSettings settings)
        {
            if (!settings.InfiniteWorld)
            {
                return;
            }

            string regionsFolder = Path.Combine(saveFolder, RegionsFolderName);
            Directory.CreateDirectory(regionsFolder);

            var byChunk = new Dictionary<ChunkCoord, WorldSaveData>();
            int chunkSize = settings.ChunkSizeHex;

            for (int i = 0; i < manifest.columns.Count; i++)
            {
                SavedColumn column = manifest.columns[i];
                var hex = new HexCoord(column.q, column.r);
                ChunkCoord chunk = ChunkCoord.FromHex(hex, chunkSize);

                if (!byChunk.TryGetValue(chunk, out WorldSaveData regionData))
                {
                    regionData = new WorldSaveData
                    {
                        version = manifest.version,
                        seed = manifest.seed,
                    };
                    byChunk[chunk] = regionData;
                }

                regionData.columns.Add(column);
            }

            for (int i = 0; i < manifest.biomes.Count; i++)
            {
                SavedBiome biome = manifest.biomes[i];
                var hex = new HexCoord(biome.q, biome.r);
                ChunkCoord chunk = ChunkCoord.FromHex(hex, chunkSize);
                if (byChunk.TryGetValue(chunk, out WorldSaveData regionData))
                {
                    regionData.biomes.Add(biome);
                }
            }

            foreach (KeyValuePair<ChunkCoord, WorldSaveData> entry in byChunk)
            {
                ChunkCoord chunk = entry.Key;
                string path = Path.Combine(regionsFolder, $"chunk_{chunk.Q}_{chunk.R}.json");
                File.WriteAllText(path, JsonUtility.ToJson(entry.Value, true));
            }
        }

        public static void LoadRegionFiles(string saveFolder, WorldSaveData manifest, HexWorld hexWorld, WorldSettings settings)
        {
            if (!settings.InfiniteWorld)
            {
                return;
            }

            string regionsFolder = Path.Combine(saveFolder, RegionsFolderName);
            if (!Directory.Exists(regionsFolder))
            {
                return;
            }

            string[] files = Directory.GetFiles(regionsFolder, "chunk_*.json");
            manifest.columns.Clear();
            manifest.biomes.Clear();

            for (int i = 0; i < files.Length; i++)
            {
                WorldSaveData region = JsonUtility.FromJson<WorldSaveData>(File.ReadAllText(files[i]));
                if (region?.columns == null)
                {
                    continue;
                }

                manifest.columns.AddRange(region.columns);
                if (region.biomes != null)
                {
                    manifest.biomes.AddRange(region.biomes);
                }
            }
        }
    }
}
