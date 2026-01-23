using UnityEngine;
using System;
using System.IO;
using System.Runtime.InteropServices;

[ExecuteInEditMode]
public class DifResource : IDisposable {

	#region Native Methods

	[DllImport("DifPlugin", EntryPoint = "dif_create")]
	public static extern IntPtr dif_create();

	[DllImport("DifPlugin", EntryPoint = "dif_free")]
	public static extern void dif_free(IntPtr dif);

	[DllImport("DifPlugin", EntryPoint = "dif_read")]
	public static extern void dif_read(IntPtr dif, string file, int interiorIndex);

	[DllImport("DifPlugin", EntryPoint = "dif_get_vertices")]
	public static extern void dif_get_vertices(IntPtr dif, [In, Out] float[] vertices);

	[DllImport("DifPlugin", EntryPoint = "dif_get_uvs")]
	public static extern void dif_get_uvs(IntPtr dif, [In, Out] float[] uvs);

	[DllImport("DifPlugin", EntryPoint = "dif_get_normals")]
	public static extern void dif_get_normals(IntPtr dif, [In, Out] float[] normals);

	[DllImport("DifPlugin", EntryPoint = "dif_get_tangents")]
	public static extern void dif_get_tangents(IntPtr dif, [In, Out] float[] tangents);

	[DllImport("DifPlugin", EntryPoint = "dif_get_materials")]
	public static extern void dif_get_materials(IntPtr dif, [In, Out] int[] materials);

	[DllImport("DifPlugin", EntryPoint = "dif_get_triangle_count_by_material")]
	public static extern int dif_get_triangle_count_by_material(IntPtr dif, int materialId);

	[DllImport("DifPlugin", EntryPoint = "dif_get_material_count")]
	public static extern int dif_get_material_count(IntPtr dif);

	[DllImport("DifPlugin", EntryPoint = "dif_get_total_triangle_count")]
	public static extern int dif_get_total_triangle_count(IntPtr dif);

	[DllImport("DifPlugin", EntryPoint = "dif_get_triangles_by_material")]
	public static extern void dif_get_triangles_by_material(IntPtr dif, int materialId, [In, Out] int[] indices);

	[DllImport("DifPlugin", EntryPoint = "dif_get_material_at")]
	public static extern IntPtr dif_get_material_at(IntPtr dif, int index);

	#endregion

	private IntPtr handle;
	private string filePath;
	private string fullPath;
	private int index;

	private Vector3[] verticesArray;
	private Vector2[] uvsArray;
	private Vector3[] normalsArray;
	private Vector4[] tangentsArray;
	private String[] materialsArray;
	private int[][] indicesArray;
	private int[] materialIndicesArray;

	#region Creation and Destruction

	public DifResource(string path, int interiorIndex) {
		index = interiorIndex;

		filePath = path;
		fullPath = Path.Combine(Application.dataPath, path);
		create(interiorIndex);
	}

	~DifResource() {
		Dispose(false);
	}

	public void Dispose() {
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	public void Dispose(bool freeManaged) {
		dif_free(handle);
		handle = IntPtr.Zero;
	}
	#endregion

	private void create(int interiorIndex) {
        if (File.Exists(fullPath) == false) {
            Debug.LogError("Dif file not found: \"" + fullPath + "\"");
            return;
        }

		handle = dif_create();

		// First, read the dif file
		dif_read(handle, fullPath, interiorIndex);

		// Grab vertices, UVs, normals, tangents, and material indices
		int triCount = dif_get_total_triangle_count(handle);
		int vertexCount = triCount * 9;
		int uvCount = triCount * 6;
		int normalCount = triCount * 3;
		int tangentCount = triCount * 3;
		float[] vertArray = new float[vertexCount];
		float[] uvArray = new float[uvCount];
		float[] normalArray = new float[normalCount];
		float[] tangentArray = new float[tangentCount];
		materialIndicesArray = new int[triCount];
		dif_get_vertices(handle, vertArray);
		dif_get_uvs(handle, uvArray);
		dif_get_normals(handle, normalArray);
		dif_get_tangents(handle, tangentArray);
		dif_get_materials(handle, materialIndicesArray);

		int i = 0;

		// Next, grab triangle indices
		int subMeshCount = dif_get_material_count(handle);
		indicesArray = new int[subMeshCount][];
		for (i = 0; i < subMeshCount; i++) {
			indicesArray[i] = new int[dif_get_triangle_count_by_material(handle, i)];
			dif_get_triangles_by_material(handle, i, indicesArray[i]);
		}

		// Next, load materials, and replace each material indice with one via script instead of engine.
		// it effecivly swaps values from what the c++ gives and what the script gives.
		materialsArray = new string[subMeshCount];
		for (i = 0; i < subMeshCount; i++) {
			IntPtr ptr = dif_get_material_at(handle, i);
			string temp = Marshal.PtrToStringAnsi(ptr).ToLower();

			// Remove any texture albums.
			if (temp.LastIndexOf("/") != -1)
				temp = temp.Substring(temp.LastIndexOf("/") + 1);

			materialsArray[i] = temp;
		}

		// Now convert each "material" into the proper physics material id
		for (i = 0; i < triCount; i++) {
			// Perform material swap here to get the PhysicsMaterial array.
//			var str = matArray[materialIndicesArray[i]];
//			materialIndicesArray[i] = PhysicsMaterialManager.getPhysicsMaterial(str).id;
		}

		// Finally, convert our verts, uvs, normals, and tangents into Unity types.
		toUnityTypes(vertArray, uvArray, normalArray, tangentArray);
	}

	private void toUnityTypes(float[] vertArray, float[] uvArray, float[] normalArray, float[] tangentArray)
	{
		int index = 0;
		verticesArray = new Vector3[vertArray.Length / 3];
		for (int i = 0; i < vertArray.Length / 3; i++) {
			verticesArray[i].x = vertArray[index];
			verticesArray[i].y = -vertArray[index + 1];
			verticesArray[i].z = vertArray[index + 2];
			index += 3;
		}

		index = 0;
		uvsArray = new Vector2[uvArray.Length / 2];
		for (int i = 0; i < uvArray.Length / 2; i++) {
			uvsArray[i].x = uvArray[index];
			uvsArray[i].y = -uvArray[index + 1];
			index += 2;
		}

		index = 0;
		normalsArray = new Vector3[vertArray.Length / 3];
		tangentsArray = new Vector4[vertArray.Length / 3];
		for (int i = 0; i < vertArray.Length / 3; i += 3) {
			normalsArray[i].x = normalArray[index];
			normalsArray[i].y = -normalArray[index + 1];
			normalsArray[i].z = normalArray[index + 2];
			tangentsArray[i].x = tangentArray[index];
			tangentsArray[i].y = -tangentArray[index + 1];
			tangentsArray[i].z = tangentArray[index + 2];
			tangentsArray[i].w = 1.0f;

			normalsArray[i + 1].x = normalArray[index];
			normalsArray[i + 1].y = -normalArray[index + 1];
			normalsArray[i + 1].z = normalArray[index + 2];
			tangentsArray[i + 1].x = tangentArray[index];
			tangentsArray[i + 1].y = -tangentArray[index + 1];
			tangentsArray[i + 1].z = tangentArray[index + 2];
			tangentsArray[i + 1].w = 1.0f;

			normalsArray[i + 2].x = normalArray[index];
			normalsArray[i + 2].y = -normalArray[index + 1];
			normalsArray[i + 2].z = normalArray[index + 2];
			tangentsArray[i + 2].x = tangentArray[index];
			tangentsArray[i + 2].y = -tangentArray[index + 1];
			tangentsArray[i + 2].z = tangentArray[index + 2];
			tangentsArray[i + 2].w = 1.0f;

			index += 3;
		}
	}

	#region Getters
	public int Index => index;
	public string file => filePath;
	public string fullFilePath => fullPath;
	public Vector3[] vertices => verticesArray;
	public Vector2[] uvs => uvsArray;
	public Vector3[] normals => normalsArray;
	public Vector4[] tangents => tangentsArray;
	public string[] materials => materialsArray;
	public int[][] triangleIndices => indicesArray;
	public int[] materialIndices => materialIndicesArray;
	#endregion
}
