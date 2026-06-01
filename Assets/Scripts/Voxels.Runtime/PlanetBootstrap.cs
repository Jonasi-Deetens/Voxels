using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Voxels.Rendering;
using Voxels.World;
using Voxels.World.Generation;

namespace Voxels.Runtime
{
    [DefaultExecutionOrder(-200)]
    public sealed class PlanetBootstrap : MonoBehaviour
    {
        [SerializeField] PlanetSettings settings;
        [SerializeField] BlockDefinition[] blockDefinitions;
        [SerializeField] Transform chunkRoot;
        [SerializeField] Light directionalLight;
        [SerializeField] Transform playerAnchorRoot;

        PlanetWorld planetWorld;
        PlayerAnchor playerAnchor;
        SurfacePlayerController playerController;
        CelestialSystem celestialSystem;
        bool buildComplete;

        public PlanetWorld PlanetWorld => planetWorld;
        public PlayerAnchor PlayerAnchor => playerAnchor;
        public bool BuildComplete => buildComplete;

        void Awake()
        {
            StartCoroutine(BuildPlanetAsync());
        }

        public void RegeneratePlanet()
        {
            StopAllCoroutines();
            if (Application.isPlaying)
            {
                StartCoroutine(BuildPlanetAsync());
                return;
            }

            IEnumerator build = BuildPlanetAsync();
            while (build.MoveNext())
            {
            }
        }

        public IEnumerator BuildPlanetAsync()
        {
            buildComplete = false;
            var stopwatch = Stopwatch.StartNew();

            if (settings == null)
            {
                UnityEngine.Debug.LogError("PlanetBootstrap requires PlanetSettings.");
                yield break;
            }

            if (settings.BiomeCatalog == null && settings.Biome == null)
            {
                UnityEngine.Debug.LogError("PlanetSettings needs BiomeCatalog. Run Voxels/Setup Default Content.");
                yield break;
            }

            PlanetBuildOverlay overlay = PlanetBuildOverlay.Ensure();
            overlay.SetVisible(true);
            overlay.Report(0f, "Preparing planet…");

            SurfaceSpawnCamera surfaceCamera = FindAnyObjectByType<SurfaceSpawnCamera>();
            if (surfaceCamera != null)
            {
                surfaceCamera.enabled = false;
            }

            if (playerController != null)
            {
                playerController.enabled = false;
            }

            ClearChunks();
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;

            BlockRegistry registry = BlockRegistryBuilder.Build(settings, blockDefinitions);
            overlay.Report(0.01f, "Building planet grid…");
            yield return null;

            planetWorld = new PlanetWorld(settings, registry);

            var generator = new PlanetLayerGenerator(settings);
            yield return generator.GenerateBatched(
                planetWorld,
                PlanetLayerGenerator.DefaultBatchSize,
                overlay.Report);

            Transform root = chunkRoot != null ? chunkRoot : transform;
            var meshBuilder = new HexBlockMeshBuilder(planetWorld);
            List<int>[] chunkGroups = PlanetChunkUtility.BuildChunkCellGroups(
                planetWorld.Grid.CellCount,
                settings.CellsPerChunk);

            int chunkCount = 0;
            int waterVertices = 0;
            var frameBudget = new BuildFrameBudget(settings.BuildFrameBudgetMs);
            var builtChunks = new List<(GameObject chunkObject, Mesh mesh)>(chunkGroups.Length);

            for (int i = 0; i < chunkGroups.Length; i++)
            {
                ChunkMeshData meshData = meshBuilder.BuildChunk(chunkGroups[i]);
                if (!meshData.IsEmpty)
                {
                    chunkCount++;
                    var chunkObject = new GameObject($"Chunk_{i}");
                    chunkObject.transform.SetParent(root, false);

                    var meshFilter = chunkObject.AddComponent<MeshFilter>();
                    var meshRenderer = chunkObject.AddComponent<MeshRenderer>();

                    Mesh mesh = ChunkMeshFactory.CreateMesh(meshData);
                    meshFilter.sharedMesh = mesh;
                    meshRenderer.sharedMaterials = ChunkMeshFactory.GetMaterials(meshData);
                    builtChunks.Add((chunkObject, mesh));
                }

                float meshProgress = 0.92f + 0.04f * (i + 1) / chunkGroups.Length;
                overlay.Report(meshProgress, $"Meshing chunk {i + 1}/{chunkGroups.Length}…");

                if (frameBudget.ShouldYield())
                {
                    yield return null;
                    frameBudget.MarkYield();
                }
            }

            if (settings.CreateTerrainColliders)
            {
                overlay.Report(0.96f, "Building collision…");
                yield return null;

                for (int i = 0; i < builtChunks.Count; i++)
                {
                    (GameObject chunkObject, Mesh mesh) = builtChunks[i];
                    AttachMeshCollider(chunkObject, mesh);

                    float colliderProgress = 0.96f + 0.02f * (i + 1) / builtChunks.Count;
                    overlay.Report(colliderProgress, $"Collision {i + 1}/{builtChunks.Count}…");

                    if (frameBudget.ShouldYield())
                    {
                        yield return null;
                        frameBudget.MarkYield();
                    }
                }
            }

            overlay.Report(0.98f, "Building water…");
            yield return null;

            ChunkMeshData waterMeshData = new WaterMeshBuilder(planetWorld).Build();
            if (!waterMeshData.IsEmpty)
            {
                waterVertices = waterMeshData.Vertices.Count;
                var waterObject = new GameObject("Water");
                waterObject.transform.SetParent(root, false);

                var waterFilter = waterObject.AddComponent<MeshFilter>();
                var waterRenderer = waterObject.AddComponent<MeshRenderer>();

                Mesh waterMesh = ChunkMeshFactory.CreateMesh(waterMeshData);
                waterFilter.sharedMesh = waterMesh;
                waterRenderer.sharedMaterials = ChunkMeshFactory.GetMaterials(waterMeshData);
            }

            SetupPlayerAnchor(surfaceCamera);
            SetupCelestialSystem();

            stopwatch.Stop();
            UnityEngine.Debug.Log(
                $"Planet built (seed={settings.Seed}, subdiv={settings.ResolveSubdivisionLevel()}): " +
                $"{planetWorld.Grid.CellCount} cells, {chunkCount}/{chunkGroups.Length} terrain chunks, " +
                $"waterVertices={waterVertices}, shellRadius={planetWorld.ShellRadius:F1}, " +
                $"buildTime={stopwatch.Elapsed.TotalSeconds:F1}s.");

            if (chunkCount == 0)
            {
                UnityEngine.Debug.LogError("Planet built zero visible chunks. Check block definitions and biome block references.");
            }

            overlay.Report(1f, "Ready.");
            yield return null;
            overlay.SetVisible(false);
            buildComplete = true;

            if (surfaceCamera != null)
            {
                surfaceCamera.enabled = true;
            }

            if (playerController != null)
            {
                playerController.enabled = true;
                var characterController = playerController.GetComponent<CharacterController>();
                if (characterController != null)
                {
                    characterController.enabled = true;
                }
            }
        }

        void SetupCelestialSystem()
        {
            if (celestialSystem == null)
            {
                celestialSystem = GetComponent<CelestialSystem>();
                if (celestialSystem == null)
                {
                    celestialSystem = gameObject.AddComponent<CelestialSystem>();
                }
            }

            celestialSystem.Initialize(settings, planetWorld, directionalLight, playerAnchor);
        }

        void SetupPlayerAnchor(SurfaceSpawnCamera surfaceCamera)
        {
            Transform anchorTransform = playerAnchorRoot;
            if (anchorTransform == null)
            {
                var anchorObject = new GameObject("PlayerAnchor");
                anchorObject.transform.SetParent(transform, false);
                anchorTransform = anchorObject.transform;
            }

            playerAnchor = anchorTransform.GetComponent<PlayerAnchor>();
            if (playerAnchor == null)
            {
                playerAnchor = anchorTransform.gameObject.AddComponent<PlayerAnchor>();
            }

            if (surfaceCamera != null)
            {
                surfaceCamera.transform.SetParent(anchorTransform, false);
                surfaceCamera.TrySpawnOnSurface(allowDuringBuild: true);
            }

            DetachPlayerFromPlanetSpin(anchorTransform, transform);
            EnsurePlayerController(anchorTransform.gameObject, surfaceCamera);
        }

        static void DetachPlayerFromPlanetSpin(Transform anchorTransform, Transform planetTransform)
        {
            if (planetTransform == null)
            {
                return;
            }

            var rigObject = GameObject.Find("PlayerRig");
            if (rigObject == null)
            {
                rigObject = new GameObject("PlayerRig");
            }

            Transform rigTransform = rigObject.transform;
            rigTransform.SetPositionAndRotation(planetTransform.position, Quaternion.identity);
            if (anchorTransform.parent != rigTransform)
            {
                anchorTransform.SetParent(rigTransform, true);
            }
        }

        void EnsurePlayerController(GameObject anchorObject, SurfaceSpawnCamera surfaceCamera)
        {
            var characterController = anchorObject.GetComponent<CharacterController>();
            if (characterController == null)
            {
                characterController = anchorObject.AddComponent<CharacterController>();
            }

            playerController = anchorObject.GetComponent<SurfacePlayerController>();
            if (playerController == null)
            {
                playerController = anchorObject.AddComponent<SurfacePlayerController>();
            }

            playerController.Initialize(
                planetWorld,
                transform,
                surfaceCamera != null ? surfaceCamera.transform : null,
                surfaceCamera);
            playerController.enabled = false;
        }

        void ClearChunks()
        {
            Transform root = chunkRoot != null ? chunkRoot : transform;
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Transform child = root.GetChild(i);
                if (child.GetComponent<PlayerAnchor>() != null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }

        static void AttachMeshCollider(GameObject chunkObject, Mesh mesh)
        {
            var meshCollider = chunkObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = mesh;
            meshCollider.cookingOptions = MeshColliderCookingOptions.CookForFasterSimulation;
        }
    }
}
