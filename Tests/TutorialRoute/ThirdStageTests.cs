using System;
using System.IO;
using OLE.Core;

internal static class ThirdStageTests
{
    private static string folder;
    private static int assertions;

    public static void Run()
    {
        folder = Path.Combine(Path.GetTempPath(), "ole-completion-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        assertions = 0;
        try
        {
            CompletionWaitsForArrivalAndChoice();
            NextAdvancesOnlyOnce();
            RetryKeepsTheSelectedLevel();
            ManualWinningSaveRestoresTheChoice();
            FinalVictoryHasNoNextLevel();
            MenuContinuationKeepsItsPreviousBehaviour();
            SaveFailureDoesNotBlockCompletion();
            Console.WriteLine("Completion actions: 7 scenarios passed, " + assertions + " assertions.");
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

    private static void Walk(GameProgress progress, string target)
    {
        Check(progress.Session.TryStartMove(target, out Move move), "Begin a move to " + target);
        Check(progress.CompleteMove(move), "Arrive at " + target);
    }

    private static void FinishLevel(GameProgress progress)
    {
        for (int actions = 0; actions < 64 && !progress.Session.IsLevelComplete; actions++)
        {
            HintStep hint = progress.Session.GetHint();
            Check(hint != null, "An unfinished level has a route");
            if (hint.Kind == HintKind.Toggle)
                Check(progress.TryToggleMode(), "A route can change mode");
            else
                Walk(progress, hint.Target);
        }
        Check(progress.Session.IsLevelComplete, "Finish within the action limit");
    }

    private static GameSession Read(string path)
    {
        var game = TutorialCampaign.CreateSession();
        Check(new SaveStore(path).LoadInto(game) == SaveResult.Success, "Read a saved checkpoint");
        return game;
    }

    private static void CompletionWaitsForArrivalAndChoice()
    {
        var progress = Progress("arrival");
        progress.StartNewGame();
        Walk(progress, "P2");
        Walk(progress, "P3");
        Check(progress.TryToggleMode(), "Switch to the goal mode");
        Check(progress.Session.TryStartMove("P4", out Move goal), "Begin the final move");
        Check(!progress.Session.IsLevelComplete && !progress.TryNextLevel(), "No victory or next level before arrival");
        Check(progress.CompleteMove(goal), "The goal arrival is accepted");
        Check(progress.Session.IsLevelComplete && progress.Session.LevelNumber == 1
            && progress.Session.LevelCount == 5 && progress.Session.HasNextLevel,
            "The completed first level waits for the player's choice");
        string checkpoint = File.ReadAllText(Automatic("arrival"));
        Check(!progress.CompleteMove(goal) && !progress.Session.TryStartMove("P3", out _),
            "Repeated arrival and backward movement cannot leave the finished state");
        Check(File.ReadAllText(Automatic("arrival")) == checkpoint, "Rejected actions preserve the winning save");
        var saved = Read(Automatic("arrival"));
        Check(saved.IsLevelComplete && saved.LevelNumber == 1, "The automatic checkpoint records victory without skipping ahead");
    }

    private static void NextAdvancesOnlyOnce()
    {
        var progress = Progress("next");
        progress.StartNewGame();
        FinishLevel(progress);
        Check(progress.TryNextLevel(), "The Next action accepts a completed level");
        Check(progress.Session.Level.Id == "crossroads" && progress.Session.LevelNumber == 2
            && progress.Session.LevelCount == 5 && progress.Session.Platform == "P1"
            && progress.Session.Mode == BoardMode.A && !progress.Session.IsLevelComplete,
            "The second level opens at its start");
        Check(!progress.TryNextLevel() && progress.Session.LevelNumber == 2,
            "A repeated Next action cannot skip the second level");
        Check(Read(Automatic("next")).LevelNumber == 2, "Next persists the selected level");
    }

    private static void RetryKeepsTheSelectedLevel()
    {
        var progress = Progress("retry");
        progress.StartNewGame();
        FinishLevel(progress);
        Check(progress.TryNextLevel(), "Reach the second level");
        FinishLevel(progress);
        Check(progress.Save() == SaveResult.Success, "Keep a manual checkpoint of the completed second level");
        progress.RestartLevel();
        Check(progress.Session.LevelNumber == 2 && progress.Session.LevelCount == 5
            && progress.Session.Platform == "P1" && progress.Session.Mode == BoardMode.A
            && !progress.Session.IsLevelComplete && !progress.Session.IsPaused,
            "Retry starts the same level and clears completion");
        Check(Read(Automatic("retry")).Platform == "P1", "Retry updates automatic progress");
        var manual = Read(Manual("retry"));
        Check(manual.LevelNumber == 2 && manual.IsLevelComplete, "Retry preserves the manual winning checkpoint");
    }

    private static void ManualWinningSaveRestoresTheChoice()
    {
        var progress = Progress("load");
        progress.StartNewGame();
        FinishLevel(progress);
        Check(progress.Save() == SaveResult.Success && progress.TryNextLevel(), "Save victory and move on");
        progress.Session.Pause();
        Check(progress.Load(keepPaused: true) == SaveResult.Success && progress.Session.IsPaused
            && progress.Session.IsLevelComplete && progress.Session.LevelNumber == 1,
            "Manual loading restores the winning level instead of advancing");
        // GameplayController restores time before displaying the completion menu.
        progress.Session.Resume();
        Check(progress.Session.IsLevelComplete && progress.TryNextLevel() && progress.Session.LevelNumber == 2,
            "The restored completion can use the Next action");
    }

    private static void FinalVictoryHasNoNextLevel()
    {
        var progress = Progress("final");
        progress.StartNewGame();
        int count = progress.Session.LevelCount;
        for (int number = 1; number <= count; number++)
        {
            Check(progress.Session.LevelNumber == number, "Level counter follows the campaign");
            FinishLevel(progress);
            if (number < count)
                Check(progress.TryNextLevel(), "Advance to the following level");
        }
        Check(progress.Session.IsGameComplete && !progress.Session.HasNextLevel
            && progress.Session.LevelNumber == progress.Session.LevelCount,
            "Final victory has no next-level action");
        Check(!progress.TryNextLevel(), "Next cannot wrap the finished campaign");
        var restarted = Progress("final");
        Check(restarted.Continue() == SaveResult.Success && restarted.Session.IsGameComplete
            && restarted.Session.LevelNumber == count, "App restart retains the final victory");
        restarted.RestartLevel();
        Check(restarted.Session.LevelNumber == count && restarted.Session.Platform == "P1"
            && !restarted.Session.IsLevelComplete, "Final-level Retry stays on the last level");
    }

    private static void MenuContinuationKeepsItsPreviousBehaviour()
    {
        var progress = Progress("menu");
        progress.StartNewGame();
        FinishLevel(progress);
        progress.EnterMainMenu();
        Check(progress.Continue() == SaveResult.Success && progress.Session.LevelNumber == 2,
            "Main-menu Continue still advances an already completed level");
        Check(progress.Continue() == SaveResult.Success && progress.Session.LevelNumber == 2,
            "A second main-menu continuation cannot skip an unfinished level");
    }

    private static void SaveFailureDoesNotBlockCompletion()
    {
        Directory.CreateDirectory(Automatic("io"));
        var progress = Progress("io");
        progress.StartNewGame();
        FinishLevel(progress);
        Check(progress.LastAutosaveResult == SaveResult.IoError && progress.Session.IsLevelComplete,
            "Victory is reached even if writing the automatic file fails");
        Check(progress.TryNextLevel() && progress.Session.LevelNumber == 2,
            "A disk error does not block the Next action");
    }
}
