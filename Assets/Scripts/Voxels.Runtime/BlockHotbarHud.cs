using UnityEngine;
using Voxels.World;

namespace Voxels.Runtime
{
    public sealed class BlockHotbarHud : MonoBehaviour
    {
        BlockHotbar hotbar;
        BlockRegistry registry;

        public void Initialize(BlockHotbar blockHotbar, BlockRegistry blockRegistry)
        {
            hotbar = blockHotbar;
            registry = blockRegistry;
        }

        void OnGUI()
        {
            if (hotbar == null)
            {
                return;
            }

            const int slotCount = 9;
            const float slotWidth = 52f;
            const float slotHeight = 28f;
            float totalWidth = slotCount * slotWidth + 8f;
            float x = (Screen.width - totalWidth) * 0.5f;
            float y = Screen.height - slotHeight - 16f;

            for (int i = 0; i < slotCount; i++)
            {
                Rect rect = new Rect(x + i * slotWidth, y, slotWidth - 4f, slotHeight);
                bool selected = i == hotbar.SelectedIndex;
                GUI.Box(rect, selected ? GUI.skin.box : GUI.skin.window);
                string label = $"{i + 1}: {hotbar.GetSlotLabel(i, registry)}";
                GUI.Label(rect, label);
            }
        }
    }
}
