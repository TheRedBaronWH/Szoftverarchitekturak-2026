using UnityEngine;

// Screen-relative movement: prefer the requested direction; for near-collinear
// branches choose the nearest landing. Platform keys can select every other branch.
public static class ScreenNavigation
{
    public static bool IsBetter(Vector2 delta, Vector2 direction, float best, float bestDistance, out float score)
    {
        score = Vector2.Dot(delta.normalized,direction.normalized);
        if (delta.sqrMagnitude < 0.01f || score <= 0.35f) return false;
        return score > best+0.025f || Mathf.Abs(score-best) <= 0.025f && delta.sqrMagnitude < bestDistance;
    }
}
