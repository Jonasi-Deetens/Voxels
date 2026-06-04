using NUnit.Framework;
using UnityEngine;
using Voxels.Core.Blocks;
using Voxels.Core.Hex;
using Voxels.World;
using Voxels.World.Generation;

namespace Voxels.Tests
{
    public sealed class BlockLightSamplerTests
    {
        [Test]
        public void Emissive_block_raises_light_above_sun()
        {
            var settings = ScriptableObject.CreateInstance<WorldSettings>();
            SetField(settings, "maxDepthBelowSurface", 40);
            SetField(settings, "maxHeightAboveSurface", 20);
            var registry = new BlockRegistry();
            var crystal = ScriptableObject.CreateInstance<BlockDefinition>();
            SetField(crystal, "id", (ushort)13);
            SetField(crystal, "displayName", "Crystal");
            SetField(crystal, "isEmissive", true);
            SetField(crystal, "lightEmission", 12);
            registry.Register(crystal);

            var world = new HexWorld(settings, registry);
            HexCoord hex = HexCoord.Zero;
            BlockColumn column = world.GetOrCreateColumn(hex);
            column.SetSurfaceHeight(10);
            column.SetBlock(11, crystal.BlockId);

            int light = BlockLightSampler.SamplePlayerLightLevel(world, hex, 2);
            Assert.GreaterOrEqual(light, 12);
        }

        static void SetField(object target, string name, object value)
        {
            var field = target.GetType().GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field?.SetValue(target, value);
        }

        static void SetField(BlockDefinition block, string name, object value)
        {
            var field = typeof(BlockDefinition).GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field?.SetValue(block, value);
        }
    }
}
