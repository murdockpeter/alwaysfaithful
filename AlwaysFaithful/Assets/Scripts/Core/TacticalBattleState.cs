using System;

namespace AlwaysFaithful.Core
{
    public enum UnitReadiness
    {
        Available,
        Moving,
        Spent
    }

    public enum TacticalCombatStatus
    {
        Ready,
        Suppressed,
        Disrupted,
        Reduced
    }

    [Serializable]
    public sealed class TacticalUnitState
    {
        public string Id;
        public string DisplayName;
        public HexCoord Position;
        public int MaximumActionPoints;
        public int RemainingActionPoints;
        public UnitReadiness Readiness;
        public bool IsSelected;
        public int SuppressionPoints;
        public TacticalCombatStatus CombatStatus;
        public TacticalMovementPosture MovementPosture = TacticalMovementPosture.Tactical;
        public TacticalMovementPosture LastMovementPosture = TacticalMovementPosture.Tactical;
        public TacticalReactionPolicy ReactionPolicy = TacticalReactionPolicy.WeaponsFree;
        public int FacingSector;
        public int ReactionPoints = TacticalFireAndManeuver.ReactionPointMaximum;
        public bool MovedThisTurn;
        public bool FiredThisTurn;
        public bool WasFiredUponThisTurn;
        public int MaximumStrength = TacticalCombatPower.DefaultStrength;
        public int Strength = TacticalCombatPower.DefaultStrength;
        public int MaximumSupply = TacticalCombatPower.DefaultSupply;
        public int Supply = TacticalCombatPower.DefaultSupply;
        public int EntrenchmentLevel;
        public HexCoord EntrenchedPosition;
        public bool IsReserve;
        public bool HasArrived = true;
        public int ReinforcementTurn;

        public TacticalUnitState(string id, string displayName, HexCoord position, int maximumActionPoints)
        {
            Id = id;
            DisplayName = displayName;
            Position = position;
            MaximumActionPoints = Math.Max(1, maximumActionPoints);
            RemainingActionPoints = MaximumActionPoints;
            Readiness = UnitReadiness.Available;
            CombatStatus = TacticalCombatStatus.Ready;
        }

        public bool CanMove => HasArrived && Strength > 0 && Readiness == UnitReadiness.Available && RemainingActionPoints > 0 &&
            CombatStatus != TacticalCombatStatus.Reduced;

        public bool CanFire => CanMove && CombatStatus != TacticalCombatStatus.Disrupted;

        public bool CanRally => Readiness == UnitReadiness.Available && RemainingActionPoints > 0 &&
            CombatStatus != TacticalCombatStatus.Ready;

        public void ApplySuppressionPoints(int delta)
        {
            SuppressionPoints = Math.Max(0, Math.Min(TacticalSuppression.MaximumPoints, SuppressionPoints + delta));
            CombatStatus = TacticalSuppression.ComputeStatus(SuppressionPoints);
        }

        public bool TryRally(int actionPointCost)
        {
            if (!CanRally || actionPointCost <= 0 || actionPointCost > RemainingActionPoints) return false;
            RemainingActionPoints -= actionPointCost;
            Readiness = RemainingActionPoints > 0 ? UnitReadiness.Available : UnitReadiness.Spent;
            IsSelected = false;
            ApplySuppressionPoints(-TacticalSuppression.RallyRecoveryAmount);
            return true;
        }

        public bool TryBeginMove(int actionPointCost)
        {
            if (!CanMove || actionPointCost <= 0 || actionPointCost > RemainingActionPoints) return false;
            RemainingActionPoints -= actionPointCost;
            Readiness = UnitReadiness.Moving;
            return true;
        }

        public bool TrySpendActionPoints(int actionPointCost)
        {
            if (!CanMove || actionPointCost <= 0 || actionPointCost > RemainingActionPoints) return false;
            RemainingActionPoints -= actionPointCost;
            Readiness = RemainingActionPoints > 0 ? UnitReadiness.Available : UnitReadiness.Spent;
            IsSelected = false;
            return true;
        }

        public void CompleteMove(HexCoord destination)
        {
            Position = destination;
            Readiness = RemainingActionPoints > 0 ? UnitReadiness.Available : UnitReadiness.Spent;
            IsSelected = false;
        }

        public void BeginTurn()
        {
            ApplySuppressionPoints(-TacticalSuppression.PassiveRecoveryAmount);
            RemainingActionPoints = MaximumActionPoints;
            Readiness = UnitReadiness.Available;
            IsSelected = false;
            ReactionPoints = TacticalFireAndManeuver.ReactionPointMaximum;
            MovedThisTurn = false;
            FiredThisTurn = false;
            WasFiredUponThisTurn = false;
        }
    }

    [Serializable]
    public sealed class TacticalTurnState
    {
        public int TurnNumber = 1;
        public string ActiveSide = "USMC";

        public void EndTurn(TacticalUnitState unit)
        {
            TurnNumber++;
            unit.BeginTurn();
        }
    }
}
