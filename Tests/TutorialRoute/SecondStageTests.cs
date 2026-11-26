using System;
using System.IO;
using OLE.Core;

internal static class SecondStageTests
{
    private static string folder;
    private static int assertions;

    public static void Run()
    {
        folder = Path.Combine(Path.GetTempPath(), "ole-autosave-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        assertions = 0;
        try
        {
            NewGameIsAutomaticallySaved();
            ArrivalsAndModesAreSaved();
            ManualAndAutomaticSlotsStaySeparate();
            LoadingFromPauseKeepsPause();
            QuittingDuringMotionKeepsTheSource();
            LevelTransitionsAndRestartsAreSaved();
            NewGamePreservesTheManualSlot();
            DamagedAutosaveFallsBackToManual();
            OldManualSaveStillContinues();
            InvalidLoadsPreserveTheRunningGame();
            AutosaveFailureDoesNotBlockPlaying();
            ManualLoadDoesNotUseTheAutomaticSlot();
            Console.WriteLine("Save controls and autosave: 12 scenarios passed, " + assertions + " assertions.");
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    private static void Check(bool condition, string message)
    {
        assertions++;
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static string Manual(string test) => Path.Combine(folder, test + "-manual.xml");
    private static string Automatic(string test) => Path.Combine(folder, test + "-auto.xml");

    private static GameProgress Progress(string test)
    {
        return new GameProgress(TutorialCampaign.CreateSession,
            new SaveStore(Manual(test)), new SaveStore(Automatic(test)));
    }

    private static void SavedAt(string path, string level, string platform, BoardMode mode)
    {
        var restored = TutorialCampaign.CreateSession();
        Check(new SaveStore(path).LoadInto(restored) == SaveResult.Success, "The checkpoint is valid: " + path);
        Check(restored.Level.Id == level && restored.Platform == platform && restored.Mode == mode
            && !restored.IsMoving && !restored.IsPaused, "The checkpoint restores the reached state");
    }

    private static void Walk(GameProgress progress, string target)
    {
        Check(progress.Session.TryStartMove(target, out Move move), "Begin move to " + target);
        Check(progress.CompleteMove(move), "Arrive and save at " + target);
    }

    private static void FinishLevel(GameProgress progress)
    {
        for (int actions = 0; actions < 64 && !progress.Session.IsLevelComplete; actions++)
        {
            HintStep hint = progress.Session.GetHint();
            Check(hint != null, "A route to the goal exists");
            if (hint.Kind == HintKind.Toggle)
                Check(progress.TryToggleMode(), "Switch and save the mode");
            else
                Walk(progress, hint.Target);
        }
        Check(progress.Session.IsLevelComplete, "The level is completed within the action limit");
    }

    private static void NewGameIsAutomaticallySaved()
    {
        var first = Progress("new");
        first.Autosave();
        Check(!File.Exists(Automatic("new")), "Opening the app without a game writes nothing");
        first.StartNewGame();
        Check(first.LastAutosaveResult == SaveResult.Success && !File.Exists(Manual("new")),
            "New Game writes only the automatic slot");
        SavedAt(Automatic("new"), "tutorial", "P1", BoardMode.A);
        var restarted = Progress("new");
        string before = File.ReadAllText(Automatic("new"));
        Check(restarted.CheckContinuation() == SaveResult.Success && restarted.Session == null,
            "Continue is enabled without starting a session");
        Check(File.ReadAllText(Automatic("new")) == before, "Checking availability does not rewrite the checkpoint");
        Check(restarted.Continue() == SaveResult.Success && restarted.Session.Platform == "P1",
            "App restart continues without F5");
    }

    private static void ArrivalsAndModesAreSaved()
    {
        var progress = Progress("arrival");
        progress.StartNewGame();
        Check(progress.Session.TryStartMove("P2", out Move move), "Start an automatic move");
        SavedAt(Automatic("arrival"), "tutorial", "P1", BoardMode.A);
        Check(progress.CompleteMove(move), "Arrival is accepted");
        SavedAt(Automatic("arrival"), "tutorial", "P2", BoardMode.A);
        Check(progress.TryToggleMode(), "Mode change is accepted");
        SavedAt(Automatic("arrival"), "tutorial", "P2", BoardMode.B);
        string before = File.ReadAllText(Automatic("arrival"));
        progress.Session.Pause();
        Check(!progress.CompleteMove(move) && !progress.TryToggleMode() && !progress.TryNextLevel(),
            "Paused or stale commands are rejected");
        Check(File.ReadAllText(Automatic("arrival")) == before, "Rejected commands cannot alter the checkpoint");
    }

    private static void ManualAndAutomaticSlotsStaySeparate()
    {
        var progress = Progress("slots");
        progress.StartNewGame();
        Walk(progress, "P2");
        Check(progress.Save() == SaveResult.Success, "The player creates a manual checkpoint");
        Walk(progress, "P3");
        Check(progress.TryToggleMode(), "Advance automatic state to B");
        SavedAt(Manual("slots"), "tutorial", "P2", BoardMode.A);
        SavedAt(Automatic("slots"), "tutorial", "P3", BoardMode.B);
        var restarted = Progress("slots");
        Check(restarted.Continue() == SaveResult.Success && restarted.Session.Platform == "P3"
            && restarted.Session.Mode == BoardMode.B, "Continue prefers automatic progress");
        Check(restarted.Load() == SaveResult.Success && restarted.Session.Platform == "P2"
            && restarted.Session.Mode == BoardMode.A, "Load explicitly restores the manual checkpoint");
        SavedAt(Automatic("slots"), "tutorial", "P2", BoardMode.A);
        Check(Progress("slots").Continue() == SaveResult.Success, "The loaded state can be continued after quitting");
    }

    private static void LoadingFromPauseKeepsPause()
    {
        var progress = Progress("paused-load");
        progress.StartNewGame();
        Walk(progress, "P2");
        progress.Session.Pause();
        Check(progress.Save() == SaveResult.Success && progress.Session.IsPaused, "The Save button leaves the game paused");
        progress.Session.Resume();
        Check(progress.Session.TryStartMove("P3", out Move oldMove), "Start a move before loading");
        var previous = progress.Session;
        previous.Pause();
        Check(progress.Load(keepPaused: true) == SaveResult.Success && progress.Session.IsPaused
            && progress.Session.Platform == "P2" && !progress.Session.IsMoving,
            "The Load button restores the checkpoint while keeping the pause");
        previous.Resume();
        Check(!previous.CompleteMove(oldMove) && !progress.CompleteMove(oldMove), "The replaced animation cannot finish");
        SavedAt(Automatic("paused-load"), "tutorial", "P2", BoardMode.A);
        progress.Session.Resume();
        Walk(progress, "P3");
    }

    private static void QuittingDuringMotionKeepsTheSource()
    {
        var progress = Progress("quit");
        progress.StartNewGame();
        Walk(progress, "P2");
        Check(progress.Session.TryStartMove("P3", out Move move), "Start an unfinished move before quit");
        progress.Session.Pause();
        progress.Autosave();
        Check(progress.Session.IsMoving && progress.Session.IsPaused, "Saving does not complete or unpause motion");
        SavedAt(Automatic("quit"), "tutorial", "P2", BoardMode.A);
        var restarted = Progress("quit");
        Check(restarted.Continue() == SaveResult.Success && restarted.Session.Platform == "P2"
            && !restarted.Session.IsMoving && !restarted.Session.IsPaused, "Restart resumes from the last arrival");
        progress.EnterMainMenu();
        Check(!progress.CompleteMove(move) && progress.Session.Platform == "P2", "Leaving for the menu cancels old motion");
    }

    private static void LevelTransitionsAndRestartsAreSaved()
    {
        var progress = Progress("levels");
        progress.StartNewGame();
        FinishLevel(progress);
        SavedAt(Automatic("levels"), "tutorial", "P4", BoardMode.B);
        var restarted = Progress("levels");
        Check(restarted.Continue() == SaveResult.Success && restarted.Session.Level.Id == "crossroads",
            "Continue advances an automatically saved victory");
        SavedAt(Automatic("levels"), "crossroads", "P1", BoardMode.A);
        FinishLevel(restarted);
        Check(restarted.TryNextLevel(), "Next level is accepted");
        SavedAt(Automatic("levels"), "skybridge", "P1", BoardMode.A);
        FinishLevel(restarted);
        Check(restarted.TryNextLevel(), "Paradox Stairs unlocks after Skybridge");
        SavedAt(Automatic("levels"), "paradox-stairs", "P1", BoardMode.A);
        FinishLevel(restarted);
        var finished = Progress("levels");
        Check(finished.Continue() == SaveResult.Success && finished.Session.IsGameComplete,
            "The last victory stays completed after restart");
        finished.RestartLevel();
        SavedAt(Automatic("levels"), "paradox-stairs", "P1", BoardMode.A);
        Walk(finished, "P2");
        Check(finished.TryToggleMode(), "The next staircase edge requires B");
        Check(finished.Session.TryStartMove("P3", out Move interrupted), "Start a move before restart");
        finished.RestartLevel();
        Check(!finished.CompleteMove(interrupted), "Restart invalidates the old completion callback");
        SavedAt(Automatic("levels"), "paradox-stairs", "P1", BoardMode.A);
    }

    private static void NewGamePreservesTheManualSlot()
    {
        var progress = Progress("reset");
        progress.StartNewGame();
        Walk(progress, "P2");
        Check(progress.Save() == SaveResult.Success, "Create a manual checkpoint before New Game");
        Check(progress.Session.TryStartMove("P3", out Move oldMove), "Begin old campaign motion");
        progress.StartNewGame();
        Check(!progress.CompleteMove(oldMove), "New Game rejects old campaign callbacks");
        SavedAt(Automatic("reset"), "tutorial", "P1", BoardMode.A);
        SavedAt(Manual("reset"), "tutorial", "P2", BoardMode.A);
    }

    private static void DamagedAutosaveFallsBackToManual()
    {
        var progress = Progress("fallback");
        progress.StartNewGame();
        Walk(progress, "P2");
        Check(progress.Save() == SaveResult.Success, "Create a fallback checkpoint");
        File.WriteAllText(Automatic("fallback"), "<broken>");
        var restarted = Progress("fallback");
        Check(restarted.CheckContinuation() == SaveResult.Success && restarted.Session == null,
            "A valid manual slot keeps Continue available");
        Check(File.ReadAllText(Automatic("fallback")) == "<broken>", "Availability checking does not repair files");
        Check(restarted.Continue() == SaveResult.Success && restarted.UsedManualSaveFallback
            && restarted.Session.Platform == "P2", "Continue reports using the manual fallback");
        SavedAt(Automatic("fallback"), "tutorial", "P2", BoardMode.A);
        var next = Progress("fallback");
        Check(next.Continue() == SaveResult.Success && !next.UsedManualSaveFallback,
            "The repaired automatic slot works on the next restart");
    }

    private static void OldManualSaveStillContinues()
    {
        var older = TutorialCampaign.CreateSession();
        Check(older.TryStartMove("P2", out Move move) && older.CompleteMove(move), "Prepare a stage-one save");
        Check(new SaveStore(Manual("legacy")).Save(older) == SaveResult.Success, "Write the unchanged version-one format");
        var progress = Progress("legacy");
        Check(progress.Continue() == SaveResult.Success && !progress.UsedManualSaveFallback
            && progress.Session.Platform == "P2", "An older manual file continues without an automatic file");
        SavedAt(Automatic("legacy"), "tutorial", "P2", BoardMode.A);
    }

    private static void InvalidLoadsPreserveTheRunningGame()
    {
        File.WriteAllText(Automatic("invalid"), "<broken>");
        File.WriteAllText(Manual("invalid"), "<broken>");
        var progress = Progress("invalid");
        Check(progress.CheckContinuation() == SaveResult.InvalidData && progress.Continue() == SaveResult.InvalidData
            && progress.Session == null, "Two damaged slots cannot start a game");
        Check(File.ReadAllText(Automatic("invalid")) == "<broken>", "Failed continuation keeps the files unchanged");
        progress.StartNewGame();
        Walk(progress, "P2");
        var current = progress.Session;
        Check(current.TryStartMove("P3", out Move move), "Begin motion before a failed manual load");
        current.Pause();
        Check(progress.Load(keepPaused: true) == SaveResult.InvalidData && ReferenceEquals(current, progress.Session)
            && current.IsPaused && current.IsMoving, "Failed loading preserves the paused session and pending move");
        current.Resume();
        Check(progress.CompleteMove(move), "The original motion still finishes after a failed load");
    }

    private static void AutosaveFailureDoesNotBlockPlaying()
    {
        // A directory at the target filename causes an actual filesystem failure on every platform.
        Directory.CreateDirectory(Automatic("io"));
        var progress = Progress("io");
        progress.StartNewGame();
        Check(progress.LastAutosaveResult == SaveResult.IoError, "A failed autosave is reported");
        Walk(progress, "P2");
        Check(progress.Session.Platform == "P2" && progress.LastAutosaveResult == SaveResult.IoError,
            "Filesystem failure does not undo an accepted move");
        Check(progress.Save() == SaveResult.Success, "The separate manual slot can still be saved");
        progress.EnterMainMenu();
        Check(progress.Continue() == SaveResult.Success && progress.Session.Platform == "P2",
            "Memory continuation works while automatic saving is unavailable");
        var restarted = Progress("io");
        Check(restarted.Continue() == SaveResult.Success && restarted.UsedManualSaveFallback,
            "An unreadable automatic slot falls back to the manual slot");
        Directory.Delete(Automatic("io"));
        restarted.Autosave();
        Check(restarted.LastAutosaveResult == SaveResult.Success, "Saving recovers when the I/O problem is removed");
        SavedAt(Automatic("io"), "tutorial", "P2", BoardMode.A);
    }

    private static void ManualLoadDoesNotUseTheAutomaticSlot()
    {
        var progress = Progress("no-manual");
        progress.StartNewGame();
        Walk(progress, "P2");
        progress.Session.Pause();
        var current = progress.Session;
        Check(progress.CheckManualSave() == SaveResult.NotFound, "An automatic file does not enable manual loading");
        Check(progress.Load(keepPaused: true) == SaveResult.NotFound && ReferenceEquals(current, progress.Session)
            && current.Platform == "P2" && current.IsPaused, "Missing manual save leaves the game unchanged");
        var restarted = Progress("no-manual");
        Check(restarted.Continue() == SaveResult.Success && restarted.Session.Platform == "P2",
            "Main-menu continuation still uses automatic progress");
    }
}
