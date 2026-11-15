using System.Collections.Generic;
using UnityEngine;

public class PyramidMesh : MonoBehaviour
{
    #region Static Variables

    private readonly Vector3[] verts =
    {
        new(-1, -1, -1), // 0: Front-Left base
        new(1, -1, -1),  // 1: Front-Right base
        new(1, -1, 1),   // 2: Back-Right base
        new(-1, -1, 1),  // 3: Back-Left base
        new(0, 1, 0)     // 4: Apex
    };

    private readonly int[][] faces =
    {
        new[] { 0, 1, 4 },       // Front triangle
        new[] { 1, 2, 4 },       // Right triangle
        new[] { 2, 3, 4 },       // Back triangle
        new[] { 3, 0, 4 },       // Left triangle
        new[] { 3, 2, 1, 0 }     // Square base
    };

    private readonly List<int> visibleEdges = new();

    #endregion

    [SerializeField] private Material lineMat;
    [SerializeField] private Vector3 position = Vector3.zero;
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