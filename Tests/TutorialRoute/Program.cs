using System;
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
        Check(!game.HasNextLevel, "Crossroads is the final level");

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
        Check(game.IsGameComplete && !game.TryNextLevel(), "Campaign ends on P8");

        game.RestartLevel();
        Check(game.Level.Id == "crossroads" && game.Platform == "P1"
            && game.Mode == BoardMode.A, "Restart keeps the selected level");
        game.StartNewGame();
        Check(game.Level.Id == "tutorial" && game.Platform == "P1", "New game returns to tutorial");
        Console.WriteLine("Crossroads route passed.");
    }
}
