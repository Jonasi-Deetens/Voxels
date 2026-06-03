using System.Collections.Generic;

namespace Voxels.World
{
    public static class BlockRegistryBuilder
    {
        public static BlockRegistry Build(WorldSettings settings, BlockDefinition[] blockDefinitions)
        {
            var registry = new BlockRegistry();
            registry.RegisterRange(blockDefinitions);

            if (settings?.BiomeCatalog != null)
            {
                List<BiomeDefinition> biomes = settings.BiomeCatalog.GetAllBiomesList();
                for (int i = 0; i < biomes.Count; i++)
                {
                    RegisterBiomeBlocks(registry, biomes[i]);
                }
            }
            else if (settings?.Biome != null)
            {
                RegisterBiomeBlocks(registry, settings.Biome);
            }

            return registry;
        }

        static void RegisterBiomeBlocks(BlockRegistry registry, BiomeDefinition biome)
        {
            if (biome == null)
            {
                return;
            }

            registry.Register(biome.SurfaceBlock);
            registry.Register(biome.SubsoilBlock);
            registry.Register(biome.UnderwaterSurfaceBlock);
            registry.Register(biome.WaterBlock);
            registry.Register(biome.CoreBlock);
            registry.Register(biome.MantleBlock);
            registry.Register(biome.BedrockBlock);
        }
    }
}
