using System;
using System.Collections.Generic;

namespace AlwaysFaithful.Core
{
    public enum TacticalPosture
    {
        Attack,
        Defend
    }

    // Standalone-only mission variety layered on top of Posture (which now only
    // means "which hex-siting shape ChooseObjective uses" — Attack-shaped or
    // Defend-shaped). A BattleRequest-driven battle only ever sets this to a
    // plain Attack/Defend mirror of Posture; the frozen SOU-interop contract
    // never rolls or accepts a new mission type.
    public enum TacticalMissionType
    {
        Attack,
        Defend,
        Raid,
        ReconInForce,
        Withdrawal
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
        public TacticalMissionType MissionType = TacticalMissionType.Attack;
        public int TurnLimit;
        public int BattleStartTurn;
        public TacticalBattleOutcome Outcome = TacticalBattleOutcome.InProgress;
        public int OutcomeTurn;
        public string OutcomeSummary;
        public int RequiredObjectiveHoldTurns = TacticalVictory.DefaultObjectiveHoldTurns;
        public int ObjectiveHoldTurns;
        public int LastObjectiveHoldEvaluationTurn;
        public bool ObjectiveOvertimeGranted;
        public int ObjectiveOvertimeTurnsGranted;
        public bool RaidObjectiveAchieved;
        public List<string> ObservedEnemyIds = new List<string>();
    }

    [Serializable]
    public sealed class TacticalObjectiveEvent
    {
        public int Sequence;
        public string BattlefieldId;
        public int Turn;
        public HexCoord Hex;
        public bool ControlledByUsmc;
        public bool IsHoldProgress;
        public int HoldTurns;
        public int RequiredHoldTurns;
        public bool IsOvertime;
        public int ExtendedTurnLimit;
    }

    public static class TacticalVictory
    {
        public const int DefaultTurnLimit = 6;
        public const int DefaultObjectiveHoldTurns = 3;
        public const int DefaultObjectiveOvertimeTurns = 3;
        public const int ObjectiveExclusionRadiusHexes = 4;

        public static int HoldTurnsRequired(TacticalObjectiveState objective)
            => objective != null && objective.RequiredObjectiveHoldTurns > 0
                ? objective.RequiredObjectiveHoldTurns
                : DefaultObjectiveHoldTurns;

        public static TacticalBattleOutcome Evaluate(
            TacticalObjectiveState objective,
            TacticalUnitState usmc,
            IReadOnlyList<TacticalUnitState> enemies,
            IReadOnlyDictionary<HexCoord, TacticalMovementCell> board,
            int currentTurn,
            out string summary)
        {
            bool usmcReduced = usmc != null && !TacticalCombatPower.IsCombatEffective(usmc);
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

            if (objective.MissionType == TacticalMissionType.Raid)
            {
                if (!objective.RaidObjectiveAchieved && enemies != null && enemies.Count > 0 &&
                    ReducedCount(enemies) * 2 >= enemies.Count)
                    objective.RaidObjectiveAchieved = true;
                if (objective.RaidObjectiveAchieved)
                {
                    summary = "Raid objective achieved: PLA element strength broken.";
                    return TacticalBattleOutcome.UsmcVictory;
                }
            }
            else if (objective.MissionType == TacticalMissionType.ReconInForce)
            {
                if (enemies != null && enemies.Count > 0 && objective.ObservedEnemyIds.Count >= enemies.Count)
                {
                    summary = "Enemy roster fully identified.";
                    return TacticalBattleOutcome.UsmcVictory;
                }
            }

            if (objective.MissionType == TacticalMissionType.Attack || objective.MissionType == TacticalMissionType.Defend)
            {
                int requiredHoldTurns = HoldTurnsRequired(objective);
                objective.RequiredObjectiveHoldTurns = requiredHoldTurns;
                bool usmcControls = usmc != null && board.TryGetValue(objective.ObjectiveHex, out TacticalMovementCell objectiveCell) &&
                    objectiveCell.OccupantId == usmc.Id;
                // Evaluate is normally called once as the enemy phase hands
                // back to USMC. Guarding on turn makes the rule idempotent for
                // save/load checks and any extra presentation refreshes.
                if (objective.LastObjectiveHoldEvaluationTurn != currentTurn)
                {
                    objective.LastObjectiveHoldEvaluationTurn = currentTurn;
                    objective.ObjectiveHoldTurns = usmcControls ? objective.ObjectiveHoldTurns + 1 : 0;
                }
                if (objective.ObjectiveHoldTurns >= requiredHoldTurns)
                {
                    summary = $"Objective {objective.ObjectiveHex} held for {requiredHoldTurns} consecutive turns.";
                    return TacticalBattleOutcome.UsmcVictory;
                }
            }

            int turnsElapsed = currentTurn - objective.BattleStartTurn;
            if (turnsElapsed >= objective.TurnLimit)
            {
                if (objective.MissionType == TacticalMissionType.Withdrawal)
                {
                    // Reaching this branch already proves the platoon survived
                    // (usmcReduced would have returned UsmcDefeat above otherwise);
                    // withdrawal success never depends on holding any hex.
                    summary = "Platoon withdrew intact.";
                    return TacticalBattleOutcome.UsmcVictory;
                }
                if ((objective.MissionType == TacticalMissionType.Attack || objective.MissionType == TacticalMissionType.Defend) &&
                    !objective.ObjectiveOvertimeGranted)
                {
                    objective.ObjectiveOvertimeGranted = true;
                    objective.ObjectiveOvertimeTurnsGranted = DefaultObjectiveOvertimeTurns;
                    objective.TurnLimit += DefaultObjectiveOvertimeTurns;
                    summary = null;
                    return TacticalBattleOutcome.InProgress;
                }
                summary = objective.MissionType == TacticalMissionType.Attack || objective.MissionType == TacticalMissionType.Defend
                    ? $"Turn limit reached without holding {objective.ObjectiveHex} for {objective.RequiredObjectiveHoldTurns} consecutive turns."
                    : $"Turn limit reached without securing {objective.ObjectiveHex}.";
                return TacticalBattleOutcome.Stalemate;
            }

            summary = null;
            return TacticalBattleOutcome.InProgress;
        }

        public static TacticalBattleOutcome Evaluate(
            TacticalObjectiveState objective,
            IReadOnlyList<TacticalUnitState> friendlies,
            IReadOnlyList<TacticalUnitState> enemies,
            IReadOnlyDictionary<HexCoord, TacticalMovementCell> board,
            int currentTurn,
            out string summary)
        {
            bool allFriendliesReduced = AllReduced(friendlies);
            bool allEnemiesReduced = AllReduced(enemies);
            if (allFriendliesReduced && allEnemiesReduced)
            {
                summary = "Mutual destruction: both forces combat ineffective.";
                return TacticalBattleOutcome.Draw;
            }
            if (allFriendliesReduced)
            {
                summary = "USMC company combat ineffective.";
                return TacticalBattleOutcome.UsmcDefeat;
            }
            if (allEnemiesReduced)
            {
                summary = "PLANMC company combat ineffective.";
                return TacticalBattleOutcome.UsmcVictory;
            }

            // Reuse the established mission/deadline rules with a surviving
            // representative, but temporarily mirror objective occupancy so
            // any friendly platoon can seize/hold the company objective.
            TacticalUnitState representative = FirstEffective(friendlies);
            if (representative == null)
            {
                summary = "USMC company combat ineffective.";
                return TacticalBattleOutcome.UsmcDefeat;
            }
            string originalOccupant = null;
            bool substituted = false;
            if (board.TryGetValue(objective.ObjectiveHex, out TacticalMovementCell objectiveCell))
            {
                originalOccupant = objectiveCell.OccupantId;
                TacticalUnitState controller = FindById(friendlies, originalOccupant);
                if (controller != null && controller.Id != representative.Id)
                {
                    objectiveCell.OccupantId = representative.Id;
                    substituted = true;
                }
            }
            TacticalBattleOutcome outcome = Evaluate(objective, representative, enemies, board, currentTurn, out summary);
            if (substituted && board.TryGetValue(objective.ObjectiveHex, out TacticalMovementCell restoreCell))
                restoreCell.OccupantId = originalOccupant;
            return outcome;
        }

        // Tracks cumulative recon-in-force progress — called once per turn
        // handback alongside Evaluate, since fog can regress an enemy back to
        // Hidden after a stale track expires; achievement should not un-happen.
        public static void TrackObservation(TacticalObjectiveState objective, IReadOnlyDictionary<string, TacticalContactState> contacts)
        {
            if (objective == null || objective.MissionType != TacticalMissionType.ReconInForce || contacts == null) return;
            foreach (TacticalContactState contact in contacts.Values)
            {
                if (contact.State < TacticalVisibilityState.Identified) continue;
                if (!objective.ObservedEnemyIds.Contains(contact.TargetId)) objective.ObservedEnemyIds.Add(contact.TargetId);
            }
        }

        private static bool AllReduced(IReadOnlyList<TacticalUnitState> units)
        {
            if (units == null || units.Count == 0) return false;
            foreach (TacticalUnitState unit in units)
                if (TacticalCombatPower.IsCombatEffective(unit)) return false;
            return true;
        }

        private static TacticalUnitState FirstEffective(IReadOnlyList<TacticalUnitState> units)
        {
            if (units == null) return null;
            foreach (TacticalUnitState unit in units)
                if (TacticalCombatPower.IsCombatEffective(unit)) return unit;
            return null;
        }

        private static TacticalUnitState FindById(IReadOnlyList<TacticalUnitState> units, string id)
        {
            if (units == null || string.IsNullOrEmpty(id)) return null;
            foreach (TacticalUnitState unit in units)
                if (unit != null && unit.Id == id) return unit;
            return null;
        }

        private static int ReducedCount(IReadOnlyList<TacticalUnitState> units)
        {
            int count = 0;
            foreach (TacticalUnitState unit in units)
                if (unit.CombatStatus == TacticalCombatStatus.Reduced) count++;
            return count;
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

        // Raid and ReconInForce both site an objective the same way Attack does
        // (farthest passable hex); Withdrawal sites like Defend (the platoon's
        // own start hex) since it never advances toward new ground.
        public static TacticalPosture SitingShapeFor(TacticalMissionType missionType)
            => missionType == TacticalMissionType.Defend || missionType == TacticalMissionType.Withdrawal
                ? TacticalPosture.Defend
                : TacticalPosture.Attack;

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
