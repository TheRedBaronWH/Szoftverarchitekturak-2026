using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

[RequireComponent(typeof(GameplayController))]
public sealed class TutorialControls : MonoBehaviour
{
    private static readonly Key[] PlatformKeys =
    {
        Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4,
        Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9, Key.Digit0
    };
    private GameplayController game;
    private GUIStyle statusStyle;

    private void Awake()
    {
        game = GetComponent<GameplayController>();
    }

    private void Update()
    {
        if (!game.IsReady)
            return;

        Keyboard keys = Keyboard.current;
        if (game.IsEnding)
        {
            if (keys != null && keys.escapeKey.wasPressedThisFrame) game.SkipFinale();
            return;
        }
        if (keys != null && keys.f3Key.wasPressedThisFrame) game.ToggleDiagnostics();
        if (keys != null)
        {
            if (keys.escapeKey.wasPressedThisFrame)
            {
                if (game.HasWon)
                    game.ExitToMenu();
                else if (game.IsPaused)
                    game.ResumeGame();
                else
                    game.PauseGame();
                return;
            }
        }

        if (game.IsInputBlocked)
            return;

        if (keys != null)
        {
            if (keys.rKey.wasPressedThisFrame)
            {
                game.RestartLevel();
                return;
            }
            if (keys.nKey.wasPressedThisFrame)
            {
                game.NextLevel();
                return;
            }
            if (keys.f5Key.wasPressedThisFrame)
            {
                game.SaveProgress();
                return;
            }
            if (keys.f9Key.wasPressedThisFrame)
            {
                game.LoadProgress();
                return;
            }

            if (game.HasWon)
                return;
            if (keys.upArrowKey.wasPressedThisFrame) { game.RequestDirectionalMove(Vector2.up); return; }
            if (keys.downArrowKey.wasPressedThisFrame) { game.RequestDirectionalMove(Vector2.down); return; }
            if (keys.leftArrowKey.wasPressedThisFrame) { game.RequestDirectionalMove(Vector2.left); return; }
            if (keys.rightArrowKey.wasPressedThisFrame) { game.RequestDirectionalMove(Vector2.right); return; }
            if (keys.spaceKey.wasPressedThisFrame)
            {
                game.ToggleMode();
                return;
            }
            if (keys.hKey.wasPressedThisFrame)
            {
                game.ShowHint();
                return;
            }

            for (int index = 0; index < PlatformKeys.Length; index++)
            {
                if (keys[PlatformKeys[index]].wasPressedThisFrame)
                {
                    game.RequestMove("P" + (index + 1));
                    return;
                }
            }
        }

        if (game.HasWon)
            return;

        Mouse mouse = Mouse.current;
        Camera camera = Camera.main;
        if (mouse == null || camera == null || !mouse.leftButton.wasPressedThisFrame)
            return;
        if (MenuUI.IsPointerOverUI(mouse.position.ReadValue()))
            return;

        Ray ray = camera.ScreenPointToRay(mouse.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
            game.SelectCollider(hit.collider);
    }

    // Passive labels only. All input is handled by the Input System above.
    private void OnGUI()
    {
        if (!game.IsReady || game.IsPaused || game.HasWon || !game.DebugVisible)
            return;

        if (statusStyle == null)
        {
            statusStyle = new GUIStyle(GUI.skin.box);
            statusStyle.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            statusStyle.fontSize = 14;
            statusStyle.alignment = TextAnchor.MiddleLeft;
            statusStyle.wordWrap = true;
        }

        string mode = game.ModeB ? "B" : "A";
        string status = game.Feedback;

        GUI.Box(new Rect(12, Mathf.Max(12, Screen.height - 168), Mathf.Min(620, Screen.width - 24), 156),
            "Pálya: " + game.LevelNumber + "/" + game.LevelCount + " " + game.LevelName + "   |   Cél: " + game.Goal
            + "\nPlatform: " + game.Platform + "   |   Mód: " + mode + "   |   Járható: " + game.ReachablePlatforms()
            + "\n1-9, 0 / kattintás: lépés   Space: A/B   R: újra   Esc: szünet"
            + "\nH: tipp   F5: mentés   F9: betöltés   N: következő pálya"
            + "\n" + status + "\n" + game.SaveFeedback, statusStyle);
    }
}
