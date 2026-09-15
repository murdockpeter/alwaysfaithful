using System;

namespace AlwaysFaithful.Core
{
    public enum UnitReadiness
    {
        Available,
        Moving,
        Spent
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

        public TacticalUnitState(string id, string displayName, HexCoord position, int maximumActionPoints)
        {
            Id = id;
            DisplayName = displayName;
            Position = position;
            MaximumActionPoints = Math.Max(1, maximumActionPoints);
            RemainingActionPoints = MaximumActionPoints;
            Readiness = UnitReadiness.Available;
        }

        public bool CanMove => Readiness == UnitReadiness.Available && RemainingActionPoints > 0;

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
            RemainingActionPoints = MaximumActionPoints;
            Readiness = UnitReadiness.Available;
            IsSelected = false;
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
