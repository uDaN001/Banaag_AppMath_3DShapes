using System.Collections.Generic;
using UnityEngine;

public class SphereMesh : MonoBehaviour
{
    [Range(6, 32)][SerializeField] private int segments = 12;

    [SerializeField] private Material lineMat;
    [SerializeField] private Vector3 position = new(2.5f, 0f, 0f);
    [SerializeField] private float scale = 1f;
    [SerializeField] private float xRotation = 0f;
    [SerializeField] private float yRotation = 0f;
    [SerializeField] private Vector2 rotationSpeed = new(45f, 60f);

    [Range(1, 500)][SerializeField] private float focalLength = 20f;

    private Vector3[] verts;
    private int[][] faces;
    private readonly List<int> visibleEdges = new();
    private readonly List<Vector3> actualPosition = new();
    private Mesh mesh;
    private int lastSegments;

    private void Awake()
    {
        mesh = new Mesh();
        gameObject.AddComponent<MeshFilter>().mesh = mesh;
        gameObject.AddComponent<MeshRenderer>().material = lineMat;
        GenerateSphere(segments);
    }

    private void Update()
    {
        if (segments != lastSegments)
        {
            GenerateSphere(segments);
        }

        xRotation = (xRotation + rotationSpeed.x * Time.deltaTime) % 360f;
        yRotation = (yRotation + rotationSpeed.y * Time.deltaTime) % 360f;
    }

    private void LateUpdate()
    {
        actualPosition.Clear();
        foreach (var point in verts)
        {
            actualPosition.Add(Project(point));
        }

        mesh.Clear();
        mesh.vertices = actualPosition.ToArray();
        visibleEdges.Clear();

        foreach (var f in faces)
        {
            if (!FacesCamera(actualPosition[f[0]], actualPosition[f[1]], actualPosition[f[2]])) continue;

            int vertexCount = f.Length;
            for (int i = 0; i < vertexCount; i++)
            {
                visibleEdges.Add(f[i]);
                visibleEdges.Add(f[(i + 1) % vertexCount]);
            }
        }

        mesh.SetIndices(visibleEdges, MeshTopology.Lines, 0);
        mesh.RecalculateBounds();
    }

    private void GenerateSphere(int segCount)
    {
        lastSegments = segCount;
        int rings = segCount;
        int sectors = segCount;

        List<Vector3> vertList = new();
        List<int[]> faceList = new();

        // Top pole
        vertList.Add(new Vector3(0, 1, 0));

        // Latitude rings
        for (int r = 1; r < rings; r++)
        {
            float phi = Mathf.PI * r / rings;
            float y = Mathf.Cos(phi);
            float ringRadius = Mathf.Sin(phi);

            for (int s = 0; s < sectors; s++)
            {
                float theta = 2f * Mathf.PI * s / sectors;
                float x = ringRadius * Mathf.Cos(theta);
                float z = ringRadius * Mathf.Sin(theta);
                vertList.Add(new Vector3(x, y, z));
            }
        }

        // Bottom pole
        int bottomPoleIndex = vertList.Count;
        vertList.Add(new Vector3(0, -1, 0));

        // Top cap triangles
        for (int s = 0; s < sectors; s++)
        {
            int nextS = (s + 1) % sectors;
            faceList.Add(new[] { 0, 1 + nextS, 1 + s });
        }

        // Middle quads
        for (int r = 0; r < rings - 2; r++)
        {
            int ringStart = 1 + r * sectors;
            int nextRingStart = 1 + (r + 1) * sectors;

            for (int s = 0; s < sectors; s++)
            {
                int nextS = (s + 1) % sectors;
                faceList.Add(new[]
                {
                    ringStart + s,
                    ringStart + nextS,
                    nextRingStart + nextS,
                    nextRingStart + s
                });
            }
        }

        // Bottom cap triangles
        int lastRingStart = 1 + (rings - 2) * sectors;
        for (int s = 0; s < sectors; s++)
        {
            int nextS = (s + 1) % sectors;
            faceList.Add(new[] { bottomPoleIndex, lastRingStart + s, lastRingStart + nextS });
        }

        verts = vertList.ToArray();
        faces = faceList.ToArray();
    }

    private Vector2 Project(Vector3 point)
    {
        point *= 0.5f;

        // Rotate via X axis
        float radX = xRotation * Mathf.Deg2Rad;
        float yRot = point.y * Mathf.Cos(radX) - point.z * Mathf.Sin(radX);
        float zRotX = point.y * Mathf.Sin(radX) + point.z * Mathf.Cos(radX);
        point = new Vector3(point.x, yRot, zRotX);

        // Rotate via Y axis
        float radY = yRotation * Mathf.Deg2Rad;
        float xRot = point.x * Mathf.Cos(radY) - point.z * Mathf.Sin(radY);
        float zRotY = point.x * Mathf.Sin(radY) + point.z * Mathf.Cos(radY);
        point = new Vector3(xRot, point.y, zRotY);

        point += position;

        float p = focalLength / (focalLength + point.z);
        return new Vector2(point.x, point.y) * (scale * p);
    }

    private bool FacesCamera(Vector2 a, Vector2 b, Vector2 c)
    {
        return (b - a).x * (c - a).y - (b - a).y * (c - a).x > 0;
    }
}