using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static MenuUI;

public sealed class LevelCompleteUI : MonoBehaviour
{
    private GameplayController game;
    private Text title;
    private Text description;
    private Text saveStatus;
    private Button nextButton;
    private Button restartButton;
    private Button menuButton;
    private CelebrationEffect confetti;

    // Shares the pause menu's Canvas and EventSystem, but has its own panel.
    public static LevelCompleteUI Create(GameplayController game, Transform canvas)
    {
        RectTransform root = CreateRect("Level Complete", canvas);
        Stretch(root);
        root.gameObject.AddComponent<Image>().color = new Color(0.01f, 0.02f, 0.04f, 0.85f);
        var menu = root.gameObject.AddComponent<LevelCompleteUI>();
        menu.Build(game);
        menu.confetti = CelebrationEffect.Create(root);
        menu.Hide();
        return menu;
    }

    private void Build(GameplayController game)
    {
        this.game = game;
        RectTransform card = CreateRect("Card", transform);
        card.sizeDelta = new Vector2(600, 440);
        card.gameObject.AddComponent<Image>().color = new Color(0.07f, 0.12f, 0.19f);

        RectTransform heading = CreateRect("Title", card);
        heading.anchoredPosition = new Vector2(0, 174);
        heading.sizeDelta = new Vector2(550, 44);
        title = AddText(heading, "", 28);
        RectTransform message = CreateRect("Description", card);
        message.anchoredPosition = new Vector2(0, 106);
        message.sizeDelta = new Vector2(540, 72);
        description = AddText(message, "", 17);

        nextButton = CreateButton("Next Level", "Következő pálya", card, 34);
        nextButton.onClick.AddListener(game.NextLevel);
        restartButton = CreateButton("Restart Level", "Pálya újrakezdése", card, -30);
        restartButton.onClick.AddListener(game.RestartLevel);
        menuButton = CreateButton("Main Menu", "Főmenü", card, -94);
        menuButton.onClick.AddListener(game.ExitToMenu);

        RectTransform status = CreateRect("Save Status", card);
        status.anchoredPosition = new Vector2(0, -166);
        status.sizeDelta = new Vector2(540, 64);
        saveStatus = AddText(status, "", 15);
    }

    private void Update()
    {
        saveStatus.text = game.SaveFeedback;
        if (game.HasNextLevel)
            description.text = "Pálya: " + game.LevelNumber + " / " + game.LevelCount
                + "\nAutomatikus továbblépés " + game.NextLevelSeconds + " másodperc múlva.";
        else description.text = "Minden pályát teljesítettél!\nA zárójelenet hamarosan elindul.";
    }

    public void Show()
    {
        bool hasNext = game.HasNextLevel;
        title.text = hasNext ? "Pálya teljesítve!" : "Gratulálunk!";
        string progress = "Pálya: " + game.LevelNumber + " / " + game.LevelCount;
        description.text = hasNext
            ? progress + "\nElérted a célt. Hamarosan indul a következő pálya."
            : progress + "\nMinden pályát teljesítettél! Hamarosan indul a zárójelenet.";
        nextButton.gameObject.SetActive(hasNext);
        restartButton.GetComponentInChildren<Text>().text = hasNext ? "Pálya újrakezdése" : "Új játék";
        restartButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, hasNext ? -30 : 2);
        menuButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, hasNext ? -94 : -62);
        saveStatus.text = game.SaveFeedback;
        bool wasVisible = gameObject.activeSelf;
        gameObject.SetActive(true);
        if (!wasVisible) confetti.Play(false);
        if (!wasVisible && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(hasNext ? nextButton.gameObject : restartButton.gameObject);
    }

    public void Hide()
    {
        ClearSelection(transform);
        gameObject.SetActive(false);
    }
}
