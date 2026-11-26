using System;
using System.Collections.Generic;

namespace OLE.Core.Tests
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            if (args.Length == 2 && args[0] == "--interrupt-save") return SaveTests.InterruptWriter(args[1]);
            if (args.Length != 0)
            {
                Console.Error.WriteLine("Run with no arguments for tests or with --play for the console demo.");
                return 2;
            }

            GameTests.Run();
            SaveTests.Run();
            Console.WriteLine($"{Check.Passed} passed, {Check.Failed} failed.");
            return Check.Failed == 0 ? 0 : 1;
        }
    }

    internal static class Check
    {
        public static int Passed { get; private set; }
        public static int Failed { get; private set; }

        public static void Case(string name, Action test)
        {
            try
            {
                test();
                Passed++;
                Console.WriteLine("[PASS] " + name);
            }
            catch (Exception error)
            {
                Failed++;
                Console.Error.WriteLine("[FAIL] " + name + Environment.NewLine + error);
            }
        }

        public static void True(bool condition, string message = "Expected true.")
        {
            if (!condition) throw new Exception(message);
        }

        public static void False(bool condition, string message = "Expected false.") => True(!condition, message);

        public static void Equal<T>(T expected, T actual)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new Exception($"Expected '{expected}', got '{actual}'.");
        }

        public static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected " + typeof(T).Name);
        }

        public static Move Start(GameSession game, string target)
        {
            True(game.TryStartMove(target, out Move move), "Expected move to " + target);
            return move;
        }

        public static void Travel(GameSession game, string target)
        {
            True(game.CompleteMove(Start(game, target)));
            Equal(target, game.Platform);
        }

        public static void SolveTutorial(GameSession game)
        {
            Travel(game, "P2");
            Travel(game, "P3");
            True(game.TryToggleMode());
            Travel(game, "P4");
        }
    }
}
