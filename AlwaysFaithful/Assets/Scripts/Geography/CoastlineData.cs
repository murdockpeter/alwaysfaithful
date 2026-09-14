using System;
using System.Collections.Generic;
using UnityEngine;

namespace AlwaysFaithful.Geography
{
    [Serializable] public sealed class CoastlinePointData { public double longitude; public double latitude; }
    [Serializable] public sealed class CoastlinePolygonData { public List<CoastlinePointData> points = new List<CoastlinePointData>(); }
    [Serializable] public sealed class CoastlineMetadataData { public string title; public string source; public string sourceFile; public string license; }

    [Serializable]
    public sealed class CoastlineData
    {
        public CoastlineMetadataData metadata;
        public double west;
        public double east;
        public double south;
        public double north;
        public List<CoastlinePolygonData> polygons = new List<CoastlinePolygonData>();

        public static CoastlineData Load(TextAsset asset)
            => asset == null ? null : JsonUtility.FromJson<CoastlineData>(asset.text);

        public bool ContainsLand(double longitude, double latitude)
        {
            if (polygons == null) return false;
            foreach (CoastlinePolygonData polygon in polygons)
            {
                if (Contains(polygon.points, longitude, latitude)) return true;
            }
            return false;
        }

        private static bool Contains(IReadOnlyList<CoastlinePointData> points, double x, double y)
        {
            if (points == null || points.Count < 3) return false;
            bool inside = false;
            for (int current = 0, previous = points.Count - 1; current < points.Count; previous = current++)
            {
                CoastlinePointData a = points[current];
                CoastlinePointData b = points[previous];
                bool crosses = (a.latitude > y) != (b.latitude > y) &&
                    x < (b.longitude - a.longitude) * (y - a.latitude) / (b.latitude - a.latitude) + a.longitude;
                if (crosses) inside = !inside;
            }
            return inside;
        }
    }
}
