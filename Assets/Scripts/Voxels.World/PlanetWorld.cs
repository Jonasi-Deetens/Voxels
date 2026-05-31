using Voxels.Core.Sphere;
using Voxels.World.Generation;

namespace Voxels.World
{
    public sealed class PlanetWorld
    {
        readonly PlanetSettings settings;
        readonly IcosphereHexGrid grid;
        readonly PlanetColumnStorage columns;
        readonly PlanetBiomeMap biomeMap;
        readonly BlockRegistry blockRegistry;
        readonly float shellRadius;

        public PlanetSettings Settings => settings;
        public IcosphereHexGrid Grid => grid;
        public PlanetColumnStorage Columns => columns;
        public PlanetBiomeMap BiomeMap => biomeMap;
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
            float maxAmplitude = settings.BiomeCatalog != null
                ? settings.BiomeCatalog.MaxMountainAmplitude
                : settings.Biome != null ? settings.Biome.MountainAmplitude : 18f;
            int columnCapacity = settings.SeaLevelLayer + (int)maxAmplitude + 16;
            columns = new PlanetColumnStorage(grid, columnCapacity);
            biomeMap = new PlanetBiomeMap(grid.CellCount);
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
