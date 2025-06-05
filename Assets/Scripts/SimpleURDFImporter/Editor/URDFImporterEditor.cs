using UnityEngine;
using UnityEditor;
using SimpleURDFImporter.Components;

namespace SimpleURDFImporter.Editor
{
    [CustomEditor(typeof(URDFImporter))]
    public class URDFImporterEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            URDFImporter importer = (URDFImporter)target;
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Import URDF", GUILayout.Height(30)))
            {
                string path = EditorUtility.OpenFilePanel("Select URDF File", Application.dataPath, "urdf,xml");
                if (!string.IsNullOrEmpty(path))
                {
                    importer.ImportURDF(path);
                }
            }
            
            if (GUILayout.Button("Clear Robot", GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog("Clear Robot", "Are you sure you want to clear the current robot?", "Yes", "No"))
                {
                    // Clear the robot
                    while (importer.transform.childCount > 0)
                    {
                        DestroyImmediate(importer.transform.GetChild(0).gameObject);
                    }
                }
            }
        }
    }
}