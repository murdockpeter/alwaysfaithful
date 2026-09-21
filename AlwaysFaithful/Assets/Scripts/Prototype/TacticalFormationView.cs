using System.Collections.Generic;
using AlwaysFaithful.Core;
using UnityEngine;

namespace AlwaysFaithful.Prototype
{
    public enum TacticalFormationAffiliation
    {
        Usmc,
        Pla
    }

    public sealed class TacticalFormationView : MonoBehaviour
    {
        private readonly List<MeshRenderer> elementRenderers = new List<MeshRenderer>();
        private MeshRenderer commandDeckRenderer;
        private MeshRenderer designationRenderer;
        private Color bodyColor;
        private Color deckColor;
        private Color plateColor;
        private GameObject statusBadge;
        private MeshRenderer statusBadgeRenderer;

        public int ManeuverElementCount { get; private set; }
        public bool HasRecognitionStripe { get; private set; }
        public bool HasCommandNode { get; private set; }
        public TacticalFormationAffiliation Affiliation { get; private set; }
        public MeshRenderer CommandDeckRenderer => commandDeckRenderer;
        public MeshRenderer DesignationRenderer => designationRenderer;
        public TacticalCombatStatus PresentedStatus { get; private set; }

        public void Initialize(TacticalFormationAffiliation affiliation, string formationCode, string echelon)
        {
            Affiliation = affiliation;
            bodyColor = affiliation == TacticalFormationAffiliation.Usmc
                ? new Color(.28f, .43f, .31f) : new Color(.49f, .15f, .11f);
            deckColor = affiliation == TacticalFormationAffiliation.Usmc
                ? new Color(.07f, .17f, .14f) : new Color(.22f, .055f, .045f);
            plateColor = affiliation == TacticalFormationAffiliation.Usmc
                ? new Color(.79f, .74f, .55f) : new Color(.80f, .38f, .27f);
            Shader overlay = Resources.Load<Shader>("Shaders/MapOverlay") ?? Shader.Find("Sprites/Default");
            Shader solid = Resources.Load<Shader>("Shaders/MapSolid") ?? overlay;

            GameObject shadow = Primitive(PrimitiveType.Cylinder, "Formation Soft Shadow", transform, overlay, new Color(.01f, .025f, .023f, .42f));
            shadow.transform.localPosition = Vector3.up * .025f;
            shadow.transform.localScale = new Vector3(.86f, .012f, .69f);

            GameObject deck = Primitive(PrimitiveType.Cylinder, "Formation Command Deck", transform, solid, deckColor);
            deck.transform.localPosition = Vector3.up * .09f;
            deck.transform.localScale = new Vector3(.73f, .045f, .59f);
            commandDeckRenderer = deck.GetComponent<MeshRenderer>();

            Vector3[] elementPositions =
            {
                new Vector3(-.34f, .19f, .08f),
                new Vector3(0f, .21f, .27f),
                new Vector3(.34f, .19f, .08f)
            };
            float[] yaws = { -14f, 0f, 14f };
            for (int index = 0; index < elementPositions.Length; index++)
            {
                GameObject element = Primitive(PrimitiveType.Cube, $"Maneuver Element {index + 1}", transform, solid, bodyColor);
                element.transform.localPosition = elementPositions[index];
                element.transform.localRotation = Quaternion.Euler(0f, yaws[index], 0f);
                element.transform.localScale = new Vector3(.22f, .11f, .37f);
                elementRenderers.Add(element.GetComponent<MeshRenderer>());

                GameObject elementCap = Primitive(PrimitiveType.Sphere, $"Element Node {index + 1}", element.transform, solid, plateColor);
                elementCap.transform.localPosition = new Vector3(0f, .58f, .16f);
                elementCap.transform.localScale = new Vector3(.34f, .20f, .26f);
            }
            ManeuverElementCount = elementPositions.Length;

            GameObject commandNode = Primitive(PrimitiveType.Cylinder, "Formation Command Node", transform, solid, plateColor);
            commandNode.transform.localPosition = new Vector3(0f, .24f, -.08f);
            commandNode.transform.localScale = new Vector3(.13f, .12f, .13f);
            GameObject commandCap = Primitive(PrimitiveType.Sphere, "Command Node Cap", transform, solid, plateColor);
            commandCap.transform.localPosition = new Vector3(0f, .39f, -.08f);
            commandCap.transform.localScale = Vector3.one * .16f;
            HasCommandNode = true;

            Color recognitionColor = affiliation == TacticalFormationAffiliation.Usmc
                ? new Color(.26f, .94f, .79f) : new Color(1f, .55f, .19f);
            GameObject stripe = Primitive(PrimitiveType.Cube, "Side Recognition Stripe", transform, solid, recognitionColor);
            stripe.transform.localPosition = new Vector3(0f, .235f, -.30f);
            stripe.transform.localScale = new Vector3(.72f, .025f, .055f);
            HasRecognitionStripe = true;

            GameObject plate = Primitive(PrimitiveType.Cube, "Formation Designation Plate", transform, solid, plateColor);
            plate.transform.localPosition = new Vector3(0f, .19f, -.42f);
            plate.transform.localScale = new Vector3(.70f, .055f, .20f);
            designationRenderer = plate.GetComponent<MeshRenderer>();

            GameObject labelObject = new GameObject("Formation Designation");
            labelObject.transform.SetParent(transform, false);
            labelObject.transform.localPosition = new Vector3(0f, .253f, -.42f);
            labelObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            TextMesh label = labelObject.AddComponent<TextMesh>();
            label.text = formationCode + "\n" + echelon;
            label.alignment = TextAlignment.Center;
            label.anchor = TextAnchor.MiddleCenter;
            label.fontSize = 48;
            label.characterSize = .062f;
            label.color = new Color(.06f, .08f, .065f);

            LineRenderer frontage = new GameObject("Formation Frontage Chevron").AddComponent<LineRenderer>();
            frontage.transform.SetParent(transform, false);
            frontage.useWorldSpace = false;
            frontage.positionCount = 3;
            frontage.widthMultiplier = .035f;
            frontage.material = new Material(overlay);
            frontage.startColor = recognitionColor;
            frontage.endColor = recognitionColor;
            frontage.SetPosition(0, new Vector3(-.54f, .275f, .24f));
            frontage.SetPosition(1, new Vector3(0f, .275f, .48f));
            frontage.SetPosition(2, new Vector3(.54f, .275f, .24f));

            statusBadge = Primitive(PrimitiveType.Sphere, "Suppression Status Badge", transform, solid, new Color(1f, 1f, 1f, 0f));
            statusBadge.transform.localPosition = new Vector3(0f, .47f, .12f);
            statusBadge.transform.localScale = Vector3.one * .16f;
            statusBadgeRenderer = statusBadge.GetComponent<MeshRenderer>();
            statusBadge.SetActive(false);
        }

        public void Present(TacticalUnitState state)
        {
            if (state == null) return;
            PresentedStatus = state.CombatStatus;
            float readiness = state.Readiness == UnitReadiness.Spent ? .42f : state.Readiness == UnitReadiness.Moving ? .82f : 1f;
            float desaturation = TacticalStatusVisuals.DesaturationFor(state.CombatStatus);
            Color effectiveDeck = TacticalStatusVisuals.Desaturate(deckColor, desaturation);
            Color effectivePlate = TacticalStatusVisuals.Desaturate(plateColor, desaturation);
            Color effectiveBody = TacticalStatusVisuals.Desaturate(bodyColor, desaturation);
            commandDeckRenderer.material.color = Color.Lerp(new Color(.06f, .075f, .07f), effectiveDeck, readiness);
            designationRenderer.material.color = Color.Lerp(new Color(.28f, .29f, .25f), effectivePlate, readiness);
            foreach (MeshRenderer renderer in elementRenderers)
                renderer.material.color = Color.Lerp(new Color(.15f, .17f, .15f), effectiveBody, readiness);
            UpdateStatusBadge(state.CombatStatus);
        }

        private void UpdateStatusBadge(TacticalCombatStatus status)
        {
            if (statusBadge == null) return;
            bool visible = status != TacticalCombatStatus.Ready;
            statusBadge.SetActive(visible);
            if (!visible) return;
            statusBadgeRenderer.material.color = TacticalStatusVisuals.BadgeColor(status);
        }

        private void Update()
        {
            if (statusBadge == null || !statusBadge.activeSelf) return;
            statusBadge.transform.localScale = Vector3.one * TacticalStatusVisuals.PulseScale(PresentedStatus, .16f);
        }

        private static GameObject Primitive(PrimitiveType type, string objectName, Transform parent, Shader shader, Color color)
        {
            GameObject result = GameObject.CreatePrimitive(type);
            result.name = objectName;
            result.layer = LayerMask.NameToLayer("Ignore Raycast");
            result.transform.SetParent(parent, false);
            result.GetComponent<MeshRenderer>().sharedMaterial = new Material(shader) { color = color };
            Destroy(result.GetComponent<Collider>());
            return result;
        }
    }
}
