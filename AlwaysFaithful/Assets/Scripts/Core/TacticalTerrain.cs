using System;

namespace AlwaysFaithful.Core
{
    public enum TacticalTerrain
    {
        Water,
        Open,
        Rough,
        Highland
    }

    public enum TacticalCover
    {
        None,
        Light,
        Medium,
        Heavy
    }

    [Serializable]
    public sealed class TacticalCell
    {
        public HexCoord Coord;
        public TacticalTerrain Terrain;

        public TacticalCell(HexCoord coord, TacticalTerrain terrain)
        {
            Coord = coord;
            Terrain = terrain;
        }

        public bool IsPassable => Terrain != TacticalTerrain.Water;

        public int MovementCost
        {
            get
            {
                switch (Terrain)
                {
                    case TacticalTerrain.Open: return 1;
                    case TacticalTerrain.Rough: return 2;
                    case TacticalTerrain.Highland: return 3;
                    default: return int.MaxValue;
                }
            }
        }
    }
}
