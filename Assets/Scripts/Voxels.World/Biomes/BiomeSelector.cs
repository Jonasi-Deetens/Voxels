using Voxels.World.Climate;

namespace Voxels.World
{
    public sealed class BiomeSelector
    {
        readonly BiomeCatalog catalog;

        public BiomeSelector(BiomeCatalog catalog)
        {
            this.catalog = catalog;
        }

        public BiomeDefinition Select(in ClimateSample climate)
        {
            if (climate.IsOcean)
            {
                return catalog.OceanBiome ?? catalog.FallbackBiome;
            }

            if (catalog.AlpineBiome != null &&
                climate.ElevationAboveSea >= catalog.AlpineBiome.AlpineElevationThreshold)
            {
                return catalog.AlpineBiome;
            }

            BiomeDefinition best = null;
            int bestPriority = int.MinValue;

            if (catalog.Rules != null)
            {
                for (int i = 0; i < catalog.Rules.Count; i++)
                {
                    BiomeRule rule = catalog.Rules[i];
                    if (rule.Biome == null || !Matches(rule, climate))
                    {
                        continue;
                    }

                    if (rule.Priority > bestPriority)
                    {
                        bestPriority = rule.Priority;
                        best = rule.Biome;
                    }
                }
            }

            return best ?? catalog.FallbackBiome;
        }

        static bool Matches(BiomeRule rule, in ClimateSample climate)
        {
            if (rule.RequiresLand && climate.IsOcean)
            {
                return false;
            }

            if (climate.Temperature < rule.MinTemperature || climate.Temperature > rule.MaxTemperature)
            {
                return false;
            }

            if (climate.Humidity < rule.MinHumidity || climate.Humidity > rule.MaxHumidity)
            {
                return false;
            }

            if (climate.LeyLine < rule.MinLeyLine || climate.LeyLine > rule.MaxLeyLine)
            {
                return false;
            }

            if (climate.ElevationAboveSea < rule.MinElevationAboveSea ||
                climate.ElevationAboveSea > rule.MaxElevationAboveSea)
            {
                return false;
            }

            if (climate.CoastDistance < rule.MinCoastDistance ||
                climate.CoastDistance > rule.MaxCoastDistance)
            {
                return false;
            }

            return true;
        }
    }
}
