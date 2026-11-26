using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class PauseMenuUI : MonoBehaviour
{
    private GameObject panel;
    private Button resumeButton;
    private Button loadButton;
    private Text saveStatus;
    private Text manualInfo;
    private GameplayController game;

    public static PauseMenuUI Create(GameplayController game, Scene scene)
    {
        var root = new GameObject("Pause UI", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        SceneManager.MoveGameObjectToScene(root, scene);
        var menu = root.AddComponent<PauseMenuUI>();
        menu.Build(game);
        return menu;
    }

    private void Build(GameplayController game)
    {
        this.game = game;
        Canvas canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform shade = CreateRect("Pause", transform);
        Stretch(shade);
        shade.gameObject.AddComponent<Image>().color = new Color(0.01f, 0.02f, 0.04f, 0.85f);
        panel = shade.gameObject;

        RectTransform card = CreateRect("Card", shade);
        card.sizeDelta = new Vector2(600, 500);
        card.gameObject.AddComponent<Image>().color = new Color(0.07f, 0.12f, 0.19f);

        RectTransform title = CreateRect("Title", card);
        title.anchoredPosition = new Vector2(0, 204);
        title.sizeDelta = new Vector2(360, 44);
        AddText(title, "Szünet", 28);

        resumeButton = CreateButton("Resume", "Folytatás", card, 130);
        resumeButton.onClick.AddListener(game.ResumeGame);
        Button saveButton = CreateButton("Save", "Mentés", card, 62);
        saveButton.onClick.AddListener(game.SaveProgress);
        loadButton = CreateButton("Load", "Betöltés", card, -6);
        loadButton.onClick.AddListener(game.LoadProgress);
        Button menuButton = CreateButton("Main Menu", "Főmenü", card, -74);
        menuButton.onClick.AddListener(game.ExitToMenu);

        RectTransform status = CreateRect("Save Status", card);
        status.anchoredPosition = new Vector2(0, -142);
        status.sizeDelta = new Vector2(540, 56);
        saveStatus = AddText(status, "", 16);
        RectTransform info = CreateRect("Manual Save Info", card);
        info.anchoredPosition = new Vector2(0, -204);
        info.sizeDelta = new Vector2(540, 50);
        manualInfo = AddText(info, "", 14);
        manualInfo.color = new Color(0.7f, 0.8f, 0.9f);

        // MainMenu has its own EventSystem. This one belongs to the playable scene.
        if (EventSystem.current == null)
        {
            var events = new GameObject("Pause UI Input", typeof(EventSystem), typeof(InputSystemUIInputModule));
            SceneManager.MoveGameObjectToScene(events, gameObject.scene);
            events.transform.SetParent(transform, false);
        }
        Hide();
    }

    private void Update()
    {
        if (panel == null || !panel.activeSelf)
            return;

        RefreshView();
    }

    private void RefreshView()
    {
        loadButton.interactable = game.CanLoadManualSave;
        saveStatus.text = game.SaveFeedback;
        manualInfo.text = game.ManualSaveInfo;
    }

    public void Show()
    {
        panel.SetActive(true);
        RefreshView();
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(resumeButton.gameObject);
    }

    public void Hide()
    {
        if (EventSystem.current != null)
        {
            GameObject selected = EventSystem.current.currentSelectedGameObject;
            if (selected != null && selected.transform.IsChildOf(panel.transform))
                EventSystem.current.SetSelectedGameObject(null);
        }
        panel.SetActive(false);
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        var item = new GameObject(name, typeof(RectTransform));
        SceneManager.MoveGameObjectToScene(item, parent.gameObject.scene);
        item.transform.SetParent(parent, false);
        RectTransform rect = item.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        return rect;
    }

    private static Button CreateButton(string name, string caption, Transform parent, float y)
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

    private static Text AddText(RectTransform rect, string caption, int size)
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

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
