using AlwaysFaithful.Core;
using UnityEngine;

namespace AlwaysFaithful.Prototype
{
    // Procedural cover/built-up dressing for the 250 m tactical map only (never
    // the whole-island operational board). Follows the same restrained,
    // primitive-composed, Ignore-Raycast-layer idiom as TacticalFormationView
    // and ContactMarkerView: flat-colored primitives, no imported assets.
    public static class TacticalCoverView
    {
        private const float SurfaceOffset = .10f;

        public static void Build(Transform cellTransform, HexCoord coord, TacticalCover cover, bool isBuiltUp, Shader shader, GraphicsPresetTier preset)
        {
            if (cover == TacticalCover.None) return;
            if (isBuiltUp) BuildBuiltUp(cellTransform, coord, cover, shader, preset);
            else BuildNatural(cellTransform, coord, cover, shader, preset);
        }

        // Low trims a prop off every cell (floor of 1, so cover is never
        // invisible); High adds one for denser clutter. Medium is the
        // original, unscaled density.
        private static int ScaledPropCount(int baseCount, GraphicsPresetTier preset)
        {
            switch (preset)
            {
                case GraphicsPresetTier.Low: return Mathf.Max(1, baseCount - 1);
                case GraphicsPresetTier.High: return baseCount + 1;
                default: return baseCount;
            }
        }

        private static void BuildNatural(Transform cellTransform, HexCoord coord, TacticalCover cover, Shader shader, GraphicsPresetTier preset)
        {
            int count = ScaledPropCount(cover == TacticalCover.Light ? 1 : cover == TacticalCover.Medium ? 2 : 3, preset);
            Color color = cover == TacticalCover.Light
                ? new Color(.32f, .42f, .23f)
                : cover == TacticalCover.Medium
                    ? new Color(.25f, .35f, .19f)
                    : new Color(.17f, .27f, .15f);
            float clumpHeight = cover == TacticalCover.Light ? .05f : cover == TacticalCover.Medium ? .09f : .14f;
            for (int index = 0; index < count; index++)
            {
                Vector2 offset = JitterOffset(coord, index);
                float radius = .14f + JitterUnit(coord, index) * .09f;
                GameObject clump = Primitive(PrimitiveType.Cylinder, $"Cover Clump {index}", cellTransform, shader, color);
                clump.transform.localPosition = new Vector3(offset.x, SurfaceOffset + clumpHeight * .5f, offset.y);
                clump.transform.localScale = new Vector3(radius, clumpHeight, radius);
            }
        }

        private static void BuildBuiltUp(Transform cellTransform, HexCoord coord, TacticalCover cover, Shader shader, GraphicsPresetTier preset)
        {
            int count = ScaledPropCount(cover == TacticalCover.Heavy ? 3 : 2, preset);
            Color wallColor = new Color(.53f, .50f, .43f);
            Color roofColor = new Color(.37f, .28f, .22f);
            for (int index = 0; index < count; index++)
            {
                Vector2 offset = JitterOffset(coord, index + 11);
                float height = .16f + JitterUnit(coord, index + 11) * .10f;
                GameObject building = Primitive(PrimitiveType.Cube, $"Building {index}", cellTransform, shader, wallColor);
                building.transform.localPosition = new Vector3(offset.x, SurfaceOffset + height * .5f, offset.y);
                building.transform.localScale = new Vector3(.20f, height, .17f);

                GameObject roof = Primitive(PrimitiveType.Cube, $"Roof {index}", cellTransform, shader, roofColor);
                roof.transform.localPosition = new Vector3(offset.x, SurfaceOffset + height + .015f, offset.y);
                roof.transform.localScale = new Vector3(.22f, .03f, .19f);
            }
        }

        private static Vector2 JitterOffset(HexCoord coord, int salt)
        {
            uint hash = Hash(coord, salt);
            float angle = (hash % 360u) * Mathf.Deg2Rad;
            float radius = .12f + (hash / 360u % 100u) / 100f * .26f;
            return new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
        }

        private static float JitterUnit(HexCoord coord, int salt) => Hash(coord, salt + 97) % 1000u / 1000f;

        // Same spatial-hash-prime + xorshift idiom used for gameplay-deterministic
        // rolls elsewhere (TacticalEnemyTurn.TieBreak, TacticalBattlefieldExtractor);
        // reused here purely for reproducible, non-gameplay prop placement.
        private static uint Hash(HexCoord coord, int salt)
        {
            unchecked
            {
                uint value = (uint)salt ^ ((uint)coord.Q * 73856093u) ^ ((uint)coord.R * 19349663u);
                value ^= value << 13;
                value ^= value >> 17;
                value ^= value << 5;
                return value;
            }
        }

        private static GameObject Primitive(PrimitiveType type, string objectName, Transform parent, Shader shader, Color color)
        {
            GameObject result = GameObject.CreatePrimitive(type);
            result.name = objectName;
            result.layer = LayerMask.NameToLayer("Ignore Raycast");
            result.transform.SetParent(parent, false);
            result.GetComponent<MeshRenderer>().sharedMaterial = new Material(shader) { color = color };
            Object.Destroy(result.GetComponent<Collider>());
            return result;
        }
    }
}
