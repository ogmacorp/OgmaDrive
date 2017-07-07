// -----------------------------------------------------------------------------
// OgmaDrive
// Copyright (c) 2017 Ogma Intelligent Systems Corp. All rights reserved.
// -----------------------------------------------------------------------------

using UnityEngine;
using System.Collections;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class BarrierGenerator : MonoBehaviour
{
    public GameObject trackMesh;

    // Traack barrier sizes
    public float barrierHeight;
    public float barrierWidth;

    private BezierSpline spline;

    private float trackWidth;
    private float trackHeight;

    private uint sectionsPerCurve;

    private Vector3[] points;
    private Vector3[] vertices;
    private Vector2[] uvs;
    private int[] triangles;

    private Mesh mesh;

    private BarrierGenerator()
    {
        barrierHeight = 1.0f;
        barrierWidth = 0.1f;

        sectionsPerCurve = 8;
    }

    private void Awake() {
        // Need a track mesh and generator script to determine track size,
        // sections per bezier curve, and spline to follow
        if (trackMesh == null || trackMesh.GetComponent<TrackGenerator>() == null)
            return;

        TrackGenerator trackGenerator = trackMesh.GetComponent<TrackGenerator>();

        trackWidth = trackGenerator.trackWidth;
        trackHeight = trackGenerator.trackHeight;

        sectionsPerCurve = trackGenerator.sectionsPerCurve;

        spline = trackGenerator.spline;

        Generate();
    }

    private void Generate()
    {
        GetComponent<MeshFilter>().mesh = mesh = new Mesh();
        mesh.name = "Procedural Barrier";

        int run = 0;
        int n = spline.CurveCount * (int)sectionsPerCurve;

        points = new Vector3[n+1];

        run = 0;
        for (float t = 0.0f; t <= 1.0f; t += 1.0f / (float)n)
        {
            points[run++] = spline.GetPoint(t);
        }

        vertices = new Vector3[18 + (n - 1) * 8];
        uvs = new Vector2[vertices.Length];

        float w = 0;
        float h = 0;

        run = 0;
        for (int s = 0; s < 8; s++)
        {
            switch (s)
            {
                case 0:
                    w = -trackWidth / 2f;
                    h = trackHeight / 2f;
                    break;

                case 1:
                    w = trackWidth / 2f;
                    h = trackHeight / 2f;
                    break;

                case 2:
                    w = trackWidth / 2f;
                    h = trackHeight / 2f + barrierHeight;
                    break;

                case 3:
                    w = trackWidth / 2f + barrierWidth;
                    h = trackHeight / 2f + barrierHeight;
                    break;

                case 4:
                    w = trackWidth / 2f + barrierWidth;
                    h = -trackHeight / 2f;
                    break;

                case 5:
                    w = -trackWidth / 2f - barrierWidth;
                    h = -trackHeight / 2f;
                    break;

                case 6:
                    w = -trackWidth / 2f - barrierWidth;
                    h = trackHeight / 2f + barrierHeight;
                    break;

                case 7:
                    w = -trackWidth / 2f;
                    h = trackHeight / 2f + barrierHeight;
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
                //uvs[run].Set((vertices[run].y / barrierHeight) % 1024.0f, (vertices[run].z / barrierHeight) % 1024.0f);
                uvs[run].Set(vertices[run].y / barrierHeight, vertices[run].z / barrierHeight);

                run++;
            }
        }

        triangles = new int[n * 12 * 3];

        run = 0;
        for (int s = 1; s < 8; s++)
        {
            for (int i = 0; s != 4 && i < n; i++)
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

        GetComponent<MeshCollider>().sharedMesh = mesh;
    }
}