using System;
using System.Collections.Generic;
using UnityEngine;
using OLE.Core;

public sealed class TutorialBoard : MonoBehaviour
{
    [Serializable]
    private struct Platform
    {
        public string id;
        public Transform cube;
    }

    [SerializeField] private Platform[] platforms;
    [SerializeField] private Vector3 standingOffset = Vector3.up;
    private LevelDefinition level;

    public void ShowLevel(LevelDefinition definition)
    {
        level = definition;
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
    }

    private void OnGUI()
    {
        Camera camera = Camera.main;
        if (level == null || camera == null)
            return;

        foreach (string id in level.Platforms)
        {
            if (!TryGetPosition(id, out Vector3 position))
                continue;

            Vector3 screen = camera.WorldToScreenPoint(position + Vector3.up * 0.35f);
            if (screen.z <= 0f)
                continue;

            string label = id == level.Goal ? id + " (cél)" : id;
            GUI.Box(new Rect(screen.x - 35, Screen.height - screen.y - 24, 70, 24), label);
        }
    }
}
