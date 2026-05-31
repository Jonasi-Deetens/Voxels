using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Voxels.Core.Blocks;
using Voxels.Core.Sphere;
using Voxels.World;

namespace Voxels.Rendering
{
    public sealed class HexBlockMeshBuilder
    {
        readonly PlanetWorld world;
        readonly float planetRadius;
        readonly float blockHeight;

        readonly int[] sortedNeighborScratch = new int[6];
        readonly float3[] neighborDirScratch = new float3[6];
        readonly float3[] bottomCornerScratch = new float3[6];
        readonly float3[] topCornerScratch = new float3[6];
        readonly int[] sortIndexScratch = new int[6];
        readonly float[] sortAngleScratch = new float[6];

        public HexBlockMeshBuilder(PlanetWorld world)
        {
            this.world = world;
            planetRadius = world.ShellRadius;
            blockHeight = world.BlockHeight;
        }

        public ChunkMeshData BuildChunk(IReadOnlyList<int> cellIndices)
        {
            var meshData = new ChunkMeshData();
            var materialIndices = new Dictionary<Material, int>();

            IcosphereHexGrid grid = world.Grid;
            PlanetColumnStorage columns = world.Columns;

            for (int c = 0; c < cellIndices.Count; c++)
            {
                int cellIndex = cellIndices[c];
                ref readonly SphereHexCell cell = ref grid.GetCell(cellIndex);
                BlockColumn column = columns.GetColumn(cellIndex);
                int surfaceHeight = column.SurfaceHeight;
                int maxLayer = math.max(surfaceHeight, world.Settings.SeaLevelLayer);

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
                    BuildBlock(meshData, materialIndex, cell, layer, column, maxLayer);
                }
            }

            return meshData;
        }

        void BuildBlock(
            ChunkMeshData meshData,
            int materialIndex,
            in SphereHexCell cell,
            int layer,
            BlockColumn column,
            int maxLayer)
        {
            int cornerCount = GetSortedNeighbors(cell, sortedNeighborScratch);
            float innerRadius = planetRadius + layer * blockHeight;
            float outerRadius = planetRadius + (layer + 1) * blockHeight;

            float3 bottomCenter = cell.Normal * innerRadius;
            float3 topCenter = cell.Normal * outerRadius;

            for (int i = 0; i < cornerCount; i++)
            {
                int prev = (i + cornerCount - 1) % cornerCount;
                ref readonly SphereHexCell prevNeighbor = ref world.Grid.GetCell(sortedNeighborScratch[prev]);
                ref readonly SphereHexCell curNeighbor = ref world.Grid.GetCell(sortedNeighborScratch[i]);
                float3 cornerDirection = math.normalize(cell.Normal + prevNeighbor.Normal + curNeighbor.Normal);
                bottomCornerScratch[i] = cornerDirection * innerRadius;
                topCornerScratch[i] = cornerDirection * outerRadius;
            }

            BlockId blockId = column.GetBlock(layer);
            bool isOpaque = world.BlockRegistry.GetDefinition(blockId).IsOpaque;

            if (ShouldShowTopFace(column, layer, maxLayer, isOpaque))
            {
                AddPolygon(meshData, materialIndex, topCornerScratch, cornerCount, math.normalize(topCenter));
            }

            if (ShouldShowBottomFace(column, layer))
            {
                AddPolygon(meshData, materialIndex, bottomCornerScratch, cornerCount, -math.normalize(bottomCenter));
            }

            for (int side = 0; side < cornerCount; side++)
            {
                if (ShouldCullSide(column, layer, sortedNeighborScratch[side], blockId, isOpaque))
                {
                    continue;
                }

                int next = (side + 1) % cornerCount;
                float3 sideNormal = GetSideNormal(
                    bottomCornerScratch[side],
                    bottomCornerScratch[next],
                    topCenter - bottomCenter);
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
                BlockId above = column.GetBlock(layer + 1);
                return above.IsAir;
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
            int neighborIndex,
            BlockId blockId,
            bool sourceIsOpaque)
        {
            BlockColumn neighborColumn = world.Columns.GetColumn(neighborIndex);
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

        int GetSortedNeighbors(in SphereHexCell cell, int[] sortedNeighbors)
        {
            int neighborCount = cell.NeighborCount;
            float3 up = cell.Normal;

            for (int i = 0; i < neighborCount; i++)
            {
                sortedNeighbors[i] = cell.Neighbors[i];
                ref readonly SphereHexCell neighbor = ref world.Grid.GetCell(cell.Neighbors[i]);
                float3 toNeighbor = neighbor.Normal - up * math.dot(neighbor.Normal, up);
                neighborDirScratch[i] = math.normalizesafe(toNeighbor, float3.zero);
            }

            SortNeighborsByAngle(neighborDirScratch, sortedNeighbors, neighborCount, up);
            return neighborCount;
        }

        void SortNeighborsByAngle(float3[] directions, int[] neighborIndices, int count, float3 up)
        {
            float3 reference = directions[0];
            for (int i = 0; i < count; i++)
            {
                sortIndexScratch[i] = neighborIndices[i];
                sortAngleScratch[i] = math.atan2(
                    math.dot(math.cross(reference, directions[i]), up),
                    math.dot(reference, directions[i]));
            }

            for (int i = 1; i < count; i++)
            {
                int index = sortIndexScratch[i];
                float angle = sortAngleScratch[i];
                float3 direction = directions[i];
                int j = i - 1;
                while (j >= 0 && sortAngleScratch[j] > angle)
                {
                    sortIndexScratch[j + 1] = sortIndexScratch[j];
                    sortAngleScratch[j + 1] = sortAngleScratch[j];
                    directions[j + 1] = directions[j];
                    j--;
                }

                sortIndexScratch[j + 1] = index;
                sortAngleScratch[j + 1] = angle;
                directions[j + 1] = direction;
            }

            for (int i = 0; i < count; i++)
            {
                neighborIndices[i] = sortIndexScratch[i];
            }
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
            if (cornerCount < 3)
            {
                return;
            }

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
