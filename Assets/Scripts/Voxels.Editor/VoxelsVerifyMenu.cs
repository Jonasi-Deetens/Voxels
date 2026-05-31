using UnityEditor;
using UnityEngine;
using Voxels.Rendering;
using Voxels.World;
using Voxels.World.Generation;

namespace Voxels.EditorTools
{
    public static class VoxelsVerifyMenu
    {
        [MenuItem("Voxels/Verify Build Pipeline")]
        public static void VerifyBuildPipeline()
        {
            PlanetSettings settings = AssetDatabase.LoadAssetAtPath<PlanetSettings>("Assets/Data/Planet_Default.asset");
            if (settings == null)
            {
                Debug.LogError("Missing Assets/Data/Planet_Default.asset. Run Voxels/Setup Default Content first.");
                return;
            }

            BiomeDefinition biome = settings.Biome;
            var registry = new BlockRegistry();
            registry.Register(biome.SurfaceBlock);
            registry.Register(biome.SubsoilBlock);
            registry.Register(biome.BedrockBlock);
            registry.Register(biome.CoreBlock);
            registry.Register(biome.MantleBlock);
            registry.Register(biome.WaterBlock);
            registry.Register(biome.UnderwaterSurfaceBlock);

            var world = new PlanetWorld(settings, registry);
            int pentagons = 0;
            int hexagons = 0;

            for (int i = 0; i < world.Grid.CellCount; i++)
            {
                ref readonly var cell = ref world.Grid.GetCell(i);
                if (cell.IsPentagon)
                {
                    pentagons++;
                }
                else
                {
                    hexagons++;
                }
            }

            world.Generate(new PlanetLayerGenerator(settings));

            int waterColumns = 0;
            int caveColumns = 0;
            for (int i = 0; i < world.Columns.CellCount; i++)
            {
                BlockColumn column = world.Columns.GetColumn(i);
                if (column.GetBlock(settings.SeaLevelLayer) == biome.WaterBlock.BlockId)
                {
                    waterColumns++;
                }

                if (column.IsAir(settings.CrustTopLayer + 2))
                {
                    caveColumns++;
                }
            }

            var meshBuilder = new HexBlockMeshBuilder(world);
            var firstChunk = PlanetChunkUtility.BuildChunkCellGroups(world.Grid.CellCount, settings.CellsPerChunk)[0];
            ChunkMeshData meshData = meshBuilder.BuildChunk(firstChunk);

            Debug.Log(
                $"Voxels verification passed. Cells={world.Grid.CellCount}, hex={hexagons}, pent={pentagons}, " +
                $"shellRadius={world.ShellRadius:F2}, blockSize={settings.BlockSize}, " +
                $"seaLevel={settings.SeaLevelLayer}, waterColumns={waterColumns}, caveColumns={caveColumns}, " +
                $"firstChunkVertices={meshData.Vertices.Count}.");
        }
    }
}
