using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Reads the team's animated cube mesh: the support point and ink edges follow A/B deformation.
[DefaultExecutionOrder(50)]
public sealed class PlatformSurface : MonoBehaviour
{
    private struct Edge
    {
        public int a, b;
        public Vector3 normal;
        public bool crease;
        public int faces;
    }

    private SkinnedMeshRenderer skin;
    private Renderer source;
    private NeckerCubeGFXController cubeAnimation;
    private Mesh baked;
    private Mesh outline;
    private Transform beacon;
    private Transform switchPad;
    private float sampledBlend = -1;
    private MaterialPropertyBlock tint;
    private Color baseTint = Color.white;
    private bool hovered;
    private readonly Dictionary<Vector3Int, int> welded = new Dictionary<Vector3Int, int>();
    private readonly Dictionary<(int, int), Edge> edges = new Dictionary<(int, int), Edge>();
    private readonly List<Vector3> points = new List<Vector3>();
    private readonly List<int> lines = new List<int>();
    private readonly WalkableFace face = new WalkableFace();
    private readonly List<Vector3> faceVertices = new List<Vector3>();

    public Pose Support { get; private set; }
    public bool IsChanging => cubeAnimation != null && cubeAnimation.IsTransitioning;
    public float ModeBlend => cubeAnimation != null ? cubeAnimation.ModeBlend : 0;
    public Vector3 LabelAnchor => source != null
        ? new Vector3(source.bounds.center.x, source.bounds.min.y - 0.12f, source.bounds.center.z) : Support.position;
    public Vector3 DockToward(Vector3 target, Vector3 side) => face.Dock(target, WalkwayPath.Width, side);

    public void Initialize(SceneMaterials materials)
    {
        tint = new MaterialPropertyBlock();
        skin = GetComponentInChildren<SkinnedMeshRenderer>(true);
        source = skin != null ? (Renderer)skin : GetComponentInChildren<MeshRenderer>();
        cubeAnimation = GetComponentInChildren<NeckerCubeGFXController>(true);
        if (skin != null) skin.updateWhenOffscreen = true;
        if (source != null) { source.sharedMaterial = materials.Solid; source.gameObject.layer = 0; }
        foreach (HoverOutline hover in GetComponentsInChildren<HoverOutline>(true)) hover.UseSurfaceFeedback(SetHovered);
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
            if (child.name == "Outline") child.gameObject.SetActive(false);

        baked = new Mesh { name = "Cube support sample" };
        outline = new Mesh { name = "Cube ink edges" };
        var drawing = new GameObject("Ink edges", typeof(MeshFilter), typeof(MeshRenderer));
        SceneManager.MoveGameObjectToScene(drawing, gameObject.scene);
        drawing.transform.SetParent(transform, false);
        drawing.GetComponent<MeshFilter>().sharedMesh = outline;
        drawing.GetComponent<MeshRenderer>().sharedMaterial = materials.Ink;
        beacon = SceneMaterials.Part("Goal marker", PrimitiveType.Cylinder, transform,
            Vector3.zero, new Vector3(0.28f, 0.012f, 0.28f), materials.Goal).transform;
        beacon.gameObject.SetActive(false);
        switchPad = SceneMaterials.Part("A-B switch pad", PrimitiveType.Cylinder, transform,
            Vector3.zero, new Vector3(0.44f,0.009f,0.44f), materials.Accent).transform;
        switchPad.gameObject.SetActive(false);
        RefreshPose();
    }

    private void LateUpdate()
    {
        if (cubeAnimation != null && (IsChanging || sampledBlend != cubeAnimation.ModeBlend)) RefreshPose();
    }

    public void SetTint(Color color, bool goal)
    {
        baseTint = color;
        ApplyTint();
        beacon.gameObject.SetActive(goal);
    }

    private void SetHovered(bool active) { hovered = active; ApplyTint(); }
    private void ApplyTint()
    {
        if (source != null)
        {
            tint.SetColor("_BaseColor", hovered ? Color.Lerp(baseTint, PresentationSettings.Load().accent, 0.3f) : baseTint);
            source.SetPropertyBlock(tint);
        }
    }

    public void SetSwitchPad(bool active) => switchPad.gameObject.SetActive(active);

    public void RefreshPose()
    {
        if (source == null) return;
        if (cubeAnimation != null) sampledBlend = cubeAnimation.ModeBlend;
        if (skin == null)
        {
            Bounds bounds = source.bounds;
            Support = new Pose(new Vector3(bounds.center.x, bounds.max.y + 0.02f, bounds.center.z), Quaternion.identity);
            face.Set(new[] {
                new Vector3(bounds.min.x,bounds.max.y,bounds.min.z), new Vector3(bounds.max.x,bounds.max.y,bounds.min.z),
                new Vector3(bounds.max.x,bounds.max.y,bounds.max.z), new Vector3(bounds.min.x,bounds.max.y,bounds.max.z)
            }, Support.position, Vector3.up);
            beacon.SetPositionAndRotation(Support.position, Support.rotation);
            switchPad.SetPositionAndRotation(Support.position, Support.rotation);
            return;
        }
        // TransformPoint below applies the FBX scale once, after the local-space bake.
        skin.BakeMesh(baked, false);
        Vector3[] vertices = baked.vertices;
        int[] triangles = baked.triangles;
        var world = new Vector3[vertices.Length];
        var map = new int[vertices.Length];
        welded.Clear(); edges.Clear(); points.Clear(); lines.Clear();
        for (int i = 0; i < vertices.Length; i++)
        {
            world[i] = skin.transform.TransformPoint(vertices[i]);
            Vector3 local = transform.InverseTransformPoint(world[i]);
            Vector3Int key = Vector3Int.RoundToInt(local * 10000);
            if (!welded.TryGetValue(key, out int index))
            {
                index = points.Count; welded.Add(key, index); points.Add(local);
            }
            map[i] = index;
        }
        Vector3 center = source.bounds.center, up = Vector3.up;
        float bestHeight = float.NegativeInfinity, totalArea = 0;
        Vector3 bestFace = center, bestNormal = up;
        Vector3 sum = Vector3.zero, sumNormal = Vector3.zero;
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int a = triangles[i], b = triangles[i + 1], c = triangles[i + 2];
            Vector3 cross = Vector3.Cross(world[b] - world[a], world[c] - world[a]);
            float area = cross.magnitude;
            if (area < 0.000001f) continue;
            Vector3 normal = cross / area;
            AddEdge(map[a], map[b], normal); AddEdge(map[b], map[c], normal); AddEdge(map[c], map[a], normal);
            Vector3 face = (world[a] + world[b] + world[c]) / 3;
            if (normal.y < 0.55f || face.y <= bestHeight) continue;
            bestHeight = face.y; bestFace = face; bestNormal = normal;
        }
        // Include the WHOLE coplanar face. Comparing triangle centroid heights clips
        // half of a sloping quadrilateral and makes bridges dock to an imaginary edge.
        faceVertices.Clear();
        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 a = world[triangles[i]], b = world[triangles[i + 1]], c = world[triangles[i + 2]];
            Vector3 cross = Vector3.Cross(b - a, c - a);
            float area = cross.magnitude;
            if (area < 0.000001f || Vector3.Dot(cross / area, bestNormal) < 0.995f
                || Mathf.Abs(Vector3.Dot(a - bestFace, bestNormal)) > 0.025f) continue;
            sum += (a + b + c) / 3 * area; sumNormal += cross; totalArea += area;
            faceVertices.Add(a); faceVertices.Add(b); faceVertices.Add(c);
        }
        if (totalArea > 0) { center = sum / totalArea; up = sumNormal.normalized; }
        else center.y = source.bounds.max.y;
        Vector3 forward = Vector3.ProjectOnPlane(Vector3.back, up).normalized;
        if (forward.sqrMagnitude < 0.01f) forward = Vector3.right;
        Support = new Pose(center + up * 0.02f, Quaternion.LookRotation(forward, up));
        face.Set(faceVertices, Support.position, up);
        beacon.SetPositionAndRotation(Support.position + up * 0.006f, Support.rotation);
        switchPad.SetPositionAndRotation(Support.position + up * 0.004f, Support.rotation);

        foreach (Edge edge in edges.Values)
            if (edge.faces == 1 || edge.crease) { lines.Add(edge.a); lines.Add(edge.b); }
        if (Camera.main != null)
        {
            Vector3 offset = transform.InverseTransformVector(-Camera.main.transform.forward * 0.003f);
            for (int i = 0; i < points.Count; i++) points[i] += offset;
        }
        outline.Clear(); outline.SetVertices(points); outline.SetIndices(lines, MeshTopology.Lines, 0, true, 0); outline.RecalculateBounds();
    }

    private void AddEdge(int a, int b, Vector3 normal)
    {
        var key = a < b ? (a, b) : (b, a);
        if (edges.TryGetValue(key, out Edge edge))
        {
            edge.faces++; edge.crease |= Vector3.Dot(edge.normal, normal) < 0.995f;
            edges[key] = edge;
        }
        else edges.Add(key, new Edge { a = a, b = b, normal = normal, faces = 1 });
    }

    private void OnDestroy() { Destroy(baked); Destroy(outline); }
}
