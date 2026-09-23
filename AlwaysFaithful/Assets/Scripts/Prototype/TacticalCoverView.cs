using AlwaysFaithful.Core;
using UnityEngine;

namespace AlwaysFaithful.Prototype
{
    // Procedural cover/built-up/terrain dressing for the 250 m tactical map
    // only (never the whole-island operational board). Follows the same
    // restrained, primitive-composed, Ignore-Raycast-layer idiom as
    // TacticalFormationView and ContactMarkerView: flat-colored primitives,
    // no imported assets.
    public static class TacticalCoverView
    {
        private const float SurfaceOffset = .10f;

        private static readonly Color BushColor = new Color(.30f, .40f, .21f);
        private static readonly Color TrunkColor = new Color(.32f, .23f, .16f);
        private static readonly Color MediumCanopyColor = new Color(.22f, .33f, .18f);
        private static readonly Color HeavyCanopyColor = new Color(.16f, .26f, .14f);
        private static readonly Color HighlandRockColor = new Color(.50f, .48f, .45f);
        private static readonly Color RoughRockColor = new Color(.42f, .39f, .35f);
        private static readonly Color PondColor = new Color(.15f, .42f, .43f);
        private static readonly Color ReedColor = new Color(.34f, .38f, .20f);

        // A small body of water is purely cosmetic set dressing, not a new
        // Terrain value -- it never changes movement cost, LOS, or the
        // saved battlefield's terrain data, only what a land hex looks like.
        private const int PondChancePerMille = 120;

        public static void Build(Transform cellTransform, HexCoord coord, TacticalTerrain terrain, TacticalCover cover, bool isBuiltUp, Shader shader, GraphicsPresetTier preset)
        {
            if (terrain == TacticalTerrain.Water) return;
            if (isBuiltUp)
            {
                if (cover != TacticalCover.None) BuildBuiltUp(cellTransform, coord, cover, shader, preset);
                return;
            }
            if (cover != TacticalCover.None) BuildNatural(cellTransform, coord, cover, shader, preset);
            // Rocky ground reads by terrain class, independent of vegetation
            // cover -- previously Rough/Highland were color-only, so a bare
            // highland hex with no cover looked identical to bare lowland.
            if (terrain == TacticalTerrain.Rough || terrain == TacticalTerrain.Highland)
                BuildRocks(cellTransform, coord, terrain, shader, preset);
            if (terrain != TacticalTerrain.Highland && ShouldHavePond(coord))
                BuildPond(cellTransform, coord, shader);
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

        // Light cover reads as low bushes; Medium/Heavy read as actual
        // trees (trunk + a jittered cluster of canopy lobes) rather than
        // the flat single-cylinder "clump" this used to be.
        private static void BuildNatural(Transform cellTransform, HexCoord coord, TacticalCover cover, Shader shader, GraphicsPresetTier preset)
        {
            int count = ScaledPropCount(cover == TacticalCover.Light ? 1 : cover == TacticalCover.Medium ? 2 : 3, preset);
            for (int index = 0; index < count; index++)
            {
                if (cover == TacticalCover.Light) BuildBush(cellTransform, coord, index, shader);
                else BuildTree(cellTransform, coord, index, cover == TacticalCover.Heavy, shader);
            }
        }

        private static void BuildBush(Transform cellTransform, HexCoord coord, int index, Shader shader)
        {
            Vector2 offset = JitterOffset(coord, index);
            float radius = .15f + JitterUnit(coord, index) * .08f;
            GameObject bush = Primitive(PrimitiveType.Sphere, $"Bush {index}", cellTransform, shader, BushColor);
            bush.transform.localPosition = new Vector3(offset.x, SurfaceOffset + radius * .70f, offset.y);
            bush.transform.localScale = new Vector3(radius * 2.1f, radius * 1.3f, radius * 2.1f);
        }

        private static void BuildTree(Transform cellTransform, HexCoord coord, int index, bool heavy, Shader shader)
        {
            Vector2 offset = JitterOffset(coord, index);
            float trunkHeight = .22f + JitterUnit(coord, index) * .13f;
            GameObject trunk = Primitive(PrimitiveType.Cylinder, $"Trunk {index}", cellTransform, shader, TrunkColor);
            trunk.transform.localPosition = new Vector3(offset.x, SurfaceOffset + trunkHeight * .5f, offset.y);
            trunk.transform.localScale = new Vector3(.040f, trunkHeight, .040f);

            Color canopyColor = heavy ? HeavyCanopyColor : MediumCanopyColor;
            int lobes = heavy ? 3 : 2;
            for (int lobe = 0; lobe < lobes; lobe++)
            {
                int salt = index * 7 + lobe + 40;
                float lobeRadius = (.15f + JitterUnit(coord, salt) * .07f) * (heavy ? 1.15f : 1f);
                Vector2 lobeSpread = JitterOffset(coord, salt) * .22f;
                GameObject canopy = Primitive(PrimitiveType.Sphere, $"Canopy {index}-{lobe}", cellTransform, shader, canopyColor);
                canopy.transform.localPosition = new Vector3(offset.x + lobeSpread.x, SurfaceOffset + trunkHeight + lobeRadius * .55f, offset.y + lobeSpread.y);
                canopy.transform.localScale = Vector3.one * lobeRadius * 2f;
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
                building.transform.localScale = new Vector3(.29f, height * 1.25f, .24f);

                GameObject roof = Primitive(PrimitiveType.Cube, $"Roof {index}", cellTransform, shader, roofColor);
                roof.transform.localPosition = new Vector3(offset.x, SurfaceOffset + height * 1.25f + .015f, offset.y);
                roof.transform.localScale = new Vector3(.32f, .035f, .27f);
            }
        }

        // Boulder clutter keyed off Terrain rather than Cover, so a bare
        // Rough or Highland hex still reads as rocky ground even with no
        // vegetation rolled on it. Irregular jittered scale/rotation per
        // rock, not a uniform cube, for a less obviously primitive look.
        private static void BuildRocks(Transform cellTransform, HexCoord coord, TacticalTerrain terrain, Shader shader, GraphicsPresetTier preset)
        {
            int baseCount = terrain == TacticalTerrain.Highland ? 2 : 1;
            int count = ScaledPropCount(baseCount, preset);
            Color color = terrain == TacticalTerrain.Highland ? HighlandRockColor : RoughRockColor;
            for (int index = 0; index < count; index++)
            {
                int salt = index + 31;
                Vector2 offset = JitterOffset(coord, salt);
                float size = .12f + JitterUnit(coord, salt) * .09f;
                GameObject rock = Primitive(PrimitiveType.Cube, $"Rock {index}", cellTransform, shader, color);
                rock.transform.localPosition = new Vector3(offset.x, SurfaceOffset + size * .32f, offset.y);
                float yaw = Hash(coord, salt + 200) % 360u;
                float tiltX = (JitterUnit(coord, salt + 3) - .5f) * 22f;
                float tiltZ = (JitterUnit(coord, salt + 5) - .5f) * 18f;
                rock.transform.localRotation = Quaternion.Euler(tiltX, yaw, tiltZ);
                rock.transform.localScale = new Vector3(
                    size * (.85f + JitterUnit(coord, salt + 7) * .5f),
                    size * (.55f + JitterUnit(coord, salt + 9) * .35f),
                    size * (.85f + JitterUnit(coord, salt + 13) * .5f));
            }
        }

        private static bool ShouldHavePond(HexCoord coord) => Hash(coord, 91) % 1000u < PondChancePerMille;

        // A shallow flat disc plus a few edge reeds -- deliberately tiny
        // next to the real coastline water tiles, so it never reads as a
        // miscolored ordinary hex, only as a small pond within one.
        private static void BuildPond(Transform cellTransform, HexCoord coord, Shader shader)
        {
            Vector2 offset = JitterOffset(coord, 73) * .55f;
            float radius = .17f + JitterUnit(coord, 73) * .09f;
            GameObject pond = Primitive(PrimitiveType.Cylinder, "Pond", cellTransform, shader, PondColor);
            pond.transform.localPosition = new Vector3(offset.x, SurfaceOffset + .008f, offset.y);
            pond.transform.localScale = new Vector3(radius, .008f, radius);

            for (int reed = 0; reed < 3; reed++)
            {
                int salt = 79 + reed;
                float angle = (Hash(coord, salt) % 360u) * Mathf.Deg2Rad;
                Vector2 reedOffset = offset + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius * .92f;
                float reedHeight = .07f + JitterUnit(coord, salt) * .05f;
                GameObject reedProp = Primitive(PrimitiveType.Cylinder, $"Reed {reed}", cellTransform, shader, ReedColor);
                reedProp.transform.localPosition = new Vector3(reedOffset.x, SurfaceOffset + reedHeight * .5f, reedOffset.y);
                reedProp.transform.localScale = new Vector3(.012f, reedHeight, .012f);
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
