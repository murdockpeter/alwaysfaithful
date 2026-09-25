using System;
using System.Collections.Generic;

namespace AlwaysFaithful.Core
{
    public enum TacticalMovementPosture { Quick, Tactical, Bounding }
    public enum TacticalReactionPolicy { WeaponsHold, ReturnFire, WeaponsFree }
    public enum TacticalFacingAspect { Forward, Flank, Rear }

    [Serializable]
    public sealed class TacticalSmokeMarker
    {
        public HexCoord Position;
        public int PlacedTurn;
        public int ExpiresAfterTurn;
        public string SourceUnitId;
        public bool IsActive(int turn) => turn <= ExpiresAfterTurn;
    }

    [Serializable]
    public sealed class TacticalSmokeEvent
    {
        public int Sequence;
        public string BattlefieldId;
        public int Turn;
        public string UnitId;
        public HexCoord Position;
        public int ActionPointCost;
        public int ExpiresAfterTurn;
    }

    [Serializable]
    public sealed class TacticalAreaSuppressionEvent
    {
        public int Sequence;
        public string BattlefieldId;
        public int Turn;
        public int Seed;
        public string AttackerId;
        public string TargetId;
        public HexCoord TargetPosition;
        public int Roll;
        public int SuppressionChance;
        public int SuppressionApplied;
        public int AmmunitionBefore;
        public int AmmunitionAfter;
    }

    [Serializable]
    public sealed class TacticalAssaultEvent
    {
        public int Sequence;
        public string BattlefieldId;
        public int Turn;
        public int Seed;
        public string AttackerId;
        public string DefenderId;
        public HexCoord Origin;
        public HexCoord Destination;
        public int SuccessChance;
        public int Roll;
        public bool Succeeded;
        public string Resolution;
    }

    public static class TacticalFireAndManeuver
    {
        public const int ReactionPointMaximum = 2;
        public const int ReactionPointCost = 1;
        public const int SmokeActionPointCost = 2;
        public const int SmokeRangeHexes = 2;
        public const int SmokeDurationTurns = 2;
        public const int SuppressionActionPointCost = 3;
        public const int SuppressionAmmunitionCost = 2;
        public const int SuppressionRangeHexes = 6;
        public const int AssaultActionPointCost = 4;

        public static int PlanningBudget(int remainingActionPoints, TacticalMovementPosture posture)
        {
            if (posture == TacticalMovementPosture.Quick) return remainingActionPoints * 2;
            if (posture == TacticalMovementPosture.Bounding) return Math.Max(1, (remainingActionPoints * 2) / 3);
            return remainingActionPoints;
        }

        public static int MovementCost(int routeCost, TacticalMovementPosture posture)
        {
            if (posture == TacticalMovementPosture.Quick) return Math.Max(1, (routeCost + 1) / 2);
            if (posture == TacticalMovementPosture.Bounding) return Math.Max(1, (routeCost * 3 + 1) / 2);
            return routeCost;
        }

        public static int DirectFireModifier(TacticalUnitState unit)
        {
            if (unit == null || !unit.MovedThisTurn) return 0;
            return unit.LastMovementPosture == TacticalMovementPosture.Quick ? -20 :
                unit.LastMovementPosture == TacticalMovementPosture.Bounding ? -5 : -10;
        }

        public static int DirectionSector(HexCoord origin, HexCoord target)
        {
            if (origin.Equals(target)) return 0;
            int diagonal = (origin.Q & 1) == 0 ? -1 : 0;
            var neighbors = new List<HexCoord>
            {
                new HexCoord(origin.Q, origin.R - 1),
                new HexCoord(origin.Q + 1, origin.R + diagonal),
                new HexCoord(origin.Q + 1, origin.R + diagonal + 1),
                new HexCoord(origin.Q, origin.R + 1),
                new HexCoord(origin.Q - 1, origin.R + diagonal + 1),
                new HexCoord(origin.Q - 1, origin.R + diagonal)
            };
            int best = 0;
            int distance = int.MaxValue;
            for (int index = 0; index < neighbors.Count; index++)
            {
                int candidate = HexCoord.Distance(neighbors[index], target);
                if (candidate >= distance) continue;
                best = index;
                distance = candidate;
            }
            return best;
        }

        public static TacticalFacingAspect FacingAspect(TacticalUnitState unit, HexCoord target)
        {
            int delta = Math.Abs(DirectionSector(unit.Position, target) - NormalizedFacing(unit.FacingSector));
            delta = Math.Min(delta, 6 - delta);
            return delta <= 1 ? TacticalFacingAspect.Forward : delta == 2 ? TacticalFacingAspect.Flank : TacticalFacingAspect.Rear;
        }

        public static int ReactionModifierForAspect(TacticalFacingAspect aspect)
            => aspect == TacticalFacingAspect.Forward ? 10 : aspect == TacticalFacingAspect.Flank ? -10 : -25;

        public static bool CanReact(TacticalUnitState unit)
        {
            if (unit == null || !unit.CanFire || unit.ReactionPoints < ReactionPointCost) return false;
            if (unit.ReactionPolicy == TacticalReactionPolicy.WeaponsHold) return false;
            return unit.ReactionPolicy == TacticalReactionPolicy.WeaponsFree || unit.WasFiredUponThisTurn;
        }

        public static void Face(TacticalUnitState unit, HexCoord target)
        {
            if (unit != null && !unit.Position.Equals(target)) unit.FacingSector = DirectionSector(unit.Position, target);
        }

        public static void RecordMove(TacticalUnitState unit, TacticalMovementPosture posture, IReadOnlyList<HexCoord> path)
        {
            if (unit == null) return;
            TacticalConcealmentMorale.Reveal(unit);
            unit.MovedThisTurn = true;
            TacticalCombatPower.ClearEntrenchment(unit);
            unit.LastMovementPosture = posture;
            if (path != null && path.Count > 1) unit.FacingSector = DirectionSector(path[path.Count - 2], path[path.Count - 1]);
            if (posture == TacticalMovementPosture.Quick) unit.ReactionPoints = 0;
            else if (posture == TacticalMovementPosture.Bounding) unit.ReactionPoints = Math.Min(ReactionPointMaximum, unit.ReactionPoints + 1);
        }

        public static int AreaSuppressionChance(TacticalLosResult los)
        {
            if (los == null || !los.IsValid || los.State == TacticalLosState.Blocked) return 0;
            int chance = 75 - Math.Max(0, los.RangeHexes - 2) * 5;
            if (los.State == TacticalLosState.Obscured) chance -= 15;
            return Math.Max(15, Math.Min(85, chance));
        }

        public static int AssaultChance(TacticalUnitState attacker, TacticalUnitState defender, TacticalMovementCell target, bool engineerAssault = false)
        {
            if (attacker == null || defender == null || target == null || HexCoord.Distance(attacker.Position, defender.Position) != 1) return 0;
            int chance = 50 + (defender.SuppressionPoints - attacker.SuppressionPoints) * 6;
            if (target.Cover == TacticalCover.Light) chance -= 5;
            else if (target.Cover == TacticalCover.Medium) chance -= 15;
            else if (target.Cover == TacticalCover.Heavy) chance -= 25;
            if (engineerAssault && (defender.EntrenchmentLevel > 0 || target.Cover >= TacticalCover.Medium)) chance += 20;
            if (attacker.LastMovementPosture == TacticalMovementPosture.Bounding) chance += 10;
            return Math.Max(10, Math.Min(90, chance));
        }

        public static int DeterministicRoll(int seed)
        {
            unchecked
            {
                uint value = (uint)seed;
                value ^= value << 13;
                value ^= value >> 17;
                value ^= value << 5;
                return (int)(value % 100u) + 1;
            }
        }

        private static int NormalizedFacing(int facing) => (facing % 6 + 6) % 6;
    }
}
