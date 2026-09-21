using AlwaysFaithful.Core;
using UnityEngine;

namespace AlwaysFaithful.Prototype
{
    // Alternate NATO/MIL-STD-2525-inspired presentation skin over the exact
    // same underlying unit, switchable against TacticalFormationView via
    // Settings.CounterSkin: a flat frame (blue friendly / red hostile, the
    // doctrinal convention, independent of this game's own illustrated
    // green-USMC/red-PLA palette) with an infantry-cross or support-dot
    // icon. Not full MIL-STD-2525 fidelity (no size/echelon glyph, no
    // mobility/equipment modifiers) — a simplified stand-in for players who
    // prefer the standardized look over the illustrated counter.
    public sealed class NatoSymbolView : MonoBehaviour
    {
        private LineRenderer frame;
        private LineRenderer crossA;
        private LineRenderer crossB;
        private MeshRenderer supportDotRenderer;
        private TextMesh label;
        private GameObject statusBadge;
        private MeshRenderer statusBadgeRenderer;
        private bool isSupportRole;
        private TacticalUnitState boundUnit;
        private Color boundFrameColor;
        private Color boundIconColor;
        private Color flashColor;
        private float flashTimer;

        public TacticalCombatStatus PresentedStatus { get; private set; }

        public void Initialize(bool friendly, bool isSupportRoleUnit, string labelText)
        {
            isSupportRole = isSupportRoleUnit;
            Shader overlay = Resources.Load<Shader>("Shaders/MapOverlay") ?? Shader.Find("Sprites/Default");
            Shader solid = Resources.Load<Shader>("Shaders/MapSolid") ?? overlay;
            Color frameColor = friendly ? new Color(.22f, .55f, .95f) : new Color(.92f, .20f, .16f);

            frame = new GameObject("NATO Frame").AddComponent<LineRenderer>();
            frame.transform.SetParent(transform, false);
            frame.loop = true;
            frame.useWorldSpace = false;
            frame.positionCount = 4;
            frame.widthMultiplier = .055f;
            frame.material = new Material(overlay);
            frame.startColor = frameColor;
            frame.endColor = frameColor;
            Vector3[] corners =
            {
                new Vector3(-.52f, .22f, -.36f),
                new Vector3(-.52f, .22f, .36f),
                new Vector3(.52f, .22f, .36f),
                new Vector3(.52f, .22f, -.36f)
            };
            for (int index = 0; index < corners.Length; index++) frame.SetPosition(index, corners[index]);

            if (isSupportRole)
            {
                GameObject dot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                dot.name = "NATO Support Dot";
                dot.layer = LayerMask.NameToLayer("Ignore Raycast");
                dot.transform.SetParent(transform, false);
                dot.transform.localPosition = new Vector3(0f, .23f, 0f);
                dot.transform.localScale = new Vector3(.30f, .03f, .30f);
                dot.GetComponent<MeshRenderer>().sharedMaterial = new Material(solid) { color = frameColor };
                Destroy(dot.GetComponent<Collider>());
                supportDotRenderer = dot.GetComponent<MeshRenderer>();
            }
            else
            {
                // The standard MIL-STD-2525 infantry glyph: a diagonal cross
                // filling the frame.
                crossA = BuildCrossLine("NATO Cross A", overlay, frameColor,
                    new Vector3(-.42f, .225f, -.28f), new Vector3(.42f, .225f, .28f));
                crossB = BuildCrossLine("NATO Cross B", overlay, frameColor,
                    new Vector3(-.42f, .225f, .28f), new Vector3(.42f, .225f, -.28f));
            }

            GameObject labelObject = new GameObject("NATO Label");
            labelObject.transform.SetParent(transform, false);
            labelObject.transform.localPosition = new Vector3(0f, .225f, -.52f);
            labelObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            label = labelObject.AddComponent<TextMesh>();
            label.text = labelText;
            label.alignment = TextAlignment.Center;
            label.anchor = TextAnchor.MiddleCenter;
            label.fontSize = 44;
            label.characterSize = .055f;
            label.color = new Color(.92f, .92f, .90f);

            statusBadge = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            statusBadge.name = "NATO Status Badge";
            statusBadge.layer = LayerMask.NameToLayer("Ignore Raycast");
            statusBadge.transform.SetParent(transform, false);
            statusBadge.transform.localPosition = new Vector3(0f, .45f, .12f);
            statusBadge.transform.localScale = Vector3.one * .16f;
            statusBadgeRenderer = statusBadge.GetComponent<MeshRenderer>();
            statusBadgeRenderer.sharedMaterial = new Material(solid);
            Destroy(statusBadge.GetComponent<Collider>());
            statusBadge.SetActive(false);
        }

        private LineRenderer BuildCrossLine(string objectName, Shader shader, Color color, Vector3 from, Vector3 to)
        {
            LineRenderer line = new GameObject(objectName).AddComponent<LineRenderer>();
            line.transform.SetParent(transform, false);
            line.useWorldSpace = false;
            line.positionCount = 2;
            line.widthMultiplier = .05f;
            line.material = new Material(shader);
            line.startColor = color;
            line.endColor = color;
            line.SetPosition(0, from);
            line.SetPosition(1, to);
            return line;
        }

        // Mirrors ContactMarkerView/TacticalFormationView's own direct-poke
        // color pattern rather than a shared interface, so this skin can be
        // driven from the exact same call sites without a bigger refactor.
        public void SetColors(Color frameColor, Color iconColor)
        {
            if (frame == null) return;
            frame.startColor = frameColor;
            frame.endColor = frameColor;
            if (isSupportRole)
            {
                if (supportDotRenderer != null) supportDotRenderer.material.color = iconColor;
            }
            else
            {
                crossA.startColor = iconColor;
                crossA.endColor = iconColor;
                crossB.startColor = iconColor;
                crossB.endColor = iconColor;
            }
        }

        // Momentary override (fire-hit flash etc.); Update() re-asserts it
        // for a short window so a same-frame reactive recompute (see
        // BindReactive) can't immediately stomp it, then lets the reactive
        // color take back over once it expires.
        public void FlashIcon(Color color)
        {
            flashColor = color;
            flashTimer = .35f;
            ApplyIconColor(color);
        }

        private void ApplyIconColor(Color color)
        {
            if (isSupportRole)
            {
                if (supportDotRenderer != null) supportDotRenderer.material.color = color;
            }
            else if (crossA != null)
            {
                crossA.startColor = color;
                crossA.endColor = color;
                crossB.startColor = color;
                crossB.endColor = color;
            }
        }

        public void UpdateStatusBadge(bool visible, TacticalCombatStatus status)
        {
            PresentedStatus = status;
            if (statusBadge == null) return;
            statusBadge.SetActive(visible);
            if (visible) statusBadgeRenderer.material.color = TacticalStatusVisuals.BadgeColor(status);
        }

        // Self-driving alternative to the explicit Present()/Cue* push
        // pattern used elsewhere: binds once to the live TacticalUnitState
        // (mutated in place for the rest of the battle, never reassigned)
        // and Update() keeps this skin in sync every frame on its own, so
        // the player's counter doesn't need this view wired into its ~20
        // scattered Present() call sites. Only used for the player's own
        // unit; PLA contacts go through the explicit ContactMarkerView path
        // instead, since their color depends on fog-of-war contact tier,
        // not just unit state.
        public void BindReactive(TacticalUnitState unit, Color frameColor, Color iconColor)
        {
            boundUnit = unit;
            boundFrameColor = frameColor;
            boundIconColor = iconColor;
        }

        private void Update()
        {
            if (flashTimer > 0f)
            {
                flashTimer -= Time.unscaledDeltaTime;
                ApplyIconColor(flashColor);
            }
            else if (boundUnit != null)
            {
                float readiness = boundUnit.Readiness == UnitReadiness.Spent ? .42f : boundUnit.Readiness == UnitReadiness.Moving ? .82f : 1f;
                float desaturation = TacticalStatusVisuals.DesaturationFor(boundUnit.CombatStatus);
                Color effectiveFrame = TacticalStatusVisuals.Desaturate(boundFrameColor, desaturation);
                Color effectiveIcon = TacticalStatusVisuals.Desaturate(boundIconColor, desaturation);
                Color dim = new Color(.12f, .12f, .12f);
                SetColors(Color.Lerp(dim, effectiveFrame, readiness), Color.Lerp(dim, effectiveIcon, readiness));
                UpdateStatusBadge(boundUnit.CombatStatus != TacticalCombatStatus.Ready, boundUnit.CombatStatus);
            }
            if (statusBadge != null && statusBadge.activeSelf)
                statusBadge.transform.localScale = Vector3.one * TacticalStatusVisuals.PulseScale(PresentedStatus, .16f);
        }
    }
}
