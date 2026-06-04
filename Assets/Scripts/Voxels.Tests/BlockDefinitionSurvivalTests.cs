using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Voxels.World;

namespace Voxels.Tests
{
    public sealed class BlockDefinitionSurvivalTests
    {
        [Test]
        public void IsEdible_when_hunger_restore_positive()
        {
            BlockDefinition block = ScriptableObject.CreateInstance<BlockDefinition>();
            SetField(block, "hungerRestore", 20f);
            Assert.IsTrue(block.IsEdible);
        }

        [Test]
        public void HasHeldBonus_when_defense_or_mining_set()
        {
            BlockDefinition block = ScriptableObject.CreateInstance<BlockDefinition>();
            SetField(block, "heldDefensePercent", 0.1f);
            Assert.IsTrue(block.HasHeldBonus);
        }

        static void SetField(BlockDefinition block, string fieldName, float value)
        {
            FieldInfo field = typeof(BlockDefinition).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, $"Missing field {fieldName}");
            field.SetValue(block, value);
        }
    }
}
