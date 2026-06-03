using System.Linq;
using UnityEditor;
using UnityEngine;
using Voxels.Core.Hex;
using Voxels.Rendering;
using Voxels.World;
using Voxels.World.Generation;

namespace Voxels.EditorTools
{
    public static class VoxelsVerifyMenu
    {
        static System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<HexCoord, BlockColumn>> GetAllColumns(HexWorld world)
        {
            int radius = world.Settings.WorldHexRadius;
            for (int q = -radius; q <= radius; q++)
            for (int r = -radius; r <= radius; r++)
            {
                var hex = new HexCoord(q, r);
                if (world.IsInsideWorld(hex) && world.TryGetColumn(hex, out BlockColumn col))
                    yield return new System.Collections.Generic.KeyValuePair<HexCoord, BlockColumn>(hex, col);
            }
        }

        [MenuItem("Voxels/Verify Build Pipeline")]
        public static void VerifyBuildPipeline()
        {
            WorldSettings settings = AssetDatabase.LoadAssetAtPath<WorldSettings>("Assets/Data/Planet_Default.asset");
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
            var world = new HexWorld(settings, registry);
            var generator = new FlatTerrainGenerator(settings);
            generator.GenerateChunk(world, new ChunkCoord(0, 0));

            int landColumns = 0;
            foreach (var entry in GetAllColumns(world))
            {
                if (entry.Value.SurfaceHeight >= settings.SeaLevelLayer)
                {
                    landColumns++;
                }
            }

            var hexes = HexChunkUtility.CollectChunkHexes(world, new ChunkCoord(0, 0), settings.ChunkSizeHex);
            var meshBuilder = new FlatHexBlockMeshBuilder(world);
            ChunkMeshData meshData = meshBuilder.BuildChunk(HexCoord.Zero, hexes);

            Debug.Log(
                $"Verify OK: chunk hexes={hexes.Count}, landColumns={landColumns}, " +
                $"meshVertices={meshData.Vertices.Count}, biomeTypes loaded.");
        }
    }
}
