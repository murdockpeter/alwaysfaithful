using AlwaysFaithful.Core;
using UnityEngine;

namespace AlwaysFaithful.Prototype
{
    public sealed class ContactMarkerView : MonoBehaviour
    {
        private MeshRenderer baseRenderer;
        private MeshRenderer faceRenderer;
        private TextMesh label;
        private GameObject labelObject;
        private GameObject formationDetail;
        private GameObject contactGlyph;
        private MeshRenderer contactGlyphRenderer;
        private TacticalFormationView formationView;
        private LineRenderer uncertaintyRing;
        private Vector3 settledScale = Vector3.one;
        private float displayScale = 1f;
        private GameObject statusBadge;
        private MeshRenderer statusBadgeRenderer;

        public TacticalVisibilityState PresentedState { get; private set; } = TacticalVisibilityState.Hidden;
        public bool PresentedStale { get; private set; }
        public int TransitionCount { get; private set; }
        public TacticalFireOutcome LastFireOutcome { get; private set; } = TacticalFireOutcome.Rejected;
        public int FireCueCount { get; private set; }
        public TacticalCombatStatus PresentedStatus { get; private set; } = TacticalCombatStatus.Ready;
        public bool DetailedFormationVisible => formationDetail != null && formationDetail.activeSelf;
        public bool ContactGlyphVisible => contactGlyph != null && contactGlyph.activeSelf;
        public int FormationElementCount => formationView != null ? formationView.ManeuverElementCount : 0;

        public void Initialize(string echelon)
        {
            Shader overlay = Resources.Load<Shader>("Shaders/MapOverlay") ?? Shader.Find("Sprites/Default");
            formationDetail = new GameObject("Identified Formation Detail");
            formationDetail.transform.SetParent(transform, false);
            formationView = formationDetail.AddComponent<TacticalFormationView>();
            formationView.Initialize(TacticalFormationAffiliation.Pla, "PLA", echelon);
            baseRenderer = formationView.CommandDeckRenderer;
            faceRenderer = formationView.DesignationRenderer;

            contactGlyph = GameObject.CreatePrimitive(PrimitiveType.Cube);
            contactGlyph.name = "Uncertain Contact Diamond";
            contactGlyph.layer = LayerMask.NameToLayer("Ignore Raycast");
            contactGlyph.transform.SetParent(transform, false);
            contactGlyph.transform.localPosition = Vector3.up * .16f;
            contactGlyph.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            contactGlyph.transform.localScale = new Vector3(.48f, .08f, .48f);
            contactGlyphRenderer = contactGlyph.GetComponent<MeshRenderer>();
            contactGlyphRenderer.sharedMaterial = new Material(overlay);
            Destroy(contactGlyph.GetComponent<Collider>());

            labelObject = new GameObject("Contact Label");
            labelObject.transform.SetParent(transform, false);
            labelObject.transform.localPosition = Vector3.up * .255f;
            labelObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            label = labelObject.AddComponent<TextMesh>();
            label.alignment = TextAlignment.Center;
            label.anchor = TextAnchor.MiddleCenter;
            label.fontSize = 48;
            label.characterSize = .10f;

            GameObject ring = new GameObject("Uncertainty Ring");
            ring.transform.SetParent(transform, false);
            ring.transform.localPosition = Vector3.up * .15f;
            uncertaintyRing = ring.AddComponent<LineRenderer>();
            uncertaintyRing.loop = true;
            uncertaintyRing.useWorldSpace = false;
            uncertaintyRing.positionCount = 36;
            uncertaintyRing.widthMultiplier = .055f;
            uncertaintyRing.material = new Material(overlay);
            for (int index = 0; index < uncertaintyRing.positionCount; index++)
            {
                float angle = index / (float)uncertaintyRing.positionCount * Mathf.PI * 2f;
                float radius = index % 2 == 0 ? .70f : .64f;
                uncertaintyRing.SetPosition(index, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
            }

            statusBadge = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            statusBadge.name = "Contact Status Badge";
            statusBadge.layer = LayerMask.NameToLayer("Ignore Raycast");
            statusBadge.transform.SetParent(transform, false);
            statusBadge.transform.localPosition = new Vector3(0f, .47f, .12f);
            statusBadge.transform.localScale = Vector3.one * .16f;
            statusBadgeRenderer = statusBadge.GetComponent<MeshRenderer>();
            statusBadgeRenderer.sharedMaterial = new Material(overlay);
            Destroy(statusBadge.GetComponent<Collider>());
            statusBadge.SetActive(false);

            gameObject.SetActive(false);
        }

        public void Present(TacticalContactState contact, TacticalUnitState unit = null)
        {
            bool changed = contact.State != PresentedState || contact.IsStale != PresentedStale;
            PresentedState = contact.State;
            PresentedStale = contact.IsStale;
            if (contact.State == TacticalVisibilityState.Hidden)
            {
                gameObject.SetActive(false);
                return;
            }
            gameObject.SetActive(true);
            if (changed)
            {
                TransitionCount++;
                transform.localScale = Vector3.one * .72f;
            }
            Color baseColor;
            Color faceColor;
            Color ringColor;
            if (contact.State == TacticalVisibilityState.Contact)
            {
                baseColor = new Color(.31f, .24f, .09f, contact.IsStale ? .58f : .92f);
                faceColor = new Color(.88f, .61f, .17f, contact.IsStale ? .52f : .90f);
                ringColor = new Color(1f, .69f, .19f, contact.IsStale ? .46f : .92f);
                label.text = contact.IsStale ? "?\nLAST" : "?";
                settledScale = Vector3.one * .86f;
                formationDetail.SetActive(false);
                contactGlyph.SetActive(true);
                labelObject.SetActive(true);
            }
            else if (contact.State == TacticalVisibilityState.Identified)
            {
                baseColor = new Color(.38f, .12f, .09f, .94f);
                faceColor = new Color(.82f, .48f, .24f, .94f);
                ringColor = new Color(1f, .56f, .20f, .94f);
                settledScale = Vector3.one * .94f;
                formationDetail.SetActive(true);
                contactGlyph.SetActive(false);
                labelObject.SetActive(false);
            }
            else
            {
                baseColor = new Color(.38f, .07f, .06f, 1f);
                faceColor = new Color(.79f, .33f, .25f, 1f);
                ringColor = new Color(1f, .31f, .22f, 1f);
                settledScale = Vector3.one;
                formationDetail.SetActive(true);
                contactGlyph.SetActive(false);
                labelObject.SetActive(false);
            }
            bool statusKnown = unit != null && formationDetail.activeSelf;
            PresentedStatus = statusKnown ? unit.CombatStatus : TacticalCombatStatus.Ready;
            float desaturation = statusKnown ? TacticalStatusVisuals.DesaturationFor(PresentedStatus) : 0f;
            baseRenderer.material.color = TacticalStatusVisuals.Desaturate(baseColor, desaturation);
            faceRenderer.material.color = TacticalStatusVisuals.Desaturate(faceColor, desaturation);
            contactGlyphRenderer.material.color = faceColor;
            label.color = new Color(.10f, .07f, .05f, 1f);
            uncertaintyRing.startColor = ringColor;
            uncertaintyRing.endColor = ringColor;
            uncertaintyRing.enabled = contact.State != TacticalVisibilityState.Observed;
            UpdateStatusBadge(statusKnown && PresentedStatus != TacticalCombatStatus.Ready);
        }

        private void UpdateStatusBadge(bool visible)
        {
            if (statusBadge == null) return;
            statusBadge.SetActive(visible);
            if (visible) statusBadgeRenderer.material.color = TacticalStatusVisuals.BadgeColor(PresentedStatus);
        }

        public void CueFireOutcome(TacticalFireOutcome outcome)
        {
            LastFireOutcome = outcome;
            FireCueCount++;
            transform.localScale = settledScale * 1.22f;
            if (outcome == TacticalFireOutcome.Hit) faceRenderer.material.color = new Color(1f, .24f, .14f, 1f);
            else if (outcome == TacticalFireOutcome.Suppressed) faceRenderer.material.color = new Color(1f, .68f, .16f, 1f);
            else faceRenderer.material.color = new Color(.62f, .66f, .61f, 1f);
        }

        public int ReactionCueCount { get; private set; }

        public void CueReactionSource()
        {
            ReactionCueCount++;
            gameObject.SetActive(true);
            transform.localScale = settledScale * 1.30f;
            faceRenderer.material.color = new Color(1f, .90f, .40f, 1f);
            uncertaintyRing.enabled = true;
            uncertaintyRing.startColor = new Color(1f, .90f, .30f, .95f);
            uncertaintyRing.endColor = uncertaintyRing.startColor;
        }

        public void SetDisplayScale(float scale)
        {
            displayScale = Mathf.Max(.1f, scale);
        }

        private void Update()
        {
            transform.localScale = Vector3.Lerp(transform.localScale, settledScale * displayScale, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 9f));
            if (statusBadge != null && statusBadge.activeSelf)
                statusBadge.transform.localScale = Vector3.one * TacticalStatusVisuals.PulseScale(PresentedStatus, .16f);
        }
    }
}
