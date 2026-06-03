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

            FlatSpawnCamera camera = FindAnyObjectByType<FlatSpawnCamera>();
            if (camera != null)
            {
                camera.enabled = false;
            }

            ClearWorld();

            BlockRegistry registry = BlockRegistryBuilder.Build(settings, blockDefinitions);
            hexWorld = new HexWorld(settings, registry);

            var worldRootObject = new GameObject("WorldRoot");
            worldRootTransform = worldRootObject.transform;
            worldRootTransform.SetParent(transform, false);

            Transform chunksParent = chunkRoot != null ? chunkRoot : worldRootTransform;

            worldScroller = gameObject.GetComponent<WorldScroller>();
            if (worldScroller == null)
            {
                worldScroller = gameObject.AddComponent<WorldScroller>();
            }

            chunkManager = gameObject.GetComponent<HexChunkManager>();
            if (chunkManager == null)
            {
                chunkManager = gameObject.AddComponent<HexChunkManager>();
            }

            chunkManager.Initialize(hexWorld, settings, worldScroller, chunksParent);
            worldScroller.Initialize(settings, worldRootTransform, ResolvePlayerTransform(), HexCoord.Zero);

            overlay.Report(0.2f, "Loading terrain…");
            chunkManager.RefreshAroundPlayer();
            while (chunkManager == null || !HasLoadedChunks())
            {
                yield return null;
            }

            yield return new WaitForSeconds(0.1f);

            HexCoord spawnHex = HexCoord.Zero;
            if (camera != null)
            {
                camera.enabled = true;
            }

            SetupPlayer(camera, ref spawnHex);
            worldScroller.Initialize(settings, worldRootTransform, ResolvePlayerTransform(), spawnHex);
            chunkManager.ClearAll();
            chunkManager.RefreshAroundPlayer();

            while (!HasLoadedChunks())
            {
                yield return null;
            }

            SetupCelestial(ResolvePlayerTransform());
            if (playerController != null)
            {
                playerController.enabled = true;
                playerController.SnapToGround();
            }

            if (camera != null)
            {
                camera.enabled = true;
            }

            stopwatch.Stop();
            UnityEngine.Debug.Log(
                $"World built (seed={settings.Seed}, hexRadius={settings.WorldHexRadius}): " +
                $"buildTime={stopwatch.Elapsed.TotalSeconds:F1}s.");

            overlay.Report(1f, "Ready.");
            yield return null;
            overlay.SetVisible(false);
            buildComplete = true;
        }

        bool HasLoadedChunks()
        {
            return chunkRoot != null
                ? chunkRoot.childCount > 0
                : worldRootTransform != null && worldRootTransform.childCount > 0;
        }

        void SetupPlayer(FlatSpawnCamera camera, ref HexCoord spawnHex)
        {
            Transform player = ResolvePlayerTransform();
            if (player == null)
            {
                var playerObject = new GameObject("Player");
                player = playerObject.transform;
            }

            player.SetParent(null, true);
            player.position = Vector3.zero;

            var controller = player.GetComponent<CharacterController>();
            if (controller == null)
            {
                controller = player.gameObject.AddComponent<CharacterController>();
            }

            playerController = player.GetComponent<FlatPlayerController>();
            if (playerController == null)
            {
                playerController = player.gameObject.AddComponent<FlatPlayerController>();
            }

            playerController.enabled = false;
            playerController.Initialize(settings, worldScroller, chunkManager, camera != null ? camera.transform : null);

            if (camera != null)
            {
                camera.TrySpawn(hexWorld, settings, player);
                spawnHex = camera.SpawnHex;
            }

            worldScroller.Initialize(settings, worldRootTransform, player, spawnHex);
        }

        void SetupCelestial(Transform player)
        {
            celestialSystem = GetComponent<CelestialSystem>();
            if (celestialSystem == null)
            {
                celestialSystem = gameObject.AddComponent<CelestialSystem>();
            }

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
            HexCoord after = worldScroller.PlayerWorldHex;
            if (before != after)
            {
                chunkManager.RefreshAroundPlayer(forceRebuildMeshes: true);
            }
        }
    }
}
