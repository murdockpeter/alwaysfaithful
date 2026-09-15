using AlwaysFaithful.Core;
using UnityEngine;

namespace AlwaysFaithful.Prototype
{
    public sealed class HexCellView : MonoBehaviour
    {
        private MeshRenderer meshRenderer;
        private MaterialPropertyBlock properties;
        private Color baseColor;
        private bool hovered;
        private bool selected;
        private bool reachable;
        private bool path;
        private bool invalid;
        private TacticalLosState lineOfSight;

        public HexCoord Coord { get; private set; }
        public bool IsLand { get; private set; }
        public float ElevationMetres { get; private set; }
        public double Longitude { get; private set; }
        public double Latitude { get; private set; }
        public TacticalTerrain Terrain { get; private set; }

        public void Initialize(HexCoord coord, TacticalTerrain terrain, float elevationMetres, double longitude, double latitude, MeshRenderer renderer, Color color)
        {
            Coord = coord;
            Terrain = terrain;
            IsLand = terrain != TacticalTerrain.Water;
            ElevationMetres = elevationMetres;
            Longitude = longitude;
            Latitude = latitude;
            meshRenderer = renderer;
            properties = new MaterialPropertyBlock();
            baseColor = color;
            RefreshColor();
        }

        public void SetHighlighted(bool highlighted)
        {
            hovered = highlighted;
            RefreshColor();
        }

        public void SetSelected(bool value)
        {
            selected = value;
            RefreshColor();
        }

        public void SetReachable(bool value)
        {
            reachable = value;
            RefreshColor();
        }

        public void SetPath(bool value)
        {
            path = value;
            RefreshColor();
        }

        public void SetInvalid(bool value)
        {
            invalid = value;
            RefreshColor();
        }

        public void SetLineOfSight(TacticalLosState value)
        {
            lineOfSight = value;
            RefreshColor();
        }

        private void RefreshColor()
        {
            if (meshRenderer == null) return;
            Color color = baseColor;
            if (reachable) color = Color.Lerp(color, new Color(.20f, .64f, .55f), .38f);
            if (path) color = Color.Lerp(color, new Color(1f, .69f, .16f), .78f);
            if (hovered) color = Color.Lerp(color, Color.white, .28f);
            if (selected) color = Color.Lerp(color, new Color(1f, .78f, .25f), .48f);
            if (invalid) color = Color.Lerp(color, new Color(.92f, .18f, .12f), .68f);
            if (lineOfSight == TacticalLosState.Clear) color = Color.Lerp(color, new Color(.20f, .78f, .70f), .42f);
            else if (lineOfSight == TacticalLosState.Obscured) color = Color.Lerp(color, new Color(.95f, .66f, .18f), .52f);
            else if (lineOfSight == TacticalLosState.Blocked) color = Color.Lerp(color, new Color(.88f, .16f, .13f), .62f);
            properties.SetColor("_Color", color);
            meshRenderer.SetPropertyBlock(properties);
        }
    }
}
