using System;
using System.Collections.Generic;

namespace AlwaysFaithful.Core
{
    [Serializable]
    public sealed class BattleUnitImport
    {
        public string Role;
        public string UnitId;
        public string DisplayName;
    }

    // The Phase I "stub" request/result contract: theater location, forces,
    // posture, seed, and objective only. The full production contract
    // (personnel/equipment/ammunition/experience/support-asset timing,
    // integrity hashes, idempotent-reapply semantics) is a later milestone
    // and deliberately out of scope here.
    [Serializable]
    public sealed class BattleRequest
    {
        public const int CurrentContractVersion = 1;

        public int ContractVersion = CurrentContractVersion;
        public string RequestId;
        public string CampaignId;
        public int Seed;
        public HexCoord TheaterHex;
        public bool HasPostureOverride;
        public TacticalPosture Posture;
        public bool HasObjectiveOverride;
        public HexCoord ObjectiveHexOverride;
        public int TurnLimitOverride;
        public List<BattleUnitImport> Forces = new List<BattleUnitImport>();
        public string OutputPath;
    }

    [Serializable]
    public sealed class BattleUnitResult
    {
        public string Role;
        public string UnitId;
        public string DisplayName;
        public TacticalCombatStatus FinalStatus;
    }

    [Serializable]
    public sealed class BattleResult
    {
        public const int CurrentContractVersion = 1;

        public int ContractVersion = CurrentContractVersion;
        public string RequestId;
        public string CampaignId;
        public string BattlefieldId;
        public int Seed;
        public TacticalBattleOutcome Outcome;
        public string OutcomeSummary;
        public TacticalPosture Posture;
        public HexCoord ObjectiveHex;
        public bool ObjectiveControlledByUsmc;
        public int TurnsTaken;
        public int TurnLimit;
        public List<BattleUnitResult> Forces = new List<BattleUnitResult>();
        public int UsmcCasualties;
        public int PlaCasualties;
        public List<string> EventLog = new List<string>();
        public string CompletedAtUtc;
    }

    // Pure structural validation only — no file I/O, no JSON library, and no
    // board/geography access. Parsing the JSON (UnityEngine.JsonUtility) and
    // reading/writing files both live in the Prototype/Unity layer, which is
    // the only place that already depends on either; Core stays plain C#.
    public static class TacticalBattleContract
    {
        public static bool ValidateStructure(BattleRequest request, out string error)
        {
            if (request == null)
            {
                error = "Request file did not parse to a valid BattleRequest";
                return false;
            }
            if (request.ContractVersion != BattleRequest.CurrentContractVersion)
            {
                error = $"Unsupported BattleRequest contract version {request.ContractVersion} (expected {BattleRequest.CurrentContractVersion})";
                return false;
            }
            if (string.IsNullOrWhiteSpace(request.RequestId))
            {
                error = "BattleRequest is missing a RequestId";
                return false;
            }
            if (string.IsNullOrWhiteSpace(request.OutputPath))
            {
                error = "BattleRequest is missing an OutputPath";
                return false;
            }
            error = null;
            return true;
        }
    }
}
