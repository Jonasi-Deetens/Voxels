using System.Collections.Generic;
using Voxels.Core.Blocks;

namespace Voxels.World.Crafting
{
    public static class CraftingRecipeCatalog
    {
        static readonly CraftingRecipe[] Recipes =
        {
            Make("Dirt", 2, "Stone", 1),
            Make("Sand", 2, "Gravel", 1),
            MakeDual("Gravel", 1, "Stone", 1, "Stone", 2),
            Make("Snow", 2, "Ice", 1),
        };

        public static IReadOnlyList<CraftingRecipe> All => Recipes;

        static CraftingRecipe Make(string input, int inputCount, string output, int outputCount) =>
            new CraftingRecipe
            {
                inputs = new[] { new CraftingIngredient { blockName = input, count = inputCount } },
                outputBlockName = output,
                outputCount = outputCount,
            };

        static CraftingRecipe MakeDual(string a, int aCount, string b, int bCount, string output, int outputCount) =>
            new CraftingRecipe
            {
                inputs = new[]
                {
                    new CraftingIngredient { blockName = a, count = aCount },
                    new CraftingIngredient { blockName = b, count = bCount },
                },
                outputBlockName = output,
                outputCount = outputCount,
            };

        public static bool TryCraft(BlockRegistry registry, PlayerInventoryAccessor inventory, out string resultMessage)
        {
            for (int i = 0; i < Recipes.Length; i++)
            {
                if (TryCraftRecipe(registry, inventory, Recipes[i], out BlockId output, out int count))
                {
                    inventory.Add(output, count);
                    registry.TryGetDefinition(output, out BlockDefinition def);
                    string name = def != null ? def.DisplayName : output.ToString();
                    resultMessage = $"Crafted {name} x{count}";
                    return true;
                }
            }

            resultMessage = "Can't craft";
            return false;
        }

        static bool TryCraftRecipe(
            BlockRegistry registry,
            PlayerInventoryAccessor inventory,
            CraftingRecipe recipe,
            out BlockId output,
            out int outputCount)
        {
            output = BlockId.Air;
            outputCount = recipe.outputCount;
            if (!registry.TryGetByName(recipe.outputBlockName, out BlockDefinition outputDef))
            {
                return false;
            }

            for (int i = 0; i < recipe.inputs.Length; i++)
            {
                CraftingIngredient ingredient = recipe.inputs[i];
                if (!registry.TryGetByName(ingredient.blockName, out BlockDefinition inputDef))
                {
                    return false;
                }

                if (!inventory.HasAtLeast(inputDef.BlockId, ingredient.count))
                {
                    return false;
                }
            }

            for (int i = 0; i < recipe.inputs.Length; i++)
            {
                CraftingIngredient ingredient = recipe.inputs[i];
                registry.TryGetByName(ingredient.blockName, out BlockDefinition inputDef);
                inventory.TryConsume(inputDef.BlockId, ingredient.count);
            }

            output = outputDef.BlockId;
            return true;
        }
    }

    public interface PlayerInventoryAccessor
    {
        bool HasAtLeast(BlockId blockId, int amount);
        bool TryConsume(BlockId blockId, int amount);
        void Add(BlockId blockId, int amount);
    }
}
