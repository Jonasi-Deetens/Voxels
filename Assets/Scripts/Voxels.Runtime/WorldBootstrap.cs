using System.Collections;
using System.Diagnostics;
using UnityEngine;
using Voxels.Core.Hex;
using Voxels.World;

namespace Voxels.Runtime
{
    [DefaultExecutionOrder(-200)]
    public class WorldBootstrap : MonoBehaviour
    {
        [SerializeField] WorldSettings settings;
        [SerializeField] BlockDefinition[] blockDefinitions;
        [SerializeField] Transform chunkRoot;
        [SerializeField] Light directionalLight;
        [SerializeField] Transform playerRoot;

        HexWorld hexWorld;
        WorldScroller worldScroller;
        HexChunkManager chunkManager;
        WorldBoundary worldBoundary;
        HexBlockInteractor blockInteractor;
        ProceduralSkyController skyController;
        FlatPlayerController playerController;
        FlatSpawnCamera spawnCamera;
        CelestialSystem celestialSystem;
        BlockHotbar blockHotbar;
        WorldSaveSystem saveSystem;
        PlayerToolState toolState;
        WorldRuntimeProfiler runtimeProfiler;
        GameHudView gameHud;
        HexSkyCloudController skyClouds;
        BlockEditFeedback blockFeedback;
        Transform worldRootTransform;
        bool buildComplete;

        public HexWorld HexWorld => hexWorld;
        public bool BuildComplete => buildComplete;

        void Awake()
        {
            StartCoroutine(BuildWorldAsync());
        }

        public void RegenerateWorld()
        {
            StopAllCoroutines();
            if (Application.isPlaying)
            {
                StartCoroutine(BuildWorldAsync());
            }
        }

        IEnumerator BuildWorldAsync()
        {
            buildComplete = false;
            var stopwatch = Stopwatch.StartNew();

            if (settings == null)
            {
                UnityEngine.Debug.LogError("WorldBootstrap requires WorldSettings.");
                yield break;
            }

            settings.ApplyPerformancePreset();

            if (settings.BiomeCatalog == null && settings.Biome == null)
            {
                UnityEngine.Debug.LogError("WorldSettings needs BiomeCatalog. Run Voxels/Setup Default Content.");
                yield break;
            }

            WorldBuildOverlay overlay = WorldBuildOverlay.Ensure();
            overlay.SetVisible(true);
            overlay.Report(0f, "Preparing world…");

            spawnCamera = FindAnyObjectByType<FlatSpawnCamera>();
            if (spawnCamera != null)
            {
                spawnCamera.enabled = false;
            }

            ClearWorld();

            BlockRegistry registry = BlockRegistryBuilder.Build(settings, blockDefinitions);
            hexWorld = new HexWorld(settings, registry);

            var worldRootObject = new GameObject("WorldRoot");
            worldRootTransform = worldRootObject.transform;
            worldRootTransform.SetParent(transform, false);

            Transform chunksParent = chunkRoot != null ? chunkRoot : worldRootTransform;

            worldScroller = GetOrAdd<WorldScroller>();
            chunkManager = GetOrAdd<HexChunkManager>();
            worldBoundary = GetOrAdd<WorldBoundary>();
            blockInteractor = GetOrAdd<HexBlockInteractor>();
            skyController = GetOrAdd<ProceduralSkyController>();
            blockHotbar = GetOrAdd<BlockHotbar>();
            saveSystem = GetOrAdd<WorldSaveSystem>();
            toolState = GetOrAdd<PlayerToolState>();
            runtimeProfiler = GetOrAdd<WorldRuntimeProfiler>();
            gameHud = GetOrAdd<GameHudView>();
            skyClouds = GetOrAdd<HexSkyCloudController>();
            GetOrAdd<PlayerGameplayState>();
            PlayerInventory playerInventory = GetOrAdd<PlayerInventory>();
            WorldRegionLoader regionLoader = GetOrAdd<WorldRegionLoader>();
            CraftingSystem craftingSystem = GetOrAdd<CraftingSystem>();
            BiomeAmbienceController biomeAmbience = GetOrAdd<BiomeAmbienceController>();
            NightCreatureSpawner nightSpawner = GetOrAdd<NightCreatureSpawner>();
            DayNightGameplayController dayNight = GetOrAdd<DayNightGameplayController>();

            blockHotbar.Initialize(blockDefinitions, registry, playerInventory);
            regionLoader.Initialize(hexWorld, settings);
            craftingSystem.Initialize(playerInventory, registry);
            chunkManager.Initialize(hexWorld, settings, worldScroller, chunksParent);
            worldScroller.Initialize(settings, worldRootTransform, ResolvePlayerTransform(), HexCoord.Zero, hexWorld);

            overlay.Report(0.25f, "Finding spawn…");
            chunkManager.RefreshAroundPlayer();
            while (!HasLoadedChunks())
            {
                yield return null;
            }

            HexCoord spawnHex = FlatWorldSpawn.FindSpawnHex(hexWorld, settings);
            worldScroller.Initialize(settings, worldRootTransform, ResolvePlayerTransform(), spawnHex, hexWorld);
            hexWorld.SetNoiseOrigin(spawnHex);

            chunkManager.ClearMeshesOnly();
            chunkManager.RefreshAroundPlayer();
            while (!HasLoadedChunks())
            {
                yield return null;
            }

            Transform player = SetupPlayer(spawnCamera, playerInventory);
            worldBoundary.Initialize(settings, worldScroller, worldRootTransform);
            SetupCelestial(player);
            skyController.Initialize(celestialSystem, settings);
            skyClouds.Initialize(celestialSystem, settings, player);
            blockFeedback = BlockEditFeedback.Ensure(transform);
            blockInteractor.Initialize(
                hexWorld,
                settings,
                worldScroller,
                chunkManager,
                spawnCamera != null ? spawnCamera.transform : null,
                player,
                blockHotbar,
                blockFeedback,
                toolState,
                playerInventory);
            saveSystem.Initialize(hexWorld, settings, chunkManager, worldScroller, blockHotbar, toolState, playerInventory, player);
            gameHud.Initialize(worldScroller, chunkManager, celestialSystem, blockHotbar, toolState, runtimeProfiler, settings, blockInteractor, playerInventory);
            biomeAmbience.Initialize(worldScroller, celestialSystem);
            nightSpawner.Initialize(worldScroller, settings);
            dayNight.Initialize(celestialSystem, nightSpawner);
            GrantStarterInventory(playerInventory, registry);

            if (spawnCamera != null)
            {
                spawnCamera.enabled = true;
            }

            stopwatch.Stop();
            UnityEngine.Debug.Log(
                $"World built (seed={settings.Seed}, hexRadius={settings.WorldHexRadius}, preset={settings.PerformancePreset}, spawn={spawnHex}): " +
                $"buildTime={stopwatch.Elapsed.TotalSeconds:F1}s.");

            overlay.Report(1f, "Ready.");
            yield return null;
            overlay.SetVisible(false);
            buildComplete = true;
        }

        T GetOrAdd<T>() where T : Component
        {
            T component = GetComponent<T>();
            if (component == null)
            {
                component = gameObject.AddComponent<T>();
            }

            return component;
        }

        bool HasLoadedChunks()
        {
            return chunkManager != null && chunkManager.LoadedChunkCount > 0;
        }

        Transform SetupPlayer(FlatSpawnCamera camera, PlayerInventory inventory)
        {
            Transform player = ResolvePlayerTransform();
            if (player == null)
            {
                player = new GameObject("Player").transform;
            }

            player.SetParent(null, true);

            if (player.GetComponent<CharacterController>() == null)
            {
                player.gameObject.AddComponent<CharacterController>();
            }

            playerController = player.GetComponent<FlatPlayerController>();
            if (playerController == null)
            {
                playerController = player.gameObject.AddComponent<FlatPlayerController>();
            }

            playerController.Initialize(settings, worldScroller, chunkManager, camera != null ? camera.transform : null, hexWorld);

            if (camera != null)
            {
                camera.TrySpawn(hexWorld, settings, player);
            }
            else
            {
                float y = hexWorld.GetSurfaceWorldY(worldScroller.PlayerWorldHex);
                player.position = new Vector3(0f, y + settings.PlayerHeight * 0.5f, 0f);
            }

            playerController.SnapToGround();
            playerController.enabled = true;

            PickupCollector pickupCollector = player.GetComponent<PickupCollector>();
            if (pickupCollector == null)
            {
                pickupCollector = player.gameObject.AddComponent<PickupCollector>();
            }

            pickupCollector.Initialize(inventory);
            return player;
        }

        void SetupCelestial(Transform player)
        {
            celestialSystem = GetOrAdd<CelestialSystem>();
            celestialSystem.Initialize(settings, directionalLight, player);
        }

        Transform ResolvePlayerTransform()
        {
            if (playerRoot != null)
            {
                return playerRoot;
            }

            GameObject existing = GameObject.Find("Player");
            return existing != null ? existing.transform : null;
        }

        void ClearWorld()
        {
            if (chunkManager != null)
            {
                chunkManager.ClearAll();
            }

            Transform root = chunkRoot != null ? chunkRoot : transform;
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Destroy(root.GetChild(i).gameObject);
            }

            if (worldRootTransform != null)
            {
                Destroy(worldRootTransform.gameObject);
                worldRootTransform = null;
            }
        }

        static void GrantStarterInventory(PlayerInventory inventory, BlockRegistry registry)
        {
            if (inventory == null || registry == null)
            {
                return;
            }

            string[] names =
            {
                "Grass", "Dirt", "Stone", "Sand", "Gravel", "Snow", "Dark Grass", "Fungus", "Crystal",
            };
            int[] amounts = { 24, 24, 16, 12, 12, 8, 8, 6, 4 };

            for (int i = 0; i < names.Length; i++)
            {
                if (registry.TryGetByName(names[i], out BlockDefinition definition))
                {
                    inventory.Add(definition.BlockId, amounts[i]);
                }
            }
        }

        void Update()
        {
            if (!buildComplete || chunkManager == null || worldScroller == null)
            {
                return;
            }

            HexCoord before = worldScroller.PlayerWorldHex;
            chunkManager.RefreshAroundPlayer();
            if (before != worldScroller.PlayerWorldHex)
            {
                hexWorld.SetNoiseOrigin(worldScroller.PlayerWorldHex);
                chunkManager.RefreshAroundPlayer(forceRebuildMeshes: true);
            }
        }
    }
}
