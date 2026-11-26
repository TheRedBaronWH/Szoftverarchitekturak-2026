using System;
using OLE.Core;

internal static class CompletionTimerTests
{
    private static int assertions;
    public static void Run()
    {
        assertions = 0;
        var game = TutorialCampaign.CreateSession();
        var timer = new CompletionTimer(2);
        Check(!timer.Tick(game, 10), "An unfinished level cannot advance");
        Finish(game);
        Check(!timer.Tick(game, 1), "Victory waits for the delay");
        game.Pause();
        Check(!timer.Tick(game, 100) && timer.Remaining == 1, "Pause suspends the countdown");
        game.Resume();
        Check(timer.Tick(game, 1), "Completion fires after the remaining delay");
        Check(!timer.Tick(game, 20), "A completion cannot fire twice");
        Check(game.TryNextLevel() && !timer.Tick(game, 20), "Next starts without a stale countdown");
        Finish(game);
        Check(timer.Tick(game, 20), "A later completion has its own countdown");
        timer.Reset();
        Check(!timer.Tick(game, 1), "Explicit reset starts a fresh delay");
        var replacement = TutorialCampaign.CreateSession();
        Finish(replacement);
        Check(!timer.Tick(replacement, 1), "A loaded session cannot inherit elapsed time");
        Check(timer.Tick(replacement, 1), "The loaded victory can advance after its own delay");
        while (replacement.HasNextLevel)
        {
            Check(replacement.TryNextLevel(), "Advance toward the final completion");
            Finish(replacement);
        }
        timer.Reset();
        Check(!timer.Tick(replacement, 1) && timer.Tick(replacement, 1), "Final victory also fires its delayed event");
        Check(!timer.Tick(replacement, 10), "The ending event cannot be started twice");
        Console.WriteLine("Automatic progression and ending timer: " + assertions + " checks passed.");
    }

    private static void Finish(GameSession game)
    {
        for (int i = 0; i < 64 && !game.IsLevelComplete; i++)
        {
            HintStep hint = game.GetHint();
            if (hint.Kind == HintKind.Toggle) game.TryToggleMode();
            else if (game.TryStartMove(hint.Target, out Move move)) game.CompleteMove(move);
        }
        Check(game.IsLevelComplete, "Reach the goal");
    }

    private static void Check(bool condition, string message)
    {
        assertions++;
        if (!condition) throw new InvalidOperationException(message);
    }
}
