using System;
using System.Collections.Generic;

namespace AlwaysFaithful.Core
{
    public enum TacticalPlatoonRole
    {
        Rifle,
        Weapons,
        Reconnaissance,
        Engineers
    }

    public enum TacticalCompanyPreset
    {
        Balanced,
        Recon,
        Assault
    }

    [Serializable]
    public sealed class TacticalPlatoonDefinition
    {
        public string IdSuffix;
        public string DisplayName;
        public TacticalPlatoonRole Role;

        public TacticalPlatoonDefinition(string idSuffix, string displayName, TacticalPlatoonRole role)
        {
            IdSuffix = idSuffix;
            DisplayName = displayName;
            Role = role;
        }
    }

    // Package 1's deliberately compact reinforced-company roster. Every entry
    // is a platoon-scale counter; presets exchange the attachment, not the
    // three-platoon rifle core, so scenario balance remains predictable.
    public static class TacticalCompany
    {
        public const int DefaultPlatoonCount = 4;

        public static List<TacticalPlatoonDefinition> BuildRoster(TacticalCompanyPreset preset)
        {
            var roster = new List<TacticalPlatoonDefinition>
            {
                new TacticalPlatoonDefinition("rifle-1", "1st Rifle Platoon", TacticalPlatoonRole.Rifle),
                new TacticalPlatoonDefinition("rifle-2", "2nd Rifle Platoon", TacticalPlatoonRole.Rifle),
                new TacticalPlatoonDefinition("rifle-3", "3rd Rifle Platoon", TacticalPlatoonRole.Rifle)
            };
            switch (preset)
            {
                case TacticalCompanyPreset.Recon:
                    roster.Add(new TacticalPlatoonDefinition("recon", "Reconnaissance Platoon", TacticalPlatoonRole.Reconnaissance));
                    break;
                case TacticalCompanyPreset.Assault:
                    roster.Add(new TacticalPlatoonDefinition("engineers", "Combat Engineer Platoon", TacticalPlatoonRole.Engineers));
                    break;
                default:
                    roster.Add(new TacticalPlatoonDefinition("weapons", "Weapons Platoon", TacticalPlatoonRole.Weapons));
                    break;
            }
            return roster;
        }

        public static string ShortRole(TacticalPlatoonRole role)
        {
            switch (role)
            {
                case TacticalPlatoonRole.Weapons: return "WPNS PLT";
                case TacticalPlatoonRole.Reconnaissance: return "RECON PLT";
                case TacticalPlatoonRole.Engineers: return "ENGR PLT";
                default: return "RIFLE PLT";
            }
        }
    }
}
