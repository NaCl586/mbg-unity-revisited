using UnityEngine;

[RequireComponent(typeof(ReflectionProbe))]
public class MarbleReflectionUpdater : MonoBehaviour
{
    public Transform marble;        // Assign your marble
    public float updateDistance = 1f; // Only update probe when marble moves far enough
    private Vector3 lastMarblePos;
    private ReflectionProbe probe;

    void Awake()
    {
        probe = GetComponent<ReflectionProbe>();

        probe.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.ViaScripting;
        lastMarblePos = marble.position;

        // Initialize probe at marble position
        probe.transform.position = marble.position;
    }

    void LateUpdate()
    {
        // Move the probe to follow the marble
        probe.transform.position = marble.position;

        // Optional: only render if marble moved enough
        if (Vector3.Distance(lastMarblePos, marble.position) > updateDistance)
        {
            probe.RenderProbe(); // Expensive, do sparingly
            lastMarblePos = marble.position;
        }
    }
}
