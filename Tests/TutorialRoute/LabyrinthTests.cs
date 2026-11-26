using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OLE.Core;

internal static class LabyrinthTests
{
    private static readonly HashSet<string> Pads = new HashSet<string> { "P4","P9","P14","P18" };
    private static int checks;

    public static void Run()
    {
        LevelDefinition level = TutorialCampaign.CreateLabyrinth();
        Check(level.Platforms.Count == 20 && level.Connections.Count == 22, "Twenty platforms and three independent graph cycles");
        Check(level.Connections.Count(e => e.First == "P6" || e.Second == "P6") == 1, "P6 is a genuine optional dead end");
        var start = ("P1",BoardMode.A);
        var reachable = Search(level,start,null);
        int shortest = reachable[(level.Goal,BoardMode.A)];
        Check(shortest >= 25, "The final solution requires a substantial detour, not sequential platform numbers");
        foreach (string pad in Pads)
            Check(!Search(level,start,pad).ContainsKey((level.Goal,BoardMode.A)), "Reaching switch " + pad + " is necessary in every solution");

        int solvableStates = 0;
        foreach (string platform in level.Platforms)
        foreach (BoardMode mode in new[] {BoardMode.A,BoardMode.B})
        {
            var game = new GameSession(new[] {level});
            bool expected = reachable.ContainsKey((platform,mode));
            Check(game.TryRestore(level.Id,platform,mode) == expected, "Restoration checks actual reachable platform/mode pairs");
            if (!expected) continue;
            var routes = Search(level,(platform,mode),null);
            Check(routes.Keys.Any(s => s.Item1 == level.Goal), "Every reachable state has an escape: no permanent softlock");
            int actions = 0;
            while (!game.IsLevelComplete && actions++ < 100)
            {
                HintStep hint = game.GetHint();
                Check(hint != null,"Hints also solve dead ends and revisited junctions");
                if (hint.Kind == HintKind.Toggle)
                    Check(Pads.Contains(game.Platform) && game.TryToggleMode(), "Hints never switch outside a pad");
                else Check(game.TryStartMove(hint.Target,out Move move) && game.CompleteMove(move),"A suggested move obeys the graph");
            }
            Check(game.IsLevelComplete,"Hints reach the goal from every reachable state");
            solvableStates++;
        }

        var walk = new GameSession(new[] {level});
        Check(!walk.TryToggleMode() && walk.Mode == BoardMode.A,"The initial platform is not a switch pad");
        Step(walk,"P5"); Step(walk,"P6");
        Check(walk.GetHint().Target == "P5", "A dead end leads back to its junction");
        Step(walk,"P5"); Step(walk,"P4");
        walk.Pause(); Check(!walk.TryToggleMode(),"Pause blocks a switch pad too"); walk.Resume();
        Check(walk.TryToggleMode(),"The original A/B command works on an allowed pad");
        Step(walk,"P5"); Step(walk,"P3"); Step(walk,"P7");
        Check(!level.CanTraverse("P7","P18",BoardMode.B),"Visually crossing walkways do not create an accidental graph junction");

        string folder = Path.Combine(Path.GetTempPath(), "ole-labyrinth-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var store = new SaveStore(Path.Combine(folder,"manual.xml"));
            Check(store.Save(walk) == SaveResult.Success,"The fifth level uses the existing save format");
            var campaign = TutorialCampaign.CreateSession();
            Check(store.LoadInto(campaign) == SaveResult.Success && campaign.LevelNumber == 5
                && campaign.Platform == "P7" && campaign.Mode == BoardMode.B,"Load restores the exact fifth-level position and mode");
            Check(!campaign.TryRestore(level.Id,"P6",BoardMode.B) && campaign.Platform == "P7",
                "An impossible mode at a dead end cannot be restored or replace the active game");
        }
        finally { Directory.Delete(folder,true); }
        Console.WriteLine("Labyrinth: "+shortest+" minimum actions; "+solvableStates+" reachable states all solvable; "+checks+" checks passed.");
    }

    // Independent reference search: use edge flags and this test's pad list,
    // without calling RouteHints or LevelDefinition.CanToggleAt.
    private static Dictionary<(string,BoardMode),int> Search(LevelDefinition level,(string,BoardMode) start,string disabledPad)
    {
        var found = new Dictionary<(string,BoardMode),int> { [start]=0 };
        var queue = new Queue<(string,BoardMode)>(); queue.Enqueue(start);
        while (queue.Count > 0)
        {
            var state=queue.Dequeue();
            if (state.Item1==level.Goal) continue;
            var nextStates=new List<(string,BoardMode)>();
            foreach (Connection edge in level.Connections)
                if ((state.Item2==BoardMode.A ? edge.OpenInA : edge.OpenInB)
                    && (edge.First==state.Item1 || edge.Second==state.Item1))
                    nextStates.Add((edge.First==state.Item1 ? edge.Second : edge.First,state.Item2));
            if (Pads.Contains(state.Item1) && state.Item1!=disabledPad)
                nextStates.Add((state.Item1,state.Item2==BoardMode.A ? BoardMode.B : BoardMode.A));
            foreach (var next in nextStates)
                if (!found.ContainsKey(next)) { found.Add(next,found[state]+1); queue.Enqueue(next); }
        }
        return found;
    }
    private static void Step(GameSession game,string target)
    {
        Check(game.TryStartMove(target,out Move move) && game.CompleteMove(move),"Walk to "+target);
    }
    private static void Check(bool result,string message)
    {
        checks++; if (!result) throw new InvalidOperationException(message);
    }
}
