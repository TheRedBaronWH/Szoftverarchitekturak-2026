using System;
using System.Collections;
using System.Linq;
using System.IO;
using UnityEngine;
using OLE.Core;

[RequireComponent(typeof(GameManager))]
public sealed class GameplayController : MonoBehaviour
{
    [SerializeField, Min(0.05f)] private float moveDuration = 0.4f;

    private GameManager scenes;
    private GameProgress progress;
    private GameSession session => progress?.Session;
    private TutorialBoard board;
    private Transform player;
    private Coroutine motion;
    private PauseMenuUI pauseMenu;
    private float timeScaleBeforePause = 1f;
    private bool timeFrozen;
    private int resumedFrame = -1;

    public bool IsReady => session != null && board != null && player != null && !scenes.IsTransitioning;
    public string Platform => session.Platform;
    public bool ModeB => session.Mode == BoardMode.B;
    public bool HasWon => session.IsLevelComplete;
    public string LevelName => session.Level.Id;
    public string Goal => session.Level.Goal;
    public bool HasNextLevel => session.HasNextLevel;
    public string Feedback { get; private set; }
    public string SaveFeedback { get; private set; }
    public string ManualSaveInfo { get; private set; }
    public bool CanLoadManualSave { get; private set; }
    public bool IsPaused => session != null && session.IsPaused;
    public bool IsInputBlocked => IsPaused || Time.frameCount == resumedFrame;
    public static string SavePath => Path.Combine(Application.persistentDataPath, "progress.xml");
    public static string AutosavePath => Path.Combine(Application.persistentDataPath, "autosave.xml");

    private void OnEnable()
    {
        scenes = GetComponent<GameManager>();
        if (progress == null)
            progress = new GameProgress(TutorialCampaign.CreateSession,
                new SaveStore(SavePath), new SaveStore(AutosavePath));
        scenes.SceneReady += BindScene;
        if (!scenes.IsTransitioning && scenes.CurrentSceneIndex >= 0)
            BindScene(scenes.CurrentSceneIndex);
    }

    private void OnDisable()
    {
        scenes.SceneReady -= BindScene;
        StopMotion();
        ResumeGame();
    }

    private void OnApplicationQuit()
    {
        progress?.Autosave();
    }

    private void BindScene(int sceneID)
    {
        StopMotion();
        board = null;
        player = null;
        if (pauseMenu != null)
        {
            pauseMenu.gameObject.SetActive(false);
            Destroy(pauseMenu.gameObject);
        }
        pauseMenu = null;
        if (sceneID == (int)SceneID.MainMenu)
        {
            Feedback = "";
        }
        if (sceneID != (int)SceneID.SampleScene)
            return;

        board = FindAnyObjectByType<TutorialBoard>();
        GameObject avatar = GameObject.FindWithTag("Player");
        if (board == null || avatar == null)
        {
            Debug.LogError("SampleScene needs a TutorialBoard and a player tagged Player.", this);
            return;
        }

        player = avatar.transform;
        if (session == null)
        {
            progress.StartNewGame();
            ReportAutosave();
        }
        else if (session.IsMoving)
        {
            progress.EnterMainMenu();
            ReportAutosave();
        }
        ShowSession();
        RefreshManualSave();
        if (IsReady)
            pauseMenu = PauseMenuUI.Create(this, board.gameObject.scene);
    }

    // Both a mouse click and a key reach the same rule in GameSession.
    public void RequestMove(string target)
    {
        if (!IsReady || IsInputBlocked || !session.Level.HasPlatform(target)
            || !board.TryGetPosition(target, out Vector3 destination))
            return;

        var game = session;
        if (game.TryStartMove(target, out var move))
        {
            Feedback = "";
            motion = StartCoroutine(AnimateMove(destination, () =>
            {
                if (progress.CompleteMove(move))
                {
                    board.HighlightPlatforms(game.Platform, game.Mode);
                    ReportAutosave();
                }
            }));
        }
        else
            Feedback = "A lépés most nem engedélyezett: " + target;
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
        if (IsReady && progress.TryToggleMode())
        {
            Feedback = "";
            board.ShowMode(ModeB, false);
            board.HighlightPlatforms(session.Platform, session.Mode);
            ReportAutosave();
        }
        else if (IsReady)
            Feedback = "Mozgás közben nem válthatsz módot.";
    }

    public void NextLevel()
    {
        if (IsReady && progress.TryNextLevel())
        {
            ShowSession();
            ReportAutosave();
        }
    }

    public void PauseGame()
    {
        if (!IsReady || IsPaused)
            return;

        timeScaleBeforePause = Time.timeScale;
        session.Pause();
        timeFrozen = true;
        Time.timeScale = 0f;
        RefreshManualSave();
        pauseMenu.Show();
    }

    public void ResumeGame()
    {
        session?.Resume();
        if (timeFrozen)
            Time.timeScale = timeScaleBeforePause;
        timeFrozen = false;
        resumedFrame = Time.frameCount;
        if (pauseMenu != null)
            pauseMenu.Hide();
    }

    public bool CheckContinuation()
    {
        SaveResult result = progress.CheckContinuation();
        Feedback = result == SaveResult.Success ? AutosaveWarning().Trim()
            : result == SaveResult.NotFound ? "" : SaveMessage(result, "");
        return result == SaveResult.Success;
    }

    public void StartNewGame()
    {
        if (scenes.IsTransitioning || scenes.CurrentSceneIndex != (int)SceneID.MainMenu)
            return;

        ResumeGame();
        progress.StartNewGame();
        ReportAutosave();
        scenes.MenuLoadLevel((int)SceneID.SampleScene);
    }

    public void ShowHint()
    {
        if (!IsReady)
            return;

        HintStep hint = session.GetHint();
        if (hint == null)
            Feedback = session.IsMoving ? "Várd meg a lépés végét." : "A pálya már teljesült.";
        else
            Feedback = hint.Kind == HintKind.Toggle ? "Tipp: válts A/B módot." : "Tipp: lépj ide: " + hint.Target;
    }

    public void SaveProgress()
    {
        if (!IsReady)
            return;

        Feedback = SaveMessage(progress.Save(), "Kézi mentés kész.");
        SaveFeedback = Feedback;
        RefreshManualSave();
    }

    public void LoadProgress()
    {
        if (!IsReady)
            return;

        bool wasPaused = IsPaused;
        SaveResult result = progress.Load(keepPaused: wasPaused);
        if (result == SaveResult.Success)
        {
            StopMotion();
            if (!wasPaused)
                ResumeGame();
            ShowSession();
            ReportAutosave();
        }
        Feedback = SaveMessage(result, "Kézi mentés betöltve.");
        SaveFeedback = Feedback + (result == SaveResult.Success ? AutosaveWarning() : "");
        RefreshManualSave();
    }

    public void ContinueSavedGame()
    {
        if (scenes.IsTransitioning || scenes.CurrentSceneIndex != (int)SceneID.MainMenu)
            return;

        SaveResult result = progress.Continue();
        Feedback = SaveMessage(result, "Játék folytatása.");
        if (result == SaveResult.Success)
        {
            ReportAutosave();
            if (progress.UsedManualSaveFallback)
                SaveFeedback = "Az automatikus mentés nem tölthető be. A kézi mentésből folytatod. " + SaveFeedback;
            ResumeGame();
            scenes.MenuLoadLevel((int)SceneID.SampleScene);
        }
    }

    private void RefreshManualSave()
    {
        SaveResult result = progress.CheckManualSave();
        CanLoadManualSave = result == SaveResult.Success;
        ManualSaveInfo = result == SaveResult.Success
            ? "Betöltés: a Mentés / F5 gombbal rögzített állapot."
            : result == SaveResult.NotFound ? "Még nincs kézi mentés. Válaszd a Mentés gombot."
            : SaveMessage(result, "");
    }

    private void ReportAutosave()
    {
        if (!progress.LastAutosaveResult.HasValue)
            return;
        SaveFeedback = progress.LastAutosaveResult == SaveResult.Success
            ? "Automatikus mentés kész."
            : "Automatikus mentés: " + SaveMessage(progress.LastAutosaveResult.Value, "");
    }

    private string AutosaveWarning()
    {
        return progress.LastAutosaveResult.HasValue && progress.LastAutosaveResult != SaveResult.Success
            ? " Automatikus mentés: " + SaveMessage(progress.LastAutosaveResult.Value, "") : "";
    }

    private static string SaveMessage(SaveResult result, string success)
    {
        switch (result)
        {
            case SaveResult.Success: return success;
            case SaveResult.NotFound: return "Még nincs mentett játék.";
            case SaveResult.InvalidData: return "A mentés sérült vagy ehhez a pályához nem használható.";
            case SaveResult.UnsupportedVersion: return "Ez a mentési verzió nem támogatott.";
            case SaveResult.IntegrityFailed: return "A mentés sértetlenségi ellenőrzése sikertelen.";
            case SaveResult.KeyUnavailable: return "A mentés ellenőrző kulcsa nem érhető el.";
            default: return "A mentési fájl nem érhető el. Ellenőrizd az írási jogosultságot.";
        }
    }

    public void RestartLevel()
    {
        if (!IsReady)
            return;

        StopMotion();
        ResumeGame();
        progress.RestartLevel();
        ShowSession();
        ReportAutosave();
    }

    public void ExitToMenu()
    {
        if (!IsReady)
            return;

        StopMotion();
        ResumeGame();
        progress.EnterMainMenu();
        ReportAutosave();
        if (pauseMenu != null)
            pauseMenu.gameObject.SetActive(false);
        scenes.ExitToMainMenu();
    }

    private void ShowSession()
    {
        if (!board.ShowLevel(session.Level))
        {
            board = null;
            return;
        }
        if (!board.TryGetPosition(session.Platform, out Vector3 position))
        {
            Debug.LogError("No scene binding for platform " + session.Platform, board);
            board = null;
            return;
        }

        player.position = position;
        Feedback = "";
        board.ShowMode(ModeB, true);
        board.HighlightPlatforms(session.Platform, session.Mode);
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

            if (session.IsPaused)
                continue;

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
