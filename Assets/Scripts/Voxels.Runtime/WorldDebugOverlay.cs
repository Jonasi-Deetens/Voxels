using UnityEngine;
using Voxels.Core.Hex;

namespace Voxels.Runtime
{
    public sealed class WorldDebugOverlay : MonoBehaviour
    {
        [SerializeField] bool visible = true;

        WorldScroller scroller;
        HexChunkManager chunkManager;
        CelestialSystem celestial;

        public void Initialize(WorldScroller worldScroller, HexChunkManager chunks, CelestialSystem celestialSystem)
        {
            scroller = worldScroller;
            chunkManager = chunks;
            celestial = celestialSystem;
        }

        void OnGUI()
        {
            if (!visible || scroller == null)
            {
                return;
            }

            HexCoord hex = scroller.PlayerWorldHex;
            GUILayout.BeginArea(new Rect(12f, 12f, 360f, 160f), GUI.skin.box);
            GUILayout.Label($"World hex: ({hex.Q}, {hex.R})");
            if (scroller.HexWorld != null)
            {
                GUILayout.Label($"Edge distance: {scroller.HexWorld.DistanceToEdge(hex)} hex");
                GUILayout.Label($"Cached columns: {scroller.HexWorld.DataCache.CachedCellCount}");
            }

            if (chunkManager != null)
            {
                GUILayout.Label($"Loaded chunks: {chunkManager.LoadedChunkCount}  mesh queue: {chunkManager.PendingMeshJobs}");
            }

            if (celestial != null)
            {
                GUILayout.Label($"Time of day: {celestial.TimeOfDay:0.000}");
            }

            GUILayout.EndArea();
        }
    }
}
