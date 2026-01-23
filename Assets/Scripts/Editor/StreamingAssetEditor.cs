using UnityEditor;

[CustomEditor(typeof(StreamingAsset))]
public class StreamingAssetEditor : Editor
{
    SerializedProperty filePath;
    SerializedProperty streamingAsset;
    
    const string kAssetPrefix = "Assets/StreamingAssets";
    
    private void OnEnable()
    {
        filePath = serializedObject.FindProperty("filePath");
        streamingAsset = serializedObject.FindProperty("streamingAsset");
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(streamingAsset);
        EditorGUILayout.PropertyField(filePath);
    
        if (streamingAsset.objectReferenceValue == null) {
            return;
        }
    
        string assetPath = AssetDatabase.GetAssetPath(streamingAsset.objectReferenceValue.GetInstanceID());
        if (assetPath.StartsWith(kAssetPrefix)) {
            assetPath = assetPath.Substring(kAssetPrefix.Length);
        }
        filePath.stringValue = assetPath;
        serializedObject.ApplyModifiedProperties();
    }
}