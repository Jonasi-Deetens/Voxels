using Voxels.Core.Blocks;
using Voxels.World;

namespace Voxels.Runtime
{
    public static class PlayerEquipmentUtility
    {
        const string HeldArmorSource = "held_block";

        public static void SyncHeldBonuses(
            BlockHotbar hotbar,
            BlockRegistry registry,
            PlayerStatsController stats)
        {
            if (hotbar == null || registry == null || stats == null)
            {
                return;
            }

            stats.RemoveModifiersFromSource(HeldArmorSource);

            BlockId blockId = hotbar.SelectedBlock;
            if (blockId.IsAir || !registry.TryGetDefinition(blockId, out BlockDefinition definition))
            {
                return;
            }

            if (!definition.HasHeldBonus)
            {
                return;
            }

            stats.AddModifier(new StatModifier(
                HeldArmorSource,
                definition.HeldDefensePercent,
                definition.HeldMiningMultiplier));
        }
    }
}
