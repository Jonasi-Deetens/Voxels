using Unity.Mathematics;
using UnityEngine;
using Voxels.Core.Blocks;
using Voxels.Core.Hex;
using Voxels.World;
using Voxels.World.Generation;

namespace Voxels.Runtime
{
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
        PlayerToolState toolState;
        PlayerInventory inventory;
        PlayerStatsController playerStats;

        float breakTimer;
        float breakDuration = 0.35f;
        HexBlockTarget breakTarget;
        bool breaking;

        public float BreakProgress => breaking && breakDuration > 0f ? Mathf.Clamp01(breakTimer / breakDuration) : 0f;
        public bool IsBreaking => breaking;
        public bool HasBlockTarget { get; private set; }

        public void Initialize(
            HexWorld world,
            WorldSettings worldSettings,
            WorldScroller worldScroller,
            HexChunkManager chunks,
            Transform view,
            Transform player,
            BlockHotbar blockHotbar,
            BlockEditFeedback editFeedback,
            PlayerToolState playerToolState,
            PlayerInventory playerInventory,
            PlayerStatsController stats = null)
        {
            hexWorld = world;
            settings = worldSettings;
            scroller = worldScroller;
            chunkManager = chunks;
            viewTransform = view;
            playerTransform = player;
            hotbar = blockHotbar;
            feedback = editFeedback;
            toolState = playerToolState;
            inventory = playerInventory;
            playerStats = stats;
        }

        void Update()
        {
            if (hexWorld == null || scroller == null || chunkManager == null || viewTransform == null)
            {
                return;
            }

            if (playerStats != null && playerStats.IsDead)
            {
                breaking = false;
                HasBlockTarget = false;
                return;
            }

            HasBlockTarget = TryGetTarget(false, out _);

            if (GameInput.WasCreativeTogglePressedThisFrame())
            {
                PlayerGameplayState state = PlayerGameplayState.Instance;
                if (state != null)
                {
                    state.ToggleCreativeMode();
                }
            }

            if (GameInput.WasEatPressedThisFrame() && hotbar != null && inventory != null && playerStats != null)
            {
                if (PlayerFoodUtility.TryEatFromHotbar(hotbar, inventory, hexWorld.BlockRegistry, playerStats, out string eatFeedback))
                {
                    GameHudView hud = FindAnyObjectByType<GameHudView>();
                    hud?.ShowTransientMessage(eatFeedback, 2.2f);
                }
            }

            if (GameInput.WasCycleToolPressedThisFrame() && toolState != null)
            {
                toolState.CycleTool();
            }

            if (TryGetTarget(false, out HexBlockTarget pickTarget) && GameInput.WasPickBlockPressedThisFrame())
            {
                PickBlockToHotbar(pickTarget);
            }

            if (GameInput.WasSecondaryPressedThisFrame())
            {
                if (toolState != null && toolState.ActiveTool == PlayerToolMode.Bucket)
                {
                    TryUseBucket();
                }
                else if (TryGetTarget(true, out HexBlockTarget placeTarget))
                {
                    TryPlaceBlock(placeTarget);
                }
            }

            UpdateBreaking();
        }

        void UpdateBreaking()
        {
            bool creative = IsCreative();

            if (GameInput.WasPrimaryPressedThisFrame())
            {
                if (TryGetTarget(false, out HexBlockTarget target))
                {
                    breaking = true;
                    breakTarget = target;
                    breakTimer = 0f;
                    RefreshBreakDuration();
                    if (creative)
                    {
                        CompleteBreak();
                    }
                }
            }

            if (!breaking)
            {
                return;
            }

            if (!GameInput.IsPrimaryHeld())
            {
                breaking = false;
                return;
            }

            if (!breakTarget.IsValid || !TryGetTarget(false, out HexBlockTarget current) ||
                current.WorldHex != breakTarget.WorldHex || current.Layer != breakTarget.Layer)
            {
                breaking = false;
                return;
            }

            if (creative)
            {
                return;
            }

            breakTimer += Time.deltaTime;
            if (breakTimer >= breakDuration)
            {
                CompleteBreak();
            }
        }

        void RefreshBreakDuration()
        {
            breakDuration = 0.35f;
            if (!hexWorld.TryGetColumn(breakTarget.WorldHex, out BlockColumn column))
            {
                return;
            }

            BlockId existing = column.GetBlock(breakTarget.Layer);
            hexWorld.BlockRegistry.TryGetDefinition(existing, out BlockDefinition definition);
            PlayerToolMode tool = toolState != null ? toolState.ActiveTool : PlayerToolMode.Hand;
            float mining = playerStats != null ? playerStats.GetMiningMultiplier() : 1f;
            breakDuration = Mathf.Max(0.05f, BlockBreakCalculator.GetBreakDuration(definition, tool, false, mining));
        }

        void CompleteBreak()
        {
            if (!breakTarget.IsValid)
            {
                breaking = false;
                return;
            }

            if (!hexWorld.TryGetColumn(breakTarget.WorldHex, out BlockColumn column))
            {
                breaking = false;
                return;
            }

            BlockId existing = column.GetBlock(breakTarget.Layer);
            if (existing.IsAir)
            {
                breaking = false;
                return;
            }

            if (!IsCreative())
            {
                if (!hexWorld.BlockRegistry.TryGetDefinition(existing, out BlockDefinition definition) ||
                    !definition.IsSolid)
                {
                    breaking = false;
                    return;
                }
            }

            column.SetBlock(breakTarget.Layer, BlockId.Air);
            hexWorld.MarkColumnDirty(breakTarget.WorldHex);
            chunkManager.RebuildChunksForWorldHex(breakTarget.WorldHex, scroller.PlayerWorldHex);
            feedback?.PlayBreak(GetBlockWorldPosition(breakTarget));

            if (!existing.IsAir)
            {
                if (IsCreative())
                {
                    inventory?.Add(existing);
                }
                else
                {
                    BlockPickup.Spawn(existing, GetBlockWorldPosition(breakTarget), hexWorld.BlockRegistry);
                }
            }

            WaterFlowUtility.SettleAround(hexWorld, breakTarget.WorldHex, settings);

            breaking = false;
        }

        void PickBlockToHotbar(in HexBlockTarget target)
        {
            if (!hexWorld.TryGetColumn(target.WorldHex, out BlockColumn column) || hotbar == null)
            {
                return;
            }

            BlockId blockId = column.GetBlock(target.Layer);
            if (blockId.IsAir)
            {
                return;
            }

            hotbar.SelectBlock(blockId);
            inventory?.Add(blockId);
        }

        void TryPlaceBlock(in HexBlockTarget target)
        {
            bool creative = IsCreative();
            if (!HexBlockPlacement.CanPlace(hexWorld, settings, playerTransform, target, creative))
            {
                return;
            }

            if (!creative && hotbar != null && !hotbar.CanUseSelectedBlock(false))
            {
                return;
            }

            BlockId placeId = ResolvePlaceBlock();
            if (placeId.IsAir)
            {
                return;
            }

            if (!creative && inventory != null && !inventory.TryConsume(placeId))
            {
                return;
            }

            if (!creative && hexWorld.BlockRegistry.TryGetDefinition(placeId, out BlockDefinition placeDef) &&
                placeDef.IsFluid)
            {
                return;
            }

            BlockColumn column = hexWorld.GetOrCreateColumn(target.WorldHex);
            column.SetBlock(target.Layer, placeId);
            hexWorld.MarkColumnDirty(target.WorldHex);
            chunkManager.RebuildChunksForWorldHex(target.WorldHex, scroller.PlayerWorldHex);
            feedback?.PlayPlace(GetBlockWorldPosition(target));
            WaterFlowUtility.SettleAround(hexWorld, target.WorldHex, settings);
        }

        bool IsCreative() =>
            PlayerGameplayState.Instance != null && PlayerGameplayState.Instance.CreativeMode;

        bool TryGetTarget(bool placeMode, out HexBlockTarget target)
        {
            target = default;
            Transform worldRoot = scroller.WorldRoot;
            if (worldRoot == null)
            {
                return false;
            }

            Ray ray = new Ray(viewTransform.position, viewTransform.forward);
            return HexWorldRaycast.TryRaycastBlock(
                ray,
                worldRoot,
                scroller.PlayerWorldHex,
                settings,
                blockMask,
                reachDistance,
                placeMode,
                out target) && target.IsValid && hexWorld.IsInsideWorld(target.WorldHex);
        }

        BlockId ResolvePlaceBlock()
        {
            if (hotbar != null && !hotbar.SelectedBlock.IsAir)
            {
                return hotbar.SelectedBlock;
            }

            BiomeDefinition biome = hexWorld.GetBiome(scroller.PlayerWorldHex);
            if (biome?.SurfaceBlock != null)
            {
                return biome.SurfaceBlock.BlockId;
            }

            return settings.Biome?.SurfaceBlock != null ? settings.Biome.SurfaceBlock.BlockId : new BlockId(2);
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
