using NUnit.Framework;
using Unity.Mathematics;
using Voxels.Core.Hex;

namespace Voxels.Core.Tests
{
    public sealed class HexGridTests
    {
        [Test]
        public void NeighborOffsets_ReturnSixUniqueNeighbors()
        {
            var center = new HexCoord(3, 5);
            var seen = new System.Collections.Generic.HashSet<HexCoord>();
            for (int i = 0; i < HexCoord.NeighborOffsets.Length; i++)
            {
                HexCoord neighbor = center.Add(HexCoord.NeighborOffsets[i]);
                Assert.IsTrue(seen.Add(neighbor));
            }

            Assert.AreEqual(6, seen.Count);
        }

        [Test]
        public void HexagonMask_RadiusZero_OnlyCenter()
        {
            Assert.IsTrue(HexagonMask.IsInsideWorld(HexCoord.Zero, 0));
            Assert.IsFalse(HexagonMask.IsInsideWorld(new HexCoord(1, 0), 0));
        }

        [Test]
        public void WorldToAxial_RoundTrip_IsStable()
        {
            const float blockSize = 1f;
            var hex = new HexCoord(4, -2);
            float3 world = FlatHexGrid.AxialToWorld(hex, blockSize);
            HexCoord roundTrip = FlatHexGrid.WorldToAxial(world, blockSize);
            Assert.AreEqual(hex, roundTrip);
        }

        [Test]
        public void ClampToWorld_PullsOutsideHexBackInside()
        {
            var outside = new HexCoord(100, 100);
            HexCoord clamped = HexagonMask.ClampToWorld(outside, 5);
            Assert.IsTrue(HexagonMask.IsInsideWorld(clamped, 5));
        }
    }
}
