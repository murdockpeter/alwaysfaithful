using System;
using System.Collections.Generic;

namespace AlwaysFaithful.Core
{
    public enum TacticalFireMissionType { HighExplosive, Smoke, Illumination, MortarHighExplosive, MortarSmoke }
    public enum TacticalFireMissionStatus { Pending, Delivered, Cancelled }

    [Serializable]
    public sealed class TacticalPlannedFireMission
    {
        public string Id;
        public string ObserverId;
        public TacticalFireMissionType Type;
        public TacticalFireMissionStatus Status;
        public HexCoord RequestedHex;
        public HexCoord ImpactHex;
        public int OrderedTurn;
        public int DeliveryTurn;
        public int BaseScatterRadius;
        public int FinalScatterRadius;
        public bool Adjusted;
        public bool DangerClose;
        public int AmmunitionBefore;
        public int AmmunitionAfter;
        public int AffectedUnits;
    }

    [Serializable]
    public sealed class TacticalFireSupportState
    {
        public int MaximumAmmunition = TacticalPlannedFires.DefaultSupportAmmunition;
        public int Ammunition = TacticalPlannedFires.DefaultSupportAmmunition;
        public int NextAvailableTurn = 1;
        public HexCoord AdjustedAimPoint;
        public bool HasAdjustedAimPoint;
        public int AdjustmentLevel;
    }

    [Serializable]
    public sealed class TacticalIlluminationMarker
    {
        public HexCoord Position;
        public int PlacedTurn;
        public int ExpiresAfterTurn;
        public int RadiusHexes = TacticalPlannedFires.IlluminationRadiusHexes;

        public bool IsActive(int turn) => turn <= ExpiresAfterTurn;
    }

    [Serializable]
    public sealed class TacticalPlannedFireEvent
    {
        public int Sequence;
        public string BattlefieldId;
        public int Turn;
        public string MissionId;
        public string ObserverId;
        public TacticalFireMissionType Type;
        public TacticalFireMissionStatus Status;
        public HexCoord RequestedHex;
        public HexCoord ImpactHex;
        public int ScatterRadius;
        public bool DangerClose;
        public int AffectedUnits;
        public string Summary;
    }

    public static class TacticalPlannedFires
    {
        public const int OrderActionPointCost = 3;
        public const int AdjustActionPointCost = 1;
        public const int DefaultSupportAmmunition = 4;
        public const int SupportCooldownTurns = 2;
        public const int DeliveryDelayTurns = 1;
        public const int MaximumRangeHexes = 12;
        public const int DangerCloseRadiusHexes = 1;
        public const int EffectRadiusHexes = 1;
        public const int HeSuppression = 45;
        public const int HeStrengthDamage = 15;
        public const int SmokeDurationTurns = 2;
        public const int IlluminationDurationTurns = 2;
        public const int IlluminationRadiusHexes = 3;
        public const int MortarSetupActionPointCost = 2;
        public const int MortarFireActionPointCost = 2;
        public const int DefaultMortarAmmunition = 4;
        public const int MortarMinimumRangeHexes = 2;
        public const int MortarMaximumRangeHexes = 6;

        public static int ScatterRadius(TacticalVisibilityState contact, bool qualityObserver, int adjustmentLevel)
        {
            int scatter = contact == TacticalVisibilityState.Observed ? 0 :
                contact == TacticalVisibilityState.Identified ? 1 : contact == TacticalVisibilityState.Contact ? 2 : 3;
            if (qualityObserver) scatter--;
            scatter -= Math.Max(0, adjustmentLevel);
            return Math.Max(0, Math.Min(3, scatter));
        }

        public static bool CanOrder(TacticalUnitState observer, TacticalFireSupportState support, HexCoord target,
            int turn, bool mortar, out string rejection)
        {
            rejection = null;
            if (observer == null || !observer.CanMove) return Reject(out rejection, "Observer cannot act");
            int range = HexCoord.Distance(observer.Position, target);
            if (mortar)
            {
                if (!observer.MortarIsDeployed || !observer.MortarSetupPosition.Equals(observer.Position))
                    return Reject(out rejection, "Mortars must set up before firing");
                if (observer.MortarRounds <= 0) return Reject(out rejection, "Mortar ammunition exhausted");
                if (range < MortarMinimumRangeHexes || range > MortarMaximumRangeHexes)
                    return Reject(out rejection, $"Mortar range is {MortarMinimumRangeHexes}-{MortarMaximumRangeHexes} hexes");
                if (observer.RemainingActionPoints < MortarFireActionPointCost)
                    return Reject(out rejection, "Insufficient AP for mortar fire");
                return true;
            }
            if (support == null || support.Ammunition <= 0) return Reject(out rejection, "Fire-support ammunition exhausted");
            if (turn < support.NextAvailableTurn) return Reject(out rejection, $"Fire support reloads on turn {support.NextAvailableTurn}");
            if (range > MaximumRangeHexes) return Reject(out rejection, "Fire mission out of range");
            if (observer.RemainingActionPoints < OrderActionPointCost) return Reject(out rejection, "Insufficient AP for fire mission");
            return true;
        }

        public static TacticalPlannedFireMission Order(TacticalUnitState observer, TacticalFireSupportState support,
            TacticalFireMissionType type, HexCoord target, int turn, TacticalVisibilityState contact,
            bool qualityObserver, bool dangerClose, int sequence)
        {
            bool mortar = type == TacticalFireMissionType.MortarHighExplosive || type == TacticalFireMissionType.MortarSmoke;
            if (!CanOrder(observer, support, target, turn, mortar, out _)) return null;
            int ap = mortar ? MortarFireActionPointCost : OrderActionPointCost;
            if (!observer.TrySpendActionPoints(ap)) return null;
            int before;
            int after;
            int adjustment = support?.HasAdjustedAimPoint == true && support.AdjustedAimPoint.Equals(target) ? support.AdjustmentLevel : 0;
            if (mortar)
            {
                before = observer.MortarRounds;
                after = --observer.MortarRounds;
            }
            else
            {
                before = support.Ammunition;
                after = --support.Ammunition;
                support.NextAvailableTurn = turn + SupportCooldownTurns;
            }
            return new TacticalPlannedFireMission
            {
                Id = $"FIRE-{turn:D2}-{sequence:D4}",
                ObserverId = observer.Id,
                Type = type,
                Status = TacticalFireMissionStatus.Pending,
                RequestedHex = target,
                ImpactHex = target,
                OrderedTurn = turn,
                DeliveryTurn = turn + DeliveryDelayTurns,
                BaseScatterRadius = ScatterRadius(contact, qualityObserver, 0),
                FinalScatterRadius = ScatterRadius(contact, qualityObserver, adjustment),
                Adjusted = adjustment > 0,
                DangerClose = dangerClose,
                AmmunitionBefore = before,
                AmmunitionAfter = after
            };
        }

        public static bool TryAdjust(TacticalUnitState observer, TacticalFireSupportState support, HexCoord aimPoint)
        {
            if (observer == null || support == null || observer.RemainingActionPoints < AdjustActionPointCost ||
                !observer.TrySpendActionPoints(AdjustActionPointCost)) return false;
            if (support.HasAdjustedAimPoint && support.AdjustedAimPoint.Equals(aimPoint))
                support.AdjustmentLevel = Math.Min(2, support.AdjustmentLevel + 1);
            else
            {
                support.AdjustedAimPoint = aimPoint;
                support.HasAdjustedAimPoint = true;
                support.AdjustmentLevel = 1;
            }
            return true;
        }

        public static bool TrySetupMortar(TacticalUnitState unit, bool isWeaponsPlatoon)
        {
            if (!isWeaponsPlatoon || unit == null || unit.MovedThisTurn || unit.FiredThisTurn || unit.MortarIsDeployed ||
                unit.RemainingActionPoints < MortarSetupActionPointCost || !unit.TrySpendActionPoints(MortarSetupActionPointCost)) return false;
            unit.MortarIsDeployed = true;
            unit.MortarSetupPosition = unit.Position;
            if (unit.MaximumMortarRounds <= 0)
            {
                unit.MaximumMortarRounds = DefaultMortarAmmunition;
                unit.MortarRounds = DefaultMortarAmmunition;
            }
            return true;
        }

        public static HexCoord ResolveScatter(IReadOnlyDictionary<HexCoord, TacticalMovementCell> board,
            HexCoord aimPoint, int scatterRadius, int seed)
        {
            if (scatterRadius <= 0 || board == null) return aimPoint;
            var candidates = new List<HexCoord>();
            foreach (HexCoord coord in board.Keys)
                if (board[coord].IsPassable && HexCoord.Distance(aimPoint, coord) <= scatterRadius) candidates.Add(coord);
            if (candidates.Count == 0) return aimPoint;
            candidates.Sort((a, b) => a.Q != b.Q ? a.Q.CompareTo(b.Q) : a.R.CompareTo(b.R));
            return candidates[StableRoll(seed, aimPoint, candidates.Count)];
        }

        public static bool IsIlluminated(IEnumerable<TacticalIlluminationMarker> markers, HexCoord position, int turn)
        {
            if (markers == null) return false;
            foreach (TacticalIlluminationMarker marker in markers)
                if (marker.IsActive(turn) && HexCoord.Distance(marker.Position, position) <= marker.RadiusHexes) return true;
            return false;
        }

        public static bool IsDangerClose(IEnumerable<TacticalUnitState> friendlies, HexCoord target)
        {
            if (friendlies == null) return false;
            foreach (TacticalUnitState unit in friendlies)
                if (unit.Strength > 0 && HexCoord.Distance(unit.Position, target) <= DangerCloseRadiusHexes) return true;
            return false;
        }

        private static int StableRoll(int seed, HexCoord coord, int range)
        {
            unchecked
            {
                uint value = (uint)seed ^ ((uint)coord.Q * 73856093u) ^ ((uint)coord.R * 19349663u);
                value ^= value << 13; value ^= value >> 17; value ^= value << 5;
                return (int)(value % (uint)Math.Max(1, range));
            }
        }

        private static bool Reject(out string rejection, string reason) { rejection = reason; return false; }
    }
}
