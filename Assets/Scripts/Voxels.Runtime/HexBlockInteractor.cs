using UnityEngine;
using Voxels.Core.Blocks;
using Voxels.Core.Hex;
using Voxels.World;

namespace Voxels.Runtime
{
    /// <summary>
    /// Left click breaks blocks; right click places biome surface blocks.
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

        public void Initialize(
            HexWorld world,
            WorldSettings worldSettings,
            WorldScroller worldScroller,
            HexChunkManager chunks,
            Transform view)
        {
            hexWorld = world;
            settings = worldSettings;
            scroller = worldScroller;
            chunkManager = chunks;
            viewTransform = view;
        }

        void Update()
        {
            if (hexWorld == null || scroller == null || chunkManager == null || viewTransform == null)
            {
                return;
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

            if (!hexWorld.BlockRegistry.TryGetDefinition(existing, out BlockDefinition definition) ||
                !definition.IsSolid)
            {
                return;
            }

            column.SetBlock(target.Layer, BlockId.Air);
            chunkManager.RebuildChunksForWorldHex(target.WorldHex, scroller.PlayerWorldHex);
        }

        void TryPlaceBlock(in HexBlockTarget target)
        {
            BlockColumn column = hexWorld.GetOrCreateColumn(target.WorldHex);
            if (!column.GetBlock(target.Layer).IsAir)
            {
                return;
            }

            if (target.Layer > column.SurfaceHeight + settings.MaxHeightAboveSurface)
            {
                return;
            }

            BlockId placeId = ResolvePlaceBlock(target.WorldHex);
            column.SetBlock(target.Layer, placeId);
            chunkManager.RebuildChunksForWorldHex(target.WorldHex, scroller.PlayerWorldHex);
        }

        BlockId ResolvePlaceBlock(in HexCoord worldHex)
        {
            BiomeDefinition biome = hexWorld.GetBiome(worldHex);
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
    }
}
