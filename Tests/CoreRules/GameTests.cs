using System;
using System.Collections.Generic;
using System.IO;
using OLE.Examples;

namespace OLE.Core.Tests
{
    internal static class GameTests
    {
        public static void Run()
        {
            Check.Case("Console rejects an unexpected restart argument without losing progress", ConsoleArguments);
            Check.Case("Hint guides the tutorial in four actions without changing state", HintRoute);
            Check.Case("Hint includes a required switch and follows a shortest route", ShortestHint);
            Check.Case("Dead ends and unreachable goals are handled at level creation", GoalValidation);
            Check.Case("FR-02  level starts at its start platform in A", StartState);
            Check.Case("FR-03/10  every pair of tutorial platforms in both modes", EveryTutorialEdge);
            Check.Case("FR-05  invalid and closed destinations leave the session unchanged", RejectInvalidMove);
            Check.Case("FR-06  a toggle changes only the mode and works in both directions", ToggleTwice);
            Check.Case("FR-06/10  changing mode changes which moves the session accepts", ModeAffectsMoves);
            Check.Case("FR-07  switching during motion is dropped and never replayed", RejectToggleInMotion);
            Check.Case("FR-05/07  a second move cannot displace one in progress", RejectOverlappingMove);
            Check.Case("FR-13  completion is recorded on arrival, once", CompleteOnArrival);
            Check.Case("FR-12  restart during motion invalidates callbacks even for an identical new move", RestartDuringMotion);
            Check.Case("FR-12  restarting a completed level restores the start and A", RestartAfterWin);
            Check.Case("FR-14  next level is locked before completion and before landing", NextLocked);
            Check.Case("FR-14  advance once and recognise the final victory", FinishCampaign);
            Check.Case("FR-02/12  new game always returns to the first level", NewGameResetsCampaign);
            Check.Case("A different session cannot finish this session's pending move", ForeignCallback);
            Check.Case("Reject inconsistent level definitions and duplicate campaign IDs", RejectInvalidDefinitions);
            Check.Case("Editing the input collections cannot change an existing level", DefensiveCopy);
            Check.Case("Independent reference table: 300 commands from 20 reachable states", ReferenceReplay.Run);
        }

        private static void ConsoleArguments()
        {
            TextReader previousInput = Console.In;
            TextWriter previousOutput = Console.Out;
            using (var script = new StringReader("go\tP2\narrive\nrestart extra\nquit\n"))
            using (var log = new StringWriter())
            {
                try
                {
                    Console.SetIn(script);
                    Console.SetOut(log);
                    Check.Equal(0, ConsoleGame.Run(ExampleCampaign.NewGame()));
                }
                finally
                {
                    Console.SetIn(previousInput);
                    Console.SetOut(previousOutput);
                }
                string output = log.ToString();
                Check.True(output.Contains("Unexpected argument. Command cancelled."));
                Check.True(output.EndsWith("tutorial / P2 / A / standing > ", StringComparison.Ordinal));
            }
        }

        private static void HintRoute()
        {
            GameSession game = ExampleCampaign.NewGame();
            int moves = 0, switches = 0;
            while (!game.IsLevelComplete && moves + switches < 8)
            {
                string platform = game.Platform;
                BoardMode mode = game.Mode;
                HintStep hint = game.GetHint();
                Check.True(hint != null);
                Check.Equal(platform, game.Platform);
                Check.Equal(mode, game.Mode);
                Check.False(game.IsMoving);
                if (hint.Kind == HintKind.Toggle)
                {
                    switches++;
                    Check.True(game.TryToggleMode());
                }
                else
                {
                    moves++;
                    Move pending = Check.Start(game, hint.Target);
                    Check.True(game.GetHint() == null);
                    Check.True(game.CompleteMove(pending));
                }
            }
            Check.Equal(3, moves);
            Check.Equal(1, switches);
            Check.True(game.IsLevelComplete);
            Check.True(game.GetHint() == null);
        }

        private static void ShortestHint()
        {
            var level = new LevelDefinition("choice", "S", "G", new[] { "S", "X", "Y", "G" },
                new[] { new Connection("S", "X", true, true), new Connection("X", "Y", true, true),
                        new Connection("Y", "G", true, true), new Connection("S", "G", false, true) });
            var game = new GameSession(new[] { level });
            Check.Equal(HintKind.Toggle, game.GetHint().Kind);
            Check.True(game.TryToggleMode());
            HintStep next = game.GetHint();
            Check.Equal(HintKind.Move, next.Kind);
            Check.Equal("G", next.Target);
        }

        private static void GoalValidation()
        {
            Check.Throws<ArgumentException>(() => new LevelDefinition("broken", "S", "G", new[] { "S", "G" },
                new[] { new Connection("S", "G", false, false) }));
        }

        private static void StartState()
        {
            GameSession game = ExampleCampaign.NewGame();
            Check.Equal("tutorial", game.Level.Id);
            Check.Equal("P1", game.Platform);
            Check.Equal(BoardMode.A, game.Mode);
            Check.False(game.IsMoving);
            Check.False(game.IsLevelComplete);
        }

        private static void EveryTutorialEdge()
        {
            LevelDefinition level = ExampleCampaign.NewGame().Level;
            // Matrix rows and columns are P1, P2, P3 and P4. Expected values come from Figure 1.
            string[] expectedA = { "0100", "1010", "0100", "0000" };
            string[] expectedB = { "0000", "0010", "0101", "0010" };
            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    string from = "P" + (row + 1);
                    string to = "P" + (col + 1);
                    Check.Equal(expectedA[row][col] == '1', level.CanTraverse(from, to, BoardMode.A));
                    Check.Equal(expectedB[row][col] == '1', level.CanTraverse(from, to, BoardMode.B));
                }
            }
        }

        private static void RejectInvalidMove()
        {
            GameSession game = ExampleCampaign.NewGame();
            foreach (string target in new[] { "P1", "P3", "missing", null })
            {
                Check.False(game.TryStartMove(target, out Move move));
                Check.True(move == null);
            }
            Check.True(game.TryToggleMode());
            Check.False(game.TryStartMove("P2", out _));
            Check.Equal("P1", game.Platform);
            Check.False(game.IsMoving);
        }

        private static void ToggleTwice()
        {
            GameSession game = ExampleCampaign.NewGame();
            Check.True(game.TryToggleMode());
            Check.Equal(BoardMode.B, game.Mode);
            Check.Equal("P1", game.Platform);
            Check.True(game.TryToggleMode());
            Check.Equal(BoardMode.A, game.Mode);
            Check.Equal("P1", game.Platform);
        }

        private static void ModeAffectsMoves()
        {
            GameSession game = ExampleCampaign.NewGame();
            Check.Travel(game, "P2");
            Check.Travel(game, "P3");
            Check.False(game.TryStartMove("P4", out _));
            Check.True(game.TryToggleMode());
            Check.Travel(game, "P2"); // the common edge is traversable in reverse in B
            Check.False(game.TryStartMove("P1", out _));
            Check.True(game.TryToggleMode());
            Check.Travel(game, "P1");
        }

        private static void RejectToggleInMotion()
        {
            GameSession game = ExampleCampaign.NewGame();
            Move move = Check.Start(game, "P2");
            Check.False(game.TryToggleMode());
            Check.Equal(BoardMode.A, game.Mode);
            Check.True(game.CompleteMove(move));
            Check.Equal(BoardMode.A, game.Mode);
            Check.True(game.TryToggleMode());
            Check.Equal(BoardMode.B, game.Mode);
        }

        private static void RejectOverlappingMove()
        {
            GameSession game = ExampleCampaign.NewGame();
            Move first = Check.Start(game, "P2");
            Check.False(game.TryStartMove("P3", out _));
            Check.False(game.TryStartMove("P2", out _));
            Check.Equal("P1", game.Platform);
            Check.True(game.CompleteMove(first));
            Check.Equal("P2", game.Platform);
        }

        private static void CompleteOnArrival()
        {
            GameSession game = ExampleCampaign.NewGame();
            Check.Travel(game, "P2");
            Check.Travel(game, "P3");
            Check.True(game.TryToggleMode());
            Move move = Check.Start(game, "P4");
            Check.Equal("P3", move.From);
            Check.Equal("P4", move.To);
            Check.False(game.IsLevelComplete);
            Check.True(game.CompleteMove(move));
            Check.True(game.IsLevelComplete);
            Check.False(game.IsGameComplete);
            Check.False(game.CompleteMove(move));
            Check.False(game.TryStartMove("P3", out _));
        }

        private static void RestartDuringMotion()
        {
            GameSession game = ExampleCampaign.NewGame();
            Move old = Check.Start(game, "P2");
            game.RestartLevel();
            Move current = Check.Start(game, "P2");
            Check.False(game.CompleteMove(old));
            Check.False(game.CompleteMove(null));
            Check.True(game.IsMoving);
            Check.True(game.CompleteMove(current));
            Check.Equal("P2", game.Platform);
        }

        private static void RestartAfterWin()
        {
            GameSession game = ExampleCampaign.NewGame();
            Check.SolveTutorial(game);
            game.RestartLevel();
            Check.Equal("tutorial", game.Level.Id);
            Check.Equal("P1", game.Platform);
            Check.Equal(BoardMode.A, game.Mode);
            Check.False(game.IsLevelComplete);
        }

        private static void NextLocked()
        {
            GameSession game = ExampleCampaign.NewGame();
            Check.False(game.TryNextLevel());
            Check.Travel(game, "P2");
            Check.Travel(game, "P3");
            Check.True(game.TryToggleMode());
            Move pending = Check.Start(game, "P4");
            Check.False(game.TryNextLevel());
            Check.True(game.CompleteMove(pending));
        }

        private static void FinishCampaign()
        {
            GameSession game = ExampleCampaign.NewGame();
            Check.SolveTutorial(game);
            Check.True(game.TryNextLevel());
            Check.Equal("bridge", game.Level.Id);
            Check.Equal("Q1", game.Platform);
            Check.Equal(BoardMode.A, game.Mode);
            Check.False(game.IsLevelComplete);
            Check.False(game.HasNextLevel);
            Check.True(game.TryToggleMode());
            Check.Travel(game, "Q2");
            Check.True(game.IsGameComplete);
            Check.False(game.TryNextLevel());
            Check.Equal("Q2", game.Platform);
        }

        private static void NewGameResetsCampaign()
        {
            GameSession game = ExampleCampaign.NewGame();
            Check.SolveTutorial(game);
            Check.True(game.TryNextLevel());
            game.StartNewGame();
            Check.Equal("tutorial", game.Level.Id);
            Check.Equal("P1", game.Platform);
            Check.Equal(BoardMode.A, game.Mode);
        }

        private static void ForeignCallback()
        {
            GameSession first = ExampleCampaign.NewGame();
            GameSession second = ExampleCampaign.NewGame();
            Move own = Check.Start(first, "P2");
            Move other = Check.Start(second, "P2");
            Check.False(first.CompleteMove(other));
            Check.True(first.CompleteMove(own));
            Check.False(second.IsLevelComplete);
            Check.True(second.IsMoving);
        }

        private static void RejectInvalidDefinitions()
        {
            string[] ids = { "X", "Y" };
            var edge = new Connection("X", "Y", true, false);
            Check.Throws<ArgumentException>(() => new Connection("X", "X", true, true));
            Check.Throws<ArgumentException>(() => new LevelDefinition("L", "X", "Z", ids, new[] { edge }));
            Check.Throws<ArgumentException>(() => new LevelDefinition("L", "X", "Y", new[] { "X", "Y", "X" }, new[] { edge }));
            Check.Throws<ArgumentException>(() => new LevelDefinition("L", "X", "Y", ids, new[] { new Connection("X", "Z", true, true) }));
            Check.Throws<ArgumentException>(() => new LevelDefinition("L", "X", "Y", ids, new[] { edge, new Connection("Y", "X", true, true) }));
            LevelDefinition level = ExampleCampaign.NewGame().Level;
            Check.Throws<ArgumentException>(() => new GameSession(new[] { level, level }));
        }

        private static void DefensiveCopy()
        {
            var ids = new List<string> { "X", "Y" };
            var edges = new List<Connection> { new Connection("X", "Y", true, false) };
            var level = new LevelDefinition("L", "X", "Y", ids, edges);
            ids.Clear();
            edges.Clear();
            Check.True(level.HasPlatform("Y"));
            Check.Equal(2, level.Platforms.Count);
            Check.True(level.CanTraverse("X", "Y", BoardMode.A));
        }
    }
}
