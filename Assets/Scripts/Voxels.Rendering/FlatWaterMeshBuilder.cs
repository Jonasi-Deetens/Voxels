using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Voxels.Core.Blocks;
using Voxels.Core.Hex;
using Voxels.World;

namespace Voxels.Rendering
{
    public sealed class FlatWaterMeshBuilder
    {
        readonly HexWorld world;
        readonly float blockSize;
        readonly float3[] cornerScratch = new float3[6];

        public FlatWaterMeshBuilder(HexWorld world)
        {
            this.world = world;
            blockSize = world.BlockSize;
        }

        public ChunkMeshData BuildChunk(
            in HexCoord playerHex,
            IReadOnlyList<HexCoord> worldHexes)
        {
            var meshData = new ChunkMeshData();
            int seaLevel = world.Settings.SeaLevelLayer;
            Material waterMaterial = null;

            for (int i = 0; i < worldHexes.Count; i++)
            {
                HexCoord worldHex = worldHexes[i];
                if (!world.TryGetColumn(worldHex, out BlockColumn column))
                {
                    continue;
                }

                int surfaceHeight = column.SurfaceHeight;
                if (surfaceHeight >= seaLevel)
                {
                    continue;
                }

                for (int layer = surfaceHeight + 1; layer <= seaLevel; layer++)
                {
                    BlockId blockId = column.GetBlock(layer);
                    if (blockId.IsAir || !world.BlockRegistry.TryGetDefinition(blockId, out BlockDefinition definition))
                    {
                        continue;
                    }

                    if (definition.IsOpaque)
                    {
                        continue;
                    }

                    waterMaterial ??= definition.Material;
                    HexCoord localHex = worldHex.Subtract(playerHex);
                    float3 center = FlatHexGrid.AxialToWorld(localHex, blockSize);
                    float y = (layer + 1) * blockSize - 0.02f;
                    center.y = y;
                    FlatHexGeometry.GetBlockCorners(center, blockSize, cornerScratch);
                    AddWaterSurface(meshData, cornerScratch);
                }
            }

            if (waterMaterial != null)
            {
                Material renderMaterial = BlockMaterialUtility.CreateRenderMaterial(
                    waterMaterial,
                    false,
                    new Color(0.12f, 0.38f, 0.78f, 0.6f));
                meshData.Materials.Add(renderMaterial);
                meshData.SubmeshTriangles.Add(new List<int>());
            }

            return meshData;
        }

        static void AddWaterSurface(ChunkMeshData meshData, float3[] corners)
        {
            if (meshData.Materials.Count == 0)
            {
                return;
            }

            int materialIndex = 0;
            int start = meshData.Vertices.Count;
            float3 normal = Vector3.up;
            for (int i = 0; i < 6; i++)
            {
                meshData.Vertices.Add(corners[i]);
                meshData.Normals.Add(normal);
            }

            List<int> submesh = meshData.SubmeshTriangles[materialIndex];
            for (int i = 1; i < 5; i++)
            {
                submesh.Add(start);
                submesh.Add(start + i);
                submesh.Add(start + i + 1);
            }
        }
    }
}
