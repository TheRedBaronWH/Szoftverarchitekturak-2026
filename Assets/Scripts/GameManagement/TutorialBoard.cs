using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using OLE.Core;

[DefaultExecutionOrder(75)]
public sealed class TutorialBoard : MonoBehaviour
{
    [Serializable] private struct Platform { public string id; public Transform cube; }
    [Serializable] private struct Layout
    {
        public string levelId;
        public Vector3[] positions;
        [Min(1f)] public float cameraSize;
        public Color platformColor, backgroundColor, connectionColor;
    }

    [SerializeField] private Platform[] platforms = Array.Empty<Platform>();
    [SerializeField] private Layout[] layouts = Array.Empty<Layout>();
    [SerializeField] private Material connectionMaterial;
    private readonly List<Platform> bank = new List<Platform>();
    private readonly Dictionary<string, PlatformSurface> surfaces = new Dictionary<string, PlatformSurface>();
    private readonly List<StairBridge> bridges = new List<StairBridge>();
    private GameObject template;
    private LevelDefinition level;
    private BoardMode mode;
    private float cameraSize;
    private float framedAspect = -1;
    private GUIStyle labelStyle;
    private string currentPlatform;
    public SceneMaterials Materials { get; private set; }
    public Vector3 Center { get; private set; }
    public bool LabelsVisible { get; set; } = true;
    public bool DebugVisible { get; set; }
    public bool IsChanging
    {
        get
        {
            if (level == null) return false;
            foreach (string id in level.Platforms) if (surfaces[id].IsChanging) return true;
            return false;
        }
    }

    private void Awake()
    {
        var style = PresentationSettings.Load();
        Materials = new SceneMaterials(style, connectionMaterial);
        bank.AddRange(platforms);
        if (bank.Count > 0 && bank[0].cube != null)
        {
            template = Instantiate(bank[0].cube.gameObject);
            SceneManager.MoveGameObjectToScene(template, gameObject.scene);
            template.name = "Platform template";
            template.SetActive(false);
        }
    }

    private void EnsurePlatforms(LevelDefinition definition)
    {
        foreach (string id in definition.Platforms)
        {
            Platform item = bank.Find(p => p.id == id);
            if (item.cube == null && template != null)
            {
                GameObject copy = Instantiate(template);
                SceneManager.MoveGameObjectToScene(copy, gameObject.scene);
                copy.name = "Platform " + id;
                copy.SetActive(true);
                item = new Platform { id = id, cube = copy.transform };
                bank.Add(item);
            }
            if (item.cube != null && !surfaces.ContainsKey(id))
            {
                PlatformSurface surface = item.cube.GetComponent<PlatformSurface>();
                if (surface == null) surface = item.cube.gameObject.AddComponent<PlatformSurface>();
                surface.Initialize(Materials);
                surfaces.Add(id, surface);
            }
        }
    }

    public bool ShowLevel(LevelDefinition definition)
    {
        if (level == definition) return true;
        Layout next = Array.Find(layouts, item => item.levelId == definition.Id);
        if (definition.Id == "paradox-stairs")
            next = new Layout { levelId = definition.Id, cameraSize = 7,
                positions = new[] {
                    new Vector3(-6,0,-3), new Vector3(-4,0,-3), new Vector3(-2,0.8f,-1.4f),
                    new Vector3(-4,1.7f,0.8f), new Vector3(0,2.4f,1.8f), new Vector3(0,1.6f,-1.4f),
                    new Vector3(-2,3.2f,2.6f), new Vector3(2,3.3f,3), new Vector3(0,4.3f,5),
                    new Vector3(5,4.5f,1.4f), new Vector3(4,4.1f,3), new Vector3(6.5f,5.3f,3.5f) } };
        if (definition.Id == "woven-labyrinth")
            next = new Layout { levelId = definition.Id, cameraSize = 8,
                positions = new[] {
                    new Vector3(-7,0,-4), new Vector3(-3,0,-4), new Vector3(-3,0,1),
                    new Vector3(-7,0,2), new Vector3(-7,0,-1), new Vector3(-5,0,-1),
                    new Vector3(4,2.4f,-3.5f), new Vector3(1,2.4f,2), new Vector3(5,2.4f,4.5f),
                    new Vector3(5,2.4f,1.5f), new Vector3(7.5f,2.4f,-1),
                    new Vector3(-5,4.8f,1.5f), new Vector3(2,4.8f,0.2f), new Vector3(-4.5f,4.8f,-2.5f),
                    new Vector3(2,4.8f,-5.5f), new Vector3(-4.5f,4.8f,-5.5f),
                    new Vector3(5.5f,7.2f,-1.8f), new Vector3(0,7.2f,-8),
                    new Vector3(-1,7.2f,-3.5f), new Vector3(-7,7.2f,-2.5f) } };
        if (next.positions == null || next.positions.Length != definition.Platforms.Count)
        {
            Debug.LogError("Missing layout for " + definition.Id, this);
            return false;
        }
        EnsurePlatforms(definition);
        foreach (string id in definition.Platforms)
            if (!surfaces.ContainsKey(id)) { Debug.LogError("Missing platform " + id, this); return false; }
        level = definition;
        foreach (Platform platform in bank) if (platform.cube != null) platform.cube.gameObject.SetActive(false);
        Vector3 center = Vector3.zero;
        for (int i = 0; i < definition.Platforms.Count; i++)
        {
            Platform platform = bank.Find(p => p.id == definition.Platforms[i]);
            platform.cube.position = next.positions[i];
            platform.cube.gameObject.SetActive(true);
            surfaces[platform.id].RefreshPose();
            surfaces[platform.id].SetSwitchPad(definition.HasSwitchPads && definition.CanToggleAt(platform.id));
            center += platform.cube.position;
        }
        center /= definition.Platforms.Count;
        Center = center;
        cameraSize = next.cameraSize;
        FrameCamera();
        foreach (StairBridge bridge in bridges) { bridge.gameObject.SetActive(false); Destroy(bridge.gameObject); }
        bridges.Clear();
        foreach (Connection edge in definition.Connections)
        {
            var root = new GameObject("Stairs " + edge.First + "-" + edge.Second);
            SceneManager.MoveGameObjectToScene(root, gameObject.scene);
            root.transform.SetParent(transform, false);
            var bridge = root.AddComponent<StairBridge>();
            bridge.Initialize(edge, Materials, IsPerspectiveEdge(definition.Id, edge));
            bridges.Add(bridge);
        }
        RefreshBridges();
        return true;
    }

    public IEnumerable<string> PlatformIds => level == null ? Array.Empty<string>() : level.Platforms;

    public bool TryGetPose(string id, out Pose pose)
    {
        if (id != null && surfaces.TryGetValue(id, out PlatformSurface surface))
        {
            pose = surface.Support;
            return true;
        }
        pose = default;
        return false;
    }

    public bool TryGetPosition(string id, out Vector3 position)
    {
        bool found = TryGetPose(id, out Pose pose);
        position = pose.position;
        return found;
    }

    public Pose MovePose(string source, string target, float position)
    {
        foreach (StairBridge bridge in bridges)
            if ((bridge.Connection.First == source && bridge.Connection.Second == target)
                || (bridge.Connection.Second == source && bridge.Connection.First == target))
                return bridge.Sample(source, position);
        TryGetPose(source, out Pose first); TryGetPose(target, out Pose second);
        return new Pose(Vector3.Lerp(first.position, second.position, position), first.rotation);
    }

    public string Identify(Collider hit, string current)
    {
        StairBridge bridge = hit.GetComponentInParent<StairBridge>();
        if (bridge != null)
            return bridge.Connection.First == current ? bridge.Connection.Second
                : bridge.Connection.Second == current ? bridge.Connection.First : null;
        foreach (Platform platform in bank)
            if (platform.cube != null && hit.transform.IsChildOf(platform.cube)) return platform.id;
        return null;
    }

    public string DirectionalTarget(string current, Vector2 direction)
    {
        if (level == null || Camera.main == null || !TryGetPosition(current, out Vector3 origin)) return null;
        Vector3 start = Camera.main.WorldToScreenPoint(origin);
        string result = null;
        float best = 0.35f;
        foreach (string id in level.Platforms)
        {
            if (!level.CanTraverse(current, id, mode) || !TryGetPosition(id, out Vector3 target)) continue;
            Vector2 delta = (Vector2)(Camera.main.WorldToScreenPoint(target) - start);
            float score = Vector2.Dot(delta.normalized, direction.normalized);
            if (score > best) { best = score; result = id; }
        }
        return result;
    }

    public void ShowMode(bool modeB, bool immediately)
    {
        mode = modeB ? BoardMode.B : BoardMode.A;
        foreach (string id in level.Platforms)
        {
            PlatformSurface surface = surfaces[id];
            surface.GetComponentInChildren<NeckerCubeGFXController>().ShowMode(modeB, immediately);
            surface.RefreshPose();
        }
        RefreshBridges();
    }

    public void HighlightPlatforms(string current, BoardMode state)
    {
        currentPlatform = current;
        var style = PresentationSettings.Load();
        foreach (string id in level.Platforms)
        {
            Color color = id == current ? Color.white
                : id == level.Goal ? Color.Lerp(style.surface, style.goal, 0.5f)
                : level.CanTraverse(current, id, state) ? Color.Lerp(style.surface, style.accent, 0.25f)
                : style.surface;
            surfaces[id].SetTint(color, id == level.Goal);
        }
    }

    public void HighlightHint(string target)
    {
        if (surfaces.TryGetValue(target, out PlatformSurface surface))
            surface.SetTint(PresentationSettings.Load().accent, target == level.Goal);
    }

    public void EnsureCameraFrame()
    {
        if (Camera.main != null && Mathf.Abs(Camera.main.aspect - framedAspect) > 0.01f) FrameCamera();
    }

    private void FrameCamera()
    {
        Camera camera = Camera.main;
        if (camera == null || level == null) return;
        Vector3 up = new Vector3(0,1,1).normalized;
        Vector3 center = Vector3.zero;
        float min = float.PositiveInfinity, max = float.NegativeInfinity;
        float left = float.PositiveInfinity, right = float.NegativeInfinity;
        foreach (string id in level.Platforms)
        {
            Vector3 point = bank.Find(p => p.id == id).cube.position;
            center += point;
            min = Mathf.Min(min, Vector3.Dot(point,up)-0.8f);
            max = Mathf.Max(max, Vector3.Dot(point,up)+1.2f);
            left = Mathf.Min(left,point.x-0.8f); right = Mathf.Max(right,point.x+0.8f);
        }
        center /= level.Platforms.Count;
        center.x = (left + right) / 2;
        float aspect = Mathf.Max(0.3f,camera.aspect);
        float size = Mathf.Max(cameraSize, (max-min)/(2*0.73f), (right-left)/(2*aspect*0.92f));
        // Reserve the lower 23% of the viewport for the HUD; include the walker's head above each cube.
        float focusHeight = (min+max)/2 - size*0.19f;
        Center = center + up*(focusHeight-Vector3.Dot(center,up));
        camera.transform.position = Center + new Vector3(0,12,-12);
        camera.transform.LookAt(Center);
        camera.orthographic = true; camera.orthographicSize = size;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = PresentationSettings.Load().background;
        framedAspect = camera.aspect;
    }

    private void LateUpdate() => RefreshBridges();
    private void RefreshBridges()
    {
        foreach (StairBridge bridge in bridges)
            if (surfaces.TryGetValue(bridge.Connection.First, out PlatformSurface first)
                && surfaces.TryGetValue(bridge.Connection.Second, out PlatformSurface second))
                bridge.Refresh(first, second, mode);
    }

    private static bool IsPerspectiveEdge(string levelId, Connection edge)
    {
        if (!PresentationSettings.Load().perspectiveSeams) return false;
        string pair = edge.First + "/" + edge.Second;
        return levelId == "tutorial" && pair == "P3/P4"
            || levelId == "crossroads" && pair == "P5/P6"
            || levelId == "skybridge" && pair == "P4/P6"
            || levelId == "paradox-stairs" && pair == "P10/P12"
            || levelId == "woven-labyrinth" && (pair == "P3/P7" || pair == "P8/P12" || pair == "P13/P17" || pair == "P19/P20");
    }

    private void OnDestroy()
    {
        Materials?.Destroy();
        if (template != null) Destroy(template);
    }

    private void OnGUI()
    {
        if (!LabelsVisible || level == null || Camera.main == null) return;
        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.box);
            labelStyle.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelStyle.alignment = TextAnchor.MiddleCenter;
            labelStyle.fontStyle = FontStyle.Bold;
        }
        float scale = Mathf.Clamp(Screen.height / 720f, 0.9f, 1.6f);
        labelStyle.fontSize = Mathf.RoundToInt(17 * scale);
        foreach (string id in level.Platforms)
        {
            if (!surfaces.TryGetValue(id, out PlatformSurface surface)) continue;
            Vector3 screen = Camera.main.WorldToScreenPoint(surface.LabelAnchor);
            string caption = PlatformShortcuts.DisplayCaption(id);
            bool pad = level.HasSwitchPads && level.CanToggleAt(id);
            if (pad) caption += " · A/B";
            if (DebugVisible) caption += " · " + id;
            Color previous = GUI.backgroundColor;
            GUI.backgroundColor = id == currentPlatform ? new Color(0.1f,0.8f,0.68f)
                : id == level.Goal ? new Color(1,0.72f,0.2f) : Color.white;
            float width = (DebugVisible ? 130 : pad ? 90 : 36) * scale;
            if (screen.z > 0) GUI.Box(new Rect(screen.x-width/2, Screen.height-screen.y-30*scale, width, 28*scale), caption, labelStyle);
            GUI.backgroundColor = previous;
        }
    }
}
