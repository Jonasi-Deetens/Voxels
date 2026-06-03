using UnityEngine;

namespace Voxels.Runtime
{
    public sealed class WorldRuntimeProfiler : MonoBehaviour
    {
        int meshesBuiltThisFrame;
        int totalMeshesBuilt;
        float lastMeshBuildMs;
        float smoothedFrameMs;

        public int TotalMeshesBuilt => totalMeshesBuilt;
        public float LastMeshBuildMs => lastMeshBuildMs;
        public float SmoothedFrameMs => smoothedFrameMs;
        public int MeshesBuiltThisFrame => meshesBuiltThisFrame;

        void Update()
        {
            meshesBuiltThisFrame = 0;
            smoothedFrameMs = Mathf.Lerp(smoothedFrameMs, Time.unscaledDeltaTime * 1000f, 0.08f);
        }

        public void RecordMeshBuild(float milliseconds)
        {
            lastMeshBuildMs = milliseconds;
            totalMeshesBuilt++;
            meshesBuiltThisFrame++;
        }

        public string BuildSummary()
        {
            return $"frame {smoothedFrameMs:0.0}ms | last mesh {lastMeshBuildMs:0.0}ms | meshes {totalMeshesBuilt}";
        }
    }
}
