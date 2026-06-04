using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Voxels.World.Crafting;

namespace Voxels.World.Modding
{
    [Serializable]
    public sealed class VoxelsModConfigData
    {
        public string[] hotbarBlockNames;
        public float structureDensity = -1f;
        public ModStructurePoiEntry[] structurePois;
        public ModFoodEntry[] foodOverrides;
        public ModRecipeEntry[] craftingRecipes;
    }

    [Serializable]
    public sealed class ModStructurePoiEntry
    {
        public string biomeNameContains;
        public string poiKind;
        public float weight = 1f;
    }

    [Serializable]
    public sealed class ModFoodEntry
    {
        public string blockName;
        public float hungerRestore;
        public float healthRestoreOnEat;
    }

    [Serializable]
    public sealed class ModRecipeEntry
    {
        public string outputBlockName;
        public int outputCount = 1;
        public ModRecipeIngredient[] inputs;
    }

    [Serializable]
    public sealed class ModRecipeIngredient
    {
        public string blockName;
        public int count = 1;
    }

    public static class VoxelsModConfig
    {
        const string ConfigFileName = "voxels_config.json";
        static VoxelsModConfigData cached;
        static Dictionary<string, ModFoodEntry> foodByBlock;

        public static VoxelsModConfigData Load()
        {
            if (cached != null)
            {
                return cached;
            }

            string path = Path.Combine(Application.streamingAssetsPath, ConfigFileName);
            if (!File.Exists(path))
            {
                cached = new VoxelsModConfigData();
            }
            else
            {
                cached = JsonUtility.FromJson<VoxelsModConfigData>(File.ReadAllText(path)) ?? new VoxelsModConfigData();
            }

            BuildFoodLookup();
            return cached;
        }

        public static void Reload()
        {
            cached = null;
            foodByBlock = null;
            Load();
        }

        public static float ResolveStructureDensity(WorldSettings settings)
        {
            float mod = Load().structureDensity;
            return mod >= 0f ? mod : settings.StructureDensity;
        }

        public static bool TryGetFoodOverride(string blockName, out float hunger, out float health)
        {
            hunger = 0f;
            health = 0f;
            if (string.IsNullOrEmpty(blockName) || foodByBlock == null)
            {
                return false;
            }

            if (!foodByBlock.TryGetValue(blockName, out ModFoodEntry entry))
            {
                return false;
            }

            hunger = entry.hungerRestore;
            health = entry.healthRestoreOnEat;
            return hunger > 0f || health > 0f;
        }

        public static IReadOnlyList<CraftingRecipe> BuildModRecipes()
        {
            ModRecipeEntry[] entries = Load().craftingRecipes;
            if (entries == null || entries.Length == 0)
            {
                return Array.Empty<CraftingRecipe>();
            }

            var recipes = new List<CraftingRecipe>(entries.Length);
            for (int i = 0; i < entries.Length; i++)
            {
                ModRecipeEntry entry = entries[i];
                if (entry == null || entry.inputs == null || entry.inputs.Length == 0 || string.IsNullOrEmpty(entry.outputBlockName))
                {
                    continue;
                }

                var ingredients = new CraftingIngredient[entry.inputs.Length];
                for (int j = 0; j < entry.inputs.Length; j++)
                {
                    ingredients[j] = new CraftingIngredient
                    {
                        blockName = entry.inputs[j].blockName,
                        count = Mathf.Max(1, entry.inputs[j].count),
                    };
                }

                recipes.Add(new CraftingRecipe
                {
                    outputBlockName = entry.outputBlockName,
                    outputCount = Mathf.Max(1, entry.outputCount),
                    inputs = ingredients,
                });
            }

            return recipes;
        }

        public static float ResolvePoiWeight(string biomeName, string poiKind, float baseWeight)
        {
            ModStructurePoiEntry[] pois = Load().structurePois;
            if (pois == null || string.IsNullOrEmpty(biomeName) || string.IsNullOrEmpty(poiKind))
            {
                return baseWeight;
            }

            string biomeLower = biomeName.ToLowerInvariant();
            string poiLower = poiKind.ToLowerInvariant();
            float mult = 1f;
            for (int i = 0; i < pois.Length; i++)
            {
                ModStructurePoiEntry entry = pois[i];
                if (entry == null || string.IsNullOrEmpty(entry.biomeNameContains) || string.IsNullOrEmpty(entry.poiKind))
                {
                    continue;
                }

                if (biomeLower.Contains(entry.biomeNameContains.ToLowerInvariant()) &&
                    poiLower.Contains(entry.poiKind.ToLowerInvariant()))
                {
                    mult *= Mathf.Max(0.1f, entry.weight);
                }
            }

            return baseWeight * mult;
        }

        static void BuildFoodLookup()
        {
            foodByBlock = new Dictionary<string, ModFoodEntry>(StringComparer.OrdinalIgnoreCase);
            ModFoodEntry[] entries = cached?.foodOverrides;
            if (entries == null)
            {
                return;
            }

            for (int i = 0; i < entries.Length; i++)
            {
                ModFoodEntry entry = entries[i];
                if (entry == null || string.IsNullOrEmpty(entry.blockName))
                {
                    continue;
                }

                foodByBlock[entry.blockName] = entry;
            }
        }
    }
}
