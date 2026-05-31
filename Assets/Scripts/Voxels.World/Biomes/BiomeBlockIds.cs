using System;
using Voxels.Core.Blocks;

namespace Voxels.World
{
    public readonly struct BiomeBlockIds
    {
        public readonly BlockId Core;
        public readonly BlockId Mantle;
        public readonly BlockId Bedrock;
        public readonly BlockId Grass;
        public readonly BlockId Dirt;
        public readonly BlockId Sand;
        public readonly BlockId Water;

        BiomeBlockIds(
            BlockId core,
            BlockId mantle,
            BlockId bedrock,
            BlockId grass,
            BlockId dirt,
            BlockId sand,
            BlockId water)
        {
            Core = core;
            Mantle = mantle;
            Bedrock = bedrock;
            Grass = grass;
            Dirt = dirt;
            Sand = sand;
            Water = water;
        }

        public static BiomeBlockIds FromBiome(BiomeDefinition biome)
        {
            if (biome == null)
            {
                throw new InvalidOperationException("PlanetSettings.Biome is not assigned.");
            }

            return new BiomeBlockIds(
                RequireBlock(biome.CoreBlock, nameof(biome.CoreBlock)),
                RequireBlock(biome.MantleBlock, nameof(biome.MantleBlock)),
                RequireBlock(biome.BedrockBlock, nameof(biome.BedrockBlock)),
                RequireBlock(biome.SurfaceBlock, nameof(biome.SurfaceBlock)),
                RequireBlock(biome.SubsoilBlock, nameof(biome.SubsoilBlock)),
                RequireBlock(biome.UnderwaterSurfaceBlock, nameof(biome.UnderwaterSurfaceBlock)),
                RequireBlock(biome.WaterBlock, nameof(biome.WaterBlock)));
        }

        static BlockId RequireBlock(BlockDefinition definition, string fieldName)
        {
            if (definition == null)
            {
                throw new InvalidOperationException(
                    $"BiomeDefinition is missing {fieldName}. Run Voxels/Setup Default Content in the editor.");
            }

            return definition.BlockId;
        }
    }
}
