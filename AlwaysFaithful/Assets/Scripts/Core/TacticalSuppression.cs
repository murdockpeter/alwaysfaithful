using System;

namespace AlwaysFaithful.Core
{
    [Serializable]
    public sealed class TacticalSuppressionEvent
    {
        public int Sequence;
        public string BattlefieldId;
        public int Turn;
        public string UnitId;
        public string Cause;
        public int PointsBefore;
        public int PointsAfter;
        public TacticalCombatStatus StatusBefore;
        public TacticalCombatStatus StatusAfter;
    }

    public static class TacticalSuppression
    {
        public const int MaximumPoints = 100;
        public const int SuppressedThreshold = 25;
        public const int DisruptedThreshold = 50;
        public const int ReducedThreshold = 75;
        public const int MissPoints = 0;
        public const int SuppressedFirePoints = 25;
        public const int HitFirePoints = 45;
        public const int RallyActionPointCost = 1;
        public const int RallyRecoveryAmount = 40;
        public const int PassiveRecoveryAmount = 15;

        public static TacticalCombatStatus ComputeStatus(int points)
        {
            if (points >= ReducedThreshold) return TacticalCombatStatus.Reduced;
            if (points >= DisruptedThreshold) return TacticalCombatStatus.Disrupted;
            if (points >= SuppressedThreshold) return TacticalCombatStatus.Suppressed;
            return TacticalCombatStatus.Ready;
        }

        public static int PointsForFireOutcome(TacticalFireOutcome outcome)
        {
            switch (outcome)
            {
                case TacticalFireOutcome.Hit: return HitFirePoints;
                case TacticalFireOutcome.Suppressed: return SuppressedFirePoints;
                default: return MissPoints;
            }
        }

        public static TacticalSuppressionEvent ApplyFireOutcome(TacticalUnitState target, TacticalFireOutcome outcome)
        {
            if (target == null) return null;
            int points = PointsForFireOutcome(outcome);
            if (points <= 0) return null;
            int before = target.SuppressionPoints;
            TacticalCombatStatus statusBefore = target.CombatStatus;
            target.ApplySuppressionPoints(points);
            return new TacticalSuppressionEvent
            {
                UnitId = target.Id,
                Cause = outcome.ToString(),
                PointsBefore = before,
                PointsAfter = target.SuppressionPoints,
                StatusBefore = statusBefore,
                StatusAfter = target.CombatStatus
            };
        }

        public static TacticalSuppressionEvent ApplyRally(TacticalUnitState unit)
        {
            if (unit == null) return null;
            int before = unit.SuppressionPoints;
            TacticalCombatStatus statusBefore = unit.CombatStatus;
            if (!unit.TryRally(RallyActionPointCost)) return null;
            return new TacticalSuppressionEvent
            {
                UnitId = unit.Id,
                Cause = "Rally",
                PointsBefore = before,
                PointsAfter = unit.SuppressionPoints,
                StatusBefore = statusBefore,
                StatusAfter = unit.CombatStatus
            };
        }
    }
}
