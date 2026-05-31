using Voxels.World;

namespace Voxels.World.Generation
{
    public interface IWorldGenerator
    {
        void Generate(PlanetWorld world);
    }
}
