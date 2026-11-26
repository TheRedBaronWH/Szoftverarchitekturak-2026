using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Shared visual elements for the pause and level-completion menus.
public static class MenuUI
{
    public static bool IsPointerOverUI(Vector2 position)
    {
        if (EventSystem.current == null) return false;
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = position }, hits);
        foreach (RaycastResult hit in hits) if (hit.module is GraphicRaycaster) return true;
        return false;
    }

    public static RectTransform CreateRect(string name, Transform parent)
    {
        var item = new GameObject(name, typeof(RectTransform));
        SceneManager.MoveGameObjectToScene(item, parent.gameObject.scene);
        item.transform.SetParent(parent, false);
        RectTransform rect = item.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        return rect;
    }

    public static Button CreateButton(string name, string caption, Transform parent, float y)
    {
        RectTransform rect = CreateRect(name, parent);
        rect.anchoredPosition = new Vector2(0, y);
        rect.sizeDelta = new Vector2(300, 52);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0.15f, 0.34f, 0.44f);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.selectedColor = new Color(0.6f, 0.9f, 1f);
        colors.disabledColor = new Color(0.35f, 0.35f, 0.35f);
        colors.fadeDuration = 0f;
        button.colors = colors;
        RectTransform label = CreateRect("Label", rect);
        Stretch(label);
        AddText(label, caption, 23);
        return button;
    }

    public static Text AddText(RectTransform rect, string caption, int size)
    {
        Text label = rect.gameObject.AddComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = size;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.raycastTarget = false;
        label.text = caption;
        return label;
    }

    public static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    public static void ClearSelection(Transform panel)
    {
        if (EventSystem.current == null)
            return;
        GameObject selected = EventSystem.current.currentSelectedGameObject;
        if (selected != null && selected.transform.IsChildOf(panel))
            EventSystem.current.SetSelectedGameObject(null);
    }
}
