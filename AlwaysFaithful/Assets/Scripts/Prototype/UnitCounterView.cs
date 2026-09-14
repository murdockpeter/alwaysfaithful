using AlwaysFaithful.Core;
using UnityEngine;

namespace AlwaysFaithful.Prototype
{
    public sealed class UnitCounterView : MonoBehaviour
    {
        private LineRenderer selectionRing;

        public string UnitName { get; private set; }
        public HexCoord Position { get; private set; }
        public bool IsSelected { get; private set; }

        public void Initialize(string unitName, HexCoord position)
        {
            UnitName = unitName;
            Position = position;
            name = unitName;
            BuildSelectionRing();
            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            IsSelected = selected;
            if (selectionRing != null) selectionRing.enabled = selected;
        }

        public void SetPosition(HexCoord position)
        {
            Position = position;
        }

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
            selectionRing.material = new Material(Shader.Find("Sprites/Default"));
            selectionRing.startColor = new Color(.98f, .76f, .22f, 1f);
            selectionRing.endColor = selectionRing.startColor;
            for (int index = 0; index < selectionRing.positionCount; index++)
            {
                float angle = index / 48f * Mathf.PI * 2f;
                selectionRing.SetPosition(index, new Vector3(Mathf.Cos(angle) * .72f, 0f, Mathf.Sin(angle) * .72f));
            }
        }
    }
}
