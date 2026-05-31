using Voxels.World;

namespace Voxels.World
{
    public static class BlockRegistryBuilder
    {
        public static BlockRegistry Build(PlanetSettings settings, BlockDefinition[] blockDefinitions)
        {
            var registry = new BlockRegistry();
            registry.RegisterRange(blockDefinitions);

            if (settings?.Biome != null)
            {
                BiomeDefinition biome = settings.Biome;
                registry.Register(biome.SurfaceBlock);
                registry.Register(biome.SubsoilBlock);
                registry.Register(biome.UnderwaterSurfaceBlock);
                registry.Register(biome.WaterBlock);
                registry.Register(biome.CoreBlock);
                registry.Register(biome.MantleBlock);
                registry.Register(biome.BedrockBlock);
            }

            return registry;
        }
    }
}
