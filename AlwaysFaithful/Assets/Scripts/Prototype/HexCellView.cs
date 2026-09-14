using AlwaysFaithful.Core;
using UnityEngine;

namespace AlwaysFaithful.Prototype
{
    public sealed class HexCellView : MonoBehaviour
    {
        private Material material;
        private Color baseColor;
        private bool hovered;
        private bool selected;
        private bool reachable;
        private bool path;

        public HexCoord Coord { get; private set; }
        public bool IsLand { get; private set; }
        public float ElevationMetres { get; private set; }
        public TacticalTerrain Terrain { get; private set; }

        public void Initialize(HexCoord coord, TacticalTerrain terrain, float elevationMetres, Material cellMaterial, Color color)
        {
            Coord = coord;
            Terrain = terrain;
            IsLand = terrain != TacticalTerrain.Water;
            ElevationMetres = elevationMetres;
            material = cellMaterial;
            baseColor = color;
            material.color = color;
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

        private void RefreshColor()
        {
            if (material == null) return;
            Color color = baseColor;
            if (reachable) color = Color.Lerp(color, new Color(.20f, .64f, .55f), .38f);
            if (path) color = Color.Lerp(color, new Color(1f, .69f, .16f), .78f);
            if (hovered) color = Color.Lerp(color, Color.white, .28f);
            if (selected) color = Color.Lerp(color, new Color(1f, .78f, .25f), .48f);
            material.color = color;
        }
    }
}
