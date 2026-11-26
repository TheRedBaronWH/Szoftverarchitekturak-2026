using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(GameplayController))]
public sealed class TutorialControls : MonoBehaviour
{
    private static readonly Key[] PlatformKeys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4 };
    private GameplayController game;

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

        string mode = game.ModeB ? "B" : "A";
        string status = game.HasWon ? "Pálya teljesítve!" : "Platform: " + game.Platform;
        GUI.Box(new Rect(12, 12, 405, 68),
            status + "   |   Mód: " + mode + "   |   Járható: " + game.ReachablePlatforms() + "\n1–4 / kattintás: lépés    Space: A/B    R: újra    Esc: menü");
    }
}
