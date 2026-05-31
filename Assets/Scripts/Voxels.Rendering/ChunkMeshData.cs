using System.Collections.Generic;
using UnityEngine;

namespace Voxels.Rendering
{
    public sealed class ChunkMeshData
    {
        public List<Vector3> Vertices { get; } = new();
        public List<Vector3> Normals { get; } = new();
        public List<int> Triangles { get; } = new();
        public List<Material> Materials { get; } = new();
        public List<List<int>> SubmeshTriangles { get; } = new();

        public bool IsEmpty => Vertices.Count == 0;
    }
}
