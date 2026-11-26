using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static MenuUI;

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
        card.sizeDelta = new Vector2(600, 580);
        card.gameObject.AddComponent<Image>().color = new Color(0.07f, 0.12f, 0.19f);

        RectTransform title = CreateRect("Title", card);
        title.anchoredPosition = new Vector2(0, 240);
        title.sizeDelta = new Vector2(360, 44);
        AddText(title, "Szünet", 28);

        resumeButton = CreateButton("Resume", "Folytatás", card, 170);
        resumeButton.onClick.AddListener(game.ResumeGame);
        Button saveButton = CreateButton("Save", "Mentés", card, 102);
        saveButton.onClick.AddListener(game.SaveProgress);
        loadButton = CreateButton("Load", "Kézi betöltés", card, 34);
        loadButton.onClick.AddListener(game.LoadProgress);
        Button menuButton = CreateButton("Main Menu", "Főmenü", card, -34);
        menuButton.onClick.AddListener(game.ExitToMenu);
        Button exitButton = CreateButton("Quit", "Kilépés", card, -102);
        exitButton.onClick.AddListener(game.QuitGame);

        RectTransform status = CreateRect("Save Status", card);
        status.anchoredPosition = new Vector2(0, -180);
        status.sizeDelta = new Vector2(540, 56);
        saveStatus = AddText(status, "", 16);
        RectTransform info = CreateRect("Manual Save Info", card);
        info.anchoredPosition = new Vector2(0, -244);
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
        ClearSelection(panel.transform);
        panel.SetActive(false);
    }
}
