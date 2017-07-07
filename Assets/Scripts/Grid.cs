// -----------------------------------------------------------------------------
// OgmaDrive
// Copyright (c) 2017 Ogma Intelligent Systems Corp. All rights reserved.
// -----------------------------------------------------------------------------

using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class Grid : MonoBehaviour
{

    public int uSize, vSize;
    public int xStep, zStep;

    private Mesh mesh;
    private Vector3[] vertices;

    private Grid()
    {
        uSize = vSize = 100;
        xStep = zStep = 1;
    }

    private void Awake()
    {
        Generate();
    }

    private void Generate()
    {
        GetComponent<MeshFilter>().mesh = mesh = new Mesh();
        mesh.name = "Procedural Grid";

        vertices = new Vector3[((uSize / xStep) + 1) * ((vSize / zStep) + 1)];
        Vector2[] uv = new Vector2[vertices.Length];
        Vector4[] tangents = new Vector4[vertices.Length];
        Vector4 tangent = new Vector4(1f, 0f, 0f, -1f);

        for (int i = 0, z = 0; z <= vSize; z += zStep)
        {
            for (int x = 0; x <= uSize; x += xStep, i++)
            {
                vertices[i] = new Vector3(x, 0, z);
                uv[i] = new Vector2((float)x / uSize, (float)z / vSize);
                tangents[i] = tangent;
            }
        }
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.tangents = tangents;

        int[] triangles = new int[(uSize / xStep) * (vSize / zStep) * 6];
        for (int ti = 0, vi = 0, z = 0; z < vSize; z += zStep, vi++)
        {
            for (int x = 0; x < uSize; x += xStep, ti += 6, vi++)
            {
                triangles[ti] = vi;
                triangles[ti + 3] = triangles[ti + 2] = vi + 1;
                triangles[ti + 4] = triangles[ti + 1] = vi + uSize + 1;
                triangles[ti + 5] = vi + uSize + 2;
            }
        }
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        GetComponent<MeshCollider>().sharedMesh = mesh;
    }
}
