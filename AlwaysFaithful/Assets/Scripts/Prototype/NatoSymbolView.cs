using AlwaysFaithful.Core;
using UnityEngine;

namespace AlwaysFaithful.Prototype
{
    // Alternate NATO/MIL-STD-2525-inspired presentation skin over the exact
    // same underlying unit, switchable against TacticalFormationView via
    // Settings.CounterSkin: a frame (blue friendly / red hostile, the
    // doctrinal convention, independent of this game's own illustrated
    // green-USMC/red-PLA palette) with an infantry-cross or support-dot
    // icon. Not full MIL-STD-2525 fidelity (no size/echelon glyph, no
    // mobility/equipment modifiers) — a simplified stand-in for players who
    // prefer the standardized look over the illustrated counter.
    //
    // The card itself is a fixed-tilt "billboard", not a decal lying flat on
    // the hex: real NATO symbology is read face-on, like a paper counter, so
    // the whole frame/icon/label/badge group sits on a tilted billboardRoot
    // child rather than directly on this component's own transform. A
    // separate small ground-anchor dot stays flat on the hex so it's still
    // obvious which hex the unit occupies once the card above it is tilted.
    public sealed class NatoSymbolView : MonoBehaviour
    {
        private LineRenderer frame;
        private LineRenderer crossA;
        private LineRenderer crossB;
        private MeshRenderer supportDotRenderer;
        private TextMesh label;
        private GameObject statusBadge;
        private MeshRenderer statusBadgeRenderer;
        private MeshRenderer groundAnchorRenderer;
        private bool isSupportRole;
        private TacticalUnitState boundUnit;
        private Color boundFrameColor;
        private Color boundIconColor;
        private Color flashColor;
        private float flashTimer;

        public TacticalCombatStatus PresentedStatus { get; private set; }

        // The tactical camera's fixed viewing offset (see
        // AlwaysFaithfulPrototype.ApplyCamera: cameraFocus + (0, distance *
        // 1.08, -distance * .56), always LookAt(cameraFocus)) never orbits —
        // only pans and zooms — so a single static tilt already faces the
        // camera everywhere on the board; no per-frame billboard math needed.
        private static readonly Quaternion CameraFacingTilt =
            Quaternion.FromToRotation(Vector3.up, new Vector3(0f, 1.08f, -.56f).normalized);

        public void Initialize(bool friendly, bool isSupportRoleUnit, string labelText)
        {
            isSupportRole = isSupportRoleUnit;
            Shader overlay = Resources.Load<Shader>("Shaders/MapOverlay") ?? Shader.Find("Sprites/Default");
            Shader solid = Resources.Load<Shader>("Shaders/MapSolid") ?? overlay;
            Color frameColor = friendly ? new Color(.22f, .55f, .95f) : new Color(.92f, .20f, .16f);

            GameObject anchor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            anchor.name = "NATO Ground Anchor";
            anchor.layer = LayerMask.NameToLayer("Ignore Raycast");
            anchor.transform.SetParent(transform, false);
            anchor.transform.localPosition = new Vector3(0f, .05f, 0f);
            anchor.transform.localScale = new Vector3(.16f, .025f, .16f);
            anchor.GetComponent<MeshRenderer>().sharedMaterial = new Material(solid) { color = frameColor };
            Destroy(anchor.GetComponent<Collider>());
            groundAnchorRenderer = anchor.GetComponent<MeshRenderer>();

            GameObject billboardRoot = new GameObject("Billboard Root");
            billboardRoot.transform.SetParent(transform, false);
            billboardRoot.transform.localPosition = new Vector3(0f, .28f, 0f);
            billboardRoot.transform.localRotation = CameraFacingTilt;
            Transform card = billboardRoot.transform;

            frame = new GameObject("NATO Frame").AddComponent<LineRenderer>();
            frame.transform.SetParent(card, false);
            frame.loop = true;
            frame.useWorldSpace = false;
            frame.positionCount = 4;
            frame.widthMultiplier = .055f;
            frame.material = new Material(overlay);
            frame.startColor = frameColor;
            frame.endColor = frameColor;
            Vector3[] corners =
            {
                new Vector3(-.42f, 0f, -.34f),
                new Vector3(-.42f, 0f, .34f),
                new Vector3(.42f, 0f, .34f),
                new Vector3(.42f, 0f, -.34f)
            };
            for (int index = 0; index < corners.Length; index++) frame.SetPosition(index, corners[index]);

            if (isSupportRole)
            {
                GameObject dot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                dot.name = "NATO Support Dot";
                dot.layer = LayerMask.NameToLayer("Ignore Raycast");
                dot.transform.SetParent(card, false);
                dot.transform.localPosition = Vector3.zero;
                dot.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                dot.transform.localScale = new Vector3(.26f, .015f, .26f);
                dot.GetComponent<MeshRenderer>().sharedMaterial = new Material(solid) { color = frameColor };
                Destroy(dot.GetComponent<Collider>());
                supportDotRenderer = dot.GetComponent<MeshRenderer>();
            }
            else
            {
                // The standard MIL-STD-2525 infantry glyph: a diagonal cross
                // filling the frame.
                crossA = BuildCrossLine("NATO Cross A", card, overlay, frameColor,
                    new Vector3(-.34f, 0f, -.26f), new Vector3(.34f, 0f, .26f));
                crossB = BuildCrossLine("NATO Cross B", card, overlay, frameColor,
                    new Vector3(-.34f, 0f, .26f), new Vector3(.34f, 0f, -.26f));
            }

            GameObject labelObject = new GameObject("NATO Label");
            labelObject.transform.SetParent(card, false);
            labelObject.transform.localPosition = new Vector3(0f, 0f, -.50f);
            // TextMesh's own default facing (normal along local Z) needs to
            // match the rest of this group's shared "lying in XZ, normal +Y"
            // authoring convention before card's tilt carries everything to
            // face the camera together.
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
            statusBadge.transform.SetParent(card, false);
            statusBadge.transform.localPosition = new Vector3(0f, .20f, .10f);
            statusBadge.transform.localScale = Vector3.one * .16f;
            statusBadgeRenderer = statusBadge.GetComponent<MeshRenderer>();
            statusBadgeRenderer.sharedMaterial = new Material(solid);
            Destroy(statusBadge.GetComponent<Collider>());
            statusBadge.SetActive(false);
        }

        private static LineRenderer BuildCrossLine(string objectName, Transform parent, Shader shader, Color color, Vector3 from, Vector3 to)
        {
            LineRenderer line = new GameObject(objectName).AddComponent<LineRenderer>();
            line.transform.SetParent(parent, false);
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
            if (groundAnchorRenderer != null) groundAnchorRenderer.material.color = frameColor;
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
