using UnityEngine;
using UnityEngine.UI;

public sealed class MainMenuActions : MonoBehaviour
{
    [SerializeField] private Button continueButton;
    [SerializeField] private Text messageLabel;
    private GameplayController game;

    private void Awake()
    {
        foreach (Text label in transform.parent.GetComponentsInChildren<Text>())
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (GameManager.Instance == null)
        {
            continueButton.interactable = false;
            messageLabel.text = "A játékot a GameManagement jelenetből indítsd.";
            return;
        }
        game = GameManager.Instance.GetComponent<GameplayController>();
        continueButton.interactable = game.CheckContinuation();
        messageLabel.text = game.Feedback;
    }

    // Called by the New Game button in MainMenu.unity.
    public void StartTutorial()
    {
        if (game != null)
            game.StartNewGame();
    }

    public void ContinueGame()
    {
        if (game == null)
            return;
        game.ContinueSavedGame();
        messageLabel.text = game.Feedback;
    }
}
