using System;
using System.Collections;
using System.Linq;
using System.IO;
using UnityEngine;
using OLE.Core;

[RequireComponent(typeof(GameManager))]
[DefaultExecutionOrder(100)]
public sealed class GameplayController : MonoBehaviour
{
    [SerializeField, Min(0.05f)] private float moveDuration = 0.4f;

    private GameManager scenes;
    private GameProgress progress;
    private GameSession session => progress?.Session;
    private TutorialBoard board;
    private Transform player;
    private AvatarVisual avatar;
    private Coroutine motion;
    private PauseMenuUI pauseMenu;
    private LevelCompleteUI completionMenu;
    private FinaleDirector finale;
    private readonly CompletionTimer completionTimer = new CompletionTimer();
    private float timeScaleBeforePause = 1f;
    private bool timeFrozen;
    private int resumedFrame = -1;

    public bool IsReady => isActiveAndEnabled && session != null && board != null && player != null && !scenes.IsTransitioning;
    public string Platform => session.Platform;
    public bool ModeB => session.Mode == BoardMode.B;
    public bool HasWon => session.IsLevelComplete;
    public string LevelName => session.Level.Id;
    public string Goal => session.Level.Goal;
    public bool HasNextLevel => session.HasNextLevel;
    public int LevelNumber => session.LevelNumber;
    public int LevelCount => session.LevelCount;
    public int NextLevelSeconds => Mathf.CeilToInt((float)completionTimer.Remaining);
    public bool IsMoving => session != null && session.IsMoving;
    public bool IsModeChanging => board != null && board.IsChanging;
    public bool CanChangeModeHere => session != null && session.Level.CanToggleAt(session.Platform);
    public bool HasSwitchPads => session != null && session.Level.HasSwitchPads;
    public bool IsEnding => finale != null && finale.IsPlaying;
    public Vector3 LevelCenter => board != null ? board.Center : Vector3.zero;
    public bool DebugVisible { get; private set; }
    public string LevelTitle => LevelName == "tutorial" ? "Első lépések" : LevelName == "crossroads" ? "Elágazások"
        : LevelName == "skybridge" ? "Égi hidak" : LevelName == "paradox-stairs" ? "Paradox lépcsők" : "Szőtt labirintus";
    public string Feedback { get; private set; }
    public string SaveFeedback { get; private set; }
    public string ManualSaveInfo { get; private set; }
    public bool CanLoadManualSave { get; private set; }
    public bool IsPaused => session != null && session.IsPaused;
    public bool IsInputBlocked => IsEnding || IsPaused || IsModeChanging || Time.frameCount == resumedFrame;
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
        session?.Resume();
        RestoreTime();
        if (finale != null) finale.Stop();
        if (pauseMenu != null)
            pauseMenu.gameObject.SetActive(false);
        if (board != null)
            board.LabelsVisible = true;
    }

    private void OnApplicationQuit()
    {
        progress?.Autosave();
    }

    private void Update()
    {
        if (!IsReady || IsEnding || !HasWon || !completionTimer.Tick(session, Time.unscaledDeltaTime)) return;
        if (HasNextLevel) NextLevel();
        else StartFinale();
    }

    private void StartFinale()
    {
        if (!IsReady || HasNextLevel || !HasWon || finale == null || IsEnding) return;
        progress.Autosave(); ReportAutosave();
        completionMenu.Hide(); pauseMenu.Hide();
        finale.Play();
    }

    public void SkipFinale() { if (finale != null) finale.Skip(); }

    public void ReplayCampaign()
    {
        if (!IsReady || !session.IsGameComplete) return;
        if (finale != null) finale.Stop();
        StopMotion(); progress.StartNewGame(); ResumeGame(); ShowSession(); ReportAutosave();
    }

    private void LateUpdate()
    {
        if (avatar == null) return;
        avatar.SetVisible(IsReady);
        if (!IsReady) return;
        if (!IsEnding) board.EnsureCameraFrame();
        if (!IsMoving && board.TryGetPose(session.Platform, out Pose pose)) avatar.SetFootPose(pose);
        if (!IsPaused) avatar.Animate(IsMoving);
    }

    public void ToggleDiagnostics()
    {
        DebugVisible = !DebugVisible;
        if (board != null) board.DebugVisible = DebugVisible;
    }

    public void RequestDirectionalMove(Vector2 direction)
    {
        if (IsReady) RequestMove(board.DirectionalTarget(session.Platform, direction));
    }

    private void BindScene(int sceneID)
    {
        completionTimer.Reset();
        StopMotion();
        board = null;
        player = null;
        avatar = null;
        if (pauseMenu != null)
        {
            pauseMenu.gameObject.SetActive(false);
            Destroy(pauseMenu.gameObject);
        }
        pauseMenu = null;
        completionMenu = null;
        finale = null;
        if (sceneID == (int)SceneID.MainMenu)
        {
            Feedback = "";
        }
        if (sceneID != (int)SceneID.SampleScene)
            return;

        board = FindAnyObjectByType<TutorialBoard>();
        GameObject playerObject = GameObject.FindWithTag("Player");
        if (board == null || playerObject == null)
        {
            Debug.LogError("SampleScene needs a TutorialBoard and a player tagged Player.", this);
            return;
        }

        player = playerObject.transform;
        avatar = player.GetComponent<AvatarVisual>();
        if (avatar == null) avatar = player.gameObject.AddComponent<AvatarVisual>();
        avatar.Initialize(board.Materials);
        board.DebugVisible = DebugVisible;
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
        {
            pauseMenu = PauseMenuUI.Create(this, board.gameObject.scene);
            completionMenu = LevelCompleteUI.Create(this, pauseMenu.transform);
            GameHUD.Create(this, pauseMenu.transform);
            finale = FinaleDirector.Create(this, pauseMenu.transform);
            RefreshMenus();
        }
    }

    // Both a mouse click and a key reach the same rule in GameSession.
    public void RequestMove(string target)
    {
        if (!IsReady || IsInputBlocked || HasWon || !session.Level.HasPlatform(target)
            || !board.TryGetPosition(target, out Vector3 destination))
            return;

        var game = session;
        if (game.TryStartMove(target, out var move))
        {
            Feedback = "";
            motion = StartCoroutine(AnimateMove(move.From, move.To, destination, () =>
            {
                if (progress.CompleteMove(move))
                {
                    board.HighlightPlatforms(game.Platform, game.Mode);
                    ReportAutosave();
                    RefreshMenus();
                }
            }));
        }
        else
            Feedback = DebugVisible ? "A lépés most nem engedélyezett: " + target
                : "Ez a kapcsolat ebben a nézőpontban nem járható.";
    }

    public void SelectCollider(Collider hit)
    {
        if (IsReady)
            RequestMove(board.Identify(hit, session.Platform));
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
        if (!IsReady || HasWon || IsModeChanging)
            return;
        if (!CanChangeModeHere)
        {
            Feedback = "Itt csak az A/B jelű türkiz kapcsolóplatformokon válthatsz nézőpontot. Járd be az oldalágakat!";
            return;
        }

        if (progress.TryToggleMode())
        {
            Feedback = "";
            board.ShowMode(ModeB, false);
            board.HighlightPlatforms(session.Platform, session.Mode);
            ReportAutosave();
        }
        else
            Feedback = "Mozgás közben nem válthatsz módot.";
    }

    public void NextLevel()
    {
        if (IsReady && progress.TryNextLevel())
        {
            ResumeGame();
            ShowSession();
            ReportAutosave();
        }
    }

    public void PauseGame()
    {
        if (!IsReady)
            return;
        if (HasWon)
        {
            RefreshMenus();
            return;
        }
        if (IsPaused)
            return;

        timeScaleBeforePause = Time.timeScale;
        session.Pause();
        timeFrozen = true;
        Time.timeScale = 0f;
        RefreshManualSave();
        pauseMenu.Show();
        board.LabelsVisible = false;
    }

    public void ResumeGame()
    {
        session?.Resume();
        RestoreTime();
        resumedFrame = Time.frameCount;
        if (pauseMenu != null)
            pauseMenu.Hide();
        RefreshMenus();
    }

    private void RestoreTime()
    {
        if (timeFrozen)
            Time.timeScale = timeScaleBeforePause;
        timeFrozen = false;
    }

    private void RefreshMenus()
    {
        if (!IsReady)
            return;

        if (IsEnding)
        {
            completionMenu.Hide(); pauseMenu.Hide(); board.LabelsVisible = false;
            return;
        }

        if (HasWon)
        {
            // A restored winning save may have been loaded from the pause menu.
            session.Resume();
            RestoreTime();
            if (pauseMenu != null)
                pauseMenu.Hide();
            if (completionMenu != null)
            {
                if (!completionMenu.gameObject.activeSelf)
                    resumedFrame = Time.frameCount;
                completionMenu.Show();
            }
        }
        else if (completionMenu != null)
            completionMenu.Hide();

        board.LabelsVisible = !IsPaused && !HasWon;
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
        if (!IsReady || HasWon)
            return;

        HintStep hint = session.GetHint();
        if (hint == null)
            Feedback = session.IsMoving ? "Várd meg a lépés végét." : "A pálya már teljesült.";
        else if (hint.Kind == HintKind.Toggle) Feedback = "Tipp: válts nézőpontot.";
        else
        {
            board.HighlightHint(hint.Target);
            Feedback = DebugVisible ? "Tipp: lépj ide: " + hint.Target : "Tipp: a kiemelt platformra lépj.";
        }
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

        if (finale != null) finale.Stop();
        StopMotion();
        progress.RestartLevel();
        ResumeGame();
        ShowSession();
        ReportAutosave();
    }

    public void ExitToMenu()
    {
        if (!IsReady)
            return;

        if (finale != null) finale.Stop();
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
        completionTimer.Reset();
        if (!board.ShowLevel(session.Level))
        {
            board = null;
            return;
        }
        board.ShowMode(ModeB, true);
        if (!board.TryGetPose(session.Platform, out Pose pose))
        {
            Debug.LogError("No scene binding for platform " + session.Platform, board);
            board = null;
            return;
        }

        avatar.SetFootPose(pose, true);
        Feedback = "";
        board.HighlightPlatforms(session.Platform, session.Mode);
        RefreshMenus();
    }

    private IEnumerator AnimateMove(string source, string target, Vector3 destination, Action onArrival)
    {
        Vector3 start = player.position;
        float duration = Mathf.Max(moveDuration, Vector3.Distance(start, destination) / 2.4f);
        float elapsed = 0f;
        while (elapsed < duration)
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
            avatar.SetFootPose(board.MovePose(source, target, Mathf.Clamp01(elapsed / duration)), true);
        }

        avatar.SetFootPose(board.MovePose(source, target, 1), true);
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
