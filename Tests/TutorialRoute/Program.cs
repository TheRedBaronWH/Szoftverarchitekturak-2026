using System;
using System.IO;
using OLE.Core;

internal static class Program
{
    private static void Check(bool condition, string rule)
    {
        if (!condition)
            throw new InvalidOperationException(rule);
    }

    private static void Walk(GameSession game, string target)
    {
        Check(game.TryStartMove(target, out var move), "Expected a move to " + target);
        game.CompleteMove(move);
        Check(game.Platform == target, "Arrival at " + target);
    }

    private static void FinishWithHints(GameSession game)
    {
        for (int actions = 0; actions < 64 && !game.IsLevelComplete; actions++)
        {
            HintStep hint = game.GetHint();
            Check(hint != null, "An unfinished level has a route to the goal");
            if (hint.Kind == HintKind.Toggle)
                Check(game.TryToggleMode(), "Hint can toggle the mode");
            else
                Walk(game, hint.Target);
        }
        Check(game.IsLevelComplete, "Hints reach the goal within the action limit");
    }

    public static void Main()
    {
        var game = TutorialCampaign.CreateSession();
        Check(game.Platform == "P1" && game.Mode == BoardMode.A, "Initial state");
        Check(!game.TryNextLevel(), "Next level is locked before victory");
        Check(!game.TryStartMove("P4", out _), "A non-neighbour is rejected");

        Check(game.TryStartMove("P2", out var interrupted), "P1 to P2 is open");
        Check(!game.TryStartMove("P2", out _), "A second move is rejected in transit");
        Check(!game.TryToggleMode(), "A mode change is rejected in transit");
        Check(game.Platform == "P1", "Position changes only on arrival");

        game.RestartLevel();
        game.CompleteMove(interrupted);
        Check(!game.IsMoving && game.Platform == "P1", "Old arrival after restart is ignored");

        Walk(game, "P2");
        Walk(game, "P3");
        Check(!game.TryStartMove("P4", out _), "Goal edge is closed in A");
        Check(game.TryToggleMode() && game.Mode == BoardMode.B, "Standing toggle A to B");
        Check(game.TryStartMove("P4", out var finish), "Goal edge is open in B");
        Check(!game.IsLevelComplete, "Victory waits for arrival");
        game.CompleteMove(finish);
        Check(game.IsLevelComplete && game.Platform == "P4", "Victory on P4");
        Console.WriteLine("Tutorial route passed.");

        Check(game.HasNextLevel && !game.IsGameComplete, "Challenge follows the tutorial");
        Check(game.TryNextLevel(), "Victory unlocks the next level");
        Check(game.Level.Id == "crossroads" && game.Platform == "P1"
            && game.Mode == BoardMode.A && !game.IsMoving, "Next level starts in a clean state");
        Check(game.HasNextLevel, "Skybridge follows Crossroads");

        Walk(game, "P2");
        Walk(game, "P3");
        Check(game.TryToggleMode(), "Branch switches to B");
        Walk(game, "P4");
        Check(!game.IsLevelComplete, "The old tutorial goal is a side branch here");
        Check(!game.TryStartMove("P8", out _), "The branch cannot jump to the goal");
        Walk(game, "P3");
        Check(game.TryToggleMode(), "Return switches to A");
        Walk(game, "P2");

        Check(!game.TryStartMove("P5", out _), "First climb requires B");
        Check(game.TryToggleMode(), "Climb switches to B");
        Walk(game, "P5");
        Check(!game.TryStartMove("P6", out _), "Second climb requires A");
        Check(game.TryToggleMode(), "Second climb switches to A");
        Walk(game, "P6");
        Check(!game.TryStartMove("P7", out _), "Upper bridge requires B");
        Check(game.TryToggleMode(), "Bridge switches to B");
        Walk(game, "P7");
        Check(!game.TryStartMove("P8", out _), "Final edge requires A");
        Check(game.TryToggleMode(), "Goal switches to A");
        Walk(game, "P8");
        Check(game.IsLevelComplete && !game.IsGameComplete, "Crossroads ends on P8");

        game.RestartLevel();
        Check(game.Level.Id == "crossroads" && game.Platform == "P1"
            && game.Mode == BoardMode.A, "Restart keeps the selected level");
        FinishWithHints(game);
        Console.WriteLine("Crossroads route passed.");

        Check(game.TryNextLevel() && game.Level.Id == "skybridge", "Skybridge unlocks after Crossroads");
        Check(!game.HasNextLevel && game.Platform == "P1", "Skybridge starts at P1 and is the final level");
        Walk(game, "P2");
        Check(!game.TryStartMove("P3", out _), "Upper route requires B at P2");
        Walk(game, "P5");
        Check(game.TryToggleMode(), "Lower bridge switches to B");
        Walk(game, "P8");
        Check(!game.TryStartMove("P6", out _), "Return bridge requires A");
        Check(game.TryToggleMode(), "Return bridge switches to A");
        Walk(game, "P6");
        Walk(game, "P7");
        Check(game.TryToggleMode(), "Final island requires B");
        Walk(game, "P9");
        Check(game.TryToggleMode(), "Final platform requires A");
        Walk(game, "P10");
        Check(game.IsGameComplete && !game.TryNextLevel(), "Campaign ends on P10");

        game.RestartLevel();
        Walk(game, "P2");
        Check(game.TryToggleMode(), "Upper route switches to B");
        Walk(game, "P3");
        Check(game.TryToggleMode(), "Upper route switches to A");
        Walk(game, "P4");
        Check(game.TryToggleMode(), "Upper route rejoins in B");
        Walk(game, "P6");
        FinishWithHints(game);
        Check(game.IsGameComplete, "Both Skybridge routes reach the finish");
        Console.WriteLine("Skybridge routes and hints passed.");

        game.StartNewGame();
        Check(game.Level.Id == "tutorial" && game.Platform == "P1", "New game returns to tutorial");
        CheckCampaignSaves();
        FirstStageTests.Run();
        SecondStageTests.Run();
    }

    private static void CheckCampaignSaves()
    {
        string folder = Path.Combine(Path.GetTempPath(), "ole-campaign-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, "progress.xml");
        var store = new SaveStore(path);
        try
        {
            // An older one-level build writes the same version-1 save format.
            var older = new GameSession(new[] { TutorialCampaign.CreateLevel() });
            Walk(older, "P2");
            Check(store.Save(older) == SaveResult.Success, "Older tutorial can be saved");
            var restored = TutorialCampaign.CreateSession();
            Check(store.LoadInto(restored) == SaveResult.Success && restored.Platform == "P2"
                && restored.Level.Id == "tutorial", "Older saves load into the expanded campaign");

            FinishWithHints(restored);
            Check(restored.TryNextLevel(), "Loaded tutorial can advance");
            FinishWithHints(restored);
            Check(restored.TryNextLevel(), "Loaded campaign reaches Skybridge");
            Walk(restored, "P2");
            Walk(restored, "P5");
            Check(restored.TryToggleMode(), "Save test enters mode B");
            Walk(restored, "P8");
            Check(store.Save(restored) == SaveResult.Success, "Skybridge can be saved");

            var loaded = TutorialCampaign.CreateSession();
            Check(store.LoadInto(loaded) == SaveResult.Success && loaded.Level.Id == "skybridge"
                && loaded.Platform == "P8" && loaded.Mode == BoardMode.B && !loaded.IsMoving,
                "Continue restores the level, platform and mode");
            File.WriteAllText(path, "<broken>");
            Check(store.LoadInto(loaded) == SaveResult.InvalidData && loaded.Level.Id == "skybridge"
                && loaded.Platform == "P8" && loaded.Mode == BoardMode.B,
                "A damaged save preserves the running game");
            FinishWithHints(loaded);
            Check(loaded.IsGameComplete, "Restored game can finish");
            Console.WriteLine("Campaign saves and compatibility passed.");
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}
