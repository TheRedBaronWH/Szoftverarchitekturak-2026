using UnityEngine;
using UnityEngine.UI;
using static MenuUI;

public sealed class GameHUD : MonoBehaviour
{
    private GameplayController game;
    private GameObject panel;
    private Text heading, status, modeCaption;
    private Button modeButton, loadButton;

    public static GameHUD Create(GameplayController game, Transform canvas)
    {
        RectTransform root = CreateRect("Gameplay HUD", canvas);
        Stretch(root);
        root.SetAsFirstSibling();
        var hud = root.gameObject.AddComponent<GameHUD>();
        hud.Build(game);
        return hud;
    }

    private void Build(GameplayController game)
    {
        this.game = game;
        RectTransform card = CreateRect("Controls", transform);
        card.anchorMin = card.anchorMax = new Vector2(0.5f,0);
        card.anchoredPosition = new Vector2(0,76);
        card.sizeDelta = new Vector2(950,132);
        card.gameObject.AddComponent<Image>().color = new Color(0.04f,0.08f,0.14f,0.94f);
        panel = card.gameObject;
        RectTransform title = CreateRect("Level", card);
        title.anchoredPosition = new Vector2(0,44); title.sizeDelta = new Vector2(900,26);
        heading = AddText(title,"",18);
        modeButton = Control("Mode","Nézőpont A/B",card,-285);
        modeCaption = modeButton.GetComponentInChildren<Text>();
        modeButton.onClick.AddListener(game.ToggleMode);
        Control("Save","Mentés",card,-95).onClick.AddListener(game.SaveProgress);
        loadButton = Control("Load","Kézi betöltés",card,95);
        loadButton.onClick.AddListener(game.LoadProgress);
        Control("Pause","Szünet",card,285).onClick.AddListener(game.PauseGame);
        RectTransform message = CreateRect("Status",card);
        message.anchoredPosition = new Vector2(0,-43); message.sizeDelta = new Vector2(900,32);
        status = AddText(message,"",13);
    }

    private static Button Control(string name, string caption, Transform parent, float x)
    {
        Button button = CreateButton(name,caption,parent,0);
        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(x,1); rect.sizeDelta = new Vector2(180,40);
        button.GetComponentInChildren<Text>().fontSize = 19;
        return button;
    }

    private void Update()
    {
        bool visible = game.IsReady && !game.IsPaused && !game.HasWon;
        panel.SetActive(visible);
        if (!visible) return;
        heading.text = game.LevelNumber + " / " + game.LevelCount + " · " + game.LevelTitle + " · Arany jel: cél";
        modeCaption.text = "Nézőpont: " + (game.ModeB ? "B" : "A") + (game.HasSwitchPads && !game.CanChangeModeHere ? " · zárolt" : "");
        modeButton.interactable = !game.IsMoving && !game.IsModeChanging;
        loadButton.interactable = game.CanLoadManualSave;
        status.text = string.IsNullOrEmpty(game.Feedback)
            ? (game.HasSwitchPads ? "A/B csak a türkiz kapcsolókon · Keresztezés ≠ csomópont · H: tipp · "
                : "Billentyűjel / kattintás / nyilak: mozgás · Space: A/B · H: tipp · ") + game.SaveFeedback
            : game.Feedback;
    }
}
