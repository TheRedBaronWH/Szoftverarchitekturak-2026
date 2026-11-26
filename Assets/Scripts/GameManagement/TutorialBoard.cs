using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using OLE.Core;

public sealed class TutorialBoard : MonoBehaviour
{
    [Serializable]
    private struct Platform
    {
        public string id;
        public Transform cube;
    }

    [Serializable]
    private struct Layout
    {
        public string levelId;
        public Vector3[] positions;
        [Min(1f)] public float cameraSize;
        public Color platformColor;
        public Color backgroundColor;
        public Color connectionColor;
    }

    [SerializeField] private Platform[] platforms = Array.Empty<Platform>();
    [SerializeField] private Vector3 standingOffset = Vector3.up;
    [SerializeField] private Layout[] layouts = Array.Empty<Layout>();
    [SerializeField] private Material connectionMaterial;

    private LevelDefinition level;
    private Layout layout;
    private readonly List<LineRenderer> connections = new List<LineRenderer>();
    private Material openMaterial;
    private Material closedMaterial;
    private MaterialPropertyBlock colors;
    private GUIStyle labelStyle;

    private void Awake()
    {
        colors = new MaterialPropertyBlock();
        if (connectionMaterial != null)
        {
            openMaterial = new Material(connectionMaterial);
            closedMaterial = new Material(connectionMaterial);
        }
    }

    public bool ShowLevel(LevelDefinition definition)
    {
        if (level == definition)
            return true;

        Layout next = Array.Find(layouts, item => item.levelId == definition.Id);
        if (next.positions == null || next.positions.Length != definition.Platforms.Count || openMaterial == null)
        {
            Debug.LogError("Missing layout or connection material for " + definition.Id, this);
            return false;
        }
        foreach (string id in definition.Platforms)
        {
            if (Array.Find(platforms, item => item.id == id).cube == null)
            {
                Debug.LogError("No cube assigned to " + id, this);
                return false;
            }
        }

        level = definition;
        layout = next;
        // The scene contains the team's cube instances. Each level activates its own set.
        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
        {
            if (root.GetComponentInChildren<NeckerCubeGFXController>(true) != null)
                root.SetActive(false);
        }

        Vector3 center = Vector3.zero;
        for (int index = 0; index < definition.Platforms.Count; index++)
        {
            Transform cube = Array.Find(platforms, item => item.id == definition.Platforms[index]).cube;
            cube.position = layout.positions[index];
            cube.gameObject.SetActive(true);
            center += cube.position;
        }
        center /= definition.Platforms.Count;

        Camera camera = Camera.main;
        if (camera != null)
        {
            camera.transform.position = center + new Vector3(0, 10, -10);
            camera.transform.LookAt(center);
            camera.orthographic = true;
            camera.orthographicSize = layout.cameraSize;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = layout.backgroundColor;
        }

        openMaterial.SetColor("_BaseColor", layout.connectionColor);
        closedMaterial.SetColor("_BaseColor", Color.Lerp(layout.backgroundColor, Color.white, 0.2f));
        BuildConnections();
        return true;
    }

    public IEnumerable<string> PlatformIds
    {
        get
        {
            foreach (Platform platform in platforms)
                yield return platform.id;
        }
    }

    public bool TryGetPosition(string id, out Vector3 position)
    {
        foreach (Platform platform in platforms)
        {
            if (platform.id == id && platform.cube != null)
            {
                position = platform.cube.TransformPoint(standingOffset);
                return true;
            }
        }

        position = default;
        return false;
    }

    public string Identify(Collider hit)
    {
        foreach (Platform platform in platforms)
        {
            if (platform.cube != null && hit.transform.IsChildOf(platform.cube))
                return platform.id;
        }

        return null;
    }

    public void ShowMode(bool modeB, bool immediately)
    {
        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
        {
            foreach (var cube in root.GetComponentsInChildren<NeckerCubeGFXController>())
                cube.ShowMode(modeB, immediately);
        }

        BoardMode mode = modeB ? BoardMode.B : BoardMode.A;
        for (int index = 0; index < connections.Count; index++)
        {
            bool open = level.Connections[index].IsOpen(mode);
            connections[index].sharedMaterial = open ? openMaterial : closedMaterial;
            connections[index].widthMultiplier = open ? 0.075f : 0.025f;
        }
    }

    public void HighlightPlatforms(string current, BoardMode mode)
    {
        foreach (Platform platform in platforms)
        {
            if (platform.cube == null || !level.HasPlatform(platform.id))
                continue;

            Color tint = layout.platformColor;
            if (platform.id == current)
                tint = Color.white;
            else if (platform.id == level.Goal)
                tint = new Color(1f, 0.72f, 0.24f);
            else if (level.CanTraverse(current, platform.id, mode))
                tint = layout.connectionColor;

            foreach (Renderer renderer in platform.cube.GetComponentsInChildren<Renderer>())
            {
                renderer.GetPropertyBlock(colors);
                colors.SetColor("_BaseColor", tint);
                renderer.SetPropertyBlock(colors);
            }
        }
    }

    private void BuildConnections()
    {
        foreach (LineRenderer line in connections)
        {
            line.gameObject.SetActive(false);
            Destroy(line.gameObject);
        }
        connections.Clear();

        foreach (Connection edge in level.Connections)
        {
            TryGetPosition(edge.First, out Vector3 first);
            TryGetPosition(edge.Second, out Vector3 second);
            var link = new GameObject("Link " + edge.First + "-" + edge.Second);
            SceneManager.MoveGameObjectToScene(link, gameObject.scene);
            link.transform.SetParent(transform, false);
            var line = link.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.numCapVertices = 6;
            line.sharedMaterial = closedMaterial;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.SetPosition(0, first + Vector3.up * 0.15f);
            line.SetPosition(1, second + Vector3.up * 0.15f);
            connections.Add(line);
        }
    }

    private void OnDestroy()
    {
        Destroy(openMaterial);
        Destroy(closedMaterial);
    }

    private void OnGUI()
    {
        Camera camera = Camera.main;
        if (level == null || camera == null)
            return;

        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.box);
            labelStyle.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelStyle.fontSize = 14;
            labelStyle.alignment = TextAnchor.MiddleCenter;
        }

        foreach (string id in level.Platforms)
        {
            if (!TryGetPosition(id, out Vector3 position))
                continue;

            Vector3 screen = camera.WorldToScreenPoint(position + Vector3.up * 0.35f);
            if (screen.z <= 0f)
                continue;

            string label = id == level.Goal ? id + " (cél)" : id;
            GUI.Box(new Rect(screen.x - 38, Screen.height - screen.y - 24, 76, 24), label, labelStyle);
        }
    }
}
