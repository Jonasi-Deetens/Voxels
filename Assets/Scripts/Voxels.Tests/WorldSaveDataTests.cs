using NUnit.Framework;
using UnityEngine;
using Voxels.World;

namespace Voxels.Tests
{
    public sealed class WorldSaveDataTests
    {
        [Test]
        public void Save_round_trips_column_blocks()
        {
            var data = new WorldSaveData
            {
                version = WorldSaveData.CurrentVersion,
                seed = 42,
            };

            data.columns.Add(new SavedColumn
            {
                q = 2,
                r = -1,
                surfaceHeight = 10,
            });
            data.columns[0].blocks.Add(new SavedBlock { layer = 5, blockId = 3 });

            string json = JsonUtility.ToJson(data);
            WorldSaveData loaded = JsonUtility.FromJson<WorldSaveData>(json);

            Assert.AreEqual(42, loaded.seed);
            Assert.AreEqual(1, loaded.columns.Count);
            Assert.AreEqual(3, loaded.columns[0].blocks[0].blockId);
        }
    }
}
