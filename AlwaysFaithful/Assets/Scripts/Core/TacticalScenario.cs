using System.Collections.Generic;

namespace AlwaysFaithful.Core
{
    // Standalone-only scenario variability: turn limit, enemy roster, and
    // mission type, all hashed from BattlefieldId exactly like
    // TacticalVictory.ObjectiveRoll and TacticalBattlefieldExtractor's cover
    // generation, so a given battlefield always rerolls the same way.
    // Sequences 1-4 are cover/built-up generation and 5 is ObjectiveRoll's
    // tie-break. Within this file: 6=turn limit, 7=roster count, 9-10=roster
    // slots 1-2 (ChooseEnemyRoster's loop starts at slot=1, so 8+1..8+2),
    // 11=mission type; 12+ still reserved.
    public static class TacticalScenario
    {
        public const int MinTurnLimit = 5;
        public const int MaxTurnLimit = 8;
        public const int MinRosterSize = 1;
        public const int MaxRosterSize = 3;
        public const string RifleRole = "pla-rifle-squad";
        public const string SupportRole = "pla-support-team";

        public static int ChooseTurnLimit(string battlefieldId)
            => MinTurnLimit + (int)((uint)ScenarioRoll(battlefieldId, 6) % (MaxTurnLimit - MinTurnLimit + 1));

        // Slot 0 is always RifleRole (a generated scenario always has a clear
        // "main" enemy formation for FindObservationDeployment's Observed
        // slot); remaining slots are a coin flip per slot.
        public static List<string> ChooseEnemyRoster(string battlefieldId)
        {
            int count = MinRosterSize + (int)((uint)ScenarioRoll(battlefieldId, 7) % (MaxRosterSize - MinRosterSize + 1));
            var roster = new List<string> { RifleRole };
            for (int slot = 1; slot < count; slot++)
                roster.Add(ScenarioRoll(battlefieldId, 8 + slot) % 2 == 0 ? RifleRole : SupportRole);
            return roster;
        }

        public static TacticalMissionType ChooseMissionType(string battlefieldId)
        {
            uint roll = (uint)ScenarioRoll(battlefieldId, 11) % 100u;
            if (roll < 35) return TacticalMissionType.Attack;
            if (roll < 70) return TacticalMissionType.Defend;
            if (roll < 80) return TacticalMissionType.Raid;
            if (roll < 90) return TacticalMissionType.ReconInForce;
            return TacticalMissionType.Withdrawal;
        }

        // Mirrors TacticalVictory.ObjectiveRoll's seed + xorshift idiom.
        private static int ScenarioRoll(string battlefieldId, int sequence)
        {
            int seed = TacticalDirectFire.CreateSeed(battlefieldId, 0, sequence);
            unchecked
            {
                uint value = (uint)seed + 0x9E3779B9u;
                value ^= value << 13;
                value ^= value >> 17;
                value ^= value << 5;
                return (int)(value & 0x7fffffff);
            }
        }
    }
}
