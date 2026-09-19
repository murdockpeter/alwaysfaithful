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

    // App-level presentation/input preference, not tactical-battle domain
    // state (deliberately not prefixed Tactical, unlike every other Core
    // persisted type). File-persisted the same way TacticalBattalionStatus
    // is, surviving an app restart until the player resets it.
    [Serializable]
    public sealed class AlwaysFaithfulSettings
    {
        public const int CurrentSchemaVersion = 1;

        public int SchemaVersion = CurrentSchemaVersion;
        public UiScaleTier UiScale = UiScaleTier.Auto;
        public KeyCode RemapCancelKey = KeyCode.Escape;
        public KeyCode RemapResetCameraKey = KeyCode.R;
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
