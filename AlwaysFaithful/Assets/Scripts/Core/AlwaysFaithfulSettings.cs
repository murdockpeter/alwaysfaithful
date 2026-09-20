using System;
using UnityEngine;

namespace AlwaysFaithful.Core
{
    public enum UiScaleTier
    {
        Auto,
        Small,
        Normal,
        Large,
        ExtraLarge
    }

    // Low/Medium/High trade decorative density (cover/built-up prop count,
    // terrain contour shading) for draw calls; never touches gameplay values.
    public enum GraphicsPresetTier
    {
        Low,
        Medium,
        High
    }

    // Normal/Fast/Skip scale every timed presentation animation (movement
    // steps, fire lines, reaction-fire camera pans, enemy-turn pacing)
    // without changing what happens, only how long it takes to watch.
    public enum AnimationSpeedTier
    {
        Normal,
        Fast,
        Skip
    }

    // App-level presentation/input preference, not tactical-battle domain
    // state (deliberately not prefixed Tactical, unlike every other Core
    // persisted type). File-persisted the same way TacticalBattalionStatus
    // is, surviving an app restart until the player resets it.
    [Serializable]
    public sealed class AlwaysFaithfulSettings
    {
        public const int CurrentSchemaVersion = 2;

        public int SchemaVersion = CurrentSchemaVersion;
        public UiScaleTier UiScale = UiScaleTier.Auto;
        public KeyCode RemapCancelKey = KeyCode.Escape;
        public KeyCode RemapResetCameraKey = KeyCode.R;
        public GraphicsPresetTier GraphicsPreset = GraphicsPresetTier.Medium;
        public bool ColorSafePalette;
        public bool ReducedMotion;
        public AnimationSpeedTier AnimationSpeed = AnimationSpeedTier.Normal;
    }

    public static class AlwaysFaithfulSettingsRules
    {
        public static AlwaysFaithfulSettings CreateDefault() => new AlwaysFaithfulSettings();

        public static bool Validate(AlwaysFaithfulSettings settings, out string error)
        {
            if (settings == null)
            {
                error = "Settings file did not parse to a valid settings state";
                return false;
            }
            if (settings.SchemaVersion != AlwaysFaithfulSettings.CurrentSchemaVersion)
            {
                error = $"Unsupported settings schema version {settings.SchemaVersion} (expected {AlwaysFaithfulSettings.CurrentSchemaVersion})";
                return false;
            }
            if (settings.RemapCancelKey == KeyCode.None || settings.RemapResetCameraKey == KeyCode.None)
            {
                error = "Settings file has an unbound essential control";
                return false;
            }
            if (settings.RemapCancelKey == settings.RemapResetCameraKey)
            {
                error = "Settings file binds Cancel and Reset Camera to the same key";
                return false;
            }
            error = null;
            return true;
        }
    }
}
