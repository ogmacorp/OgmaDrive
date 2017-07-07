// -----------------------------------------------------------------------------
// OgmaDrive
// Copyright (c) 2017 Ogma Intelligent Systems Corp. All rights reserved.
// -----------------------------------------------------------------------------

using UnityEngine;
using UnityEditor;
using System.Collections;

public class CreateTrackMesh : ScriptableWizard
{

    public float trackWidth = 10f;
    public float trackHeight = 0.1f;

    public uint sectionsPerCurve = 8;

    public BezierSpline spline;

    public bool addCollider = false;

    [MenuItem("GameObject/Create Other/Track Mesh")]
    static void CreateWizard()
    {
        ScriptableWizard.DisplayWizard("Create Track Mesh", typeof(CreateTrackMesh));
    }

    void OnWizardCreate()
    {
        GameObject newTrackMesh = new GameObject("TrackMesh");

        string meshName = newTrackMesh.name + spline.name;
        string meshPrefabPath = "Assets/Editor/" + meshName + ".asset";

        Mesh mesh = (Mesh)AssetDatabase.LoadAssetAtPath(meshPrefabPath, typeof(Mesh));
        if (mesh == null)
        {
            mesh = new Mesh();
            mesh.name = meshName;

            int run = 0;
            int n = spline.CurveCount * (int)sectionsPerCurve;

            Vector3[] points = new Vector3[n + 1];

            run = 0;
            for (float t = 0.0f; t <= 1.0f; t += 1.0f / (float)n)
            {
                points[run++] = spline.GetPoint(t);
            }

            Vector3[] vertices = new Vector3[18 + (n - 1) * 2];
            Vector2[] uvs = new Vector2[vertices.Length];

            float w = 0;
            float h = 0;
            float u = 0;

            run = 0;
            for (int s = 0; s < 2; s++)
            {
                switch (s)
                {
                    case 0:
                        w = -trackWidth / 2f;
                        h = trackHeight / 2f;
                        u = 0.0f;
                        break;

                    case 1:
                        w = trackWidth / 2f;
                        h = trackHeight / 2f;
                        u = 1.0f;
                        break;

                    default:
                        break;
                }

                Vector3 fwd = Vector3.forward, left = Vector3.left;

                for (int i = 0; i <= n; i++)
                {
                    if (i != n)
                    {
                        fwd = points[i + 1] - points[i];
                        fwd.y = 0;
                        fwd.Normalize();

                        left = Vector3.Cross(Vector3.up, fwd);
                    }

                    vertices[run] = points[i] + left * w + Vector3.up * h;

                    if ((i % 2) == 1)
                        uvs[run].Set(u, 1.0f);
                    else
                        uvs[run].Set(u, 0.0f);

                    run++;
                }
            }

            int[] triangles = new int[n * 2 * 3];

            run = 0;
            for (int s = 0; s < 1; s++)
            {
                for (int i = 0; i < n; i++)
                {
                    triangles[run + 0] = s * (n + 1) + i;
                    triangles[run + 1] = s * (n + 1) + i + 1;
                    triangles[run + 2] = ((s + 1) % 8) * (n + 1) + i;

                    triangles[run + 3] = s * (n + 1) + i + 1;
                    triangles[run + 4] = ((s + 1) % 8) * (n + 1) + i + 1;
                    triangles[run + 5] = ((s + 1) % 8) * (n + 1) + i;

                    run += 6;
                }
            }

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;

            mesh.RecalculateNormals();

            AssetDatabase.CreateAsset(mesh, meshPrefabPath);
            AssetDatabase.SaveAssets();
        }

        MeshFilter mf = newTrackMesh.AddComponent<MeshFilter>();
        mf.mesh = mesh;

        newTrackMesh.AddComponent<MeshRenderer>();

        if (addCollider)
        {
            MeshCollider mc = newTrackMesh.AddComponent<MeshCollider>();
            mc.sharedMesh = mf.sharedMesh;
        }

        Selection.activeObject = newTrackMesh;
    }
}