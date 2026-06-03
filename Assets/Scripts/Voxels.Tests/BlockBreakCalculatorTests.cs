using NUnit.Framework;
using UnityEngine;
using Voxels.Runtime;
using Voxels.World;

namespace Voxels.Tests
{
    public sealed class BlockBreakCalculatorTests
    {
        [Test]
        public void Iron_pickaxe_breaks_stone_faster_than_hand()
        {
            var stone = ScriptableObject.CreateInstance<BlockDefinition>();
            SetCategory(stone, BlockMaterialCategory.Stone, 0.5f);

            float hand = BlockBreakCalculator.GetBreakDuration(stone, PlayerToolMode.Hand, false);
            float pick = BlockBreakCalculator.GetBreakDuration(stone, PlayerToolMode.IronPickaxe, false);
            Assert.Less(pick, hand);
        }

        [Test]
        public void Wooden_pickaxe_slower_than_iron_on_stone()
        {
            var stone = ScriptableObject.CreateInstance<BlockDefinition>();
            SetCategory(stone, BlockMaterialCategory.Stone, 0.5f);
            float wood = BlockBreakCalculator.GetBreakDuration(stone, PlayerToolMode.WoodenPickaxe, false);
            float iron = BlockBreakCalculator.GetBreakDuration(stone, PlayerToolMode.IronPickaxe, false);
            Assert.Less(iron, wood);
        }

        [Test]
        public void Creative_break_is_instant()
        {
            var dirt = ScriptableObject.CreateInstance<BlockDefinition>();
            SetCategory(dirt, BlockMaterialCategory.Soil, 0.4f);
            Assert.AreEqual(0f, BlockBreakCalculator.GetBreakDuration(dirt, PlayerToolMode.Hand, true));
        }

        static void SetCategory(BlockDefinition block, BlockMaterialCategory category, float breakTime)
        {
            var type = typeof(BlockDefinition);
            var categoryField = type.GetField("materialCategory", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var breakField = type.GetField("breakTime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            categoryField?.SetValue(block, category);
            breakField?.SetValue(block, breakTime);
        }
    }
}
