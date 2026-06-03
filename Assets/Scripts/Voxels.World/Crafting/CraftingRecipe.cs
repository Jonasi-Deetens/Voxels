using System;
using Voxels.Core.Blocks;

namespace Voxels.World.Crafting
{
    [Serializable]
    public sealed class CraftingIngredient
    {
        public string blockName;
        public int count = 1;
    }

    [Serializable]
    public sealed class CraftingRecipe
    {
        public CraftingIngredient[] inputs = Array.Empty<CraftingIngredient>();
        public string outputBlockName;
        public int outputCount = 1;
    }
}
