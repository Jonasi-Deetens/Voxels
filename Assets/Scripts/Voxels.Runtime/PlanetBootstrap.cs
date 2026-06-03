using System.Collections;
using System.Diagnostics;
using UnityEngine;
using Voxels.Core.Hex;
using Voxels.World;

namespace Voxels.Runtime
{
    [DefaultExecutionOrder(-200)]
    public sealed class PlanetBootstrap : MonoBehaviour
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
        WorldDebugOverlay debugOverlay;
        HexBlockInteractor blockInteractor;
        ProceduralSkyController skyController;
        FlatPlayerController playerController;
        FlatSpawnCamera spawnCamera;
        CelestialSystem celestialSystem;
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
                UnityEngine.Debug.LogError("PlanetBootstrap requires WorldSettings.");
                yield break;
            }

            if (settings.BiomeCatalog == null && settings.Biome == null)
            {
                UnityEngine.Debug.LogError("WorldSettings needs BiomeCatalog. Run Voxels/Setup Default Content.");
                yield break;
            }

            PlanetBuildOverlay overlay = PlanetBuildOverlay.Ensure();
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
            debugOverlay = GetOrAdd<WorldDebugOverlay>();
            blockInteractor = GetOrAdd<HexBlockInteractor>();
            skyController = GetOrAdd<ProceduralSkyController>();

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

            chunkManager.ClearMeshesOnly();
            chunkManager.RefreshAroundPlayer();
            while (!HasLoadedChunks())
            {
                yield return null;
            }

            SetupPlayer(spawnCamera);
            worldBoundary.Initialize(settings, worldScroller, worldRootTransform);
            SetupCelestial(ResolvePlayerTransform());
            skyController.Initialize(celestialSystem);
            blockInteractor.Initialize(hexWorld, settings, worldScroller, chunkManager, spawnCamera != null ? spawnCamera.transform : null);
            debugOverlay.Initialize(worldScroller, chunkManager, celestialSystem, blockInteractor);

            if (spawnCamera != null)
            {
                spawnCamera.enabled = true;
            }

            stopwatch.Stop();
            UnityEngine.Debug.Log(
                $"World built (seed={settings.Seed}, hexRadius={settings.WorldHexRadius}, spawn={spawnHex}): " +
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

        void SetupPlayer(FlatSpawnCamera camera)
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

            playerController.Initialize(settings, worldScroller, chunkManager, camera != null ? camera.transform : null);

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
                chunkManager.RefreshAroundPlayer(forceRebuildMeshes: true);
            }
        }
    }
}
