using System.Collections.Generic;

namespace OLE.Core
{
    public enum HintKind { Move, Toggle }

    public sealed class HintStep
    {
        public HintKind Kind { get; }
        public string Target { get; }

        internal HintStep(HintKind kind, string target)
        {
            Kind = kind;
            Target = target;
        }
    }

    internal static class RouteHints
    {
        // One step costs one action: follow an open connection, or toggle on the spot.
        // Remember only the first action on each path; that is all the interface displays.
        internal static IEnumerable<(string Platform, HintStep First)> Walk(
            LevelDefinition level, string start, BoardMode mode)
        {
            var queue = new Queue<(string Platform, BoardMode Mode, HintStep First)>();
            var seen = new HashSet<(string, BoardMode)> { (start, mode) };
            queue.Enqueue((start, mode, null));
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                yield return (current.Platform, current.First);
                if (current.Platform == level.Goal) continue; // arrival ends movement on this level

                foreach (string next in level.OpenNeighbours(current.Platform, current.Mode))
                {
                    if (seen.Add((next, current.Mode)))
                        queue.Enqueue((next, current.Mode, current.First ?? new HintStep(HintKind.Move, next)));
                }

                BoardMode other = current.Mode == BoardMode.A ? BoardMode.B : BoardMode.A;
                if (seen.Add((current.Platform, other)))
                    queue.Enqueue((current.Platform, other, current.First ?? new HintStep(HintKind.Toggle, null)));
            }
        }
    }
}
