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
    }
}
