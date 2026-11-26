using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SceneMaterials
{
    public readonly Material Solid;
    public readonly Material Ink;
    public readonly Material Accent;
    public readonly Material Goal;
    public readonly Material Muted;
    public readonly Material Shadow;

    public SceneMaterials(PresentationSettings style, Material fallbackUnlit = null)
    {
        Solid = Make(style.solidMaterial, style.surface, false);
        Material unlit = style.unlitMaterial != null ? style.unlitMaterial : fallbackUnlit;
        Ink = Make(unlit, style.ink, true);
        Accent = Make(unlit, style.accent, true);
        Goal = Make(unlit, style.goal, true);
        Muted = Make(unlit, Color.Lerp(style.background, style.ink, 0.3f), true);
        Shadow = Make(unlit, new Color(0.65f, 0.69f, 0.74f), true);
    }

    private static Material Make(Material template, Color color, bool unlit)
    {
        var result = template != null ? new Material(template)
            : new Material(Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit"));
        result.SetColor("_BaseColor", color);
        if (result.HasProperty("_Smoothness")) result.SetFloat("_Smoothness", 0.1f);
        if (unlit) result.renderQueue = 2450;
        return result;
    }

    public void Destroy()
    {
        foreach (Material material in new[] { Solid, Ink, Accent, Goal, Muted, Shadow })
            Object.Destroy(material);
    }

    public static GameObject Part(string name, PrimitiveType shape, Transform parent,
        Vector3 position, Vector3 scale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(shape);
        SceneManager.MoveGameObjectToScene(part, parent.gameObject.scene);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        part.layer = 0;
        Collider collider = part.GetComponent<Collider>();
        collider.enabled = false;
        Object.Destroy(collider);
        part.GetComponent<Renderer>().sharedMaterial = material;
        return part;
    }
}
