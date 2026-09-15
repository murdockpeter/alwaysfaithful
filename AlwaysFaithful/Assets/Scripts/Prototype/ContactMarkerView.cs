using AlwaysFaithful.Core;
using UnityEngine;

namespace AlwaysFaithful.Prototype
{
    public sealed class ContactMarkerView : MonoBehaviour
    {
        private MeshRenderer baseRenderer;
        private MeshRenderer faceRenderer;
        private TextMesh label;
        private LineRenderer uncertaintyRing;
        private Vector3 settledScale = Vector3.one;

        public TacticalVisibilityState PresentedState { get; private set; } = TacticalVisibilityState.Hidden;
        public bool PresentedStale { get; private set; }
        public int TransitionCount { get; private set; }
        public TacticalFireOutcome LastFireOutcome { get; private set; } = TacticalFireOutcome.Rejected;
        public int FireCueCount { get; private set; }

        public void Initialize()
        {
            Shader overlay = Resources.Load<Shader>("Shaders/MapOverlay") ?? Shader.Find("Sprites/Default");
            GameObject counterBase = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            counterBase.transform.SetParent(transform, false);
            counterBase.transform.localPosition = Vector3.up * .10f;
            counterBase.transform.localScale = new Vector3(.62f, .08f, .62f);
            baseRenderer = counterBase.GetComponent<MeshRenderer>();
            baseRenderer.sharedMaterial = new Material(overlay);
            Destroy(counterBase.GetComponent<Collider>());

            GameObject face = GameObject.CreatePrimitive(PrimitiveType.Cube);
            face.transform.SetParent(transform, false);
            face.transform.localPosition = Vector3.up * .21f;
            face.transform.localScale = new Vector3(.82f, .07f, .60f);
            faceRenderer = face.GetComponent<MeshRenderer>();
            faceRenderer.sharedMaterial = new Material(overlay);
            Destroy(face.GetComponent<Collider>());

            GameObject text = new GameObject("Contact Label");
            text.transform.SetParent(transform, false);
            text.transform.localPosition = Vector3.up * .265f;
            text.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            label = text.AddComponent<TextMesh>();
            label.alignment = TextAlignment.Center;
            label.anchor = TextAnchor.MiddleCenter;
            label.fontSize = 48;
            label.characterSize = .085f;

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
            gameObject.SetActive(false);
        }

        public void Present(TacticalContactState contact)
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
            }
            else if (contact.State == TacticalVisibilityState.Identified)
            {
                baseColor = new Color(.38f, .12f, .09f, .94f);
                faceColor = new Color(.82f, .48f, .24f, .94f);
                ringColor = new Color(1f, .56f, .20f, .94f);
                label.text = "PLA\nUNIT";
                settledScale = Vector3.one * .94f;
            }
            else
            {
                baseColor = new Color(.38f, .07f, .06f, 1f);
                faceColor = new Color(.79f, .33f, .25f, 1f);
                ringColor = new Color(1f, .31f, .22f, 1f);
                label.text = "PLA\nRIFLE";
                settledScale = Vector3.one;
            }
            baseRenderer.material.color = baseColor;
            faceRenderer.material.color = faceColor;
            label.color = new Color(.10f, .07f, .05f, 1f);
            uncertaintyRing.startColor = ringColor;
            uncertaintyRing.endColor = ringColor;
            uncertaintyRing.enabled = contact.State != TacticalVisibilityState.Observed;
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

        private void Update()
        {
            transform.localScale = Vector3.Lerp(transform.localScale, settledScale, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 9f));
        }
    }
}
