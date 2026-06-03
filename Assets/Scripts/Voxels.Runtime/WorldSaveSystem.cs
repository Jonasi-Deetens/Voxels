using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Voxels.Core.Blocks;
using Voxels.Core.Hex;
using Voxels.World;

namespace Voxels.Runtime
{
    public sealed class WorldSaveSystem : MonoBehaviour
    {
        const string SaveFileName = "voxels_world_save.json";

        HexWorld hexWorld;
        WorldSettings settings;
        HexChunkManager chunkManager;
        WorldScroller scroller;

        public void Initialize(HexWorld world, WorldSettings worldSettings, HexChunkManager chunks, WorldScroller worldScroller)
        {
            hexWorld = world;
            settings = worldSettings;
            chunkManager = chunks;
            scroller = worldScroller;
        }

        void Update()
        {
            if (GameInput.WasSavePressedThisFrame())
            {
                Save();
            }

            if (GameInput.WasLoadPressedThisFrame())
            {
                Load();
            }
        }

        public void Save()
        {
            if (hexWorld == null)
            {
                return;
            }

            var data = new WorldSaveData { seed = settings.Seed };
            var seen = new HashSet<HexCoord>();

            foreach (KeyValuePair<HexCoord, BlockColumn> entry in hexWorld.DataCache.EnumerateColumns())
            {
                HexCoord hex = entry.Key;
                if (!seen.Add(hex))
                {
                    continue;
                }

                BlockColumn column = entry.Value;
                var saved = new SavedColumn
                {
                    q = hex.Q,
                    r = hex.R,
                    surfaceHeight = column.SurfaceHeight,
                };

                for (int layer = 0; layer < settings.ColumnCapacity; layer++)
                {
                    BlockId blockId = column.GetBlock(layer);
                    if (blockId.IsAir)
                    {
                        continue;
                    }

                    saved.blocks.Add(new SavedBlock { layer = layer, blockId = blockId.Value });
                }

                if (saved.blocks.Count > 0)
                {
                    data.columns.Add(saved);
                }
            }

            string path = GetSavePath();
            File.WriteAllText(path, JsonUtility.ToJson(data, true));
            UnityEngine.Debug.Log($"World saved to {path} ({data.columns.Count} columns).");
        }

        public void Load()
        {
            if (hexWorld == null)
            {
                return;
            }

            string path = GetSavePath();
            if (!File.Exists(path))
            {
                UnityEngine.Debug.LogWarning($"No save file at {path}");
                return;
            }

            WorldSaveData data = JsonUtility.FromJson<WorldSaveData>(File.ReadAllText(path));
            if (data == null)
            {
                return;
            }

            hexWorld.DataCache.Clear();

            for (int i = 0; i < data.columns.Count; i++)
            {
                SavedColumn saved = data.columns[i];
                var hex = new HexCoord(saved.q, saved.r);
                BlockColumn column = hexWorld.GetOrCreateColumn(hex);
                column.SetSurfaceHeight(saved.surfaceHeight);

                for (int b = 0; b < saved.blocks.Count; b++)
                {
                    column.SetBlock(saved.blocks[b].layer, new BlockId(saved.blocks[b].blockId));
                }
            }

            chunkManager.ClearMeshesOnly();
            chunkManager.RefreshAroundPlayer(forceRebuildMeshes: true);
            UnityEngine.Debug.Log($"World loaded from {path} ({data.columns.Count} columns).");
        }

        static string GetSavePath() => Path.Combine(Application.persistentDataPath, SaveFileName);
    }
}
