using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Voxels.Core.Blocks;
using Voxels.Core.Hex;
using Voxels.Runtime;
using Voxels.World;

namespace Voxels.PlayModeTests
{
    public sealed class SaveLoadSmokePlayModeTest
    {
        [UnityTest]
        public IEnumerator Break_save_and_load_roundtrip()
        {
            var scene = SceneManager.GetSceneByName("SampleScene");
            if (!scene.isLoaded)
            {
                yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
            }

            WorldBootstrap bootstrap = null;
            float timeout = 30f;
            while (timeout > 0f)
            {
                bootstrap = Object.FindAnyObjectByType<WorldBootstrap>();
                if (bootstrap != null && bootstrap.BuildComplete)
                {
                    break;
                }

                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.IsNotNull(bootstrap, "WorldBootstrap did not finish building.");
            HexWorld world = bootstrap.HexWorld;
            Assert.IsNotNull(world);

            HexCoord editHex = new HexCoord(0, 0);
            BlockColumn column = world.GetOrCreateColumn(editHex);
            column.SetBlock(5, new BlockId(3));
            world.MarkColumnDirty(editHex);

            WorldSaveSystem saveSystem = Object.FindAnyObjectByType<WorldSaveSystem>();
            Assert.IsNotNull(saveSystem);
            saveSystem.Save();

            column.SetBlock(5, BlockId.Air);
            Assert.IsTrue(column.GetBlock(5).IsAir);

            saveSystem.Load();

            Assert.IsTrue(world.TryGetColumn(editHex, out BlockColumn loaded));
            Assert.AreEqual(new BlockId(3), loaded.GetBlock(5));
        }
    }
}
