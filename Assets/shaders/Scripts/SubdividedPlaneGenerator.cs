using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class SubdividedPlaneGenerator : MonoBehaviour
{
    [Header("Plane Size")]
    [SerializeField] private float width = 10f;
    [SerializeField] private float length = 10f;

    [Header("Subdivision")]
    [SerializeField, Range(1, 256)] private int subdivisions = 100;

    private Mesh generatedMesh;

    public float Width => width;
    public float Length => length;
    public int Subdivisions => subdivisions;

    private void OnEnable()
    {
        Rebuild();
    }

    private void OnValidate()
    {
        Rebuild();
    }

    [ContextMenu("Rebuild Plane")]
    public void Rebuild()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        MeshCollider meshCollider = GetComponent<MeshCollider>();

        if (meshFilter == null || meshCollider == null)
            return;

        width = Mathf.Max(0.01f, width);
        length = Mathf.Max(0.01f, length);
        subdivisions = Mathf.Clamp(subdivisions, 1, 256);

        EnsureMesh();

        int vertsX = subdivisions + 1;
        int vertsZ = subdivisions + 1;
        int vertexCount = vertsX * vertsZ;
        int indexCount = subdivisions * subdivisions * 6;

        generatedMesh.indexFormat = vertexCount > 65535
            ? UnityEngine.Rendering.IndexFormat.UInt32
            : UnityEngine.Rendering.IndexFormat.UInt16;

        Vector3[] vertices = new Vector3[vertexCount];
        Vector3[] normals = new Vector3[vertexCount];
        Vector2[] uvs = new Vector2[vertexCount];
        int[] triangles = new int[indexCount];

        float halfWidth = width * 0.5f;
        float halfLength = length * 0.5f;

        int vertexIndex = 0;
        for (int z = 0; z < vertsZ; z++)
        {
            float z01 = (float)z / subdivisions;
            float posZ = Mathf.Lerp(-halfLength, halfLength, z01);

            for (int x = 0; x < vertsX; x++)
            {
                float x01 = (float)x / subdivisions;
                float posX = Mathf.Lerp(-halfWidth, halfWidth, x01);

                vertices[vertexIndex] = new Vector3(posX, 0f, posZ);
                normals[vertexIndex] = Vector3.up;
                uvs[vertexIndex] = new Vector2(x01, z01);
                vertexIndex++;
            }
        }

        int triangleIndex = 0;
        for (int z = 0; z < subdivisions; z++)
        {
            for (int x = 0; x < subdivisions; x++)
            {
                int root = z * vertsX + x;
                int nextRow = root + vertsX;

                triangles[triangleIndex++] = root;
                triangles[triangleIndex++] = nextRow;
                triangles[triangleIndex++] = root + 1;

                triangles[triangleIndex++] = root + 1;
                triangles[triangleIndex++] = nextRow;
                triangles[triangleIndex++] = nextRow + 1;
            }
        }

        generatedMesh.Clear();
        generatedMesh.vertices = vertices;
        generatedMesh.normals = normals;
        generatedMesh.uv = uvs;
        generatedMesh.triangles = triangles;
        generatedMesh.RecalculateBounds();

        meshFilter.sharedMesh = generatedMesh;
        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = generatedMesh;
    }

    public void SetSubdivisions(int value)
    {
        subdivisions = Mathf.Clamp(value, 1, 256);
        Rebuild();
    }

    public void SetSize(float newWidth, float newLength)
    {
        width = Mathf.Max(0.01f, newWidth);
        length = Mathf.Max(0.01f, newLength);
        Rebuild();
    }

    private void EnsureMesh()
    {
        if (generatedMesh != null)
            return;

        generatedMesh = new Mesh
        {
            name = "Generated Subdivided Plane"
        };
    }
}
