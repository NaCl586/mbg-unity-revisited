using System.Collections.Generic;
using UnityEngine;

public class CheckCollision : MonoBehaviour
{
    [Header("Collision Info")]
    public bool isColliding = false;
    public Vector3 normal = Vector3.up;
    public Vector3 point = Vector3.zero;
    public Collider other;

    [System.Serializable]
    private class CollisionRecord
    {
        public Collider collider;
        public float time;
        public RaycastHit hit;
    }

    List<CollisionRecord> collisions = new List<CollisionRecord>();
    private Vector3 previousPosition;
    private SphereCollider sphereCollider;
    private Movement movement;

    private static readonly Vector3[] probeDirections =
    {
        Vector3.down, Vector3.up, Vector3.forward, Vector3.back,
        Vector3.left, Vector3.right,
        (Vector3.down + Vector3.forward).normalized,
        (Vector3.down + Vector3.back).normalized,
        (Vector3.down + Vector3.left).normalized,
        (Vector3.down + Vector3.right).normalized
    };

    private void Awake()
    {
        sphereCollider = GetComponent<SphereCollider>();
        movement = GetComponent<Movement>();
        previousPosition = transform.position;
    }

    private void FixedUpdate()
    {
        Vector3 movementVector = transform.position - previousPosition;
        ManualCollisionCheck(previousPosition, movementVector);
        previousPosition = transform.position;

        // Expire old collisions
        for (int i = collisions.Count - 1; i >= 0; i--)
        {
            if (Time.time - collisions[i].time > 0.05f)
            {
                OnManualCollisionExit(collisions[i].collider);
                collisions.RemoveAt(i);
            }
        }

        isColliding = collisions.Count > 0;
        if (!isColliding) other = null;
    }

    private void ManualCollisionCheck(Vector3 startPos, Vector3 movement)
    {
        float radius = (sphereCollider.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z)) + 0.1f;

        // Ground check
        Vector3 origin = transform.position - Vector3.up * radius;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit groundHit, 0.5f) && groundHit.collider != sphereCollider)
            RegisterCollision(groundHit.collider, groundHit);

        // Side probes
        DetectMaterialsMultiRay();

        // Movement sweep
        if (movement.sqrMagnitude > Mathf.Epsilon &&
            Physics.SphereCast(startPos, radius, movement.normalized, out RaycastHit moveHit, movement.magnitude + 0.01f) &&
            moveHit.collider != sphereCollider)
        {
            RegisterCollision(moveHit.collider, moveHit);
        }
    }

    private void DetectMaterialsMultiRay()
    {
        float probeDistance = sphereCollider.radius + 0.1f;
        foreach (var dir in probeDirections)
        {
            if (Physics.Raycast(transform.position, dir, out RaycastHit hit, probeDistance) && hit.collider != sphereCollider)
            {
                RegisterCollision(hit.collider, hit);
                return;
            }
        }

        // No hits → airborne
        ClearCollisionState();
    }

    private void RegisterCollision(Collider col, RaycastHit hit)
    {
        // Update or add collision record
        CollisionRecord record = collisions.Find(c => c.collider == col);
        if (record != null)
        {
            record.time = Time.time;
            OnManualCollisionStay(col, hit);
        }
        else
        {
            collisions.Add(new CollisionRecord { collider = col, time = Time.time, hit = hit });
            OnManualCollisionEnter(col, hit);
        }
    }

    // Manual Collision Event Methods
    private void OnManualCollisionEnter(Collider collider, RaycastHit hit)
    {
        ApplyCollision(hit);

        LandMine lm;
        if (hit.collider.TryGetComponent<LandMine>(out lm))
            lm.CollisionEnter();
    }

    private void OnManualCollisionStay(Collider collider, RaycastHit hit) { 
        ApplyCollision(hit);

        Trapdoor t;
        if (hit.collider.TryGetComponent<Trapdoor>(out t))
            t.OnCollisionWithMarble();
    }

    private void OnManualCollisionExit(Collider collider)
    {
        if (collisions.Count == 0) ClearCollisionState();
    }

    private void OnTriggerStay(Collider other)
    {
        Powerups p;
        if (other.TryGetComponent<Powerups>(out p))
            p.PickupItem();
    }

    private void OnTriggerEnter(Collider other)
    {
        Gem g;
        if (other.TryGetComponent<Gem>(out g))
            g.PickupItem();

        if (other.CompareTag("OutOfBounds"))
            GameManager.onOutOfBounds?.Invoke();

        if (other.CompareTag("Finish"))
            GameManager.onFinish?.Invoke();

        Tornado t;
        if (other.TryGetComponent<Tornado>(out t))
            t.playerIsColliding = true;

        DuctFan df;
        if (other.TryGetComponent<DuctFan>(out df))
            df.playerIsColliding = true;

        HelpTrigger ht;
        if (other.TryGetComponent<HelpTrigger>(out ht))
            ht.TriggerEnter();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("InBounds"))
            GameManager.onOutOfBounds?.Invoke();

        Tornado t;
        if (other.TryGetComponent<Tornado>(out t))
            t.playerIsColliding = false;

        DuctFan df;
        if (other.TryGetComponent<DuctFan>(out df))
            df.playerIsColliding = false;
    }

    private void ClearCollisionState()
    {
        isColliding = false;
        other = null;
        movement.collidedTexture = null;

        FrictionManager.instance.RevertMaterial();
    }

    public Vector3 Rounding(Vector3 _vector)
    {
        var x = _vector.x;
        var y = _vector.y;
        var z = _vector.z;

        if (Mathf.Abs(x) < 0.1f) x = 0;
        if (Mathf.Abs(y) < 0.1f) y = 0;
        if (Mathf.Abs(z) < 0.1f) z = 0;

        return new Vector3(x, y, z).normalized;
    }

    private void ApplyCollision(RaycastHit hit)
    {
        if (hit.collider == null) return;

        normal = Rounding(hit.normal);
        point = hit.point;
        other = hit.collider;
        isColliding = true;

        string detectedTextureName = string.Empty;
        movement.collidedTexture = ResolveTexture(hit, out detectedTextureName);

        const string instanceSuffix = " (Instance)";
        if (detectedTextureName.EndsWith(instanceSuffix))
            detectedTextureName = detectedTextureName.Substring(0, detectedTextureName.Length - instanceSuffix.Length);

        FrictionSO frictionSO = FrictionManager.instance.SearchFriction(detectedTextureName);

        if (frictionSO)
            FrictionManager.instance.ApplyMaterial(frictionSO);
        else
            FrictionManager.instance.RevertMaterial();
    }

    // Material Detection
    private Texture ResolveTexture(RaycastHit hit, out string detectedTextureName)
    {
        detectedTextureName = string.Empty;
        Renderer renderer = hit.collider.GetComponent<Renderer>();
        MeshFilter meshFilter = hit.collider.GetComponent<MeshFilter>();
        Mesh mesh = meshFilter ? meshFilter.sharedMesh : null;

        if (renderer == null) return null;

        // Single material or no triangle info
        if (mesh == null || mesh.subMeshCount == 1 || hit.triangleIndex < 0)
        {
            Material mat = renderer.sharedMaterial;
            if (mat != null)
            {
                detectedTextureName = mat.name;
                return mat.mainTexture;
            }
            return null;
        }

        // Multi-material mesh → use cached lookup
        int[] triToSubmesh = MeshSubmeshCache.GetTriangleToSubmeshMap(mesh);
        if (triToSubmesh != null && hit.triangleIndex < triToSubmesh.Length)
        {
            int submeshIndex = triToSubmesh[hit.triangleIndex];

            Material[] materials = renderer.sharedMaterials;
            if (submeshIndex < 0 || submeshIndex >= materials.Length)
            {
                // Fallback to first material
                Material fallback = renderer.sharedMaterial;
                if (fallback != null)
                {
                    detectedTextureName = fallback.name;
                    return fallback.mainTexture;
                }
                return null;
            }

            Material mat = materials[submeshIndex];
            if (mat != null)
            {
                detectedTextureName = mat.name;
                return mat.mainTexture;
            }

        }

        return null;
    }


    public static class MeshSubmeshCache
    {
        private static readonly Dictionary<Mesh, int[]> cache = new Dictionary<Mesh, int[]>();

        public static int[] GetTriangleToSubmeshMap(Mesh mesh)
        {
            if (mesh == null) return null;

            if (!cache.TryGetValue(mesh, out int[] triToSubmesh))
            {
                int triangleCount = mesh.triangles.Length / 3;
                triToSubmesh = new int[triangleCount];

                // For each submesh, mark which global triangles belong to it
                for (int sub = 0; sub < mesh.subMeshCount; sub++)
                {
                    int[] tris = mesh.GetTriangles(sub);
                    for (int j = 0; j < tris.Length; j += 3)
                    {
                        // Find this triplet in the global mesh.triangles array
                        int a = tris[j];
                        int b = tris[j + 1];
                        int c = tris[j + 2];

                        // Search global triangles once
                        int[] globalTris = mesh.triangles;
                        for (int k = 0; k < globalTris.Length; k += 3)
                        {
                            if (globalTris[k] == a && globalTris[k + 1] == b && globalTris[k + 2] == c)
                            {
                                int triIndex = k / 3;
                                triToSubmesh[triIndex] = sub;
                                break;
                            }
                        }
                    }
                }

                cache[mesh] = triToSubmesh;
            }

            return triToSubmesh;
        }
    }


}
