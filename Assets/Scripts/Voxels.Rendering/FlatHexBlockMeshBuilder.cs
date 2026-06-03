using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Voxels.Core.Blocks;
using Voxels.Core.Hex;
using Voxels.World;

namespace Voxels.Rendering
{
    public sealed class FlatHexBlockMeshBuilder
    {
        readonly HexWorld world;
        readonly float blockSize;

        readonly int[] sortedNeighborScratch = new int[6];
        readonly float3[] bottomCornerScratch = new float3[6];
        readonly float3[] topCornerScratch = new float3[6];

        public FlatHexBlockMeshBuilder(HexWorld world)
        {
            this.world = world;
            blockSize = world.BlockSize;
        }

        public ChunkMeshData BuildChunk(
            in HexCoord playerHex,
            IReadOnlyList<HexCoord> worldHexes)
        {
            var meshData = new ChunkMeshData();
            var materialIndices = new Dictionary<Material, int>();

            for (int i = 0; i < worldHexes.Count; i++)
            {
                HexCoord worldHex = worldHexes[i];
                if (!world.Columns.TryGetColumn(worldHex, out BlockColumn column))
                {
                    continue;
                }

                HexCoord localHex = world.WorldToLocal(worldHex, playerHex);
                float3 cellCenter = FlatHexGrid.AxialToWorld(localHex, blockSize);
                int surfaceHeight = column.SurfaceHeight;
                int maxLayer = math.min(
                    surfaceHeight + world.Settings.MaxHeightAboveSurface,
                    world.Settings.ColumnCapacity - 1);

                for (int layer = 0; layer <= maxLayer; layer++)
                {
                    BlockId blockId = column.GetBlock(layer);
                    if (blockId.IsAir || !world.BlockRegistry.TryGetDefinition(blockId, out BlockDefinition definition))
                    {
                        continue;
                    }

                    if (!definition.IsOpaque)
                    {
                        continue;
                    }

                    int materialIndex = GetOrAddMaterial(
                        meshData,
                        materialIndices,
                        definition.Material,
                        definition.IsOpaque);
                    BuildBlock(meshData, materialIndex, worldHex, localHex, cellCenter, layer, column, maxLayer);
                }
            }

            return meshData;
        }

        void BuildBlock(
            ChunkMeshData meshData,
            int materialIndex,
            in HexCoord worldHex,
            in HexCoord localHex,
            float3 cellCenter,
            int layer,
            BlockColumn column,
            int maxLayer)
        {
            FlatHexGeometry.GetSortedNeighborIndices(localHex, sortedNeighborScratch);
            float y0 = layer * blockSize;
            float y1 = (layer + 1) * blockSize;
            float3 bottomCenter = cellCenter + new float3(0f, y0, 0f);
            float3 topCenter = cellCenter + new float3(0f, y1, 0f);

            FlatHexGeometry.GetBlockCorners(bottomCenter, blockSize, bottomCornerScratch);
            FlatHexGeometry.GetBlockCorners(topCenter, blockSize, topCornerScratch);

            BlockId blockId = column.GetBlock(layer);
            bool isOpaque = world.BlockRegistry.GetDefinition(blockId).IsOpaque;

            if (ShouldShowTopFace(column, layer, maxLayer, isOpaque))
            {
                AddPolygon(meshData, materialIndex, topCornerScratch, 6, Vector3.up);
            }

            if (ShouldShowBottomFace(column, layer))
            {
                AddPolygon(meshData, materialIndex, bottomCornerScratch, 6, Vector3.down);
            }

            for (int side = 0; side < 6; side++)
            {
                int neighborDir = sortedNeighborScratch[side];
                HexCoord neighborWorld = worldHex.Add(HexCoord.NeighborOffsets[neighborDir]);
                if (ShouldCullSide(column, layer, neighborWorld, blockId, isOpaque))
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

        bool ShouldShowTopFace(BlockColumn column, int layer, int maxLayer, bool sourceIsOpaque)
        {
            if (layer >= maxLayer)
            {
                return true;
            }

            if (!sourceIsOpaque)
            {
                return column.GetBlock(layer + 1).IsAir;
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

        bool ShouldCullSide(
            BlockColumn column,
            int layer,
            in HexCoord neighborWorld,
            BlockId blockId,
            bool sourceIsOpaque)
        {
            if (!world.Columns.TryGetColumn(neighborWorld, out BlockColumn neighborColumn))
            {
                return false;
            }

            BlockId neighborBlockId = neighborColumn.GetBlock(layer);
            if (!sourceIsOpaque)
            {
                return neighborBlockId == blockId;
            }

            return IsOpaqueAt(neighborColumn, layer);
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
