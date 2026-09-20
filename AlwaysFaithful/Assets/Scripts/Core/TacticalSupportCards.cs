using System;

namespace AlwaysFaithful.Core
{
    public enum TacticalSupportAssetType
    {
        Isr,
        FireSupport,
        Reserve
    }

    // A single drawable/playable Battalion support asset. Deliberately holds
    // only an enum + a stable ID — display text and (eventually) imagery are
    // derived from the enum via TacticalSupportCardCatalog below, so adding
    // artwork later is a catalog addition, not a change to this persisted
    // shape.
    [Serializable]
    public sealed class TacticalSupportCard
    {
        public int CardId;
        public TacticalSupportAssetType AssetType;
    }

    // A record of a card actually being played, for the in-battle/after-action
    // event log — distinct from TacticalSupportCard (the persisted hand entry
    // itself), the same split TacticalReconMarker/TacticalReconEvent already use.
    [Serializable]
    public sealed class TacticalSupportCardEvent
    {
        public int Sequence;
        public string BattlefieldId;
        public int Turn;
        public TacticalSupportAssetType AssetType;
        public string Summary;
    }

    public static class TacticalSupportCardCatalog
    {
        public static string DisplayName(TacticalSupportAssetType type)
        {
            switch (type)
            {
                case TacticalSupportAssetType.Isr: return "ISR SWEEP";
                case TacticalSupportAssetType.FireSupport: return "FIRE SUPPORT";
                default: return "RESERVE PLATOON";
            }
        }

        public static string Description(TacticalSupportAssetType type)
        {
            switch (type)
            {
                case TacticalSupportAssetType.Isr:
                    return "Elevates enemy detection for the whole battle.";
                case TacticalSupportAssetType.FireSupport:
                    return "Prep bombardment suppresses enemies near the objective.";
                default:
                    return "Reinforcement grants bonus action points for the battle.";
            }
        }
    }

    // Standalone-only Battalion resource pool. Cards are drawn deterministically
    // at battle conclusion (AlwaysFaithfulPrototype.cs) and spent for a
    // whole-battle pre-battle effect; the roll idiom here is intentionally a
    // small duplicate of TacticalScenario/TacticalVictory/TacticalDirectFire's
    // own CreateSeed/DeterministicRoll pairs rather than a shared utility,
    // matching this codebase's established convention.
    public static class TacticalSupportCards
    {
        public const int MaximumHandSize = 4;
        public const int ReserveActionPointBonus = 2;
        public const int FireSupportRadiusHexes = 2;

        public static int CreateSeed(string battlefieldId, int turn, int sequence)
        {
            unchecked
            {
                uint hash = 2166136261u;
                string text = battlefieldId ?? string.Empty;
                for (int index = 0; index < text.Length; index++) hash = (hash ^ text[index]) * 16777619u;
                hash = (hash ^ (uint)turn) * 16777619u;
                hash = (hash ^ (uint)sequence) * 16777619u;
                return (int)(hash & 0x7fffffff);
            }
        }

        private static int DeterministicRoll(int seed)
        {
            unchecked
            {
                uint value = (uint)seed + 0x9E3779B9u;
                value ^= value << 13;
                value ^= value >> 17;
                value ^= value << 5;
                return (int)(value % 100u) + 1;
            }
        }

        public static TacticalSupportAssetType RollAssetType(int seed)
        {
            return (TacticalSupportAssetType)((uint)DeterministicRoll(seed) % 3u);
        }
    }
}
