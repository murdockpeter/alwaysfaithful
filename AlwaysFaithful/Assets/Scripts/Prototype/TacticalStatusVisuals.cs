using AlwaysFaithful.Core;
using UnityEngine;

namespace AlwaysFaithful.Prototype
{
    internal static class TacticalStatusVisuals
    {
        public static float DesaturationFor(TacticalCombatStatus status)
        {
            switch (status)
            {
                case TacticalCombatStatus.Suppressed: return .35f;
                case TacticalCombatStatus.Disrupted: return .62f;
                case TacticalCombatStatus.Reduced: return .85f;
                default: return 0f;
            }
        }

        public static Color Desaturate(Color color, float amount)
        {
            float gray = color.r * .299f + color.g * .587f + color.b * .114f;
            return new Color(
                Mathf.Lerp(color.r, gray, amount),
                Mathf.Lerp(color.g, gray, amount),
                Mathf.Lerp(color.b, gray, amount),
                color.a);
        }

        public static Color BadgeColor(TacticalCombatStatus status)
        {
            switch (status)
            {
                case TacticalCombatStatus.Reduced: return new Color(.94f, .16f, .13f, .95f);
                case TacticalCombatStatus.Disrupted: return new Color(1f, .52f, .14f, .95f);
                case TacticalCombatStatus.Suppressed: return new Color(1f, .82f, .22f, .95f);
                default: return new Color(1f, 1f, 1f, 0f);
            }
        }

        public static float PulseScale(TacticalCombatStatus status, float baseScale)
        {
            float speed = status == TacticalCombatStatus.Reduced ? 6f : 3.4f;
            return baseScale * (1f + Mathf.Sin(Time.unscaledTime * speed) * .12f);
        }
    }
}
