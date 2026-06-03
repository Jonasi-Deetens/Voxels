using UnityEngine;
using Voxels.Core.Blocks;
using Voxels.World;

namespace Voxels.Runtime
{
    public sealed class BlockPickup : MonoBehaviour
    {
        [SerializeField] float lifetime = 45f;
        [SerializeField] float bobSpeed = 2f;
        [SerializeField] float bobHeight = 0.15f;

        BlockId blockId;
        Vector3 startPosition;
        float age;

        public BlockId BlockId => blockId;

        public static BlockPickup Spawn(BlockId id, Vector3 worldPosition, BlockRegistry registry)
        {
            if (id.IsAir)
            {
                return null;
            }

            var pickupObject = new GameObject($"Pickup_{id.Value}");
            pickupObject.transform.position = worldPosition;
            var pickup = pickupObject.AddComponent<BlockPickup>();
            pickup.blockId = id;
            pickup.startPosition = worldPosition;

            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "Visual";
            visual.transform.SetParent(pickupObject.transform, false);
            visual.transform.localScale = Vector3.one * 0.28f;
            Object.Destroy(visual.GetComponent<Collider>());

            if (registry != null && registry.TryGetDefinition(id, out BlockDefinition definition) &&
                definition.Material != null)
            {
                visual.GetComponent<Renderer>().sharedMaterial = definition.Material;
            }

            var collider = pickupObject.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = 0.35f;
            return pickup;
        }

        void Update()
        {
            age += Time.deltaTime;
            if (age >= lifetime)
            {
                Destroy(gameObject);
                return;
            }

            float bob = Mathf.Sin(age * bobSpeed) * bobHeight;
            transform.position = startPosition + Vector3.up * bob;
        }

        void Awake()
        {
            if (startPosition == Vector3.zero)
            {
                startPosition = transform.position;
            }
        }
    }
}
