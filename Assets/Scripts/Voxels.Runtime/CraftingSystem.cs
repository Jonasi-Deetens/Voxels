using System.Collections.Generic;
using UnityEngine;
using Voxels.Core.Blocks;
using Voxels.World;

namespace Voxels.Runtime
{
    public sealed class CraftingSystem : MonoBehaviour
    {
        sealed class Recipe
        {
            public BlockId InputA;
            public BlockId InputB;
            public BlockId Output;
            public int OutputCount = 1;
        }

        readonly List<Recipe> recipes = new List<Recipe>();
        PlayerInventory inventory;

        public void Initialize(PlayerInventory playerInventory, BlockRegistry registry)
        {
            inventory = playerInventory;
            recipes.Clear();
            if (registry == null)
            {
                return;
            }

            TryAdd(registry, "Dirt", "Dirt", "Stone", 1);
            TryAdd(registry, "Sand", "Sand", "Gravel", 1);
            TryAdd(registry, "Gravel", "Stone", "Stone", 2);
        }

        void TryAdd(BlockRegistry registry, string a, string b, string output, int count)
        {
            if (registry.TryGetByName(a, out BlockDefinition defA) &&
                registry.TryGetByName(b, out BlockDefinition defB) &&
                registry.TryGetByName(output, out BlockDefinition defOut))
            {
                recipes.Add(new Recipe
                {
                    InputA = defA.BlockId,
                    InputB = defB.BlockId,
                    Output = defOut.BlockId,
                    OutputCount = count,
                });
            }
        }

        void Update()
        {
            if (!GameInput.WasCraftPressedThisFrame() || inventory == null)
            {
                return;
            }

            for (int i = 0; i < recipes.Count; i++)
            {
                Recipe recipe = recipes[i];
                if (inventory.TryConsume(recipe.InputA) && inventory.TryConsume(recipe.InputB))
                {
                    inventory.Add(recipe.Output, recipe.OutputCount);
                    return;
                }
            }
        }
    }
}
