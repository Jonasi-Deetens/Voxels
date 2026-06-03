using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Voxels.Core.Hex;
using Voxels.Rendering;
using Voxels.World;
using Voxels.World.Generation;

namespace Voxels.Runtime
{
    public sealed class HexChunkManager : MonoBehaviour
    {
        sealed class LoadedChunk
        {
            public GameObject TerrainObject;
            public GameObject WaterObject;
            public Mesh TerrainMesh;
            public Mesh WaterMesh;
            public List<HexCoord> WorldHexes;
        }

        readonly Dictionary<ChunkCoord, LoadedChunk> loadedChunks = new Dictionary<ChunkCoord, LoadedChunk>();

        HexWorld hexWorld;
        WorldSettings settings;
        WorldScroller scroller;
        Transform chunkRoot;
        FlatTerrainGenerator terrainGenerator;
        FlatHexBlockMeshBuilder meshBuilder;
        FlatWaterMeshBuilder waterMeshBuilder;
        HexCoord lastPlayerHex = new HexCoord(int.MinValue, int.MinValue);
        bool isLoading;

        public HexWorld HexWorld => hexWorld;

        public void Initialize(
            HexWorld world,
            WorldSettings worldSettings,
            WorldScroller worldScroller,
            Transform root)
        {
            hexWorld = world;
            settings = worldSettings;
            scroller = worldScroller;
            chunkRoot = root;
            terrainGenerator = new FlatTerrainGenerator(settings);
            meshBuilder = new FlatHexBlockMeshBuilder(hexWorld);
            waterMeshBuilder = new FlatWaterMeshBuilder(hexWorld);
        }

        public void RefreshAroundPlayer(bool forceRebuildMeshes = false)
        {
            if (hexWorld == null || scroller == null || isLoading)
            {
                return;
            }

            HexCoord playerHex = scroller.PlayerWorldHex;
            if (!forceRebuildMeshes && playerHex == lastPlayerHex && loadedChunks.Count > 0)
            {
                return;
            }

            lastPlayerHex = playerHex;
            if (forceRebuildMeshes && loadedChunks.Count > 0)
            {
                RebuildAllMeshes(playerHex);
                return;
            }

            StartCoroutine(RefreshChunksAsync(playerHex));
        }

        public void RebuildAllMeshes(HexCoord playerHex)
        {
            foreach (ChunkCoord chunk in new List<ChunkCoord>(loadedChunks.Keys))
            {
                RebuildChunkMesh(chunk, playerHex);
            }
        }

        IEnumerator RefreshChunksAsync(HexCoord playerHex)
        {
            isLoading = true;
            int chunkSize = settings.ChunkSizeHex;
            int viewRadius = settings.ViewRadiusChunks;
            var needed = new HashSet<ChunkCoord>();
            foreach (ChunkCoord chunk in HexChunkUtility.EnumerateChunksAround(playerHex, viewRadius, chunkSize))
            {
                needed.Add(chunk);
            }

            var toUnload = new List<ChunkCoord>();
            foreach (ChunkCoord existing in loadedChunks.Keys)
            {
                if (!needed.Contains(existing))
                {
                    toUnload.Add(existing);
                }
            }

            for (int i = 0; i < toUnload.Count; i++)
            {
                UnloadChunk(toUnload[i]);
                yield return null;
            }

            var frameBudget = new BuildFrameBudget(settings.BuildFrameBudgetMs);
            foreach (ChunkCoord chunk in needed)
            {
                if (!loadedChunks.ContainsKey(chunk))
                {
                    yield return LoadChunkAsync(chunk, playerHex, frameBudget);
                }
                else
                {
                    RebuildChunkMesh(chunk, playerHex);
                }

                if (frameBudget.ShouldYield())
                {
                    yield return null;
                    frameBudget.MarkYield();
                }
            }

            isLoading = false;
        }

        IEnumerator LoadChunkAsync(ChunkCoord chunk, HexCoord playerHex, BuildFrameBudget frameBudget)
        {
            terrainGenerator.GenerateChunk(hexWorld, chunk);
            List<HexCoord> worldHexes = HexChunkUtility.CollectChunkHexes(hexWorld, chunk, settings.ChunkSizeHex);
            if (worldHexes.Count == 0)
            {
                yield break;
            }

            ChunkMeshData terrainData = meshBuilder.BuildChunk(playerHex, worldHexes);
            ChunkMeshData waterData = waterMeshBuilder.BuildChunk(playerHex, worldHexes);

            var loaded = new LoadedChunk { WorldHexes = worldHexes };

            if (!terrainData.IsEmpty)
            {
                loaded.TerrainObject = new GameObject($"Chunk_{chunk.Q}_{chunk.R}");
                loaded.TerrainObject.transform.SetParent(chunkRoot, false);

                var meshFilter = loaded.TerrainObject.AddComponent<MeshFilter>();
                var meshRenderer = loaded.TerrainObject.AddComponent<MeshRenderer>();
                loaded.TerrainMesh = ChunkMeshFactory.CreateMesh(terrainData);
                meshFilter.sharedMesh = loaded.TerrainMesh;
                meshRenderer.sharedMaterials = ChunkMeshFactory.GetMaterials(terrainData);

                if (settings.CreateTerrainColliders)
                {
                    var collider = loaded.TerrainObject.AddComponent<MeshCollider>();
                    collider.sharedMesh = loaded.TerrainMesh;
                    collider.cookingOptions = MeshColliderCookingOptions.CookForFasterSimulation;
                }
            }

            if (!waterData.IsEmpty)
            {
                loaded.WaterObject = new GameObject($"Water_{chunk.Q}_{chunk.R}");
                loaded.WaterObject.transform.SetParent(chunkRoot, false);

                var waterFilter = loaded.WaterObject.AddComponent<MeshFilter>();
                var waterRenderer = loaded.WaterObject.AddComponent<MeshRenderer>();
                loaded.WaterMesh = ChunkMeshFactory.CreateMesh(waterData);
                waterFilter.sharedMesh = loaded.WaterMesh;
                waterRenderer.sharedMaterials = ChunkMeshFactory.GetMaterials(waterData);
            }

            loadedChunks[chunk] = loaded;

            if (frameBudget.ShouldYield())
            {
                yield return null;
                frameBudget.MarkYield();
            }
        }

        void RebuildChunkMesh(ChunkCoord chunk, HexCoord playerHex)
        {
            if (!loadedChunks.TryGetValue(chunk, out LoadedChunk loaded) || loaded.WorldHexes == null)
            {
                return;
            }

            ChunkMeshData terrainData = meshBuilder.BuildChunk(playerHex, loaded.WorldHexes);
            if (loaded.TerrainObject != null && loaded.TerrainMesh != null)
            {
                ReplaceMesh(loaded.TerrainMesh, terrainData, out loaded.TerrainMesh);
                loaded.TerrainObject.GetComponent<MeshFilter>().sharedMesh = loaded.TerrainMesh;
                loaded.TerrainObject.GetComponent<MeshRenderer>().sharedMaterials =
                    ChunkMeshFactory.GetMaterials(terrainData);

                var collider = loaded.TerrainObject.GetComponent<MeshCollider>();
                if (collider != null)
                {
                    collider.sharedMesh = null;
                    collider.sharedMesh = loaded.TerrainMesh;
                }
            }

            ChunkMeshData waterData = waterMeshBuilder.BuildChunk(playerHex, loaded.WorldHexes);
            if (loaded.WaterObject != null && loaded.WaterMesh != null)
            {
                ReplaceMesh(loaded.WaterMesh, waterData, out loaded.WaterMesh);
                loaded.WaterObject.GetComponent<MeshFilter>().sharedMesh = loaded.WaterMesh;
                loaded.WaterObject.GetComponent<MeshRenderer>().sharedMaterials =
                    ChunkMeshFactory.GetMaterials(waterData);
            }
        }

        static void ReplaceMesh(Mesh oldMesh, ChunkMeshData data, out Mesh newMesh)
        {
            if (oldMesh != null)
            {
                Object.Destroy(oldMesh);
            }

            newMesh = ChunkMeshFactory.CreateMesh(data);
        }

        void UnloadChunk(ChunkCoord chunk)
        {
            if (!loadedChunks.TryGetValue(chunk, out LoadedChunk loaded))
            {
                return;
            }

            if (loaded.TerrainObject != null)
            {
                Destroy(loaded.TerrainObject);
            }

            if (loaded.WaterObject != null)
            {
                Destroy(loaded.WaterObject);
            }

            if (loaded.TerrainMesh != null)
            {
                Destroy(loaded.TerrainMesh);
            }

            if (loaded.WaterMesh != null)
            {
                Destroy(loaded.WaterMesh);
            }

            if (loaded.WorldHexes != null)
            {
                for (int i = 0; i < loaded.WorldHexes.Count; i++)
                {
                    HexCoord hex = loaded.WorldHexes[i];
                    hexWorld.Columns.RemoveColumn(hex);
                    hexWorld.BiomeMap.SetBiome(hex, null);
                }
            }

            loadedChunks.Remove(chunk);
        }

        public void ClearAll()
        {
            var chunks = new List<ChunkCoord>(loadedChunks.Keys);
            for (int i = 0; i < chunks.Count; i++)
            {
                UnloadChunk(chunks[i]);
            }

            lastPlayerHex = new HexCoord(int.MinValue, int.MinValue);
        }
    }
}
