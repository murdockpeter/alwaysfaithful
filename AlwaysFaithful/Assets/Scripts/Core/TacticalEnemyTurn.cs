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
        Hold,
        // Appended, not inserted — TacticalEnemyActionEvent.Kind persists this
        // enum in save files, and inserting a member earlier would silently
        // renumber every already-saved historical Kind value.
        Recon,
        Hide,
        Search,
        Withdraw,
        Smoke,
        AreaSuppress
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

        // opponentContact is the AI's own already-computed, memory-carrying read
        // on the opponent (built by the caller via TacticalObservation.Check with
        // its own previous-turn contact passed in) — PlanOrder no longer computes
        // this itself, so every caller (planning, validation, execution) reasons
        // from the exact same contact rather than four independent fresh checks.
        public static TacticalAiOrder PlanOrder(
            IReadOnlyDictionary<HexCoord, TacticalMovementCell> board,
            TacticalUnitState unit,
            TacticalWeaponState weapon,
            HexCoord objective,
            TacticalUnitState opponent,
            TacticalContactState opponentContact,
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
            if (unit == null || opponent == null || board == null || opponentContact == null)
                return Hold(order, "Incomplete tactical state");

            if (unit.CanRally && unit.CombatStatus >= TacticalCombatStatus.Disrupted)
            {
                order.Kind = TacticalAiOrderKind.Recover;
                order.ActionPointCost = TacticalSuppression.RallyActionPointCost;
                order.Intent = "Restore combat effectiveness";
                return order;
            }

            if (unit.MoraleState == TacticalMoraleState.FallingBack || unit.MoraleState == TacticalMoraleState.Routed)
            {
                if (TacticalConcealmentMorale.TryChooseFallbackHex(board, unit, new[] { opponent }, out HexCoord fallback))
                {
                    order.Kind = TacticalAiOrderKind.Withdraw;
                    order.Destination = fallback;
                    order.ActionPointCost = TacticalConcealmentMorale.WithdrawActionPointCost;
                    order.Intent = "Break contact and restore cohesion";
                    return order;
                }
                return Hold(order, "No safe fallback route");
            }

            if (unit.CombatStatus == TacticalCombatStatus.Suppressed && unit.RemainingActionPoints >= TacticalFireAndManeuver.SmokeActionPointCost)
            {
                order.Kind = TacticalAiOrderKind.Smoke;
                order.Destination = unit.Position;
                order.ActionPointCost = TacticalFireAndManeuver.SmokeActionPointCost;
                order.Intent = "Screen a suppressed position before maneuver";
                return order;
            }

            TacticalFirePreview fire = TacticalDirectFire.Preview(board, unit.Id, unit.Position,
                opponentContact, opponent.Position, weapon, unit.RemainingActionPoints);
            if (unit.CanFire && fire.IsValid)
            {
                order.Kind = TacticalAiOrderKind.Fire;
                order.TargetId = opponent.Id;
                order.Destination = opponent.Position;
                order.ActionPointCost = TacticalDirectFire.ActionPointCost;
                order.Intent = "Engage observed opposing formation";
                return order;
            }

            if (unit.CanFire && opponentContact.State != TacticalVisibilityState.Hidden && !opponentContact.IsStale &&
                unit.RemainingActionPoints >= TacticalFireAndManeuver.SuppressionActionPointCost &&
                weapon.RemainingAmmunition >= TacticalFireAndManeuver.SuppressionAmmunitionCost &&
                HexCoord.Distance(unit.Position, opponentContact.LastKnownPosition) <= TacticalFireAndManeuver.SuppressionRangeHexes)
            {
                order.Kind = TacticalAiOrderKind.AreaSuppress;
                order.TargetId = opponent.Id;
                order.Destination = opponentContact.LastKnownPosition;
                order.ActionPointCost = TacticalFireAndManeuver.SuppressionActionPointCost;
                order.Intent = "Suppress a known area before advancing";
                return order;
            }

            // A recently lost track (still within its one-turn stale-carryover
            // window — see TacticalObservation.Check) is worth an active sensor
            // sweep on its last-known hex before falling back to blind advance,
            // the same reacquire-lost-contact behavior the player's own Recon
            // order already lets the USMC platoon do.
            if (opponentContact.IsStale && opponentContact.State != TacticalVisibilityState.Hidden &&
                unit.RemainingActionPoints >= TacticalRecon.ActionPointCost &&
                HexCoord.Distance(unit.Position, opponentContact.LastKnownPosition) <= TacticalRecon.MaximumRangeHexes)
            {
                order.Kind = TacticalAiOrderKind.Recon;
                order.TargetId = opponent.Id;
                order.Destination = opponentContact.LastKnownPosition;
                order.ActionPointCost = TacticalRecon.ActionPointCost;
                order.Intent = "Reacquire lost contact";
                return order;
            }

            if (opponentContact.State == TacticalVisibilityState.Contact && !opponentContact.IsStale &&
                HexCoord.Distance(unit.Position, opponentContact.LastKnownPosition) == TacticalConcealmentMorale.SearchRangeHexes &&
                unit.RemainingActionPoints >= TacticalConcealmentMorale.SearchActionPointCost)
            {
                order.Kind = TacticalAiOrderKind.Search;
                order.TargetId = opponent.Id;
                order.Destination = opponentContact.LastKnownPosition;
                order.ActionPointCost = TacticalConcealmentMorale.SearchActionPointCost;
                order.Intent = "Search suspected covered position";
                return order;
            }

            if (unit.CanMove)
            {
                Dictionary<HexCoord, int> reachable = TacticalMovementPlanner.Reachable(
                    board, unit.Position, unit.RemainingActionPoints, unit.Id, "PLA");
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
                        unit.RemainingActionPoints, unit.Id, "PLA");
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
            if (board.TryGetValue(unit.Position, out TacticalMovementCell currentCell) &&
                TacticalConcealmentMorale.CanHide(unit, currentCell))
            {
                order.Kind = TacticalAiOrderKind.Hide;
                order.ActionPointCost = TacticalConcealmentMorale.HideActionPointCost;
                order.Intent = "Prepare a concealed ambush";
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
            TacticalContactState opponentContact,
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
                    TacticalFirePreview fire = TacticalDirectFire.Preview(board, unit.Id, unit.Position,
                        opponentContact, opponent.Position, weapon, unit.RemainingActionPoints);
                    if (!fire.IsValid || order.TargetId != opponent.Id || !order.Destination.Equals(opponent.Position))
                        return Reject(out rejection, fire.RejectionReason ?? "Fire target mismatch");
                    return true;
                case TacticalAiOrderKind.Move:
                    TacticalRouteResult route = TacticalMovementPlanner.FindRoute(board, unit.Position,
                        order.Destination, unit.RemainingActionPoints, unit.Id, "PLA");
                    if (!route.IsValid || route.ActionPointCost != order.ActionPointCost)
                        return Reject(out rejection, route.RejectionReason ?? "Movement cost mismatch");
                    return true;
                case TacticalAiOrderKind.Observe:
                    TacticalLosResult los = TacticalLineOfSight.Inspect(board, unit.Position, order.Destination);
                    if (!los.IsValid || order.TargetId != opponent.Id)
                        return Reject(out rejection, los.RejectionReason ?? "Observation target mismatch");
                    return true;
                case TacticalAiOrderKind.Recon:
                    if (order.ActionPointCost != TacticalRecon.ActionPointCost ||
                        HexCoord.Distance(unit.Position, order.Destination) > TacticalRecon.MaximumRangeHexes)
                        return Reject(out rejection, "Recon target out of range");
                    return true;
                case TacticalAiOrderKind.Hide:
                    if (!board.TryGetValue(unit.Position, out TacticalMovementCell hideCell) ||
                        !TacticalConcealmentMorale.CanHide(unit, hideCell))
                        return Reject(out rejection, "Unit cannot conceal in this position");
                    return true;
                case TacticalAiOrderKind.Search:
                    if (order.ActionPointCost != TacticalConcealmentMorale.SearchActionPointCost ||
                        !TacticalConcealmentMorale.CanSearch(unit, order.Destination))
                        return Reject(out rejection, "Search target is not adjacent");
                    return true;
                case TacticalAiOrderKind.Withdraw:
                    if (order.ActionPointCost != TacticalConcealmentMorale.WithdrawActionPointCost ||
                        !board.TryGetValue(order.Destination, out TacticalMovementCell withdrawCell) || !withdrawCell.IsPassable ||
                        !string.IsNullOrEmpty(withdrawCell.OccupantId) && withdrawCell.OccupantId != unit.Id ||
                        HexCoord.Distance(unit.Position, order.Destination) != TacticalConcealmentMorale.WithdrawRangeHexes)
                        return Reject(out rejection, "Withdrawal route is not legal");
                    return true;
                case TacticalAiOrderKind.Smoke:
                    if (order.ActionPointCost != TacticalFireAndManeuver.SmokeActionPointCost ||
                        !order.Destination.Equals(unit.Position) || unit.RemainingActionPoints < order.ActionPointCost)
                        return Reject(out rejection, "Smoke order is not legal");
                    return true;
                case TacticalAiOrderKind.AreaSuppress:
                    if (order.ActionPointCost != TacticalFireAndManeuver.SuppressionActionPointCost ||
                        weapon.RemainingAmmunition < TacticalFireAndManeuver.SuppressionAmmunitionCost ||
                        unit.RemainingActionPoints < order.ActionPointCost ||
                        HexCoord.Distance(unit.Position, order.Destination) > TacticalFireAndManeuver.SuppressionRangeHexes)
                        return Reject(out rejection, "Area suppression is not legal");
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
            {
                terrain = cell.Terrain == TacticalTerrain.Rough ? -8 : cell.Terrain == TacticalTerrain.Highland ? -5 : 0;
                if (cell.ObstacleBreached) terrain -= 12;
                else if (cell.Obstacle != TacticalObstacleType.None &&
                         !string.Equals(cell.ObstacleOwnerSide, "PLA", StringComparison.OrdinalIgnoreCase)) terrain += 80;
            }
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
