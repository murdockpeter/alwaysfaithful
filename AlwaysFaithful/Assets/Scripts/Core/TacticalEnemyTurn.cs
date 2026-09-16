using System;
using System.Collections.Generic;

namespace AlwaysFaithful.Core
{
    public enum TacticalAiOrderKind
    {
        Observe,
        Recover,
        Fire,
        Move,
        Hold
    }

    [Serializable]
    public sealed class TacticalAiOrder
    {
        public string UnitId;
        public TacticalAiOrderKind Kind;
        public HexCoord Origin;
        public HexCoord Destination;
        public string TargetId;
        public int ActionPointCost;
        public int Seed;
        public string Intent;
        public List<HexCoord> Path = new List<HexCoord>();
    }

    [Serializable]
    public sealed class TacticalEnemyActionEvent
    {
        public int Sequence;
        public string BattlefieldId;
        public int Turn;
        public string UnitId;
        public TacticalAiOrderKind Kind;
        public HexCoord Origin;
        public HexCoord Destination;
        public string TargetId;
        public bool WasVisible;
        public string Summary;
    }

    public static class TacticalEnemyTurn
    {
        public const int MaximumOrdersPerTurn = 16;
        public const int PlanningBudgetMilliseconds = 100;

        public static TacticalAiOrder PlanOrder(
            IReadOnlyDictionary<HexCoord, TacticalMovementCell> board,
            TacticalUnitState unit,
            TacticalWeaponState weapon,
            HexCoord objective,
            TacticalUnitState opponent,
            int turn,
            int seed)
        {
            var order = new TacticalAiOrder
            {
                UnitId = unit?.Id,
                Origin = unit?.Position ?? default,
                Destination = unit?.Position ?? default,
                Seed = seed
            };
            if (unit == null || opponent == null || board == null)
                return Hold(order, "Incomplete tactical state");

            if (unit.CanRally && unit.CombatStatus >= TacticalCombatStatus.Disrupted)
            {
                order.Kind = TacticalAiOrderKind.Recover;
                order.ActionPointCost = TacticalSuppression.RallyActionPointCost;
                order.Intent = "Restore combat effectiveness";
                return order;
            }

            TacticalContactState contact = TacticalObservation.Check(board, unit.Id, unit.Position,
                opponent.Id, opponent.DisplayName, opponent.Position, turn);
            TacticalFirePreview fire = TacticalDirectFire.Preview(board, unit.Id, unit.Position,
                contact, opponent.Position, weapon, unit.RemainingActionPoints);
            if (unit.CanFire && fire.IsValid)
            {
                order.Kind = TacticalAiOrderKind.Fire;
                order.TargetId = opponent.Id;
                order.Destination = opponent.Position;
                order.ActionPointCost = TacticalDirectFire.ActionPointCost;
                order.Intent = "Engage observed opposing formation";
                return order;
            }

            if (unit.CanMove)
            {
                Dictionary<HexCoord, int> reachable = TacticalMovementPlanner.Reachable(
                    board, unit.Position, unit.RemainingActionPoints, unit.Id);
                HexCoord destination = unit.Position;
                int currentDistance = HexCoord.Distance(unit.Position, objective);
                int bestScore = ScoreDestination(board, unit.Position, objective, seed);
                foreach (KeyValuePair<HexCoord, int> candidate in reachable)
                {
                    if (candidate.Key.Equals(unit.Position)) continue;
                    int score = ScoreDestination(board, candidate.Key, objective, seed);
                    if (score >= bestScore) continue;
                    destination = candidate.Key;
                    bestScore = score;
                }
                if (!destination.Equals(unit.Position) && HexCoord.Distance(destination, objective) <= currentDistance)
                {
                    TacticalRouteResult route = TacticalMovementPlanner.FindRoute(board, unit.Position, destination,
                        unit.RemainingActionPoints, unit.Id);
                    if (route.IsValid)
                    {
                        order.Kind = TacticalAiOrderKind.Move;
                        order.Destination = destination;
                        order.ActionPointCost = route.ActionPointCost;
                        order.Path = new List<HexCoord>(route.Path);
                        order.Intent = "Advance toward tactical objective";
                        return order;
                    }
                }
            }

            TacticalLosResult observation = TacticalLineOfSight.Inspect(board, unit.Position, opponent.Position);
            if (observation.IsValid)
            {
                order.Kind = TacticalAiOrderKind.Observe;
                order.TargetId = opponent.Id;
                order.Destination = opponent.Position;
                order.Intent = "Update observation picture";
                return order;
            }
            return Hold(order, "No legal action improves objective posture");
        }

        public static bool ValidateOrder(
            IReadOnlyDictionary<HexCoord, TacticalMovementCell> board,
            TacticalAiOrder order,
            TacticalUnitState unit,
            TacticalWeaponState weapon,
            TacticalUnitState opponent,
            int turn,
            out string rejection)
        {
            rejection = null;
            if (order == null || unit == null || order.UnitId != unit.Id || !order.Origin.Equals(unit.Position))
                return Reject(out rejection, "Order does not match active unit state");
            switch (order.Kind)
            {
                case TacticalAiOrderKind.Recover:
                    if (!unit.CanRally || order.ActionPointCost != TacticalSuppression.RallyActionPointCost)
                        return Reject(out rejection, "Recovery is not legal");
                    return true;
                case TacticalAiOrderKind.Fire:
                    TacticalContactState contact = TacticalObservation.Check(board, unit.Id, unit.Position,
                        opponent.Id, opponent.DisplayName, opponent.Position, turn);
                    TacticalFirePreview fire = TacticalDirectFire.Preview(board, unit.Id, unit.Position,
                        contact, opponent.Position, weapon, unit.RemainingActionPoints);
                    if (!fire.IsValid || order.TargetId != opponent.Id || !order.Destination.Equals(opponent.Position))
                        return Reject(out rejection, fire.RejectionReason ?? "Fire target mismatch");
                    return true;
                case TacticalAiOrderKind.Move:
                    TacticalRouteResult route = TacticalMovementPlanner.FindRoute(board, unit.Position,
                        order.Destination, unit.RemainingActionPoints, unit.Id);
                    if (!route.IsValid || route.ActionPointCost != order.ActionPointCost)
                        return Reject(out rejection, route.RejectionReason ?? "Movement cost mismatch");
                    return true;
                case TacticalAiOrderKind.Observe:
                    TacticalLosResult los = TacticalLineOfSight.Inspect(board, unit.Position, order.Destination);
                    if (!los.IsValid || order.TargetId != opponent.Id)
                        return Reject(out rejection, los.RejectionReason ?? "Observation target mismatch");
                    return true;
                case TacticalAiOrderKind.Hold:
                    return true;
                default:
                    return Reject(out rejection, "Unknown AI order");
            }
        }

        private static int ScoreDestination(IReadOnlyDictionary<HexCoord, TacticalMovementCell> board,
            HexCoord coord, HexCoord objective, int seed)
        {
            int terrain = 0;
            if (board.TryGetValue(coord, out TacticalMovementCell cell))
                terrain = cell.Terrain == TacticalTerrain.Rough ? -8 : cell.Terrain == TacticalTerrain.Highland ? -5 : 0;
            return HexCoord.Distance(coord, objective) * 100 + terrain + TieBreak(coord, seed);
        }

        private static int TieBreak(HexCoord coord, int seed)
        {
            unchecked
            {
                uint value = (uint)seed ^ ((uint)coord.Q * 73856093u) ^ ((uint)coord.R * 19349663u);
                value ^= value << 13;
                value ^= value >> 17;
                return (int)(value % 7u);
            }
        }

        private static TacticalAiOrder Hold(TacticalAiOrder order, string reason)
        {
            order.Kind = TacticalAiOrderKind.Hold;
            order.Intent = reason;
            return order;
        }

        private static bool Reject(out string rejection, string reason)
        {
            rejection = reason;
            return false;
        }
    }
}
