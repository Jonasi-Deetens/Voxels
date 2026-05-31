using System.Collections.Generic;
using UnityEngine;

namespace Voxels.Rendering
{
    public static class ChunkMeshFactory
    {
        public static Mesh CreateMesh(ChunkMeshData data)
        {
            var mesh = new Mesh { name = "PlanetChunk" };
            mesh.indexFormat = data.Vertices.Count > 65535
                ? UnityEngine.Rendering.IndexFormat.UInt32
                : UnityEngine.Rendering.IndexFormat.UInt16;

            mesh.SetVertices(data.Vertices);
            mesh.SetNormals(data.Normals);

            mesh.subMeshCount = data.SubmeshTriangles.Count;
            for (int i = 0; i < data.SubmeshTriangles.Count; i++)
            {
                mesh.SetTriangles(data.SubmeshTriangles[i], i);
            }

            mesh.RecalculateBounds();
            return mesh;
        }

        public static Material[] GetMaterials(ChunkMeshData data)
        {
            return data.Materials.ToArray();
        }
    }
}
