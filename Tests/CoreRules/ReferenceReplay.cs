using System;
using System.IO;
using OLE.Examples;

namespace OLE.Core.Tests
{
    internal static class ReferenceReplay
    {
        public static void Run()
        {
            using (Stream data = typeof(ReferenceReplay).Assembly.GetManifestResourceStream("OLE.Core.Tests.ReferenceTransitions.tsv"))
            using (var reader = new StreamReader(data))
            {
                reader.ReadLine(); // column headings
                int rows = 0;
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] cell = line.Split('\t');
                    GameSession game = ExampleCampaign.NewGame();
                    Move pending = null;
                    try
                    {
                        foreach (string step in cell[0].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                            Check.True(Apply(game, ref pending, step), "Setup step rejected: " + step);

                        Check.Equal(cell[2] == "1", Apply(game, ref pending, cell[1]));
                        Check.Equal(cell[3], game.Level.Id);
                        Check.Equal(cell[4], game.Platform);
                        Check.Equal(cell[5], game.Mode.ToString());
                        Check.Equal(cell[6] != "-", game.IsMoving);
                        Check.Equal(cell[6], pending?.To ?? "-");
                        Check.Equal(cell[7] == "1", game.IsLevelComplete);
                        Check.Equal(cell[8] == "1", game.IsGameComplete);
                    }
                    catch (Exception error)
                    {
                        throw new Exception($"Reference row {rows + 2}, trace '{cell[0]}', command '{cell[1]}'", error);
                    }
                    rows++;
                }
                Check.Equal(300, rows);
            }
        }

        private static bool Apply(GameSession game, ref Move pending, string command)
        {
            if (command.StartsWith("go:", StringComparison.Ordinal))
            {
                string target = command.Substring(3);
                if (!game.TryStartMove(target == "(null)" ? null : target, out Move requested)) return false;
                pending = requested;
                return true;
            }
            switch (command)
            {
                case "arrive":
                    if (!game.CompleteMove(pending)) return false;
                    pending = null;
                    return true;
                case "null": return game.CompleteMove(null);
                case "stale": return game.CompleteMove(Check.Start(ExampleCampaign.NewGame(), "P2"));
                case "switch": return game.TryToggleMode();
                case "restart": game.RestartLevel(); pending = null; return true;
                case "new": game.StartNewGame(); pending = null; return true;
                case "next": return game.TryNextLevel();
                default: throw new ArgumentException("Unknown reference command " + command);
            }
        }
    }
}
