// References:
// P-Invoke for strings:
//   http://stackoverflow.com/questions/370079/pinvoke-for-c-function-that-returns-char/370519#370519
//
// Note:
// This script must execute before other classes can be used. Make sure the execution is prior
// to the default time.

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class Dif : MonoBehaviour {

	public string filePath;
	public Material DefaultMaterial;

    [Header("Collision / Chunking")]
    public int maxTrianglesPerChunk = 1000;

    public bool GenerateMovingPlatformMesh(int interiorIndex)
    {
        // Ensure MeshFilter and MeshRenderer exist
        var meshFilter = gameObject.GetComponent<MeshFilter>() ?? gameObject.AddComponent<MeshFilter>();
        var meshRenderer = gameObject.GetComponent<MeshRenderer>() ?? gameObject.AddComponent<MeshRenderer>();

        // Load DIF resource
        var resource = DifResourceManager.getResource(Path.Combine(Application.streamingAssetsPath, filePath), interiorIndex);

        if (resource == null)
        {
            Debug.LogError("Dif decode failed");
            return false;
        }

        if (resource.vertices == null ||
            resource.normals == null ||
            resource.tangents == null ||
            resource.uvs == null)
        {
            Debug.LogError(
                $"Invalid DIF resource\n" +
                $"verts: {resource.vertices != null}\n" +
                $"normals: {resource.normals != null}\n" +
                $"tangents: {resource.tangents != null}\n" +
                $"uvs: {resource.uvs != null}"
            );

            return false;
        }


        // Torque (Z-up) → Unity (Y-up)
        Quaternion torqueToUnity = Quaternion.Euler(90f, 0f, 0f);

        // --- Render Mesh (visuals) ---
        Mesh renderMesh = new Mesh();
        renderMesh.name = Path.GetFileNameWithoutExtension(filePath);

        renderMesh.vertices = resource.vertices
            .Select(p => torqueToUnity * new Vector3(p.x, -p.y, p.z))
            .ToArray();

        renderMesh.normals = resource.normals
            .Select(n => torqueToUnity * new Vector3(n.x, -n.y, n.z))
            .ToArray();

        renderMesh.uv = resource.uvs;

        renderMesh.tangents = resource.tangents
            .Select(t =>
            {
                Vector3 v = torqueToUnity * new Vector3(t.x, -t.y, t.z);
                return new Vector4(v.x, v.y, v.z, t.w);
            })
            .ToArray();

        renderMesh.subMeshCount = resource.triangleIndices.Length;
        for (int i = 0; i < resource.triangleIndices.Length; i++)
            renderMesh.SetTriangles(resource.triangleIndices[i], i);

        renderMesh.RecalculateBounds();
        meshFilter.mesh = renderMesh;

        // --- Physics Mesh (collider) ---
        Mesh physicsMesh = new Mesh();
        physicsMesh.vertices = renderMesh.vertices;
        physicsMesh.triangles = renderMesh.triangles;

        physicsMesh = WeldPhysicsMesh(physicsMesh, 0.0001f);
        physicsMesh = FlatShadePhysicsMesh(physicsMesh);

        physicsMesh.RecalculateNormals();
        physicsMesh.RecalculateTangents();
        physicsMesh.RecalculateBounds();

        var meshCollider = gameObject.GetComponent<MeshCollider>() ?? gameObject.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = physicsMesh;
        meshCollider.convex = false;

        // --- Materials ---
        Material[] materials = new Material[resource.triangleIndices.Length];
        for (int i = 0; i < materials.Length; i++)
        {
            var materialPath = ResolveTexturePath(resource.materials[i]);
            materials[i] = DefaultMaterial;

            if (File.Exists(materialPath))
            {
                var tex = new Texture2D(2, 2);
                tex.LoadImage(File.ReadAllBytes(materialPath));

                materials[i] = Instantiate(DefaultMaterial);
                materials[i].mainTexture = tex;
                materials[i].name = resource.materials[i];
            }
        }
        meshRenderer.materials = materials;

        return true;
    }

    public bool GenerateMesh(int interiorIndex)
    {
        // Load DIF resource
        var resource = DifResourceManager.getResource(Path.Combine(Application.streamingAssetsPath, filePath), interiorIndex);

        if (resource == null)
        {
            Debug.LogError("Dif decode failed");
            return false;
        }

        if (resource.vertices == null ||
            resource.normals == null ||
            resource.tangents == null ||
            resource.uvs == null)
        {
            Debug.LogError(
                $"Invalid DIF resource\n" +
                $"verts: {resource.vertices != null}\n" +
                $"normals: {resource.normals != null}\n" +
                $"tangents: {resource.tangents != null}\n" +
                $"uvs: {resource.uvs != null}"
            );

            return false;
        }

        // Torque (Z-up) → Unity (Y-up)
        Quaternion torqueToUnity = Quaternion.Euler(90f, 0f, 0f);

        // Remove any old colliders
        foreach (var c in GetComponents<MeshCollider>())
            DestroyImmediate(c);

        int chunkIndex = 0;

        for (int mat = 0; mat < resource.triangleIndices.Length; mat++)
        {
            int[] materialTris = resource.triangleIndices[mat];
            if (materialTris == null || materialTris.Length == 0)
                continue;

            string matName = resource.materials[mat];
            Material material = ResolveMaterial(matName);

            List<int> triangleBuffer = new List<int>(maxTrianglesPerChunk * 3);

            for (int i = 0; i < materialTris.Length; i += 3)
            {
                triangleBuffer.Add(materialTris[i]);
                triangleBuffer.Add(materialTris[i + 1]);
                triangleBuffer.Add(materialTris[i + 2]);

                if (triangleBuffer.Count >= maxTrianglesPerChunk * 3)
                {
                    CreateChunk(
                        chunkIndex++,
                        triangleBuffer.ToArray(),
                        torqueToUnity,
                        resource,
                        material
                    );
                    triangleBuffer.Clear();
                }
            }

            // Flush remainder
            if (triangleBuffer.Count > 0)
            {
                CreateChunk(
                    chunkIndex++,
                    triangleBuffer.ToArray(),
                    torqueToUnity,
                    resource,
                    material
                );
            }
        }
        return true;
    }

    void CreateChunk(
    int index,
    int[] tris,
    Quaternion torqueToUnity,
    DifResource resource,
    Material material
)
    {
        GameObject chunk = new GameObject($"DIF_Chunk_{index}");
        chunk.transform.SetParent(transform, false);
        chunk.isStatic = true;

        // --- Build render mesh ---
        Mesh mesh = new Mesh();
        mesh.name = $"DIF_Mesh_{index}";

        mesh.vertices = resource.vertices
            .Select(v => torqueToUnity * new Vector3(v.x, -v.y, v.z))
            .ToArray();

        mesh.normals = resource.normals
            .Select(n => torqueToUnity * new Vector3(n.x, -n.y, n.z))
            .ToArray();

        mesh.uv = resource.uvs;
        mesh.triangles = tris;
        mesh.RecalculateBounds();

        var mf = chunk.AddComponent<MeshFilter>();
        var mr = chunk.AddComponent<MeshRenderer>();

        mf.sharedMesh = mesh;
        mr.sharedMaterial = material; // ✅ CORRECT MATERIAL

        // --- Physics mesh ---
        Mesh physicsMesh = Instantiate(mesh);
        physicsMesh = WeldPhysicsMesh(physicsMesh, 0.01f);
        physicsMesh = RemoveTinyTriangles(physicsMesh, 0.0005f);
        physicsMesh.RecalculateNormals();
        physicsMesh.RecalculateBounds();

        var mc = chunk.AddComponent<MeshCollider>();
        mc.sharedMesh = physicsMesh;
        mc.convex = false;
    }



    Material ResolveMaterial(string materialName)
    {
        if (string.IsNullOrEmpty(materialName))
            return DefaultMaterial;

        var mat = Instantiate(DefaultMaterial);
        mat.name = materialName;

        string texPath = ResolveTexturePath(materialName);
        if (File.Exists(texPath))
        {
            var tex = new Texture2D(2, 2);
            tex.LoadImage(File.ReadAllBytes(texPath));
            mat.mainTexture = tex;
        }

        return mat;
    }


    Mesh RemoveTinyTriangles(Mesh mesh, float minArea)
    {
        var verts = mesh.vertices;
        var tris = mesh.triangles;

        var newTris = new List<int>(tris.Length);

        for (int i = 0; i < tris.Length; i += 3)
        {
            Vector3 a = verts[tris[i]];
            Vector3 b = verts[tris[i + 1]];
            Vector3 c = verts[tris[i + 2]];

            float area = Vector3.Cross(b - a, c - a).magnitude * 0.5f;
            if (area >= minArea)
            {
                newTris.Add(tris[i]);
                newTris.Add(tris[i + 1]);
                newTris.Add(tris[i + 2]);
            }
        }

        mesh.triangles = newTris.ToArray();
        return mesh;
    }


    // Weld by position only (for physics mesh)
    private Mesh WeldPhysicsMesh(Mesh mesh, float tolerance)
    {
        var oldVerts = mesh.vertices;
        var oldTris = mesh.triangles;

        var newVerts = new List<Vector3>();
        var remap = new int[oldVerts.Length];
        var dict = new Dictionary<Vector3, int>();

        for (int i = 0; i < oldVerts.Length; i++)
        {
            Vector3 v = oldVerts[i];
            Vector3 key = new Vector3(
                Mathf.Round(v.x / tolerance) * tolerance,
                Mathf.Round(v.y / tolerance) * tolerance,
                Mathf.Round(v.z / tolerance) * tolerance
            );

            if (!dict.TryGetValue(key, out int idx))
            {
                idx = newVerts.Count;
                newVerts.Add(v);
                dict[key] = idx;
            }
            remap[i] = idx;
        }

        var newTris = new int[oldTris.Length];
        for (int i = 0; i < oldTris.Length; i++)
            newTris[i] = remap[oldTris[i]];

        mesh.Clear();
        mesh.vertices = newVerts.ToArray();
        mesh.triangles = newTris;
        return mesh;
    }

    // Force flat shading normals for physics mesh
    private Mesh FlatShadePhysicsMesh(Mesh mesh)
    {
        var oldVerts = mesh.vertices;
        var oldTris = mesh.triangles;

        // Each triangle gets its own unique vertices
        var newVerts = new List<Vector3>();
        var newNormals = new List<Vector3>();
        var newTris = new List<int>();

        for (int i = 0; i < oldTris.Length; i += 3)
        {
            int i0 = oldTris[i];
            int i1 = oldTris[i + 1];
            int i2 = oldTris[i + 2];

            Vector3 v0 = oldVerts[i0];
            Vector3 v1 = oldVerts[i1];
            Vector3 v2 = oldVerts[i2];

            // Compute one normal for the whole face
            Vector3 faceNormal = Vector3.Cross(v1 - v0, v2 - v0).normalized;

            int baseIndex = newVerts.Count;
            newVerts.Add(v0); newNormals.Add(faceNormal);
            newVerts.Add(v1); newNormals.Add(faceNormal);
            newVerts.Add(v2); newNormals.Add(faceNormal);

            newTris.Add(baseIndex);
            newTris.Add(baseIndex + 1);
            newTris.Add(baseIndex + 2);
        }

        mesh.Clear();
        mesh.vertices = newVerts.ToArray();
        mesh.normals = newNormals.ToArray();
        mesh.triangles = newTris.ToArray();
        mesh.RecalculateBounds();
        return mesh;
    }


    private string ResolveTexturePath(string texture)
	{
		var basePath = Path.GetDirectoryName(filePath);
		while (!string.IsNullOrEmpty(basePath))
		{
			var assetPath = Path.Combine(Application.streamingAssetsPath, basePath);
			var possibleTextures = new List<string>
			{
				Path.Combine(assetPath, texture + ".png"),
				Path.Combine(assetPath, texture + ".jpg"),
				Path.Combine(assetPath, texture + ".jp2"),
				Path.Combine(assetPath, texture + ".bmp"),
				Path.Combine(assetPath, texture + ".bm8"),
				Path.Combine(assetPath, texture + ".gif"),
				Path.Combine(assetPath, texture + ".dds"),
			};
			foreach (var possibleTexture in possibleTextures)
			{
				if (File.Exists(possibleTexture))
				{
					return possibleTexture;
				}
			}

			basePath = Path.GetDirectoryName(basePath);
		}

		return texture;
	}
}
