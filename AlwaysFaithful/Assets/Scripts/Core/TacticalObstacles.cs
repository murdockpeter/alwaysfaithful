using System;
using System.Collections.Generic;

namespace AlwaysFaithful.Core
{
    public enum TacticalObstacleType
    {
        None,
        Minefield,
        Wire,
        Roadblock
    }

    public enum TacticalObstacleIntelligence
    {
        Hidden,
        Suspected,
        Detected,
        Identified
    }

    public enum TacticalObstacleEventKind
    {
        Suspected,
        Detected,
        Identified,
        Encountered,
        MovementHalted,
        Marked,
        HastyBreach,
        DeliberateBreach,
        Breached
    }

    [Serializable]
    public sealed class TacticalObstacleState
    {
        public string Id;
        public HexCoord Position;
        public TacticalObstacleType Type;
        public string OwnerSide = "PLA";
        public TacticalObstacleIntelligence UsmcIntelligence = TacticalObstacleIntelligence.Hidden;
        public bool IsBreached;

        public bool IsKnownTo(string side)
            => string.Equals(side, OwnerSide, StringComparison.OrdinalIgnoreCase) ||
               UsmcIntelligence != TacticalObstacleIntelligence.Hidden;
    }

    [Serializable]
    public sealed class TacticalObstacleEvent
    {
        public int Sequence;
        public string BattlefieldId;
        public int Turn;
        public TacticalObstacleEventKind Kind;
        public string UnitId;
        public string ObstacleId;
        public HexCoord Position;
        public TacticalObstacleType Type;
        public TacticalObstacleIntelligence Intelligence;
        public int SuppressionApplied;
        public int StrengthDamage;
        public int ActionPointCost;
        public int EngineerSupplyBefore;
        public int EngineerSupplyAfter;
        public bool Succeeded;
        public string Summary;
    }

    public readonly struct TacticalObstacleEncounter
    {
        public readonly bool Halted;
        public readonly bool NewlyIdentified;
        public readonly int Suppression;
        public readonly int StrengthDamage;

        public TacticalObstacleEncounter(bool halted, bool newlyIdentified, int suppression, int strengthDamage)
        {
            Halted = halted;
            NewlyIdentified = newlyIdentified;
            Suppression = suppression;
            StrengthDamage = strengthDamage;
        }
    }

    public static class TacticalObstacles
    {
        public const int DefaultObstacleCount = 6;
        public const int DefaultDefensiveObstacleBudget = 3;
        public const int DefaultEngineerSupply = 5;
        public const int DetectActionPointCost = 2;
        public const int HastyBreachActionPointCost = 2;
        public const int HastyBreachSupplyCost = 1;
        public const int HastyBreachChance = 70;
        public const int DeliberateBreachActionPointCost = 4;
        public const int DeliberateBreachSupplyCost = 2;
        public const int EngineerRangeHexes = 1;

        public static int MovementCost(TacticalObstacleType type, TacticalObstacleIntelligence intelligence, bool breached)
        {
            if (type == TacticalObstacleType.None || breached || intelligence == TacticalObstacleIntelligence.Hidden) return 0;
            if (intelligence == TacticalObstacleIntelligence.Suspected) return 1;
            switch (type)
            {
                case TacticalObstacleType.Minefield: return 2;
                case TacticalObstacleType.Wire: return 2;
                case TacticalObstacleType.Roadblock: return 3;
                default: return 0;
            }
        }

        public static TacticalObstacleIntelligence ImproveIntelligence(
            TacticalObstacleState obstacle, TacticalObstacleIntelligence minimum)
        {
            if (obstacle == null) return TacticalObstacleIntelligence.Hidden;
            if (minimum > obstacle.UsmcIntelligence) obstacle.UsmcIntelligence = minimum;
            return obstacle.UsmcIntelligence;
        }

        public static bool CanDetect(TacticalUnitState unit, TacticalObstacleState obstacle, HexCoord target, bool isEngineer)
            => isEngineer && unit != null && unit.CanMove && unit.RemainingActionPoints >= DetectActionPointCost &&
               HexCoord.Distance(unit.Position, target) <= EngineerRangeHexes &&
               (obstacle == null || !obstacle.IsBreached);

        public static bool TryDetect(TacticalUnitState unit, TacticalObstacleState obstacle, HexCoord target, bool isEngineer)
        {
            if (!CanDetect(unit, obstacle, target, isEngineer) || !unit.TrySpendActionPoints(DetectActionPointCost)) return false;
            if (obstacle != null) obstacle.UsmcIntelligence = TacticalObstacleIntelligence.Identified;
            return true;
        }

        public static bool CanBreach(TacticalUnitState unit, TacticalObstacleState obstacle, bool isEngineer, bool deliberate)
        {
            int actionPoints = deliberate ? DeliberateBreachActionPointCost : HastyBreachActionPointCost;
            int supply = deliberate ? DeliberateBreachSupplyCost : HastyBreachSupplyCost;
            return isEngineer && unit != null && unit.CanMove && unit.RemainingActionPoints >= actionPoints &&
                   unit.EngineerSupply >= supply && obstacle != null && !obstacle.IsBreached &&
                   obstacle.UsmcIntelligence >= TacticalObstacleIntelligence.Detected &&
                   HexCoord.Distance(unit.Position, obstacle.Position) <= EngineerRangeHexes;
        }

        public static bool TryBreach(TacticalUnitState unit, TacticalObstacleState obstacle, bool isEngineer,
            bool deliberate, int seed, out bool succeeded)
        {
            succeeded = false;
            if (!CanBreach(unit, obstacle, isEngineer, deliberate)) return false;
            int actionPoints = deliberate ? DeliberateBreachActionPointCost : HastyBreachActionPointCost;
            int supply = deliberate ? DeliberateBreachSupplyCost : HastyBreachSupplyCost;
            if (!unit.TrySpendActionPoints(actionPoints)) return false;
            unit.EngineerSupply -= supply;
            succeeded = deliberate || StableRoll(seed, obstacle.Position, 100) < HastyBreachChance;
            if (succeeded) obstacle.IsBreached = true;
            else unit.ApplySuppressionPoints(5);
            return true;
        }

        public static TacticalObstacleEncounter Enter(
            TacticalObstacleState obstacle, TacticalMovementPosture posture, string movingSide, int seed)
        {
            if (obstacle == null || obstacle.Type == TacticalObstacleType.None || obstacle.IsBreached ||
                string.Equals(obstacle.OwnerSide, movingSide, StringComparison.OrdinalIgnoreCase))
                return new TacticalObstacleEncounter(false, false, 0, 0);

            bool newlyIdentified = obstacle.UsmcIntelligence != TacticalObstacleIntelligence.Identified;
            obstacle.UsmcIntelligence = TacticalObstacleIntelligence.Identified;
            int suppression = 0;
            int damage = 0;
            if (obstacle.Type == TacticalObstacleType.Minefield)
            {
                suppression = posture == TacticalMovementPosture.Quick ? 3 : posture == TacticalMovementPosture.Bounding ? 1 : 2;
                int baseDamage = posture == TacticalMovementPosture.Quick ? 8 : posture == TacticalMovementPosture.Bounding ? 2 : 5;
                damage = baseDamage + StableRoll(seed, obstacle.Position, 3);
            }
            else if (obstacle.Type == TacticalObstacleType.Wire)
            {
                suppression = posture == TacticalMovementPosture.Quick ? 2 : 1;
            }
            return new TacticalObstacleEncounter(true, newlyIdentified, suppression, damage);
        }

        public static List<TacticalObstacleState> GenerateDefensiveLayout(
            TacticalBattlefieldState battlefield, HexCoord objective, ISet<HexCoord> excluded)
        {
            var candidates = new List<TacticalBattlefieldCell>();
            if (battlefield == null || battlefield.Cells == null) return new List<TacticalObstacleState>();
            foreach (TacticalBattlefieldCell cell in battlefield.Cells)
            {
                int range = HexCoord.Distance(cell.LocalCoord, objective);
                if (cell.Terrain == TacticalTerrain.Water || range < 2 || range > 4 ||
                    excluded != null && excluded.Contains(cell.LocalCoord)) continue;
                candidates.Add(cell);
            }
            candidates.Sort((a, b) =>
            {
                int aScore = LayoutScore(battlefield.Seed, battlefield.BattlefieldId, a.LocalCoord);
                int bScore = LayoutScore(battlefield.Seed, battlefield.BattlefieldId, b.LocalCoord);
                int byScore = aScore.CompareTo(bScore);
                if (byScore != 0) return byScore;
                int byQ = a.LocalCoord.Q.CompareTo(b.LocalCoord.Q);
                return byQ != 0 ? byQ : a.LocalCoord.R.CompareTo(b.LocalCoord.R);
            });

            var result = new List<TacticalObstacleState>();
            int count = Math.Min(DefaultObstacleCount, candidates.Count);
            for (int index = 0; index < count; index++)
            {
                TacticalObstacleType type = index % 3 == 0 ? TacticalObstacleType.Minefield :
                    index % 3 == 1 ? TacticalObstacleType.Wire : TacticalObstacleType.Roadblock;
                result.Add(new TacticalObstacleState
                {
                    Id = $"OBS-{index + 1:D2}-{candidates[index].LocalCoord.Q:D2}-{candidates[index].LocalCoord.R:D2}",
                    Position = candidates[index].LocalCoord,
                    Type = type,
                    OwnerSide = "PLA"
                });
            }
            return result;
        }

        public static void ApplyToCell(TacticalMovementCell cell, TacticalObstacleState obstacle)
        {
            if (cell == null) return;
            cell.Obstacle = obstacle?.Type ?? TacticalObstacleType.None;
            cell.ObstacleIntelligence = obstacle?.UsmcIntelligence ?? TacticalObstacleIntelligence.Hidden;
            cell.ObstacleBreached = obstacle != null && obstacle.IsBreached;
            cell.ObstacleOwnerSide = obstacle?.OwnerSide;
        }

        private static int StableRoll(int seed, HexCoord coord, int range)
        {
            unchecked
            {
                uint value = (uint)seed ^ ((uint)coord.Q * 73856093u) ^ ((uint)coord.R * 19349663u);
                value ^= value << 13;
                value ^= value >> 17;
                value ^= value << 5;
                return (int)(value % (uint)Math.Max(1, range));
            }
        }

        private static int LayoutScore(int seed, string battlefieldId, HexCoord coord)
            => StableRoll(seed ^ StableStringHash(battlefieldId), coord, 100000);

        private static int StableStringHash(string value)
        {
            unchecked
            {
                int hash = 17;
                if (value != null)
                    foreach (char character in value) hash = hash * 31 + character;
                return hash;
            }
        }
    }
}
