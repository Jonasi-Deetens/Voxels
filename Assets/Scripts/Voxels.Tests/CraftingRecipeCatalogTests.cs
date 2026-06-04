using NUnit.Framework;
using UnityEngine;
using Voxels.Core.Blocks;
using Voxels.World;
using Voxels.World.Crafting;

namespace Voxels.Tests
{
    public sealed class CraftingRecipeCatalogTests
    {
        sealed class FakeInventory : PlayerInventoryAccessor
        {
            public int Dirt;
            public int Stone;

            public bool HasAtLeast(BlockId blockId, int amount) =>
                blockId.Value == 2 ? Dirt >= amount : blockId.Value == 3 && Stone >= amount;

            public bool TryConsume(BlockId blockId, int amount)
            {
                if (!HasAtLeast(blockId, amount))
                {
                    return false;
                }

                if (blockId.Value == 2)
                {
                    Dirt -= amount;
                }
                else if (blockId.Value == 3)
                {
                    Stone -= amount;
                }

                return true;
            }

            public void Add(BlockId blockId, int amount)
            {
                if (blockId.Value == 3)
                {
                    Stone += amount;
                }
            }
        }

        [Test]
        public void TryCraft_consumes_inputs()
        {
            var registry = new BlockRegistry();
            registry.Register(MakeBlock(2, "Dirt"));
            registry.Register(MakeBlock(3, "Stone"));

            var inventory = new FakeInventory { Dirt = 2 };
            bool crafted = CraftingRecipeCatalog.TryCraft(registry, inventory, out string message);
            Assert.IsTrue(crafted);
            Assert.IsTrue(message.Contains("Crafted"));
            Assert.AreEqual(0, inventory.Dirt);
            Assert.AreEqual(1, inventory.Stone);
        }

        [Test]
        public void Catalog_has_multiple_recipes()
        {
            Assert.GreaterOrEqual(CraftingRecipeCatalog.All.Count, 8);
        }

        static BlockDefinition MakeBlock(ushort id, string name)
        {
            var block = ScriptableObject.CreateInstance<BlockDefinition>();
            var type = typeof(BlockDefinition);
            type.GetField("id", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(block, id);
            type.GetField("displayName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(block, name);
            return block;
        }
    }
}
