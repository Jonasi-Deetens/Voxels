using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Voxels.Core.Hex;
using Voxels.World;

namespace Voxels.Runtime
{
    /// <summary>
    /// Visual world edge and soft clamp on player world-hex position near the hexagon boundary.
    /// </summary>
    public sealed class WorldBoundary : MonoBehaviour
    {
        [SerializeField] Transform worldRoot;
        [SerializeField] Material boundaryMaterial;

        WorldSettings settings;
        WorldScroller scroller;
        GameObject boundaryObject;
        readonly List<Vector3> vertices = new List<Vector3>();
        readonly List<int> triangles = new List<int>();

        public void Initialize(WorldSettings worldSettings, WorldScroller worldScroller, Transform root)
        {
            settings = worldSettings;
            scroller = worldScroller;
            worldRoot = root != null ? root : worldRoot;

            if (!settings.ShowWorldBoundary)
            {
                return;
            }

            BuildBoundaryMesh();
        }

        void LateUpdate()
        {
            if (scroller == null || settings == null)
            {
                return;
            }

            ClampPlayerToWorld();
        }

        void ClampPlayerToWorld()
        {
            HexCoord playerHex = scroller.PlayerWorldHex;
            if (HexagonMask.IsInsideWorld(playerHex, settings.WorldHexRadius))
            {
                return;
            }

            HexCoord clamped = HexagonMask.ClampToWorld(playerHex, settings.WorldHexRadius);
            if (clamped == playerHex)
            {
                return;
            }

            float3 targetOffset = FlatHexGrid.AxialToWorld(clamped, settings.BlockSize);
            scroller.SetWorldHex(clamped, targetOffset);
        }

        void BuildBoundaryMesh()
        {
            if (worldRoot == null || settings == null)
            {
                return;
            }

            if (boundaryObject != null)
            {
                Destroy(boundaryObject);
            }

            vertices.Clear();
            triangles.Clear();

            int radius = settings.WorldHexRadius;
            float blockSize = settings.BlockSize;
            float wallHeight = settings.BoundaryWallHeight;
            var ring = new List<HexCoord>();

            for (int q = -radius; q <= radius; q++)
            {
                for (int r = -radius; r <= radius; r++)
                {
                    var hex = new HexCoord(q, r);
                    if (HexagonMask.DistanceFromCenter(hex) == radius)
                    {
                        ring.Add(hex);
                    }
                }
            }

            ring.Sort((a, b) =>
            {
                float angA = math.atan2(
                    FlatHexGrid.AxialToWorld(a, 1f).z,
                    FlatHexGrid.AxialToWorld(a, 1f).x);
                float angB = math.atan2(
                    FlatHexGrid.AxialToWorld(b, 1f).z,
                    FlatHexGrid.AxialToWorld(b, 1f).x);
                return angA.CompareTo(angB);
            });

            for (int i = 0; i < ring.Count; i++)
            {
                int next = (i + 1) % ring.Count;
                float3 c0 = FlatHexGrid.AxialToWorld(ring[i], blockSize);
                float3 c1 = FlatHexGrid.AxialToWorld(ring[next], blockSize);
                AddWallQuad(c0, c1, wallHeight);
            }

            if (vertices.Count == 0)
            {
                return;
            }

            boundaryObject = new GameObject("WorldBoundary");
            boundaryObject.transform.SetParent(worldRoot, false);

            var mesh = new Mesh { name = "WorldBoundary" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var filter = boundaryObject.AddComponent<MeshFilter>();
            var renderer = boundaryObject.AddComponent<MeshRenderer>();
            filter.sharedMesh = mesh;

            if (boundaryMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                boundaryMaterial = new Material(shader);
                boundaryMaterial.SetColor("_BaseColor", new Color(0.12f, 0.1f, 0.14f, 1f));
            }

            renderer.sharedMaterial = boundaryMaterial;
        }

        void AddWallQuad(float3 a, float3 b, float height)
        {
            int baseIndex = vertices.Count;
            vertices.Add(new Vector3(a.x, 0f, a.z));
            vertices.Add(new Vector3(b.x, 0f, b.z));
            vertices.Add(new Vector3(b.x, height, b.z));
            vertices.Add(new Vector3(a.x, height, a.z));

            triangles.Add(baseIndex);
            triangles.Add(baseIndex + 2);
            triangles.Add(baseIndex + 1);
            triangles.Add(baseIndex);
            triangles.Add(baseIndex + 3);
            triangles.Add(baseIndex + 2);
        }
    }
}
