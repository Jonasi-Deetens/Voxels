using Unity.Mathematics;
using UnityEngine;
using Voxels.Core.Hex;

namespace Voxels.Rendering
{
    /// <summary>
    /// Shared flat hexagon mesh for sky clouds (fan from center, XZ plane, normal +Y).
    /// </summary>
    public static class HexCloudMeshUtility
    {
        static Mesh sharedMesh;

        public static Mesh GetSharedMesh(float radius = 1f)
        {
            if (sharedMesh != null)
            {
                return sharedMesh;
            }

            var corners = new float3[6];
            FlatHexGrid.GetCornerOffsets(radius, corners);

            var vertices = new Vector3[7];
            vertices[0] = Vector3.zero;
            for (int i = 0; i < 6; i++)
            {
                vertices[i + 1] = corners[i];
            }

            var colors = new Color[7];
            colors[0] = new Color(1f, 1f, 1f, 0.55f);
            for (int i = 1; i < 7; i++)
            {
                colors[i] = new Color(1f, 1f, 1f, 0.12f);
            }

            var triangles = new int[18];
            for (int i = 0; i < 6; i++)
            {
                int tri = i * 3;
                triangles[tri] = 0;
                triangles[tri + 1] = 1 + i;
                triangles[tri + 2] = 1 + ((i + 1) % 6);
            }

            sharedMesh = new Mesh { name = "HexCloud" };
            sharedMesh.SetVertices(vertices);
            sharedMesh.SetColors(colors);
            sharedMesh.SetTriangles(triangles, 0);
            sharedMesh.RecalculateNormals();
            sharedMesh.RecalculateBounds();
            return sharedMesh;
        }
    }
}
