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

            ClearChunks();
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;

            BlockRegistry registry = BlockRegistryBuilder.Build(settings, blockDefinitions);
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
            int meshBatch = 0;

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
                    var meshCollider = chunkObject.AddComponent<MeshCollider>();

                    Mesh mesh = ChunkMeshFactory.CreateMesh(meshData);
                    meshFilter.sharedMesh = mesh;
                    meshRenderer.sharedMaterials = ChunkMeshFactory.GetMaterials(meshData);
                    meshCollider.sharedMesh = mesh;
                }

                meshBatch++;
                if (meshBatch >= 8)
                {
                    meshBatch = 0;
                    float meshProgress = 0.92f + 0.06f * (i + 1) / chunkGroups.Length;
                    overlay.Report(meshProgress, $"Meshing chunk {i + 1}/{chunkGroups.Length}…");
                    yield return null;
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
    }
}
