using System;
using System.Collections.Generic;
using Unity.Mathematics;
using Voxels.Core.Hex;

namespace Voxels.Core.Sphere
{
    public sealed class IcosphereHexGrid
    {
        const float GoldenRatio = 1.618033988749895f;

        readonly SphereHexCell[] cells;
        readonly float averageNeighborDistance;
        readonly float averageNeighborArc;
        readonly float apothem;

        public int CellCount => cells.Length;
        public float AverageNeighborDistance => averageNeighborDistance;

        /// <summary>
        /// Average geodesic arc between adjacent cell centers on the unit sphere.
        /// At shell radius R, neighbor spacing equals R * AverageNeighborArc.
        /// </summary>
        public float AverageNeighborArc => averageNeighborArc;
        public float Apothem => apothem;
        public float BlockHeight => HexMetrics.BlockHeight(apothem);

        public IcosphereHexGrid(int subdivisionLevel)
        {
            if (subdivisionLevel < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(subdivisionLevel));
            }

            BuildMesh(subdivisionLevel, out List<float3> vertices, out List<int> triangles);
            var neighbors = BuildNeighborLists(vertices, triangles);

            cells = new SphereHexCell[vertices.Count];
            float neighborDistanceSum = 0f;
            float neighborArcSum = 0f;
            int neighborDistanceCount = 0;

            for (int i = 0; i < vertices.Count; i++)
            {
                HashSet<int> neighborSet = neighbors[i];
                int[] cellNeighbors = new int[neighborSet.Count];
                neighborSet.CopyTo(cellNeighbors);
                Array.Sort(cellNeighbors);
                bool isPentagon = cellNeighbors.Length == 5;
                cells[i] = new SphereHexCell(i, vertices[i], cellNeighbors, isPentagon);

                for (int n = 0; n < cellNeighbors.Length; n++)
                {
                    if (cellNeighbors[n] <= i)
                    {
                        continue;
                    }

                    float chord = math.distance(vertices[i], vertices[cellNeighbors[n]]);
                    neighborDistanceSum += chord;
                    neighborArcSum += 2f * math.asin(math.min(chord * 0.5f, 1f));
                    neighborDistanceCount++;
                }
            }

            averageNeighborDistance = neighborDistanceCount > 0
                ? neighborDistanceSum / neighborDistanceCount
                : 1f;
            averageNeighborArc = neighborDistanceCount > 0
                ? neighborArcSum / neighborDistanceCount
                : 1f;
            apothem = HexMetrics.ApothemFromNeighborDistance(averageNeighborDistance);
        }

        public ref readonly SphereHexCell GetCell(int index) => ref cells[index];

        public ReadOnlySpan<SphereHexCell> Cells => cells;

        static void BuildMesh(int subdivisionLevel, out List<float3> vertices, out List<int> triangles)
        {
            vertices = new List<float3>(12);
            float t = GoldenRatio;
            vertices.Add(math.normalize(new float3(-1f, t, 0f)));
            vertices.Add(math.normalize(new float3(1f, t, 0f)));
            vertices.Add(math.normalize(new float3(-1f, -t, 0f)));
            vertices.Add(math.normalize(new float3(1f, -t, 0f)));
            vertices.Add(math.normalize(new float3(0f, -1f, t)));
            vertices.Add(math.normalize(new float3(0f, 1f, t)));
            vertices.Add(math.normalize(new float3(0f, -1f, -t)));
            vertices.Add(math.normalize(new float3(0f, 1f, -t)));
            vertices.Add(math.normalize(new float3(t, 0f, -1f)));
            vertices.Add(math.normalize(new float3(t, 0f, 1f)));
            vertices.Add(math.normalize(new float3(-t, 0f, -1f)));
            vertices.Add(math.normalize(new float3(-t, 0f, 1f)));

            int[] faces =
            {
                0, 11, 5,
                0, 5, 1,
                0, 1, 7,
                0, 7, 10,
                0, 10, 11,
                1, 5, 9,
                5, 11, 4,
                11, 10, 2,
                10, 7, 6,
                7, 1, 8,
                3, 9, 4,
                3, 4, 2,
                3, 2, 6,
                3, 6, 8,
                3, 8, 9,
                4, 9, 5,
                2, 4, 11,
                6, 2, 10,
                8, 6, 7,
                9, 8, 1
            };

            var currentFaces = new List<int>(faces);
            var midpointCache = new Dictionary<long, int>();

            for (int level = 0; level < subdivisionLevel; level++)
            {
                var nextFaces = new List<int>(currentFaces.Count * 4);

                for (int i = 0; i < currentFaces.Count; i += 3)
                {
                    int a = currentFaces[i];
                    int b = currentFaces[i + 1];
                    int c = currentFaces[i + 2];

                    int ab = GetMidpoint(a, b, vertices, midpointCache);
                    int bc = GetMidpoint(b, c, vertices, midpointCache);
                    int ca = GetMidpoint(c, a, vertices, midpointCache);

                    nextFaces.Add(a);
                    nextFaces.Add(ab);
                    nextFaces.Add(ca);

                    nextFaces.Add(b);
                    nextFaces.Add(bc);
                    nextFaces.Add(ab);

                    nextFaces.Add(c);
                    nextFaces.Add(ca);
                    nextFaces.Add(bc);

                    nextFaces.Add(ab);
                    nextFaces.Add(bc);
                    nextFaces.Add(ca);
                }

                currentFaces = nextFaces;
            }

            triangles = currentFaces;
        }

        static List<HashSet<int>> BuildNeighborLists(IReadOnlyList<float3> vertices, IReadOnlyList<int> triangleIndices)
        {
            var neighbors = new List<HashSet<int>>(vertices.Count);
            for (int i = 0; i < vertices.Count; i++)
            {
                neighbors.Add(new HashSet<int>());
            }

            for (int i = 0; i < triangleIndices.Count; i += 3)
            {
                int a = triangleIndices[i];
                int b = triangleIndices[i + 1];
                int c = triangleIndices[i + 2];

                neighbors[a].Add(b);
                neighbors[a].Add(c);
                neighbors[b].Add(a);
                neighbors[b].Add(c);
                neighbors[c].Add(a);
                neighbors[c].Add(b);
            }

            return neighbors;
        }

        static int GetMidpoint(int indexA, int indexB, List<float3> vertices, Dictionary<long, int> cache)
        {
            long key = indexA < indexB
                ? ((long)indexA << 32) | (uint)indexB
                : ((long)indexB << 32) | (uint)indexA;

            if (cache.TryGetValue(key, out int index))
            {
                return index;
            }

            float3 midpoint = math.normalize((vertices[indexA] + vertices[indexB]) * 0.5f);
            index = vertices.Count;
            vertices.Add(midpoint);
            cache[key] = index;
            return index;
        }

        public float3 GetTangentBasisRight(in SphereHexCell cell)
        {
            float3 up = cell.Normal;
            float3 arbitrary = math.abs(up.y) < 0.99f ? new float3(0f, 1f, 0f) : new float3(1f, 0f, 0f);
            return math.normalize(math.cross(arbitrary, up));
        }

        public float3 GetTangentBasisForward(in SphereHexCell cell, float3 right)
        {
            return math.normalize(math.cross(cell.Normal, right));
        }

        public float3 GetCellSurfacePoint(in SphereHexCell cell, float planetRadius, int heightLayer)
        {
            float radius = planetRadius + heightLayer * BlockHeight;
            return cell.Normal * radius;
        }
    }
}
