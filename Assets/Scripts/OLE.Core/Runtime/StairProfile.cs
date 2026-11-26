using System;

namespace OLE.Core
{
    // Shared by the visible stairs and the character's foot path.
    public static class StairProfile
    {
        public static double Height(double position, int steps)
        {
            if (steps < 1) throw new ArgumentOutOfRangeException(nameof(steps));
            if (double.IsNaN(position) || double.IsInfinity(position))
                throw new ArgumentOutOfRangeException(nameof(position));
            if (position <= 0) return 0;
            if (position >= 1) return 1;
            double scaled = position * steps;
            int step = (int)Math.Floor(scaled);
            double rise = Math.Max(0, (scaled - step - 0.75) / 0.25);
            rise = rise * rise * (3 - 2 * rise);
            return (step + rise) / steps;
        }
    }
}
