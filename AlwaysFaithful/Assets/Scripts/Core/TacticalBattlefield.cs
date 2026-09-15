using System;
using System.Collections.Generic;

namespace AlwaysFaithful.Core
{
    [Serializable]
    public sealed class TacticalBattlefieldCell
    {
        public string Id;
        public HexCoord LocalCoord;
        public double Longitude;
        public double Latitude;
        public float ElevationMetres;
        public TacticalTerrain Terrain;
    }

    [Serializable]
    public sealed class TacticalBattlefieldState
    {
        public const int CurrentSchemaVersion = 2;

        public int SchemaVersion = CurrentSchemaVersion;
        public string BattlefieldId;
        public HexCoord ParentHex;
        public int Width;
        public int Height;
        public int CellSizeMetres;
        public double CenterLongitude;
        public double CenterLatitude;
        public double OriginLongitude;
        public double OriginLatitude;
        public double West;
        public double East;
        public double South;
        public double North;
        public List<TacticalBattlefieldCell> Cells = new List<TacticalBattlefieldCell>();
        public List<TacticalMovementEvent> MovementEvents = new List<TacticalMovementEvent>();
        public List<TacticalFireEvent> FireEvents = new List<TacticalFireEvent>();

        public bool Contains(double longitude, double latitude)
            => longitude >= West && longitude <= East && latitude >= South && latitude <= North;

        public bool TryGeographicToLocal(double longitude, double latitude, out HexCoord coord)
        {
            double eastWestKm = (longitude - OriginLongitude) * 111.32d * Math.Cos(CenterLatitude * Math.PI / 180d);
            double northSouthKm = (latitude - OriginLatitude) * 110.574d;
            int q = (int)Math.Round(eastWestKm * 1000d / (CellSizeMetres * Math.Sqrt(3d) * .5d));
            int r = (int)Math.Round(northSouthKm * 1000d / CellSizeMetres - ((q & 1) == 0 ? 0d : .5d));
            coord = new HexCoord(q, r);
            return q >= 0 && q < Width && r >= 0 && r < Height;
        }
    }

    public static class TacticalBattlefieldExtractor
    {
        public const int DefaultWidth = 25;
        public const int DefaultHeight = 19;
        public const int CellSizeMetres = 250;

        public static TacticalBattlefieldState Extract(
            HexCoord parentHex,
            double centerLongitude,
            double centerLatitude,
            Func<double, double, float> sampleElevation,
            Func<double, double, bool> containsLand)
        {
            var battlefield = new TacticalBattlefieldState
            {
                BattlefieldId = $"TW-{parentHex.Q:D2}-{parentHex.R:D3}-250M",
                ParentHex = parentHex,
                Width = DefaultWidth,
                Height = DefaultHeight,
                CellSizeMetres = CellSizeMetres,
                CenterLongitude = centerLongitude,
                CenterLatitude = centerLatitude
            };

            // A flat-top hex grid advances sqrt(3)/2 of the adjacent-center
            // distance east/west and half a cell north/south on diagonal steps.
            double longitudeStep = CellSizeMetres * Math.Sqrt(3d) * .5d / 1000d / (111.32d * Math.Cos(centerLatitude * Math.PI / 180d));
            double latitudeStep = CellSizeMetres / 1000d / 110.574d;
            int centerQ = battlefield.Width / 2;
            int centerR = battlefield.Height / 2;
            battlefield.OriginLongitude = centerLongitude - centerQ * longitudeStep;
            battlefield.OriginLatitude = centerLatitude - (centerR + ((centerQ & 1) == 0 ? 0d : .5d)) * latitudeStep;
            battlefield.West = double.MaxValue;
            battlefield.East = double.MinValue;
            battlefield.South = double.MaxValue;
            battlefield.North = double.MinValue;

            for (int q = 0; q < battlefield.Width; q++)
            {
                for (int r = 0; r < battlefield.Height; r++)
                {
                    double longitude = battlefield.OriginLongitude + q * longitudeStep;
                    double latitude = battlefield.OriginLatitude + (r + ((q & 1) == 0 ? 0d : .5d)) * latitudeStep;
                    float elevation = sampleElevation(longitude, latitude);
                    bool land = containsLand(longitude, latitude);
                    var coord = new HexCoord(q, r);
                    battlefield.Cells.Add(new TacticalBattlefieldCell
                    {
                        Id = battlefield.BattlefieldId + ":" + coord,
                        LocalCoord = coord,
                        Longitude = longitude,
                        Latitude = latitude,
                        ElevationMetres = elevation,
                        Terrain = ClassifyTerrain(land, elevation)
                    });
                    battlefield.West = Math.Min(battlefield.West, longitude - longitudeStep * .5d);
                    battlefield.East = Math.Max(battlefield.East, longitude + longitudeStep * .5d);
                    battlefield.South = Math.Min(battlefield.South, latitude - latitudeStep * .5d);
                    battlefield.North = Math.Max(battlefield.North, latitude + latitudeStep * .5d);
                }
            }
            return battlefield;
        }

        private static TacticalTerrain ClassifyTerrain(bool isLand, float metres)
        {
            if (!isLand) return TacticalTerrain.Water;
            if (metres >= 550f) return TacticalTerrain.Highland;
            return metres >= 160f ? TacticalTerrain.Rough : TacticalTerrain.Open;
        }
    }
}
