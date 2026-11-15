using System.Collections.Generic;
using UnityEngine;

public class CubeMesh : MonoBehaviour
{
    #region Static Variables

    private readonly Vector3[] verts =
    {
        new(-1, -1, -1), new(1, -1, -1), // 0, 1
        new(1, 1, -1),   new(-1, 1, -1), // 2, 3
        new(-1, -1, 1),  new(1, -1, 1),  // 4, 5
        new(1, 1, 1),    new(-1, 1, 1)   // 6, 7
    };

    private readonly int[][] faces =
    {
        new[] { 0, 1, 2, 3 }, // Front
        new[] { 5, 4, 7, 6 }, // Back
        new[] { 4, 0, 3, 7 }, // Left
        new[] { 1, 5, 6, 2 }, // Right
        new[] { 3, 2, 6, 7 }, // Top
        new[] { 4, 5, 1, 0 }  // Bottom
    };

    private readonly List<int> visibleEdges = new();

    #endregion

    [SerializeField] private Material lineMat;
    [SerializeField] private Vector3 position = new(-2.5f, 0f, 0f);
    [SerializeField] private float scale = 1f;
    [SerializeField] private float xRotation = 0f;
    [SerializeField] private float yRotation = 0f;
    [SerializeField] private Vector2 rotationSpeed = new(45f, 60f);

    [Range(1, 500)][SerializeField] private float focalLength = 20f;

    private Mesh mesh;
    private readonly List<Vector3> actualPosition = new();

    private void Awake()
    {
        mesh = new Mesh();
        gameObject.AddComponent<MeshFilter>().mesh = mesh;
        gameObject.AddComponent<MeshRenderer>().material = lineMat;
    }

    private void Update()
    {
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

        mesh.vertices = actualPosition.ToArray();
        visibleEdges.Clear();

        foreach (var f in faces)
        {
            if (!FacesCamera(actualPosition[f[0]], actualPosition[f[1]], actualPosition[f[2]])) continue;

            for (int i = 0; i < 4; i++)
            {
                visibleEdges.Add(f[i]);
                visibleEdges.Add(f[(i + 1) % 4]);
            }
        }

        mesh.SetIndices(visibleEdges, MeshTopology.Lines, 0);
        mesh.RecalculateBounds();
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