using Voxels.Core.Sphere;
using Voxels.World.Generation;

namespace Voxels.World
{
    public sealed class PlanetWorld
    {
        readonly PlanetSettings settings;
        readonly IcosphereHexGrid grid;
        readonly PlanetColumnStorage columns;
        readonly BlockRegistry blockRegistry;
        readonly float shellRadius;

        public PlanetSettings Settings => settings;
        public IcosphereHexGrid Grid => grid;
        public PlanetColumnStorage Columns => columns;
        public BlockRegistry BlockRegistry => blockRegistry;
        public float BlockHeight => settings.BlockSize;
        public float ShellRadius => shellRadius;

        public float ApproximateOuterRadius =>
            settings.ApproximateOuterRadius(shellRadius, settings.Biome);

        public PlanetWorld(PlanetSettings settings, BlockRegistry blockRegistry)
        {
            this.settings = settings;
            this.blockRegistry = blockRegistry;
            grid = new IcosphereHexGrid(settings.ResolveSubdivisionLevel());
            shellRadius = settings.ResolveShellRadius(grid.AverageNeighborArc);
            int columnCapacity = settings.SeaLevelLayer + (int)settings.Biome.MountainAmplitude + 16;
            columns = new PlanetColumnStorage(grid, columnCapacity);
        }

        public void Generate(IWorldGenerator generator)
        {
            generator.Generate(this);
        }

        public float LayerToWorldRadius(int layer) => settings.LayerToWorldRadius(shellRadius, layer);

        public float GetCellSurfaceWorldRadius(int cellIndex)
        {
            BlockColumn column = columns.GetColumn(cellIndex);
            return LayerToWorldRadius(column.SurfaceHeight + 1);
        }
    }
}
