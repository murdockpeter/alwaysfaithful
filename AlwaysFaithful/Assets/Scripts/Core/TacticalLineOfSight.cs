using System;
using System.Collections.Generic;

namespace AlwaysFaithful.Core
{
    public enum TacticalLosState
    {
        None,
        Clear,
        Obscured,
        Blocked
    }

    [Serializable]
    public sealed class TacticalLosSample
    {
        public HexCoord Coord;
        public TacticalLosState State;
        public float SightlineHeightMetres;
        public float ObstructionHeightMetres;
        public string Detail;
    }

    [Serializable]
    public sealed class TacticalLosResult
    {
        public bool IsValid;
        public string RejectionReason;
        public HexCoord Observer;
        public HexCoord Target;
        public int RangeHexes;
        public TacticalLosState State;
        public HexCoord BlockingCell;
        public float MinimumClearanceMetres = float.MaxValue;
        public List<TacticalLosSample> Samples = new List<TacticalLosSample>();
        public List<string> Modifiers = new List<string>();
    }

    public static class TacticalLineOfSight
    {
        public const int MaximumInspectionRangeHexes = 12;
        public const float ObserverHeightMetres = 2f;
        public const float TargetHeightMetres = 2f;

        public static TacticalLosResult Inspect(
            IReadOnlyDictionary<HexCoord, TacticalMovementCell> board,
            HexCoord observer,
            HexCoord target,
            int maximumRangeHexes = MaximumInspectionRangeHexes)
        {
            var result = new TacticalLosResult { Observer = observer, Target = target };
            if (!board.TryGetValue(observer, out TacticalMovementCell observerCell) ||
                !board.TryGetValue(target, out TacticalMovementCell targetCell))
                return Reject(result, "Outside mapped battlefield");

            result.RangeHexes = HexCoord.Distance(observer, target);
            if (result.RangeHexes > maximumRangeHexes) return Reject(result, $"Beyond {maximumRangeHexes}-hex LOS range");
            List<HexCoord> line = Trace(observer, target);
            foreach (HexCoord coord in line)
                if (!board.ContainsKey(coord)) return Reject(result, "LOS crosses map edge");

            result.IsValid = true;
            result.State = TacticalLosState.Clear;
            result.Modifiers.Add($"Range {result.RangeHexes} hex / {result.RangeHexes * 250} m");
            float observerEye = observerCell.ElevationMetres + ObserverHeightMetres;
            float targetEye = targetCell.ElevationMetres + TargetHeightMetres;
            bool blocked = false;
            bool obscured = false;
            for (int index = 0; index < line.Count; index++)
            {
                HexCoord coord = line[index];
                TacticalMovementCell cell = board[coord];
                float progress = line.Count <= 1 ? 0f : index / (float)(line.Count - 1);
                float sightline = observerEye + (targetEye - observerEye) * progress;
                float obstruction = cell.ElevationMetres;
                string detail = "Open sightline";
                TacticalLosState sampleState = obscured ? TacticalLosState.Obscured : TacticalLosState.Clear;

                if (index > 0 && index < line.Count - 1)
                {
                    if (cell.SmokeExpiresAfterTurn > 0)
                    {
                        obscured = true;
                        result.State = TacticalLosState.Obscured;
                        detail = "Smoke obscures sightline";
                        if (!result.Modifiers.Contains("Intervening smoke: obscured")) result.Modifiers.Add("Intervening smoke: obscured");
                    }
                    // Highland already contributes its measured terrain elevation.
                    // Rough adds an abstract vegetation/surface-obstruction height,
                    // and cover (vegetation clumps or built-up structures) adds its
                    // own obstruction on top, additive since both can occupy a cell.
                    float terrainHeight = cell.Terrain == TacticalTerrain.Rough ? 8f : 0f;
                    terrainHeight += CoverHeightMetres(cell.Cover);
                    obstruction += terrainHeight;
                    float clearance = sightline - obstruction;
                    result.MinimumClearanceMetres = Math.Min(result.MinimumClearanceMetres, clearance);
                    if (!blocked && clearance < 0f)
                    {
                        blocked = true;
                        result.BlockingCell = coord;
                        result.State = TacticalLosState.Blocked;
                        detail = $"Terrain crest blocks by {-clearance:0} m";
                        result.Modifiers.Add($"Blocked at {coord}: {-clearance:0} m above sightline");
                    }
                    else if (!blocked && (cell.Terrain == TacticalTerrain.Rough || cell.Cover != TacticalCover.None))
                    {
                        obscured = true;
                        result.State = TacticalLosState.Obscured;
                        detail = cell.Terrain == TacticalTerrain.Rough ? cell.Terrain + " intervening terrain" : cell.Cover + " cover intervening";
                        if (!result.Modifiers.Contains("Intervening terrain: obscured")) result.Modifiers.Add("Intervening terrain: obscured");
                    }
                }
                if (blocked) sampleState = TacticalLosState.Blocked;
                else if (obscured) sampleState = TacticalLosState.Obscured;
                result.Samples.Add(new TacticalLosSample
                {
                    Coord = coord,
                    State = sampleState,
                    SightlineHeightMetres = sightline,
                    ObstructionHeightMetres = obstruction,
                    Detail = detail
                });
            }
            if (result.MinimumClearanceMetres == float.MaxValue) result.MinimumClearanceMetres = 0f;
            if (result.State != TacticalLosState.Blocked && (observerCell.SmokeExpiresAfterTurn > 0 || targetCell.SmokeExpiresAfterTurn > 0))
            {
                result.State = TacticalLosState.Obscured;
                result.Modifiers.Add(observerCell.SmokeExpiresAfterTurn > 0 ? "Observer smoke: obscured" : "Target smoke: obscured");
            }
            if (targetCell.Terrain == TacticalTerrain.Rough || targetCell.Terrain == TacticalTerrain.Highland)
                result.Modifiers.Add("Target terrain: " + targetCell.Terrain);
            if (targetCell.Cover != TacticalCover.None)
                result.Modifiers.Add("Target cover: " + targetCell.Cover);
            return result;
        }

        // None/Light/Medium/Heavy obstruction contributed by cover, additive with
        // any terrain-based obstruction on the same intervening cell.
        public static float CoverHeightMetres(TacticalCover cover)
        {
            switch (cover)
            {
                case TacticalCover.Light: return 2f;
                case TacticalCover.Medium: return 5f;
                case TacticalCover.Heavy: return 9f;
                default: return 0f;
            }
        }

        public static List<HexCoord> Trace(HexCoord start, HexCoord end)
        {
            int distance = HexCoord.Distance(start, end);
            var line = new List<HexCoord>(distance + 1);
            ToCube(start, out float ax, out float ay, out float az);
            ToCube(end, out float bx, out float by, out float bz);
            for (int step = 0; step <= distance; step++)
            {
                float blend = distance == 0 ? 0f : step / (float)distance;
                line.Add(FromCubeRound(
                    ax + (bx - ax) * blend,
                    ay + (by - ay) * blend,
                    az + (bz - az) * blend));
            }
            return line;
        }

        private static TacticalLosResult Reject(TacticalLosResult result, string reason)
        {
            result.RejectionReason = reason;
            result.State = TacticalLosState.Blocked;
            return result;
        }

        private static void ToCube(HexCoord coord, out float x, out float y, out float z)
        {
            x = coord.Q;
            z = coord.R - (coord.Q - (coord.Q & 1)) / 2f;
            y = -x - z;
        }

        private static HexCoord FromCubeRound(float x, float y, float z)
        {
            int rx = (int)Math.Round(x);
            int ry = (int)Math.Round(y);
            int rz = (int)Math.Round(z);
            float dx = Math.Abs(rx - x);
            float dy = Math.Abs(ry - y);
            float dz = Math.Abs(rz - z);
            if (dx > dy && dx > dz) rx = -ry - rz;
            else if (dy > dz) ry = -rx - rz;
            else rz = -rx - ry;
            int row = rz + (rx - (rx & 1)) / 2;
            return new HexCoord(rx, row);
        }
    }
}
