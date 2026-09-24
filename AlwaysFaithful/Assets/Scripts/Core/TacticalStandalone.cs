using System;
using System.Collections.Generic;

namespace AlwaysFaithful.Core
{
    public enum TacticalDifficulty { Regular, Veteran, Elite }
    public enum TacticalWeather { Clear, Rain, Storm }
    public enum TacticalVisibility { Day, DawnDusk, Night }
    public enum TacticalSecondaryObjectiveType { PreserveForce, AmmunitionDiscipline, IdentifyEnemy }

    [Serializable]
    public sealed class TacticalQuickBattleConfiguration
    {
        public int SchemaVersion = 1;
        public string ScenarioName = "Quick Battle";
        public int Seed;
        public TacticalCompanyPreset CompanyPreset = TacticalCompanyPreset.Balanced;
        public TacticalMissionType MissionType = TacticalMissionType.Attack;
        public TacticalDifficulty Difficulty = TacticalDifficulty.Regular;
        public TacticalWeather Weather = TacticalWeather.Clear;
        public TacticalVisibility Visibility = TacticalVisibility.Day;
        public int TurnLimit = 10;
        public bool TutorialEnabled;
    }

    [Serializable]
    public sealed class TacticalSecondaryObjectiveState
    {
        public TacticalSecondaryObjectiveType Type;
        public string Description;
        public bool Completed;
        public bool Failed;
        public int RewardPoints;
    }

    [Serializable]
    public sealed class TacticalPlatoonCareerRecord
    {
        public string UnitId;
        public string DisplayName;
        public int Battles;
        public int Victories;
        public int Experience;
        public int Strength = TacticalCombatPower.DefaultStrength;
    }

    [Serializable]
    public sealed class TacticalCompanyCareerState
    {
        public int SchemaVersion = 1;
        public int Battles;
        public int Victories;
        public List<TacticalPlatoonCareerRecord> Platoons = new List<TacticalPlatoonCareerRecord>();
    }

    [Serializable]
    public sealed class TacticalAfterActionReport
    {
        public string ScenarioName;
        public TacticalBattleOutcome Outcome;
        public int Turns;
        public int FriendlyStrengthRemaining;
        public int EnemyStrengthRemaining;
        public int AmmunitionExpended;
        public int FireMissionsExpended;
        public int SecondaryObjectivesCompleted;
        public List<string> Highlights = new List<string>();
    }

    [Serializable]
    public sealed class TacticalTutorialState
    {
        public bool Enabled;
        public int Step;
        public bool Completed;
    }

    public static class TacticalStandaloneRules
    {
        public static int FireModifier(TacticalQuickBattleConfiguration configuration)
        {
            if (configuration == null) return 0;
            int modifier = configuration.Weather == TacticalWeather.Rain ? -8 : configuration.Weather == TacticalWeather.Storm ? -15 : 0;
            modifier += configuration.Visibility == TacticalVisibility.DawnDusk ? -6 : configuration.Visibility == TacticalVisibility.Night ? -18 : 0;
            return modifier;
        }

        public static int ObservationRange(TacticalQuickBattleConfiguration configuration, int normalRange)
        {
            if (configuration == null) return normalRange;
            int range = normalRange;
            if (configuration.Weather == TacticalWeather.Rain) range -= 2;
            else if (configuration.Weather == TacticalWeather.Storm) range -= 4;
            if (configuration.Visibility == TacticalVisibility.DawnDusk) range -= 2;
            else if (configuration.Visibility == TacticalVisibility.Night) range -= 5;
            return Math.Max(2, range);
        }

        public static int EnemyStrength(TacticalDifficulty difficulty)
            => difficulty == TacticalDifficulty.Elite ? 115 : difficulty == TacticalDifficulty.Veteran ? 105 : 100;

        public static List<TacticalSecondaryObjectiveState> CreateSecondaryObjectives(TacticalMissionType mission)
        {
            var objectives = new List<TacticalSecondaryObjectiveState>
            {
                new TacticalSecondaryObjectiveState { Type = TacticalSecondaryObjectiveType.PreserveForce, Description = "Finish with every platoon above 50 strength.", RewardPoints = 2 },
                new TacticalSecondaryObjectiveState { Type = TacticalSecondaryObjectiveType.AmmunitionDiscipline, Description = "Finish with at least half the company ammunition.", RewardPoints = 1 }
            };
            if (mission == TacticalMissionType.ReconInForce)
                objectives.Add(new TacticalSecondaryObjectiveState { Type = TacticalSecondaryObjectiveType.IdentifyEnemy, Description = "Identify the complete enemy order of battle.", RewardPoints = 2 });
            return objectives;
        }

        public static string TutorialPrompt(int step)
        {
            switch (step)
            {
                case 0: return "Select a platoon from the company list.";
                case 1: return "Open Orders and choose a movement posture.";
                case 2: return "Move one platoon into covered terrain.";
                case 3: return "Inspect LOS or task Recon against enemy ground.";
                case 4: return "Suppress, smoke, or fire on an enemy position.";
                case 5: return "End the turn and observe reaction policy.";
                default: return "Tutorial complete. Command the company.";
            }
        }
    }
}
