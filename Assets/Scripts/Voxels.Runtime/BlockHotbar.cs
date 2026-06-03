using UnityEngine;
using Voxels.Core.Blocks;
using Voxels.World;

namespace Voxels.Runtime
{
    public sealed class BlockHotbar : MonoBehaviour
    {
        const int SlotCount = 9;

        readonly BlockId[] slots = new BlockId[SlotCount];
        int selectedIndex;
        PlayerInventory inventory;

        public int SelectedIndex => selectedIndex;
        public BlockId SelectedBlock => slots[selectedIndex];

        public void Initialize(BlockDefinition[] availableBlocks, BlockRegistry registry, PlayerInventory playerInventory = null)
        {
            inventory = playerInventory;
            if (VoxelsModConfig.TryApplyHotbar(this, registry))
            {
                return;
            }

            if (registry != null)
            {
                BlockHotbarPalette.ApplyToHotbar(this, registry);
                return;
            }

            SetSlotsFromDefinitions(availableBlocks);
        }

        public void BindInventory(PlayerInventory playerInventory) => inventory = playerInventory;

        public void SetSlots(BlockId[] blockIds)
        {
            for (int i = 0; i < SlotCount; i++)
            {
                slots[i] = i < blockIds.Length ? blockIds[i] : BlockId.Air;
            }

            if (slots[0].IsAir)
            {
                slots[0] = new BlockId(1);
            }

            selectedIndex = 0;
        }

        void SetSlotsFromDefinitions(BlockDefinition[] availableBlocks)
        {
            for (int i = 0; i < SlotCount; i++)
            {
                slots[i] = BlockId.Air;
            }

            if (availableBlocks != null)
            {
                int count = Mathf.Min(SlotCount, availableBlocks.Length);
                for (int i = 0; i < count; i++)
                {
                    if (availableBlocks[i] != null)
                    {
                        slots[i] = availableBlocks[i].BlockId;
                    }
                }
            }

            if (slots[0].IsAir)
            {
                slots[0] = new BlockId(1);
            }

            selectedIndex = 0;
        }

        public bool CanUseSelectedBlock(bool creative) =>
            creative || inventory == null || inventory.HasAtLeast(SelectedBlock);

        public bool IsSlotEmptyForGameplay(int index, bool creative)
        {
            if (creative)
            {
                return false;
            }

            BlockId id = GetSlot(index);
            return !id.IsAir && inventory != null && inventory.GetCount(id) <= 0;
        }

        public void SelectSlot(int index) => selectedIndex = Mathf.Clamp(index, 0, SlotCount - 1);

        public void SelectBlock(BlockId blockId)
        {
            for (int i = 0; i < SlotCount; i++)
            {
                if (slots[i] == blockId && HasStackForSlot(i))
                {
                    selectedIndex = i;
                    return;
                }
            }

            for (int i = 0; i < SlotCount; i++)
            {
                if (slots[i] == blockId)
                {
                    selectedIndex = i;
                    return;
                }
            }

            slots[selectedIndex] = blockId;
        }

        bool HasStackForSlot(int index)
        {
            if (inventory == null)
            {
                return true;
            }

            BlockId id = GetSlot(index);
            return id.IsAir || inventory.GetCount(id) > 0;
        }

        void Update()
        {
            for (int i = 0; i < SlotCount; i++)
            {
                if (GameInput.WasHotbarSlotPressed(i))
                {
                    selectedIndex = i;
                }
            }

            float scroll = GameInput.ReadScrollDelta();
            if (scroll > 0.01f)
            {
                selectedIndex = (selectedIndex + SlotCount - 1) % SlotCount;
            }
            else if (scroll < -0.01f)
            {
                selectedIndex = (selectedIndex + 1) % SlotCount;
            }
        }

        public BlockId GetSlot(int index) => slots[Mathf.Clamp(index, 0, SlotCount - 1)];

        public string GetSlotLabel(int index, BlockRegistry registry)
        {
            BlockId id = GetSlot(index);
            if (registry != null && registry.TryGetDefinition(id, out BlockDefinition definition))
            {
                return definition.DisplayName;
            }

            return id.IsAir ? "Air" : $"#{id.Value}";
        }
    }
}
