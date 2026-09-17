using System;
using System.Collections.Generic;

namespace AlwaysFaithful.Core
{
    [Serializable]
    public sealed class TacticalReconMarker
    {
        public HexCoord Hex;
        public int TurnsRemaining;
    }

    [Serializable]
    public sealed class TacticalReconEvent
    {
        public int Sequence;
        public string BattlefieldId;
        public int Turn;
        public string UnitId;
        public HexCoord Hex;
        public int DurationTurns;
    }

    // A focused sensor tasking on a fixed hex, not a tracked unit: no ground
    // line of sight is required (representing an indirect/overhead request
    // rather than the platoon's own eyes), but a target that moves off the
    // tasked hex stops benefiting from it. This is deliberate, not a gap to
    // fix later.
    public static class TacticalRecon
    {
        public const int ActionPointCost = 3;
        public const int DurationTurns = 2;
        public const int MaximumRangeHexes = TacticalLineOfSight.MaximumInspectionRangeHexes;

        public static bool IsUnderActiveRecon(IReadOnlyList<TacticalReconMarker> markers, HexCoord hex)
        {
            if (markers == null) return false;
            foreach (TacticalReconMarker marker in markers)
                if (marker.Hex.Equals(hex)) return true;
            return false;
        }

        public static TacticalVisibilityState ApplyBonus(TacticalVisibilityState baseState, bool underActiveRecon)
            => underActiveRecon && baseState != TacticalVisibilityState.Observed
                ? (TacticalVisibilityState)((int)baseState + 1)
                : baseState;
    }
}
