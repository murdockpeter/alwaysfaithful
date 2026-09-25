using System;
using System.Collections.Generic;

namespace AlwaysFaithful.Core
{
    public enum OperationalSide
    {
        Usmc,
        Pla
    }

    public enum OperationalScenarioOutcome
    {
        InProgress,
        UsmcVictory,
        PlaVictory
    }

    public enum OperationalBattalionConfiguration
    {
        UsmcReinforcedAssault,
        UsmcLittoralScreen,
        PlaAmphibiousCombinedArms,
        PlaReconnaissance
    }

    [Serializable]
    public sealed class OperationalPlatoonState
    {
        public string Id;
        public string DisplayName;
        public string ParentCompany;
        public TacticalPlatoonRole Role;
        public int Strength = TacticalCombatPower.DefaultStrength;
        public int MaximumStrength = TacticalCombatPower.DefaultStrength;
    }

    [Serializable]
    public sealed class OperationalBattalionState
    {
        public string Id;
        public string DisplayName;
        public string ShortName;
        public OperationalSide Side;
        public HexCoord Position;
        public HexCoord ObjectiveHex;
        public int Strength = 100;
        public int MaximumActionPoints = 4;
        public int RemainingActionPoints = 4;
        public UnitReadiness Readiness = UnitReadiness.Available;
        public bool IsSelected;
        public OperationalBattalionConfiguration Configuration;
        public List<string> SubordinateUnits = new List<string>();
        // The persistent, platoon-scale order of battle used verbatim whenever
        // this operational counter is opened on the 250 m tactical map.
        public List<OperationalPlatoonState> Platoons = new List<OperationalPlatoonState>();

        public bool CanAct => Strength > 0 && Readiness == UnitReadiness.Available && RemainingActionPoints > 0;

        public bool TrySpendActionPoints(int cost)
        {
            if (!CanAct || cost <= 0 || cost > RemainingActionPoints) return false;
            RemainingActionPoints -= cost;
            Readiness = RemainingActionPoints > 0 ? UnitReadiness.Available : UnitReadiness.Spent;
            IsSelected = false;
            return true;
        }

        public void BeginTurn()
        {
            RemainingActionPoints = MaximumActionPoints;
            Readiness = UnitReadiness.Available;
            IsSelected = false;
        }
    }

    [Serializable]
    public sealed class OperationalContactState
    {
        public string TargetId;
        public TacticalVisibilityState State;
        public HexCoord LastKnownPosition;
        public int LastObservedTurn;
        public bool IsStale;
    }

    [Serializable]
    public sealed class OperationalReconMarker
    {
        public HexCoord Hex;
        public int TurnsRemaining;
    }

    [Serializable]
    public sealed class OperationalScenarioState
    {
        public const int CurrentSchemaVersion = 1;

        public int SchemaVersion = CurrentSchemaVersion;
        public string ScenarioId = "island-sentinel-2030";
        public string Title = "OPERATION ISLAND SENTINEL";
        public string DateTimeGroup = "180600Z MAY 2030";
        public int TurnNumber = 1;
        public int TurnLimit = 12;
        public HexCoord PrimaryObjective;
        public OperationalScenarioOutcome Outcome = OperationalScenarioOutcome.InProgress;
        public string OutcomeSummary;
        public string Situation;
        public string Mission;
        public string Execution;
        public string IntelligenceEstimate;
        public List<OperationalBattalionState> FriendlyBattalions = new List<OperationalBattalionState>();
        public List<OperationalBattalionState> EnemyBattalions = new List<OperationalBattalionState>();
        public List<OperationalContactState> EnemyContacts = new List<OperationalContactState>();
        public List<OperationalReconMarker> ReconMarkers = new List<OperationalReconMarker>();
    }

    public static class OperationalScenarioRules
    {
        public const int ReconActionPointCost = 2;
        public const int ReconMaximumRangeHexes = 12;
        public const int ReconEffectRadiusHexes = 2;
        public const int ReconDurationTurns = 2;
        public const int PassiveContactRangeHexes = 8;
        public const int PassiveIdentificationRangeHexes = 5;
        public const int PassiveObservationRangeHexes = 2;

        public static TacticalVisibilityState DetectionTier(int range, bool underRecon)
        {
            TacticalVisibilityState state = range <= PassiveObservationRangeHexes
                ? TacticalVisibilityState.Observed
                : range <= PassiveIdentificationRangeHexes
                    ? TacticalVisibilityState.Identified
                    : range <= PassiveContactRangeHexes
                        ? TacticalVisibilityState.Contact
                        : TacticalVisibilityState.Hidden;
            if (underRecon && state != TacticalVisibilityState.Observed)
                state = (TacticalVisibilityState)((int)state + 1);
            return state;
        }

        public static OperationalContactState Observe(
            OperationalBattalionState target,
            IReadOnlyList<OperationalBattalionState> observers,
            IReadOnlyList<OperationalReconMarker> reconMarkers,
            int turn,
            OperationalContactState previous = null)
        {
            int range = int.MaxValue;
            if (observers != null)
                foreach (OperationalBattalionState observer in observers)
                    range = Math.Min(range, HexCoord.Distance(observer.Position, target.Position));

            bool underRecon = false;
            if (reconMarkers != null)
                foreach (OperationalReconMarker marker in reconMarkers)
                    if (HexCoord.Distance(marker.Hex, target.Position) <= ReconEffectRadiusHexes)
                    {
                        underRecon = true;
                        break;
                    }

            TacticalVisibilityState state = DetectionTier(range, underRecon);
            if (state == TacticalVisibilityState.Hidden && previous != null && previous.State != TacticalVisibilityState.Hidden)
            {
                if (!previous.IsStale || previous.LastObservedTurn >= turn - 1)
                {
                    return new OperationalContactState
                    {
                        TargetId = target.Id,
                        State = TacticalVisibilityState.Contact,
                        LastKnownPosition = previous.LastKnownPosition,
                        LastObservedTurn = previous.LastObservedTurn,
                        IsStale = true
                    };
                }
            }

            return new OperationalContactState
            {
                TargetId = target.Id,
                State = state,
                LastKnownPosition = target.Position,
                LastObservedTurn = state == TacticalVisibilityState.Hidden ? 0 : turn,
                IsStale = false
            };
        }

        public static bool Validate(OperationalScenarioState scenario, out string error)
        {
            if (scenario == null)
            {
                error = "Operational scenario did not parse";
                return false;
            }
            if (scenario.SchemaVersion != OperationalScenarioState.CurrentSchemaVersion)
            {
                error = $"Unsupported operational scenario schema {scenario.SchemaVersion}";
                return false;
            }
            if (scenario.FriendlyBattalions == null || scenario.FriendlyBattalions.Count == 0 ||
                scenario.EnemyBattalions == null || scenario.EnemyBattalions.Count == 0)
            {
                error = "Operational scenario requires battalions on both sides";
                return false;
            }
            if (scenario.TurnNumber < 1 || scenario.TurnLimit < 1 || !Enum.IsDefined(typeof(OperationalScenarioOutcome), scenario.Outcome))
            {
                error = "Operational scenario has invalid turn or outcome state";
                return false;
            }
            if (scenario.EnemyContacts == null || scenario.ReconMarkers == null)
            {
                error = "Operational scenario is missing intelligence state";
                return false;
            }
            var ids = new HashSet<string>();
            foreach (OperationalBattalionState battalion in scenario.FriendlyBattalions)
            {
                if (battalion == null || battalion.Side != OperationalSide.Usmc || string.IsNullOrWhiteSpace(battalion.Id) ||
                    string.IsNullOrWhiteSpace(battalion.DisplayName) || battalion.Strength < 0 || battalion.Strength > 100 ||
                    battalion.SubordinateUnits == null || battalion.Platoons == null || battalion.Platoons.Count == 0 || !ids.Add(battalion.Id))
                {
                    error = "Operational scenario contains a malformed friendly battalion";
                    return false;
                }
            }
            foreach (OperationalBattalionState battalion in scenario.EnemyBattalions)
            {
                if (battalion == null || battalion.Side != OperationalSide.Pla || string.IsNullOrWhiteSpace(battalion.Id) ||
                    string.IsNullOrWhiteSpace(battalion.DisplayName) || battalion.Strength < 0 || battalion.Strength > 100 ||
                    battalion.SubordinateUnits == null || battalion.Platoons == null || battalion.Platoons.Count == 0 || !ids.Add(battalion.Id))
                {
                    error = "Operational scenario contains a malformed enemy battalion";
                    return false;
                }
            }
            var contactTargets = new HashSet<string>();
            foreach (OperationalContactState contact in scenario.EnemyContacts)
                if (contact == null || string.IsNullOrWhiteSpace(contact.TargetId) || !contactTargets.Add(contact.TargetId))
                {
                    error = "Operational scenario contains malformed contact memory";
                    return false;
                }
            foreach (OperationalBattalionState enemy in scenario.EnemyBattalions)
                if (!contactTargets.Contains(enemy.Id))
                {
                    error = "Operational scenario is missing enemy contact memory";
                    return false;
                }
            error = null;
            return true;
        }

        public static OperationalScenarioOutcome Evaluate(OperationalScenarioState scenario, out string summary)
        {
            foreach (OperationalBattalionState enemy in scenario.EnemyBattalions)
                if (enemy.Strength > 0 && enemy.Position.Equals(scenario.PrimaryObjective))
                {
                    summary = $"{enemy.DisplayName} seized the primary objective.";
                    return OperationalScenarioOutcome.PlaVictory;
                }
            bool anyEnemyEffective = false;
            foreach (OperationalBattalionState enemy in scenario.EnemyBattalions)
                anyEnemyEffective |= enemy.Strength > 0;
            if (!anyEnemyEffective)
            {
                summary = "All assessed PLA battalions are combat ineffective.";
                return OperationalScenarioOutcome.UsmcVictory;
            }
            if (scenario.TurnNumber > scenario.TurnLimit)
            {
                summary = "The USMC held the primary objective through the operational deadline.";
                return OperationalScenarioOutcome.UsmcVictory;
            }
            summary = null;
            return OperationalScenarioOutcome.InProgress;
        }

        public static List<OperationalPlatoonState> BuildPlatoons(
            string battalionId, string battalionShortName, OperationalBattalionConfiguration configuration)
        {
            var result = new List<OperationalPlatoonState>();
            switch (configuration)
            {
                case OperationalBattalionConfiguration.UsmcReinforcedAssault:
                    AddThreeRifleCompanies(result, battalionId, battalionShortName, "Alpha", "Bravo", "Charlie");
                    Add(result, battalionId, battalionShortName, "Weapons Company", "Mortar Platoon", "mortars", TacticalPlatoonRole.Weapons);
                    Add(result, battalionId, battalionShortName, "Weapons Company", "Combined Antiarmor Platoon", "antiarmor", TacticalPlatoonRole.Weapons);
                    Add(result, battalionId, battalionShortName, "Attached Engineers", "Combat Engineer Platoon", "engineers", TacticalPlatoonRole.Engineers);
                    break;
                case OperationalBattalionConfiguration.UsmcLittoralScreen:
                    AddThreeRifleCompanies(result, battalionId, battalionShortName, "Echo", "Fox", "Golf");
                    Add(result, battalionId, battalionShortName, "Weapons Company", "Mortar Platoon", "mortars", TacticalPlatoonRole.Weapons);
                    Add(result, battalionId, battalionShortName, "Weapons Company", "Combined Antiarmor Platoon", "antiarmor", TacticalPlatoonRole.Weapons);
                    Add(result, battalionId, battalionShortName, "Attached Reconnaissance", "Scout Platoon", "recon", TacticalPlatoonRole.Reconnaissance);
                    break;
                case OperationalBattalionConfiguration.PlaReconnaissance:
                    AddThreeReconCompanies(result, battalionId, battalionShortName, "1st Recon", "2d Recon", "3d Recon");
                    Add(result, battalionId, battalionShortName, "Support Company", "Firepower Platoon", "firepower-1", TacticalPlatoonRole.Weapons);
                    Add(result, battalionId, battalionShortName, "Support Company", "UAS / Support Platoon", "support-2", TacticalPlatoonRole.Weapons);
                    break;
                default:
                    AddThreeRifleCompanies(result, battalionId, battalionShortName, "1st Maneuver", "2d Maneuver", "3d Maneuver");
                    Add(result, battalionId, battalionShortName, "Firepower Company", "Mortar Platoon", "mortars", TacticalPlatoonRole.Weapons);
                    Add(result, battalionId, battalionShortName, "Firepower Company", "Antiarmor Platoon", "antiarmor", TacticalPlatoonRole.Weapons);
                    Add(result, battalionId, battalionShortName, "Firepower Company", "Machine Gun Platoon", "machine-guns", TacticalPlatoonRole.Weapons);
                    break;
            }
            return result;
        }

        public static void EnsurePlatoonOrderOfBattle(OperationalBattalionState battalion)
        {
            if (battalion == null) return;
            if (battalion.Platoons == null) battalion.Platoons = new List<OperationalPlatoonState>();
            if (battalion.Platoons.Count == 0)
            {
                battalion.Platoons = BuildPlatoons(battalion.Id, battalion.ShortName, battalion.Configuration);
                foreach (OperationalPlatoonState platoon in battalion.Platoons)
                    platoon.Strength = Math.Max(0, Math.Min(platoon.MaximumStrength, battalion.Strength));
            }
        }

        public static int SynchronizeBattalionStrength(OperationalBattalionState battalion)
        {
            if (battalion?.Platoons == null || battalion.Platoons.Count == 0) return battalion?.Strength ?? 0;
            int remaining = 0;
            int maximum = 0;
            foreach (OperationalPlatoonState platoon in battalion.Platoons)
            {
                remaining += Math.Max(0, platoon.Strength);
                maximum += Math.Max(1, platoon.MaximumStrength);
            }
            battalion.Strength = maximum == 0 ? 0 : (int)Math.Round(remaining * 100.0 / maximum);
            return battalion.Strength;
        }

        private static void AddThreeRifleCompanies(List<OperationalPlatoonState> result, string id, string shortName, params string[] companies)
        {
            foreach (string company in companies)
                for (int platoon = 1; platoon <= 3; platoon++)
                    Add(result, id, shortName, company + " Company", $"{company} {platoon}{Ordinal(platoon)} Rifle Platoon",
                        company.ToLowerInvariant() + "-rifle-" + platoon, TacticalPlatoonRole.Rifle);
        }

        private static void AddThreeReconCompanies(List<OperationalPlatoonState> result, string id, string shortName, params string[] companies)
        {
            foreach (string company in companies)
                for (int platoon = 1; platoon <= 3; platoon++)
                    Add(result, id, shortName, company + " Company", $"{company} {platoon}{Ordinal(platoon)} Recon Platoon",
                        company.ToLowerInvariant().Replace(" ", "-") + "-recon-" + platoon, TacticalPlatoonRole.Reconnaissance);
        }

        private static string Ordinal(int value) => value == 1 ? "st" : value == 2 ? "d" : "d";

        private static void Add(List<OperationalPlatoonState> result, string battalionId, string shortName,
            string company, string name, string suffix, TacticalPlatoonRole role)
        {
            result.Add(new OperationalPlatoonState
            {
                Id = battalionId + "-" + suffix,
                DisplayName = shortName + " " + name,
                ParentCompany = company,
                Role = role
            });
        }
    }
}
