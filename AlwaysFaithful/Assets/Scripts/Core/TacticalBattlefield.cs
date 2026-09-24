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
        public TacticalCover Cover;
        public bool IsBuiltUp;
    }

    [Serializable]
    public sealed class TacticalBattlefieldState
    {
        public const int CurrentSchemaVersion = 14;

        public int SchemaVersion = CurrentSchemaVersion;
        public string BattlefieldId;
        public int Seed;
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
        public List<TacticalSuppressionEvent> SuppressionEvents = new List<TacticalSuppressionEvent>();
        public List<TacticalReactionEvent> ReactionEvents = new List<TacticalReactionEvent>();
        public List<TacticalEnemyActionEvent> EnemyActionEvents = new List<TacticalEnemyActionEvent>();
        public List<TacticalReconEvent> ReconEvents = new List<TacticalReconEvent>();
        public List<TacticalObjectiveEvent> ObjectiveEvents = new List<TacticalObjectiveEvent>();
        public List<TacticalReconMarker> ActiveReconMarkers = new List<TacticalReconMarker>();
        public TacticalObjectiveState Objective;

        // Schema 12: set for the whole battle when the player commits an ISR
        // support card before this scenario begins (AlwaysFaithfulPrototype's
        // pre-battle support-card modal). Read by RefreshTacticalObservation
        // alongside the existing recon-marker bonus.
        public bool IsrCardActive;

        // Schema 13: a played-support-card log (mirrors ReconEvents' role for
        // TacticalReconMarker) and the PLA-side counterpart of ActiveReconMarkers
        // — the enemy's own active sensor-tasking markers on the platoon.
        public List<TacticalSupportCardEvent> SupportCardEvents = new List<TacticalSupportCardEvent>();
        public List<TacticalReconMarker> ActivePlaReconMarkers = new List<TacticalReconMarker>();

        // Schema 14: platoon fire-and-maneuver effects and their replayable log.
        public List<TacticalSmokeMarker> ActiveSmokeMarkers = new List<TacticalSmokeMarker>();
        public List<TacticalSmokeEvent> SmokeEvents = new List<TacticalSmokeEvent>();
        public List<TacticalAreaSuppressionEvent> AreaSuppressionEvents = new List<TacticalAreaSuppressionEvent>();
        public List<TacticalAssaultEvent> AssaultEvents = new List<TacticalAssaultEvent>();

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

        // seed == 0 reproduces today's exact unseeded string, so every existing
        // caller and regression fixture is unaffected; a nonzero seed (from an
        // imported BattleRequest) both labels the battlefield distinctly and,
        // since cover/posture/objective generation all hash BattlefieldId
        // itself, deterministically reseeds the whole battle in one change.
        public static string BuildBattlefieldId(HexCoord parentHex, int seed = 0)
            => seed == 0
                ? $"TW-{parentHex.Q:D2}-{parentHex.R:D3}-250M"
                : $"TW-{parentHex.Q:D2}-{parentHex.R:D3}-250M-S{seed}";

        public static TacticalBattlefieldState Extract(
            HexCoord parentHex,
            double centerLongitude,
            double centerLatitude,
            Func<double, double, float> sampleElevation,
            Func<double, double, bool> containsLand,
            int seed = 0)
        {
            var battlefield = new TacticalBattlefieldState
            {
                BattlefieldId = BuildBattlefieldId(parentHex, seed),
                Seed = seed,
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
            GenerateCoverAndBuiltUp(battlefield);
            return battlefield;
        }

        private static TacticalTerrain ClassifyTerrain(bool isLand, float metres)
        {
            if (!isLand) return TacticalTerrain.Water;
            if (metres >= 550f) return TacticalTerrain.Highland;
            return metres >= 160f ? TacticalTerrain.Rough : TacticalTerrain.Open;
        }

        // Cover density and built-up clusters are derived deterministically from
        // BattlefieldId (via TacticalDirectFire.CreateSeed) plus each cell's own
        // local coordinate, so re-extracting the same operational hex always
        // reproduces an identical layout.
        public const int BuiltUpShoreBandHexes = 3;
        private const int ClusterSeedChancePerMille = 100;
        private const int ClusterRadiusSmallWeightPerMille = 700;
        private const int BuiltUpMediumWeightPerMille = 450;

        private static void GenerateCoverAndBuiltUp(TacticalBattlefieldState battlefield)
        {
            var byCoord = new Dictionary<HexCoord, TacticalBattlefieldCell>();
            var waterCoords = new List<HexCoord>();
            foreach (TacticalBattlefieldCell cell in battlefield.Cells)
            {
                byCoord[cell.LocalCoord] = cell;
                if (cell.Terrain == TacticalTerrain.Water) waterCoords.Add(cell.LocalCoord);
            }

            int coverSeed = TacticalDirectFire.CreateSeed(battlefield.BattlefieldId, 0, 1);
            int clusterSeedSeed = TacticalDirectFire.CreateSeed(battlefield.BattlefieldId, 0, 2);
            int clusterRadiusSeed = TacticalDirectFire.CreateSeed(battlefield.BattlefieldId, 0, 3);
            int builtUpTierSeed = TacticalDirectFire.CreateSeed(battlefield.BattlefieldId, 0, 4);

            foreach (TacticalBattlefieldCell cell in battlefield.Cells)
            {
                if (cell.Terrain == TacticalTerrain.Water) continue;
                cell.Cover = RollCoverLevel(cell.Terrain, CoverRoll(coverSeed, cell.LocalCoord));
            }

            foreach (TacticalBattlefieldCell center in battlefield.Cells)
            {
                if (center.IsBuiltUp || center.Terrain == TacticalTerrain.Water || center.Terrain == TacticalTerrain.Highland) continue;
                if (ShoreDistanceHexes(center.LocalCoord, waterCoords) > BuiltUpShoreBandHexes) continue;
                if (CoverRoll(clusterSeedSeed, center.LocalCoord) >= ClusterSeedChancePerMille) continue;

                int radius = CoverRoll(clusterRadiusSeed, center.LocalCoord) < ClusterRadiusSmallWeightPerMille ? 1 : 2;
                foreach (TacticalBattlefieldCell candidate in battlefield.Cells)
                {
                    if (candidate.IsBuiltUp || candidate.Terrain == TacticalTerrain.Water || candidate.Terrain == TacticalTerrain.Highland) continue;
                    if (HexCoord.Distance(center.LocalCoord, candidate.LocalCoord) > radius) continue;
                    // A cluster's growth radius can reach farther from shore than
                    // its seed cell alone, so every member is re-checked against
                    // the same shore band rather than trusting the center's check.
                    if (ShoreDistanceHexes(candidate.LocalCoord, waterCoords) > BuiltUpShoreBandHexes) continue;
                    candidate.IsBuiltUp = true;
                    candidate.Cover = CoverRoll(builtUpTierSeed, candidate.LocalCoord) < BuiltUpMediumWeightPerMille
                        ? TacticalCover.Medium
                        : TacticalCover.Heavy;
                }
            }
        }

        private static int ShoreDistanceHexes(HexCoord coord, List<HexCoord> waterCoords)
        {
            int nearest = int.MaxValue;
            foreach (HexCoord water in waterCoords)
                nearest = Math.Min(nearest, HexCoord.Distance(coord, water));
            return nearest;
        }

        // Weighted per-mille tables: rougher terrain skews toward heavier natural
        // cover, matching the same vegetation-density reasoning already used for
        // Rough terrain's LOS obstruction.
        private static TacticalCover RollCoverLevel(TacticalTerrain terrain, int roll)
        {
            switch (terrain)
            {
                case TacticalTerrain.Rough:
                    if (roll < 200) return TacticalCover.None;
                    if (roll < 550) return TacticalCover.Light;
                    if (roll < 870) return TacticalCover.Medium;
                    return TacticalCover.Heavy;
                case TacticalTerrain.Highland:
                    if (roll < 450) return TacticalCover.None;
                    if (roll < 780) return TacticalCover.Light;
                    if (roll < 950) return TacticalCover.Medium;
                    return TacticalCover.Heavy;
                default:
                    if (roll < 550) return TacticalCover.None;
                    if (roll < 850) return TacticalCover.Light;
                    if (roll < 970) return TacticalCover.Medium;
                    return TacticalCover.Heavy;
            }
        }

        // Mirrors TacticalEnemyTurn.TieBreak's spatial-hash-prime + xorshift idiom,
        // widened to a 0-999 per-mille roll instead of a %7 tie-break jitter.
        private static int CoverRoll(int seed, HexCoord coord)
        {
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
