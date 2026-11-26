using System;
using System.Collections.Generic;
using UnityEngine;

// A convex walkable face sampled from the team's animated mesh. All coordinates are world space.
public sealed class WalkableFace
{
    private readonly List<Vector3> hull = new List<Vector3>();
    private Vector3 center, normal;

    public void Set(IReadOnlyList<Vector3> vertices, Vector3 center, Vector3 normal)
    {
        this.center = center; this.normal = normal;
        var sorted = new List<Vector3>(vertices);
        sorted.Sort((a,b) => a.x == b.x ? a.z.CompareTo(b.z) : a.x.CompareTo(b.x));
        for (int i = sorted.Count - 1; i > 0; i--)
            if ((sorted[i] - sorted[i - 1]).sqrMagnitude < 0.000001f) sorted.RemoveAt(i);
        hull.Clear();
        foreach (Vector3 point in sorted)
        {
            while (hull.Count >= 2 && Turn(hull[hull.Count - 2], hull[hull.Count - 1], point) <= 0) hull.RemoveAt(hull.Count - 1);
            hull.Add(point);
        }
        int lowerCount = hull.Count;
        for (int i = sorted.Count - 2; i >= 0; i--)
        {
            Vector3 point = sorted[i];
            while (hull.Count > lowerCount && Turn(hull[hull.Count - 2], hull[hull.Count - 1], point) <= 0) hull.RemoveAt(hull.Count - 1);
            hull.Add(point);
        }
        if (hull.Count > 1) hull.RemoveAt(hull.Count - 1);
    }

    public Vector3 Dock(Vector3 toward, float width, Vector3 side)
    {
        Vector3 direction = Vector3.ProjectOnPlane(toward - center, Vector3.up).normalized;
        if (hull.Count < 3 || direction.sqrMagnitude < 0.01f) return center;
        float distance = float.PositiveInfinity;
        // Both sides of the walkway must remain on the platform, including diamond-shaped faces.
        foreach (float offset in new[] { -width / 2, 0, width / 2 })
        {
            Vector3 origin = center + side * offset;
            for (int i = 0; i < hull.Count; i++)
            {
                Vector3 a = hull[i], edge = hull[(i + 1) % hull.Count] - a;
                float denominator = Cross(direction, edge);
                if (Mathf.Abs(denominator) < 0.000001f) continue;
                float ray = Cross(a - origin, edge) / denominator;
                float segment = Cross(a - origin, direction) / denominator;
                if (ray >= 0 && segment >= -0.0001f && segment <= 1.0001f) distance = Mathf.Min(distance, ray);
            }
        }
        if (float.IsInfinity(distance)) return center;
        // A small overlap hides cracks without spanning through the cube walls.
        Vector3 dock = center + direction * Mathf.Max(0, distance - 0.025f);
        if (Mathf.Abs(normal.y) > 0.01f)
            dock.y = center.y - (normal.x * (dock.x - center.x) + normal.z * (dock.z - center.z)) / normal.y;
        return dock;
    }

    private static float Cross(Vector3 a, Vector3 b) => a.x * b.z - a.z * b.x;
    private static float Turn(Vector3 a, Vector3 b, Vector3 c) => Cross(b - a, c - a);
}
