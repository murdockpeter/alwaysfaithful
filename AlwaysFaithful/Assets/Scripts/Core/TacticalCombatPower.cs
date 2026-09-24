using System;

namespace AlwaysFaithful.Core
{
    public enum TacticalAttackType { Hasty, Standard, Deliberate, Rapid }

    [Serializable]
    public sealed class TacticalStrengthEvent
    {
        public int Sequence;
        public string BattlefieldId;
        public int Turn;
        public string UnitId;
        public string Cause;
        public int StrengthBefore;
        public int StrengthAfter;
    }

    [Serializable]
    public sealed class TacticalPositionEvent
    {
        public int Sequence;
        public string BattlefieldId;
        public int Turn;
        public string UnitId;
        public HexCoord Position;
        public int LevelBefore;
        public int LevelAfter;
    }

    [Serializable]
    public sealed class TacticalLogisticsEvent
    {
        public int Sequence;
        public string BattlefieldId;
        public int Turn;
        public string UnitId;
        public string SourceUnitId;
        public string Kind;
        public int StrengthBefore;
        public int StrengthAfter;
        public int SupplyBefore;
        public int SupplyAfter;
        public int AmmunitionBefore;
        public int AmmunitionAfter;
    }

    [Serializable]
    public sealed class TacticalCallForFireEvent
    {
        public int Sequence;
        public string BattlefieldId;
        public int Turn;
        public int Seed;
        public string ObserverId;
        public HexCoord Target;
        public int MissionsBefore;
        public int MissionsAfter;
        public int AffectedUnits;
    }

    public readonly struct TacticalAttackProfile
    {
        public readonly TacticalAttackType Type;
        public readonly int ActionPointCost;
        public readonly int AmmunitionCost;
        public readonly int HitModifier;
        public readonly int SuppressionModifier;
        public readonly int StrengthDamage;

        public TacticalAttackProfile(TacticalAttackType type, int ap, int ammunition, int hit, int suppression, int damage)
        {
            Type = type;
            ActionPointCost = ap;
            AmmunitionCost = ammunition;
            HitModifier = hit;
            SuppressionModifier = suppression;
            StrengthDamage = damage;
        }
    }

    public static class TacticalCombatPower
    {
        public const int DefaultStrength = 100;
        public const int DefaultSupply = 6;
        public const int EntrenchActionPointCost = 4;
        public const int MaximumEntrenchmentLevel = 2;
        public const int EntrenchmentPenaltyPerLevel = -12;
        public const int ReorganizeActionPointCost = 4;
        public const int ReorganizeSupplyCost = 2;
        public const int ReorganizeStrengthRecovery = 15;
        public const int ReorganizeSuppressionRecovery = 40;
        public const int ResupplyActionPointCost = 2;
        public const int ResupplyRangeHexes = 1;
        public const int ResupplyAmmunition = 3;
        public const int ResupplySupply = 3;
        public const int CallForFireActionPointCost = 3;
        public const int CallForFireRangeHexes = 12;
        public const int CallForFireRadiusHexes = 1;
        public const int DefaultFireMissions = 2;
        public const int FireMissionStrengthDamage = 15;

        public static TacticalAttackProfile AttackProfile(TacticalAttackType type)
        {
            switch (type)
            {
                case TacticalAttackType.Hasty: return new TacticalAttackProfile(type, 1, 1, -15, -10, 10);
                case TacticalAttackType.Deliberate: return new TacticalAttackProfile(type, 3, 1, 15, 10, 25);
                case TacticalAttackType.Rapid: return new TacticalAttackProfile(type, 3, 2, 5, 20, 20);
                default: return new TacticalAttackProfile(type, 2, 1, 0, 0, 20);
            }
        }

        public static int EntrenchmentHitModifier(TacticalUnitState target, HexCoord position)
            => target != null && target.EntrenchmentLevel > 0 && target.EntrenchedPosition.Equals(position)
                ? target.EntrenchmentLevel * EntrenchmentPenaltyPerLevel : 0;

        public static bool TryEntrench(TacticalUnitState unit)
        {
            if (unit == null || unit.MovedThisTurn || unit.FiredThisTurn || unit.EntrenchmentLevel >= MaximumEntrenchmentLevel) return false;
            if (!unit.TrySpendActionPoints(EntrenchActionPointCost)) return false;
            unit.EntrenchmentLevel++;
            unit.EntrenchedPosition = unit.Position;
            return true;
        }

        public static void ClearEntrenchment(TacticalUnitState unit)
        {
            if (unit == null) return;
            unit.EntrenchmentLevel = 0;
            unit.EntrenchedPosition = unit.Position;
        }

        public static int StrengthDamage(TacticalFireOutcome outcome, TacticalAttackType type)
            => outcome == TacticalFireOutcome.Hit ? AttackProfile(type).StrengthDamage : 0;

        public static TacticalStrengthEvent ApplyStrengthDamage(TacticalUnitState unit, int damage, string cause)
        {
            if (unit == null || damage <= 0) return null;
            int before = unit.Strength;
            unit.Strength = Math.Max(0, unit.Strength - damage);
            return new TacticalStrengthEvent { UnitId = unit.Id, Cause = cause, StrengthBefore = before, StrengthAfter = unit.Strength };
        }

        public static bool TryReorganize(TacticalUnitState unit)
        {
            if (unit == null || unit.MovedThisTurn || unit.FiredThisTurn || unit.Supply < ReorganizeSupplyCost ||
                unit.Strength >= unit.MaximumStrength && unit.SuppressionPoints == 0) return false;
            if (!unit.TrySpendActionPoints(ReorganizeActionPointCost)) return false;
            unit.Supply -= ReorganizeSupplyCost;
            unit.Strength = Math.Min(unit.MaximumStrength, unit.Strength + ReorganizeStrengthRecovery);
            unit.ApplySuppressionPoints(-ReorganizeSuppressionRecovery);
            return true;
        }

        public static bool IsCombatEffective(TacticalUnitState unit)
            => unit != null && unit.HasArrived && unit.Strength > 0 && unit.CombatStatus != TacticalCombatStatus.Reduced;
    }
}
