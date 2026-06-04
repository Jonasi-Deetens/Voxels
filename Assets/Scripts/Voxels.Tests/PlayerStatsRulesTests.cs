using NUnit.Framework;
using Voxels.Runtime;

namespace Voxels.Tests
{
    public sealed class PlayerStatsRulesTests
    {
        [Test]
        public void Defense_reduces_damage()
        {
            float result = PlayerStatsRules.ApplyDefense(10f, 0.25f, 0.8f);
            Assert.AreEqual(7.5f, result, 0.001f);
        }

        [Test]
        public void Mining_multiplier_shortens_break_time()
        {
            float result = PlayerStatsRules.ApplyMiningSpeed(2f, 1.25f);
            Assert.AreEqual(1.6f, result, 0.001f);
        }
    }
}
