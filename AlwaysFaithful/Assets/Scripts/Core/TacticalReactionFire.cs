using System;
using System.Collections.Generic;

namespace AlwaysFaithful.Core
{
    public readonly struct TacticalReactionCandidate
    {
        public readonly TacticalUnitState Unit;
        public readonly TacticalWeaponState Weapon;

        public TacticalReactionCandidate(TacticalUnitState unit, TacticalWeaponState weapon)
        {
            Unit = unit;
            Weapon = weapon;
        }
    }

    [Serializable]
    public sealed class TacticalReactionEvent
    {
        public int Sequence;
        public string BattlefieldId;
        public int Turn;
        public int Seed;
        public string ReactorId;
        public string MoverId;
        public HexCoord ReactorPosition;
        public HexCoord TriggerPosition;
        public int RangeHexes;
        public int HitChance;
        public int SuppressionChance;
        public int Roll;
        public TacticalFireOutcome Outcome;
        public string Resolution;
        public int AmmunitionBefore;
        public int AmmunitionAfter;
        public List<TacticalFireModifier> Modifiers = new List<TacticalFireModifier>();
    }

    public static class TacticalReactionFire
    {
        public const int ReactionRangeHexes = 5;
        public const int SnapShotPenalty = -15;

        public static TacticalReactionCandidate? SelectReactor(
            IReadOnlyDictionary<HexCoord, TacticalMovementCell> board,
            IEnumerable<TacticalReactionCandidate> candidates,
            HexCoord triggerPosition,
            out TacticalLosResult losResult)
        {
            TacticalReactionCandidate? best = null;
            TacticalLosResult bestLos = null;
            int bestRange = int.MaxValue;
            foreach (TacticalReactionCandidate candidate in candidates)
            {
                if (candidate.Unit == null || !candidate.Unit.CanFire) continue;
                if (candidate.Weapon == null || candidate.Weapon.RemainingAmmunition <= 0) continue;
                TacticalLosResult los = TacticalLineOfSight.Inspect(board, candidate.Unit.Position, triggerPosition, ReactionRangeHexes);
                if (!los.IsValid || los.State == TacticalLosState.Blocked) continue;
                if (los.RangeHexes > bestRange) continue;
                if (los.RangeHexes == bestRange && best.HasValue &&
                    string.CompareOrdinal(candidate.Unit.Id, best.Value.Unit.Id) >= 0) continue;
                best = candidate;
                bestLos = los;
                bestRange = los.RangeHexes;
            }
            losResult = bestLos;
            return best;
        }

        public static TacticalFirePreview Preview(
            IReadOnlyDictionary<HexCoord, TacticalMovementCell> board,
            TacticalUnitState reactor,
            TacticalWeaponState weapon,
            HexCoord moverPosition,
            TacticalLosResult los)
        {
            var preview = new TacticalFirePreview
            {
                AttackerId = reactor?.Id,
                AttackerPosition = reactor?.Position ?? default,
                TargetPosition = moverPosition
            };
            if (reactor == null || !reactor.CanFire) return Reject(preview, "Reactor cannot fire");
            if (weapon == null || weapon.RemainingAmmunition <= 0) return Reject(preview, "No ammunition");
            if (los == null || !los.IsValid || los.State == TacticalLosState.Blocked)
                return Reject(preview, los != null && !los.IsValid ? los.RejectionReason : "Line of sight blocked");
            if (!board.TryGetValue(moverPosition, out TacticalMovementCell targetCell) || !targetCell.IsPassable)
                return Reject(preview, "Illegal target terrain");

            preview.RangeHexes = los.RangeHexes;
            preview.LineOfSight = los.State;
            int chance = TacticalDirectFire.BaseHitChance;
            preview.Modifiers.Add(new TacticalFireModifier { Label = "Base small-arms fire", Value = TacticalDirectFire.BaseHitChance });
            preview.Modifiers.Add(new TacticalFireModifier { Label = "Reaction snap shot", Value = SnapShotPenalty });
            chance += SnapShotPenalty;
            int rangeModifier = -Math.Max(0, los.RangeHexes - 2) * 6;
            if (rangeModifier != 0)
            {
                preview.Modifiers.Add(new TacticalFireModifier { Label = $"Range {los.RangeHexes * 250} m", Value = rangeModifier });
                chance += rangeModifier;
            }
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
                int coverPenalty = TacticalDirectFire.CoverHitPenalty(targetCell.Cover);
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

        public static string ResolutionFor(TacticalFireOutcome outcome)
            => outcome == TacticalFireOutcome.Miss ? "Resumed" : "Halted";

        private static TacticalFirePreview Reject(TacticalFirePreview preview, string reason)
        {
            preview.RejectionReason = reason;
            return preview;
        }
    }
}
