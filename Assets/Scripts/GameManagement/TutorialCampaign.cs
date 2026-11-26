using OLE.Core;

internal static class TutorialCampaign
{
    public static GameSession CreateSession()
    {
        return new GameSession(new[] { CreateLevel(), CreateChallenge(), CreateSkybridge(), CreateParadoxStairs(), CreateLabyrinth() });
    }

    internal static LevelDefinition CreateLevel()
    {
        var platforms = new[] { "P1", "P2", "P3", "P4" };
        var connections = new[]
        {
            new Connection("P1", "P2", openInA: true, openInB: false),
            new Connection("P2", "P3", openInA: true, openInB: true),
            new Connection("P3", "P4", openInA: false, openInB: true)
        };

        return new LevelDefinition("tutorial", "P1", "P4", platforms, connections);
    }

    private static LevelDefinition CreateChallenge()
    {
        var platforms = new[] { "P1", "P2", "P3", "P4", "P5", "P6", "P7", "P8" };
        var connections = new[]
        {
            // The route to P8 alternates A, B, A, B, A.
            new Connection("P1", "P2", openInA: true, openInB: false),
            new Connection("P2", "P5", openInA: false, openInB: true),
            new Connection("P5", "P6", openInA: true, openInB: false),
            new Connection("P6", "P7", openInA: false, openInB: true),
            new Connection("P7", "P8", openInA: true, openInB: false),
            // A side branch reaches P4 and requires returning to P2.
            new Connection("P2", "P3", openInA: true, openInB: false),
            new Connection("P3", "P4", openInA: false, openInB: true)
        };

        return new LevelDefinition("crossroads", "P1", "P8", platforms, connections);
    }

    private static LevelDefinition CreateParadoxStairs()
    {
        var platforms = new[] { "P1", "P2", "P3", "P4", "P5", "P6", "P7", "P8", "P9", "P10", "P11", "P12" };
        var edges = new[] {
            new Connection("P1", "P2", true, false), new Connection("P2", "P3", false, true),
            new Connection("P3", "P6", true, false), new Connection("P6", "P5", false, true),
            new Connection("P5", "P8", true, false), new Connection("P8", "P11", false, true),
            new Connection("P11", "P10", true, false), new Connection("P10", "P12", false, true),
            // Three side landings require returning to the main staircase.
            new Connection("P2", "P4", true, false), new Connection("P3", "P7", false, true),
            new Connection("P5", "P9", true, false) };
        return new LevelDefinition("paradox-stairs", "P1", "P12", platforms, edges);
    }

    private static LevelDefinition CreateSkybridge()
    {
        var platforms = new[] { "P1", "P2", "P3", "P4", "P5", "P6", "P7", "P8", "P9", "P10" };
        var connections = new[]
        {
            new Connection("P1", "P2", openInA: true, openInB: false),
            // The upper route needs a mode change on every bridge.
            new Connection("P2", "P3", openInA: false, openInB: true),
            new Connection("P3", "P4", openInA: true, openInB: false),
            new Connection("P4", "P6", openInA: false, openInB: true),
            // The lower route rejoins at P6 with fewer mode changes.
            new Connection("P2", "P5", openInA: true, openInB: false),
            new Connection("P5", "P8", openInA: false, openInB: true),
            new Connection("P8", "P6", openInA: true, openInB: false),
            new Connection("P6", "P7", openInA: true, openInB: false),
            new Connection("P7", "P9", openInA: false, openInB: true),
            new Connection("P9", "P10", openInA: true, openInB: false)
        };

        return new LevelDefinition("skybridge", "P1", "P10", platforms, connections);
    }

    internal static LevelDefinition CreateLabyrinth()
    {
        var platforms = new string[20];
        for (int i = 0; i < platforms.Length; i++) platforms[i] = "P" + (i + 1);
        var edges = new[] {
            // Lower loop and a genuine dead end. P4 is the first switch pad.
            new Connection("P1","P2",true,false), new Connection("P2","P3",true,false),
            new Connection("P3","P5",true,true), new Connection("P5","P1",true,false),
            new Connection("P5","P6",true,false), new Connection("P5","P4",true,true),
            new Connection("P3","P7",false,true),
            // Middle loop: switch at P9, then return through P11/P10/P8 in A.
            new Connection("P7","P8",false,true), new Connection("P8","P10",true,true),
            new Connection("P10","P11",true,true), new Connection("P11","P7",false,true),
            new Connection("P11","P9",true,true), new Connection("P8","P12",true,false),
            // Upper loop: P14 lies beyond the side landing; turning back is necessary.
            new Connection("P12","P13",true,false), new Connection("P13","P15",true,true),
            new Connection("P15","P16",true,true), new Connection("P16","P12",true,false),
            new Connection("P16","P14",true,true), new Connection("P13","P17",false,true),
            // The apparent finish is blocked in B. P18 opens it from a side branch.
            new Connection("P17","P19",true,true), new Connection("P19","P18",true,true),
            new Connection("P19","P20",true,false)
        };
        return new LevelDefinition("woven-labyrinth", "P1", "P20", platforms, edges,
            togglePlatforms: new[] { "P4", "P9", "P14", "P18" });
    }
}
