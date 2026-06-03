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
        const string SaveFolderName = "voxels_world_save";
        const string ManifestFileName = "manifest.json";

        HexWorld hexWorld;
        WorldSettings settings;
        HexChunkManager chunkManager;
        WorldScroller scroller;
        BlockHotbar hotbar;
        PlayerToolState toolState;
        Transform playerTransform;

        public void Initialize(
            HexWorld world,
            WorldSettings worldSettings,
            HexChunkManager chunks,
            WorldScroller worldScroller,
            BlockHotbar blockHotbar,
            PlayerToolState playerTools,
            Transform player)
        {
            hexWorld = world;
            settings = worldSettings;
            chunkManager = chunks;
            scroller = worldScroller;
            hotbar = blockHotbar;
            toolState = playerTools;
            playerTransform = player;
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
            if (hexWorld == null || scroller == null)
            {
                return;
            }

            var data = new WorldSaveData
            {
                version = WorldSaveData.CurrentVersion,
                seed = settings.Seed,
                playerQ = scroller.PlayerWorldHex.Q,
                playerR = scroller.PlayerWorldHex.R,
                playerY = playerTransform != null ? playerTransform.position.y : 0f,
                hotbarIndex = hotbar != null ? hotbar.SelectedIndex : 0,
                activeTool = toolState != null ? (int)toolState.ActiveTool : 0,
            };

            foreach (HexCoord hex in hexWorld.GetDirtyHexes())
            {
                if (!hexWorld.TryGetColumn(hex, out BlockColumn column))
                {
                    continue;
                }

                BiomeDefinition biome = hexWorld.GetBiome(hex);
                if (biome != null)
                {
                    data.biomes.Add(new SavedBiome
                    {
                        q = hex.Q,
                        r = hex.R,
                        biomeName = biome.name,
                    });
                }

                SavedColumn saved = SerializeColumn(hex, column);
                if (saved.blocks.Count > 0)
                {
                    data.columns.Add(saved);
                }
            }

            string folder = GetSaveFolder();
            Directory.CreateDirectory(folder);
            string manifestPath = Path.Combine(folder, ManifestFileName);
            File.WriteAllText(manifestPath, JsonUtility.ToJson(data, true));

            string columnsFolder = Path.Combine(folder, "columns");
            Directory.CreateDirectory(columnsFolder);
            for (int i = 0; i < data.columns.Count; i++)
            {
                SavedColumn column = data.columns[i];
                string columnPath = Path.Combine(columnsFolder, $"{column.q}_{column.r}.json");
                File.WriteAllText(columnPath, JsonUtility.ToJson(column, true));
            }

            hexWorld.ClearDirtyColumns();
            UnityEngine.Debug.Log($"World saved to {folder} ({data.columns.Count} dirty columns).");
        }

        public void Load()
        {
            if (hexWorld == null || scroller == null)
            {
                return;
            }

            string manifestPath = Path.Combine(GetSaveFolder(), ManifestFileName);
            if (!File.Exists(manifestPath))
            {
                UnityEngine.Debug.LogWarning($"No save at {manifestPath}");
                return;
            }

            WorldSaveData data = JsonUtility.FromJson<WorldSaveData>(File.ReadAllText(manifestPath));
            if (data == null)
            {
                return;
            }

            if (data.version < 2)
            {
                UnityEngine.Debug.LogWarning("Save version is outdated; re-save after loading in play mode.");
            }

            if (data.seed != settings.Seed)
            {
                UnityEngine.Debug.LogError(
                    $"Save seed ({data.seed}) does not match world seed ({settings.Seed}). Load aborted.");
                return;
            }

            hexWorld.DataCache.Clear();
            hexWorld.ClearDirtyColumns();

            string columnsFolder = Path.Combine(GetSaveFolder(), "columns");
            for (int i = 0; i < data.columns.Count; i++)
            {
                SavedColumn saved = data.columns[i];
                string columnPath = Path.Combine(columnsFolder, $"{saved.q}_{saved.r}.json");
                if (File.Exists(columnPath))
                {
                    saved = JsonUtility.FromJson<SavedColumn>(File.ReadAllText(columnPath));
                }

                ApplyColumn(saved);
            }

            for (int i = 0; i < data.biomes.Count; i++)
            {
                SavedBiome savedBiome = data.biomes[i];
                var hex = new HexCoord(savedBiome.q, savedBiome.r);
                if (settings.BiomeCatalog != null &&
                    settings.BiomeCatalog.TryGetBiomeByAssetName(savedBiome.biomeName, out BiomeDefinition biome))
                {
                    hexWorld.SetBiome(hex, biome);
                }
            }

            var playerHex = new HexCoord(data.playerQ, data.playerR);
            float3 playerOffset = FlatHexGrid.AxialToWorld(playerHex, settings.BlockSize);
            scroller.SetWorldHex(playerHex, playerOffset);

            if (playerTransform != null)
            {
                Vector3 pos = playerTransform.position;
                pos.y = data.playerY;
                playerTransform.position = pos;
            }

            if (hotbar != null)
            {
                hotbar.SelectSlot(data.hotbarIndex);
            }

            if (toolState != null)
            {
                toolState.SetTool((PlayerToolMode)Mathf.Clamp(data.activeTool, 0, 2));
            }

            chunkManager.ClearMeshesOnly();
            chunkManager.RefreshAroundPlayer(forceRebuildMeshes: true);
            UnityEngine.Debug.Log($"World loaded from {GetSaveFolder()} ({data.columns.Count} columns).");
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
        }

        static SavedColumn SerializeColumn(in HexCoord hex, BlockColumn column)
        {
            var saved = new SavedColumn
            {
                q = hex.Q,
                r = hex.R,
                surfaceHeight = column.SurfaceHeight,
            };

            for (int layer = 0; layer < settings.ColumnCapacity; layer++)
            {
                BlockId blockId = column.GetBlock(layer);
                if (!blockId.IsAir)
                {
                    saved.blocks.Add(new SavedBlock { layer = layer, blockId = blockId.Value });
                }
            }

            return saved;
        }

        static string GetSaveFolder() => Path.Combine(Application.persistentDataPath, SaveFolderName);
    }
}
