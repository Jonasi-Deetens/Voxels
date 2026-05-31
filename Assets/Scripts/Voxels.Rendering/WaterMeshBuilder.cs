using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Voxels.Core.Blocks;
using Voxels.Core.Sphere;
using Voxels.World;

namespace Voxels.Rendering
{
    /// <summary>
    /// Water must live in its own mesh — mixed opaque/transparent submeshes in one renderer do not draw in URP.
    /// </summary>
    public sealed class WaterMeshBuilder
    {
        readonly PlanetWorld world;
        readonly float planetRadius;
        readonly float blockHeight;
        readonly BlockId waterId;
        readonly Material waterMaterial;

        readonly int[] sortedNeighborScratch = new int[6];
        readonly float3[] neighborDirScratch = new float3[6];
        readonly float3[] bottomCornerScratch = new float3[6];
        readonly float3[] topCornerScratch = new float3[6];
        readonly int[] sortIndexScratch = new int[6];
        readonly float[] sortAngleScratch = new float[6];

        public WaterMeshBuilder(PlanetWorld world)
        {
            this.world = world;
            planetRadius = world.ShellRadius;
            blockHeight = world.BlockHeight;
            waterId = world.Settings.Biome.WaterBlock.BlockId;
            waterMaterial = BlockMaterialUtility.CreateWaterMaterial(world.Settings.Biome.WaterBlock.Material);
        }

        public ChunkMeshData Build()
        {
            var meshData = new ChunkMeshData();
            meshData.Materials.Add(waterMaterial);
            meshData.SubmeshTriangles.Add(new List<int>());
            const int waterMaterialIndex = 0;

            int seaLevel = world.Settings.SeaLevelLayer;
            IcosphereHexGrid grid = world.Grid;

            for (int cellIndex = 0; cellIndex < grid.CellCount; cellIndex++)
            {
                BlockColumn column = world.Columns.GetColumn(cellIndex);
                int maxLayer = math.max(column.SurfaceHeight, seaLevel);

                for (int layer = 0; layer <= maxLayer; layer++)
                {
                    if (column.GetBlock(layer) != waterId)
                    {
                        continue;
                    }

                    ref readonly SphereHexCell cell = ref grid.GetCell(cellIndex);
                    BuildWaterBlock(meshData, waterMaterialIndex, cell, layer, column, maxLayer);
                }
            }

            return meshData;
        }

        void BuildWaterBlock(
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

            if (ShouldShowTopFace(column, layer, maxLayer))
            {
                AddPolygon(meshData, materialIndex, topCornerScratch, cornerCount, math.normalize(topCenter));
            }

            if (!IsWater(column, layer - 1))
            {
                AddPolygon(meshData, materialIndex, bottomCornerScratch, cornerCount, -math.normalize(bottomCenter));
            }

            for (int side = 0; side < cornerCount; side++)
            {
                if (IsWater(world.Columns.GetColumn(sortedNeighborScratch[side]), layer))
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

        bool IsWater(BlockColumn column, int layer) => layer >= 0 && column.GetBlock(layer) == waterId;

        bool ShouldShowTopFace(BlockColumn column, int layer, int maxLayer)
        {
            if (layer >= maxLayer)
            {
                return true;
            }

            BlockId above = column.GetBlock(layer + 1);
            return above.IsAir || above != waterId;
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
