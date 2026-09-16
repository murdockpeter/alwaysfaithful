using System;
using System.Collections.Generic;

namespace AlwaysFaithful.Core
{
    public enum TacticalFireOutcome
    {
        Rejected,
        Miss,
        Suppressed,
        Hit
    }

    [Serializable]
    public sealed class TacticalWeaponState
    {
        public string WeaponId;
        public string DisplayName;
        public int MaximumAmmunition;
        public int RemainingAmmunition;

        public TacticalWeaponState(string weaponId, string displayName, int ammunition)
        {
            WeaponId = weaponId;
            DisplayName = displayName;
            MaximumAmmunition = Math.Max(1, ammunition);
            RemainingAmmunition = MaximumAmmunition;
        }
    }

    [Serializable]
    public sealed class TacticalFireModifier
    {
        public string Label;
        public int Value;
    }

    [Serializable]
    public sealed class TacticalFirePreview
    {
        public bool IsValid;
        public string RejectionReason;
        public string AttackerId;
        public string TargetId;
        public HexCoord AttackerPosition;
        public HexCoord TargetPosition;
        public int RangeHexes;
        public TacticalLosState LineOfSight;
        public int HitChance;
        public int SuppressionChance;
        public string ExpectedEffect;
        public List<TacticalFireModifier> Modifiers = new List<TacticalFireModifier>();
    }

    [Serializable]
    public sealed class TacticalFireEvent
    {
        public int Sequence;
        public string BattlefieldId;
        public int Turn;
        public int Seed;
        public string AttackerId;
        public string TargetId;
        public HexCoord AttackerPosition;
        public HexCoord TargetPosition;
        public int RangeHexes;
        public int HitChance;
        public int SuppressionChance;
        public int Roll;
        public TacticalFireOutcome Outcome;
        public int AmmunitionBefore;
        public int AmmunitionAfter;
        public List<TacticalFireModifier> Modifiers = new List<TacticalFireModifier>();
    }

    public static class TacticalDirectFire
    {
        public const int MaximumRangeHexes = 8;
        public const int ActionPointCost = 2;
        public const int BaseHitChance = 72;
        public const int LightCoverHitPenalty = -8;
        public const int MediumCoverHitPenalty = -18;
        public const int HeavyCoverHitPenalty = -32;

        // Shared by TacticalReactionFire.Preview, which duplicates this method's
        // modifier stack rather than calling into it directly.
        public static int CoverHitPenalty(TacticalCover cover)
        {
            switch (cover)
            {
                case TacticalCover.Light: return LightCoverHitPenalty;
                case TacticalCover.Medium: return MediumCoverHitPenalty;
                case TacticalCover.Heavy: return HeavyCoverHitPenalty;
                default: return 0;
            }
        }

        public static TacticalFirePreview Preview(
            IReadOnlyDictionary<HexCoord, TacticalMovementCell> board,
            string attackerId,
            HexCoord attackerPosition,
            TacticalContactState contact,
            HexCoord targetPosition,
            TacticalWeaponState weapon,
            int availableActionPoints)
        {
            var preview = new TacticalFirePreview
            {
                AttackerId = attackerId,
                TargetId = contact?.TargetId,
                AttackerPosition = attackerPosition,
                TargetPosition = targetPosition
            };
            if (weapon == null || weapon.RemainingAmmunition <= 0) return Reject(preview, "No ammunition");
            if (availableActionPoints < ActionPointCost) return Reject(preview, $"Requires {ActionPointCost} AP");
            if (!TacticalObservation.CanAttack(contact, targetPosition)) return Reject(preview, "Target is not currently observed");
            if (!board.TryGetValue(targetPosition, out TacticalMovementCell targetCell) || !targetCell.IsPassable)
                return Reject(preview, "Illegal target terrain");

            TacticalLosResult los = TacticalLineOfSight.Inspect(board, attackerPosition, targetPosition, MaximumRangeHexes);
            preview.RangeHexes = los.RangeHexes;
            preview.LineOfSight = los.State;
            if (!los.IsValid || los.State == TacticalLosState.Blocked)
                return Reject(preview, los.IsValid ? "Line of sight blocked" : los.RejectionReason);

            int chance = BaseHitChance;
            preview.Modifiers.Add(new TacticalFireModifier { Label = "Base small-arms fire", Value = BaseHitChance });
            int rangeModifier = -Math.Max(0, los.RangeHexes - 2) * 6;
            if (rangeModifier != 0) preview.Modifiers.Add(new TacticalFireModifier { Label = $"Range {los.RangeHexes * 250} m", Value = rangeModifier });
            chance += rangeModifier;
            if (targetCell.Terrain == TacticalTerrain.Rough)
            {
                chance -= 18;
                preview.Modifiers.Add(new TacticalFireModifier { Label = "Target in rough terrain", Value = -18 });
            }
            else if (targetCell.Terrain == TacticalTerrain.Highland)
            {
                chance -= 10;
                preview.Modifiers.Add(new TacticalFireModifier { Label = "Target in highland", Value = -10 });
            }
            if (targetCell.Cover != TacticalCover.None)
            {
                int coverPenalty = CoverHitPenalty(targetCell.Cover);
                chance += coverPenalty;
                preview.Modifiers.Add(new TacticalFireModifier { Label = $"Target under {targetCell.Cover.ToString().ToLowerInvariant()} cover", Value = coverPenalty });
            }
            if (los.State == TacticalLosState.Obscured)
            {
                chance -= 15;
                preview.Modifiers.Add(new TacticalFireModifier { Label = "Obscured sightline", Value = -15 });
            }
            preview.HitChance = Math.Max(5, Math.Min(90, chance));
            preview.SuppressionChance = Math.Min(95, preview.HitChance + 20);
            preview.ExpectedEffect = preview.SuppressionChance >= 75 ? "HIGH" : preview.SuppressionChance >= 50 ? "MODERATE" : "LOW";
            preview.IsValid = true;
            return preview;
        }

        public static TacticalFireEvent Resolve(TacticalFirePreview preview, TacticalWeaponState weapon, int seed)
        {
            if (preview == null || !preview.IsValid || weapon == null || weapon.RemainingAmmunition <= 0)
                return new TacticalFireEvent { Seed = seed, Outcome = TacticalFireOutcome.Rejected };
            int ammunitionBefore = weapon.RemainingAmmunition;
            weapon.RemainingAmmunition--;
            int roll = DeterministicRoll(seed);
            TacticalFireOutcome outcome = roll <= preview.HitChance
                ? TacticalFireOutcome.Hit
                : roll <= preview.SuppressionChance ? TacticalFireOutcome.Suppressed : TacticalFireOutcome.Miss;
            return new TacticalFireEvent
            {
                Seed = seed,
                AttackerId = preview.AttackerId,
                TargetId = preview.TargetId,
                AttackerPosition = preview.AttackerPosition,
                TargetPosition = preview.TargetPosition,
                RangeHexes = preview.RangeHexes,
                HitChance = preview.HitChance,
                SuppressionChance = preview.SuppressionChance,
                Roll = roll,
                Outcome = outcome,
                AmmunitionBefore = ammunitionBefore,
                AmmunitionAfter = weapon.RemainingAmmunition,
                Modifiers = new List<TacticalFireModifier>(preview.Modifiers)
            };
        }

        public static int CreateSeed(string battlefieldId, int turn, int sequence)
        {
            unchecked
            {
                uint hash = 2166136261u;
                string text = battlefieldId ?? string.Empty;
                for (int index = 0; index < text.Length; index++) hash = (hash ^ text[index]) * 16777619u;
                hash = (hash ^ (uint)turn) * 16777619u;
                hash = (hash ^ (uint)sequence) * 16777619u;
                return (int)(hash & 0x7fffffff);
            }
        }

        private static int DeterministicRoll(int seed)
        {
            unchecked
            {
                uint value = (uint)seed + 0x9E3779B9u;
                value ^= value << 13;
                value ^= value >> 17;
                value ^= value << 5;
                return (int)(value % 100u) + 1;
            }
        }

        private static TacticalFirePreview Reject(TacticalFirePreview preview, string reason)
        {
            preview.RejectionReason = reason;
            return preview;
        }
    }
}
