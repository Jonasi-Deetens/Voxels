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
        HexBlockInteractor interactor;
        BlockHotbar hotbar;

        public bool Visible => visible;

        public void Initialize(
            WorldScroller worldScroller,
            HexChunkManager chunks,
            CelestialSystem celestialSystem,
            HexBlockInteractor blockInteractor = null,
            BlockHotbar blockHotbar = null)
        {
            scroller = worldScroller;
            chunkManager = chunks;
            celestial = celestialSystem;
            interactor = blockInteractor;
            hotbar = blockHotbar;
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
            bool creative = PlayerGameplayState.Instance != null && PlayerGameplayState.Instance.CreativeMode;

            GUILayout.BeginArea(new Rect(12f, 12f, 420f, 260f), GUI.skin.box);
            GUILayout.Label("Debug (F3 toggle)");
            GUILayout.Label($"World hex: ({hex.Q}, {hex.R})  creative: {creative}");
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

            if (hotbar != null && scroller.HexWorld != null)
            {
                GUILayout.Label(
                    $"Hotbar [{hotbar.SelectedIndex + 1}]: {hotbar.GetSlotLabel(hotbar.SelectedIndex, scroller.HexWorld.BlockRegistry)}");
            }

            GUILayout.Label("LMB break | RMB place | 1-9 / scroll hotbar");
            GUILayout.Label("F4 creative | F5 save | F6 load");
            GUILayout.EndArea();
        }
    }
}
