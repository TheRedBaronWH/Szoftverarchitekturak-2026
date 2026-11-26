using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(GameplayController))]
public sealed class TutorialControls : MonoBehaviour
{
    private static readonly Key[] PlatformKeys =
    {
        Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4,
        Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8
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
        if (keys != null)
        {
            if (keys.escapeKey.wasPressedThisFrame)
            {
                game.ExitToMenu();
                return;
            }
            if (keys.rKey.wasPressedThisFrame)
            {
                game.RestartLevel();
                return;
            }
            if (keys.spaceKey.wasPressedThisFrame)
            {
                game.ToggleMode();
                return;
            }
            if (keys.nKey.wasPressedThisFrame)
            {
                game.NextLevel();
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

        Mouse mouse = Mouse.current;
        Camera camera = Camera.main;
        if (mouse == null || camera == null || !mouse.leftButton.wasPressedThisFrame)
            return;

        Ray ray = camera.ScreenPointToRay(mouse.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
            game.SelectCollider(hit.collider);
    }

    // Passive labels only. All input is handled by the Input System above.
    private void OnGUI()
    {
        if (!game.IsReady)
            return;

        if (statusStyle == null)
        {
            statusStyle = new GUIStyle(GUI.skin.box);
            statusStyle.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            statusStyle.fontSize = 16;
            statusStyle.alignment = TextAnchor.MiddleLeft;
            statusStyle.wordWrap = true;
        }

        string mode = game.ModeB ? "B" : "A";
        string status = game.Feedback;
        if (game.HasWon)
            status = game.HasNextLevel ? "Pálya teljesítve! N: következő pálya" : "Minden pálya teljesítve!";

        GUI.Box(new Rect(12, Mathf.Max(12, Screen.height - 128), Mathf.Min(540, Screen.width - 24), 116),
            "Pálya: " + game.LevelName + "   |   Cél: " + game.Goal
            + "\nPlatform: " + game.Platform + "   |   Mód: " + mode + "   |   Járható: " + game.ReachablePlatforms()
            + "\n1-8 / kattintás: lépés   Space: A/B   R: újra   Esc: menü"
            + "\n" + status, statusStyle);
    }
}
