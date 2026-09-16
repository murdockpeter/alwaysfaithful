using System;
using System.Collections.Generic;

namespace AlwaysFaithful.Core
{
    public enum TacticalPosture
    {
        Attack,
        Defend
    }

    public enum TacticalBattleOutcome
    {
        InProgress,
        UsmcVictory,
        UsmcDefeat,
        Draw,
        Stalemate
    }

    [Serializable]
    public sealed class TacticalObjectiveState
    {
        public HexCoord ObjectiveHex;
        public TacticalPosture Posture;
        public int TurnLimit;
        public int BattleStartTurn;
        public TacticalBattleOutcome Outcome = TacticalBattleOutcome.InProgress;
        public int OutcomeTurn;
        public string OutcomeSummary;
    }

    public static class TacticalVictory
    {
        public const int DefaultTurnLimit = 6;
        public const int ObjectiveExclusionRadiusHexes = 4;

        public static TacticalBattleOutcome Evaluate(
            TacticalObjectiveState objective,
            TacticalUnitState usmc,
            IReadOnlyList<TacticalUnitState> enemies,
            IReadOnlyDictionary<HexCoord, TacticalMovementCell> board,
            int currentTurn,
            out string summary)
        {
            bool usmcReduced = usmc != null && usmc.CombatStatus == TacticalCombatStatus.Reduced;
            bool allEnemiesReduced = AllReduced(enemies);

            if (usmcReduced && allEnemiesReduced)
            {
                summary = "Mutual destruction: both forces combat ineffective.";
                return TacticalBattleOutcome.Draw;
            }
            if (usmcReduced)
            {
                summary = "USMC platoon combat ineffective.";
                return TacticalBattleOutcome.UsmcDefeat;
            }
            if (allEnemiesReduced)
            {
                summary = "All observed PLA elements combat ineffective.";
                return TacticalBattleOutcome.UsmcVictory;
            }

            int turnsElapsed = currentTurn - objective.BattleStartTurn;
            if (turnsElapsed >= objective.TurnLimit)
            {
                bool usmcControls = usmc != null && board.TryGetValue(objective.ObjectiveHex, out TacticalMovementCell cell) &&
                    cell.OccupantId == usmc.Id;
                if (usmcControls)
                {
                    summary = $"Objective {objective.ObjectiveHex} secured by the turn limit.";
                    return TacticalBattleOutcome.UsmcVictory;
                }
                summary = $"Turn limit reached without securing {objective.ObjectiveHex}.";
                return TacticalBattleOutcome.Stalemate;
            }

            summary = null;
            return TacticalBattleOutcome.InProgress;
        }

        private static bool AllReduced(IReadOnlyList<TacticalUnitState> units)
        {
            if (units == null || units.Count == 0) return false;
            foreach (TacticalUnitState unit in units)
                if (unit.CombatStatus != TacticalCombatStatus.Reduced) return false;
            return true;
        }

        // Defend holds the same hex USMC already deploys on. Attack picks the
        // farthest passable land hex from that center, excluding a band around
        // the center and each enemy's start position (an enemy-occupied hex can
        // never physically be reached — movement is hard-blocked onto it — so
        // the objective must always be somewhere USMC can actually stand).
        public static HexCoord ChooseObjective(
            IReadOnlyDictionary<HexCoord, TacticalMovementCell> board,
            HexCoord center,
            TacticalPosture posture,
            IEnumerable<HexCoord> enemyStartPositions,
            string battlefieldId)
        {
            if (posture == TacticalPosture.Defend) return center;

            var excluded = new List<HexCoord> { center };
            foreach (HexCoord position in enemyStartPositions) excluded.Add(position);

            HexCoord best = center;
            int bestDistance = -1;
            int bestTieBreak = int.MaxValue;
            foreach (KeyValuePair<HexCoord, TacticalMovementCell> pair in board)
            {
                if (!pair.Value.IsPassable) continue;
                bool tooClose = false;
                foreach (HexCoord exclude in excluded)
                {
                    if (HexCoord.Distance(pair.Key, exclude) > ObjectiveExclusionRadiusHexes) continue;
                    tooClose = true;
                    break;
                }
                if (tooClose) continue;
                int distance = HexCoord.Distance(pair.Key, center);
                int tieBreak = ObjectiveRoll(battlefieldId, pair.Key);
                if (distance > bestDistance || distance == bestDistance && tieBreak < bestTieBreak)
                {
                    best = pair.Key;
                    bestDistance = distance;
                    bestTieBreak = tieBreak;
                }
            }
            return best;
        }

        public static TacticalPosture ChoosePosture(string battlefieldId)
            => ObjectiveRoll(battlefieldId, default) % 2 == 0 ? TacticalPosture.Attack : TacticalPosture.Defend;

        // Mirrors the FNV-seed + coordinate-prime-xorshift idiom already used by
        // TacticalBattlefieldExtractor's cover generation and TacticalEnemyTurn's
        // TieBreak, so posture and objective placement are fully deterministic
        // per battlefield.
        private static int ObjectiveRoll(string battlefieldId, HexCoord coord)
        {
            int seed = TacticalDirectFire.CreateSeed(battlefieldId, 0, 5);
            unchecked
            {
                uint value = (uint)seed ^ ((uint)coord.Q * 73856093u) ^ ((uint)coord.R * 19349663u);
                value ^= value << 13;
                value ^= value >> 17;
                value ^= value << 5;
                return (int)(value % 1000u);
            }
        }
    }
}
