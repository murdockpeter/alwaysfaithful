using System;
using System.Collections.Generic;

namespace AlwaysFaithful.Core
{
    public enum TacticalVisibilityState
    {
        Hidden,
        Contact,
        Identified,
        Observed
    }

    [Serializable]
    public sealed class TacticalContactState
    {
        public string TargetId;
        public string DisplayName;
        public TacticalVisibilityState State;
        public HexCoord LastKnownPosition;
        public int LastObservedTurn;
        public bool IsStale;
        public string ObserverId;
        public int RangeHexes;
        public TacticalLosState LineOfSight;

        public bool CanAttack => State == TacticalVisibilityState.Observed && !IsStale;
    }

    [Serializable]
    public sealed class TacticalObservationSnapshot
    {
        public int SchemaVersion = 1;
        public List<TacticalContactState> Contacts = new List<TacticalContactState>();
    }

    public static class TacticalObservation
    {
        public const int ClearObservationRangeHexes = 8;
        public const int ObscuredObservationRangeHexes = 3;
        public const int ObscuredIdentificationRangeHexes = 6;
        public const int ObscuredContactRangeHexes = 10;

        public static TacticalContactState Check(
            IReadOnlyDictionary<HexCoord, TacticalMovementCell> board,
            string observerId,
            HexCoord observer,
            string targetId,
            string displayName,
            HexCoord target,
            int turn,
            TacticalContactState previous = null,
            TacticalUnitState targetUnit = null)
        {
            TacticalLosResult los = TacticalLineOfSight.Inspect(board, observer, target);
            TacticalVisibilityState state = DetermineCurrentState(los);
            if (los.IsValid && board.TryGetValue(target, out TacticalMovementCell targetCell) &&
                (targetCell.Terrain == TacticalTerrain.Rough || targetCell.Terrain == TacticalTerrain.Highland))
            {
                // Occupying complex terrain reduces classification confidence even
                // when the geometric sightline itself remains open.
                if (state == TacticalVisibilityState.Observed && los.RangeHexes > ObscuredObservationRangeHexes)
                    state = TacticalVisibilityState.Identified;
                else if (state == TacticalVisibilityState.Identified)
                    state = TacticalVisibilityState.Contact;
            }
            state = TacticalConcealmentMorale.ApplyConcealment(state, targetUnit, los.RangeHexes);
            if (state == TacticalVisibilityState.Hidden && previous != null && previous.State != TacticalVisibilityState.Hidden)
            {
                // A lost track remains as one-turn-old map information, then expires.
                if (!previous.IsStale || previous.LastObservedTurn >= turn - 1)
                    return Copy(previous, TacticalVisibilityState.Contact, true);
            }

            var result = new TacticalContactState
            {
                TargetId = targetId,
                DisplayName = displayName,
                State = state,
                LastKnownPosition = target,
                LastObservedTurn = state == TacticalVisibilityState.Hidden ? 0 : turn,
                IsStale = false,
                ObserverId = observerId,
                RangeHexes = los.RangeHexes,
                LineOfSight = los.IsValid ? los.State : TacticalLosState.Blocked
            };
            return result;
        }

        public static TacticalVisibilityState DetermineCurrentState(TacticalLosResult los)
        {
            if (los == null || !los.IsValid || los.State == TacticalLosState.Blocked)
                return TacticalVisibilityState.Hidden;
            if (los.State == TacticalLosState.Obscured)
            {
                if (los.RangeHexes <= ObscuredObservationRangeHexes) return TacticalVisibilityState.Observed;
                if (los.RangeHexes <= ObscuredIdentificationRangeHexes) return TacticalVisibilityState.Identified;
                if (los.RangeHexes <= ObscuredContactRangeHexes) return TacticalVisibilityState.Contact;
                return TacticalVisibilityState.Hidden;
            }
            return los.RangeHexes <= ClearObservationRangeHexes
                ? TacticalVisibilityState.Observed
                : TacticalVisibilityState.Identified;
        }

        public static bool CanAttack(TacticalContactState contact, HexCoord reportedPosition)
            => contact != null && contact.CanAttack && contact.LastKnownPosition.Equals(reportedPosition);

        private static TacticalContactState Copy(TacticalContactState source, TacticalVisibilityState state, bool stale)
        {
            return new TacticalContactState
            {
                TargetId = source.TargetId,
                DisplayName = source.DisplayName,
                State = state,
                LastKnownPosition = source.LastKnownPosition,
                LastObservedTurn = source.LastObservedTurn,
                IsStale = stale,
                ObserverId = source.ObserverId,
                RangeHexes = source.RangeHexes,
                LineOfSight = source.LineOfSight
            };
        }
    }
}
