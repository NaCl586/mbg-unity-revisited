using UnityEngine;

public class StreamingAsset : MonoBehaviour {
    [SerializeField] private Object streamingAsset;
    [SerializeField] private string filePath;

    public override string ToString()
    {
        return filePath;
    }
}