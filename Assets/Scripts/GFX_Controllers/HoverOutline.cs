using UnityEngine;

public class HoverOutline : MonoBehaviour
{
    [SerializeField]
    private GameObject outline;

    private void OnMouseEnter()
    {
        outline.SetActive(true);
        gameObject.layer = LayerMask.NameToLayer("Foreground");
    }

    private void OnMouseExit()
    {
        outline.SetActive(false);
        gameObject.layer = LayerMask.NameToLayer("Default");
    }
}
