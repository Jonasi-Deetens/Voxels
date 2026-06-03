using UnityEngine;
using Voxels.Core.Hex;
using Voxels.World;

namespace Voxels.Runtime
{
    public static class HexWorldRaycast
    {
        const float Epsilon = 0.05f;

        public static bool TryRaycastBlock(
            Ray ray,
            Transform worldRoot,
            HexCoord playerWorldHex,
            WorldSettings settings,
            LayerMask mask,
            float maxDistance,
            bool placeMode,
            out HexBlockTarget target)
        {
            target = default;
            if (worldRoot == null || settings == null)
            {
                return false;
            }

            if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance, mask, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            Vector3 sample = placeMode
                ? hit.point + hit.normal * Epsilon
                : hit.point - hit.normal * Epsilon;

            Vector3 local = worldRoot.InverseTransformPoint(sample);
            HexCoord localHex = FlatHexGrid.WorldToAxial(local, settings.BlockSize);
            HexCoord worldHex = playerWorldHex.Add(localHex);

            int layer = Mathf.Clamp(
                Mathf.FloorToInt(local.y / settings.BlockSize),
                0,
                settings.ColumnCapacity - 1);

            target = new HexBlockTarget(worldHex, layer, true);
            return true;
        }
    }
}
