using UnityEditor;
using UnityEngine;
using Voxels.Runtime;

namespace Voxels.EditorTools
{
    [CustomEditor(typeof(PlanetBootstrap))]
    public sealed class PlanetBootstrapEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (GUILayout.Button("Regenerate Planet"))
            {
                var bootstrap = (PlanetBootstrap)target;
                bootstrap.RegeneratePlanet();

                if (!Application.isPlaying && bootstrap.gameObject.scene.IsValid())
                {
                    EditorUtility.SetDirty(bootstrap);
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(bootstrap.gameObject.scene);
                }
            }
        }
    }
}
