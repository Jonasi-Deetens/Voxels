using System.Collections.Generic;
using UnityEngine;

namespace Voxels.Rendering
{
    public static class ChunkMeshFactory
    {
        public static Mesh CreateMesh(ChunkMeshData data)
        {
            var mesh = new Mesh { name = "WorldChunk" };
            ApplyData(mesh, data);
            return mesh;
        }

        public static void UpdateMesh(Mesh mesh, ChunkMeshData data)
        {
            if (mesh == null)
            {
                return;
            }

            ApplyData(mesh, data);
        }

        static void ApplyData(Mesh mesh, ChunkMeshData data)
        {
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
            mesh.UploadMeshData(false);
        }

        public static Material[] GetMaterials(ChunkMeshData data)
        {
            return data.Materials.ToArray();
        }
    }
}
