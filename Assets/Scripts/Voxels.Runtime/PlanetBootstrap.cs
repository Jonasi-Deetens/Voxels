using System.Collections.Generic;
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

        PlanetWorld planetWorld;

        public PlanetWorld PlanetWorld => planetWorld;

        void Awake()
        {
            BuildPlanet();
        }

        public void BuildPlanet()
        {
            if (settings == null)
            {
                Debug.LogError("PlanetBootstrap requires PlanetSettings.");
                return;
            }

            if (settings.Biome == null)
            {
                Debug.LogError("PlanetSettings.Biome is not assigned. Run Voxels/Setup Default Content.");
                return;
            }

            ClearChunks();

            BlockRegistry registry = BlockRegistryBuilder.Build(settings, blockDefinitions);

            planetWorld = new PlanetWorld(settings, registry);
            planetWorld.Generate(new PlanetLayerGenerator(settings));

            var surfaceCamera = FindAnyObjectByType<SurfaceSpawnCamera>();

            var meshBuilder = new HexBlockMeshBuilder(planetWorld);
            List<int>[] chunkGroups = PlanetChunkUtility.BuildChunkCellGroups(
                planetWorld.Grid.CellCount,
                settings.CellsPerChunk);

            Transform root = chunkRoot != null ? chunkRoot : transform;
            int chunkCount = 0;
            int waterVertices = 0;

            for (int i = 0; i < chunkGroups.Length; i++)
            {
                ChunkMeshData meshData = meshBuilder.BuildChunk(chunkGroups[i]);
                if (meshData.IsEmpty)
                {
                    continue;
                }

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

            Debug.Log(
                $"Planet built (seed={settings.Seed}): {planetWorld.Grid.CellCount} cells, " +
                $"{chunkCount}/{chunkGroups.Length} terrain chunks, waterVertices={waterVertices}, " +
                $"shellRadius={planetWorld.ShellRadius:F1}.");

            if (chunkCount == 0)
            {
                Debug.LogError("Planet built zero visible chunks. Check block definitions and biome block references.");
            }

            if (surfaceCamera != null && surfaceCamera.TrySpawnOnSurface())
            {
                transform.position = -surfaceCamera.SpawnGroundPosition;
                surfaceCamera.ApplyPlanetRecenter();
            }
        }

        void ClearChunks()
        {
            Transform root = chunkRoot != null ? chunkRoot : transform;
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Destroy(root.GetChild(i).gameObject);
            }
        }
    }
}
