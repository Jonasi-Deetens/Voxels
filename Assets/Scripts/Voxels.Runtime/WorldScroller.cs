using Unity.Mathematics;
using UnityEngine;
using Voxels.Core.Hex;
using Voxels.World;

namespace Voxels.Runtime
{
    /// <summary>
    /// Keeps the player near the origin by scrolling the world root. Tracks world-space hex position.
    /// </summary>
    public sealed class WorldScroller : MonoBehaviour
    {
        [SerializeField] Transform worldRoot;
        [SerializeField] Transform player;

        WorldSettings settings;
        HexCoord playerWorldHex;
        float3 accumulatedWorldOffset;

        public HexCoord PlayerWorldHex => playerWorldHex;
        public Transform WorldRoot => worldRoot;

        public void Initialize(WorldSettings worldSettings, Transform root, Transform playerTransform, HexCoord spawnHex)
        {
            settings = worldSettings;
            worldRoot = root;
            player = playerTransform;
            playerWorldHex = spawnHex;
            accumulatedWorldOffset = FlatHexGrid.AxialToWorld(spawnHex, settings.BlockSize);
            if (worldRoot != null)
            {
                worldRoot.position = Vector3.zero;
            }
        }

        public void AddWorldOffset(float3 delta)
        {
            accumulatedWorldOffset += delta;
            playerWorldHex = FlatHexGrid.WorldToAxial(accumulatedWorldOffset, settings.BlockSize);
        }

        void LateUpdate()
        {
            if (player == null || worldRoot == null || settings == null)
            {
                return;
            }

            Vector3 playerPos = player.position;
            Vector3 xzOffset = new Vector3(playerPos.x, 0f, playerPos.z);
            if (xzOffset.sqrMagnitude < 0.0001f)
            {
                return;
            }

            worldRoot.position -= xzOffset;
            player.position = new Vector3(0f, playerPos.y, 0f);
            TryRecenterFloatingOrigin();
        }

        void TryRecenterFloatingOrigin()
        {
            float threshold = settings.FloatingOriginRecenterDistance;
            if (worldRoot.position.sqrMagnitude <= threshold * threshold)
            {
                return;
            }

            Vector3 shift = worldRoot.position;
            worldRoot.position = Vector3.zero;
            if (player != null)
            {
                player.position -= shift;
            }
        }
    }
}
