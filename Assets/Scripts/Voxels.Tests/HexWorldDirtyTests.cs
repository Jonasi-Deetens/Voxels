using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
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


        [Test]
        public void Protected_dirty_hex_survives_cache_trim()
        {
            var settings = ScriptableObject.CreateInstance<WorldSettings>();
            var settingsObject = new SerializedObject(settings);
            settingsObject.FindProperty("columnCacheMaxCells").intValue = 2;
            settingsObject.ApplyModifiedPropertiesWithoutUndo();

            var registry = new BlockRegistry();
            var world = new HexWorld(settings, registry);
            var dirtyHex = new HexCoord(0, 0);
            var fillerA = new HexCoord(1, 0);
            var fillerB = new HexCoord(2, 0);
            var editedId = new BlockId(7);

            world.GetOrCreateColumn(dirtyHex).SetBlock(4, editedId);
            world.MarkColumnDirty(dirtyHex);
            world.GetOrCreateColumn(fillerA);
            world.GetOrCreateColumn(fillerB);

            var protectedHexes = new HashSet<HexCoord> { dirtyHex };
            world.TrimCache(protectedHexes);

            Assert.IsTrue(world.TryGetColumn(dirtyHex, out BlockColumn column));
            Assert.AreEqual(editedId, column.GetBlock(4));
        }
