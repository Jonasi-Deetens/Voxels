using UnityEngine;
using Voxels.Core.Hex;

namespace Voxels.Runtime
{
    public sealed class WorldDebugOverlay : MonoBehaviour
    {
        [SerializeField] bool visible = true;
        [SerializeField] KeyCode toggleKey = KeyCode.F3;

        WorldScroller scroller;
        HexChunkManager chunkManager;
        CelestialSystem celestial;
        HexBlockInteractor interactor;

        public bool Visible => visible;

        public void Initialize(
            WorldScroller worldScroller,
            HexChunkManager chunks,
            CelestialSystem celestialSystem,
            HexBlockInteractor blockInteractor = null)
        {
            scroller = worldScroller;
            chunkManager = chunks;
            celestial = celestialSystem;
            interactor = blockInteractor;
        }

        void Update()
        {
            if (GameInput.WasDebugTogglePressedThisFrame())
            {
                visible = !visible;
            }
        }

        void OnGUI()
        {
            if (!visible || scroller == null)
            {
                return;
            }

            HexCoord hex = scroller.PlayerWorldHex;
            GUILayout.BeginArea(new Rect(12f, 12f, 380f, 200f), GUI.skin.box);
            GUILayout.Label("Debug (F3 to toggle)");
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
                GUILayout.Label($"Time of day: {celestial.TimeOfDay:0.000}  sun height: {celestial.SunHeight:0.00}");
            }

            if (interactor != null)
            {
                GUILayout.Label("LMB break | RMB place block");
            }

            GUILayout.EndArea();
        }
    }
}
