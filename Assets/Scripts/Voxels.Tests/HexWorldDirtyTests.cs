using NUnit.Framework;
using Voxels.Core.Blocks;
using Voxels.Core.Hex;
using Voxels.World;

namespace Voxels.Tests
{
    public sealed class HexWorldDirtyTests
    {
        [Test]
        public void MarkColumnDirty_tracks_hex()
        {
            var settings = ScriptableObject.CreateInstance<WorldSettings>();
            var registry = new BlockRegistry();
            var world = new HexWorld(settings, registry);
            var hex = new HexCoord(1, 0);

            world.GetOrCreateColumn(hex).SetBlock(3, new BlockId(2));
            world.MarkColumnDirty(hex);

            int count = 0;
            foreach (HexCoord dirty in world.GetDirtyHexes())
            {
                if (dirty == hex)
                {
                    count++;
                }
            }

            Assert.AreEqual(1, count);
        }
    }
}
