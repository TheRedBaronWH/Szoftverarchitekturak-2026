using System;
using System.IO;
using OLE.Core;

internal static class FirstStageTests
{
    private static string folder;
    private static int assertions;

    public static void Run()
    {
        folder = Path.Combine(Path.GetTempPath(), "ole-pause-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        assertions = 0;
        try
        {
            PauseWhileStanding();
            PauseDuringMove();
            ContinueThroughMenu();
            PreferTheRunningSession();
            ContinueAfterAppRestart();
            SavingDuringMove();
            NewGameStartsFresh();
            RejectDamagedSaves();
            LoadingCancelsOldCallbacks();
            ContinueAfterVictory();
            Console.WriteLine("Pause and continuation: 10 scenarios passed, " + assertions + " assertions.");
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

    private static GameProgress Progress(string file)
    {
        return new GameProgress(TutorialCampaign.CreateSession, new SaveStore(Path.Combine(folder, file)));
    }

    private static void Walk(GameSession game, string target)
    {
        Check(game.TryStartMove(target, out Move move), "Move opens to " + target);
        Check(game.CompleteMove(move), "Move arrives at " + target);
    }

    private static void WinTutorial(GameSession game)
    {
        Walk(game, "P2");
        Walk(game, "P3");
        Check(game.TryToggleMode(), "Switch to B for the tutorial goal");
        Walk(game, "P4");
    }

    private static void PauseWhileStanding()
    {
        var progress = Progress("standing.xml");
        progress.StartNewGame();
        var game = progress.Session;
        game.Pause();
        Check(game.IsPaused && !game.IsMoving, "Standing game pauses");
        Check(!game.TryStartMove("P2", out _), "Pause blocks movement");
        Check(!game.TryToggleMode(), "Pause blocks mode changes");
        Check(!game.TryNextLevel(), "Pause blocks level changes");
        Check(game.GetHint() == null, "Pause blocks gameplay hints");
        game.Resume();
        Check(!game.IsPaused && game.Platform == "P1" && game.Mode == BoardMode.A, "Resume keeps state");
        Walk(game, "P2");
    }

    private static void PauseDuringMove()
    {
        var progress = Progress("moving.xml");
        progress.StartNewGame();
        var game = progress.Session;
        Check(game.TryStartMove("P2", out Move move), "Move begins before pause");
        game.Pause();
        Check(game.IsMoving && game.IsPaused && game.Platform == "P1", "Pause retains the pending move");
        Check(!game.CompleteMove(move), "Arrival cannot change state during pause");
        Check(!game.TryStartMove("P2", out _), "A second move is blocked during pause");
        game.Resume();
        Check(game.CompleteMove(move) && game.Platform == "P2", "The same move finishes after resume");
        Check(!game.CompleteMove(move), "A duplicate arrival is ignored");
    }

    private static void ContinueThroughMenu()
    {
        var progress = Progress("menu.xml");
        progress.StartNewGame();
        var game = progress.Session;
        Walk(game, "P2");
        Check(game.TryToggleMode(), "Menu test starts in B");
        Check(game.TryStartMove("P3", out Move interrupted), "Menu test starts a new move");
        game.Pause();
        progress.EnterMainMenu();
        Check(ReferenceEquals(progress.Session, game), "Main menu retains the running session");
        Check(game.Platform == "P2" && game.Mode == BoardMode.B && !game.IsMoving && !game.IsPaused,
            "Leaving the scene retains the last reached platform and mode");
        Check(progress.CheckContinuation() == SaveResult.Success, "Memory continuation needs no save file");
        Check(progress.Continue() == SaveResult.Success && ReferenceEquals(progress.Session, game),
            "Continue reuses the running session");
        Check(!game.CompleteMove(interrupted), "Old scene callback is ignored after return");
        Walk(game, "P3");
    }

    private static void PreferTheRunningSession()
    {
        var progress = Progress("preference.xml");
        progress.StartNewGame();
        Check(progress.Save() == SaveResult.Success, "Save P1");
        Walk(progress.Session, "P2");
        progress.EnterMainMenu();
        Check(progress.Continue() == SaveResult.Success && progress.Session.Platform == "P2",
            "Continue prefers live P2 over saved P1");
    }

    private static void ContinueAfterAppRestart()
    {
        var first = Progress("restart.xml");
        Check(first.CheckContinuation() == SaveResult.NotFound, "Fresh install has no continuation");
        first.StartNewGame();
        Walk(first.Session, "P2");
        Check(first.Session.TryToggleMode(), "Save mode B");
        Check(first.Save() == SaveResult.Success, "Save reached platform");

        var next = Progress("restart.xml");
        Check(next.Session == null, "A new application starts without a running session");
        Check(next.CheckContinuation() == SaveResult.Success && next.Session == null,
            "Save validation does not start the game");
        Check(next.Continue() == SaveResult.Success, "Continue loads the save");
        Check(next.Session.Level.Id == "tutorial" && next.Session.Platform == "P2"
            && next.Session.Mode == BoardMode.B && !next.Session.IsMoving && !next.Session.IsPaused,
            "Restart restores the saved level, platform and mode");
    }

    private static void SavingDuringMove()
    {
        var first = Progress("checkpoint.xml");
        first.StartNewGame();
        Check(first.Session.TryStartMove("P2", out Move move), "Begin checkpoint move");
        first.Session.Pause();
        Check(first.Save() == SaveResult.Success && first.Session.IsPaused && first.Session.IsMoving,
            "Saving preserves the paused running session");
        var restarted = Progress("checkpoint.xml");
        Check(restarted.Continue() == SaveResult.Success && restarted.Session.Platform == "P1"
            && !restarted.Session.IsMoving, "A file resumes at the last fully reached platform");
        first.Session.Resume();
        Check(first.Session.CompleteMove(move), "In-memory resume retains the unfinished move");
    }

    private static void NewGameStartsFresh()
    {
        var progress = Progress("new.xml");
        progress.StartNewGame();
        var previous = progress.Session;
        Walk(previous, "P2");
        Check(previous.TryStartMove("P3", out Move oldMove), "Begin old move");
        previous.Pause();
        progress.StartNewGame();
        Check(!ReferenceEquals(progress.Session, previous) && progress.Session.Level.Id == "tutorial"
            && progress.Session.Platform == "P1" && progress.Session.Mode == BoardMode.A
            && !progress.Session.IsPaused && !progress.Session.IsMoving, "New Game resets the campaign");
        previous.Resume();
        Check(!previous.CompleteMove(oldMove), "New Game invalidates old animation callbacks");
    }

    private static void RejectDamagedSaves()
    {
        File.WriteAllText(Path.Combine(folder, "bad.xml"), "<broken>");
        var progress = Progress("bad.xml");
        Check(progress.CheckContinuation() == SaveResult.InvalidData && progress.Session == null,
            "Damaged file is rejected before enabling Continue");
        Check(progress.Continue() == SaveResult.InvalidData && progress.Session == null,
            "Damaged file does not create a running game");
        progress.StartNewGame();
        Walk(progress.Session, "P2");
        var current = progress.Session;
        current.Pause();
        Check(progress.Load() == SaveResult.InvalidData && ReferenceEquals(current, progress.Session)
            && current.Platform == "P2" && current.IsPaused, "Failed load preserves the running game and pause");
        progress.EnterMainMenu();
        Check(progress.CheckContinuation() == SaveResult.Success && progress.Continue() == SaveResult.Success
            && progress.Session.Platform == "P2", "A damaged disk save does not block live continuation");
    }

    private static void LoadingCancelsOldCallbacks()
    {
        var progress = Progress("load.xml");
        progress.StartNewGame();
        Check(progress.Save() == SaveResult.Success, "Save initial state");
        var old = progress.Session;
        Check(old.TryStartMove("P2", out Move move), "Start move before loading");
        old.Pause();
        Check(progress.Load() == SaveResult.Success && !ReferenceEquals(progress.Session, old), "Load adopts a clean session");
        Check(progress.Session.Platform == "P1" && !progress.Session.IsMoving && !progress.Session.IsPaused,
            "Loaded state is standing and resumed");
        old.Resume();
        Check(!old.CompleteMove(move), "Arrival from the replaced session is invalidated");
    }

    private static void ContinueAfterVictory()
    {
        var progress = Progress("victory.xml");
        progress.StartNewGame();
        WinTutorial(progress.Session);
        progress.Session.Pause();
        Check(!progress.Session.TryNextLevel(), "Pause blocks a completed level transition");
        progress.EnterMainMenu();
        Check(progress.Continue() == SaveResult.Success && progress.Session.Level.Id == "crossroads"
            && progress.Session.Platform == "P1" && progress.Session.Mode == BoardMode.A,
            "Main-menu continuation advances a completed level");
        Check(progress.Continue() == SaveResult.Success && progress.Session.Level.Id == "crossroads",
            "A second continuation cannot skip an unfinished level");

        // A finished campaign must remain finished when Continue has no next level.
        for (int actions = 0; actions < 64 && !progress.Session.IsGameComplete; actions++)
        {
            var game = progress.Session;
            if (game.IsLevelComplete)
            {
                Check(game.TryNextLevel(), "A completed intermediate level can advance");
                continue;
            }
            HintStep hint = game.GetHint();
            Check(hint != null, "An unfinished level has a route");
            if (hint.Kind == HintKind.Toggle)
                Check(game.TryToggleMode(), "The route can change mode");
            else
                Walk(game, hint.Target);
        }
        Check(progress.Session.IsGameComplete, "The complete campaign is reached");
        progress.EnterMainMenu();
        Check(progress.Continue() == SaveResult.Success && progress.Session.IsGameComplete
            && progress.Session.Level.Id == "skybridge", "Continue keeps the final victory");

        progress.StartNewGame();
        WinTutorial(progress.Session);
        Check(progress.Save() == SaveResult.Success, "Save the completed tutorial");
        var restarted = Progress("victory.xml");
        Check(restarted.Continue() == SaveResult.Success && restarted.Session.Level.Id == "crossroads",
            "File continuation advances a completed level");
    }
}
