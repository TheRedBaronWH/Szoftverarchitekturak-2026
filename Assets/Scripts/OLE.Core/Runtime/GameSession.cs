using System;
using System.Collections.Generic;

namespace OLE.Core
{
    // Each accepted move gets a unique object. An old animation callback cannot finish a new move.
    public sealed class Move
    {
        public string From { get; }
        public string To { get; }

        internal Move(string from, string to)
        {
            From = from;
            To = to;
        }
    }

    public sealed class GameSession
    {
        private readonly List<LevelDefinition> levels;
        private int levelIndex;
        private Move pendingMove;

        public LevelDefinition Level => levels[levelIndex];
        public string Platform { get; private set; }
        public BoardMode Mode { get; private set; }
        public bool IsMoving => pendingMove != null;
        public bool IsLevelComplete => Platform == Level.Goal;
        public bool HasNextLevel => levelIndex + 1 < levels.Count;
        public bool IsGameComplete => IsLevelComplete && !HasNextLevel;

        public GameSession(IEnumerable<LevelDefinition> levels)
        {
            if (levels == null) throw new ArgumentNullException(nameof(levels));
            this.levels = new List<LevelDefinition>(levels);
            if (this.levels.Count == 0)
                throw new ArgumentException("At least one level is required.", nameof(levels));
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (LevelDefinition level in this.levels)
            {
                if (level == null || !ids.Add(level.Id))
                    throw new ArgumentException("Each level needs a unique ID.", nameof(levels));
            }
            StartNewGame();
        }

        public bool TryStartMove(string target, out Move move)
        {
            move = null;
            if (IsMoving || IsLevelComplete || !Level.CanTraverse(Platform, target, Mode))
                return false;

            move = new Move(Platform, target);
            pendingMove = move;
            return true;
        }

        public bool CompleteMove(Move move)
        {
            if (move == null || !ReferenceEquals(move, pendingMove))
                return false;

            Platform = move.To;
            pendingMove = null;
            return true;
        }

        public bool TryToggleMode()
        {
            if (IsMoving) return false;
            Mode = Mode == BoardMode.A ? BoardMode.B : BoardMode.A;
            return true;
        }

        public HintStep GetHint()
        {
            if (IsMoving || IsLevelComplete) return null;
            foreach (var state in RouteHints.Walk(Level, Platform, Mode))
                if (state.Platform == Level.Goal) return state.First;
            return null;
        }

        public void RestartLevel() => SetPosition(levelIndex, Level.Start, BoardMode.A);

        public void StartNewGame() => SetPosition(0, levels[0].Start, BoardMode.A);

        public bool TryNextLevel()
        {
            if (!IsLevelComplete || !HasNextLevel) return false;
            int next = levelIndex + 1;
            SetPosition(next, levels[next].Start, BoardMode.A);
            return true;
        }

        internal bool TryRestore(string levelId, string platformId, BoardMode mode)
        {
            int index = levels.FindIndex(level => level.Id == levelId);
            if (index < 0 || !levels[index].CanRestoreAt(platformId)) return false;
            SetPosition(index, platformId, mode);
            return true;
        }

        private void SetPosition(int index, string platform, BoardMode mode)
        {
            levelIndex = index;
            Platform = platform;
            Mode = mode;
            pendingMove = null;
        }
    }
}
