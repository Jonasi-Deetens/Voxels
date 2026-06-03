using NUnit.Framework;
using UnityEngine;
using Voxels.Rendering;

namespace Voxels.Tests
{
    public sealed class ChunkMeshPoolTests
    {
        [Test]
        public void Rent_and_return_reuses_mesh()
        {
            var pool = new ChunkMeshPool();
            var data = new ChunkMeshData();
            data.Vertices.Add(Vector3.zero);
            data.Vertices.Add(Vector3.one);
            data.Vertices.Add(Vector3.up);
            data.Normals.Add(Vector3.up);
            data.Normals.Add(Vector3.up);
            data.Normals.Add(Vector3.up);
            data.SubmeshTriangles.Add(new System.Collections.Generic.List<int> { 0, 1, 2 });

            Mesh first = pool.Rent(data);
            int instanceId = first.GetInstanceID();
            pool.Return(first);
            Mesh second = pool.Rent(data);
            Assert.AreEqual(instanceId, second.GetInstanceID);
            pool.Clear();
        }
    }
}
