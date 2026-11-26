using System;

namespace OLE.Core
{
    // A completed level is shown briefly before advancing. Each completion fires once.
    public sealed class CompletionTimer
    {
        private readonly double delay;
        private GameSession tracked;
        private int levelNumber;
        private double elapsed;
        private bool fired;

        public double Remaining => Math.Max(0, delay - elapsed);

        public CompletionTimer(double delaySeconds = 2.5)
        {
            if (double.IsNaN(delaySeconds) || double.IsInfinity(delaySeconds) || delaySeconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(delaySeconds));
            delay = delaySeconds;
        }

        public void Reset()
        {
            tracked = null;
            elapsed = 0;
            fired = false;
        }

        public bool Tick(GameSession session, double deltaSeconds)
        {
            if (double.IsNaN(deltaSeconds) || double.IsInfinity(deltaSeconds) || deltaSeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
            if (session == null || !session.IsLevelComplete)
            {
                Reset();
                return false;
            }
            if (!ReferenceEquals(tracked, session) || levelNumber != session.LevelNumber)
            {
                Reset();
                tracked = session;
                levelNumber = session.LevelNumber;
            }
            if (session.IsPaused || fired)
                return false;
            elapsed += deltaSeconds;
            if (elapsed < delay)
                return false;
            fired = true;
            return true;
        }
    }
}
