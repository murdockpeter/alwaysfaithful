using System;
using System.Collections.Generic;

namespace AlwaysFaithful.Core
{
    [Serializable]
    public sealed class TacticalEnemyWeaponEntry
    {
        public string UnitId;
        public TacticalWeaponState Weapon;
    }

    [Serializable]
    public sealed class TacticalFriendlyWeaponEntry
    {
        public string UnitId;
        public TacticalWeaponState Weapon;
    }

    // A full in-memory tactical session snapshot: everything BuildTacticalBattlefield's
    // fresh-build path constructs deterministically (unit/enemy rosters, weapons,
    // contacts, turn number, event sequence) plus the already-serializable
    // TacticalBattlefieldState (terrain, event history, objective, recon markers).
    // Pure data — no file I/O or JSON parsing here; that lives in the Prototype layer.
    [Serializable]
    public sealed class TacticalBattleSaveState
    {
        public const int CurrentSchemaVersion = 3;

        public int SchemaVersion = CurrentSchemaVersion;
        public string SavedAtUtc;
        public TacticalBattlefieldState Battlefield;
        public TacticalTurnState Turn;
        public TacticalUnitState UsmcUnit;
        public TacticalWeaponState UsmcWeapon;
        // Schema 3: reinforced-company friendly force. The two singular fields
        // above remain populated as a compatibility mirror of the selected
        // platoon for older tooling and schema-2 save migration.
        public List<TacticalUnitState> FriendlyUnits = new List<TacticalUnitState>();
        public List<TacticalFriendlyWeaponEntry> FriendlyWeapons = new List<TacticalFriendlyWeaponEntry>();
        public string SelectedFriendlyUnitId;
        public TacticalCompanyPreset CompanyPreset = TacticalCompanyPreset.Balanced;
        public List<TacticalUnitState> EnemyUnits = new List<TacticalUnitState>();
        public List<TacticalEnemyWeaponEntry> EnemyWeapons = new List<TacticalEnemyWeaponEntry>();
        public List<TacticalContactState> Contacts = new List<TacticalContactState>();
        // Schema 2: the AI's own per-unit contact memory on the platoon
        // (TacticalContactState.ObserverId identifies which PLA unit each
        // entry belongs to, since every entry shares the same TargetId).
        public List<TacticalContactState> EnemyContacts = new List<TacticalContactState>();
        public int EventSequence;

        // JsonUtility can round-trip a null nested reference field as a
        // default-constructed object rather than null, so a request-less save
        // could otherwise come back looking request-driven. This flag makes
        // "no active request" unambiguous regardless of that behavior.
        public bool HasActiveBattleRequest;
        public BattleRequest ActiveBattleRequest;
    }

    public static class TacticalBattleSave
    {
        public static bool Validate(TacticalBattleSaveState state, out string error)
        {
            if (state == null)
            {
                error = "Save file did not parse to a valid save state";
                return false;
            }
            if (state.SchemaVersion != 2 && state.SchemaVersion != TacticalBattleSaveState.CurrentSchemaVersion)
            {
                error = $"Unsupported save schema version {state.SchemaVersion} (expected 2 or {TacticalBattleSaveState.CurrentSchemaVersion})";
                return false;
            }
            if (state.Battlefield == null || string.IsNullOrWhiteSpace(state.Battlefield.BattlefieldId))
            {
                error = "Save file is missing battlefield state";
                return false;
            }
            if (state.UsmcUnit == null && (state.FriendlyUnits == null || state.FriendlyUnits.Count == 0))
            {
                error = "Save file is missing the USMC unit";
                return false;
            }
            if (state.Turn == null)
            {
                error = "Save file is missing turn state";
                return false;
            }
            error = null;
            return true;
        }
    }
}
