using UnityEngine;
using OLE.Core;

// Geometry and avatar share these endpoints and stair heights. A perspective seam
// has two different 3D positions but one position in the orthographic camera image.
public sealed class WalkwayPath
{
    public const float Width = 0.4f;
    public const float DepthSeparation = 1.25f;
    public readonly Vector3 First, Last, DockFirst, DockLast, SeamFirst, SeamLast, Side;
    public readonly bool Perspective;
    public readonly int FirstSteps, LastSteps;
    private readonly float entry, firstRun, lastRun, exit, length;

    public WalkwayPath(Vector3 first, Vector3 last, Vector3 dockFirst, Vector3 dockLast,
        Vector3 viewForward, Vector3 viewRight, bool perspective, float alignment, Vector3? widthSide = null)
    {
        First = first; Last = last; DockFirst = dockFirst; DockLast = dockLast; Perspective = perspective;
        Vector3 middle = (dockFirst + dockLast) / 2;
        SeamFirst = perspective ? middle - viewForward * (DepthSeparation / 2) : dockLast;
        SeamLast = perspective ? middle + viewForward * (DepthSeparation / 2) : dockLast;
        if (perspective)
        {
            float gap = 0.22f * (1 - Mathf.Clamp01(alignment));
            SeamFirst -= viewRight * gap;
            SeamLast += viewRight * gap;
        }
        Side = widthSide ?? Vector3.Cross(Vector3.up, dockLast - dockFirst).normalized;
        FirstSteps = StepCount(DockFirst, SeamFirst);
        LastSteps = perspective ? StepCount(SeamLast, DockLast) : 0;
        entry = Vector3.Distance(First, DockFirst);
        firstRun = Vector3.Distance(DockFirst, SeamFirst);
        lastRun = perspective ? Vector3.Distance(SeamLast, DockLast) : 0;
        exit = Vector3.Distance(DockLast, Last);
        length = entry + firstRun + lastRun + exit;
    }

    public Vector3 Sample(float position)
    {
        float distance = Mathf.Clamp01(position) * length;
        if (distance <= entry) return Vector3.Lerp(First, DockFirst, Ratio(distance, entry));
        distance -= entry;
        if (distance <= firstRun) return OnStairs(DockFirst, SeamFirst, Ratio(distance, firstRun), FirstSteps);
        distance -= firstRun;
        if (Perspective && distance <= lastRun) return OnStairs(SeamLast, DockLast, Ratio(distance, lastRun), LastSteps);
        distance -= lastRun;
        return Vector3.Lerp(DockLast, Last, Ratio(distance, exit));
    }

    public float SeamPosition => length > 0 ? (entry + firstRun) / length : 0.5f;

    public static int StepCount(Vector3 from, Vector3 to)
    {
        float run = Vector3.ProjectOnPlane(to - from, Vector3.up).magnitude;
        if (Mathf.Abs(to.y - from.y) < 0.035f) return 0;
        return Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(run / 0.3f, Mathf.Abs(to.y - from.y) / 0.14f)), 1, 64);
    }

    public static Vector3 OnStairs(Vector3 from, Vector3 to, float t, int steps)
    {
        Vector3 point = Vector3.Lerp(from, to, t);
        if (steps > 0)
        {
            // The last tread is a landing at exactly the destination height.
            float stairT = Mathf.Clamp01(t * (steps + 1f) / steps);
            point.y = Mathf.Lerp(from.y, to.y, (float)StairProfile.Height(stairT, steps));
        }
        return point;
    }

    private static float Ratio(float distance, float segment) => segment > 0.000001f ? Mathf.Clamp01(distance / segment) : 1;
}
