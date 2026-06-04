using System.Collections.Generic;
using System.IO;
using Unity.Mathematics;
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
        const string ColumnsFolderName = "columns";

        HexWorld hexWorld;
        WorldSettings settings;
        HexChunkManager chunkManager;
        WorldScroller scroller;
        BlockHotbar hotbar;
        PlayerToolState toolState;
        PlayerInventory inventory;
        PlayerHealth playerHealth;
        PlayerStatsController playerStats;
        WeatherSystem weatherSystem;
        Transform playerTransform;

        public void Initialize(
            HexWorld world,
            WorldSettings worldSettings,
            HexChunkManager chunks,
            WorldScroller worldScroller,
            BlockHotbar blockHotbar,
            PlayerToolState playerTools,
            PlayerInventory playerInventory,
            Transform player,
            PlayerHealth health = null,
            PlayerStatsController stats = null,
            WeatherSystem weather = null)
        {
            hexWorld = world;
            settings = worldSettings;
            chunkManager = chunks;
            scroller = worldScroller;
            hotbar = blockHotbar;
            toolState = playerTools;
            inventory = playerInventory;
            playerTransform = player;
            playerHealth = health;
            playerStats = stats;
            weatherSystem = weather;
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

            var data = BuildSaveData();
            string folder = GetSaveFolder();
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, ManifestFileName), JsonUtility.ToJson(data, true));

            WriteColumnFiles(folder, data);
            WorldRegionSaveUtility.WriteRegionFiles(folder, data, settings);

            hexWorld.ClearDirtyColumns();
            UnityEngine.Debug.Log($"World saved to {folder} ({data.columns.Count} dirty columns).");
        }

        WorldSaveData BuildSaveData()
        {
            var data = new WorldSaveData
            {
                version = WorldSaveData.CurrentVersion,
                seed = settings.Seed,
                playerQ = scroller.PlayerWorldHex.Q,
                playerR = scroller.PlayerWorldHex.R,
                playerY = playerTransform != null ? playerTransform.position.y : 0f,
                hotbarIndex = hotbar != null ? hotbar.SelectedIndex : 0,
                activeTool = toolState != null ? (int)toolState.ActiveTool : 0,
                playerHealth = playerStats != null ? playerStats.GetCurrent(StatId.Health) : (playerHealth != null ? playerHealth.Health : 20f),
                playerStamina = playerStats != null ? playerStats.GetCurrent(StatId.Stamina) : 100f,
                playerHunger = playerStats != null ? playerStats.GetCurrent(StatId.Hunger) : 100f,
                playerBreath = playerStats != null ? playerStats.GetCurrent(StatId.Breath) : 100f,
            };

            if (weatherSystem != null)
            {
                weatherSystem.ExportSaveState(out data.weatherKind, out data.weatherTargetKind, out data.weatherTransition);
            }

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

            return data;
        }

        public void Load()
        {
            if (hexWorld == null || scroller == null)
            {
                return;
            }

            string folder = GetSaveFolder();
            string manifestPath = Path.Combine(folder, ManifestFileName);
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

            if (data.seed != settings.Seed)
            {
                UnityEngine.Debug.LogError(
                    $"Save seed ({data.seed}) does not match world seed ({settings.Seed}). Load aborted.");
                return;
            }

            if (settings.InfiniteWorld)
            {
                WorldRegionSaveUtility.LoadRegionFiles(folder, data, hexWorld, settings);
            }
            else
            {
                LoadColumnFiles(folder, data);
            }

            hexWorld.DataCache.Clear();
            hexWorld.ClearDirtyColumns();

            for (int i = 0; i < data.columns.Count; i++)
            {
                ApplyColumn(data.columns[i]);
            }

            ApplyBiomes(data);
            ApplyPlayerState(data);

            chunkManager.ClearMeshesOnly();
            chunkManager.RefreshAroundPlayer(forceRebuildMeshes: true);
            UnityEngine.Debug.Log($"World loaded from {folder} ({data.columns.Count} columns).");
        }

        void WriteColumnFiles(string folder, WorldSaveData data)
        {
            string columnsFolder = Path.Combine(folder, ColumnsFolderName);
            Directory.CreateDirectory(columnsFolder);
            for (int i = 0; i < data.columns.Count; i++)
            {
                SavedColumn column = data.columns[i];
                string columnPath = Path.Combine(columnsFolder, $"{column.q}_{column.r}.json");
                File.WriteAllText(columnPath, JsonUtility.ToJson(column, true));
            }
        }

        void LoadColumnFiles(string folder, WorldSaveData data)
        {
            string columnsFolder = Path.Combine(folder, ColumnsFolderName);
            for (int i = 0; i < data.columns.Count; i++)
            {
                SavedColumn saved = data.columns[i];
                string columnPath = Path.Combine(columnsFolder, $"{saved.q}_{saved.r}.json");
                if (File.Exists(columnPath))
                {
                    saved = JsonUtility.FromJson<SavedColumn>(File.ReadAllText(columnPath));
                    data.columns[i] = saved;
                }
            }
        }

        void ApplyBiomes(WorldSaveData data)
        {
            for (int i = 0; i < data.biomes.Count; i++)
            {
                SavedBiome savedBiome = data.biomes[i];
                var hex = new HexCoord(savedBiome.q, savedBiome.r);
                if (settings.BiomeCatalog != null &&
                    settings.BiomeCatalog.TryGetBiomeByAssetName(savedBiome.biomeName, out BiomeDefinition biome))
                {
                    hexWorld.SetBiome(hex, biome);
                    hexWorld.MarkColumnDirty(hex);
                }
            }
        }

        void ApplyPlayerState(WorldSaveData data)
        {
            var playerHex = new HexCoord(data.playerQ, data.playerR);
            float3 playerOffset = FlatHexGrid.AxialToWorld(playerHex, settings.BlockSize);
            scroller.SetWorldHex(playerHex, playerOffset);

            if (playerTransform != null)
            {
                Vector3 pos = playerTransform.position;
                pos.y = data.playerY;
                playerTransform.position = pos;
            }

            hotbar?.SelectSlot(data.hotbarIndex);
            toolState?.SetTool((PlayerToolMode)Mathf.Clamp(data.activeTool, 0, (int)PlayerToolMode.Bucket));
                        if (playerStats != null)
            {
                float health = data.playerHealth > 0f ? data.playerHealth : 20f;
                float stamina = data.version >= 4 ? data.playerStamina : playerStats.GetMax(StatId.Stamina);
                float hunger = data.version >= 4 ? data.playerHunger : playerStats.GetMax(StatId.Hunger);
                float breath = data.version >= 4 ? data.playerBreath : playerStats.GetMax(StatId.Breath);
                playerStats.RestoreFromSave(health, stamina, hunger, breath);
            }
            else
            {
                playerHealth?.SetHealth(data.playerHealth > 0f ? data.playerHealth : 20f);
            }
            if (weatherSystem != null && data.version >= 3)
            {
                weatherSystem.ImportSaveState(data.weatherKind, data.weatherTargetKind, data.weatherTransition, false);
            }
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

        SavedColumn SerializeColumn(in HexCoord hex, BlockColumn column)
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
