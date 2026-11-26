using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class AvatarVisual : MonoBehaviour
{
    private Transform drawing;
    private Transform body;
    private Transform leftHip, rightHip, leftArm, rightArm;
    private ParticleSystem[] originalEffects;
    private float phase;
    private bool visible;

    public void Initialize(SceneMaterials palette)
    {
        if (drawing != null) { drawing.gameObject.SetActive(false); Destroy(drawing.gameObject); }
        // Keep the team's sphere mesh, emissive material and particle animation. Only the
        // sphere's role changes: it becomes the head of the walker at runtime.
        MeshFilter originalHead = null;
        Material headMaterial = palette.Accent;
        foreach (MeshFilter filter in GetComponentsInChildren<MeshFilter>(true))
            if (drawing == null || !filter.transform.IsChildOf(drawing))
            {
                originalHead = filter;
                var renderer = filter.GetComponent<Renderer>();
                if (renderer != null) { headMaterial = renderer.sharedMaterial; renderer.enabled = false; }
                break;
            }
        foreach (Collider collider in GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        originalEffects = GetComponentsInChildren<ParticleSystem>(true);
        foreach (ParticleSystem particles in originalEffects)
        {
            particles.transform.localPosition = new Vector3(0, 0.56f, 0);
            particles.gameObject.layer = 0; // Ordinary depth: the glow cannot shine through stairs.
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            if (renderer != null) renderer.enabled = true;
        }
        var root = new GameObject("Walker");
        SceneManager.MoveGameObjectToScene(root, gameObject.scene);
        root.transform.SetParent(transform, false);
        drawing = root.transform;
        GameObject shadow = SceneMaterials.Part("Contact shadow", PrimitiveType.Cylinder, drawing,
            new Vector3(0, 0.006f, 0), new Vector3(0.27f, 0.003f, 0.2f), palette.Shadow);
        shadow.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var figure = new GameObject("Figure");
        SceneManager.MoveGameObjectToScene(figure, gameObject.scene);
        figure.transform.SetParent(drawing, false);
        body = figure.transform;
        GameObject head = SceneMaterials.Part("Head · original player material", PrimitiveType.Sphere, body,
            new Vector3(0,0.56f,0), Vector3.one * 0.17f, headMaterial);
        if (originalHead != null && originalHead.sharedMesh != null)
        {
            Mesh mesh = originalHead.sharedMesh;
            head.GetComponent<MeshFilter>().sharedMesh = mesh;
            Vector3 size = mesh.bounds.size;
            head.transform.localScale = Vector3.one * (0.17f / Mathf.Max(size.x, size.y, size.z));
        }
        SceneMaterials.Part("Collar", PrimitiveType.Cylinder, body, new Vector3(0,0.475f,0), new Vector3(0.11f,0.018f,0.11f), palette.Ink);
        SceneMaterials.Part("Torso", PrimitiveType.Capsule, body, new Vector3(0,0.35f,0), new Vector3(0.14f,0.135f,0.11f), palette.Ink);
        SceneMaterials.Part("Chest", PrimitiveType.Cube, body, new Vector3(0,0.37f,0.055f), new Vector3(0.08f,0.085f,0.012f), palette.Accent);
        leftHip = Hip("Left leg", -0.048f, palette);
        rightHip = Hip("Right leg", 0.048f, palette);
        leftArm = SceneMaterials.Part("Left arm", PrimitiveType.Capsule, body,
            new Vector3(-0.105f,0.34f,0), new Vector3(0.045f,0.1f,0.045f), palette.Ink).transform;
        rightArm = SceneMaterials.Part("Right arm", PrimitiveType.Capsule, body,
            new Vector3(0.105f,0.34f,0), new Vector3(0.045f,0.1f,0.045f), palette.Ink).transform;
        phase = 0;
        visible = true;
        foreach (ParticleSystem effect in originalEffects) if (!effect.isPlaying) effect.Play(false);
    }

    private Transform Hip(string name, float x, SceneMaterials palette)
    {
        var pivot = new GameObject(name);
        SceneManager.MoveGameObjectToScene(pivot, gameObject.scene);
        pivot.transform.SetParent(body, false);
        pivot.transform.localPosition = new Vector3(x, 0.23f, 0);
        SceneMaterials.Part("Leg", PrimitiveType.Capsule, pivot.transform,
            new Vector3(0,-0.1f,0), new Vector3(0.05f,0.1f,0.05f), palette.Ink);
        SceneMaterials.Part("Foot", PrimitiveType.Cube, pivot.transform,
            new Vector3(0,-0.205f,0.02f), new Vector3(0.075f,0.045f,0.12f), palette.Ink);
        return pivot.transform;
    }

    public void SetVisible(bool visible)
    {
        if (this.visible == visible) return;
        this.visible = visible;
        if (drawing != null) drawing.gameObject.SetActive(visible);
        if (originalEffects == null) return;
        foreach (ParticleSystem effect in originalEffects)
            if (visible) effect.Play(false);
            else effect.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    public void SetFootPose(Pose pose, bool immediately = false)
    {
        transform.position = pose.position;
        transform.rotation = immediately ? pose.rotation
            : Quaternion.Slerp(transform.rotation, pose.rotation, Time.deltaTime * 12);
    }

    public void Animate(bool moving)
    {
        if (body == null) return;
        if (moving) phase += Time.deltaTime * 9;
        else phase = 0;
        float swing = Mathf.Sin(phase) * 25;
        leftHip.localRotation = Quaternion.Euler(swing,0,0);
        rightHip.localRotation = Quaternion.Euler(-swing,0,0);
        leftArm.localRotation = Quaternion.Euler(-swing*0.6f,0,0);
        rightArm.localRotation = Quaternion.Euler(swing*0.6f,0,0);
        body.localPosition = new Vector3(0, moving ? Mathf.Abs(Mathf.Sin(phase)) * 0.01f : 0, 0);
    }
}
