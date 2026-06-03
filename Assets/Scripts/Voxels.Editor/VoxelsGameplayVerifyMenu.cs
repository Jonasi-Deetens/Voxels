using UnityEditor;
using UnityEngine;
using Voxels.Core.Blocks;
using Voxels.Core.Hex;
using Voxels.World;

namespace Voxels.EditorTools
{
    public static class VoxelsGameplayVerifyMenu
    {
        [MenuItem("Voxels/Verify Gameplay Helpers")]
        public static void VerifyGameplayHelpers()
        {
            var settings = ScriptableObject.CreateInstance<WorldSettings>();
            var cache = new HexWorldDataCache(settings.ColumnCapacity, 16);
            var hex = new HexCoord(3, -2);
            BlockColumn column = cache.GetOrCreateColumn(hex);
            column.SetSurfaceHeight(10);
            column.SetBlock(5, new BlockId(2));

            int count = 0;
            foreach (var entry in cache.EnumerateColumns())
            {
                if (entry.Key == hex && entry.Value.GetBlock(5).Value == 2)
                {
                    count++;
                }
            }

            if (count != 1)
            {
                Debug.LogError("EnumerateColumns failed verification.");
                return;
            }

            Debug.Log("Gameplay helpers verify OK (cache enumerate, placement types compile).");
        }
    }
}
