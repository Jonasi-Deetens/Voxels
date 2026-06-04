using NUnit.Framework;
using Voxels.World.Modding;

namespace Voxels.Tests
{
    public sealed class VoxelsModConfigTests
    {
        [Test]
        public void TryGetFoodOverride_reads_configured_block()
        {
            VoxelsModConfig.Reload();
            bool found = VoxelsModConfig.TryGetFoodOverride("Fungus", out float hunger, out float health);
            Assert.IsTrue(found);
            Assert.Greater(hunger, 30f);
            Assert.Greater(health, 0f);
        }

        [Test]
        public void BuildModRecipes_includes_json_entry()
        {
            VoxelsModConfig.Reload();
            Assert.Greater(VoxelsModConfig.BuildModRecipes().Count, 0);
        }

        [Test]
        public void ResolvePoiWeight_scales_matching_biome()
        {
            VoxelsModConfig.Reload();
            float weighted = VoxelsModConfig.ResolvePoiWeight("Biome_CrystalWastes", "StoneRuin", 1f);
            Assert.Greater(weighted, 1f);
        }
    }
}
