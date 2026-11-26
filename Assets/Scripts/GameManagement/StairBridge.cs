using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using OLE.Core;

public sealed class StairBridge : MonoBehaviour
{
    private Mesh surface;
    private Mesh ink;
    private MeshRenderer surfaceRenderer;
    private MeshRenderer inkRenderer;
    private BoxCollider hit;
    private SceneMaterials materials;
    private Vector3 from, to;
    private int steps;
    private bool open;
    private readonly List<Vector3> vertices = new List<Vector3>();
    private readonly List<int> triangles = new List<int>();
    private readonly List<Vector3> edgePoints = new List<Vector3>();
    private readonly List<int> edgeIndices = new List<int>();
    public Connection Connection { get; private set; }

    public void Initialize(Connection connection, SceneMaterials palette)
    {
        Connection = connection; materials = palette;
        surface = new Mesh { name = "Stair treads" }; ink = new Mesh { name = "Stair ink" };
        gameObject.AddComponent<MeshFilter>().sharedMesh = surface;
        surfaceRenderer = gameObject.AddComponent<MeshRenderer>(); surfaceRenderer.sharedMaterial = materials.Solid;
        hit = gameObject.AddComponent<BoxCollider>();
        var outline = new GameObject("Edges", typeof(MeshFilter), typeof(MeshRenderer));
        SceneManager.MoveGameObjectToScene(outline, gameObject.scene);
        outline.transform.SetParent(transform, false);
        outline.GetComponent<MeshFilter>().sharedMesh = ink;
        inkRenderer = outline.GetComponent<MeshRenderer>();
    }

    public void Refresh(Vector3 first, Vector3 second, BoardMode mode)
    {
        bool nextOpen = Connection.IsOpen(mode);
        if (steps > 0 && first == from && second == to && nextOpen == open) return;
        from = first; to = second; open = nextOpen;
        Vector3 flat = Vector3.ProjectOnPlane(to - from, Vector3.up);
        float distance = flat.magnitude;
        if (distance < 0.8f) { gameObject.SetActive(false); return; }
        gameObject.SetActive(true);
        Vector3 direction = flat / distance;
        float length = distance - 0.68f, rise = to.y - from.y;
        steps = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(length / 0.3f, Mathf.Abs(rise) / 0.15f)), 1, 32);
        transform.SetPositionAndRotation(from + direction * 0.34f, Quaternion.LookRotation(direction));
        vertices.Clear(); triangles.Clear(); edgePoints.Clear(); edgeIndices.Clear();
        float run = length / steps;
        for (int i = 0; i < steps; i++)
        {
            float y = rise * i / steps, z = i * run;
            AddBox(new Vector3(0, y - 0.055f, z + run / 2), new Vector3(0.5f, 0.11f, run));
            if (open || i % 2 == 0)
            {
                Line(new Vector3(-0.25f, y + 0.004f, z), new Vector3(0.25f, y + 0.004f, z));
                Line(new Vector3(-0.25f, y, z), new Vector3(-0.25f, y, z + run));
                Line(new Vector3(0.25f, y, z), new Vector3(0.25f, y, z + run));
            }
        }
        surface.Clear(); surface.SetVertices(vertices); surface.SetTriangles(triangles, 0, true, 0); surface.RecalculateNormals(); surface.RecalculateBounds();
        ink.Clear(); ink.SetVertices(edgePoints); ink.SetIndices(edgeIndices, MeshTopology.Lines, 0, true, 0); ink.RecalculateBounds();
        surfaceRenderer.enabled = open; inkRenderer.sharedMaterial = open ? materials.Ink : materials.Muted;
        hit.enabled = open; hit.center = new Vector3(0, rise / 2, length / 2);
        hit.size = new Vector3(0.55f, Mathf.Abs(rise) + 0.25f, length);
    }

    public Pose Sample(string source, float position)
    {
        float t = source == Connection.First ? position : 1 - position;
        Vector3 point = Vector3.Lerp(from, to, t);
        float distance = Vector3.ProjectOnPlane(to - from, Vector3.up).magnitude;
        float entry = Mathf.Min(0.45f, 0.34f / Mathf.Max(0.01f, distance));
        float stairPosition = Mathf.InverseLerp(entry, 1 - entry, t);
        point.y = Mathf.Lerp(from.y, to.y, (float)StairProfile.Height(stairPosition, Mathf.Max(1, steps)));
        Vector3 facing = Vector3.ProjectOnPlane(source == Connection.First ? to - from : from - to, Vector3.up);
        return new Pose(point, facing.sqrMagnitude > 0.01f ? Quaternion.LookRotation(facing) : Quaternion.identity);
    }

    private void Line(Vector3 a, Vector3 b)
    {
        edgeIndices.Add(edgePoints.Count); edgePoints.Add(a);
        edgeIndices.Add(edgePoints.Count); edgePoints.Add(b);
    }

    private void AddBox(Vector3 center, Vector3 size)
    {
        Vector3 h = size / 2;
        Vector3[] p = {
            new Vector3(-h.x,-h.y,-h.z),new Vector3(h.x,-h.y,-h.z),new Vector3(h.x,h.y,-h.z),new Vector3(-h.x,h.y,-h.z),
            new Vector3(-h.x,-h.y,h.z),new Vector3(h.x,-h.y,h.z),new Vector3(h.x,h.y,h.z),new Vector3(-h.x,h.y,h.z) };
        int[] faces = { 0,3,2,1, 5,6,7,4, 4,7,3,0, 1,2,6,5, 3,7,6,2, 4,0,1,5 };
        for (int f = 0; f < 6; f++)
        {
            int offset = vertices.Count;
            for (int k = 0; k < 4; k++) vertices.Add(center + p[faces[f * 4 + k]]);
            triangles.AddRange(new[] { offset, offset+1, offset+2, offset, offset+2, offset+3 });
        }
    }

    private void OnDestroy() { Destroy(surface); Destroy(ink); }
}
