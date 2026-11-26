using UnityEngine;

public class HoverOutline : MonoBehaviour
{
    private System.Action<bool> surfaceFeedback;
    [SerializeField]
    private GameObject outline;

    // The gameplay board supplies a depth-aware tint. Other scenes keep the
    // original outline/Foreground behavior without changing their prefab bindings.
    public void UseSurfaceFeedback(System.Action<bool> feedback)
    {
        surfaceFeedback = feedback;
        if (outline != null) outline.SetActive(false);
    }

    private void OnMouseEnter()
    {
        if (surfaceFeedback != null) { surfaceFeedback(true); return; }
        outline.SetActive(true);
        gameObject.layer = LayerMask.NameToLayer("Foreground");
    }

    private void OnMouseExit()
    {
        if (surfaceFeedback != null) { surfaceFeedback(false); return; }
        outline.SetActive(false);
        gameObject.layer = LayerMask.NameToLayer("Default");
    }
}
