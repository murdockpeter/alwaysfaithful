using System.Collections.Generic;

namespace AlwaysFaithful.Core
{
    public static class MovementPlanner
    {
        public static Dictionary<HexCoord, int> Reachable(
            IReadOnlyDictionary<HexCoord, TacticalCell> board,
            HexCoord origin,
            int movementPoints)
        {
            var costs = new Dictionary<HexCoord, int> { [origin] = 0 };
            var open = new List<HexCoord> { origin };
            while (open.Count > 0)
            {
                open.Sort((a, b) => Compare(a, b, costs));
                HexCoord current = open[0];
                open.RemoveAt(0);
                foreach (HexCoord neighbor in Neighbors(current))
                {
                    if (!board.TryGetValue(neighbor, out TacticalCell cell) || !cell.IsPassable) continue;
                    int candidate = costs[current] + cell.MovementCost;
                    if (candidate > movementPoints || costs.TryGetValue(neighbor, out int known) && candidate >= known) continue;
                    costs[neighbor] = candidate;
                    if (!open.Contains(neighbor)) open.Add(neighbor);
                }
            }
            return costs;
        }

        public static List<HexCoord> FindPath(
            IReadOnlyDictionary<HexCoord, TacticalCell> board,
            HexCoord origin,
            HexCoord destination,
            int movementPoints)
        {
            var costs = new Dictionary<HexCoord, int> { [origin] = 0 };
            var parents = new Dictionary<HexCoord, HexCoord>();
            var open = new List<HexCoord> { origin };
            while (open.Count > 0)
            {
                open.Sort((a, b) => Compare(a, b, costs));
                HexCoord current = open[0];
                open.RemoveAt(0);
                if (current.Equals(destination)) break;
                foreach (HexCoord neighbor in Neighbors(current))
                {
                    if (!board.TryGetValue(neighbor, out TacticalCell cell) || !cell.IsPassable) continue;
                    int candidate = costs[current] + cell.MovementCost;
                    if (candidate > movementPoints || costs.TryGetValue(neighbor, out int known) && candidate >= known) continue;
                    costs[neighbor] = candidate;
                    parents[neighbor] = current;
                    if (!open.Contains(neighbor)) open.Add(neighbor);
                }
            }

            if (!costs.ContainsKey(destination)) return new List<HexCoord>();
            var path = new List<HexCoord> { destination };
            while (!path[path.Count - 1].Equals(origin)) path.Add(parents[path[path.Count - 1]]);
            path.Reverse();
            return path;
        }

        public static IEnumerable<HexCoord> Neighbors(HexCoord coord)
        {
            yield return new HexCoord(coord.Q, coord.R - 1);
            yield return new HexCoord(coord.Q, coord.R + 1);
            int diagonal = (coord.Q & 1) == 0 ? -1 : 1;
            yield return new HexCoord(coord.Q - 1, coord.R);
            yield return new HexCoord(coord.Q + 1, coord.R);
            yield return new HexCoord(coord.Q - 1, coord.R + diagonal);
            yield return new HexCoord(coord.Q + 1, coord.R + diagonal);
        }

        private static int Compare(HexCoord a, HexCoord b, IReadOnlyDictionary<HexCoord, int> costs)
        {
            int byCost = costs[a].CompareTo(costs[b]);
            if (byCost != 0) return byCost;
            int byColumn = a.Q.CompareTo(b.Q);
            return byColumn != 0 ? byColumn : a.R.CompareTo(b.R);
        }
    }
}
