using OLE.Core;

namespace OLE.Examples
{
    public static class ExampleCampaign
    {
        // A second level lets the tests exercise campaign progression after the real tutorial.
        public static GameSession NewGame()
        {
            var bridge = new LevelDefinition("bridge", "Q1", "Q2",
                new[] { "Q1", "Q2" },
                new[] { new Connection("Q1", "Q2", openInA: false, openInB: true) });
            return new GameSession(new[] { TutorialCampaign.CreateLevel(), bridge });
        }
    }
}
