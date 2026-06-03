using Unity.Mathematics;
using UnityEngine;
using Voxels.Core.Blocks;
using Voxels.Core.Hex;
using Voxels.World;

namespace Voxels.Runtime
{
    /// <summary>
    /// Left click breaks blocks; right click places the selected hotbar block.
    /// </summary>
    [DefaultExecutionOrder(60)]
    public sealed class HexBlockInteractor : MonoBehaviour
    {
        [SerializeField] float reachDistance = 6f;
        [SerializeField] LayerMask blockMask = ~0;

        HexWorld hexWorld;
        WorldSettings settings;
        WorldScroller scroller;
        HexChunkManager chunkManager;
        Transform viewTransform;
        Transform playerTransform;
        BlockHotbar hotbar;
        BlockEditFeedback feedback;

        public void Initialize(
            HexWorld world,
            WorldSettings worldSettings,
            WorldScroller worldScroller,
            HexChunkManager chunks,
            Transform view,
            Transform player,
            BlockHotbar blockHotbar,
            BlockEditFeedback editFeedback)
        {
            hexWorld = world;
            settings = worldSettings;
            scroller = worldScroller;
            chunkManager = chunks;
            viewTransform = view;
            playerTransform = player;
            hotbar = blockHotbar;
            feedback = editFeedback;
        }

        void Update()
        {
            if (hexWorld == null || scroller == null || chunkManager == null || viewTransform == null)
            {
                return;
            }

            if (GameInput.WasCreativeTogglePressedThisFrame())
            {
                PlayerGameplayState state = PlayerGameplayState.Instance;
                if (state != null)
                {
                    state.ToggleCreativeMode();
                }
            }

            if (!GameInput.WasPrimaryPressedThisFrame() && !GameInput.WasSecondaryPressedThisFrame())
            {
                return;
            }

            Transform worldRoot = scroller.WorldRoot;
            if (worldRoot == null)
            {
                return;
            }

            Ray ray = new Ray(viewTransform.position, viewTransform.forward);
            HexCoord playerHex = scroller.PlayerWorldHex;
            bool place = GameInput.WasSecondaryPressedThisFrame();

            if (!HexWorldRaycast.TryRaycastBlock(
                ray,
                worldRoot,
                playerHex,
                settings,
                blockMask,
                reachDistance,
                place,
                out HexBlockTarget target) || !target.IsValid)
            {
                return;
            }

            if (!hexWorld.IsInsideWorld(target.WorldHex))
            {
                return;
            }

            if (place)
            {
                TryPlaceBlock(target);
            }
            else
            {
                TryBreakBlock(target);
            }
        }

        void TryBreakBlock(in HexBlockTarget target)
        {
            if (!hexWorld.TryGetColumn(target.WorldHex, out BlockColumn column))
            {
                return;
            }

            BlockId existing = column.GetBlock(target.Layer);
            if (existing.IsAir)
            {
                return;
            }

            bool creative = PlayerGameplayState.Instance != null && PlayerGameplayState.Instance.CreativeMode;
            if (!creative)
            {
                if (!hexWorld.BlockRegistry.TryGetDefinition(existing, out BlockDefinition definition) ||
                    !definition.IsSolid)
                {
                    return;
                }
            }

            column.SetBlock(target.Layer, BlockId.Air);
            chunkManager.RebuildChunksForWorldHex(target.WorldHex, scroller.PlayerWorldHex);
            feedback?.PlayBreak(GetBlockWorldPosition(target));
        }

        void TryPlaceBlock(in HexBlockTarget target)
        {
            if (!HexBlockPlacement.CanPlace(hexWorld, settings, playerTransform, target))
            {
                return;
            }

            BlockId placeId = ResolvePlaceBlock();
            if (placeId.IsAir)
            {
                return;
            }

            BlockColumn column = hexWorld.GetOrCreateColumn(target.WorldHex);
            column.SetBlock(target.Layer, placeId);
            chunkManager.RebuildChunksForWorldHex(target.WorldHex, scroller.PlayerWorldHex);
            feedback?.PlayPlace(GetBlockWorldPosition(target));
        }

        BlockId ResolvePlaceBlock()
        {
            if (hotbar != null && !hotbar.SelectedBlock.IsAir)
            {
                return hotbar.SelectedBlock;
            }

            BiomeDefinition biome = hexWorld.GetBiome(scroller.PlayerWorldHex);
            if (biome != null && biome.SurfaceBlock != null)
            {
                return biome.SurfaceBlock.BlockId;
            }

            if (settings.Biome != null && settings.Biome.SurfaceBlock != null)
            {
                return settings.Biome.SurfaceBlock.BlockId;
            }

            return new BlockId(2);
        }

        Vector3 GetBlockWorldPosition(in HexBlockTarget target)
        {
            float3 local = FlatHexGrid.AxialToWorld(target.WorldHex, settings.BlockSize);
            local.y = (target.Layer + 0.5f) * settings.BlockSize;
            Transform worldRoot = scroller.WorldRoot;
            return worldRoot != null ? worldRoot.TransformPoint(local) : (Vector3)local;
        }
    }
}
