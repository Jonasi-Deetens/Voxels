using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;
using Voxels.World;
using Voxels.World.Climate;

namespace Voxels.Tests
{
    public sealed class WeatherRulesTests
    {
        [Test]
        public void Cold_tundra_biome_can_pick_snow()
        {
            var biome = ScriptableObject.CreateInstance<BiomeDefinition>();
            biome.name = "Biome_Tundra";
            var climate = new ClimateSample(0.8f, -0.4f, 0.7f, 0.2f, 0.5f, 4, 8, false);
            var rng = new Random(42);
            int snowCount = 0;
            for (int i = 0; i < 40; i++)
            {
                if (WeatherRules.PickTarget(biome, climate, ref rng) == WeatherKind.Snow)
                {
                    snowCount++;
                }
            }

            Assert.Greater(snowCount, 10);
        }

        [Test]
        public void Desert_biome_favors_clear()
        {
            var biome = ScriptableObject.CreateInstance<BiomeDefinition>();
            biome.name = "Biome_Desert";
            var climate = new ClimateSample(0.2f, 0.6f, 0.2f, 0.8f, 0.5f, 2, 20, false);
            var rng = new Random(99);
            int clearCount = 0;
            for (int i = 0; i < 40; i++)
            {
                if (WeatherRules.PickTarget(biome, climate, ref rng) == WeatherKind.Clear)
                {
                    clearCount++;
                }
            }

            Assert.Greater(clearCount, 20);
        }
    }
}
