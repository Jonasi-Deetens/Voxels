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

        [Test]
        public void Version3_includes_weather_and_health()
        {
            var data = new WorldSaveData
            {
                version = 3,
                weatherKind = 2,
                weatherTargetKind = 4,
                weatherTransition = 0.5f,
                playerHealth = 14f,
            };

            WorldSaveData loaded = JsonUtility.FromJson<WorldSaveData>(JsonUtility.ToJson(data));
            Assert.AreEqual(3, loaded.version);
            Assert.AreEqual(2, loaded.weatherKind);
            Assert.AreEqual(4, loaded.weatherTargetKind);
            Assert.AreEqual(0.5f, loaded.weatherTransition, 0.001f);
            Assert.AreEqual(14f, loaded.playerHealth, 0.001f);
        }
    }
}
