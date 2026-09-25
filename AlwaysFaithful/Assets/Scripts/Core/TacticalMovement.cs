using System;
using System.Collections.Generic;

namespace AlwaysFaithful.Core
{
    [Serializable]
    public sealed class TacticalMovementCell
    {
        public HexCoord Coord;
        public TacticalTerrain Terrain;
        public float ElevationMetres;
        public TacticalCover Cover;
        public string OccupantId;
        public int SmokeExpiresAfterTurn;
        public TacticalObstacleType Obstacle;
        public TacticalObstacleIntelligence ObstacleIntelligence;
        public bool ObstacleBreached;
        public string ObstacleOwnerSide;

        public bool IsPassable => Terrain != TacticalTerrain.Water;
        public bool HasActiveSmoke(int turn) => SmokeExpiresAfterTurn >= turn;
    }

    [Serializable]
    public sealed class TacticalRouteResult
    {
        public bool IsValid;
        public string RejectionReason;
        public int ActionPointCost;
        public List<HexCoord> Path = new List<HexCoord>();
    }

    [Serializable]
    public sealed class TacticalMovementEvent
    {
        public int Sequence;
        public string BattlefieldId;
        public string UnitId;
        public HexCoord Origin;
        public HexCoord Destination;
        public int ActionPointCost;
        public int ActionPointsBefore;
        public int ActionPointsAfter;
        public string Outcome;
        public string Detail;
        public List<HexCoord> Path = new List<HexCoord>();
    }

    public static class TacticalMovementPlanner
    {
        public const float ImpassableElevationChangeMetres = 90f;

        public static Dictionary<HexCoord, int> Reachable(
            IReadOnlyDictionary<HexCoord, TacticalMovementCell> board,
            HexCoord origin,
            int actionPoints,
            string movingUnitId,
            string movingSide = null)
        {
            var costs = new Dictionary<HexCoord, int> { [origin] = 0 };
            var open = new List<HexCoord> { origin };
            while (open.Count > 0)
            {
                open.Sort((a, b) => Compare(a, b, costs));
                HexCoord current = open[0];
                open.RemoveAt(0);
                foreach (HexCoord neighbor in MovementPlanner.Neighbors(current))
                {
                    if (!TryEdgeCost(board, current, neighbor, movingUnitId, out int edgeCost, out _, movingSide)) continue;
                    int candidate = costs[current] + edgeCost;
                    if (candidate > actionPoints || costs.TryGetValue(neighbor, out int known) && candidate >= known) continue;
                    costs[neighbor] = candidate;
                    if (!open.Contains(neighbor)) open.Add(neighbor);
                }
            }
            return costs;
        }

        public static TacticalRouteResult FindRoute(
            IReadOnlyDictionary<HexCoord, TacticalMovementCell> board,
            HexCoord origin,
            HexCoord destination,
            int actionPoints,
            string movingUnitId,
            string movingSide = null)
        {
            var result = new TacticalRouteResult();
            if (origin.Equals(destination)) return Reject(result, "Already occupying destination");
            if (!board.TryGetValue(destination, out TacticalMovementCell target)) return Reject(result, "Outside battlefield");
            if (!target.IsPassable) return Reject(result, "Water is impassable");
            if (!string.IsNullOrEmpty(target.OccupantId) && target.OccupantId != movingUnitId) return Reject(result, "Destination occupied");

            var costs = new Dictionary<HexCoord, int> { [origin] = 0 };
            var parents = new Dictionary<HexCoord, HexCoord>();
            var open = new List<HexCoord> { origin };
            bool steepEdgeEncountered = false;
            while (open.Count > 0)
            {
                open.Sort((a, b) => Compare(a, b, costs));
                HexCoord current = open[0];
                open.RemoveAt(0);
                if (current.Equals(destination)) break;
                foreach (HexCoord neighbor in MovementPlanner.Neighbors(current))
                {
                    if (!TryEdgeCost(board, current, neighbor, movingUnitId, out int edgeCost, out string rejection, movingSide))
                    {
                        steepEdgeEncountered |= rejection == "Slope too steep";
                        continue;
                    }
                    int candidate = costs[current] + edgeCost;
                    if (candidate > actionPoints || costs.TryGetValue(neighbor, out int known) && candidate >= known) continue;
                    costs[neighbor] = candidate;
                    parents[neighbor] = current;
                    if (!open.Contains(neighbor)) open.Add(neighbor);
                }
            }
            if (!costs.TryGetValue(destination, out int cost))
                return Reject(result, steepEdgeEncountered ? "No legal route: steep terrain" : "Beyond remaining AP");

            result.IsValid = true;
            result.ActionPointCost = cost;
            result.Path.Add(destination);
            while (!result.Path[result.Path.Count - 1].Equals(origin)) result.Path.Add(parents[result.Path[result.Path.Count - 1]]);
            result.Path.Reverse();
            return result;
        }

        public static bool TryEdgeCost(
            IReadOnlyDictionary<HexCoord, TacticalMovementCell> board,
            HexCoord from,
            HexCoord to,
            string movingUnitId,
            out int cost,
            out string rejection,
            string movingSide = null)
        {
            cost = 0;
            rejection = null;
            if (!board.TryGetValue(from, out TacticalMovementCell origin) || !board.TryGetValue(to, out TacticalMovementCell destination))
            {
                rejection = "Outside battlefield";
                return false;
            }
            if (!destination.IsPassable)
            {
                rejection = "Water is impassable";
                return false;
            }
            if (!string.IsNullOrEmpty(destination.OccupantId) && destination.OccupantId != movingUnitId)
            {
                rejection = "Destination occupied";
                return false;
            }
            float elevationChange = Math.Abs(destination.ElevationMetres - origin.ElevationMetres);
            if (elevationChange > ImpassableElevationChangeMetres)
            {
                rejection = "Slope too steep";
                return false;
            }
            int terrainCost = destination.Terrain == TacticalTerrain.Open ? 1 : destination.Terrain == TacticalTerrain.Rough ? 2 : 3;
            int slopeCost = elevationChange >= 45f ? 2 : elevationChange >= 15f ? 1 : 0;
            string resolvedSide = !string.IsNullOrEmpty(movingSide) ? movingSide :
                movingUnitId != null && movingUnitId.IndexOf("pla", StringComparison.OrdinalIgnoreCase) >= 0 ? "PLA" : "USMC";
            int obstacleCost = string.Equals(destination.ObstacleOwnerSide, resolvedSide, StringComparison.OrdinalIgnoreCase)
                ? 0
                : TacticalObstacles.MovementCost(destination.Obstacle, destination.ObstacleIntelligence, destination.ObstacleBreached);
            cost = terrainCost + slopeCost + obstacleCost;
            return true;
        }

        private static TacticalRouteResult Reject(TacticalRouteResult result, string reason)
        {
            result.RejectionReason = reason;
            return result;
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
