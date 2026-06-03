using System.Collections.Generic;
using UnityEngine;

namespace Voxels.Rendering
{
  public sealed class ChunkMeshPool
  {
    const int MaxPoolSize = 64;

    readonly Stack<Mesh> available = new Stack<Mesh>();

    public Mesh Rent(ChunkMeshData data)
    {
      Mesh mesh = available.Count > 0 ? available.Pop() : new Mesh { name = "WorldChunkPooled" };
      ChunkMeshFactory.UpdateMesh(mesh, data);
      return mesh;
    }

    public void Return(Mesh mesh)
    {
      if (mesh == null)
      {
        return;
      }

      if (available.Count >= MaxPoolSize)
      {
        Object.Destroy(mesh);
        return;
      }

      mesh.Clear(false);
      available.Push(mesh);
    }

    public void Clear()
    {
      while (available.Count > 0)
      {
        Object.Destroy(available.Pop());
      }
    }
  }
}
