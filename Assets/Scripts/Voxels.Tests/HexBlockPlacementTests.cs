using NUnit.Framework;
using UnityEngine;
using Voxels.Core.Blocks;
using Voxels.Core.Hex;
using Voxels.Runtime;
using Voxels.World;

namespace Voxels.Tests
{
    public sealed class HexBlockPlacementTests
    {
        [Test]
        public void Cannot_place_in_fluid_cell()
        {
            var settings = ScriptableObject.CreateInstance<WorldSettings>();
            var registry = new BlockRegistry();
            var water = ScriptableObject.CreateInstance<BlockDefinition>();
            SetFluid(water, true);
            registry.Register(water);

            var world = new HexWorld(settings, registry);
            var hex = new HexCoord(0, 0);
            BlockColumn column = world.GetOrCreateColumn(hex);
            column.SetSurfaceHeight(5);
            column.SetBlock(4, water.BlockId);

            var target = new HexBlockTarget(hex, 4, true);
            bool canPlace = HexBlockPlacement.CanPlace(world, settings, null, target, false);
            Assert.IsFalse(canPlace);
        }

        static void SetFluid(BlockDefinition block, bool isFluid)
        {
            var type = typeof(BlockDefinition);
            type.GetField("isFluid", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(block, isFluid);
            type.GetField("id", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(block, (ushort)6);
        }
    }
}
