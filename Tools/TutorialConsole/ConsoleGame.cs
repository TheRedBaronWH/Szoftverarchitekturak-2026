using System;
using System.IO;
using OLE.Core;

namespace OLE.Examples
{
    // A stand-in for input and animation while working without a Unity scene.
    internal static class ConsoleGame
    {
        public static int Run(GameSession game, bool challenge = false)
        {
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "NeckersParadox.Tutorial", challenge ? "challenge.xml" : "progress.xml");
            var store = challenge ? SaveStore.ForChallenge(path) : new SaveStore(path);
            if (challenge) Console.WriteLine("Challenge mode: local save integrity check enabled.");
            Move pending = null;
            Console.WriteLine("Commands: go P2 | arrive | switch | hint | restart | next | new | save | load | quit");
            Console.WriteLine("Demo save: " + path);
            while (true)
            {
                string phase = game.IsMoving ? "moving" : game.IsLevelComplete ? "completed" : "standing";
                Console.Write($"{game.Level.Id} / {game.Platform} / {game.Mode} / {phase} > ");
                string input = Console.ReadLine();
                if (input == null) return 0;
                string[] words = input.Trim().Split((char[])null, 2, StringSplitOptions.RemoveEmptyEntries);
                if (words.Length == 0) continue;
                string command = words[0].ToLowerInvariant();
                if (command != "go" && words.Length != 1)
                {
                    Console.WriteLine("Unexpected argument. Command cancelled.");
                    continue;
                }
                switch (command)
                {
                    case "go":
                        if (words.Length != 2) Console.WriteLine("Usage: go P2");
                        else if (game.TryStartMove(words[1].Trim(), out Move move))
                        {
                            pending = move;
                            Console.WriteLine($"{move.From} -> {move.To}. Type arrive to finish the move.");
                        }
                        else Console.WriteLine("Move rejected.");
                        break;
                    case "arrive":
                        if (!game.CompleteMove(pending)) Console.WriteLine("No current move.");
                        else if (game.IsGameComplete) Console.WriteLine("Campaign complete.");
                        else if (game.IsLevelComplete) Console.WriteLine("Level complete. Type next to continue.");
                        pending = null;
                        break;
                    case "switch":
                        if (!game.TryToggleMode()) Console.WriteLine("Switch rejected during movement.");
                        break;
                    case "hint":
                        HintStep hint = game.GetHint();
                        if (hint == null) Console.WriteLine(game.IsMoving ? "Finish this move first." : "No hint needed.");
                        else Console.WriteLine(hint.Kind == HintKind.Toggle ? "Hint: switch." : "Hint: go " + hint.Target);
                        break;
                    case "restart":
                        game.RestartLevel();
                        pending = null;
                        break;
                    case "new":
                        game.StartNewGame();
                        pending = null;
                        break;
                    case "next":
                        if (!game.TryNextLevel()) Console.WriteLine("Next level unavailable.");
                        break;
                    case "save":
                        ShowFileResult(store.Save(game), "Saved.");
                        break;
                    case "load":
                        SaveResult result = store.LoadInto(game);
                        if (result == SaveResult.Success) pending = null;
                        ShowFileResult(result, "Loaded.");
                        break;
                    case "quit": return 0;
                    default: Console.WriteLine("Unknown command."); break;
                }
            }
        }

        private static void ShowFileResult(SaveResult result, string success)
        {
            switch (result)
            {
                case SaveResult.Success: Console.WriteLine(success); break;
                case SaveResult.NotFound: Console.WriteLine("No saved game. Type new to start."); break;
                case SaveResult.InvalidData: Console.WriteLine("Damaged save. Type new to start."); break;
                case SaveResult.UnsupportedVersion: Console.WriteLine("Unsupported save version. Type new to start."); break;
                case SaveResult.IntegrityFailed: Console.WriteLine("Challenge save integrity check failed. Current game kept."); break;
                case SaveResult.KeyUnavailable: Console.WriteLine("Challenge key missing or damaged. Restore the key from backup."); break;
                case SaveResult.IoError: Console.WriteLine("The save file could not be accessed."); break;
            }
        }
    }
}
