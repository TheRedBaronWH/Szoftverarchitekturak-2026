using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using OLE.Core;

[RequireComponent(typeof(GameManager))]
public sealed class GameplayController : MonoBehaviour
{
    [SerializeField, Min(0.05f)] private float moveDuration = 0.4f;

    private GameManager scenes;
    private GameSession session;
    private TutorialBoard board;
    private Transform player;
    private Coroutine motion;

    public bool IsReady => session != null && board != null && player != null && !scenes.IsTransitioning;
    public string Platform => session.Platform;
    public bool ModeB => session.Mode == BoardMode.B;
    public bool HasWon => session.IsLevelComplete;

    private void OnEnable()
    {
        scenes = GetComponent<GameManager>();
        scenes.SceneReady += BindScene;
        if (!scenes.IsTransitioning && scenes.CurrentSceneIndex >= 0)
            BindScene(scenes.CurrentSceneIndex);
    }

    private void OnDisable()
    {
        scenes.SceneReady -= BindScene;
        StopMotion();
    }

    private void BindScene(int sceneID)
    {
        StopMotion();
        board = null;
        player = null;
        if (sceneID == (int)SceneID.MainMenu)
            session = null;
        if (sceneID != (int)SceneID.SampleScene)
            return;

        board = FindFirstObjectByType<TutorialBoard>();
        GameObject avatar = GameObject.FindWithTag("Player");
        if (board == null || avatar == null)
        {
            Debug.LogError("SampleScene needs a TutorialBoard and a player tagged Player.", this);
            return;
        }

        player = avatar.transform;
        if (session == null)
            session = TutorialCampaign.CreateSession();
        else if (session.IsMoving)
            session.RestartLevel();
        ShowSession();
    }

    // Both a mouse click and a key reach the same rule in GameSession.
    public void RequestMove(string target)
    {
        if (!IsReady || !board.TryGetPosition(target, out Vector3 destination))
            return;

        var game = session;
        if (game.TryStartMove(target, out var move))
            motion = StartCoroutine(AnimateMove(destination, () => game.CompleteMove(move)));
    }

    public void SelectCollider(Collider hit)
    {
        if (IsReady)
            RequestMove(board.Identify(hit));
    }

    public string ReachablePlatforms()
    {
        if (!IsReady || session.IsMoving || HasWon)
            return "–";

        var reachable = board.PlatformIds.Where(id =>
            session.Level.CanTraverse(session.Platform, id, session.Mode));
        return string.Join(", ", reachable);
    }

    public void ToggleMode()
    {
        if (IsReady && session.TryToggleMode())
            board.ShowMode(ModeB, false);
    }

    public void RestartLevel()
    {
        if (!IsReady)
            return;

        StopMotion();
        session.RestartLevel();
        ShowSession();
    }

    public void ExitToMenu()
    {
        if (!IsReady)
            return;

        StopMotion();
        scenes.ExitToMainMenu();
    }

    private void ShowSession()
    {
        if (!board.TryGetPosition(session.Platform, out Vector3 position))
        {
            Debug.LogError("No scene binding for platform " + session.Platform, board);
            board = null;
            return;
        }

        player.position = position;
        board.ShowMode(ModeB, true);
    }

    private IEnumerator AnimateMove(Vector3 destination, Action onArrival)
    {
        Vector3 start = player.position;
        float elapsed = 0f;
        while (elapsed < moveDuration)
        {
            yield return null;
            if (!IsReady)
            {
                motion = null;
                yield break;
            }

            elapsed += Time.deltaTime;
            player.position = Vector3.Lerp(start, destination, elapsed / moveDuration);
        }

        player.position = destination;
        onArrival();
        motion = null;
    }

    private void StopMotion()
    {
        if (motion != null)
            StopCoroutine(motion);

        motion = null;
    }
}
