#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using TS;

[CustomEditor(typeof(MissionImporter))]
public class MissionImporterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(10);

        var importer = (MissionImporter)target;

        if (GUILayout.Button("Import Mission & Save Scene"))
        {
            importer.ImportMissionInEditor();
        }
    }
}
#endif