using System;
using System.Collections.Generic;

namespace AlwaysFaithful.Core
{
    public enum TacticalMoraleState
    {
        Steady,
        FallingBack,
        Routed,
        Surrendered,
        Withdrawn
    }

    public enum TacticalConcealmentEventKind
    {
        Hide,
        Reveal,
        Search,
        Ambush,
        FallBack,
        Rout,
        Surrender,
        Withdraw
    }

    [Serializable]
    public sealed class TacticalConcealmentEvent
    {
        public int Sequence;
        public string BattlefieldId;
        public int Turn;
        public TacticalConcealmentEventKind Kind;
        public string UnitId;
        public string TargetId;
        public HexCoord Origin;
        public HexCoord Destination;
        public int ActionPointCost;
        public string Summary;
    }

    public static class TacticalConcealmentMorale
    {
        public const int HideActionPointCost = 2;
        public const int SearchActionPointCost = 2;
        public const int WithdrawActionPointCost = 2;
        public const int SearchRangeHexes = 1;
        public const int WithdrawRangeHexes = 1;
        public const int AmbushHitModifier = 20;
        public const int AmbushSuppressionModifier = 15;
        public const int WithdrawSuppressionRecovery = 10;

        public static bool HasConcealingCover(TacticalMovementCell cell)
            => cell != null && (cell.Cover == TacticalCover.Medium || cell.Cover == TacticalCover.Heavy);

        public static bool CanHide(TacticalUnitState unit, TacticalMovementCell cell)
            => unit != null && unit.CanMove && !unit.MovedThisTurn && !unit.FiredThisTurn && !unit.IsConcealed &&
               unit.RemainingActionPoints >= HideActionPointCost && HasConcealingCover(cell);

        public static bool TryHide(TacticalUnitState unit, TacticalMovementCell cell, int turn)
        {
            if (!CanHide(unit, cell) || !unit.TrySpendActionPoints(HideActionPointCost)) return false;
            unit.IsConcealed = true;
            unit.AmbushReady = true;
            unit.ConcealedTurn = turn;
            unit.ReactionPolicy = TacticalReactionPolicy.WeaponsFree;
            return true;
        }

        public static bool Reveal(TacticalUnitState unit)
        {
            if (unit == null || !unit.IsConcealed) return false;
            unit.IsConcealed = false;
            unit.AmbushReady = false;
            return true;
        }

        public static TacticalVisibilityState ApplyConcealment(
            TacticalVisibilityState state, TacticalUnitState target, int rangeHexes)
        {
            if (target == null || !target.IsConcealed || target.FiredThisTurn || state == TacticalVisibilityState.Hidden)
                return state;
            int reduction = rangeHexes <= SearchRangeHexes ? 1 : 2;
            return (TacticalVisibilityState)Math.Max((int)TacticalVisibilityState.Hidden, (int)state - reduction);
        }

        public static void ApplyAmbushModifier(TacticalFirePreview preview, TacticalUnitState attacker)
        {
            if (preview == null || !preview.IsValid || attacker == null || !attacker.IsConcealed || !attacker.AmbushReady) return;
            preview.Modifiers.Add(new TacticalFireModifier { Label = "Prepared ambush", Value = AmbushHitModifier });
            preview.HitChance = Math.Max(5, Math.Min(95, preview.HitChance + AmbushHitModifier));
            preview.SuppressionChance = Math.Max(5, Math.Min(98, preview.SuppressionChance + AmbushSuppressionModifier));
            preview.ExpectedEffect = "AMBUSH";
        }

        public static bool CanSearch(TacticalUnitState searcher, HexCoord target)
            => searcher != null && searcher.CanMove && searcher.RemainingActionPoints >= SearchActionPointCost &&
               HexCoord.Distance(searcher.Position, target) == SearchRangeHexes;

        public static bool TrySearch(TacticalUnitState searcher, HexCoord target)
            => CanSearch(searcher, target) && searcher.TrySpendActionPoints(SearchActionPointCost);

        public static bool CanWithdraw(TacticalUnitState unit, HexCoord destination,
            IReadOnlyDictionary<HexCoord, TacticalMovementCell> board, IEnumerable<TacticalUnitState> threats)
        {
            if (unit == null || board == null || !unit.CanMove || unit.RemainingActionPoints < WithdrawActionPointCost ||
                HexCoord.Distance(unit.Position, destination) != WithdrawRangeHexes ||
                !board.TryGetValue(destination, out TacticalMovementCell cell) || !cell.IsPassable ||
                !string.IsNullOrEmpty(cell.OccupantId) && cell.OccupantId != unit.Id) return false;
            int before = NearestThreatDistance(unit.Position, threats);
            int after = NearestThreatDistance(destination, threats);
            return after > before;
        }

        public static bool TryWithdraw(TacticalUnitState unit, HexCoord destination,
            IReadOnlyDictionary<HexCoord, TacticalMovementCell> board, IEnumerable<TacticalUnitState> threats)
        {
            if (!CanWithdraw(unit, destination, board, threats) || !unit.TrySpendActionPoints(WithdrawActionPointCost)) return false;
            unit.ApplySuppressionPoints(-WithdrawSuppressionRecovery);
            unit.ReactionPoints = 0;
            unit.RemainingActionPoints = 0;
            unit.Readiness = UnitReadiness.Spent;
            unit.MoraleState = TacticalMoraleState.Withdrawn;
            Reveal(unit);
            return true;
        }

        public static bool TryChooseFallbackHex(IReadOnlyDictionary<HexCoord, TacticalMovementCell> board,
            TacticalUnitState unit, IEnumerable<TacticalUnitState> threats, out HexCoord destination)
        {
            destination = unit?.Position ?? default;
            if (board == null || unit == null) return false;
            int bestThreatDistance = NearestThreatDistance(unit.Position, threats);
            int bestCover = -1;
            bool found = false;
            foreach (HexCoord candidate in MovementPlanner.Neighbors(unit.Position))
            {
                if (!board.TryGetValue(candidate, out TacticalMovementCell cell) || !cell.IsPassable ||
                    !string.IsNullOrEmpty(cell.OccupantId) && cell.OccupantId != unit.Id) continue;
                int threatDistance = NearestThreatDistance(candidate, threats);
                int cover = (int)cell.Cover;
                if (found && threatDistance < bestThreatDistance || found && threatDistance == bestThreatDistance && cover <= bestCover) continue;
                destination = candidate;
                bestThreatDistance = threatDistance;
                bestCover = cover;
                found = true;
            }
            return found;
        }

        public static bool IsAdjacentToThreat(TacticalUnitState unit, IEnumerable<TacticalUnitState> threats)
        {
            if (unit == null || threats == null) return false;
            foreach (TacticalUnitState threat in threats)
                if (threat != null && threat.Strength > 0 && HexCoord.Distance(unit.Position, threat.Position) == 1) return true;
            return false;
        }

        private static int NearestThreatDistance(HexCoord position, IEnumerable<TacticalUnitState> threats)
        {
            int nearest = int.MaxValue / 2;
            if (threats == null) return nearest;
            foreach (TacticalUnitState threat in threats)
            {
                if (threat == null || threat.Strength <= 0 || threat.MoraleState == TacticalMoraleState.Surrendered) continue;
                nearest = Math.Min(nearest, HexCoord.Distance(position, threat.Position));
            }
            return nearest;
        }
    }
}
