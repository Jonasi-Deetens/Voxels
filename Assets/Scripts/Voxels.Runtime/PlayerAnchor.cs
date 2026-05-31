using Unity.Mathematics;
using UnityEngine;
using Voxels.World;

namespace Voxels.Runtime
{
    public sealed class PlayerAnchor : MonoBehaviour
    {
        public int SpawnCellIndex { get; private set; } = -1;
        public float3 SurfaceUp { get; private set; }
        public float SurfaceRadius { get; private set; }

        public void Configure(int cellIndex, float3 surfaceUp, float surfaceRadius)
        {
            SpawnCellIndex = cellIndex;
            SurfaceUp = math.normalize(surfaceUp);
            SurfaceRadius = surfaceRadius;
            transform.localPosition = (Vector3)(SurfaceUp * SurfaceRadius);
            transform.localRotation = Quaternion.FromToRotation(Vector3.up, (Vector3)SurfaceUp);
        }
    }
}
