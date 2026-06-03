using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Voxels.Core.Blocks;
using Voxels.Core.Hex;
using Voxels.World;

namespace Voxels.Rendering
{
    /// <summary>
    /// Greedy vertical column meshing: merges consecutive layers of the same opaque block into one hex prism.
    /// </summary>
    public sealed class FlatHexColumnMeshBuilder
    {
        readonly HexWorld world;
        readonly float blockSize;
        readonly int[] sortedNeighborScratch = new int[6];
        readonly float3[] bottomCornerScratch = new float3[6];
        readonly float3[] topCornerScratch = new float3[6];

        public FlatHexColumnMeshBuilder(HexWorld world)
        {
            this.world = world;
            blockSize = world.BlockSize;
        }

        public ChunkMeshData BuildChunk(in HexCoord playerHex, IReadOnlyList<HexCoord> worldHexes)
        {
            var meshData = new ChunkMeshData();
            var materialIndices = new Dictionary<Material, int>();

            for (int i = 0; i < worldHexes.Count; i++)
            {
                HexCoord worldHex = worldHexes[i];
                if (!world.TryGetColumn(worldHex, out BlockColumn column))
                {
                    continue;
                }

                HexCoord localHex = world.WorldToLocal(worldHex, playerHex);
                float3 cellCenter = FlatHexGrid.AxialToWorld(localHex, blockSize);
                int maxLayer = math.min(
                    column.SurfaceHeight + world.Settings.MaxHeightAboveSurface,
                    world.Settings.ColumnCapacity - 1);

                int layer = 0;
                while (layer <= maxLayer)
                {
                    BlockId blockId = column.GetBlock(layer);
                    if (blockId.IsAir ||
                        !world.BlockRegistry.TryGetDefinition(blockId, out BlockDefinition definition) ||
                        !definition.IsOpaque)
                    {
                        layer++;
                        continue;
                    }

                    int startLayer = layer;
                    while (layer + 1 <= maxLayer && column.GetBlock(layer + 1) == blockId)
                    {
                        layer++;
                    }

                    int endLayer = layer;
                    int materialIndex = GetOrAddMaterial(meshData, materialIndices, definition.Material, true);
                    BuildColumnSegment(
                        meshData,
                        materialIndex,
                        worldHex,
                        localHex,
                        cellCenter,
                        column,
                        startLayer,
                        endLayer,
                        maxLayer,
                        blockId);
                    layer++;
                }
            }

            return meshData;
        }

        void BuildColumnSegment(
            ChunkMeshData meshData,
            int materialIndex,
            in HexCoord worldHex,
            in HexCoord localHex,
            float3 cellCenter,
            BlockColumn column,
            int startLayer,
            int endLayer,
            int maxLayer,
            BlockId blockId)
        {
            FlatHexGeometry.GetSortedNeighborIndices(localHex, sortedNeighborScratch);
            float y0 = startLayer * blockSize;
            float y1 = (endLayer + 1) * blockSize;
            float3 bottomCenter = cellCenter + new float3(0f, y0, 0f);
            float3 topCenter = cellCenter + new float3(0f, y1, 0f);

            FlatHexGeometry.GetBlockCorners(bottomCenter, blockSize, bottomCornerScratch);
            FlatHexGeometry.GetBlockCorners(topCenter, blockSize, topCornerScratch);

            if (ShouldShowTopFace(column, endLayer, maxLayer, true))
            {
                AddPolygon(meshData, materialIndex, topCornerScratch, 6, Vector3.up);
            }

            if (ShouldShowBottomFace(column, startLayer))
            {
                AddPolygon(meshData, materialIndex, bottomCornerScratch, 6, Vector3.down);
            }

            for (int side = 0; side < 6; side++)
            {
                int neighborDir = sortedNeighborScratch[side];
                HexCoord neighborWorld = worldHex.Add(HexCoord.NeighborOffsets[neighborDir]);
                if (ShouldCullColumnSide(column, startLayer, endLayer, neighborWorld, blockId))
                {
                    continue;
                }

                int next = (side + 1) % 6;
                float3 sideNormal = GetSideNormal(
                    bottomCornerScratch[side],
                    bottomCornerScratch[next],
                    Vector3.up);
                AddQuad(
                    meshData,
                    materialIndex,
                    bottomCornerScratch[side],
                    bottomCornerScratch[next],
                    topCornerScratch[next],
                    topCornerScratch[side],
                    sideNormal);
            }
        }

        bool ShouldCullColumnSide(
            BlockColumn column,
            int startLayer,
            int endLayer,
            in HexCoord neighborWorld,
            BlockId blockId)
        {
            if (!world.TryGetColumn(neighborWorld, out BlockColumn neighborColumn))
            {
                return false;
            }

            for (int layer = startLayer; layer <= endLayer; layer++)
            {
                BlockId neighborId = neighborColumn.GetBlock(layer);
                if (neighborId != blockId || neighborId.IsAir)
                {
                    return false;
                }
            }

            return true;
        }

        bool ShouldShowTopFace(BlockColumn column, int layer, int maxLayer, bool sourceIsOpaque)
        {
            if (layer >= maxLayer)
            {
                return true;
            }

            return !IsOpaqueAt(column, layer + 1);
        }

        bool ShouldShowBottomFace(BlockColumn column, int layer)
        {
            if (layer == 0)
            {
                return true;
            }

            return !IsOpaqueAt(column, layer - 1);
        }

        bool IsOpaqueAt(BlockColumn column, int layer)
        {
            BlockId blockId = column.GetBlock(layer);
            if (blockId.IsAir)
            {
                return false;
            }

            return world.BlockRegistry.TryGetDefinition(blockId, out BlockDefinition definition) && definition.IsOpaque;
        }

        static float3 GetSideNormal(float3 a, float3 b, float3 upHint)
        {
            float3 edge = b - a;
            return math.normalize(math.cross(edge, upHint));
        }

        static int GetOrAddMaterial(
            ChunkMeshData meshData,
            Dictionary<Material, int> materialIndices,
            Material sourceMaterial,
            bool isOpaque)
        {
            Material key = sourceMaterial != null ? sourceMaterial : null;
            if (key != null && materialIndices.TryGetValue(key, out int existing))
            {
                return existing;
            }

            Color fallback = isOpaque ? Color.white : new Color(0.12f, 0.38f, 0.78f, 0.6f);
            Material renderMaterial = BlockMaterialUtility.CreateRenderMaterial(sourceMaterial, isOpaque, fallback);

            int index = meshData.Materials.Count;
            meshData.Materials.Add(renderMaterial);
            meshData.SubmeshTriangles.Add(new List<int>());
            if (key != null)
            {
                materialIndices[key] = index;
            }

            return index;
        }

        static void AddPolygon(ChunkMeshData meshData, int materialIndex, float3[] corners, int cornerCount, float3 normal)
        {
            int start = meshData.Vertices.Count;
            for (int i = 0; i < cornerCount; i++)
            {
                meshData.Vertices.Add(corners[i]);
                meshData.Normals.Add(normal);
            }

            List<int> submesh = meshData.SubmeshTriangles[materialIndex];
            for (int i = 1; i < cornerCount - 1; i++)
            {
                submesh.Add(start);
                submesh.Add(start + i);
                submesh.Add(start + i + 1);
            }
        }

        static void AddQuad(
            ChunkMeshData meshData,
            int materialIndex,
            float3 v0,
            float3 v1,
            float3 v2,
            float3 v3,
            float3 normal)
        {
            int start = meshData.Vertices.Count;
            meshData.Vertices.Add(v0);
            meshData.Vertices.Add(v1);
            meshData.Vertices.Add(v2);
            meshData.Vertices.Add(v3);
            for (int i = 0; i < 4; i++)
            {
                meshData.Normals.Add(normal);
            }

            List<int> submesh = meshData.SubmeshTriangles[materialIndex];
            submesh.Add(start);
            submesh.Add(start + 1);
            submesh.Add(start + 2);
            submesh.Add(start);
            submesh.Add(start + 2);
            submesh.Add(start + 3);
        }
    }
}
