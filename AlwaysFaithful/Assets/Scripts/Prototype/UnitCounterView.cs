using AlwaysFaithful.Core;
using UnityEngine;

namespace AlwaysFaithful.Prototype
{
    public sealed class UnitCounterView : MonoBehaviour
    {
        private LineRenderer selectionRing;
        private MeshRenderer baseRenderer;
        private MeshRenderer faceRenderer;

        public UnitReadiness PresentedReadiness { get; private set; }
        public bool PresentedSelection { get; private set; }

        public void Initialize(string displayName)
        {
            name = displayName;
            BuildSelectionRing();
        }

        public void BindRenderers(MeshRenderer counterBase, MeshRenderer counterFace)
        {
            baseRenderer = counterBase;
            faceRenderer = counterFace;
        }

        public void Present(TacticalUnitState state)
        {
            PresentedReadiness = state.Readiness;
            PresentedSelection = state.IsSelected;
            if (selectionRing != null)
            {
                selectionRing.enabled = state.IsSelected || state.Readiness == UnitReadiness.Moving;
                Color ringColor = state.Readiness == UnitReadiness.Moving
                    ? new Color(.34f, .96f, .82f, 1f)
                    : new Color(.98f, .76f, .22f, 1f);
                selectionRing.startColor = ringColor;
                selectionRing.endColor = ringColor;
            }

            if (baseRenderer == null || faceRenderer == null) return;
            Color baseColor;
            Color faceColor;
            switch (state.Readiness)
            {
                case UnitReadiness.Moving:
                    baseColor = new Color(.08f, .38f, .35f);
                    faceColor = new Color(.68f, .86f, .70f);
                    break;
                case UnitReadiness.Spent:
                    baseColor = new Color(.10f, .13f, .12f);
                    faceColor = new Color(.36f, .37f, .31f);
                    break;
                default:
                    baseColor = state.IsSelected ? new Color(.34f, .28f, .10f) : new Color(.13f, .25f, .20f);
                    faceColor = state.IsSelected ? new Color(.91f, .76f, .36f) : new Color(.76f, .72f, .55f);
                    break;
            }
            baseRenderer.material.color = baseColor;
            faceRenderer.material.color = faceColor;
        }

        public bool Matches(TacticalUnitState state)
            => PresentedReadiness == state.Readiness && PresentedSelection == state.IsSelected;

        private void BuildSelectionRing()
        {
            GameObject ring = new GameObject("Selection Ring");
            ring.transform.SetParent(transform, false);
            ring.transform.localPosition = Vector3.up * .12f;
            selectionRing = ring.AddComponent<LineRenderer>();
            selectionRing.loop = true;
            selectionRing.useWorldSpace = false;
            selectionRing.positionCount = 49;
            selectionRing.widthMultiplier = .045f;
            Shader overlayShader = Resources.Load<Shader>("Shaders/MapOverlay") ?? Shader.Find("Sprites/Default");
            selectionRing.material = new Material(overlayShader);
            selectionRing.startColor = new Color(.98f, .76f, .22f, 1f);
            selectionRing.endColor = selectionRing.startColor;
            selectionRing.sortingOrder = 70;
            for (int index = 0; index < selectionRing.positionCount; index++)
            {
                float angle = index / 48f * Mathf.PI * 2f;
                selectionRing.SetPosition(index, new Vector3(Mathf.Cos(angle) * .72f, 0f, Mathf.Sin(angle) * .72f));
            }
        }
    }
}
