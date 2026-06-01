using System.Diagnostics;

namespace Voxels.World.Generation
{
    public sealed class BuildFrameBudget
    {
        readonly double budgetMs;
        readonly Stopwatch stopwatch = Stopwatch.StartNew();

        public BuildFrameBudget(float budgetMs)
        {
            this.budgetMs = budgetMs > 0f ? budgetMs : 16f;
        }

        public bool ShouldYield()
        {
            return stopwatch.Elapsed.TotalMilliseconds >= budgetMs;
        }

        public void MarkYield()
        {
            stopwatch.Restart();
        }
    }
}
