using UnityEngine;
using Voxels.World;

namespace Voxels.Runtime
{
    public sealed class PlayerEquipmentController : MonoBehaviour
    {
        BlockHotbar hotbar;
        HexWorld hexWorld;
        PlayerStatsController stats;

        public void Initialize(BlockHotbar blockHotbar, HexWorld world, PlayerStatsController playerStats)
        {
            hotbar = blockHotbar;
            hexWorld = world;
            stats = playerStats;
        }

        void LateUpdate()
        {
            if (hotbar == null || hexWorld == null || stats == null || stats.IsDead)
            {
                return;
            }

            PlayerEquipmentUtility.SyncHeldBonuses(hotbar, hexWorld.BlockRegistry, stats);
        }
    }
}
