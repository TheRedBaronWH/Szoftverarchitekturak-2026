using OLE.Core;

internal static class TutorialCampaign
{
    public static GameSession CreateSession()
    {
        var platforms = new[] { "P1", "P2", "P3", "P4" };
        var connections = new[]
        {
            new Connection("P1", "P2", openInA: true, openInB: false),
            new Connection("P2", "P3", openInA: true, openInB: true),
            new Connection("P3", "P4", openInA: false, openInB: true)
        };

        var level = new LevelDefinition("tutorial", "P1", "P4", platforms, connections);
        return new GameSession(new[] { level });
    }
}
