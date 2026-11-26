using System;
using System.IO;
using OLE.Core;

internal static class RemainingIntegrationTests
{
    public static void Run()
    {
        for (int steps = 1; steps <= 32; steps++)
        {
            Check(StairProfile.Height(0, steps) == 0 && StairProfile.Height(1, steps) == 1, "Stair endpoints match the platforms");
            double previous = 0;
            for (int i = 0; i <= 1000; i++)
            {
                double height = StairProfile.Height(i / 1000.0, steps);
                Check(height >= previous && height >= 0 && height <= 1, "A staircase path is finite, bounded and monotone");
                previous = height;
            }
        }
        string folder = Path.Combine(Path.GetTempPath(), "ole-manual-choice-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var manual = new SaveStore(Path.Combine(folder, "manual.xml"));
            var automatic = new SaveStore(Path.Combine(folder, "automatic.xml"));
            var progress = new GameProgress(TutorialCampaign.CreateSession, manual, automatic);
            progress.StartNewGame();
            Walk(progress, "P2");
            Check(progress.Save() == SaveResult.Success, "Save the manual return point on tutorial P2/A");
            string manualBytes = File.ReadAllText(Path.Combine(folder, "manual.xml"));
            Finish(progress);
            Check(progress.TryNextLevel(), "Reach the second level after the manual save");
            Walk(progress, "P2");
            Check(progress.TryToggleMode(), "Create newer automatic state on Crossroads in B");
            Check(File.ReadAllText(Path.Combine(folder, "manual.xml")) == manualBytes, "New automatic progress cannot overwrite the manual file");
            var reopened = new GameProgress(TutorialCampaign.CreateSession, manual, automatic);
            Check(reopened.Continue() == SaveResult.Success && reopened.Session.Level.Id == "crossroads"
                && reopened.Session.Mode == BoardMode.B, "Continue loads newer automatic progress");
            reopened.Session.Pause();
            Check(reopened.Load(keepPaused: true) == SaveResult.Success && reopened.Session.Level.Id == "tutorial"
                && reopened.Session.Platform == "P2" && reopened.Session.Mode == BoardMode.A && reopened.Session.IsPaused,
                "Betoltes loads the older MANUAL point, including level, position, mode and pause");
            progress.StartNewGame();
            for (int i = 0; i < 3; i++) { Finish(progress); Check(progress.TryNextLevel(), "Unlock the next level"); }
            Check(progress.Session.Level.Id == "paradox-stairs" && progress.Session.Level.Platforms.Count == 12,
                "The final staircase has twelve platforms");
            Walk(progress,"P2"); Walk(progress,"P4"); Walk(progress,"P2");
            Check(progress.TryToggleMode(), "Switch for the second landing");
            Walk(progress,"P3"); Walk(progress,"P7"); Walk(progress,"P3");
            Check(progress.TryToggleMode(), "Switch to the central bridge"); Walk(progress,"P6");
            Check(progress.TryToggleMode(), "Switch for the return staircase"); Walk(progress,"P5");
            Check(progress.TryToggleMode(), "Explore the third side landing");
            Walk(progress,"P9"); Walk(progress,"P5"); Walk(progress,"P8");
            Check(progress.TryToggleMode(), "Climb to P11"); Walk(progress,"P11");
            Check(progress.Save() == SaveResult.Success, "Save the new level in the middle of its route");
            Check(progress.TryToggleMode(), "Cross the upper bridge"); Walk(progress,"P10");
            Check(progress.TryToggleMode(), "Open the last staircase"); Walk(progress,"P12");
            Check(progress.Session.IsLevelComplete && progress.Session.HasNextLevel, "All staircase side branches and its route remain playable");
            Check(progress.Load() == SaveResult.Success && progress.Session.Level.Id == "paradox-stairs"
                && progress.Session.Platform == "P11" && progress.Session.Mode == BoardMode.B,
                "The new level also restores the exact manual point rather than its newer automatic victory");
            Console.WriteLine("Stair profiles: 32 resolutions passed. Manual-load separation across levels passed.");
        }
        finally { Directory.Delete(folder, recursive: true); }
    }

    private static void Walk(GameProgress game, string target)
    {
        Check(game.Session.TryStartMove(target, out Move move) && game.CompleteMove(move), "Walk to " + target);
    }
    private static void Finish(GameProgress game)
    {
        for (int i = 0; i < 64 && !game.Session.IsLevelComplete; i++)
        {
            HintStep hint = game.Session.GetHint();
            if (hint.Kind == HintKind.Toggle) Check(game.TryToggleMode(), "Switch on the route");
            else Walk(game, hint.Target);
        }
        Check(game.Session.IsLevelComplete, "Hints solve the level");
    }
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
