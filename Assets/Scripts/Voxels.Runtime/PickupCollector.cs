using UnityEngine;
using Voxels.Core.Blocks;

namespace Voxels.Runtime
{
    public sealed class PickupCollector : MonoBehaviour
    {
        [SerializeField] float magnetRadius = 2.2f;
        [SerializeField] float vacuumRadius = 0.65f;
        [SerializeField] float magnetSpeed = 6f;

        PlayerInventory inventory;

        public void Initialize(PlayerInventory playerInventory) => inventory = playerInventory;

        void Update()
        {
            if (inventory == null)
            {
                return;
            }

            BlockPickup[] pickups = FindObjectsByType<BlockPickup>(FindObjectsSortMode.None);
            Vector3 position = transform.position;
            for (int i = 0; i < pickups.Length; i++)
            {
                BlockPickup pickup = pickups[i];
                if (pickup == null)
                {
                    continue;
                }

                float distance = Vector3.Distance(position, pickup.transform.position);
                if (distance <= vacuumRadius)
                {
                    inventory.Add(pickup.BlockId);
                    Destroy(pickup.gameObject);
                    continue;
                }

                if (distance <= magnetRadius)
                {
                    pickup.transform.position = Vector3.MoveTowards(
                        pickup.transform.position,
                        position,
                        magnetSpeed * Time.deltaTime);
                }
            }
        }
    }
}
