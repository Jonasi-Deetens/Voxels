using System.Linq;
using UnityEditor;
using UnityEngine;
using Voxels.Core.Blocks;
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

            if (settings.BiomeCatalog == null)
            {
                Debug.LogError("Planet_Default is missing BiomeCatalog. Run Voxels/Setup Default Content.");
                return;
            }

            BlockDefinition[] blockList = AssetDatabase
                .FindAssets("t:BlockDefinition", new[] { "Assets/Data/Blocks" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(path => AssetDatabase.LoadAssetAtPath<BlockDefinition>(path))
                .Where(block => block != null)
                .ToArray();

            BlockRegistry registry = BlockRegistryBuilder.Build(settings, blockList);
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
            int biomeTypes = world.BiomeMap.Biomes.Count;
            BlockId waterId = settings.BiomeCatalog.OceanBiome.WaterBlock.BlockId;

            for (int i = 0; i < world.Columns.CellCount; i++)
            {
                BlockColumn column = world.Columns.GetColumn(i);
                if (column.GetBlock(settings.SeaLevelLayer) == waterId)
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
            ChunkMeshData waterMesh = new WaterMeshBuilder(world).Build();

            Debug.Log(
                $"Voxels verification passed. Cells={world.Grid.CellCount}, subdiv={settings.ResolveSubdivisionLevel()}, " +
                $"hex={hexagons}, pent={pentagons}, biomes={biomeTypes}, shellRadius={world.ShellRadius:F2}, " +
                $"seaLevel={settings.SeaLevelLayer}, waterColumns={waterColumns}, caveColumns={caveColumns}, " +
                $"firstChunkVertices={meshData.Vertices.Count}, waterVertices={waterMesh.Vertices.Count}.");
        }
    }
}
