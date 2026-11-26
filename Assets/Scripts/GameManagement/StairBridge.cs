using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using OLE.Core;

public sealed class StairBridge : MonoBehaviour
{
    private Mesh surface, ink;
    private MeshRenderer surfaceRenderer, inkRenderer;
    private MeshCollider hit;
    private SceneMaterials materials;
    private WalkwayPath path;
    private bool open;
    private bool previousChanging;
    private float previousAlignment = -1;
    private Vector3 previousFirst, previousLast, previousDockFirst, previousDockLast, previousView;
    private readonly List<Vector3> vertices = new List<Vector3>();
    private readonly List<int> triangles = new List<int>();
    private readonly List<Vector3> edgePoints = new List<Vector3>();
    private readonly List<int> edgeIndices = new List<int>();
    public Connection Connection { get; private set; }
    public bool Perspective { get; private set; }

    public void Initialize(Connection connection, SceneMaterials palette, bool perspective = false)
    {
        Connection = connection; materials = palette; Perspective = perspective;
        surface = new Mesh { name = "Docked stair treads" }; ink = new Mesh { name = "Stair ink" };
        gameObject.AddComponent<MeshFilter>().sharedMesh = surface;
        surfaceRenderer = gameObject.AddComponent<MeshRenderer>();
        hit = gameObject.AddComponent<MeshCollider>();
        var outline = new GameObject("Edges", typeof(MeshFilter), typeof(MeshRenderer));
        SceneManager.MoveGameObjectToScene(outline, gameObject.scene);
        outline.transform.SetParent(transform, false);
        outline.GetComponent<MeshFilter>().sharedMesh = ink;
        inkRenderer = outline.GetComponent<MeshRenderer>();
    }

    public void Refresh(PlatformSurface first, PlatformSurface last, BoardMode mode)
    {
        Camera camera = Camera.main;
        Vector3 view = camera != null ? camera.transform.forward : new Vector3(0,-1,1).normalized;
        Vector3 right = camera != null ? camera.transform.right : Vector3.right;
        bool nextOpen = Connection.IsOpen(mode);
        bool changing = first.IsChanging || last.IsChanging;
        float alignment = Connection.IsOpen(BoardMode.A) && Connection.IsOpen(BoardMode.B) ? 1
            : Connection.IsOpen(BoardMode.B) ? first.ModeBlend : 1-first.ModeBlend;
        Vector3 from = first.Support.position, to = last.Support.position;
        Vector3 middle = (from + to) / 2;
        Vector3 side = Vector3.Cross(Vector3.up, to - from).normalized;
        Vector3 firstTarget = Perspective ? middle - view * (WalkwayPath.DepthSeparation / 2) : to;
        Vector3 lastTarget = Perspective ? middle + view * (WalkwayPath.DepthSeparation / 2) : from;
        Vector3 firstDock = first.DockToward(firstTarget, side), lastDock = last.DockToward(lastTarget, side);
        if (path != null && from == previousFirst && to == previousLast && firstDock == previousDockFirst
            && lastDock == previousDockLast && nextOpen == open && view == previousView && alignment == previousAlignment
            && changing == previousChanging) return;
        previousFirst = from; previousLast = to; previousDockFirst = firstDock; previousDockLast = lastDock;
        previousView = view; open = nextOpen;
        previousAlignment = alignment;
        previousChanging = changing;
        path = new WalkwayPath(from, to, firstDock, lastDock, view, right, Perspective, alignment, side);
        // All coordinates are world space; endpoints follow the animated cube faces.
        transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        vertices.Clear(); triangles.Clear(); edgePoints.Clear(); edgeIndices.Clear();
        BuildSpan(path.DockFirst, path.SeamFirst, path.Side, path.FirstSteps);
        if (Perspective) BuildSpan(path.SeamLast, path.DockLast, path.Side, path.LastSteps);
        surface.Clear(); surface.SetVertices(vertices); surface.SetTriangles(triangles, 0, true, 0);
        surface.RecalculateNormals(); surface.RecalculateBounds();
        ink.Clear(); ink.SetVertices(edgePoints); ink.SetIndices(edgeIndices, MeshTopology.Lines, 0, true, 0);
        ink.RecalculateBounds();
        surfaceRenderer.sharedMaterial = open ? materials.Solid : materials.Ghost;
        inkRenderer.sharedMaterial = open ? materials.Ink : materials.Muted;
        surfaceRenderer.enabled = open || Perspective;
        // Raycasts use the SAME mesh that is rendered, including the two separated
        // halves. A box covers empty space; disabling it made perspective stairs unclickable.
        hit.enabled = false;
        if (open && !changing)
        {
            hit.sharedMesh = null;
            hit.sharedMesh = surface;
            hit.enabled = true;
        }
    }

    public Pose Sample(string source, float position)
    {
        if (path == null) return new Pose(transform.position, Quaternion.identity);
        float t = source == Connection.First ? position : 1 - position;
        Vector3 point = path.Sample(t);
        float look = Mathf.Clamp01(t + (source == Connection.First ? 0.002f : -0.002f));
        Vector3 facing = Vector3.ProjectOnPlane(path.Sample(look) - point, Vector3.up);
        if (Perspective) facing = Vector3.ProjectOnPlane(source == Connection.First ? path.Last-path.First : path.First-path.Last, Vector3.up);
        if (facing.sqrMagnitude < 0.000001f)
            facing = Vector3.ProjectOnPlane(source == Connection.First ? path.Last - path.First : path.First - path.Last, Vector3.up);
        return new Pose(point, facing.sqrMagnitude > 0.000001f ? Quaternion.LookRotation(facing) : Quaternion.identity);
    }

    public bool IsSelectable => hit != null && hit.enabled;
    public Vector3 SelectionProbe => vertices.Count >= 19
        ? (vertices[16] + vertices[17] + vertices[18]) / 3 : transform.position;

    private void BuildSpan(Vector3 from, Vector3 to, Vector3 side, int steps)
    {
        if ((to - from).sqrMagnitude < 0.000001f) return;
        int count = steps + 1;
        float thickness = 0.1f + (steps > 0 ? Mathf.Abs(to.y - from.y) / steps : 0);
        Vector3 halfWidth = side * (WalkwayPath.Width / 2);
        for (int i = 0; i < count; i++)
        {
            float t0 = (float)i / count, t1 = (i + 1f) / count;
            Vector3 a = Vector3.Lerp(from, to, t0), b = Vector3.Lerp(from, to, t1);
            if (steps > 0) a.y = b.y = Mathf.Lerp(from.y, to.y, (float)i / steps);
            Box(a, b, halfWidth, thickness);
            Vector3 lift = Vector3.up * 0.003f;
            Line(a - halfWidth + lift, a + halfWidth + lift);
            Line(a - halfWidth + lift, b - halfWidth + lift);
            Line(a + halfWidth + lift, b + halfWidth + lift);
            if (i == count - 1) Line(b - halfWidth + lift, b + halfWidth + lift);
            else
            {
                Vector3 next = b + Vector3.up * ((to.y - from.y) / Mathf.Max(1, steps));
                Line(b - halfWidth, next - halfWidth); Line(b + halfWidth, next + halfWidth);
            }
        }
    }

    private void Box(Vector3 a, Vector3 b, Vector3 half, float thickness)
    {
        Vector3 down = Vector3.down * thickness;
        Vector3[] p = { a-half+down, a+half+down, a+half, a-half,
            b-half+down, b+half+down, b+half, b-half };
        bool reverse = Vector3.Dot(Vector3.Cross(half, b-a), Vector3.up) > 0;
        int[] faces = { 0,3,2,1, 5,6,7,4, 4,7,3,0, 1,2,6,5, 3,7,6,2, 4,0,1,5 };
        for (int f = 0; f < 6; f++)
        {
            int offset = vertices.Count;
            for (int k = 0; k < 4; k++) vertices.Add(p[faces[f * 4 + k]]);
            if (reverse) triangles.AddRange(new[] {offset,offset+2,offset+1,offset,offset+3,offset+2});
            else triangles.AddRange(new[] {offset,offset+1,offset+2,offset,offset+2,offset+3});
        }
    }

    private void Line(Vector3 a, Vector3 b)
    {
        edgeIndices.Add(edgePoints.Count); edgePoints.Add(a);
        edgeIndices.Add(edgePoints.Count); edgePoints.Add(b);
    }

    private void OnDestroy() { Destroy(surface); Destroy(ink); }
}
