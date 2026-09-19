using System;
using System.Collections.Generic;

namespace AlwaysFaithful.Core
{
    [Serializable]
    public sealed class TacticalBattalionEngagementRecord
    {
        public int Sequence;
        public string BattlefieldId;
        public TacticalMissionType MissionType;
        public TacticalBattleOutcome Outcome;
        public int StrengthBefore;
        public int StrengthAfter;
        public string CompletedAtUtc;
    }

    // Standalone-only persistent campaign layer: casualties/losses carry from
    // one generated scenario to the next until the player resets it. File
    // persisted (survives an app restart), the same way an in-progress battle
    // save does — deliberately a mini-campaign scoped to "until reset," not a
    // single process run.
    [Serializable]
    public sealed class TacticalBattalionStatus
    {
        public const int CurrentSchemaVersion = 1;

        public int SchemaVersion = CurrentSchemaVersion;
        public string BattalionName = "1st Battalion, Rifle Regiment";
        public int Strength = TacticalBattalion.MaximumStrength;
        public int EngagementsCompleted;
        public TacticalBattleOutcome LastOutcome = TacticalBattleOutcome.InProgress;
        public string LastSummary;
        public List<TacticalBattalionEngagementRecord> History = new List<TacticalBattalionEngagementRecord>();
    }

    public static class TacticalBattalion
    {
        public const int MinimumStrength = 20; // floor — never fully combat-ineffective
        public const int MaximumStrength = 100;
        public const int DegradedStrengthThreshold = 60;
        public const int ActionPointPenalty = 2;

        // First-pass tuning, centralized here for easy retuning. Keyed by
        // outcome plus (for a technical victory) the USMC unit's own final
        // CombatStatus — a finer signal than a plain win/loss bool, since a
        // Suppressed/Disrupted survivor still costs readiness in a "win."
        public static int ApplyEngagementResult(TacticalBattalionStatus status, TacticalBattleOutcome outcome, TacticalCombatStatus usmcFinalStatus)
        {
            int delta;
            switch (outcome)
            {
                case TacticalBattleOutcome.UsmcDefeat:
                    delta = -25;
                    break;
                case TacticalBattleOutcome.Draw:
                    delta = -15;
                    break;
                case TacticalBattleOutcome.Stalemate:
                    delta = -5;
                    break;
                case TacticalBattleOutcome.UsmcVictory:
                    switch (usmcFinalStatus)
                    {
                        case TacticalCombatStatus.Ready:
                            delta = 5;
                            break;
                        case TacticalCombatStatus.Suppressed:
                            delta = 0;
                            break;
                        case TacticalCombatStatus.Disrupted:
                            delta = -5;
                            break;
                        default:
                            delta = -10;
                            break;
                    }
                    break;
                default:
                    delta = 0;
                    break;
            }
            int before = status.Strength;
            status.Strength = Math.Max(MinimumStrength, Math.Min(MaximumStrength, status.Strength + delta));
            return status.Strength - before;
        }

        public static bool IsUnderStrength(TacticalBattalionStatus status) => status.Strength < DegradedStrengthThreshold;

        public static TacticalBattalionStatus CreateFresh() => new TacticalBattalionStatus();

        public static bool Validate(TacticalBattalionStatus status, out string error)
        {
            if (status == null)
            {
                error = "Battalion status file did not parse to a valid status";
                return false;
            }
            if (status.SchemaVersion != TacticalBattalionStatus.CurrentSchemaVersion)
            {
                error = $"Unsupported battalion status schema version {status.SchemaVersion} (expected {TacticalBattalionStatus.CurrentSchemaVersion})";
                return false;
            }
            if (status.Strength < MinimumStrength || status.Strength > MaximumStrength)
            {
                error = $"Battalion strength {status.Strength} outside valid range {MinimumStrength}-{MaximumStrength}";
                return false;
            }
            if (string.IsNullOrWhiteSpace(status.BattalionName))
            {
                error = "Battalion status is missing a battalion name";
                return false;
            }
            error = null;
            return true;
        }
    }
}
