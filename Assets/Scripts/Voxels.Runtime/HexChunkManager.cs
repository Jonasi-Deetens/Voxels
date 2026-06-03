using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
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
            public List<HexCoord> CoreHexes;
        }

        sealed class MeshBuildRequest
        {
            public ChunkCoord Chunk;
            public HexCoord PlayerHex;
        }

        readonly Dictionary<ChunkCoord, LoadedChunk> loadedChunks = new Dictionary<ChunkCoord, LoadedChunk>();
        readonly Queue<MeshBuildRequest> meshQueue = new Queue<MeshBuildRequest>();
        readonly HashSet<HexCoord> protectedHexes = new HashSet<HexCoord>();

        HexWorld hexWorld;
        WorldSettings settings;
        WorldScroller scroller;
        Transform chunkRoot;
        FlatTerrainGenerator terrainGenerator;
        FlatHexBlockMeshBuilder blockMeshBuilder;
        FlatHexColumnMeshBuilder columnMeshBuilder;
        FlatWaterMeshBuilder waterMeshBuilder;
        WorldRuntimeProfiler runtimeProfiler;
        WorldRegionLoader regionLoader;
        HexCoord lastPlayerHex = new HexCoord(int.MinValue, int.MinValue);
        bool isLoading;
        Coroutine meshQueueRoutine;

        public HexWorld HexWorld => hexWorld;
        public int LoadedChunkCount => loadedChunks.Count;
        public int PendingMeshJobs => meshQueue.Count;

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
            blockMeshBuilder = new FlatHexBlockMeshBuilder(hexWorld);
            columnMeshBuilder = new FlatHexColumnMeshBuilder(hexWorld);
            waterMeshBuilder = new FlatWaterMeshBuilder(hexWorld);
            runtimeProfiler = FindAnyObjectByType<WorldRuntimeProfiler>();
            regionLoader = FindAnyObjectByType<WorldRegionLoader>();
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
            hexWorld.SetNoiseOrigin(playerHex);
            if (forceRebuildMeshes && loadedChunks.Count > 0)
            {
                RebuildAllMeshes(playerHex);
                return;
            }

            StartCoroutine(RefreshChunksAsync(playerHex));
        }

        public void ClearMeshesOnly()
        {
            meshQueue.Clear();
            var chunks = new List<ChunkCoord>(loadedChunks.Keys);
            for (int i = 0; i < chunks.Count; i++)
            {
                UnloadChunkMeshes(chunks[i]);
            }
        }

        public void ClearAll()
        {
            meshQueue.Clear();
            var chunks = new List<ChunkCoord>(loadedChunks.Keys);
            for (int i = 0; i < chunks.Count; i++)
            {
                UnloadChunk(chunks[i]);
            }

            hexWorld?.DataCache.Clear();
            protectedHexes.Clear();
            lastPlayerHex = new HexCoord(int.MinValue, int.MinValue);
        }


        public void RebuildChunksForWorldHex(HexCoord worldHex, HexCoord playerHex)
        {
            int chunkSize = settings.ChunkSizeHex;
            ChunkCoord center = ChunkCoord.FromHex(worldHex, chunkSize);
            for (int dq = -1; dq <= 1; dq++)
            {
                for (int dr = -1; dr <= 1; dr++)
                {
                    var chunk = new ChunkCoord(center.Q + dq, center.R + dr);
                    if (loadedChunks.ContainsKey(chunk))
                    {
                        EnqueueMeshBuild(chunk, playerHex);
                    }
                }
            }

            EnsureMeshQueueRunning();
        }

        ChunkMeshData BuildTerrainMesh(HexCoord playerHex, System.Collections.Generic.IReadOnlyList<HexCoord> hexes)
        {
            if (settings.UseGreedyColumnMeshing)
            {
                return columnMeshBuilder.BuildChunk(playerHex, hexes);
            }

            return blockMeshBuilder.BuildChunk(playerHex, hexes);
        }

        public void RebuildAllMeshes(HexCoord playerHex)
        {
            foreach (ChunkCoord chunk in new List<ChunkCoord>(loadedChunks.Keys))
            {
                BuildChunkMeshes(chunk, playerHex);
            }
        }

        IEnumerator RefreshChunksAsync(HexCoord playerHex)
        {
            isLoading = true;
            int chunkSize = settings.ChunkSizeHex;
            int viewRadius = settings.ViewRadiusChunks;
            int padding = settings.ChunkMeshPadding;

            var needed = new HashSet<ChunkCoord>();
            foreach (ChunkCoord chunk in HexChunkUtility.EnumerateChunksAround(playerHex, viewRadius, chunkSize))
            {
                needed.Add(chunk);
            }

            HexChunkUtility.CollectProtectedHexes(
                hexWorld, playerHex, viewRadius, chunkSize, padding, protectedHexes);

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
                if (i % 2 == 0)
                {
                    yield return null;
                }
            }

            foreach (HexCoord dirtyHex in hexWorld.GetDirtyHexes())
            {
                protectedHexes.Add(dirtyHex);
            }

            hexWorld.TrimCache(protectedHexes);

            var frameBudget = new BuildFrameBudget(settings.BuildFrameBudgetMs);
            foreach (ChunkCoord chunk in needed)
            {
                if (!loadedChunks.ContainsKey(chunk))
                {
                    yield return LoadChunkDataAsync(chunk, frameBudget);
                    EnqueueMeshBuild(chunk, playerHex);
                }
                else
                {
                    BuildChunkMeshes(chunk, playerHex);
                }

                if (frameBudget.ShouldYield())
                {
                    yield return null;
                    frameBudget.MarkYield();
                }
            }

            EnsureMeshQueueRunning();
            isLoading = false;
        }

        IEnumerator LoadChunkDataAsync(ChunkCoord chunk, BuildFrameBudget frameBudget)
        {
            if (!ChunkNeedsGeneration(chunk))
            {
                loadedChunks[chunk] = new LoadedChunk
                {
                    CoreHexes = HexChunkUtility.CollectChunkHexes(hexWorld, chunk, settings.ChunkSizeHex),
                };
                yield break;
            }

            if (regionLoader != null && regionLoader.TryLoadRegionForChunk(chunk))
            {
                // Region columns restored from save.
            }
            else
            {
                terrainGenerator.GenerateChunk(hexWorld, chunk, settings.ChunkMeshPadding);
            }
            loadedChunks[chunk] = new LoadedChunk
            {
                CoreHexes = HexChunkUtility.CollectChunkHexes(hexWorld, chunk, settings.ChunkSizeHex),
            };

            if (frameBudget.ShouldYield())
            {
                yield return null;
                frameBudget.MarkYield();
            }
        }

        bool ChunkNeedsGeneration(ChunkCoord chunk)
        {
            List<HexCoord> core = HexChunkUtility.CollectChunkHexes(hexWorld, chunk, settings.ChunkSizeHex);
            for (int i = 0; i < core.Count; i++)
            {
                HexCoord hex = core[i];
                if (hexWorld.IsColumnDirty(hex))
                {
                    continue;
                }

                if (!hexWorld.DataCache.HasColumn(hex))
                {
                    return true;
                }
            }

            return false;
        }

        void EnqueueMeshBuild(ChunkCoord chunk, HexCoord playerHex)
        {
            meshQueue.Enqueue(new MeshBuildRequest { Chunk = chunk, PlayerHex = playerHex });
            EnsureMeshQueueRunning();
        }

        void EnsureMeshQueueRunning()
        {
            if (meshQueueRoutine == null && meshQueue.Count > 0)
            {
                meshQueueRoutine = StartCoroutine(ProcessMeshQueue());
            }
        }

        IEnumerator ProcessMeshQueue()
        {
            var frameBudget = new BuildFrameBudget(settings.BuildFrameBudgetMs);
            while (meshQueue.Count > 0)
            {
                MeshBuildRequest request = meshQueue.Dequeue();
                yield return BuildChunkMeshesAsync(request.Chunk, request.PlayerHex);

                if (frameBudget.ShouldYield())
                {
                    yield return null;
                    frameBudget.MarkYield();
                }
            }

            meshQueueRoutine = null;
        }

        IEnumerator BuildChunkMeshesAsync(ChunkCoord chunk, HexCoord playerHex)
        {
            if (!loadedChunks.TryGetValue(chunk, out LoadedChunk loaded) || loaded.CoreHexes == null)
            {
                yield break;
            }

            ChunkMeshData terrainData;
            ChunkMeshData waterData;

            if (settings.UseBackgroundMeshBuild)
            {
                Task<ChunkMeshData> terrainTask = Task.Run(
                    () => BuildTerrainMesh(playerHex, loaded.CoreHexes));
                Task<ChunkMeshData> waterTask = Task.Run(
                    () => waterMeshBuilder.BuildChunk(playerHex, loaded.CoreHexes));

                while (!terrainTask.IsCompleted || !waterTask.IsCompleted)
                {
                    yield return null;
                }

                terrainData = terrainTask.Result;
                waterData = waterTask.Result;
            }
            else
            {
                terrainData = BuildTerrainMesh(playerHex, loaded.CoreHexes);
                waterData = waterMeshBuilder.BuildChunk(playerHex, loaded.CoreHexes);
            }

            float buildStart = Time.realtimeSinceStartup;
            ApplyChunkMeshData(chunk, loaded, terrainData, waterData);
            if (runtimeProfiler != null)
            {
                runtimeProfiler.RecordMeshBuild((Time.realtimeSinceStartup - buildStart) * 1000f);
            }
        }

        void BuildChunkMeshes(ChunkCoord chunk, HexCoord playerHex)
        {
            if (!loadedChunks.TryGetValue(chunk, out LoadedChunk loaded) || loaded.CoreHexes == null)
            {
                return;
            }

            ChunkMeshData terrainData = BuildTerrainMesh(playerHex, loaded.CoreHexes);
            ChunkMeshData waterData = waterMeshBuilder.BuildChunk(playerHex, loaded.CoreHexes);
            float buildStart = Time.realtimeSinceStartup;
            ApplyChunkMeshData(chunk, loaded, terrainData, waterData);
            if (runtimeProfiler != null)
            {
                runtimeProfiler.RecordMeshBuild((Time.realtimeSinceStartup - buildStart) * 1000f);
            }
        }

        void ApplyChunkMeshData(
            ChunkCoord chunk,
            LoadedChunk loaded,
            ChunkMeshData terrainData,
            ChunkMeshData waterData)
        {

            if (loaded.TerrainObject == null && !terrainData.IsEmpty)
            {
                loaded.TerrainObject = new GameObject($"Chunk_{chunk.Q}_{chunk.R}");
                loaded.TerrainObject.transform.SetParent(chunkRoot, false);
                loaded.TerrainObject.AddComponent<MeshFilter>();
                loaded.TerrainObject.AddComponent<MeshRenderer>();

                if (settings.CreateTerrainColliders)
                {
                    loaded.TerrainObject.AddComponent<MeshCollider>();
                }
            }

            if (loaded.TerrainObject != null)
            {
                if (terrainData.IsEmpty)
                {
                    loaded.TerrainObject.SetActive(false);
                }
                else
                {
                    loaded.TerrainObject.SetActive(true);
                    ReplaceMesh(ref loaded.TerrainMesh, terrainData);
                    var filter = loaded.TerrainObject.GetComponent<MeshFilter>();
                    var renderer = loaded.TerrainObject.GetComponent<MeshRenderer>();
                    filter.sharedMesh = loaded.TerrainMesh;
                    renderer.sharedMaterials = ChunkMeshFactory.GetMaterials(terrainData);

                    var collider = loaded.TerrainObject.GetComponent<MeshCollider>();
                    if (collider != null)
                    {
                        collider.sharedMesh = null;
                        collider.sharedMesh = loaded.TerrainMesh;
                    }
                }
            }

            if (loaded.WaterObject == null && !waterData.IsEmpty)
            {
                loaded.WaterObject = new GameObject($"Water_{chunk.Q}_{chunk.R}");
                loaded.WaterObject.transform.SetParent(chunkRoot, false);
                loaded.WaterObject.AddComponent<MeshFilter>();
                loaded.WaterObject.AddComponent<MeshRenderer>();
            }

            if (loaded.WaterObject != null)
            {
                if (waterData.IsEmpty)
                {
                    loaded.WaterObject.SetActive(false);
                }
                else
                {
                    loaded.WaterObject.SetActive(true);
                    ReplaceMesh(ref loaded.WaterMesh, waterData);
                    var filter = loaded.WaterObject.GetComponent<MeshFilter>();
                    var renderer = loaded.WaterObject.GetComponent<MeshRenderer>();
                    filter.sharedMesh = loaded.WaterMesh;
                    renderer.sharedMaterials = ChunkMeshFactory.GetMaterials(waterData);
                }
            }
        }

        static void ReplaceMesh(ref Mesh mesh, ChunkMeshData data)
        {
            if (mesh != null)
            {
                Destroy(mesh);
            }

            mesh = ChunkMeshFactory.CreateMesh(data);
        }

        void UnloadChunkMeshes(ChunkCoord chunk)
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

            loadedChunks.Remove(chunk);
        }

        void UnloadChunk(ChunkCoord chunk)
        {
            UnloadChunkMeshes(chunk);
        }
    }
}
