using UnityEditor;
using UnityEngine;
using Voxels.Core.Hex;
using Voxels.Runtime;
using Voxels.World;

namespace Voxels.EditorTools
{
    [CustomEditor(typeof(WorldBootstrap), true)]
    public sealed class WorldBootstrapEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (GUILayout.Button("Regenerate World"))
            {
                var bootstrap = (WorldBootstrap)target;
                bootstrap.RegenerateWorld();

                if (!Application.isPlaying && bootstrap.gameObject.scene.IsValid())
                {
                    EditorUtility.SetDirty(bootstrap);
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(bootstrap.gameObject.scene);
                }
            }
        }

        void OnSceneGUI()
        {
            var bootstrap = (WorldBootstrap)target;
            SerializedProperty settingsProp = serializedObject.FindProperty("settings");
            if (settingsProp == null || settingsProp.objectReferenceValue == null)
            {
                return;
            }

            var settings = (WorldSettings)settingsProp.objectReferenceValue;
            if (settings == null || settings.InfiniteWorld)
            {
                return;
            }

            Handles.color = new Color(0.2f, 0.85f, 1f, 0.35f);
            int radius = settings.WorldHexRadius;
            float blockSize = settings.BlockSize;
            var ring = new System.Collections.Generic.List<HexCoord>();

            for (int q = -radius; q <= radius; q++)
            {
                for (int r = -radius; r <= radius; r++)
                {
                    var hex = new HexCoord(q, r);
                    if (HexagonMask.DistanceFromCenter(hex) == radius)
                    {
                        ring.Add(hex);
                    }
                }
            }

            Vector3 origin = bootstrap.transform.position;
            for (int i = 0; i < ring.Count; i++)
            {
                int next = (i + 1) % ring.Count;
                Vector3 a = origin + (Vector3)FlatHexGrid.AxialToWorld(ring[i], blockSize);
                Vector3 b = origin + (Vector3)FlatHexGrid.AxialToWorld(ring[next], blockSize);
                Handles.DrawLine(a, b);
            }
        }
    }
}
