using OLE.Core;

internal static class TutorialCampaign
{
    public static GameSession CreateSession()
    {
        return new GameSession(new[] { CreateLevel(), CreateChallenge() });
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
}
