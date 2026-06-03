using UnityEngine;
using Voxels.World;
using Voxels.World.Crafting;

namespace Voxels.Runtime
{
    public sealed class CraftingSystem : MonoBehaviour
    {
        PlayerInventory inventory;
        BlockRegistry registry;
        string lastMessage = string.Empty;
        float messageTimer;

        public string LastMessage => lastMessage;

        public void Initialize(PlayerInventory playerInventory, BlockRegistry blockRegistry)
        {
            inventory = playerInventory;
            registry = blockRegistry;
        }

        void Update()
        {
            if (messageTimer > 0f)
            {
                messageTimer -= Time.deltaTime;
            }

            if (!GameInput.WasCraftPressedThisFrame() || inventory == null || registry == null)
            {
                return;
            }

            TryCraft();
        }

        public bool TryCraft()
        {
            if (inventory == null || registry == null)
            {
                return false;
            }

            bool ok = CraftingRecipeCatalog.TryCraft(registry, inventory, out string message);
            lastMessage = message;
            messageTimer = 2.5f;
            return ok;
        }
    }
}
