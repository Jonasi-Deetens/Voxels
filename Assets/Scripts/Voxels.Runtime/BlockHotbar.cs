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

        public int SelectedIndex => selectedIndex;
        public BlockId SelectedBlock => slots[selectedIndex];

        public void Initialize(BlockDefinition[] availableBlocks)
        {
            for (int i = 0; i < SlotCount; i++)
            {
                slots[i] = BlockId.Air;
            }

            if (availableBlocks == null)
            {
                slots[0] = new BlockId(1);
                return;
            }

            int count = Mathf.Min(SlotCount, availableBlocks.Length);
            for (int i = 0; i < count; i++)
            {
                if (availableBlocks[i] != null)
                {
                    slots[i] = availableBlocks[i].BlockId;
                }
            }

            if (slots[0].IsAir)
            {
                slots[0] = new BlockId(1);
            }

            selectedIndex = 0;
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
