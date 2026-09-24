using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using AlwaysFaithful.Core;
using AlwaysFaithful.Geography;
using UnityEngine;

namespace AlwaysFaithful.Prototype
{
    public sealed class AlwaysFaithfulPrototype : MonoBehaviour
    {
        private const int Width = 64;
        private const int Height = 104;
        private const float HexRadius = 1f;
        private const float MapHexKilometres = 4f;
        private const float CellSurfaceOffset = .10f;
        private const float CounterClearance = .04f;
        private const float PathClearance = .18f;
        private const float LocalReliefScale = .006f;
        private const int TacticalPlatoonActionPoints = 8;

        // Whole-island operational layer: persistent battalion maneuver and fog
        // hand detected contacts down into separate 250 m tactical maps.
        private const double DemoWest = 119.75;
        private const double DemoEast = 122.20;
        private const double DemoSouth = 21.70;
        private const double DemoNorth = 25.40;

        private readonly Dictionary<HexCoord, HexCellView> cells = new Dictionary<HexCoord, HexCellView>();
        private readonly Dictionary<HexCoord, TacticalCell> board = new Dictionary<HexCoord, TacticalCell>();
        private readonly Dictionary<HexCoord, int> reachable = new Dictionary<HexCoord, int>();
        private readonly List<HexCoord> previewPath = new List<HexCoord>();
        private readonly List<GameObject> pathMarkers = new List<GameObject>();
        private readonly List<ScreenHexPick> screenPickCache = new List<ScreenHexPick>();
        private readonly List<GeographicLabel> geographicLabels = new List<GeographicLabel>();
        private readonly Dictionary<HexCoord, HexCellView> localCells = new Dictionary<HexCoord, HexCellView>();
        private readonly Dictionary<HexCoord, TacticalMovementCell> localMovementBoard = new Dictionary<HexCoord, TacticalMovementCell>();
        private readonly Dictionary<HexCoord, int> tacticalReachable = new Dictionary<HexCoord, int>();
        private readonly List<HexCoord> tacticalPreviewPath = new List<HexCoord>();
        private readonly List<LineRenderer> tacticalLosSegments = new List<LineRenderer>();
        private readonly List<HexCoord> tacticalLosCells = new List<HexCoord>();
        private readonly List<TacticalUnitState> tacticalEnemyStates = new List<TacticalUnitState>();
        private readonly Dictionary<string, TacticalContactState> tacticalContacts = new Dictionary<string, TacticalContactState>();
        private readonly Dictionary<string, ContactMarkerView> tacticalContactViews = new Dictionary<string, ContactMarkerView>();
        private readonly Dictionary<string, TacticalWeaponState> tacticalEnemyWeapons = new Dictionary<string, TacticalWeaponState>();
        private Camera mapCamera;
        private TacticalAudio tacticalAudio;
        private GameObject overviewRoot;
        private GameObject tacticalRoot;
        private UnitCounterView unit;
        private TacticalUnitState unitState;
        private OperationalScenarioState operationalScenario;
        private readonly List<OperationalBattalionState> operationalFriendlyBattalions = new List<OperationalBattalionState>();
        private readonly List<OperationalBattalionState> operationalEnemyBattalions = new List<OperationalBattalionState>();
        private readonly Dictionary<string, UnitCounterView> operationalFriendlyViews = new Dictionary<string, UnitCounterView>();
        private readonly Dictionary<string, TacticalUnitState> operationalUnitStates = new Dictionary<string, TacticalUnitState>();
        private readonly Dictionary<UnitCounterView, OperationalBattalionState> operationalFriendlyByView = new Dictionary<UnitCounterView, OperationalBattalionState>();
        private readonly Dictionary<string, ContactMarkerView> operationalEnemyViews = new Dictionary<string, ContactMarkerView>();
        private readonly Dictionary<string, OperationalContactState> operationalContacts = new Dictionary<string, OperationalContactState>();
        private readonly List<OperationalReconMarker> operationalReconMarkers = new List<OperationalReconMarker>();
        private readonly Dictionary<HexCoord, LineRenderer> operationalReconRings = new Dictionary<HexCoord, LineRenderer>();
        private LineRenderer operationalObjectiveRing;
        private OperationalBattalionState activeOperationalBattalion;
        private bool operationalReconPlanning;
        private bool operationalBriefingActive;
        private bool operationalBriefingIntelPage;
        private bool operationalOrderOfBattleOpen;
        private string operationalScenarioPath;
        private bool operationalPersistenceEnabled;
        private Rect operationalBriefingRect;
        private Rect operationalOrderOfBattleRect;
        private TacticalTurnState turnState;
        private HexCellView selectedCell;
        private HexCellView hoveredCell;
        private Vector3 cameraFocus;
        private float cameraDistance = 190f;
        // Degrees. Yaw wraps freely (orbit around the focus); pitch is
        // clamped so the camera can never go fully vertical (yaw would
        // become meaningless) or drop to/below the horizon (looking into
        // the terrain edge-on). Defaults reproduce each mode's original
        // fixed viewing angle exactly, so nothing changes until a player
        // actually rotates.
        private float cameraYaw;
        private float cameraPitch = OperationalDefaultPitch;
        private const float MinCameraPitch = 2f;
        private const float MaxCameraPitch = 88f;
        private const float TacticalCameraGroundClearance = .32f;
        private const float OperationalCameraGroundClearance = .42f;
        private const float TacticalDefaultPitch = 62.6f;
        private const float OperationalDefaultPitch = 77.1f;
        private GeographicElevationGrid elevation;
        private CoastlineData coastline;
        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private GUIStyle badgeStyle;
        private GUIStyle unitNameStyle;
        private GUIStyle stateStyle;
        private GUIStyle buttonStyle;
        private GUIStyle reactionBannerStyle;
        private LineRenderer pathLine;
        private LineRenderer occupiedHexRing;
        private bool unitMoving;
        private bool automatedCapture;
        private bool automatedMovementRegression;
        private bool counterMenuOpen;
        private bool movePlanning;
        private Rect counterMenuRect;
        private Rect endTurnRect;
        private Rect enterTacticalRect;
        private Rect returnToIslandRect;
        private TacticalBattlefieldState tacticalBattlefield;
        private HexCellView hoveredLocalCell;
        private UnitCounterView tacticalUnit;
        private TacticalFormationView tacticalFormationView;
        private NatoSymbolView tacticalSymbolView;
        private MiniatureCounterView tacticalMiniatureView;
        private GameObject tacticalIllustratedDetail;
        private GameObject tacticalSymbolDetail;
        private GameObject tacticalMiniatureDetail;
        private TacticalUnitState tacticalUnitState;
        private LineRenderer tacticalRouteLine;
        private GameObject tacticalDestinationGhost;
        private MeshRenderer tacticalGhostRenderer;
        private bool tacticalMovePlanning;
        private bool tacticalUnitMoving;
        private bool tacticalMenuOpen;
        private Rect tacticalMenuRect;
        private string tacticalOrderFeedback;
        private int tacticalPreviewCost;
        private int tacticalEventSequence;
        private bool tacticalLosPlanning;
        private TacticalLosResult tacticalLosResult;
        private GameObject tacticalLosTarget;
        private MeshRenderer tacticalLosTargetRenderer;
        private TacticalWeaponState tacticalWeapon;
        private bool tacticalFirePlanning;
        private bool tacticalFireResolving;
        private TacticalFirePreview tacticalFirePreview;
        private TacticalUnitState tacticalFireTarget;
        private LineRenderer tacticalFireLine;
        private GameObject tacticalFireReticle;
        private MeshRenderer tacticalFireReticleRenderer;
        private Vector3 overviewCameraFocus;
        private float overviewCameraDistance;
        private float overviewCameraYaw;
        private float overviewCameraPitch;
        private float localMinimumLandElevation;
        private float localMaximumLandElevation;
        private bool tacticalMode;
        private bool mapTransitionActive;
        private float mapTransitionOpacity;
        private bool automatedTacticalRegression;
        private bool automatedTacticalCapture;
        private bool automatedTacticalMovementRegression;
        private bool automatedLosRegression;
        private bool automatedLosCapture;
        private bool automatedHudRegression;
        private bool automatedNoContactRegression;
        private bool automatedObservationRegression;
        private bool automatedObservationCapture;
        private bool automatedFireRegression;
        private bool automatedFireCapture;
        private bool automatedSuppressionRegression;
        private bool automatedSuppressionCapture;
        private bool automatedReactionRegression;
        private bool automatedReactionCapture;
        private bool automatedEnemyTurnRegression;
        private bool automatedEnemyTurnCapture;
        private bool automatedCoverRegression;
        private bool automatedVictoryRegression;
        private bool automatedResultCapture;
        private TacticalObjectiveState tacticalObjective;
        private LineRenderer tacticalObjectiveRing;
        private bool? tacticalObjectiveControlledByUsmc;
        private bool resultScreenActive;
        private float resultScreenOpacity;
        private GUIStyle resultHeadlineStyle;
        private bool tacticalReconPlanning;
        private readonly List<TacticalReconMarker> tacticalReconMarkers = new List<TacticalReconMarker>();
        private readonly Dictionary<HexCoord, LineRenderer> tacticalReconRings = new Dictionary<HexCoord, LineRenderer>();
        // PLA-side counterparts: the enemy's own active sensor-tasking markers
        // on the platoon, and the AI's own per-unit contact memory (mirrors
        // tacticalContacts, but keyed by the PLA unit that holds the contact).
        private readonly List<TacticalReconMarker> tacticalPlaReconMarkers = new List<TacticalReconMarker>();
        private readonly Dictionary<HexCoord, LineRenderer> tacticalPlaReconRings = new Dictionary<HexCoord, LineRenderer>();
        private readonly Dictionary<string, TacticalContactState> tacticalEnemyContacts = new Dictionary<string, TacticalContactState>();
        private TacticalVisibilityState tacticalUnitSpottedTier = TacticalVisibilityState.Hidden;
        private static readonly Color PlaReconRingColor = new Color(.92f, .30f, .18f, .85f);
        private bool automatedReconRegression;
        private bool automatedReconCapture;
        private BattleRequest activeBattleRequest;
        private int standaloneScenarioSeed;
        private int? scenarioSeedOverride;
        private bool automatedScenarioRegression;
        private bool automatedBattalionRegression;
        private TacticalBattalionStatus battalionStatus;
        private string battalionStatusPath;
        private bool supportCardModalActive;
        private bool supportPanelOpen;
        private bool automatedCardsRegression;
        private bool automatedPlaReconRegression;
        private bool automatedPlaReconCapture;
        private bool automatedOperationalScenarioRegression;
        private bool automatedOperationalScenarioCapture;
        private AlwaysFaithfulSettings settings;
        private string settingsPath;
        private bool settingsPanelOpen;
        private RemapTarget? awaitingRemapFor;
        private string remapRejectionText;
        private bool automatedSettingsRegression;
        private bool eventLogPanelOpen;
        private bool tacticalLosOverlayActive;
        private readonly List<HexCoord> tacticalLosOverlayCells = new List<HexCoord>();
        private string battleRequestError;
        private string campaignErrorTitle = "BATTLE REQUEST REJECTED";
        private string lastBattleResultPath;
        private bool campaignBriefingActive;
        private float campaignBriefingOpacity;
        private bool automatedBattleContractRegression;
        private bool automatedCampaignCapture;
        private string saveFilePath;
        private bool hasSavedBattle;
        private bool automatedSaveRestoreRegression;
        private bool automatedSaveRestoreCapture;
        private bool automatedFeedbackRegression;
        private bool tacticalReactionActive;
        private bool tacticalReactionHalted;
        private string tacticalReactionBannerText;
        private bool tacticalEnemyTurnActive;
        private bool fastEnemyAnimation;
        private string tacticalEnemyActivityText;
        private int tacticalHiddenEnemyActions;
        private int landCellCount;
        private int waterCellCount;
        private float maximumLandElevation;
        private float hexTopNormalY;
        private Vector3 cachedPickCameraPosition;
        private Quaternion cachedPickCameraRotation;
        private int cachedPickScreenWidth;
        private int cachedPickScreenHeight;
        private bool screenPickCacheValid;

        private struct ScreenHexPick
        {
            public HexCellView Cell;
            public Vector2 Center;
            public float Radius;
        }

        private enum RemapTarget
        {
            Cancel,
            ResetCamera
        }

        private sealed class GeographicLabel
        {
            public Transform Transform;
            public TextMesh Text;
            public Color BaseColor;
            public bool OverviewOnly;
        }

        private const int PlatoonMovementPoints = 4;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsurePrototype()
        {
            if (FindFirstObjectByType<AlwaysFaithfulPrototype>() != null) return;
            new GameObject("Always Faithful Prototype").AddComponent<AlwaysFaithfulPrototype>();
        }

        private void Awake()
        {
            Application.runInBackground = true;
            automatedCapture = Array.Exists(Environment.GetCommandLineArgs(), value => value.StartsWith("--capture-path=", StringComparison.Ordinal));
            automatedMovementRegression = Array.IndexOf(Environment.GetCommandLineArgs(), "--movement-regression") >= 0;
            automatedTacticalRegression = Array.IndexOf(Environment.GetCommandLineArgs(), "--tactical-regression") >= 0;
            automatedTacticalCapture = Array.Exists(Environment.GetCommandLineArgs(), value => value.StartsWith("--tactical-capture-path=", StringComparison.Ordinal));
            automatedTacticalMovementRegression = Array.IndexOf(Environment.GetCommandLineArgs(), "--tactical-movement-regression") >= 0;
            automatedLosRegression = Array.IndexOf(Environment.GetCommandLineArgs(), "--los-regression") >= 0;
            automatedHudRegression = Array.IndexOf(Environment.GetCommandLineArgs(), "--hud-regression") >= 0;
            automatedNoContactRegression = Array.IndexOf(Environment.GetCommandLineArgs(), "--no-contact-regression") >= 0;
            automatedLosCapture = Array.Exists(Environment.GetCommandLineArgs(), value => value.StartsWith("--los-capture-path=", StringComparison.Ordinal));
            automatedObservationRegression = Array.IndexOf(Environment.GetCommandLineArgs(), "--observation-regression") >= 0;
            automatedObservationCapture = Array.Exists(Environment.GetCommandLineArgs(), value => value.StartsWith("--observation-capture-path=", StringComparison.Ordinal));
            automatedFireRegression = Array.IndexOf(Environment.GetCommandLineArgs(), "--fire-regression") >= 0;
            automatedFireCapture = Array.Exists(Environment.GetCommandLineArgs(), value => value.StartsWith("--fire-capture-path=", StringComparison.Ordinal));
            automatedSuppressionRegression = Array.IndexOf(Environment.GetCommandLineArgs(), "--suppression-regression") >= 0;
            automatedSuppressionCapture = Array.Exists(Environment.GetCommandLineArgs(), value => value.StartsWith("--suppression-capture-path=", StringComparison.Ordinal));
            automatedReactionRegression = Array.IndexOf(Environment.GetCommandLineArgs(), "--reaction-regression") >= 0;
            automatedReactionCapture = Array.Exists(Environment.GetCommandLineArgs(), value => value.StartsWith("--reaction-capture-path=", StringComparison.Ordinal));
            automatedEnemyTurnRegression = Array.IndexOf(Environment.GetCommandLineArgs(), "--enemy-turn-regression") >= 0;
            automatedEnemyTurnCapture = Array.Exists(Environment.GetCommandLineArgs(), value => value.StartsWith("--enemy-turn-capture-path=", StringComparison.Ordinal));
            automatedCoverRegression = Array.IndexOf(Environment.GetCommandLineArgs(), "--cover-regression") >= 0;
            automatedVictoryRegression = Array.IndexOf(Environment.GetCommandLineArgs(), "--victory-regression") >= 0;
            automatedResultCapture = Array.Exists(Environment.GetCommandLineArgs(), value => value.StartsWith("--result-capture-path=", StringComparison.Ordinal));
            automatedReconRegression = Array.IndexOf(Environment.GetCommandLineArgs(), "--recon-regression") >= 0;
            automatedReconCapture = Array.Exists(Environment.GetCommandLineArgs(), value => value.StartsWith("--recon-capture-path=", StringComparison.Ordinal));
            automatedBattleContractRegression = Array.IndexOf(Environment.GetCommandLineArgs(), "--battle-contract-regression") >= 0;
            automatedCampaignCapture = Array.Exists(Environment.GetCommandLineArgs(), value => value.StartsWith("--campaign-capture-path=", StringComparison.Ordinal));
            automatedSaveRestoreRegression = Array.IndexOf(Environment.GetCommandLineArgs(), "--save-restore-regression") >= 0;
            automatedSaveRestoreCapture = Array.Exists(Environment.GetCommandLineArgs(), value => value.StartsWith("--save-restore-capture-path=", StringComparison.Ordinal));
            automatedFeedbackRegression = Array.IndexOf(Environment.GetCommandLineArgs(), "--feedback-regression") >= 0;
            automatedScenarioRegression = Array.IndexOf(Environment.GetCommandLineArgs(), "--scenario-regression") >= 0;
            automatedBattalionRegression = Array.IndexOf(Environment.GetCommandLineArgs(), "--battalion-regression") >= 0;
            automatedSettingsRegression = Array.IndexOf(Environment.GetCommandLineArgs(), "--settings-regression") >= 0;
            automatedCardsRegression = Array.IndexOf(Environment.GetCommandLineArgs(), "--cards-regression") >= 0;
            automatedPlaReconRegression = Array.IndexOf(Environment.GetCommandLineArgs(), "--pla-recon-regression") >= 0;
            automatedPlaReconCapture = Array.Exists(Environment.GetCommandLineArgs(), value => value.StartsWith("--pla-recon-capture-path=", StringComparison.Ordinal));
            automatedOperationalScenarioRegression = Array.IndexOf(Environment.GetCommandLineArgs(), "--operational-scenario-regression") >= 0;
            automatedOperationalScenarioCapture = Array.Exists(Environment.GetCommandLineArgs(), value => value.StartsWith("--operational-scenario-capture-path=", StringComparison.Ordinal));
            string scenarioSeedArgument = Array.Find(Environment.GetCommandLineArgs(), value => value.StartsWith("--scenario-seed=", StringComparison.Ordinal));
            scenarioSeedOverride = scenarioSeedArgument != null && int.TryParse(scenarioSeedArgument.Substring("--scenario-seed=".Length), out int parsedScenarioSeed) && parsedScenarioSeed > 0
                ? parsedScenarioSeed
                : (int?)null;
            fastEnemyAnimation = Array.IndexOf(Environment.GetCommandLineArgs(), "--fast-enemy") >= 0;
            string saveArgument = Array.Find(Environment.GetCommandLineArgs(), value => value.StartsWith("--save-path=", StringComparison.Ordinal));
            saveFilePath = saveArgument != null
                ? saveArgument.Substring("--save-path=".Length)
                : Path.Combine(Application.persistentDataPath, "always-faithful-battle-save.json");
            hasSavedBattle = File.Exists(saveFilePath);
            string battalionStatusArgument = Array.Find(Environment.GetCommandLineArgs(), value => value.StartsWith("--battalion-status-path=", StringComparison.Ordinal));
            battalionStatusPath = battalionStatusArgument != null
                ? battalionStatusArgument.Substring("--battalion-status-path=".Length)
                : Path.Combine(Application.persistentDataPath, "always-faithful-battalion-status.json");
            if (!TryLoadBattalionStatus(battalionStatusPath, out battalionStatus)) battalionStatus = TacticalBattalion.CreateFresh();
            string operationalScenarioArgument = Array.Find(Environment.GetCommandLineArgs(), value => value.StartsWith("--operational-scenario-path=", StringComparison.Ordinal));
            operationalScenarioPath = operationalScenarioArgument != null
                ? operationalScenarioArgument.Substring("--operational-scenario-path=".Length)
                : Path.Combine(Application.persistentDataPath, "always-faithful-operational-scenario.json");
            operationalPersistenceEnabled = operationalScenarioArgument != null || !IsAutomatedRun();
            string settingsArgument = Array.Find(Environment.GetCommandLineArgs(), value => value.StartsWith("--settings-path=", StringComparison.Ordinal));
            settingsPath = settingsArgument != null
                ? settingsArgument.Substring("--settings-path=".Length)
                : Path.Combine(Application.persistentDataPath, "always-faithful-settings.json");
            if (!TryLoadSettings(settingsPath, out settings)) settings = AlwaysFaithfulSettingsRules.CreateDefault();
            LoadGeography();
            BuildLightingAndCamera();
            overviewRoot = new GameObject("Taiwan Operational Map");
            overviewRoot.transform.SetParent(transform, false);
            BuildCommandTable();
            BuildBoard();
            if (!operationalPersistenceEnabled || !TryLoadOperationalScenario(operationalScenarioPath, out operationalScenario))
            {
                operationalScenario = CreateOperationalScenario();
                if (operationalPersistenceEnabled) SaveOperationalScenario();
            }
            BuildUnit();
            operationalBriefingActive = !IsAutomatedRun();
            ApplyCamera();
            string battleRequestPath = Array.Find(Environment.GetCommandLineArgs(), value => value.StartsWith("--battle-request=", StringComparison.Ordinal));
            if (battleRequestPath != null) TryLoadBattleRequest(battleRequestPath.Substring("--battle-request=".Length));
            if (automatedCapture)
            {
                BeginMovePlanning();
                DisplayCapturePath();
            }
            CompleteSmokeTestWhenRequested();
            if (automatedMovementRegression) StartCoroutine(RunMovementRegression());
            if (automatedTacticalRegression) StartCoroutine(RunTacticalRegression());
            if (automatedTacticalMovementRegression) StartCoroutine(RunTacticalMovementRegression());
            if (automatedLosRegression) StartCoroutine(RunLosRegression());
            if (automatedHudRegression) StartCoroutine(RunHudRegression());
            if (automatedNoContactRegression) StartCoroutine(RunNoContactRegression());
            if (automatedObservationRegression) StartCoroutine(RunObservationRegression());
            if (automatedFireRegression) StartCoroutine(RunFireRegression());
            if (automatedSuppressionRegression) StartCoroutine(RunSuppressionRegression());
            if (automatedReactionRegression) StartCoroutine(RunReactionRegression());
            if (automatedEnemyTurnRegression) StartCoroutine(RunEnemyTurnRegression());
            if (automatedCoverRegression) StartCoroutine(RunCoverRegression());
            if (automatedVictoryRegression) StartCoroutine(RunVictoryRegression());
            if (automatedReconRegression) StartCoroutine(RunReconRegression());
            if (automatedBattleContractRegression) StartCoroutine(RunBattleContractRegression());
            if (automatedTacticalCapture)
            {
                EnterTacticalMap(FindCoastalOperationalCell(), false);
                BeginTacticalMovePlanning();
                DisplayTacticalCaptureRoute();
                ApplyDevCaptureOverrides();
                StartCoroutine(CaptureTacticalScreenshotWhenRequested());
            }
            if (automatedLosCapture)
            {
                EnterTacticalMap(FindHighReliefOperationalCell(), false);
                BeginTacticalLosPlanning();
                PreviewTacticalLineOfSight(FindLosCaptureTarget());
                StartCoroutine(CaptureLosScreenshotWhenRequested());
            }
            if (automatedObservationCapture)
            {
                EnterTacticalMap(FindHighReliefOperationalCell(), false);
                FrameObservationContacts();
                StartCoroutine(CaptureObservationScreenshotWhenRequested());
            }
            if (automatedFireCapture)
            {
                EnterTacticalMap(FindHighReliefOperationalCell(), false);
                FrameObservationContacts();
                BeginTacticalFirePlanning();
                PreviewTacticalFire(FindObservedEnemyCell());
                ApplyDevCaptureOverrides();
                StartCoroutine(CaptureFireScreenshotWhenRequested());
            }
            if (automatedSuppressionCapture)
            {
                EnterTacticalMap(FindHighReliefOperationalCell(), false);
                FrameObservationContacts();
                TacticalSuppression.ApplyFireOutcome(tacticalEnemyStates[0], TacticalFireOutcome.Suppressed);
                TacticalSuppression.ApplyFireOutcome(tacticalEnemyStates[0], TacticalFireOutcome.Hit);
                RefreshTacticalObservation();
                cameraFocus = tacticalContactViews[tacticalEnemyStates[0].Id].transform.position;
                cameraDistance = 9f;
                ApplyCamera();
                OpenTacticalMenu(new Vector2(Screen.width * .5f, Screen.height * .5f) / GetUiScale());
                StartCoroutine(CaptureSuppressionScreenshotWhenRequested());
            }
            if (automatedReactionCapture)
            {
                EnterTacticalMap(FindHighReliefOperationalCell(), false);
                FrameObservationContacts();
                TacticalUnitState reactor = tacticalEnemyStates[0];
                tacticalReactionActive = true;
                tacticalReactionBannerText = $"⚠ REACTION FIRE • {reactor.DisplayName.ToUpperInvariant()}";
                tacticalFireLine.enabled = true;
                tacticalFireLine.startColor = new Color(1f, .82f, .31f, .95f);
                tacticalFireLine.endColor = new Color(1f, .56f, .14f, .68f);
                tacticalFireLine.SetPosition(0, localCells[reactor.Position].transform.position + Vector3.up * (CellSurfaceOffset + .38f));
                tacticalFireLine.SetPosition(1, localCells[tacticalUnitState.Position].transform.position + Vector3.up * (CellSurfaceOffset + .38f));
                tacticalContactViews[reactor.Id].CueReactionSource();
                tacticalUnit.CueIncomingFire(TacticalFireOutcome.Suppressed);
                tacticalSymbolView.FlashIcon(IncomingFireFlashColor(TacticalFireOutcome.Suppressed));
                tacticalMiniatureView.FlashTint(IncomingFireFlashColor(TacticalFireOutcome.Suppressed));
                cameraFocus = Vector3.Lerp(tacticalContactViews[reactor.Id].transform.position, tacticalUnit.transform.position, .5f);
                cameraDistance = 13f;
                ApplyCamera();
                StartCoroutine(CaptureReactionScreenshotWhenRequested());
            }
            if (automatedEnemyTurnCapture)
            {
                EnterTacticalMap(FindHighReliefOperationalCell(), false);
                fastEnemyAnimation = false;
                EndTacticalTurn();
                StartCoroutine(CaptureEnemyTurnScreenshotWhenRequested());
            }
            if (automatedResultCapture)
            {
                EnterTacticalMap(FindHighReliefOperationalCell(), false);
                fastEnemyAnimation = true;
                tacticalObjective.TurnLimit = 1;
                EndTacticalTurn();
                StartCoroutine(CaptureResultScreenshotWhenRequested());
            }
            if (automatedReconCapture)
            {
                EnterTacticalMap(FindHighReliefOperationalCell(), false);
                FrameObservationContacts();
                BeginTacticalReconPlanning();
                TryIssueTacticalRecon(tacticalEnemyStates[0].Position);
                StartCoroutine(CaptureReconScreenshotWhenRequested());
            }
            // Requires --battle-request=<path> alongside --campaign-capture-path=;
            // the request load above already enters the tactical map and starts
            // the briefing fade, so this only needs to wait and snap.
            if (automatedCampaignCapture) StartCoroutine(CaptureCampaignScreenshotWhenRequested());
            if (automatedSaveRestoreRegression) StartCoroutine(RunSaveRestoreRegression());
            if (automatedSaveRestoreCapture) StartCoroutine(RunSaveRestoreCapture());
            if (automatedFeedbackRegression) StartCoroutine(RunFeedbackRegression());
            if (automatedScenarioRegression) StartCoroutine(RunScenarioRegression());
            if (automatedBattalionRegression) StartCoroutine(RunBattalionRegression());
            if (automatedSettingsRegression) StartCoroutine(RunSettingsRegression());
            if (automatedCardsRegression) StartCoroutine(RunCardsRegression());
            if (automatedPlaReconRegression) StartCoroutine(RunPlaReconRegression());
            if (automatedPlaReconCapture) StartCoroutine(CapturePlaReconScreenshotWhenRequested());
            if (automatedOperationalScenarioRegression) StartCoroutine(RunOperationalScenarioRegression());
            if (automatedOperationalScenarioCapture) StartCoroutine(CaptureOperationalScenarioWhenRequested());
            StartCoroutine(CaptureScreenshotWhenRequested());
        }

        // True for any headless/scripted regression or capture flag. Used both
        // to gate the normal per-frame input loop and to suppress presentation
        // side effects (like the standalone scenario briefing toast) that would
        // otherwise stall or corrupt an automated run that drives tactical
        // actions immediately after EnterTacticalMap returns.
        private bool IsAutomatedRun()
            => automatedCapture || automatedMovementRegression || automatedTacticalRegression || automatedTacticalMovementRegression ||
               automatedLosRegression || automatedObservationRegression || automatedFireRegression || automatedSuppressionRegression ||
               automatedReactionRegression || automatedEnemyTurnRegression || automatedCoverRegression || automatedVictoryRegression ||
               automatedReconRegression || automatedBattleContractRegression || automatedTacticalCapture || automatedLosCapture ||
               automatedObservationCapture || automatedFireCapture || automatedSuppressionCapture || automatedReactionCapture ||
               automatedEnemyTurnCapture || automatedResultCapture || automatedReconCapture || automatedCampaignCapture ||
               automatedSaveRestoreRegression || automatedSaveRestoreCapture || automatedFeedbackRegression || automatedScenarioRegression ||
               automatedBattalionRegression || automatedSettingsRegression || automatedCardsRegression || automatedPlaReconRegression ||
               automatedPlaReconCapture || automatedOperationalScenarioRegression || automatedOperationalScenarioCapture;

        private void Update()
        {
            if (IsAutomatedRun() || mapTransitionActive) return;
            UpdateCamera();
            if (tacticalMode) UpdateTacticalPointer();
            else UpdatePointer();
        }

        private void LoadGeography()
        {
            TextAsset elevationAsset = Resources.Load<TextAsset>("Geography/taiwan-etopo-2022");
            if (!GeographicElevationGrid.TryLoad(elevationAsset, out elevation, out string error)) Debug.LogWarning(error);
            coastline = CoastlineData.Load(Resources.Load<TextAsset>("Geography/taiwan-coastline"));
        }

        private void BuildLightingAndCamera()
        {
            RenderSettings.ambientLight = new Color(.42f, .46f, .40f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.fog = false;
            RenderSettings.fogColor = new Color(.15f, .22f, .23f);
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 210f;
            RenderSettings.fogEndDistance = 520f;

            GameObject lightObject = new GameObject("Command Table Sun");
            Light sun = lightObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, .93f, .78f);
            sun.intensity = 1.15f;
            lightObject.transform.rotation = Quaternion.Euler(48f, -35f, 0f);

            GameObject fillObject = new GameObject("Cool Fill Light");
            Light fill = fillObject.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(.35f, .56f, .62f);
            fill.intensity = .38f;
            fillObject.transform.rotation = Quaternion.Euler(62f, 145f, 0f);

            GameObject cameraObject = new GameObject("Map Camera");
            mapCamera = cameraObject.AddComponent<Camera>();
            mapCamera.clearFlags = CameraClearFlags.SolidColor;
            mapCamera.backgroundColor = new Color(.055f, .09f, .10f);
            mapCamera.nearClipPlane = .1f;
            mapCamera.farClipPlane = 700f;
            mapCamera.fieldOfView = 38f;
            NatoSymbolView.ActiveCamera = cameraObject.transform;
            cameraObject.AddComponent<AudioListener>();
            tacticalAudio = cameraObject.AddComponent<TacticalAudio>();
            tacticalAudio.Initialize();
            cameraFocus = HexToWorld(new HexCoord(Width / 2, Height / 2));
            ApplyCamera();
        }

        private void BuildCommandTable()
        {
            Vector3 first = HexToWorld(new HexCoord(0, 0));
            Vector3 last = HexToWorld(new HexCoord(Width - 1, Height - 1));
            GameObject table = GameObject.CreatePrimitive(PrimitiveType.Cube);
            table.name = "Recessed Command Table";
            table.transform.SetParent(overviewRoot.transform, false);
            table.transform.position = (first + last) * .5f + Vector3.down * .27f;
            table.transform.localScale = new Vector3(last.x - first.x + 4f, .42f, last.z - first.z + 4f);
            MeshRenderer tableRenderer = table.GetComponent<MeshRenderer>();
            tableRenderer.sharedMaterial = NewMaterial(new Color(.045f, .065f, .062f));
            tableRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            tableRenderer.receiveShadows = false;
            Destroy(table.GetComponent<Collider>());
        }

        private void BuildBoard()
        {
            Mesh sharedHexMesh = CreateHexMesh(HexRadius * .99f, .10f);
            hexTopNormalY = sharedHexMesh.normals[0].y;
            Material sharedHexMaterial = NewMaterial(Color.white);
            for (int q = 0; q < Width; q++)
            {
                for (int r = 0; r < Height; r++)
                {
                    var coord = new HexCoord(q, r);
                    Vector3 center = HexToWorld(coord);
                    HexToGeographic(coord, out double longitude, out double latitude);
                    float measuredElevation = elevation != null ? elevation.SampleMetres(longitude, latitude) : 0f;
                    bool isLand = coastline == null || coastline.ContainsLand(longitude, latitude);
                    TacticalTerrain terrain = ClassifyTerrain(isLand, measuredElevation);
                    if (isLand)
                    {
                        landCellCount++;
                        maximumLandElevation = Mathf.Max(maximumLandElevation, measuredElevation);
                    }
                    else waterCellCount++;
                    center.y = isLand ? Mathf.Clamp(measuredElevation, 0f, 4000f) * .00072f : 0f;

                    GameObject cellObject = new GameObject("Hex " + coord);
                    cellObject.transform.SetParent(overviewRoot.transform, false);
                    cellObject.transform.position = center;
                    cellObject.AddComponent<MeshFilter>().sharedMesh = sharedHexMesh;
                    var renderer = cellObject.AddComponent<MeshRenderer>();
                    renderer.sharedMaterial = sharedHexMaterial;
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                    Color color = isLand ? LandColor(measuredElevation) : WaterColor(measuredElevation);
                    HexCellView view = cellObject.AddComponent<HexCellView>();
                    view.Initialize(coord, terrain, measuredElevation, longitude, latitude, renderer, color);
                    cells.Add(coord, view);
                    board.Add(coord, new TacticalCell(coord, terrain));
                }
            }
            BuildCoastAccents();
            BuildGeographicLabels();
            BuildPathLine();
            BuildOccupiedHexRing();
            Debug.Log($"Built Taiwan whole-island board: {cells.Count} hexes, {landCellCount} land, {waterCellCount} water, maximum sampled land elevation {maximumLandElevation:0} m.");
        }

        private void BuildUnit()
        {
            operationalFriendlyBattalions.Clear();
            operationalFriendlyBattalions.AddRange(operationalScenario.FriendlyBattalions);
            operationalEnemyBattalions.Clear();
            operationalEnemyBattalions.AddRange(operationalScenario.EnemyBattalions);
            operationalContacts.Clear();
            foreach (OperationalContactState contact in operationalScenario.EnemyContacts)
                operationalContacts[contact.TargetId] = contact;
            operationalReconMarkers.Clear();
            operationalReconMarkers.AddRange(operationalScenario.ReconMarkers);

            foreach (OperationalBattalionState battalion in operationalFriendlyBattalions)
            {
                TacticalUnitState state = OperationalUnitState(battalion);
                UnitCounterView view = BuildOperationalFriendlyCounter(battalion, state);
                operationalUnitStates[battalion.Id] = state;
                operationalFriendlyViews[battalion.Id] = view;
                operationalFriendlyByView[view] = battalion;
            }

            activeOperationalBattalion = operationalFriendlyBattalions[0];
            unitState = operationalUnitStates[activeOperationalBattalion.Id];
            unit = operationalFriendlyViews[activeOperationalBattalion.Id];
            turnState = new TacticalTurnState { TurnNumber = operationalScenario.TurnNumber, ActiveSide = "USMC" };
            UpdateOccupiedHexRing(unitState.Position);

            foreach (OperationalBattalionState enemy in operationalEnemyBattalions)
            {
                GameObject markerObject = new GameObject("Operational contact " + enemy.Id);
                markerObject.transform.SetParent(overviewRoot.transform, false);
                ContactMarkerView marker = markerObject.AddComponent<ContactMarkerView>();
                marker.Initialize("BN");
                operationalEnemyViews[enemy.Id] = marker;
            }
            foreach (OperationalReconMarker marker in operationalReconMarkers) BuildOperationalReconRing(marker.Hex);
            BuildOperationalObjectiveMarker();
            RefreshOperationalObservation();
        }

        private UnitCounterView BuildOperationalFriendlyCounter(OperationalBattalionState battalion, TacticalUnitState state)
        {
            Vector3 position = cells[battalion.Position].transform.position;
            GameObject counterRoot = new GameObject(battalion.DisplayName);
            counterRoot.transform.SetParent(overviewRoot.transform, false);
            counterRoot.transform.position = position + Vector3.up * (CellSurfaceOffset + CounterClearance);
            SphereCollider counterCollider = counterRoot.AddComponent<SphereCollider>();
            counterCollider.radius = .65f;
            counterCollider.center = Vector3.up * .12f;
            UnitCounterView view = counterRoot.AddComponent<UnitCounterView>();
            view.Initialize(battalion.DisplayName);

            GameObject baseObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseObject.name = "Counter Base";
            baseObject.transform.SetParent(counterRoot.transform, false);
            baseObject.transform.localPosition = Vector3.up * .10f;
            baseObject.transform.localScale = new Vector3(.66f, .10f, .66f);
            MeshRenderer baseRenderer = baseObject.GetComponent<MeshRenderer>();
            baseRenderer.sharedMaterial = NewOverlayMaterial(new Color(.13f, .25f, .20f));
            baseRenderer.sortingOrder = 60;
            Destroy(baseObject.GetComponent<Collider>());

            GameObject face = GameObject.CreatePrimitive(PrimitiveType.Cube);
            face.name = "Counter Face";
            face.transform.SetParent(counterRoot.transform, false);
            face.transform.localPosition = Vector3.up * .23f;
            face.transform.localScale = new Vector3(.94f, .08f, .68f);
            MeshRenderer faceRenderer = face.GetComponent<MeshRenderer>();
            faceRenderer.sharedMaterial = NewOverlayMaterial(new Color(.76f, .72f, .55f));
            faceRenderer.sortingOrder = 62;
            Destroy(face.GetComponent<Collider>());
            view.BindRenderers(baseRenderer, faceRenderer);
            view.Present(state);

            GameObject symbolObject = new GameObject("Unit Label");
            symbolObject.transform.SetParent(counterRoot.transform, false);
            symbolObject.transform.localPosition = Vector3.up * .276f;
            symbolObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            symbolObject.transform.localScale = Vector3.one * .20f;
            TextMesh label = symbolObject.AddComponent<TextMesh>();
            label.text = battalion.ShortName + "\nINF BN";
            label.alignment = TextAlignment.Center;
            label.anchor = TextAnchor.MiddleCenter;
            label.fontSize = 42;
            label.characterSize = .10f;
            label.color = new Color(.08f, .12f, .10f);
            BuildInfantrySymbol(counterRoot.transform);
            return view;
        }

        private static TacticalUnitState OperationalUnitState(OperationalBattalionState battalion)
        {
            return new TacticalUnitState(battalion.Id, battalion.DisplayName, battalion.Position, battalion.MaximumActionPoints)
            {
                RemainingActionPoints = battalion.RemainingActionPoints,
                Readiness = battalion.Readiness,
                IsSelected = battalion.IsSelected
            };
        }

        private void CompleteSmokeTestWhenRequested()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--smoke-test") < 0) return;
            if (cells.Count != Width * Height || board.Count != Width * Height || unit == null || elevation == null || coastline == null ||
                Resources.Load<Shader>("Shaders/MapTerrain") == null || hexTopNormalY < .99f ||
                landCellCount < 1000 || waterCellCount < 1000 || maximumLandElevation < 3000f)
            {
                Debug.LogError($"ALWAYS_FAITHFUL_SMOKE_FAILED cells={cells.Count}, board={board.Count}, land={landCellCount}, water={waterCellCount}, maximumElevation={maximumLandElevation:0}, topNormalY={hexTopNormalY:0.000}, terrainShader={Resources.Load<Shader>("Shaders/MapTerrain") != null}, unit={unit != null}, elevation={elevation != null}, coastline={coastline != null}");
                Application.Quit(1);
                return;
            }
            if (unitState.IsSelected || movePlanning || counterMenuOpen || reachable.Count != 0)
            {
                Debug.LogError("ALWAYS_FAITHFUL_SMOKE_FAILED prototype did not start neutral");
                Application.Quit(1);
                return;
            }
            if (!ValidateGeographyInspection(out string geographyFailure))
            {
                Debug.LogError($"ALWAYS_FAITHFUL_SMOKE_FAILED geography inspection {geographyFailure}");
                Application.Quit(1);
                return;
            }
            OpenCounterMenu(new Vector2(640f, 360f));
            if (!counterMenuOpen || movePlanning || reachable.Count != 0)
            {
                Debug.LogError("ALWAYS_FAITHFUL_SMOKE_FAILED counter menu state");
                Application.Quit(1);
                return;
            }
            BeginMovePlanning();
            if (reachable.Count < 2 || MovementPlanner.FindPath(board, unitState.Position, unitState.Position, unitState.RemainingActionPoints).Count != 1)
            {
                Debug.LogError($"ALWAYS_FAITHFUL_SMOKE_FAILED movement reachable={reachable.Count}");
                Application.Quit(1);
                return;
            }
            if (!ValidateMovementPicking(out string pickFailure))
            {
                Debug.LogError($"ALWAYS_FAITHFUL_SMOKE_FAILED screen picking {pickFailure}");
                Application.Quit(1);
                return;
            }
            DisplayCapturePath();
            bool overlaysIgnorePointer = pathMarkers.Count > 0;
            foreach (GameObject marker in pathMarkers) overlaysIgnorePointer &= marker.layer == LayerMask.NameToLayer("Ignore Raycast");
            if (!overlaysIgnorePointer)
            {
                Debug.LogError($"ALWAYS_FAITHFUL_SMOKE_FAILED path overlays markers={pathMarkers.Count}");
                Application.Quit(1);
                return;
            }
            if (!ValidateAuthoritativeState(out string stateFailure))
            {
                Debug.LogError($"ALWAYS_FAITHFUL_SMOKE_FAILED authoritative state {stateFailure}");
                Application.Quit(1);
                return;
            }
            Debug.Log($"ALWAYS_FAITHFUL_SMOKE_OK cells={cells.Count}, land={landCellCount}, water={waterCellCount}, maximumElevation={maximumLandElevation:0}m, topNormalY={hexTopNormalY:0.000}, reachable={reachable.Count}, pathMarkers={pathMarkers.Count}, unit={unitState.DisplayName}, hex={unitState.Position}, ap={unitState.RemainingActionPoints}/{unitState.MaximumActionPoints}, turn={turnState.TurnNumber}, scale={MapHexKilometres:0.#}km");
            Application.Quit(0);
        }

        private IEnumerator CaptureScreenshotWhenRequested()
        {
            string argument = Array.Find(Environment.GetCommandLineArgs(), value => value.StartsWith("--capture-path=", StringComparison.Ordinal));
            if (argument == null) yield break;
            string path = argument.Substring("--capture-path=".Length);
            yield return new WaitForSecondsRealtime(1f);
            var target = new RenderTexture(1280, 720, 24);
            var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active;
            RenderTexture cameraTarget = mapCamera.targetTexture;
            mapCamera.targetTexture = target;
            mapCamera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0f, 0f, target.width, target.height), 0, 0);
            image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
            mapCamera.targetTexture = cameraTarget;
            RenderTexture.active = previous;
            Destroy(target);
            Destroy(image);
            Debug.Log($"ALWAYS_FAITHFUL_CAPTURED {path} camera={mapCamera.transform.position} focus={cameraFocus} unit={unit.transform.position} screen={mapCamera.WorldToScreenPoint(unit.transform.position)} renderers={unit.GetComponentsInChildren<Renderer>().Length}");
            Application.Quit(0);
        }

        private IEnumerator CaptureTacticalScreenshotWhenRequested()
        {
            string argument = Array.Find(Environment.GetCommandLineArgs(), value => value.StartsWith("--tactical-capture-path=", StringComparison.Ordinal));
            if (argument == null) yield break;
            string path = argument.Substring("--tactical-capture-path=".Length);
            yield return new WaitForSecondsRealtime(1f);
            var target = new RenderTexture(1280, 720, 24);
            var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active;
            mapCamera.targetTexture = target;
            mapCamera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0f, 0f, target.width, target.height), 0, 0);
            image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
            mapCamera.targetTexture = null;
            RenderTexture.active = previous;
            Destroy(target);
            Destroy(image);
            Debug.Log($"ALWAYS_FAITHFUL_TACTICAL_CAPTURED {path} battlefield={tacticalBattlefield.BattlefieldId} cells={localCells.Count}");
            Application.Quit(0);
        }

        private IEnumerator CaptureLosScreenshotWhenRequested()
        {
            string argument = Array.Find(Environment.GetCommandLineArgs(), value => value.StartsWith("--los-capture-path=", StringComparison.Ordinal));
            if (argument == null) yield break;
            string path = argument.Substring("--los-capture-path=".Length);
            yield return new WaitForSecondsRealtime(1f);
            var target = new RenderTexture(1280, 720, 24);
            var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active;
            mapCamera.targetTexture = target;
            mapCamera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0f, 0f, target.width, target.height), 0, 0);
            image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
            mapCamera.targetTexture = null;
            RenderTexture.active = previous;
            Destroy(target);
            Destroy(image);
            Debug.Log($"ALWAYS_FAITHFUL_LOS_CAPTURED {path} state={tacticalLosResult?.State} segments={tacticalLosSegments.Count}");
            Application.Quit(0);
        }

        private IEnumerator CaptureObservationScreenshotWhenRequested()
        {
            string argument = Array.Find(Environment.GetCommandLineArgs(), value => value.StartsWith("--observation-capture-path=", StringComparison.Ordinal));
            if (argument == null) yield break;
            string path = argument.Substring("--observation-capture-path=".Length);
            yield return new WaitForSecondsRealtime(1f);
            var target = new RenderTexture(1280, 720, 24);
            var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active;
            mapCamera.targetTexture = target;
            mapCamera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0f, 0f, target.width, target.height), 0, 0);
            image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
            mapCamera.targetTexture = null;
            RenderTexture.active = previous;
            Destroy(target);
            Destroy(image);
            Debug.Log($"ALWAYS_FAITHFUL_OBSERVATION_CAPTURED {path} contacts={tacticalContacts.Count}");
            Application.Quit(0);
        }

        private IEnumerator CaptureFireScreenshotWhenRequested()
        {
            string argument = Array.Find(Environment.GetCommandLineArgs(), value => value.StartsWith("--fire-capture-path=", StringComparison.Ordinal));
            if (argument == null) yield break;
            string path = argument.Substring("--fire-capture-path=".Length);
            yield return new WaitForSecondsRealtime(1f);
            var target = new RenderTexture(1280, 720, 24);
            var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active;
            mapCamera.targetTexture = target;
            mapCamera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0f, 0f, target.width, target.height), 0, 0);
            image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
            mapCamera.targetTexture = null;
            RenderTexture.active = previous;
            Destroy(target);
            Destroy(image);
            Debug.Log($"ALWAYS_FAITHFUL_FIRE_CAPTURED {path} valid={tacticalFirePreview?.IsValid} chance={tacticalFirePreview?.HitChance}");
            Application.Quit(0);
        }

        private IEnumerator CaptureSuppressionScreenshotWhenRequested()
        {
            string argument = Array.Find(Environment.GetCommandLineArgs(), value => value.StartsWith("--suppression-capture-path=", StringComparison.Ordinal));
            if (argument == null) yield break;
            string path = argument.Substring("--suppression-capture-path=".Length);
            yield return new WaitForSecondsRealtime(1f);
            var target = new RenderTexture(1280, 720, 24);
            var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active;
            mapCamera.targetTexture = target;
            mapCamera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0f, 0f, target.width, target.height), 0, 0);
            image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
            mapCamera.targetTexture = null;
            RenderTexture.active = previous;
            Destroy(target);
            Destroy(image);
            Debug.Log($"ALWAYS_FAITHFUL_SUPPRESSION_CAPTURED {path} status={tacticalEnemyStates[0].CombatStatus} points={tacticalEnemyStates[0].SuppressionPoints}");
            Application.Quit(0);
        }

        private IEnumerator CaptureReactionScreenshotWhenRequested()
        {
            string argument = Array.Find(Environment.GetCommandLineArgs(), value => value.StartsWith("--reaction-capture-path=", StringComparison.Ordinal));
            if (argument == null) yield break;
            string path = argument.Substring("--reaction-capture-path=".Length);
            yield return new WaitForSecondsRealtime(1f);
            var target = new RenderTexture(1280, 720, 24);
            var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active;
            mapCamera.targetTexture = target;
            mapCamera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0f, 0f, target.width, target.height), 0, 0);
            image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
            mapCamera.targetTexture = null;
            RenderTexture.active = previous;
            Destroy(target);
            Destroy(image);
            Debug.Log($"ALWAYS_FAITHFUL_REACTION_CAPTURED {path} reactor={tacticalEnemyStates[0].Id}");
            Application.Quit(0);
        }

        private IEnumerator CaptureEnemyTurnScreenshotWhenRequested()
        {
            string argument = Array.Find(Environment.GetCommandLineArgs(), value => value.StartsWith("--enemy-turn-capture-path=", StringComparison.Ordinal));
            if (argument == null) yield break;
            string path = argument.Substring("--enemy-turn-capture-path=".Length);
            yield return new WaitForSecondsRealtime(.85f);
            var target = new RenderTexture(1280, 720, 24);
            var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active;
            mapCamera.targetTexture = target;
            mapCamera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0f, 0f, target.width, target.height), 0, 0);
            image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
            mapCamera.targetTexture = null;
            RenderTexture.active = previous;
            Destroy(target);
            Destroy(image);
            Debug.Log($"ALWAYS_FAITHFUL_ENEMY_TURN_CAPTURED {path} active={tacticalEnemyTurnActive} activity={tacticalEnemyActivityText}");
            Application.Quit(0);
        }

        private IEnumerator CaptureResultScreenshotWhenRequested()
        {
            string argument = Array.Find(Environment.GetCommandLineArgs(), value => value.StartsWith("--result-capture-path=", StringComparison.Ordinal));
            if (argument == null) yield break;
            string path = argument.Substring("--result-capture-path=".Length);
            float deadline = Time.realtimeSinceStartup + 5f;
            do { yield return null; } while ((tacticalEnemyTurnActive || !resultScreenActive || resultScreenOpacity < .98f) && Time.realtimeSinceStartup < deadline);
            var target = new RenderTexture(1280, 720, 24);
            var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active;
            mapCamera.targetTexture = target;
            mapCamera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0f, 0f, target.width, target.height), 0, 0);
            image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
            mapCamera.targetTexture = null;
            RenderTexture.active = previous;
            Destroy(target);
            Destroy(image);
            Debug.Log($"ALWAYS_FAITHFUL_RESULT_CAPTURED {path} outcome={tacticalObjective?.Outcome} posture={tacticalObjective?.Posture}");
            Application.Quit(0);
        }

        private IEnumerator CaptureReconScreenshotWhenRequested()
        {
            string argument = Array.Find(Environment.GetCommandLineArgs(), value => value.StartsWith("--recon-capture-path=", StringComparison.Ordinal));
            if (argument == null) yield break;
            string path = argument.Substring("--recon-capture-path=".Length);
            yield return new WaitForSecondsRealtime(.5f);
            var target = new RenderTexture(1280, 720, 24);
            var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active;
            mapCamera.targetTexture = target;
            mapCamera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0f, 0f, target.width, target.height), 0, 0);
            image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
            mapCamera.targetTexture = null;
            RenderTexture.active = previous;
            Destroy(target);
            Destroy(image);
            Debug.Log($"ALWAYS_FAITHFUL_RECON_CAPTURED {path} markers={tacticalReconMarkers.Count}");
            Application.Quit(0);
        }

        private IEnumerator CapturePlaReconScreenshotWhenRequested()
        {
            string argument = Array.Find(Environment.GetCommandLineArgs(), value => value.StartsWith("--pla-recon-capture-path=", StringComparison.Ordinal));
            if (argument == null) yield break;
            string path = argument.Substring("--pla-recon-capture-path=".Length);

            yield return null;
            EnterTacticalMap(FindHighReliefOperationalCell(), false);
            fastEnemyAnimation = true;
            tacticalObjective.TurnLimit = (turnState.TurnNumber - tacticalObjective.BattleStartTurn) + 20;
            if (!TryManufactureLostContact(tacticalEnemyStates[0], out _))
            {
                Debug.LogError("ALWAYS_FAITHFUL_PLA_RECON_CAPTURE_FAILED could not create lost-contact fixture");
                Application.Quit(1);
                yield break;
            }
            EndTacticalTurn();
            float deadline = Time.realtimeSinceStartup + 5f;
            do { yield return null; } while (tacticalEnemyTurnActive && Time.realtimeSinceStartup < deadline);
            yield return new WaitForSecondsRealtime(.25f);

            var target = new RenderTexture(1280, 720, 24);
            var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active;
            mapCamera.targetTexture = target;
            mapCamera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0f, 0f, target.width, target.height), 0, 0);
            image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
            mapCamera.targetTexture = null;
            RenderTexture.active = previous;
            Destroy(target);
            Destroy(image);
            Debug.Log($"ALWAYS_FAITHFUL_PLA_RECON_CAPTURED {path} markers={tacticalPlaReconMarkers.Count} visibleRings={tacticalPlaReconRings.Count}");
            Application.Quit(0);
        }

        private IEnumerator CaptureCampaignScreenshotWhenRequested()
        {
            string argument = Array.Find(Environment.GetCommandLineArgs(), value => value.StartsWith("--campaign-capture-path=", StringComparison.Ordinal));
            if (argument == null) yield break;
            string path = argument.Substring("--campaign-capture-path=".Length);
            float deadline = Time.realtimeSinceStartup + 5f;
            do { yield return null; } while (!(campaignBriefingActive && campaignBriefingOpacity >= .98f) && Time.realtimeSinceStartup < deadline);
            var target = new RenderTexture(1280, 720, 24);
            var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active;
            mapCamera.targetTexture = target;
            mapCamera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0f, 0f, target.width, target.height), 0, 0);
            image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
            mapCamera.targetTexture = null;
            RenderTexture.active = previous;
            Destroy(target);
            Destroy(image);
            Debug.Log($"ALWAYS_FAITHFUL_CAMPAIGN_CAPTURED {path} requestId={activeBattleRequest?.RequestId} error={battleRequestError}");
            Application.Quit(0);
        }

        private void RequestTacticalMap(HexCellView parentCell, bool hasKnownEnemyContact = true)
        {
            if (parentCell == null || !parentCell.IsLand || mapTransitionActive) return;
            tacticalAudio.Play(TacticalSound.MapTransition);
            StartCoroutine(TransitionToTactical(parentCell, hasKnownEnemyContact));
        }

        private IEnumerator TransitionToTactical(HexCellView parentCell, bool hasKnownEnemyContact = true)
        {
            mapTransitionActive = true;
            yield return FadeMapTransition(0f, 1f, .22f);
            EnterTacticalMap(parentCell, false, null, hasKnownEnemyContact);
            yield return FadeMapTransition(1f, 0f, .36f);
            mapTransitionActive = false;
        }

        private IEnumerator TransitionToOverview()
        {
            mapTransitionActive = true;
            yield return FadeMapTransition(0f, 1f, .22f);
            ReturnToIsland(false);
            yield return FadeMapTransition(1f, 0f, .36f);
            mapTransitionActive = false;
        }

        private IEnumerator FadeMapTransition(float from, float to, float duration)
        {
            for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                mapTransitionOpacity = Mathf.SmoothStep(from, to, elapsed / duration);
                yield return null;
            }
            mapTransitionOpacity = to;
        }

        private void TryLoadBattleRequest(string path)
        {
            battleRequestError = null;
            activeBattleRequest = null;
            if (!File.Exists(path))
            {
                ShowBattleRequestError($"Battle request file not found: {path}");
                return;
            }
            string json;
            try
            {
                json = File.ReadAllText(path);
            }
            catch (Exception exception)
            {
                ShowBattleRequestError("Could not read battle request file: " + exception.Message);
                return;
            }
            BattleRequest request;
            try
            {
                request = JsonUtility.FromJson<BattleRequest>(json);
            }
            catch (Exception exception)
            {
                ShowBattleRequestError("Battle request file is not valid JSON: " + exception.Message);
                return;
            }
            if (!TacticalBattleContract.ValidateStructure(request, out string structureError))
            {
                ShowBattleRequestError(structureError);
                return;
            }
            if (!cells.TryGetValue(request.TheaterHex, out HexCellView theaterCell) || !theaterCell.IsLand)
            {
                ShowBattleRequestError($"Theater hex {request.TheaterHex} is out of bounds or not land");
                return;
            }

            activeBattleRequest = request;
            campaignBriefingActive = true;
            StartCoroutine(ShowCampaignBriefingThenDismiss());
            EnterTacticalMap(theaterCell, false);
            Debug.Log($"ALWAYS_FAITHFUL_BATTLE_REQUEST_LOADED requestId={request.RequestId} campaignId={request.CampaignId} seed={request.Seed} theater={request.TheaterHex} outputPath={request.OutputPath}");
        }

        private void ShowBattleRequestError(string message, string title = "BATTLE REQUEST REJECTED")
        {
            battleRequestError = message;
            campaignErrorTitle = title;
            campaignBriefingActive = true;
            StartCoroutine(FadeCampaignBriefing(0f, 1f, .35f));
            tacticalAudio.Play(TacticalSound.OrderCancel);
            Debug.LogWarning("ALWAYS_FAITHFUL_BATTLE_REQUEST_REJECTED " + message);
        }

        private IEnumerator ShowCampaignBriefingThenDismiss()
        {
            yield return FadeCampaignBriefing(0f, 1f, .35f);
            yield return new WaitForSecondsRealtime(2.2f);
            yield return FadeCampaignBriefing(1f, 0f, .35f);
            campaignBriefingActive = false;
        }

        private IEnumerator FadeCampaignBriefing(float from, float to, float duration)
        {
            for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                campaignBriefingOpacity = Mathf.SmoothStep(from, to, elapsed / duration);
                yield return null;
            }
            campaignBriefingOpacity = to;
        }

        private void DismissCampaignBriefing()
        {
            campaignBriefingActive = false;
            campaignBriefingOpacity = 0f;
            if (battleRequestError != null)
            {
                battleRequestError = null;
                activeBattleRequest = null;
            }
        }

        // Pass argument null for "skip"; otherwise a card already present in
        // battalionStatus.Hand. Either way, the normal mission briefing still
        // plays afterward exactly as it would with an empty hand.
        private void DismissSupportCardModal(TacticalSupportCard playedCard)
        {
            if (playedCard != null)
            {
                battalionStatus.Hand.Remove(playedCard);
                string effectSummary = ApplySupportCardEffect(playedCard.AssetType);
                tacticalBattlefield.SupportCardEvents.Add(new TacticalSupportCardEvent
                {
                    Sequence = ++tacticalEventSequence,
                    BattlefieldId = tacticalBattlefield.BattlefieldId,
                    Turn = turnState.TurnNumber,
                    AssetType = playedCard.AssetType,
                    Summary = $"{TacticalSupportCardCatalog.DisplayName(playedCard.AssetType)} committed — {effectSummary}"
                });
                SaveBattalionStatus();
                Debug.Log($"ALWAYS_FAITHFUL_SUPPORT_CARD_PLAYED type={playedCard.AssetType}");
            }
            supportCardModalActive = false;
            campaignBriefingActive = true;
            StartCoroutine(ShowCampaignBriefingThenDismiss());
        }

        // Whole-battle pre-battle effects. Called only after BuildTacticalBattlefield
        // has already constructed tacticalObjective/tacticalEnemyStates/tacticalUnitState,
        // so each effect safely mutates already-built in-memory battle state.
        // Returns a short human-readable summary of what the effect actually did,
        // for the event log this card play produces.
        private string ApplySupportCardEffect(TacticalSupportAssetType assetType)
        {
            switch (assetType)
            {
                case TacticalSupportAssetType.Isr:
                    tacticalBattlefield.IsrCardActive = true;
                    return "enemy detection elevated for the battle.";
                case TacticalSupportAssetType.FireSupport:
                    int suppressedCount = 0;
                    foreach (TacticalUnitState enemy in tacticalEnemyStates)
                    {
                        if (HexCoord.Distance(enemy.Position, tacticalObjective.ObjectiveHex) > TacticalSupportCards.FireSupportRadiusHexes) continue;
                        TacticalSuppressionEvent suppressionEvent = TacticalSuppression.ApplyFireOutcome(enemy, TacticalFireOutcome.Suppressed);
                        if (suppressionEvent == null) continue;
                        suppressionEvent.Cause = "SupportCard:FireSupport";
                        suppressionEvent.Sequence = ++tacticalEventSequence;
                        suppressionEvent.BattlefieldId = tacticalBattlefield.BattlefieldId;
                        suppressionEvent.Turn = turnState.TurnNumber;
                        tacticalBattlefield.SuppressionEvents.Add(suppressionEvent);
                        suppressedCount++;
                    }
                    return suppressedCount > 0
                        ? $"{suppressedCount} enemy unit(s) suppressed near the objective."
                        : "no enemies were within range of the objective.";
                default:
                    tacticalUnitState.MaximumActionPoints += TacticalSupportCards.ReserveActionPointBonus;
                    tacticalUnitState.RemainingActionPoints += TacticalSupportCards.ReserveActionPointBonus;
                    return $"+{TacticalSupportCards.ReserveActionPointBonus} action points this battle.";
            }
        }

        private void SaveTacticalBattle(string outputPath = null)
        {
            outputPath ??= saveFilePath;
            if (tacticalBattlefield == null || tacticalUnitState == null) return;
            var state = new TacticalBattleSaveState
            {
                SavedAtUtc = DateTime.UtcNow.ToString("o"),
                Battlefield = tacticalBattlefield,
                Turn = turnState,
                UsmcUnit = tacticalUnitState,
                UsmcWeapon = tacticalWeapon,
                EventSequence = tacticalEventSequence,
                HasActiveBattleRequest = activeBattleRequest != null,
                ActiveBattleRequest = activeBattleRequest
            };
            state.EnemyUnits.AddRange(tacticalEnemyStates);
            foreach (KeyValuePair<string, TacticalWeaponState> pair in tacticalEnemyWeapons)
                state.EnemyWeapons.Add(new TacticalEnemyWeaponEntry { UnitId = pair.Key, Weapon = pair.Value });
            foreach (TacticalContactState contact in tacticalContacts.Values)
                state.Contacts.Add(contact);
            foreach (TacticalContactState contact in tacticalEnemyContacts.Values)
                state.EnemyContacts.Add(contact);

            string tempPath = outputPath + ".tmp";
            try
            {
                string directory = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                File.WriteAllText(tempPath, JsonUtility.ToJson(state, true));
                if (File.Exists(outputPath)) File.Delete(outputPath);
                File.Move(tempPath, outputPath);
                if (outputPath == saveFilePath) hasSavedBattle = true;
                tacticalOrderFeedback = "BATTLE SAVED";
                tacticalAudio.Play(TacticalSound.OrderConfirm);
                Debug.Log($"ALWAYS_FAITHFUL_BATTLE_SAVED path={outputPath} battlefield={tacticalBattlefield.BattlefieldId} turn={turnState.TurnNumber} sequence={tacticalEventSequence}");
            }
            catch (Exception exception)
            {
                tacticalOrderFeedback = "SAVE FAILED";
                tacticalAudio.Play(TacticalSound.OrderCancel);
                Debug.LogError($"ALWAYS_FAITHFUL_BATTLE_SAVE_FAILED path={outputPath} error={exception.Message}");
            }
        }

        private bool TryLoadTacticalBattle(string path, out string error)
        {
            error = null;
            if (!File.Exists(path))
            {
                error = "Save file not found: " + path;
                return false;
            }
            TacticalBattleSaveState state;
            try
            {
                state = JsonUtility.FromJson<TacticalBattleSaveState>(File.ReadAllText(path));
            }
            catch (Exception exception)
            {
                error = "Save file is not valid JSON: " + exception.Message;
                return false;
            }
            if (!TacticalBattleSave.Validate(state, out string structureError))
            {
                error = structureError;
                return false;
            }
            if (!cells.TryGetValue(state.Battlefield.ParentHex, out HexCellView parentCell))
            {
                error = $"Saved parent hex {state.Battlefield.ParentHex} is out of bounds";
                return false;
            }

            tacticalAudio.Play(TacticalSound.MapTransition);
            EnterTacticalMap(parentCell, false, state);
            Debug.Log($"ALWAYS_FAITHFUL_BATTLE_RESTORED path={path} battlefield={state.Battlefield.BattlefieldId} turn={state.Turn.TurnNumber}");
            return true;
        }

        private void TryContinueSavedBattle()
        {
            if (!TryLoadTacticalBattle(saveFilePath, out string error)) ShowBattleRequestError(error, "SAVE LOAD FAILED");
        }

        private bool TryLoadBattalionStatus(string path, out TacticalBattalionStatus status)
        {
            status = null;
            if (!File.Exists(path)) return false;
            TacticalBattalionStatus parsed;
            try
            {
                parsed = JsonUtility.FromJson<TacticalBattalionStatus>(File.ReadAllText(path));
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"ALWAYS_FAITHFUL_BATTALION_STATUS_LOAD_FAILED path={path} error={exception.Message}");
                return false;
            }
            if (!TacticalBattalion.Validate(parsed, out string error))
            {
                Debug.LogWarning($"ALWAYS_FAITHFUL_BATTALION_STATUS_LOAD_FAILED path={path} error={error}");
                return false;
            }
            status = parsed;
            return true;
        }

        private void SaveBattalionStatus()
        {
            if (battalionStatus == null) return;
            string tempPath = battalionStatusPath + ".tmp";
            try
            {
                string directory = Path.GetDirectoryName(battalionStatusPath);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                File.WriteAllText(tempPath, JsonUtility.ToJson(battalionStatus, true));
                if (File.Exists(battalionStatusPath)) File.Delete(battalionStatusPath);
                File.Move(tempPath, battalionStatusPath);
            }
            catch (Exception exception)
            {
                Debug.LogError($"ALWAYS_FAITHFUL_BATTALION_STATUS_SAVE_FAILED path={battalionStatusPath} error={exception.Message}");
            }
        }

        private OperationalScenarioState CreateOperationalScenario()
        {
            var used = new HashSet<HexCoord>();
            HexCoord objective = FindDistinctDeploymentHex(new HexCoord(28, 34), used);
            var scenario = new OperationalScenarioState
            {
                PrimaryObjective = objective,
                Situation = "PLA amphibious forces have established dispersed lodgments in southern Taiwan. Civilian movement and broken terrain have degraded the common operating picture; enemy battalion positions are not shown until detected.",
                Mission = $"4th Marine Regiment Task Force secures the island approaches and denies the PLA the key junction at {objective}. Preserve combat power while locating the opposing battalions.",
                Execution = "Maneuver either rifle battalion, task reconnaissance against suspected approaches, and resolve close contacts on the 250 m tactical map. Enemy formations move during the PLA phase and may disappear into stale contact memory.",
                IntelligenceEstimate = "Three PLA battalion-sized formations are assessed in the area: two amphibious combined-arms battalions and one reconnaissance battalion. Exact locations and strength remain unconfirmed. Passive detection improves inside 8 operational hexes; identification inside 5; observation inside 2."
            };
            scenario.FriendlyBattalions.Add(CreateOperationalBattalion("usmc-bn-1-4", "1st Battalion, 4th Marines", "1/4", OperationalSide.Usmc,
                FindDistinctDeploymentHex(new HexCoord(21, 25), used), objective,
                "Alpha Company", "Bravo Company", "Charlie Company", "Weapons Company"));
            scenario.FriendlyBattalions.Add(CreateOperationalBattalion("usmc-bn-2-4", "2d Battalion, 4th Marines", "2/4", OperationalSide.Usmc,
                FindDistinctDeploymentHex(new HexCoord(18, 34), used), objective,
                "Echo Company", "Fox Company", "Golf Company", "Weapons Company"));
            // Preserve the lightweight campaign progress that predates the full
            // operational layer by carrying it into the lead battalion once.
            if (operationalPersistenceEnabled && battalionStatus != null)
                scenario.FriendlyBattalions[0].Strength = battalionStatus.Strength;
            scenario.EnemyBattalions.Add(CreateOperationalBattalion("pla-amphib-bn-1", "PLA 1st Amphibious Combined-Arms Battalion", "1 ACB", OperationalSide.Pla,
                FindDistinctDeploymentHex(new HexCoord(38, 52), used), objective,
                "Three maneuver companies", "Firepower company", "Service support company"));
            scenario.EnemyBattalions.Add(CreateOperationalBattalion("pla-amphib-bn-2", "PLA 2d Amphibious Combined-Arms Battalion", "2 ACB", OperationalSide.Pla,
                FindDistinctDeploymentHex(new HexCoord(43, 42), used), objective,
                "Three maneuver companies", "Firepower company", "Service support company"));
            scenario.EnemyBattalions.Add(CreateOperationalBattalion("pla-recon-bn", "PLA Reconnaissance Battalion", "RECON", OperationalSide.Pla,
                FindDistinctDeploymentHex(new HexCoord(35, 65), used), objective,
                "Reconnaissance companies", "UAS detachment", "Support company"));
            foreach (OperationalBattalionState enemy in scenario.EnemyBattalions)
                scenario.EnemyContacts.Add(new OperationalContactState { TargetId = enemy.Id, State = TacticalVisibilityState.Hidden });
            return scenario;
        }

        private static OperationalBattalionState CreateOperationalBattalion(string id, string displayName, string shortName,
            OperationalSide side, HexCoord position, HexCoord objective, params string[] subordinateUnits)
        {
            return new OperationalBattalionState
            {
                Id = id,
                DisplayName = displayName,
                ShortName = shortName,
                Side = side,
                Position = position,
                ObjectiveHex = objective,
                SubordinateUnits = new List<string>(subordinateUnits)
            };
        }

        private HexCoord FindDistinctDeploymentHex(HexCoord preferred, HashSet<HexCoord> used)
        {
            HexCoord best = default;
            int bestDistance = int.MaxValue;
            foreach (HexCellView candidate in cells.Values)
            {
                if (!candidate.IsLand || used.Contains(candidate.Coord)) continue;
                int distance = HexCoord.Distance(preferred, candidate.Coord);
                if (distance >= bestDistance) continue;
                best = candidate.Coord;
                bestDistance = distance;
            }
            used.Add(best);
            return best;
        }

        private bool TryLoadOperationalScenario(string path, out OperationalScenarioState scenario)
        {
            scenario = null;
            if (!File.Exists(path)) return false;
            try
            {
                OperationalScenarioState parsed = JsonUtility.FromJson<OperationalScenarioState>(File.ReadAllText(path));
                if (!OperationalScenarioRules.Validate(parsed, out string error))
                {
                    Debug.LogWarning($"ALWAYS_FAITHFUL_OPERATIONAL_LOAD_FAILED path={path} error={error}");
                    return false;
                }
                foreach (OperationalBattalionState battalion in parsed.FriendlyBattalions)
                    if (!cells.ContainsKey(battalion.Position)) return false;
                foreach (OperationalBattalionState battalion in parsed.EnemyBattalions)
                    if (!cells.ContainsKey(battalion.Position)) return false;
                scenario = parsed;
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"ALWAYS_FAITHFUL_OPERATIONAL_LOAD_FAILED path={path} error={exception.Message}");
                return false;
            }
        }

        private void SaveOperationalScenario()
        {
            if (!operationalPersistenceEnabled || operationalScenario == null) return;
            operationalScenario.TurnNumber = turnState?.TurnNumber ?? operationalScenario.TurnNumber;
            operationalScenario.EnemyContacts = new List<OperationalContactState>(operationalContacts.Values);
            operationalScenario.ReconMarkers = new List<OperationalReconMarker>(operationalReconMarkers);
            string tempPath = operationalScenarioPath + ".tmp";
            try
            {
                string directory = Path.GetDirectoryName(operationalScenarioPath);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                File.WriteAllText(tempPath, JsonUtility.ToJson(operationalScenario, true));
                if (File.Exists(operationalScenarioPath)) File.Delete(operationalScenarioPath);
                File.Move(tempPath, operationalScenarioPath);
            }
            catch (Exception exception)
            {
                Debug.LogError($"ALWAYS_FAITHFUL_OPERATIONAL_SAVE_FAILED path={operationalScenarioPath} error={exception.Message}");
            }
        }

        private void SyncOperationalFromUnit(TacticalUnitState source)
        {
            OperationalBattalionState battalion = operationalFriendlyBattalions.Find(candidate => candidate.Id == source.Id);
            if (battalion == null) return;
            battalion.Position = source.Position;
            battalion.MaximumActionPoints = source.MaximumActionPoints;
            battalion.RemainingActionPoints = source.RemainingActionPoints;
            battalion.Readiness = source.Readiness;
            battalion.IsSelected = source.IsSelected;
        }

        private void ResetBattalionStatus()
        {
            battalionStatus = TacticalBattalion.CreateFresh();
            SaveBattalionStatus();
            ResetOperationalScenario();
            tacticalOrderFeedback = "CAMPAIGN RESET";
            tacticalAudio.Play(TacticalSound.OrderConfirm);
            Debug.Log("ALWAYS_FAITHFUL_BATTALION_STATUS_RESET");
        }

        private void ResetOperationalScenario()
        {
            CancelUnitInteraction();
            foreach (UnitCounterView view in operationalFriendlyViews.Values)
                if (view != null) Destroy(view.gameObject);
            foreach (ContactMarkerView view in operationalEnemyViews.Values)
                if (view != null) Destroy(view.gameObject);
            foreach (LineRenderer ring in operationalReconRings.Values)
                if (ring != null) Destroy(ring.gameObject);
            if (operationalObjectiveRing != null) Destroy(operationalObjectiveRing.gameObject);
            operationalObjectiveRing = null;
            operationalFriendlyViews.Clear();
            operationalFriendlyByView.Clear();
            operationalUnitStates.Clear();
            operationalEnemyViews.Clear();
            operationalReconRings.Clear();
            operationalScenario = CreateOperationalScenario();
            BuildUnit();
            SaveOperationalScenario();
            operationalBriefingActive = true;
            operationalBriefingIntelPage = false;
        }

        private bool TryLoadSettings(string path, out AlwaysFaithfulSettings loaded)
        {
            loaded = null;
            if (!File.Exists(path)) return false;
            AlwaysFaithfulSettings parsed;
            try
            {
                parsed = JsonUtility.FromJson<AlwaysFaithfulSettings>(File.ReadAllText(path));
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"ALWAYS_FAITHFUL_SETTINGS_LOAD_FAILED path={path} error={exception.Message}");
                return false;
            }
            if (!AlwaysFaithfulSettingsRules.Validate(parsed, out string error))
            {
                Debug.LogWarning($"ALWAYS_FAITHFUL_SETTINGS_LOAD_FAILED path={path} error={error}");
                return false;
            }
            loaded = parsed;
            return true;
        }

        private void SaveSettings()
        {
            if (settings == null) return;
            string tempPath = settingsPath + ".tmp";
            try
            {
                string directory = Path.GetDirectoryName(settingsPath);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                File.WriteAllText(tempPath, JsonUtility.ToJson(settings, true));
                if (File.Exists(settingsPath)) File.Delete(settingsPath);
                File.Move(tempPath, settingsPath);
                Debug.Log($"ALWAYS_FAITHFUL_SETTINGS_SAVED path={settingsPath} uiScale={settings.UiScale} cancel={settings.RemapCancelKey} resetCamera={settings.RemapResetCameraKey} graphics={settings.GraphicsPreset} colorSafe={settings.ColorSafePalette} reducedMotion={settings.ReducedMotion} animSpeed={settings.AnimationSpeed} counterSkin={settings.CounterSkin}");
            }
            catch (Exception exception)
            {
                Debug.LogError($"ALWAYS_FAITHFUL_SETTINGS_SAVE_FAILED path={settingsPath} error={exception.Message}");
            }
        }

        private void EnterTacticalMap(HexCellView parentCell, bool animate, TacticalBattleSaveState restore = null, bool hasKnownEnemyContact = true)
        {
            if (animate)
            {
                RequestTacticalMap(parentCell, hasKnownEnemyContact);
                return;
            }
            if (!tacticalMode)
            {
                overviewCameraFocus = cameraFocus;
                overviewCameraDistance = cameraDistance;
                overviewCameraYaw = cameraYaw;
                overviewCameraPitch = cameraPitch;
            }
            CancelUnitInteraction();
            tacticalLosOverlayActive = false;
            tacticalLosOverlayCells.Clear();

            if (activeBattleRequest == null && activeOperationalBattalion != null && battalionStatus != null)
            {
                battalionStatus.BattalionName = activeOperationalBattalion.DisplayName;
                battalionStatus.Strength = activeOperationalBattalion.Strength;
            }

            // A brand-new standalone battle (no BattleRequest, no save restore,
            // and not simply re-entering a still-in-progress battle at the same
            // hex) is this codebase's one deliberate exception to total
            // determinism: roll a fresh scenario seed, so cover, posture,
            // objective siting, turn limit, and enemy roster all vary battle to
            // battle. Resuming an in-progress battle at the same hex must reuse
            // the seed already in memory, or a Return-to-Island/re-enter round
            // trip would silently reroll a live battle out from under the player.
            bool sameHexInMemory = restore == null && tacticalBattlefield != null && tacticalBattlefield.ParentHex.Equals(parentCell.Coord);
            bool resumingInProgress = sameHexInMemory && activeBattleRequest == null &&
                tacticalObjective != null && tacticalObjective.Outcome == TacticalBattleOutcome.InProgress;
            bool freshStandaloneScenario = restore == null && activeBattleRequest == null && !resumingInProgress;
            if (freshStandaloneScenario)
                standaloneScenarioSeed = scenarioSeedOverride ?? UnityEngine.Random.Range(1, int.MaxValue);

            string requestedId = TacticalBattlefieldExtractor.BuildBattlefieldId(parentCell.Coord, activeBattleRequest?.Seed ?? standaloneScenarioSeed);
            if (restore != null || tacticalBattlefield == null || tacticalBattlefield.BattlefieldId != requestedId)
                BuildTacticalBattlefield(parentCell, restore, hasKnownEnemyContact);
            tacticalMode = true;
            overviewRoot.SetActive(false);
            tacticalRoot.SetActive(true);
            cameraFocus = Vector3.zero;
            cameraDistance = 33f;
            cameraYaw = 0f;
            cameraPitch = TacticalDefaultPitch;
            hoveredLocalCell = null;
            ApplyCamera();
            // Announce mission type / Battalion status the same way a
            // BattleRequest launch already announces posture/objective/turn
            // limit — but never during an automated regression/capture run,
            // which would otherwise stall on the ~2.9s fade-in/hold/fade-out.
            // A non-empty support-card hand gets first say: the player may
            // commit one card for a whole-battle effect before the usual
            // mission briefing plays.
            if (freshStandaloneScenario && !IsAutomatedRun())
            {
                if (battalionStatus != null && battalionStatus.Hand.Count > 0)
                {
                    supportCardModalActive = true;
                }
                else
                {
                    campaignBriefingActive = true;
                    StartCoroutine(ShowCampaignBriefingThenDismiss());
                }
            }
            Debug.Log($"ALWAYS_FAITHFUL_TACTICAL_ENTER battlefield={tacticalBattlefield.BattlefieldId} parent={tacticalBattlefield.ParentHex} center={tacticalBattlefield.CenterLatitude:0.00000},{tacticalBattlefield.CenterLongitude:0.00000}");
        }

        private void ReturnToIsland(bool animate)
        {
            if (!tacticalMode || tacticalUnitMoving || tacticalFireResolving || tacticalEnemyTurnActive || mapTransitionActive && animate) return;
            if (animate)
            {
                tacticalAudio.Play(TacticalSound.MapTransition);
                StartCoroutine(TransitionToOverview());
                return;
            }
            if (hoveredLocalCell != null) hoveredLocalCell.SetHighlighted(false);
            hoveredLocalCell = null;
            CancelTacticalInteraction(false);
            resultScreenActive = false;
            resultScreenOpacity = 0f;
            campaignBriefingActive = false;
            campaignBriefingOpacity = 0f;
            supportCardModalActive = false;
            lastBattleResultPath = null;
            tacticalRoot.SetActive(false);
            overviewRoot.SetActive(true);
            tacticalMode = false;
            turnState.TurnNumber = operationalScenario.TurnNumber;
            turnState.ActiveSide = "USMC";
            cameraFocus = overviewCameraFocus;
            cameraDistance = overviewCameraDistance;
            cameraYaw = overviewCameraYaw;
            cameraPitch = overviewCameraPitch;
            ApplyCamera();
            Debug.Log($"ALWAYS_FAITHFUL_TACTICAL_EXIT battlefield={tacticalBattlefield.BattlefieldId} parent={tacticalBattlefield.ParentHex}");
        }

        private void BuildTacticalBattlefield(HexCellView parentCell, TacticalBattleSaveState restore = null, bool hasKnownEnemyContact = true)
        {
            if (tacticalRoot != null) Destroy(tacticalRoot);
            localCells.Clear();
            localMovementBoard.Clear();
            tacticalReconMarkers.Clear();
            tacticalReconRings.Clear();
            activeBattleRequest = restore != null ? (restore.HasActiveBattleRequest ? restore.ActiveBattleRequest : null) : activeBattleRequest;
            tacticalEventSequence = restore?.EventSequence ?? 0;
            tacticalBattlefield = restore?.Battlefield ?? TacticalBattlefieldExtractor.Extract(
                parentCell.Coord,
                parentCell.Longitude,
                parentCell.Latitude,
                (longitude, latitude) => elevation.SampleMetres(longitude, latitude),
                (longitude, latitude) => coastline.ContainsLand(longitude, latitude),
                activeBattleRequest?.Seed ?? standaloneScenarioSeed);
            // Resync so EnterTacticalMap's next same-hex comparison never
            // spuriously mismatches (covers restore, request-driven, and
            // freshly-rolled cases alike).
            standaloneScenarioSeed = tacticalBattlefield.Seed;
            if (restore != null)
            {
                turnState.TurnNumber = restore.Turn.TurnNumber;
                turnState.ActiveSide = restore.Turn.ActiveSide;
            }
            tacticalReconMarkers.AddRange(tacticalBattlefield.ActiveReconMarkers);
            tacticalPlaReconMarkers.AddRange(tacticalBattlefield.ActivePlaReconMarkers);
            tacticalRoot = new GameObject("Tactical Battlefield " + tacticalBattlefield.BattlefieldId);
            tacticalRoot.transform.SetParent(transform, false);

            localMinimumLandElevation = float.MaxValue;
            localMaximumLandElevation = float.MinValue;
            foreach (TacticalBattlefieldCell cell in tacticalBattlefield.Cells)
            {
                if (cell.Terrain == TacticalTerrain.Water) continue;
                localMinimumLandElevation = Mathf.Min(localMinimumLandElevation, cell.ElevationMetres);
                localMaximumLandElevation = Mathf.Max(localMaximumLandElevation, cell.ElevationMetres);
            }
            if (localMinimumLandElevation == float.MaxValue) localMinimumLandElevation = 0f;
            if (localMaximumLandElevation == float.MinValue) localMaximumLandElevation = 0f;

            Mesh sharedMesh = CreateHexMesh(HexRadius * .985f, .12f);
            Material sharedMaterial = NewMaterial(Color.white);
            sharedMaterial.SetFloat("_EdgeStrength", TacticalEdgeStrength());
            sharedMaterial.SetFloat("_Atmosphere", TacticalAtmosphereStrength());
            Material sharedWaterMaterial = NewWaterMaterial();
            sharedWaterMaterial.SetFloat("_EdgeStrength", TacticalEdgeStrength() * .70f);
            sharedWaterMaterial.SetFloat("_Atmosphere", TacticalAtmosphereStrength());
            Shader coverShader = Resources.Load<Shader>("Shaders/MapProp") ?? Resources.Load<Shader>("Shaders/MapSolid") ?? Resources.Load<Shader>("Shaders/MapOverlay") ?? Shader.Find("Sprites/Default");
            foreach (TacticalBattlefieldCell source in tacticalBattlefield.Cells)
            {
                Vector3 position = LocalHexToWorld(source.LocalCoord);
                position.y = source.Terrain == TacticalTerrain.Water
                    ? 0f
                    : .08f + Mathf.Max(0f, source.ElevationMetres - localMinimumLandElevation) * LocalReliefScale;
                GameObject cellObject = new GameObject("Local Hex " + source.LocalCoord);
                cellObject.transform.SetParent(tacticalRoot.transform, false);
                cellObject.transform.position = position;
                cellObject.AddComponent<MeshFilter>().sharedMesh = sharedMesh;
                MeshRenderer renderer = cellObject.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = source.Terrain == TacticalTerrain.Water ? sharedWaterMaterial : sharedMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                Color color = source.Terrain == TacticalTerrain.Water ? WaterColor(source.ElevationMetres) : LocalLandColor(source.ElevationMetres);
                color = CoverTint(color, source.Cover);
                HexCellView view = cellObject.AddComponent<HexCellView>();
                view.Initialize(source.LocalCoord, source.Terrain, source.ElevationMetres, source.Longitude, source.Latitude, renderer, color, source.Cover, source.IsBuiltUp);
                localCells.Add(source.LocalCoord, view);
                localMovementBoard.Add(source.LocalCoord, new TacticalMovementCell
                {
                    Coord = source.LocalCoord,
                    Terrain = source.Terrain,
                    ElevationMetres = source.ElevationMetres,
                    Cover = source.Cover
                });
                TacticalCoverView.Build(cellObject.transform, source.LocalCoord, source.Terrain, source.Cover, source.IsBuiltUp, coverShader, settings.GraphicsPreset);
            }
            foreach (TacticalReconMarker marker in tacticalReconMarkers) BuildTacticalReconRing(marker.Hex, tacticalReconRings, TacticalReconRingColor);
            foreach (TacticalReconMarker marker in tacticalPlaReconMarkers)
                if (marker.IsVisibleToUsmc) BuildTacticalReconRing(marker.Hex, tacticalPlaReconRings, PlaReconRingColor);
            BuildTacticalTable();
            BuildTacticalShoreline();
            BuildTacticalReferenceMarks();
            BuildTacticalUnit(restore);
            BuildTacticalMovementVisuals();
            BuildTacticalLosVisuals();
            BuildTacticalFireVisuals();
            BuildTacticalContacts(restore, hasKnownEnemyContact);
            ApplyCounterSkin();
            BuildTacticalObjective(restore);
            RefreshTacticalObservation();
            tacticalOrderFeedback = restore != null ? "BATTLE RESTORED • RMB platoon for orders" : "RMB platoon for tactical orders";
            if (restore != null)
                Debug.Log($"ALWAYS_FAITHFUL_TACTICAL_RESTORED battlefield={tacticalBattlefield.BattlefieldId} turn={turnState.TurnNumber} sequence={tacticalEventSequence}");
            Debug.Log($"Built tactical battlefield {tacticalBattlefield.BattlefieldId}: {localCells.Count} cells at {tacticalBattlefield.CellSizeMetres} m, relief {localMinimumLandElevation:0}-{localMaximumLandElevation:0} m.");
        }

        private void BuildTacticalTable()
        {
            GameObject table = GameObject.CreatePrimitive(PrimitiveType.Cube);
            table.name = "Tactical Map Table";
            table.transform.SetParent(tacticalRoot.transform, false);
            table.transform.position = Vector3.down * .30f;
            table.transform.localScale = new Vector3(tacticalBattlefield.Width * 1.55f, .45f, tacticalBattlefield.Height * 1.82f);
            table.GetComponent<MeshRenderer>().sharedMaterial = NewMaterial(new Color(.035f, .055f, .052f));
            Destroy(table.GetComponent<Collider>());
        }

        private void BuildTacticalShoreline()
        {
            foreach (KeyValuePair<HexCoord, HexCellView> pair in localCells)
            {
                if (!pair.Value.IsLand) continue;
                bool coastal = false;
                foreach (HexCoord neighbor in MovementPlanner.Neighbors(pair.Key))
                    if (localCells.TryGetValue(neighbor, out HexCellView adjacent) && !adjacent.IsLand) coastal = true;
                if (!coastal) continue;
                GameObject rimObject = new GameObject("Shore " + pair.Key);
                rimObject.layer = LayerMask.NameToLayer("Ignore Raycast");
                rimObject.transform.SetParent(tacticalRoot.transform, false);
                LineRenderer rim = rimObject.AddComponent<LineRenderer>();
                rim.loop = true;
                rim.useWorldSpace = true;
                rim.positionCount = 6;
                rim.widthMultiplier = .055f;
                rim.material = NewOverlayMaterial(new Color(.50f, .88f, .76f, .72f));
                rim.startColor = new Color(.50f, .88f, .76f, .72f);
                rim.endColor = rim.startColor;
                for (int corner = 0; corner < 6; corner++)
                {
                    float angle = corner * Mathf.PI / 3f;
                    rim.SetPosition(corner, pair.Value.transform.position + new Vector3(Mathf.Cos(angle) * .96f, CellSurfaceOffset + .03f, Mathf.Sin(angle) * .96f));
                }
            }
        }

        private void BuildTacticalReferenceMarks()
        {
            HexCoord northCoord = new HexCoord(tacticalBattlefield.Width - 2, tacticalBattlefield.Height - 2);
            GameObject northObject = new GameObject("North Reference");
            northObject.transform.SetParent(tacticalRoot.transform, false);
            northObject.transform.position = localCells[northCoord].transform.position + Vector3.up * .34f;
            northObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            TextMesh north = northObject.AddComponent<TextMesh>();
            north.text = "N\n▲";
            north.alignment = TextAlignment.Center;
            north.anchor = TextAnchor.MiddleCenter;
            north.fontSize = 48;
            north.characterSize = .10f;
            north.color = new Color(.91f, .86f, .67f, .92f);
        }

        private void BuildTacticalObjective(TacticalBattleSaveState restore = null)
        {
            if (restore != null)
            {
                tacticalObjective = tacticalBattlefield.Objective;
                BuildTacticalObjectiveMarker(tacticalObjective.ObjectiveHex);
                HexCoord restoreCenter = new HexCoord(tacticalBattlefield.Width / 2, tacticalBattlefield.Height / 2);
                if (!tacticalObjective.ObjectiveHex.Equals(restoreCenter)) BuildTacticalDeploymentZone(restoreCenter);
                Debug.Log($"ALWAYS_FAITHFUL_OBJECTIVE_RESTORED battlefield={tacticalBattlefield.BattlefieldId} mission={tacticalObjective.MissionType} hex={tacticalObjective.ObjectiveHex} turnLimit={tacticalObjective.TurnLimit} outcome={tacticalObjective.Outcome}");
                return;
            }
            // An imported BattleRequest's overrides take priority over both the
            // --posture=/--turn-limit= dev flags and the deterministic defaults;
            // request-driven launch reuses this exact setup path, not a parallel one.
            string[] args = Environment.GetCommandLineArgs();
            TacticalPosture posture;
            if (activeBattleRequest != null && activeBattleRequest.HasPostureOverride)
            {
                posture = activeBattleRequest.Posture;
            }
            else
            {
                string postureArgument = Array.Find(args, value => value.StartsWith("--posture=", StringComparison.Ordinal));
                string postureValue = postureArgument?.Substring("--posture=".Length);
                posture = string.Equals(postureValue, "defend", StringComparison.OrdinalIgnoreCase)
                    ? TacticalPosture.Defend
                    : string.Equals(postureValue, "attack", StringComparison.OrdinalIgnoreCase)
                        ? TacticalPosture.Attack
                        : TacticalVictory.ChoosePosture(tacticalBattlefield.BattlefieldId);
            }

            int turnLimit;
            if (activeBattleRequest != null && activeBattleRequest.TurnLimitOverride > 0)
            {
                turnLimit = activeBattleRequest.TurnLimitOverride;
            }
            else
            {
                string turnLimitArgument = Array.Find(args, value => value.StartsWith("--turn-limit=", StringComparison.Ordinal));
                if (turnLimitArgument != null && int.TryParse(turnLimitArgument.Substring("--turn-limit=".Length), out int parsedLimit) && parsedLimit > 0)
                    turnLimit = parsedLimit;
                else if (activeBattleRequest != null)
                    turnLimit = TacticalVictory.DefaultTurnLimit; // BattleRequest path: contract-frozen, never varied
                else
                    turnLimit = TacticalScenario.ChooseTurnLimit(tacticalBattlefield.BattlefieldId);
            }

            // Contract-frozen: a BattleRequest-driven battle always keeps a plain
            // Attack/Defend mission type mirroring Posture; only standalone play
            // rolls (or lets --mission=/--posture= force) the richer set.
            TacticalMissionType missionType;
            if (activeBattleRequest != null)
            {
                missionType = posture == TacticalPosture.Attack ? TacticalMissionType.Attack : TacticalMissionType.Defend;
            }
            else
            {
                string missionArgument = Array.Find(args, value => value.StartsWith("--mission=", StringComparison.Ordinal));
                bool postureArgPresent = Array.Exists(args, value => value.StartsWith("--posture=", StringComparison.Ordinal));
                if (TryParseMissionTypeArgument(missionArgument, out TacticalMissionType parsedMission))
                    missionType = parsedMission;
                else if (postureArgPresent)
                    missionType = posture == TacticalPosture.Attack ? TacticalMissionType.Attack : TacticalMissionType.Defend;
                else
                    missionType = TacticalScenario.ChooseMissionType(tacticalBattlefield.BattlefieldId);
                posture = TacticalVictory.SitingShapeFor(missionType);
            }

            var enemyStarts = new List<HexCoord>();
            foreach (TacticalUnitState enemy in tacticalEnemyStates) enemyStarts.Add(enemy.Position);
            HexCoord center = new HexCoord(tacticalBattlefield.Width / 2, tacticalBattlefield.Height / 2);
            HexCoord objectiveHex = activeBattleRequest != null && activeBattleRequest.HasObjectiveOverride
                ? activeBattleRequest.ObjectiveHexOverride
                : TacticalVictory.ChooseObjective(localMovementBoard, center, posture, enemyStarts, tacticalBattlefield.BattlefieldId);

            tacticalObjective = new TacticalObjectiveState
            {
                ObjectiveHex = objectiveHex,
                Posture = posture,
                MissionType = missionType,
                TurnLimit = turnLimit,
                BattleStartTurn = turnState.TurnNumber
            };
            tacticalBattlefield.Objective = tacticalObjective;
            BuildTacticalObjectiveMarker(objectiveHex);
            // Only draw a separate deployment-zone ring when it wouldn't just sit
            // on top of the objective marker (Defend's objective is the deployment
            // hex itself, so the objective ring already communicates that setup).
            if (!objectiveHex.Equals(center)) BuildTacticalDeploymentZone(center);
            Debug.Log($"ALWAYS_FAITHFUL_OBJECTIVE_SET battlefield={tacticalBattlefield.BattlefieldId} mission={missionType} hex={objectiveHex} turnLimit={turnLimit} startTurn={tacticalObjective.BattleStartTurn}");
        }

        private static bool TryParseMissionTypeArgument(string argument, out TacticalMissionType missionType)
        {
            missionType = TacticalMissionType.Attack;
            if (argument == null) return false;
            string value = argument.Substring("--mission=".Length);
            if (string.Equals(value, "attack", StringComparison.OrdinalIgnoreCase)) { missionType = TacticalMissionType.Attack; return true; }
            if (string.Equals(value, "defend", StringComparison.OrdinalIgnoreCase)) { missionType = TacticalMissionType.Defend; return true; }
            if (string.Equals(value, "raid", StringComparison.OrdinalIgnoreCase)) { missionType = TacticalMissionType.Raid; return true; }
            if (string.Equals(value, "recon", StringComparison.OrdinalIgnoreCase)) { missionType = TacticalMissionType.ReconInForce; return true; }
            if (string.Equals(value, "withdrawal", StringComparison.OrdinalIgnoreCase)) { missionType = TacticalMissionType.Withdrawal; return true; }
            return false;
        }

        private void BuildTacticalDeploymentZone(HexCoord hex)
        {
            HexCellView zoneCell = localCells[hex];
            GameObject zoneObject = new GameObject("Deployment Zone");
            zoneObject.layer = LayerMask.NameToLayer("Ignore Raycast");
            zoneObject.transform.SetParent(tacticalRoot.transform, false);
            LineRenderer ring = zoneObject.AddComponent<LineRenderer>();
            ring.loop = true;
            ring.useWorldSpace = true;
            ring.positionCount = 48;
            ring.widthMultiplier = .06f;
            Color color = new Color(.36f, .86f, .96f, .72f);
            ring.material = NewOverlayMaterial(color);
            ring.startColor = color;
            ring.endColor = color;
            for (int index = 0; index < ring.positionCount; index++)
            {
                float angle = index / (float)ring.positionCount * Mathf.PI * 2f;
                ring.SetPosition(index, zoneCell.transform.position + new Vector3(Mathf.Cos(angle) * .70f, CellSurfaceOffset + .07f, Mathf.Sin(angle) * .70f));
            }
        }

        private void BuildTacticalObjectiveMarker(HexCoord hex)
        {
            tacticalObjectiveControlledByUsmc = null;
            HexCellView markerCell = localCells[hex];
            GameObject ringObject = new GameObject("Objective Marker");
            ringObject.layer = LayerMask.NameToLayer("Ignore Raycast");
            ringObject.transform.SetParent(tacticalRoot.transform, false);
            LineRenderer ring = ringObject.AddComponent<LineRenderer>();
            ring.loop = true;
            ring.useWorldSpace = true;
            ring.positionCount = 48;
            ring.widthMultiplier = .075f;
            ring.material = NewOverlayMaterial(Color.white);
            for (int index = 0; index < ring.positionCount; index++)
            {
                float angle = index / (float)ring.positionCount * Mathf.PI * 2f;
                ring.SetPosition(index, markerCell.transform.position + new Vector3(Mathf.Cos(angle) * .54f, CellSurfaceOffset + .09f, Mathf.Sin(angle) * .54f));
            }
            tacticalObjectiveRing = ring;
            UpdateObjectiveMarkerColor();
        }

        private void UpdateObjectiveMarkerColor()
        {
            if (tacticalObjectiveRing == null || tacticalObjective == null || tacticalUnitState == null) return;
            bool usmcControls = localMovementBoard.TryGetValue(tacticalObjective.ObjectiveHex, out TacticalMovementCell cell) &&
                cell.OccupantId == tacticalUnitState.Id;
            Color color = usmcControls ? new Color(.32f, .92f, .52f, .94f) : new Color(.98f, .73f, .19f, .92f);
            tacticalObjectiveRing.startColor = color;
            tacticalObjectiveRing.endColor = color;
            if (tacticalObjectiveControlledByUsmc.HasValue && tacticalObjectiveControlledByUsmc.Value != usmcControls)
            {
                var objectiveEvent = new TacticalObjectiveEvent
                {
                    Sequence = ++tacticalEventSequence,
                    BattlefieldId = tacticalBattlefield.BattlefieldId,
                    Turn = turnState.TurnNumber,
                    Hex = tacticalObjective.ObjectiveHex,
                    ControlledByUsmc = usmcControls
                };
                tacticalBattlefield.ObjectiveEvents.Add(objectiveEvent);
                tacticalOrderFeedback = usmcControls ? "OBJECTIVE SECURED" : "OBJECTIVE LOST";
                tacticalAudio.Play(usmcControls ? TacticalSound.OrderConfirm : TacticalSound.OrderCancel);
                Debug.Log($"ALWAYS_FAITHFUL_OBJECTIVE_EVENT sequence={objectiveEvent.Sequence} hex={objectiveEvent.Hex} controlled={objectiveEvent.ControlledByUsmc}");
            }
            tacticalObjectiveControlledByUsmc = usmcControls;
        }

        private void BuildTacticalUnit(TacticalBattleSaveState restore = null)
        {
            HexCoord start = restore != null
                ? restore.UsmcUnit.Position
                : FindLocalDeploymentHex(new HexCoord(tacticalBattlefield.Width / 2, tacticalBattlefield.Height / 2));
            GameObject root = new GameObject("Tactical USMC Rifle Platoon");
            root.transform.SetParent(tacticalRoot.transform, false);
            root.transform.position = LocalCounterPosition(start);
            SphereCollider collider = root.AddComponent<SphereCollider>();
            collider.radius = .65f;
            collider.center = Vector3.up * .12f;
            tacticalUnit = root.AddComponent<UnitCounterView>();
            if (restore != null)
            {
                tacticalUnitState = restore.UsmcUnit;
                tacticalUnitState.IsSelected = false;
                tacticalWeapon = restore.UsmcWeapon;
            }
            else
            {
                string defaultId = activeBattleRequest == null && activeOperationalBattalion != null
                    ? activeOperationalBattalion.Id + "-lead-platoon"
                    : "usmc-rifle-platoon-1";
                string defaultName = activeBattleRequest == null && activeOperationalBattalion != null
                    ? activeOperationalBattalion.ShortName + " Lead Rifle Platoon"
                    : "USMC Rifle Platoon";
                FindForceImport("usmc-rifle-platoon", defaultId, defaultName, out string usmcId, out string usmcDisplayName);
                int startingActionPoints = TacticalPlatoonActionPoints;
                if (activeBattleRequest == null && battalionStatus != null && TacticalBattalion.IsUnderStrength(battalionStatus))
                    startingActionPoints -= TacticalBattalion.ActionPointPenalty;
                tacticalUnitState = new TacticalUnitState(usmcId, usmcDisplayName, start, startingActionPoints);
                tacticalWeapon = new TacticalWeaponState("m27-small-arms", "M27 Small Arms", 6);
            }
            tacticalUnit.Initialize(tacticalUnitState.DisplayName);
            tacticalIllustratedDetail = new GameObject("Illustrated Detail");
            tacticalIllustratedDetail.transform.SetParent(root.transform, false);
            tacticalFormationView = tacticalIllustratedDetail.AddComponent<TacticalFormationView>();
            tacticalFormationView.Initialize(TacticalFormationAffiliation.Usmc, "USMC", "RIFLE PLT");
            tacticalUnit.BindRenderers(tacticalFormationView.CommandDeckRenderer, tacticalFormationView.DesignationRenderer);
            tacticalSymbolDetail = new GameObject("Symbol Detail");
            tacticalSymbolDetail.transform.SetParent(root.transform, false);
            tacticalSymbolView = tacticalSymbolDetail.AddComponent<NatoSymbolView>();
            tacticalSymbolView.Initialize(true, false, "USMC\nRIFLE PLT");
            tacticalSymbolView.BindReactive(tacticalUnitState, new Color(.22f, .55f, .95f), new Color(.22f, .55f, .95f));
            tacticalMiniatureDetail = new GameObject("Miniature Detail");
            tacticalMiniatureDetail.transform.SetParent(root.transform, false);
            tacticalMiniatureView = tacticalMiniatureDetail.AddComponent<MiniatureCounterView>();
            tacticalMiniatureView.Initialize(true, false, "USMC\nRIFLE PLT");
            tacticalMiniatureView.BindReactive(tacticalUnitState);
            tacticalUnit.Present(tacticalUnitState);
            tacticalFormationView.Present(tacticalUnitState);
            ApplyCounterSkin();
            localMovementBoard[tacticalUnitState.Position].OccupantId = tacticalUnitState.Id;
        }

        // Toggles between the illustrated, NATO/MIL-STD-symbol, and
        // miniature skins for the player's own counter and every PLA
        // contact marker; called after any is (re)built and whenever the
        // Settings panel's Counter Skin control changes.
        private void ApplyCounterSkin()
        {
            if (tacticalIllustratedDetail != null) tacticalIllustratedDetail.SetActive(settings.CounterSkin == CounterSkinTier.Illustrated);
            if (tacticalSymbolDetail != null) tacticalSymbolDetail.SetActive(settings.CounterSkin == CounterSkinTier.Symbol);
            if (tacticalMiniatureDetail != null) tacticalMiniatureDetail.SetActive(settings.CounterSkin == CounterSkinTier.Miniature);
            foreach (ContactMarkerView marker in tacticalContactViews.Values) marker.ApplySkin(settings.CounterSkin);
        }

        // Dev-only overrides for visual review captures (e.g. checking
        // close-in terrain/counter detail, or the alternate counter skin);
        // normal play never passes these flags, so it is unaffected.
        private void ApplyDevCaptureOverrides()
        {
            string cameraDistanceArgument = Array.Find(Environment.GetCommandLineArgs(), value => value.StartsWith("--capture-camera-distance=", StringComparison.Ordinal));
            if (cameraDistanceArgument != null && float.TryParse(cameraDistanceArgument.Substring("--capture-camera-distance=".Length), out float overrideDistance))
            {
                cameraDistance = overrideDistance;
                ApplyCamera();
            }
            string cameraYawArgument = Array.Find(Environment.GetCommandLineArgs(), value => value.StartsWith("--capture-camera-yaw=", StringComparison.Ordinal));
            if (cameraYawArgument != null && float.TryParse(cameraYawArgument.Substring("--capture-camera-yaw=".Length), out float overrideYaw))
            {
                cameraYaw = overrideYaw;
                ApplyCamera();
            }
            string cameraPitchArgument = Array.Find(Environment.GetCommandLineArgs(), value => value.StartsWith("--capture-camera-pitch=", StringComparison.Ordinal));
            if (cameraPitchArgument != null && float.TryParse(cameraPitchArgument.Substring("--capture-camera-pitch=".Length), out float overridePitch))
            {
                cameraPitch = Mathf.Clamp(overridePitch, MinCameraPitch, MaxCameraPitch);
                ApplyCamera();
            }
            string counterSkinArgument = Array.Find(Environment.GetCommandLineArgs(), value => value.StartsWith("--capture-counter-skin=", StringComparison.Ordinal));
            if (counterSkinArgument != null)
            {
                string requested = counterSkinArgument.Substring("--capture-counter-skin=".Length);
                settings.CounterSkin = requested.Equals("symbol", StringComparison.OrdinalIgnoreCase) ? CounterSkinTier.Symbol
                    : requested.Equals("miniature", StringComparison.OrdinalIgnoreCase) ? CounterSkinTier.Miniature
                    : CounterSkinTier.Illustrated;
                ApplyCounterSkin();
            }
        }

        // Matches UnitCounterView.CueIncomingFire's own flash mapping so the
        // symbol skin's momentary flash reads the same as the illustrated
        // skin's.
        private static Color IncomingFireFlashColor(TacticalFireOutcome outcome)
            => outcome == TacticalFireOutcome.Hit ? new Color(1f, .20f, .12f, 1f)
                : outcome == TacticalFireOutcome.Suppressed ? new Color(1f, .62f, .14f, 1f) : new Color(.62f, .72f, .68f, 1f);

        // Lets an imported BattleRequest override a fixed roster slot's ID/name
        // without turning this into a data-driven roster (out of scope for the
        // Phase I stub) — falls back to the existing hardcoded defaults.
        private void FindForceImport(string role, string defaultId, string defaultDisplayName, out string id, out string displayName)
        {
            id = defaultId;
            displayName = defaultDisplayName;
            if (activeBattleRequest?.Forces == null) return;
            foreach (BattleUnitImport import in activeBattleRequest.Forces)
            {
                if (import == null || import.Role != role) continue;
                if (!string.IsNullOrWhiteSpace(import.UnitId)) id = import.UnitId;
                if (!string.IsNullOrWhiteSpace(import.DisplayName)) displayName = import.DisplayName;
                return;
            }
        }

        private void BuildTacticalMovementVisuals()
        {
            GameObject routeObject = new GameObject("Tactical Route Ribbon");
            routeObject.layer = LayerMask.NameToLayer("Ignore Raycast");
            routeObject.transform.SetParent(tacticalRoot.transform, false);
            tacticalRouteLine = routeObject.AddComponent<LineRenderer>();
            tacticalRouteLine.useWorldSpace = true;
            tacticalRouteLine.alignment = LineAlignment.View;
            tacticalRouteLine.numCapVertices = 8;
            tacticalRouteLine.numCornerVertices = 8;
            tacticalRouteLine.widthMultiplier = .24f;
            tacticalRouteLine.material = NewOverlayMaterial(new Color(.98f, .69f, .16f, .94f));
            tacticalRouteLine.startColor = new Color(.38f, .96f, .78f, .92f);
            tacticalRouteLine.endColor = new Color(1f, .66f, .13f, 1f);
            tacticalRouteLine.enabled = false;

            tacticalDestinationGhost = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tacticalDestinationGhost.name = "Tactical Destination Ghost";
            tacticalDestinationGhost.layer = LayerMask.NameToLayer("Ignore Raycast");
            tacticalDestinationGhost.transform.SetParent(tacticalRoot.transform, false);
            tacticalDestinationGhost.transform.localScale = new Vector3(.72f, .035f, .72f);
            tacticalGhostRenderer = tacticalDestinationGhost.GetComponent<MeshRenderer>();
            tacticalGhostRenderer.sharedMaterial = NewOverlayMaterial(new Color(.98f, .72f, .17f, .52f));
            Destroy(tacticalDestinationGhost.GetComponent<Collider>());
            tacticalDestinationGhost.SetActive(false);
        }

        private void BuildTacticalLosVisuals()
        {
            tacticalLosSegments.Clear();
            tacticalLosCells.Clear();
            tacticalLosTarget = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tacticalLosTarget.name = "LOS Target Reticle";
            tacticalLosTarget.layer = LayerMask.NameToLayer("Ignore Raycast");
            tacticalLosTarget.transform.SetParent(tacticalRoot.transform, false);
            tacticalLosTarget.transform.localScale = new Vector3(.78f, .025f, .78f);
            tacticalLosTargetRenderer = tacticalLosTarget.GetComponent<MeshRenderer>();
            tacticalLosTargetRenderer.sharedMaterial = NewOverlayMaterial(new Color(.25f, .92f, .79f, .48f));
            Destroy(tacticalLosTarget.GetComponent<Collider>());
            tacticalLosTarget.SetActive(false);
        }

        private void BuildTacticalFireVisuals()
        {
            GameObject lineObject = new GameObject("Direct Fire Tracer");
            lineObject.layer = LayerMask.NameToLayer("Ignore Raycast");
            lineObject.transform.SetParent(tacticalRoot.transform, false);
            tacticalFireLine = lineObject.AddComponent<LineRenderer>();
            tacticalFireLine.useWorldSpace = true;
            tacticalFireLine.positionCount = 2;
            tacticalFireLine.widthMultiplier = .10f;
            tacticalFireLine.numCapVertices = 7;
            tacticalFireLine.material = NewOverlayMaterial(new Color(1f, .72f, .20f, .95f));
            tacticalFireLine.enabled = false;

            tacticalFireReticle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tacticalFireReticle.name = "Direct Fire Target Reticle";
            tacticalFireReticle.layer = LayerMask.NameToLayer("Ignore Raycast");
            tacticalFireReticle.transform.SetParent(tacticalRoot.transform, false);
            tacticalFireReticle.transform.localScale = new Vector3(.86f, .025f, .86f);
            tacticalFireReticleRenderer = tacticalFireReticle.GetComponent<MeshRenderer>();
            tacticalFireReticleRenderer.sharedMaterial = NewOverlayMaterial(new Color(1f, .52f, .14f, .58f));
            Destroy(tacticalFireReticle.GetComponent<Collider>());
            tacticalFireReticle.SetActive(false);
        }

        private void BuildTacticalContacts(TacticalBattleSaveState restore = null, bool hasKnownEnemyContact = true)
        {
            tacticalEnemyStates.Clear();
            tacticalContacts.Clear();
            tacticalEnemyContacts.Clear();
            tacticalContactViews.Clear();
            tacticalEnemyWeapons.Clear();
            var enemyMarkerLabels = new List<string>();
            if (restore != null)
            {
                tacticalEnemyStates.AddRange(restore.EnemyUnits);
                foreach (TacticalEnemyWeaponEntry entry in restore.EnemyWeapons)
                    tacticalEnemyWeapons[entry.UnitId] = entry.Weapon;
                // Pre-seeds "previous" for the RefreshTacticalObservation() call at
                // the end of BuildTacticalBattlefield, which already handles a
                // populated tacticalContacts dictionary correctly with no further
                // restore-specific logic needed.
                foreach (TacticalContactState contact in restore.Contacts)
                    tacticalContacts[contact.TargetId] = contact;
                // Every EnemyContacts entry shares the same TargetId (the platoon)
                // and is differentiated by which PLA unit owns it.
                foreach (TacticalContactState contact in restore.EnemyContacts)
                    tacticalEnemyContacts[contact.ObserverId] = contact;
                // A request-driven restore is always the legacy fixed [rifle,
                // support] pair; a standalone restore's units were always named
                // "pla-rifle-squad-N"/"pla-support-team-N" by the roster branch
                // below (never overridden), so an ID prefix reliably recovers the role.
                for (int index = 0; index < tacticalEnemyStates.Count; index++)
                    enemyMarkerLabels.Add(activeBattleRequest != null
                        ? (index == 1 ? "SUPPORT" : "RIFLE")
                        : (tacticalEnemyStates[index].Id.StartsWith(TacticalScenario.SupportRole + "-", StringComparison.Ordinal) ? "SUPPORT" : "RIFLE"));
            }
            else if (activeBattleRequest != null)
            {
                // Unchanged legacy 2-unit path — the SOU-interop contract stays frozen.
                var occupied = new HashSet<HexCoord> { tacticalUnitState.Position };
                HexCoord riflePosition = FindObservationDeployment(TacticalVisibilityState.Observed, occupied);
                occupied.Add(riflePosition);
                HexCoord supportPosition = FindObservationDeployment(TacticalVisibilityState.Contact, occupied);
                FindForceImport("pla-rifle-squad", "pla-rifle-squad-1", "PLA Rifle Squad", out string rifleId, out string rifleDisplayName);
                FindForceImport("pla-support-team", "pla-support-team-1", "PLA Support Team", out string supportId, out string supportDisplayName);
                tacticalEnemyStates.Add(new TacticalUnitState(rifleId, rifleDisplayName, riflePosition, 4));
                tacticalEnemyStates.Add(new TacticalUnitState(supportId, supportDisplayName, supportPosition, 4));
                foreach (TacticalUnitState enemy in tacticalEnemyStates)
                    tacticalEnemyWeapons.Add(enemy.Id, new TacticalWeaponState(enemy.Id + "-weapon", "Squad Small Arms", 6));
                enemyMarkerLabels.Add("RIFLE");
                enemyMarkerLabels.Add("SUPPORT");
            }
            else
            {
                // Standalone: a hash-driven roster of 1-3 units, first slot always
                // the "main" rifle squad deployed at Observed range, the rest at
                // Contact range, exactly like the legacy path's two
                // FindObservationDeployment calls. Only rolled at all when the
                // operational layer actually has a detected contact here
                // ("RESOLVE CONTACT") -- "OPEN LOCAL MAP" on a hex with no known
                // enemy should show a clear, uncontested local map, not a
                // manufactured encounter.
                var occupied = new HashSet<HexCoord> { tacticalUnitState.Position };
                List<string> roster = hasKnownEnemyContact
                    ? TacticalScenario.ChooseEnemyRoster(tacticalBattlefield.BattlefieldId)
                    : new List<string>();
                var roleCounts = new Dictionary<string, int>();
                for (int slot = 0; slot < roster.Count; slot++)
                {
                    string role = roster[slot];
                    TacticalVisibilityState desired = slot == 0 ? TacticalVisibilityState.Observed : TacticalVisibilityState.Contact;
                    HexCoord position = FindObservationDeployment(desired, occupied);
                    occupied.Add(position);
                    int perRoleIndex = (roleCounts.TryGetValue(role, out int existing) ? existing : 0) + 1;
                    roleCounts[role] = perRoleIndex;
                    string id = $"{role}-{perRoleIndex}";
                    string displayName = role == TacticalScenario.SupportRole ? $"PLA Support Team {perRoleIndex}" : $"PLA Rifle Squad {perRoleIndex}";
                    var enemy = new TacticalUnitState(id, displayName, position, 4);
                    tacticalEnemyStates.Add(enemy);
                    tacticalEnemyWeapons.Add(enemy.Id, new TacticalWeaponState(enemy.Id + "-weapon", "Squad Small Arms", 6));
                    enemyMarkerLabels.Add(role == TacticalScenario.SupportRole ? "SUPPORT" : "RIFLE");
                }
            }
            for (int index = 0; index < tacticalEnemyStates.Count; index++)
            {
                TacticalUnitState enemy = tacticalEnemyStates[index];
                localMovementBoard[enemy.Position].OccupantId = enemy.Id;
                GameObject markerObject = new GameObject("Contact " + enemy.Id);
                markerObject.transform.SetParent(tacticalRoot.transform, false);
                markerObject.transform.position = LocalCounterPosition(enemy.Position) + Vector3.up * .03f;
                ContactMarkerView marker = markerObject.AddComponent<ContactMarkerView>();
                marker.Initialize(index < enemyMarkerLabels.Count ? enemyMarkerLabels[index] : "RIFLE");
                tacticalContactViews.Add(enemy.Id, marker);
            }
        }

        private IEnumerable<TacticalReactionCandidate> TacticalReactionCandidates()
        {
            foreach (TacticalUnitState enemy in tacticalEnemyStates)
                if (tacticalEnemyWeapons.TryGetValue(enemy.Id, out TacticalWeaponState weapon))
                    yield return new TacticalReactionCandidate(enemy, weapon);
        }

        private HexCoord FindObservationDeployment(TacticalVisibilityState desired, HashSet<HexCoord> excluded)
        {
            HexCoord best = tacticalUnitState.Position;
            int bestScore = int.MinValue;
            foreach (KeyValuePair<HexCoord, TacticalMovementCell> pair in localMovementBoard)
            {
                if (!pair.Value.IsPassable || excluded.Contains(pair.Key)) continue;
                TacticalContactState report = TacticalObservation.Check(localMovementBoard, tacticalUnitState.Id,
                    tacticalUnitState.Position, "probe", "Probe", pair.Key, turnState.TurnNumber);
                int stateDelta = Math.Abs((int)report.State - (int)desired);
                int preferredRange = desired == TacticalVisibilityState.Observed ? 3 : 9;
                int score = (stateDelta == 0 ? 10000 : 0) - stateDelta * 1000 - Math.Abs(report.RangeHexes - preferredRange) * 10;
                if (report.State != TacticalVisibilityState.Hidden) score += 500;
                int edgeClearance = Math.Min(Math.Min(pair.Key.Q, tacticalBattlefield.Width - 1 - pair.Key.Q),
                    Math.Min(pair.Key.R, tacticalBattlefield.Height - 1 - pair.Key.R));
                score += edgeClearance * 5;
                if (score <= bestScore) continue;
                best = pair.Key;
                bestScore = score;
            }
            return best;
        }

        private void RefreshTacticalObservation()
        {
            if (tacticalUnitState == null) return;
            foreach (KeyValuePair<HexCoord, HexCellView> pair in localCells)
            {
                TacticalLosResult los = TacticalLineOfSight.Inspect(localMovementBoard, tacticalUnitState.Position, pair.Key);
                float fog;
                if (pair.Key.Equals(tacticalUnitState.Position)) fog = 0f;
                else if (!los.IsValid) fog = .66f;
                else if (los.State == TacticalLosState.Blocked) fog = .56f;
                else if (los.State == TacticalLosState.Obscured) fog = .34f;
                else fog = los.RangeHexes <= TacticalObservation.ClearObservationRangeHexes ? .06f : .23f;
                pair.Value.SetFog(fog);
            }

            foreach (TacticalUnitState enemy in tacticalEnemyStates)
            {
                tacticalContacts.TryGetValue(enemy.Id, out TacticalContactState previous);
                TacticalContactState report = TacticalObservation.Check(localMovementBoard, tacticalUnitState.Id,
                    tacticalUnitState.Position, enemy.Id, enemy.DisplayName, enemy.Position, turnState.TurnNumber, previous);
                bool underIsrCard = tacticalBattlefield != null && tacticalBattlefield.IsrCardActive;
                report.State = TacticalRecon.ApplyBonus(report.State, TacticalRecon.IsUnderActiveRecon(tacticalReconMarkers, enemy.Position) || underIsrCard);
                tacticalContacts[enemy.Id] = report;
                if (previous != null)
                {
                    bool wasHidden = previous.State == TacticalVisibilityState.Hidden;
                    bool isHidden = report.State == TacticalVisibilityState.Hidden;
                    if (wasHidden && !isHidden) tacticalAudio.Play(TacticalSound.ContactDetected);
                    else if (!wasHidden && isHidden) tacticalAudio.Play(TacticalSound.ContactLost);
                }
                ContactMarkerView marker = tacticalContactViews[enemy.Id];
                marker.transform.position = LocalCounterPosition(report.LastKnownPosition) + Vector3.up * .03f;
                marker.Present(report, enemy);
                Debug.Log($"ALWAYS_FAITHFUL_CONTACT target={report.TargetId} state={report.State} stale={report.IsStale} position={report.LastKnownPosition} range={report.RangeHexes} observer={report.ObserverId}");
            }

            // Symmetric counterpart: what does each PLA unit currently know about
            // the platoon? Drives the AI's own contact memory (tacticalEnemyContacts,
            // consumed by RunEnemyTurn) and the player's "spotted" readout below.
            TacticalVisibilityState spottedTier = TacticalVisibilityState.Hidden;
            foreach (TacticalUnitState enemy in tacticalEnemyStates)
            {
                tacticalEnemyContacts.TryGetValue(enemy.Id, out TacticalContactState previousEnemyContact);
                TacticalContactState enemyContact = ComputeEnemyContactOnPlatoon(enemy, previousEnemyContact);
                tacticalEnemyContacts[enemy.Id] = enemyContact;
                if (!enemyContact.IsStale && enemyContact.State > spottedTier) spottedTier = enemyContact.State;
            }
            if (spottedTier != tacticalUnitSpottedTier)
            {
                bool wasHidden = tacticalUnitSpottedTier == TacticalVisibilityState.Hidden;
                bool isHidden = spottedTier == TacticalVisibilityState.Hidden;
                if (wasHidden && !isHidden) tacticalAudio.Play(TacticalSound.ContactDetected);
                else if (!wasHidden && isHidden) tacticalAudio.Play(TacticalSound.ContactLost);
                tacticalUnitSpottedTier = spottedTier;
            }
            UpdateObjectiveMarkerColor();
        }

        private TacticalContactState ComputeEnemyContactOnPlatoon(TacticalUnitState enemy, TacticalContactState previous)
        {
            TacticalContactState report = TacticalObservation.Check(localMovementBoard, enemy.Id, enemy.Position,
                tacticalUnitState.Id, tacticalUnitState.DisplayName, tacticalUnitState.Position, turnState.TurnNumber, previous);
            report.State = TacticalRecon.ApplyBonus(report.State, TacticalRecon.IsUnderActiveRecon(tacticalPlaReconMarkers, tacticalUnitState.Position));
            return report;
        }

        private void FrameObservationContacts()
        {
            Vector3 total = tacticalUnit.transform.position;
            int count = 1;
            foreach (ContactMarkerView marker in tacticalContactViews.Values)
            {
                if (!marker.gameObject.activeSelf) continue;
                total += marker.transform.position;
                count++;
            }
            cameraFocus = total / count;
            cameraFocus.y = 0f;
            cameraDistance = 30f;
            ApplyCamera();
        }

        private void UpdateTacticalPointer()
        {
            if (tacticalUnitMoving || tacticalFireResolving || tacticalEnemyTurnActive || resultScreenActive || campaignBriefingActive || settingsPanelOpen || supportCardModalActive) return;
            Vector2 guiPointer = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y) / GetUiScale();
            if (returnToIslandRect.Contains(guiPointer) || tacticalMenuOpen && tacticalMenuRect.Contains(guiPointer) ||
                new Rect(20f, 18f, 405f, 318f).Contains(guiPointer)) return;
            if (Input.GetKeyDown(settings.RemapCancelKey))
            {
                CancelTacticalInteraction(true);
                return;
            }
            HexCellView next = PickLocalHexAtScreenPoint(Input.mousePosition);
            Ray ray = mapCamera.ScreenPointToRay(Input.mousePosition);
            UnitCounterView pointedUnit = null;
            foreach (RaycastHit hit in Physics.RaycastAll(ray, 100f))
            {
                UnitCounterView candidate = hit.collider.GetComponentInParent<UnitCounterView>();
                if (candidate == tacticalUnit) pointedUnit = candidate;
            }
            if (Input.GetMouseButtonDown(1))
            {
                if (tacticalMovePlanning || tacticalLosPlanning || tacticalFirePlanning || tacticalReconPlanning)
                {
                    CancelTacticalInteraction(true);
                    return;
                }
                if (pointedUnit != null)
                {
                    OpenTacticalMenu(guiPointer);
                    return;
                }
                CancelTacticalInteraction(false);
            }
            if (Input.GetMouseButtonDown(0) && tacticalMovePlanning && next != null)
            {
                TryIssueTacticalMove(next.Coord);
                return;
            }
            if (Input.GetMouseButtonDown(0) && tacticalFirePlanning && next != null)
            {
                TryIssueTacticalFire(next.Coord);
                return;
            }
            if (Input.GetMouseButtonDown(0) && tacticalReconPlanning && next != null)
            {
                TryIssueTacticalRecon(next.Coord);
                return;
            }
            if (next == hoveredLocalCell) return;
            if (hoveredLocalCell != null)
            {
                hoveredLocalCell.SetHighlighted(false);
                hoveredLocalCell.SetInvalid(false);
            }
            hoveredLocalCell = next;
            if (hoveredLocalCell != null) hoveredLocalCell.SetHighlighted(true);
            PreviewTacticalRoute(hoveredLocalCell);
            PreviewTacticalLineOfSight(hoveredLocalCell);
            PreviewTacticalFire(hoveredLocalCell);
            PreviewTacticalRecon(hoveredLocalCell);
        }

        private HexCellView PickLocalHexAtScreenPoint(Vector2 screenPoint)
        {
            HexCellView best = null;
            float bestDistance = float.MaxValue;
            foreach (HexCellView cell in localCells.Values)
            {
                Vector3 surface = cell.transform.position + Vector3.up * CellSurfaceOffset;
                Vector3 center = mapCamera.WorldToScreenPoint(surface);
                if (center.z <= 0f) continue;
                Vector3 edge = mapCamera.WorldToScreenPoint(surface + Vector3.right * HexRadius);
                float radius = Vector2.Distance(center, edge) * 1.04f;
                float distance = Vector2.SqrMagnitude(screenPoint - new Vector2(center.x, center.y));
                if (distance > radius * radius || distance >= bestDistance) continue;
                best = cell;
                bestDistance = distance;
            }
            return best;
        }

        private void OpenTacticalMenu(Vector2 pointer)
        {
            CancelTacticalInteraction(false);
            tacticalUnitState.IsSelected = true;
            tacticalUnit.Present(tacticalUnitState);
            tacticalMenuRect = new Rect(
                Mathf.Clamp(pointer.x, 8f, Screen.width / GetUiScale() - 190f),
                Mathf.Clamp(pointer.y, 8f, Screen.height / GetUiScale() - 256f),
                178f, 238f);
            tacticalMenuOpen = true;
            tacticalOrderFeedback = "Choose a tactical order.";
            tacticalAudio.Play(TacticalSound.MenuOpen);
        }

        private void BeginTacticalMovePlanning()
        {
            if (!tacticalUnitState.CanMove) return;
            ClearTacticalLineOfSight();
            ClearTacticalFirePreview();
            tacticalLosPlanning = false;
            tacticalFirePlanning = false;
            tacticalMenuOpen = false;
            tacticalMovePlanning = true;
            tacticalUnitState.IsSelected = true;
            tacticalUnit.Present(tacticalUnitState);
            tacticalAudio.Play(TacticalSound.Inspect);
            ClearTacticalReachable();
            foreach (KeyValuePair<HexCoord, int> pair in TacticalMovementPlanner.Reachable(
                         localMovementBoard, tacticalUnitState.Position, tacticalUnitState.RemainingActionPoints, tacticalUnitState.Id))
            {
                tacticalReachable[pair.Key] = pair.Value;
                if (!pair.Key.Equals(tacticalUnitState.Position)) localCells[pair.Key].SetReachable(true);
            }
            tacticalOrderFeedback = "Hover a destination • RMB/Escape cancels";
        }

        private void BeginTacticalLosPlanning()
        {
            tacticalMenuOpen = false;
            tacticalMovePlanning = false;
            tacticalFirePlanning = false;
            if (tacticalLosOverlayActive) ClearTacticalLosOverlay();
            ClearTacticalFirePreview();
            ClearTacticalReachable();
            ClearTacticalPreview();
            tacticalLosPlanning = true;
            tacticalUnitState.IsSelected = true;
            tacticalUnit.Present(tacticalUnitState);
            tacticalAudio.Play(TacticalSound.Inspect);
            tacticalOrderFeedback = $"INSPECT LOS • {TacticalLineOfSight.MaximumInspectionRangeHexes} hex / {TacticalLineOfSight.MaximumInspectionRangeHexes * 250} m max";
            PreviewTacticalLineOfSight(hoveredLocalCell);
        }

        private void BeginTacticalFirePlanning()
        {
            if (!tacticalUnitState.CanFire || tacticalUnitState.RemainingActionPoints < TacticalDirectFire.ActionPointCost ||
                tacticalWeapon.RemainingAmmunition <= 0) return;
            tacticalMenuOpen = false;
            tacticalMovePlanning = false;
            tacticalLosPlanning = false;
            ClearTacticalReachable();
            ClearTacticalPreview();
            ClearTacticalLineOfSight();
            ClearTacticalFirePreview();
            tacticalFirePlanning = true;
            tacticalUnitState.IsSelected = true;
            tacticalUnit.Present(tacticalUnitState);
            tacticalAudio.Play(TacticalSound.Inspect);
            tacticalOrderFeedback = $"DIRECT FIRE • {tacticalWeapon.RemainingAmmunition} AMMO • Hover observed target";
            PreviewTacticalFire(hoveredLocalCell);
        }

        private void PreviewTacticalFire(HexCellView targetCell)
        {
            ClearTacticalFirePreview();
            if (!tacticalFirePlanning || targetCell == null) return;
            tacticalFireTarget = FindEnemyAt(targetCell.Coord);
            TacticalContactState contact = null;
            if (tacticalFireTarget != null) tacticalContacts.TryGetValue(tacticalFireTarget.Id, out contact);
            tacticalFirePreview = TacticalDirectFire.Preview(localMovementBoard, tacticalUnitState.Id,
                tacticalUnitState.Position, contact, targetCell.Coord, tacticalWeapon, tacticalUnitState.RemainingActionPoints);
            tacticalFireReticle.SetActive(true);
            tacticalFireReticle.transform.position = targetCell.transform.position + Vector3.up * (CellSurfaceOffset + .20f);
            Color color = tacticalFirePreview.IsValid ? new Color(1f, .56f, .14f, .68f) : new Color(.92f, .17f, .13f, .62f);
            tacticalFireReticleRenderer.material.color = color;
            tacticalFireLine.enabled = true;
            tacticalFireLine.startColor = tacticalFirePreview.IsValid ? new Color(1f, .82f, .31f, .95f) : color;
            tacticalFireLine.endColor = color;
            tacticalFireLine.SetPosition(0, localCells[tacticalUnitState.Position].transform.position + Vector3.up * (CellSurfaceOffset + .38f));
            tacticalFireLine.SetPosition(1, targetCell.transform.position + Vector3.up * (CellSurfaceOffset + .38f));
            tacticalOrderFeedback = tacticalFirePreview.IsValid
                ? $"FIRE • {tacticalFirePreview.HitChance}% • LMB confirm"
                : tacticalFirePreview.RejectionReason;
        }

        private bool TryIssueTacticalFire(HexCoord targetPosition)
        {
            if (!tacticalFirePlanning) return false;
            if (tacticalFirePreview == null || !tacticalFirePreview.TargetPosition.Equals(targetPosition) || !tacticalFirePreview.IsValid)
            {
                tacticalOrderFeedback = tacticalFirePreview != null && tacticalFirePreview.TargetPosition.Equals(targetPosition)
                    ? tacticalFirePreview.RejectionReason
                    : "Target not previewed — hover before firing";
                if (localCells.TryGetValue(targetPosition, out HexCellView rejectedFireCell)) rejectedFireCell.SetInvalid(true);
                tacticalAudio.Play(TacticalSound.OrderCancel);
                return false;
            }
            int sequence = tacticalEventSequence + 1;
            int seed = TacticalDirectFire.CreateSeed(tacticalBattlefield.BattlefieldId, turnState.TurnNumber, sequence);
            TacticalFireEvent fireEvent = TacticalDirectFire.Resolve(tacticalFirePreview, tacticalWeapon, seed);
            if (fireEvent.Outcome == TacticalFireOutcome.Rejected || !tacticalUnitState.TrySpendActionPoints(TacticalDirectFire.ActionPointCost))
            {
                tacticalOrderFeedback = fireEvent.Outcome == TacticalFireOutcome.Rejected ? "No ammunition remaining" : "Insufficient AP to fire";
                if (localCells.TryGetValue(targetPosition, out HexCellView rejectedFireCell)) rejectedFireCell.SetInvalid(true);
                tacticalAudio.Play(TacticalSound.OrderCancel);
                return false;
            }
            fireEvent.Sequence = ++tacticalEventSequence;
            fireEvent.BattlefieldId = tacticalBattlefield.BattlefieldId;
            fireEvent.Turn = turnState.TurnNumber;
            tacticalBattlefield.FireEvents.Add(fireEvent);
            TacticalUnitState target = tacticalFireTarget;
            TacticalSuppressionEvent suppressionEvent = TacticalSuppression.ApplyFireOutcome(target, fireEvent.Outcome);
            if (suppressionEvent != null)
            {
                suppressionEvent.Sequence = ++tacticalEventSequence;
                suppressionEvent.BattlefieldId = tacticalBattlefield.BattlefieldId;
                suppressionEvent.Turn = turnState.TurnNumber;
                tacticalBattlefield.SuppressionEvents.Add(suppressionEvent);
                Debug.Log($"ALWAYS_FAITHFUL_SUPPRESSION_EVENT sequence={suppressionEvent.Sequence} unit={suppressionEvent.UnitId} cause={suppressionEvent.Cause} points={suppressionEvent.PointsBefore}->{suppressionEvent.PointsAfter} status={suppressionEvent.StatusBefore}->{suppressionEvent.StatusAfter}");
            }
            tacticalFirePlanning = false;
            tacticalUnitState.IsSelected = false;
            tacticalUnit.Present(tacticalUnitState);
            if (tacticalContactViews.TryGetValue(fireEvent.TargetId, out ContactMarkerView targetView)) targetView.CueFireOutcome(fireEvent.Outcome);
            Debug.Log($"ALWAYS_FAITHFUL_FIRE_EVENT sequence={fireEvent.Sequence} seed={fireEvent.Seed} target={fireEvent.TargetId} hit={fireEvent.HitChance} effect={fireEvent.SuppressionChance} roll={fireEvent.Roll} outcome={fireEvent.Outcome} ammo={fireEvent.AmmunitionBefore}->{fireEvent.AmmunitionAfter}");
            StartCoroutine(AnimateTacticalFire(fireEvent, target));
            return true;
        }

        private void BeginTacticalReconPlanning()
        {
            if (tacticalUnitState.RemainingActionPoints < TacticalRecon.ActionPointCost) return;
            tacticalMenuOpen = false;
            tacticalMovePlanning = false;
            tacticalLosPlanning = false;
            tacticalFirePlanning = false;
            ClearTacticalReachable();
            ClearTacticalPreview();
            ClearTacticalLineOfSight();
            ClearTacticalFirePreview();
            tacticalReconPlanning = true;
            tacticalUnitState.IsSelected = true;
            tacticalUnit.Present(tacticalUnitState);
            tacticalAudio.Play(TacticalSound.Inspect);
            tacticalOrderFeedback = $"RECON • {TacticalRecon.ActionPointCost} AP • {TacticalRecon.MaximumRangeHexes} hex max • Hover a hex";
            PreviewTacticalRecon(hoveredLocalCell);
        }

        // Recon needs no line of sight to the target (it represents an indirect
        // sensor tasking, not the platoon's own eyes), so the preview is only a
        // range check rather than a traced sightline.
        private void PreviewTacticalRecon(HexCellView targetCell)
        {
            if (!tacticalReconPlanning || targetCell == null) return;
            int range = HexCoord.Distance(tacticalUnitState.Position, targetCell.Coord);
            bool inRange = range <= TacticalRecon.MaximumRangeHexes;
            targetCell.SetInvalid(!inRange);
            tacticalOrderFeedback = inRange
                ? $"RECON • {range} hex / {range * 250} m • LMB confirm"
                : $"RECON • {range} hex exceeds {TacticalRecon.MaximumRangeHexes}-hex range";
        }

        private bool TryIssueTacticalRecon(HexCoord target)
        {
            if (!tacticalReconPlanning) return false;
            int range = HexCoord.Distance(tacticalUnitState.Position, target);
            if (range > TacticalRecon.MaximumRangeHexes)
            {
                tacticalOrderFeedback = $"RECON • {range} hex exceeds {TacticalRecon.MaximumRangeHexes}-hex range";
                if (localCells.TryGetValue(target, out HexCellView rejectedReconCell)) rejectedReconCell.SetInvalid(true);
                tacticalAudio.Play(TacticalSound.OrderCancel);
                return false;
            }
            if (!tacticalUnitState.TrySpendActionPoints(TacticalRecon.ActionPointCost))
            {
                tacticalOrderFeedback = "Insufficient AP for RECON";
                if (localCells.TryGetValue(target, out HexCellView rejectedReconCell)) rejectedReconCell.SetInvalid(true);
                tacticalAudio.Play(TacticalSound.OrderCancel);
                return false;
            }

            TacticalReconMarker marker = null;
            foreach (TacticalReconMarker existing in tacticalReconMarkers)
                if (existing.Hex.Equals(target)) { marker = existing; break; }
            if (marker == null)
            {
                marker = new TacticalReconMarker { Hex = target };
                tacticalReconMarkers.Add(marker);
                tacticalBattlefield.ActiveReconMarkers.Add(marker);
            }
            marker.TurnsRemaining = TacticalRecon.DurationTurns;
            BuildTacticalReconRing(target, tacticalReconRings, TacticalReconRingColor);

            var reconEvent = new TacticalReconEvent
            {
                Sequence = ++tacticalEventSequence,
                BattlefieldId = tacticalBattlefield.BattlefieldId,
                Turn = turnState.TurnNumber,
                UnitId = tacticalUnitState.Id,
                Hex = target,
                DurationTurns = TacticalRecon.DurationTurns
            };
            tacticalBattlefield.ReconEvents.Add(reconEvent);

            tacticalReconPlanning = false;
            tacticalUnitState.IsSelected = false;
            tacticalUnit.Present(tacticalUnitState);
            if (localCells.TryGetValue(target, out HexCellView cell)) cell.SetInvalid(false);
            tacticalOrderFeedback = $"RECON TASKED • {target} • {TacticalRecon.DurationTurns} turns";
            tacticalAudio.Play(TacticalSound.OrderConfirm);
            RefreshTacticalObservation();
            Debug.Log($"ALWAYS_FAITHFUL_RECON_EVENT sequence={reconEvent.Sequence} unit={reconEvent.UnitId} hex={reconEvent.Hex} duration={reconEvent.DurationTurns}");
            return true;
        }

        private static readonly Color TacticalReconRingColor = new Color(.80f, .42f, .96f, .90f);

        // Distinct colors per ringSet so a player-recon'd hex and a PLA-recon'd
        // hex are never mistaken for each other, and both stay distinct from
        // the gold objective ring and the cyan deployment-zone ring (Pass 12).
        private void BuildTacticalReconRing(HexCoord hex, Dictionary<HexCoord, LineRenderer> ringSet, Color color)
        {
            if (ringSet.TryGetValue(hex, out LineRenderer existingRing) && existingRing != null) return;
            HexCellView cell = localCells[hex];
            GameObject ringObject = new GameObject("Recon Marker " + hex);
            ringObject.layer = LayerMask.NameToLayer("Ignore Raycast");
            ringObject.transform.SetParent(tacticalRoot.transform, false);
            LineRenderer ring = ringObject.AddComponent<LineRenderer>();
            ring.loop = true;
            ring.useWorldSpace = true;
            ring.positionCount = 36;
            ring.widthMultiplier = .05f;
            ring.material = NewOverlayMaterial(color);
            ring.startColor = color;
            ring.endColor = color;
            for (int index = 0; index < ring.positionCount; index++)
            {
                float angle = index / (float)ring.positionCount * Mathf.PI * 2f;
                float radius = index % 2 == 0 ? .62f : .50f;
                ring.SetPosition(index, cell.transform.position + new Vector3(Mathf.Cos(angle) * radius, CellSurfaceOffset + .11f, Mathf.Sin(angle) * radius));
            }
            ringSet[hex] = ring;
        }

        private void DecayTacticalReconMarkers() => DecayReconMarkerList(tacticalReconMarkers, tacticalBattlefield.ActiveReconMarkers, tacticalReconRings);

        private void DecayPlaReconMarkers() => DecayReconMarkerList(tacticalPlaReconMarkers, tacticalBattlefield.ActivePlaReconMarkers, tacticalPlaReconRings);

        private void DecayReconMarkerList(List<TacticalReconMarker> markers, List<TacticalReconMarker> battlefieldMarkers, Dictionary<HexCoord, LineRenderer> rings)
        {
            for (int index = markers.Count - 1; index >= 0; index--)
            {
                TacticalReconMarker marker = markers[index];
                marker.TurnsRemaining--;
                if (marker.TurnsRemaining > 0) continue;
                markers.RemoveAt(index);
                battlefieldMarkers.Remove(marker);
                tacticalAudio.Play(TacticalSound.ContactLost);
                if (rings.TryGetValue(marker.Hex, out LineRenderer ring) && ring != null) Destroy(ring.gameObject);
                rings.Remove(marker.Hex);
            }
        }

        private IEnumerator AnimateTacticalFire(TacticalFireEvent fireEvent, TacticalUnitState target)
        {
            tacticalFireResolving = true;
            GameObject muzzle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            muzzle.name = "Muzzle Flash";
            muzzle.transform.SetParent(tacticalRoot.transform, false);
            muzzle.transform.position = tacticalFireLine.GetPosition(0);
            muzzle.GetComponent<MeshRenderer>().sharedMaterial = NewOverlayMaterial(new Color(1f, .82f, .30f, .95f));
            Destroy(muzzle.GetComponent<Collider>());
            GameObject impact = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            impact.name = "Fire Impact";
            impact.transform.SetParent(tacticalRoot.transform, false);
            impact.transform.position = tacticalFireLine.GetPosition(1);
            Color impactColor = fireEvent.Outcome == TacticalFireOutcome.Hit
                ? new Color(1f, .20f, .12f, .95f)
                : fireEvent.Outcome == TacticalFireOutcome.Suppressed ? new Color(1f, .62f, .14f, .92f) : new Color(.62f, .72f, .68f, .75f);
            impact.GetComponent<MeshRenderer>().sharedMaterial = NewOverlayMaterial(impactColor);
            Destroy(impact.GetComponent<Collider>());
            tacticalAudio.Play(fireEvent.Outcome == TacticalFireOutcome.Hit ? TacticalSound.FireHit
                : fireEvent.Outcome == TacticalFireOutcome.Suppressed ? TacticalSound.FireSuppressed : TacticalSound.FireMiss);
            float fireDuration = .46f * AnimationTimeScale();
            for (float elapsed = 0f; elapsed < fireDuration; elapsed += Time.deltaTime)
            {
                float progress = Mathf.Clamp01(elapsed / fireDuration);
                tacticalFireLine.widthMultiplier = Mathf.Lerp(.18f, .035f, progress);
                muzzle.transform.localScale = Vector3.one * Mathf.Lerp(.28f, .03f, progress);
                impact.transform.localScale = Vector3.one * Mathf.Lerp(.12f, .62f, progress);
                yield return null;
            }
            Destroy(muzzle);
            Destroy(impact);
            tacticalFireLine.widthMultiplier = .10f;
            tacticalFireLine.enabled = false;
            tacticalFireReticle.SetActive(false);
            tacticalFirePreview = null;
            tacticalFireTarget = null;
            if (tacticalContactViews.TryGetValue(fireEvent.TargetId, out ContactMarkerView settledView) &&
                tacticalContacts.TryGetValue(fireEvent.TargetId, out TacticalContactState settledContact))
                settledView.Present(settledContact, target);
            tacticalOrderFeedback = $"{fireEvent.Outcome.ToString().ToUpperInvariant()} • ROLL {fireEvent.Roll} • H{fireEvent.HitChance}/E{fireEvent.SuppressionChance} • {tacticalWeapon.RemainingAmmunition} AMMO";
            tacticalFireResolving = false;
        }

        private void IssueTacticalRally()
        {
            if (!tacticalUnitState.CanRally) return;
            TacticalSuppressionEvent rallyEvent = TacticalSuppression.ApplyRally(tacticalUnitState);
            if (rallyEvent == null) return;
            rallyEvent.Sequence = ++tacticalEventSequence;
            rallyEvent.BattlefieldId = tacticalBattlefield.BattlefieldId;
            rallyEvent.Turn = turnState.TurnNumber;
            tacticalBattlefield.SuppressionEvents.Add(rallyEvent);
            tacticalMenuOpen = false;
            tacticalUnitState.IsSelected = false;
            tacticalUnit.Present(tacticalUnitState);
            tacticalFormationView.Present(tacticalUnitState);
            tacticalOrderFeedback = $"RALLY • {rallyEvent.StatusBefore.ToString().ToUpperInvariant()} → {rallyEvent.StatusAfter.ToString().ToUpperInvariant()} • {rallyEvent.PointsBefore}→{rallyEvent.PointsAfter} PTS";
            tacticalAudio.Play(TacticalSound.Rally);
            Debug.Log($"ALWAYS_FAITHFUL_SUPPRESSION_EVENT sequence={rallyEvent.Sequence} unit={rallyEvent.UnitId} cause={rallyEvent.Cause} points={rallyEvent.PointsBefore}->{rallyEvent.PointsAfter} status={rallyEvent.StatusBefore}->{rallyEvent.StatusAfter}");
        }

        private TacticalUnitState FindEnemyAt(HexCoord coord)
        {
            foreach (TacticalUnitState enemy in tacticalEnemyStates)
                if (enemy.Position.Equals(coord)) return enemy;
            return null;
        }

        private HexCellView FindObservedEnemyCell()
        {
            foreach (TacticalUnitState enemy in tacticalEnemyStates)
                if (tacticalContacts.TryGetValue(enemy.Id, out TacticalContactState contact) && contact.CanAttack)
                    return localCells[enemy.Position];
            return null;
        }

        private void ClearTacticalFirePreview()
        {
            if (tacticalFireResolving) return;
            if (tacticalFireLine != null) tacticalFireLine.enabled = false;
            if (tacticalFireReticle != null) tacticalFireReticle.SetActive(false);
            tacticalFirePreview = null;
            tacticalFireTarget = null;
        }

        private void PreviewTacticalRoute(HexCellView destination)
        {
            ClearTacticalPreview();
            if (!tacticalMovePlanning || destination == null) return;
            TacticalRouteResult route = TacticalMovementPlanner.FindRoute(
                localMovementBoard, tacticalUnitState.Position, destination.Coord,
                tacticalUnitState.RemainingActionPoints, tacticalUnitState.Id);
            if (!route.IsValid)
            {
                destination.SetInvalid(true);
                ShowTacticalGhost(destination.Coord, false);
                tacticalOrderFeedback = route.RejectionReason;
                tacticalPreviewCost = 0;
                return;
            }
            tacticalPreviewPath.AddRange(route.Path);
            tacticalPreviewCost = route.ActionPointCost;
            tacticalRouteLine.positionCount = route.Path.Count;
            tacticalRouteLine.enabled = route.Path.Count > 1;
            for (int index = 0; index < route.Path.Count; index++)
            {
                HexCellView cell = localCells[route.Path[index]];
                if (index > 0) cell.SetPath(true);
                tacticalRouteLine.SetPosition(index, cell.transform.position + Vector3.up * (CellSurfaceOffset + .22f));
            }
            ShowTacticalGhost(destination.Coord, true);
            tacticalOrderFeedback = $"{destination.Coord} • {route.ActionPointCost} AP • LMB confirm";
        }

        private void ShowTacticalGhost(HexCoord coord, bool valid)
        {
            tacticalDestinationGhost.SetActive(true);
            tacticalDestinationGhost.transform.position = localCells[coord].transform.position + Vector3.up * (CellSurfaceOffset + .17f);
            tacticalGhostRenderer.material.color = valid
                ? new Color(.98f, .72f, .17f, .52f)
                : new Color(.92f, .18f, .12f, .52f);
        }

        private void PreviewTacticalLineOfSight(HexCellView target)
        {
            ClearTacticalLineOfSight();
            if (!tacticalLosPlanning || target == null) return;
            tacticalLosResult = TacticalLineOfSight.Inspect(localMovementBoard, tacticalUnitState.Position, target.Coord);
            tacticalLosTarget.SetActive(true);
            tacticalLosTarget.transform.position = target.transform.position + Vector3.up * (CellSurfaceOffset + .18f);
            if (!tacticalLosResult.IsValid)
            {
                tacticalLosTargetRenderer.material.color = new Color(.91f, .16f, .13f, .58f);
                target.SetLineOfSight(TacticalLosState.Blocked);
                tacticalLosCells.Add(target.Coord);
                tacticalOrderFeedback = tacticalLosResult.RejectionReason;
                return;
            }

            tacticalLosTargetRenderer.material.color = LosColor(tacticalLosResult.State, .56f);
            for (int index = 0; index < tacticalLosResult.Samples.Count; index++)
            {
                TacticalLosSample sample = tacticalLosResult.Samples[index];
                if (index > 0)
                {
                    localCells[sample.Coord].SetLineOfSight(sample.State);
                    tacticalLosCells.Add(sample.Coord);
                    CreateTacticalLosSegment(tacticalLosResult.Samples[index - 1], sample);
                }
            }
            tacticalOrderFeedback = $"LOS {tacticalLosResult.State.ToString().ToUpperInvariant()} • {tacticalLosResult.RangeHexes} HEX / {tacticalLosResult.RangeHexes * 250} M";
        }

        private void CreateTacticalLosSegment(TacticalLosSample from, TacticalLosSample to)
        {
            GameObject segmentObject = new GameObject("LOS " + to.State + " " + to.Coord);
            segmentObject.layer = LayerMask.NameToLayer("Ignore Raycast");
            segmentObject.transform.SetParent(tacticalRoot.transform, false);
            LineRenderer segment = segmentObject.AddComponent<LineRenderer>();
            segment.useWorldSpace = true;
            segment.positionCount = 2;
            segment.widthMultiplier = .115f;
            segment.numCapVertices = 5;
            Color color = LosColor(to.State, .96f);
            segment.material = NewOverlayMaterial(color);
            segment.startColor = color;
            segment.endColor = color;
            segment.SetPosition(0, localCells[from.Coord].transform.position + Vector3.up * (CellSurfaceOffset + .31f));
            segment.SetPosition(1, localCells[to.Coord].transform.position + Vector3.up * (CellSurfaceOffset + .31f));
            tacticalLosSegments.Add(segment);
        }

        private void ClearTacticalLineOfSight()
        {
            foreach (HexCoord coord in tacticalLosCells)
                if (localCells.TryGetValue(coord, out HexCellView cell)) cell.SetLineOfSight(TacticalLosState.None);
            tacticalLosCells.Clear();
            foreach (LineRenderer segment in tacticalLosSegments)
                if (segment != null) Destroy(segment.gameObject);
            tacticalLosSegments.Clear();
            if (tacticalLosTarget != null) tacticalLosTarget.SetActive(false);
            tacticalLosResult = null;
        }

        // A persistent "what can I see from here" picture, independent of the
        // single-target INSPECT LOS tool above — same per-cell tint channel
        // (HexCellView.SetLineOfSight), so the two are kept mutually
        // exclusive rather than fighting over the same cells.
        private void ToggleTacticalLosOverlay()
        {
            if (tacticalLosOverlayActive)
            {
                ClearTacticalLosOverlay();
                return;
            }
            if (tacticalLosPlanning)
            {
                tacticalLosPlanning = false;
                ClearTacticalLineOfSight();
            }
            foreach (HexCoord coord in localMovementBoard.Keys)
            {
                if (coord.Equals(tacticalUnitState.Position)) continue;
                TacticalLosResult result = TacticalLineOfSight.Inspect(localMovementBoard, tacticalUnitState.Position, coord, TacticalLineOfSight.MaximumInspectionRangeHexes);
                if (!result.IsValid || result.State == TacticalLosState.None) continue;
                if (!localCells.TryGetValue(coord, out HexCellView cell)) continue;
                cell.SetLineOfSight(result.State);
                tacticalLosOverlayCells.Add(coord);
            }
            tacticalLosOverlayActive = true;
            tacticalAudio.Play(TacticalSound.Inspect);
        }

        private void ClearTacticalLosOverlay()
        {
            foreach (HexCoord coord in tacticalLosOverlayCells)
                if (localCells.TryGetValue(coord, out HexCellView cell)) cell.SetLineOfSight(TacticalLosState.None);
            tacticalLosOverlayCells.Clear();
            tacticalLosOverlayActive = false;
        }

        private Color LosColor(TacticalLosState state, float alpha)
        {
            if (settings.ColorSafePalette)
            {
                if (state == TacticalLosState.Blocked) return new Color(.72f, .18f, .58f, alpha);
                if (state == TacticalLosState.Obscured) return new Color(.90f, .55f, .12f, alpha);
                return new Color(.30f, .62f, .95f, alpha);
            }
            if (state == TacticalLosState.Blocked) return new Color(.92f, .17f, .13f, alpha);
            if (state == TacticalLosState.Obscured) return new Color(.98f, .66f, .16f, alpha);
            return new Color(.25f, .94f, .78f, alpha);
        }

        private bool TryIssueTacticalMove(HexCoord destination)
        {
            if (!tacticalMovePlanning || tacticalUnitMoving) return false;
            HexCoord origin = tacticalUnitState.Position;
            int before = tacticalUnitState.RemainingActionPoints;
            TacticalRouteResult route = TacticalMovementPlanner.FindRoute(
                localMovementBoard, origin, destination, before, tacticalUnitState.Id);
            if (!route.IsValid || !tacticalUnitState.TryBeginMove(route.ActionPointCost))
            {
                string reason = route.IsValid ? "Insufficient AP" : route.RejectionReason;
                tacticalOrderFeedback = reason;
                if (localCells.TryGetValue(destination, out HexCellView rejected)) rejected.SetInvalid(true);
                RecordTacticalMovement(origin, destination, 0, before, before, "Rejected", reason, route.Path);
                tacticalAudio.Play(TacticalSound.OrderCancel);
                return false;
            }
            if (tacticalLosOverlayActive) ClearTacticalLosOverlay();
            tacticalUnit.Present(tacticalUnitState);
            ClearTacticalReachable();
            DisplayCommittedTacticalRoute(route);
            tacticalAudio.Play(TacticalSound.OrderConfirm);
            StartCoroutine(MoveTacticalUnit(origin, destination, before, route));
            return true;
        }

        private void DisplayCommittedTacticalRoute(TacticalRouteResult route)
        {
            ClearTacticalPreview();
            tacticalPreviewPath.AddRange(route.Path);
            tacticalPreviewCost = route.ActionPointCost;
            tacticalRouteLine.positionCount = route.Path.Count;
            tacticalRouteLine.enabled = true;
            for (int index = 0; index < route.Path.Count; index++)
            {
                HexCellView cell = localCells[route.Path[index]];
                if (index > 0) cell.SetPath(true);
                tacticalRouteLine.SetPosition(index, cell.transform.position + Vector3.up * (CellSurfaceOffset + .22f));
            }
            ShowTacticalGhost(route.Path[route.Path.Count - 1], true);
        }

        private IEnumerator MoveTacticalUnit(HexCoord origin, HexCoord destination, int actionPointsBefore, TacticalRouteResult route)
        {
            tacticalUnitMoving = true;
            tacticalMovePlanning = false;
            tacticalLosPlanning = false;
            tacticalMenuOpen = false;
            tacticalOrderFeedback = $"MOVING • {route.ActionPointCost} AP";
            bool reactionTriggered = false;
            int reachedIndex = route.Path.Count - 1;
            for (int index = 1; index < route.Path.Count; index++)
            {
                HexCoord stepCoord = route.Path[index];
                Vector3 start = tacticalUnit.transform.position;
                Vector3 end = LocalCounterPosition(stepCoord);
                Vector3 direction = end - start;
                if (direction.sqrMagnitude > .01f) tacticalUnit.transform.rotation = Quaternion.Euler(0f, Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg, 0f);
                float stepDuration = .22f * AnimationTimeScale();
                for (float elapsed = 0f; elapsed < stepDuration; elapsed += Time.deltaTime)
                {
                    float progress = Mathf.Clamp01(elapsed / stepDuration);
                    Vector3 position = Vector3.Lerp(start, end, Mathf.SmoothStep(0f, 1f, progress));
                    if (!settings.ReducedMotion) position.y += Mathf.Sin(progress * Mathf.PI) * .16f;
                    tacticalUnit.transform.position = position;
                    yield return null;
                }
                tacticalUnit.transform.position = end;

                if (!reactionTriggered)
                {
                    TacticalReactionCandidate? candidate = TacticalReactionFire.SelectReactor(
                        localMovementBoard, TacticalReactionCandidates(), stepCoord, out TacticalLosResult los);
                    if (candidate.HasValue)
                    {
                        reactionTriggered = true;
                        yield return ResolveTacticalReaction(candidate.Value.Unit, candidate.Value.Weapon, stepCoord, los);
                        if (tacticalReactionHalted)
                        {
                            reachedIndex = index;
                            break;
                        }
                    }
                }
            }
            HexCoord finalPosition = route.Path[reachedIndex];
            bool completed = reachedIndex == route.Path.Count - 1;
            localMovementBoard[origin].OccupantId = null;
            localMovementBoard[finalPosition].OccupantId = tacticalUnitState.Id;
            tacticalUnitState.CompleteMove(finalPosition);
            tacticalUnit.transform.rotation = Quaternion.identity;
            tacticalUnit.Present(tacticalUnitState);
            RecordTacticalMovement(origin, finalPosition, route.ActionPointCost, actionPointsBefore,
                tacticalUnitState.RemainingActionPoints, completed ? "Completed" : "Interrupted",
                completed ? "Terrain and slope cost applied" : "Halted by reaction fire", route.Path.GetRange(0, reachedIndex + 1));
            RefreshTacticalObservation();
            tacticalOrderFeedback = completed
                ? $"MOVE COMPLETE • {tacticalUnitState.RemainingActionPoints} AP REMAIN"
                : $"MOVE HALTED • REACTION FIRE • {tacticalUnitState.RemainingActionPoints} AP REMAIN";
            yield return new WaitForSeconds(.30f);
            ClearTacticalPreview();
            ClearTacticalLineOfSight();
            tacticalUnitMoving = false;
        }

        private IEnumerator ResolveTacticalReaction(TacticalUnitState reactor, TacticalWeaponState reactorWeapon, HexCoord triggerPosition, TacticalLosResult los)
        {
            tacticalReactionActive = true;
            tacticalAudio.Play(TacticalSound.Interrupt);
            tacticalReactionBannerText = $"⚠ REACTION FIRE • {reactor.DisplayName.ToUpperInvariant()}";
            Vector3 savedFocus = cameraFocus;
            float savedDistance = cameraDistance;
            Vector3 reactorFocus = LocalCounterPosition(reactor.Position);
            if (settings.ReducedMotion)
            {
                cameraFocus = reactorFocus;
                cameraDistance = Mathf.Min(savedDistance, 16f);
                ApplyCamera();
            }
            else
            {
                float panDuration = .35f * AnimationTimeScale();
                float panElapsed = 0f;
                while (panElapsed < panDuration)
                {
                    panElapsed += Time.unscaledDeltaTime;
                    float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(panElapsed / panDuration));
                    cameraFocus = Vector3.Lerp(savedFocus, reactorFocus, progress);
                    cameraDistance = Mathf.Lerp(savedDistance, Mathf.Min(savedDistance, 16f), progress);
                    ApplyCamera();
                    yield return null;
                }
            }

            int sequence = tacticalEventSequence + 1;
            int seed = TacticalDirectFire.CreateSeed(tacticalBattlefield.BattlefieldId, turnState.TurnNumber, sequence);
            TacticalFirePreview preview = TacticalReactionFire.Preview(localMovementBoard, reactor, reactorWeapon, triggerPosition, los);
            TacticalFireEvent fireEvent = TacticalDirectFire.Resolve(preview, reactorWeapon, seed);
            fireEvent.Sequence = ++tacticalEventSequence;
            fireEvent.BattlefieldId = tacticalBattlefield.BattlefieldId;
            fireEvent.Turn = turnState.TurnNumber;

            tacticalFireLine.enabled = true;
            tacticalFireLine.startColor = new Color(1f, .82f, .31f, .95f);
            tacticalFireLine.endColor = new Color(1f, .56f, .14f, .68f);
            tacticalFireLine.SetPosition(0, localCells[reactor.Position].transform.position + Vector3.up * (CellSurfaceOffset + .38f));
            tacticalFireLine.SetPosition(1, localCells[triggerPosition].transform.position + Vector3.up * (CellSurfaceOffset + .38f));
            tacticalContactViews.TryGetValue(reactor.Id, out ContactMarkerView reactorView);
            yield return AnimateTacticalReaction(fireEvent, reactorView);

            TacticalSuppressionEvent suppressionEvent = TacticalSuppression.ApplyFireOutcome(tacticalUnitState, fireEvent.Outcome);
            var reactionEvent = new TacticalReactionEvent
            {
                Sequence = ++tacticalEventSequence,
                BattlefieldId = tacticalBattlefield.BattlefieldId,
                Turn = turnState.TurnNumber,
                Seed = seed,
                ReactorId = reactor.Id,
                MoverId = tacticalUnitState.Id,
                ReactorPosition = reactor.Position,
                TriggerPosition = triggerPosition,
                RangeHexes = fireEvent.RangeHexes,
                HitChance = fireEvent.HitChance,
                SuppressionChance = fireEvent.SuppressionChance,
                Roll = fireEvent.Roll,
                Outcome = fireEvent.Outcome,
                Resolution = TacticalReactionFire.ResolutionFor(fireEvent.Outcome),
                AmmunitionBefore = fireEvent.AmmunitionBefore,
                AmmunitionAfter = fireEvent.AmmunitionAfter,
                Modifiers = fireEvent.Modifiers
            };
            tacticalBattlefield.ReactionEvents.Add(reactionEvent);
            if (suppressionEvent != null)
            {
                suppressionEvent.Sequence = ++tacticalEventSequence;
                suppressionEvent.BattlefieldId = tacticalBattlefield.BattlefieldId;
                suppressionEvent.Turn = turnState.TurnNumber;
                tacticalBattlefield.SuppressionEvents.Add(suppressionEvent);
            }
            tacticalUnit.Present(tacticalUnitState);
            tacticalFormationView.Present(tacticalUnitState);
            Debug.Log($"ALWAYS_FAITHFUL_REACTION_EVENT sequence={reactionEvent.Sequence} reactor={reactionEvent.ReactorId} mover={reactionEvent.MoverId} seed={reactionEvent.Seed} hit={reactionEvent.HitChance} roll={reactionEvent.Roll} outcome={reactionEvent.Outcome} resolution={reactionEvent.Resolution}");

            if (!settings.ReducedMotion)
            {
                Vector3 fromFocus = cameraFocus;
                float fromDistance = cameraDistance;
                float returnDuration = .35f * AnimationTimeScale();
                float returnElapsed = 0f;
                while (returnElapsed < returnDuration)
                {
                    returnElapsed += Time.unscaledDeltaTime;
                    float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(returnElapsed / returnDuration));
                    cameraFocus = Vector3.Lerp(fromFocus, savedFocus, progress);
                    cameraDistance = Mathf.Lerp(fromDistance, savedDistance, progress);
                    ApplyCamera();
                    yield return null;
                }
            }
            cameraFocus = savedFocus;
            cameraDistance = savedDistance;
            ApplyCamera();
            tacticalReactionBannerText = null;
            tacticalReactionActive = false;
            tacticalReactionHalted = fireEvent.Outcome != TacticalFireOutcome.Miss;
        }

        private IEnumerator AnimateTacticalReaction(TacticalFireEvent reactionEvent, ContactMarkerView reactorView)
        {
            tacticalFireResolving = true;
            reactorView?.CueReactionSource();
            GameObject muzzle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            muzzle.name = "Reaction Muzzle Flash";
            muzzle.transform.SetParent(tacticalRoot.transform, false);
            muzzle.transform.position = tacticalFireLine.GetPosition(0);
            muzzle.GetComponent<MeshRenderer>().sharedMaterial = NewOverlayMaterial(new Color(1f, .82f, .30f, .95f));
            Destroy(muzzle.GetComponent<Collider>());
            GameObject impact = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            impact.name = "Reaction Impact";
            impact.transform.SetParent(tacticalRoot.transform, false);
            impact.transform.position = tacticalFireLine.GetPosition(1);
            Color impactColor = reactionEvent.Outcome == TacticalFireOutcome.Hit
                ? new Color(1f, .20f, .12f, .95f)
                : reactionEvent.Outcome == TacticalFireOutcome.Suppressed ? new Color(1f, .62f, .14f, .92f) : new Color(.62f, .72f, .68f, .75f);
            impact.GetComponent<MeshRenderer>().sharedMaterial = NewOverlayMaterial(impactColor);
            Destroy(impact.GetComponent<Collider>());
            tacticalAudio.Play(reactionEvent.Outcome == TacticalFireOutcome.Hit ? TacticalSound.FireHit
                : reactionEvent.Outcome == TacticalFireOutcome.Suppressed ? TacticalSound.FireSuppressed : TacticalSound.FireMiss);
            tacticalUnit.CueIncomingFire(reactionEvent.Outcome);
            tacticalSymbolView.FlashIcon(IncomingFireFlashColor(reactionEvent.Outcome));
            tacticalMiniatureView.FlashTint(IncomingFireFlashColor(reactionEvent.Outcome));
            float reactionFireDuration = .46f * AnimationTimeScale();
            for (float elapsed = 0f; elapsed < reactionFireDuration; elapsed += Time.deltaTime)
            {
                float progress = Mathf.Clamp01(elapsed / reactionFireDuration);
                tacticalFireLine.widthMultiplier = Mathf.Lerp(.18f, .035f, progress);
                muzzle.transform.localScale = Vector3.one * Mathf.Lerp(.28f, .03f, progress);
                impact.transform.localScale = Vector3.one * Mathf.Lerp(.12f, .62f, progress);
                yield return null;
            }
            Destroy(muzzle);
            Destroy(impact);
            tacticalFireLine.widthMultiplier = .10f;
            tacticalFireLine.enabled = false;
            tacticalFireResolving = false;
        }

        private void CancelTacticalInteraction(bool recordCancellation)
        {
            if (tacticalUnitMoving || tacticalFireResolving) return;
            if (recordCancellation && (tacticalMovePlanning || tacticalLosPlanning || tacticalFirePlanning || tacticalReconPlanning || tacticalMenuOpen))
                tacticalAudio.Play(TacticalSound.OrderCancel);
            if (recordCancellation && tacticalMovePlanning)
                RecordTacticalMovement(tacticalUnitState.Position, tacticalUnitState.Position, 0,
                    tacticalUnitState.RemainingActionPoints, tacticalUnitState.RemainingActionPoints,
                    "Cancelled", "Move planning cancelled", new List<HexCoord>());
            tacticalMovePlanning = false;
            tacticalLosPlanning = false;
            tacticalFirePlanning = false;
            tacticalReconPlanning = false;
            tacticalMenuOpen = false;
            tacticalUnitState.IsSelected = false;
            tacticalUnit.Present(tacticalUnitState);
            ClearTacticalReachable();
            ClearTacticalPreview();
            ClearTacticalLineOfSight();
            ClearTacticalFirePreview();
            if (hoveredLocalCell != null) hoveredLocalCell.SetInvalid(false);
            tacticalOrderFeedback = tacticalUnitState.CombatStatus == TacticalCombatStatus.Reduced
                ? "REDUCED • Rally to restore movement and fire"
                : tacticalUnitState.CanMove ? "RMB platoon for orders" : "Unit spent • End turn";
        }

        private void ClearTacticalReachable()
        {
            foreach (HexCoord coord in tacticalReachable.Keys)
                if (localCells.TryGetValue(coord, out HexCellView cell)) cell.SetReachable(false);
            tacticalReachable.Clear();
        }

        private void ClearTacticalPreview()
        {
            foreach (HexCoord coord in tacticalPreviewPath)
                if (localCells.TryGetValue(coord, out HexCellView cell)) cell.SetPath(false);
            tacticalPreviewPath.Clear();
            if (hoveredLocalCell != null) hoveredLocalCell.SetInvalid(false);
            if (tacticalRouteLine != null) tacticalRouteLine.enabled = false;
            if (tacticalDestinationGhost != null) tacticalDestinationGhost.SetActive(false);
            tacticalPreviewCost = 0;
        }

        private void DisplayTacticalCaptureRoute()
        {
            HexCoord destination = tacticalUnitState.Position;
            int highestCost = -1;
            foreach (KeyValuePair<HexCoord, int> pair in tacticalReachable)
            {
                if (pair.Key.Equals(tacticalUnitState.Position) || pair.Value <= highestCost) continue;
                destination = pair.Key;
                highestCost = pair.Value;
            }
            if (destination.Equals(tacticalUnitState.Position)) return;
            TacticalRouteResult route = TacticalMovementPlanner.FindRoute(localMovementBoard, tacticalUnitState.Position,
                destination, tacticalUnitState.RemainingActionPoints, tacticalUnitState.Id);
            if (route.IsValid)
            {
                DisplayCommittedTacticalRoute(route);
                tacticalOrderFeedback = $"ROUTE PREVIEW • {route.ActionPointCost} AP";
            }
        }

        private void RecordTacticalMovement(HexCoord origin, HexCoord destination, int cost, int before, int after,
            string outcome, string detail, IReadOnlyList<HexCoord> path)
            => RecordTacticalMovement(tacticalUnitState.Id, origin, destination, cost, before, after, outcome, detail, path);

        private void RecordTacticalMovement(string unitId, HexCoord origin, HexCoord destination, int cost, int before, int after,
            string outcome, string detail, IReadOnlyList<HexCoord> path)
        {
            var movementEvent = new TacticalMovementEvent
            {
                Sequence = ++tacticalEventSequence,
                BattlefieldId = tacticalBattlefield.BattlefieldId,
                UnitId = unitId,
                Origin = origin,
                Destination = destination,
                ActionPointCost = cost,
                ActionPointsBefore = before,
                ActionPointsAfter = after,
                Outcome = outcome,
                Detail = detail,
                Path = new List<HexCoord>(path)
            };
            tacticalBattlefield.MovementEvents.Add(movementEvent);
            Debug.Log($"ALWAYS_FAITHFUL_TACTICAL_MOVE_EVENT sequence={movementEvent.Sequence} outcome={outcome} from={origin} to={destination} cost={cost} ap={before}->{after} detail={detail}");
        }

        private void UpdatePointer()
        {
            // Do not let hover bookkeeping clear the committed route while its
            // movement coroutine is animating the counter.
            if (mapCamera == null || unitMoving || campaignBriefingActive || operationalBriefingActive ||
                operationalOrderOfBattleOpen || settingsPanelOpen || supportCardModalActive) return;
            Vector2 guiPointer = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y) / GetUiScale();
            if (endTurnRect.Contains(guiPointer) || enterTacticalRect.Contains(guiPointer) || counterMenuOpen && counterMenuRect.Contains(guiPointer)) return;
            bool leftClick = Input.GetMouseButtonDown(0);
            bool rightClick = Input.GetMouseButtonDown(1);
            Ray ray = mapCamera.ScreenPointToRay(Input.mousePosition);
            HexCellView nextHover = PickHexAtScreenPoint(Input.mousePosition);
            UnitCounterView pointedUnit = null;
            float closestUnitHit = float.MaxValue;
            foreach (RaycastHit hit in Physics.RaycastAll(ray, 100f))
            {
                UnitCounterView candidate = hit.collider.GetComponentInParent<UnitCounterView>();
                if (candidate == null || hit.distance >= closestUnitHit) continue;
                pointedUnit = candidate;
                closestUnitHit = hit.distance;
            }

            if (pointedUnit != null || nextHover != null)
            {
                if (leftClick)
                {
                    if (operationalReconPlanning && nextHover != null && TryIssueOperationalRecon(nextHover.Coord)) return;
                    if (movePlanning && pointedUnit == null && nextHover != null && TryIssueMove(nextHover.Coord)) return;
                    if (nextHover != null) SelectCell(nextHover);
                }
                if (rightClick)
                {
                    string target = pointedUnit != null ? unitState.DisplayName : nextHover.Coord.ToString();
                    Debug.Log($"ALWAYS_FAITHFUL_RMB target={target} selected={unitState.IsSelected}");
                    if (pointedUnit != null)
                    {
                        SelectOperationalBattalion(pointedUnit);
                        OpenCounterMenu(guiPointer);
                        return;
                    }
                    CancelUnitInteraction();
                }
            }
            else if (rightClick)
            {
                Debug.Log($"ALWAYS_FAITHFUL_RMB target=miss pointer={Input.mousePosition}");
                CancelUnitInteraction();
            }

            if (nextHover == hoveredCell) return;
            if (hoveredCell != null) hoveredCell.SetHighlighted(false);
            hoveredCell = nextHover;
            if (hoveredCell != null) hoveredCell.SetHighlighted(true);
            PreviewMovementPath(hoveredCell);
        }

        private HexCellView PickHexAtScreenPoint(Vector2 screenPoint)
        {
            // Visual picking is deliberately independent of the raised mesh colliders.
            // Those colliders have small presentation gaps and exposed sides, which made
            // a single physics ray oscillate between a hex and empty space over relief.
            EnsureScreenPickCache();
            HexCellView best = null;
            float bestDistance = float.MaxValue;
            foreach (ScreenHexPick candidate in screenPickCache)
            {
                if (movePlanning && !reachable.ContainsKey(candidate.Cell.Coord)) continue;
                float distance = Vector2.SqrMagnitude(screenPoint - candidate.Center);
                if (distance > candidate.Radius * candidate.Radius || distance >= bestDistance) continue;
                best = candidate.Cell;
                bestDistance = distance;
            }
            return best;
        }

        private void EnsureScreenPickCache()
        {
            if (screenPickCacheValid && mapCamera.transform.position == cachedPickCameraPosition &&
                mapCamera.transform.rotation == cachedPickCameraRotation && Screen.width == cachedPickScreenWidth &&
                Screen.height == cachedPickScreenHeight) return;

            screenPickCache.Clear();
            foreach (HexCellView cell in cells.Values)
            {
                Vector3 surface = cell.transform.position + Vector3.up * CellSurfaceOffset;
                Vector3 center = mapCamera.WorldToScreenPoint(surface);
                if (center.z <= 0f) continue;
                Vector3 edgeX = mapCamera.WorldToScreenPoint(surface + Vector3.right * HexRadius);
                Vector3 edgeZ = mapCamera.WorldToScreenPoint(surface + Vector3.forward * HexRadius);
                screenPickCache.Add(new ScreenHexPick
                {
                    Cell = cell,
                    Center = new Vector2(center.x, center.y),
                    Radius = Mathf.Max(Vector2.Distance(center, edgeX), Vector2.Distance(center, edgeZ)) * 1.04f
                });
            }
            cachedPickCameraPosition = mapCamera.transform.position;
            cachedPickCameraRotation = mapCamera.transform.rotation;
            cachedPickScreenWidth = Screen.width;
            cachedPickScreenHeight = Screen.height;
            screenPickCacheValid = true;
        }

        private bool ValidateMovementPicking(out string failure)
        {
            Vector3[] samples =
            {
                Vector3.zero,
                Vector3.right * .28f,
                Vector3.left * .28f,
                Vector3.forward * .28f,
                Vector3.back * .28f
            };
            foreach (HexCoord coord in reachable.Keys)
            {
                HexCellView expected = cells[coord];
                Vector3 surface = expected.transform.position + Vector3.up * CellSurfaceOffset;
                foreach (Vector3 offset in samples)
                {
                    Vector3 screen = mapCamera.WorldToScreenPoint(surface + offset);
                    HexCellView actual = PickHexAtScreenPoint(new Vector2(screen.x, screen.y));
                    if (actual == expected) continue;
                    failure = $"expected={expected.Coord} actual={(actual == null ? "none" : actual.Coord.ToString())} offset={offset}";
                    return false;
                }
            }
            failure = null;
            return true;
        }

        private bool ValidateGeographyInspection(out string failure)
        {
            HexCoord[] samples =
            {
                new HexCoord(0, 0),
                new HexCoord(Width - 1, 0),
                new HexCoord(0, Height - 1),
                new HexCoord(Width - 1, Height - 1),
                new HexCoord(Width / 2, Height / 2)
            };
            foreach (HexCoord expected in samples)
            {
                HexToGeographic(expected, out double longitude, out double latitude);
                if (!TryGeographicToHex(longitude, latitude, out HexCoord actual) || !actual.Equals(expected))
                {
                    failure = $"round-trip expected={expected} actual={actual} at={latitude:0.0000},{longitude:0.0000}";
                    return false;
                }
                HexCellView cell = cells[expected];
                bool expectedLand = coastline.ContainsLand(longitude, latitude);
                if (cell.IsLand != expectedLand || Mathf.Abs(cell.ElevationMetres - elevation.SampleMetres(longitude, latitude)) > .1f)
                {
                    failure = $"source mismatch hex={expected} land={cell.IsLand}/{expectedLand} elevation={cell.ElevationMetres:0.0}";
                    return false;
                }
                Vector3 screen = mapCamera.WorldToScreenPoint(cell.transform.position + Vector3.up * CellSurfaceOffset);
                HexCellView picked = PickHexAtScreenPoint(new Vector2(screen.x, screen.y));
                if (picked != cell)
                {
                    failure = $"screen edge expected={expected} actual={(picked == null ? "none" : picked.Coord.ToString())}";
                    return false;
                }
            }
            failure = null;
            return true;
        }

        private static void HexToGeographic(HexCoord coord, out double longitude, out double latitude)
        {
            longitude = DemoWest + (DemoEast - DemoWest) * coord.Q / (Width - 1d);
            latitude = DemoSouth + (DemoNorth - DemoSouth) * coord.R / (Height - 1d);
        }

        private static bool TryGeographicToHex(double longitude, double latitude, out HexCoord coord)
        {
            int q = (int)Math.Round((longitude - DemoWest) / (DemoEast - DemoWest) * (Width - 1));
            int r = (int)Math.Round((latitude - DemoSouth) / (DemoNorth - DemoSouth) * (Height - 1));
            coord = new HexCoord(Mathf.Clamp(q, 0, Width - 1), Mathf.Clamp(r, 0, Height - 1));
            return longitude >= DemoWest && longitude <= DemoEast && latitude >= DemoSouth && latitude <= DemoNorth;
        }

        private bool TryIssueMove(HexCoord destination)
        {
            if (unitMoving || !movePlanning || !unitState.IsSelected || !reachable.TryGetValue(destination, out int moveCost)) return false;
            List<HexCoord> path = MovementPlanner.FindPath(board, unitState.Position, destination, unitState.RemainingActionPoints);
            if (path.Count < 2) return false;
            if (!unitState.TryBeginMove(moveCost)) return false;
            unit.Present(unitState);
            Debug.Log($"ALWAYS_FAITHFUL_MOVE_ACCEPTED from={unitState.Position} to={destination} steps={path.Count - 1} cost={moveCost} ap={unitState.RemainingActionPoints}/{unitState.MaximumActionPoints}");
            tacticalAudio.Play(TacticalSound.OrderConfirm);
            StartCoroutine(MoveUnit(destination, path));
            return true;
        }

        private void SelectOperationalBattalion(UnitCounterView selectedView)
        {
            if (!operationalFriendlyByView.TryGetValue(selectedView, out OperationalBattalionState selected)) return;
            if (unitState != null)
            {
                unitState.IsSelected = false;
                unit.Present(unitState);
                SyncOperationalFromUnit(unitState);
            }
            activeOperationalBattalion = selected;
            unitState = operationalUnitStates[selected.Id];
            unit = selectedView;
            UpdateOccupiedHexRing(unitState.Position);
        }

        private void BeginOperationalReconPlanning()
        {
            if (activeOperationalBattalion == null || !unitState.CanMove ||
                unitState.RemainingActionPoints < OperationalScenarioRules.ReconActionPointCost) return;
            counterMenuOpen = false;
            movePlanning = false;
            operationalReconPlanning = true;
            unitState.IsSelected = true;
            unit.Present(unitState);
            ClearReachable();
            ClearPreviewPath();
            tacticalAudio.Play(TacticalSound.Inspect);
        }

        private bool TryIssueOperationalRecon(HexCoord target)
        {
            if (!operationalReconPlanning || HexCoord.Distance(unitState.Position, target) > OperationalScenarioRules.ReconMaximumRangeHexes)
                return false;
            if (!unitState.TrySpendActionPoints(OperationalScenarioRules.ReconActionPointCost)) return false;
            OperationalReconMarker marker = operationalReconMarkers.Find(candidate => candidate.Hex.Equals(target));
            if (marker == null)
            {
                marker = new OperationalReconMarker { Hex = target };
                operationalReconMarkers.Add(marker);
                operationalScenario.ReconMarkers.Add(marker);
            }
            marker.TurnsRemaining = OperationalScenarioRules.ReconDurationTurns;
            BuildOperationalReconRing(target);
            operationalReconPlanning = false;
            unit.Present(unitState);
            SyncOperationalFromUnit(unitState);
            RefreshOperationalObservation();
            SaveOperationalScenario();
            tacticalAudio.Play(TacticalSound.OrderConfirm);
            Debug.Log($"ALWAYS_FAITHFUL_OPERATIONAL_RECON unit={unitState.Id} hex={target} turn={turnState.TurnNumber}");
            return true;
        }

        private void BuildOperationalReconRing(HexCoord hex)
        {
            if (operationalReconRings.ContainsKey(hex) || !cells.TryGetValue(hex, out HexCellView cell)) return;
            GameObject ringObject = new GameObject("Operational Recon " + hex);
            ringObject.layer = LayerMask.NameToLayer("Ignore Raycast");
            ringObject.transform.SetParent(overviewRoot.transform, false);
            LineRenderer ring = ringObject.AddComponent<LineRenderer>();
            ring.loop = true;
            ring.useWorldSpace = true;
            ring.positionCount = 36;
            ring.widthMultiplier = .08f;
            Color color = new Color(.66f, .38f, .98f, .92f);
            ring.material = NewOverlayMaterial(color);
            ring.startColor = color;
            ring.endColor = color;
            for (int index = 0; index < ring.positionCount; index++)
            {
                float angle = index / (float)ring.positionCount * Mathf.PI * 2f;
                float radius = index % 2 == 0 ? 1.15f : 1.02f;
                ring.SetPosition(index, cell.transform.position + new Vector3(Mathf.Cos(angle) * radius, CellSurfaceOffset + .15f, Mathf.Sin(angle) * radius));
            }
            operationalReconRings[hex] = ring;
        }

        private void BuildOperationalObjectiveMarker()
        {
            if (operationalObjectiveRing != null || !cells.TryGetValue(operationalScenario.PrimaryObjective, out HexCellView cell)) return;
            GameObject ringObject = new GameObject("Operational Primary Objective");
            ringObject.layer = LayerMask.NameToLayer("Ignore Raycast");
            ringObject.transform.SetParent(overviewRoot.transform, false);
            operationalObjectiveRing = ringObject.AddComponent<LineRenderer>();
            operationalObjectiveRing.loop = true;
            operationalObjectiveRing.useWorldSpace = true;
            operationalObjectiveRing.positionCount = 36;
            operationalObjectiveRing.widthMultiplier = .11f;
            Color color = new Color(1f, .72f, .16f, .96f);
            operationalObjectiveRing.material = NewOverlayMaterial(color);
            operationalObjectiveRing.startColor = color;
            operationalObjectiveRing.endColor = color;
            for (int index = 0; index < operationalObjectiveRing.positionCount; index++)
            {
                float angle = index / (float)operationalObjectiveRing.positionCount * Mathf.PI * 2f;
                operationalObjectiveRing.SetPosition(index, cell.transform.position + new Vector3(Mathf.Cos(angle) * 1.22f,
                    CellSurfaceOffset + .16f, Mathf.Sin(angle) * 1.22f));
            }
            GameObject labelObject = new GameObject("Operational Objective Label");
            labelObject.transform.SetParent(ringObject.transform, false);
            labelObject.transform.position = cell.transform.position + Vector3.up * .38f;
            labelObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            TextMesh label = labelObject.AddComponent<TextMesh>();
            label.text = "OBJ";
            label.alignment = TextAlignment.Center;
            label.anchor = TextAnchor.MiddleCenter;
            label.fontSize = 44;
            label.characterSize = .10f;
            label.color = color;
        }

        private void DecayOperationalReconMarkers()
        {
            for (int index = operationalReconMarkers.Count - 1; index >= 0; index--)
            {
                OperationalReconMarker marker = operationalReconMarkers[index];
                marker.TurnsRemaining--;
                if (marker.TurnsRemaining > 0) continue;
                operationalReconMarkers.RemoveAt(index);
                operationalScenario.ReconMarkers.Remove(marker);
                if (operationalReconRings.TryGetValue(marker.Hex, out LineRenderer ring) && ring != null) Destroy(ring.gameObject);
                operationalReconRings.Remove(marker.Hex);
            }
        }

        private void RunOperationalEnemyTurn()
        {
            foreach (OperationalBattalionState enemy in operationalEnemyBattalions)
            {
                if (enemy.Strength <= 0) continue;
                Dictionary<HexCoord, int> candidates = MovementPlanner.Reachable(board, enemy.Position, 4);
                HexCoord destination = enemy.Position;
                int bestDistance = HexCoord.Distance(destination, enemy.ObjectiveHex);
                foreach (HexCoord candidate in candidates.Keys)
                {
                    int distance = HexCoord.Distance(candidate, enemy.ObjectiveHex);
                    if (distance < bestDistance)
                    {
                        destination = candidate;
                        bestDistance = distance;
                    }
                }
                enemy.Position = destination;
            }
        }

        private void RefreshOperationalObservation()
        {
            foreach (OperationalBattalionState enemy in operationalEnemyBattalions)
            {
                operationalContacts.TryGetValue(enemy.Id, out OperationalContactState previous);
                OperationalContactState contact = OperationalScenarioRules.Observe(enemy, operationalFriendlyBattalions,
                    operationalReconMarkers, operationalScenario.TurnNumber, previous);
                operationalContacts[enemy.Id] = contact;
                ContactMarkerView view = operationalEnemyViews[enemy.Id];
                HexCoord presentedPosition = contact.State == TacticalVisibilityState.Hidden ? enemy.Position : contact.LastKnownPosition;
                view.transform.position = cells[presentedPosition].transform.position + Vector3.up * (CellSurfaceOffset + CounterClearance);
                view.Present(new TacticalContactState
                {
                    TargetId = contact.TargetId,
                    DisplayName = enemy.DisplayName,
                    State = contact.State,
                    LastKnownPosition = contact.LastKnownPosition,
                    LastObservedTurn = contact.LastObservedTurn,
                    IsStale = contact.IsStale
                }, new TacticalUnitState(enemy.Id, enemy.DisplayName, enemy.Position, 1));
            }

            foreach (KeyValuePair<HexCoord, HexCellView> pair in cells)
            {
                int nearest = int.MaxValue;
                foreach (OperationalBattalionState friendly in operationalFriendlyBattalions)
                    nearest = Math.Min(nearest, HexCoord.Distance(friendly.Position, pair.Key));
                bool recon = operationalReconMarkers.Exists(marker =>
                    HexCoord.Distance(marker.Hex, pair.Key) <= OperationalScenarioRules.ReconEffectRadiusHexes);
                float fog = recon || nearest <= 5 ? 0f : nearest <= 10 ? .28f : .58f;
                pair.Value.SetFog(fog);
            }
        }

        private void OpenCounterMenu(Vector2 pointer)
        {
            if (selectedCell != null)
            {
                selectedCell.SetHighlighted(false);
                selectedCell.SetSelected(false);
            }
            selectedCell = null;
            ClearReachable();
            ClearPreviewPath();
            movePlanning = false;
            unitState.IsSelected = true;
            unit.Present(unitState);
            counterMenuRect = new Rect(
                Mathf.Clamp(pointer.x, 8f, Screen.width / GetUiScale() - 190f),
                Mathf.Clamp(pointer.y, 8f, Screen.height / GetUiScale() - 122f),
                178f,
                106f);
            counterMenuOpen = true;
            tacticalAudio.Play(TacticalSound.MenuOpen);
        }

        private void BeginMovePlanning()
        {
            if (!unitState.CanMove) return;
            counterMenuOpen = false;
            movePlanning = true;
            unitState.IsSelected = true;
            unit.Present(unitState);
            tacticalAudio.Play(TacticalSound.Inspect);
            RefreshReachable();
        }

        private void CancelUnitInteraction()
        {
            counterMenuOpen = false;
            movePlanning = false;
            operationalReconPlanning = false;
            unitState.IsSelected = false;
            unit.Present(unitState);
            SyncOperationalFromUnit(unitState);
            ClearReachable();
            ClearPreviewPath();
        }

        private void EndTurn()
        {
            if (unitMoving || operationalScenario.Outcome != OperationalScenarioOutcome.InProgress) return;
            CancelUnitInteraction();
            turnState.EndTurn(unitState);
            foreach (TacticalUnitState friendly in operationalUnitStates.Values)
            {
                if (friendly == unitState) continue;
                friendly.BeginTurn();
                operationalFriendlyViews[friendly.Id].Present(friendly);
                SyncOperationalFromUnit(friendly);
            }
            operationalScenario.TurnNumber = turnState.TurnNumber;
            RunOperationalEnemyTurn();
            DecayOperationalReconMarkers();
            RefreshOperationalObservation();
            operationalScenario.Outcome = OperationalScenarioRules.Evaluate(operationalScenario, out string operationalSummary);
            operationalScenario.OutcomeSummary = operationalSummary;
            if (tacticalUnitState != null)
            {
                tacticalUnitState.BeginTurn();
                tacticalUnit.Present(tacticalUnitState);
            }
            unit.Present(unitState);
            SyncOperationalFromUnit(unitState);
            SaveOperationalScenario();
            tacticalAudio.Play(TacticalSound.EndTurn);
            Debug.Log($"ALWAYS_FAITHFUL_TURN_STARTED turn={turnState.TurnNumber} side={turnState.ActiveSide} ap={unitState.RemainingActionPoints}/{unitState.MaximumActionPoints}");
        }

        private void EndTacticalTurn()
        {
            if (tacticalUnitMoving || tacticalFireResolving || tacticalEnemyTurnActive || resultScreenActive || campaignBriefingActive || settingsPanelOpen || supportCardModalActive) return;
            CancelTacticalInteraction(false);
            tacticalAudio.Play(TacticalSound.EndTurn);
            StartCoroutine(RunEnemyTurn());
        }

        private IEnumerator RunEnemyTurn()
        {
            tacticalEnemyTurnActive = true;
            tacticalHiddenEnemyActions = 0;
            turnState.ActiveSide = "PLA";
            tacticalEnemyActivityText = "PLA PHASE • ASSESSING BATTLESPACE";
            tacticalOrderFeedback = "ENEMY TURN • ORDERS LOCKED";
            Debug.Log($"ALWAYS_FAITHFUL_ENEMY_TURN_STARTED turn={turnState.TurnNumber}");
            yield return EnemyDelay(.55f);

            int orders = 0;
            foreach (TacticalUnitState enemy in tacticalEnemyStates)
            {
                if (orders++ >= TacticalEnemyTurn.MaximumOrdersPerTurn) break;
                enemy.BeginTurn();
                int seed = TacticalDirectFire.CreateSeed(tacticalBattlefield.BattlefieldId, turnState.TurnNumber,
                    tacticalEventSequence + orders);
                TacticalWeaponState weapon = tacticalEnemyWeapons[enemy.Id];
                tacticalEnemyContacts.TryGetValue(enemy.Id, out TacticalContactState previousEnemyContact);
                TacticalContactState opponentContact = ComputeEnemyContactOnPlatoon(enemy, previousEnemyContact);
                TacticalAiOrder order = TacticalEnemyTurn.PlanOrder(localMovementBoard, enemy, weapon,
                    tacticalUnitState.Position, tacticalUnitState, opponentContact, turnState.TurnNumber, seed);
                if (!TacticalEnemyTurn.ValidateOrder(localMovementBoard, order, enemy, weapon,
                        tacticalUnitState, opponentContact, turnState.TurnNumber, out string rejection))
                {
                    Debug.LogWarning($"ALWAYS_FAITHFUL_ENEMY_ORDER_REJECTED unit={enemy.Id} kind={order.Kind} reason={rejection}");
                    order = new TacticalAiOrder { UnitId = enemy.Id, Kind = TacticalAiOrderKind.Hold,
                        Origin = enemy.Position, Destination = enemy.Position, Seed = seed, Intent = rejection };
                }

                bool visible = tacticalContacts.TryGetValue(enemy.Id, out TacticalContactState known) &&
                    known.State != TacticalVisibilityState.Hidden && !known.IsStale;
                tacticalEnemyActivityText = visible
                    ? $"PLA PHASE • {enemy.DisplayName.ToUpperInvariant()} • {order.Kind.ToString().ToUpperInvariant()}"
                    : "PLA PHASE • HIDDEN ACTIVITY";
                if (!visible) tacticalHiddenEnemyActions++;
                if (visible)
                {
                    cameraFocus = LocalCounterPosition(enemy.Position);
                    cameraDistance = Mathf.Min(cameraDistance, 18f);
                    ApplyCamera();
                }
                yield return EnemyDelay(.30f);
                yield return ExecuteEnemyOrder(enemy, weapon, order, opponentContact, visible);
                RecordEnemyAction(enemy, order, visible);
                RefreshTacticalObservation();
                yield return EnemyDelay(.34f);
            }

            tacticalEnemyActivityText = tacticalHiddenEnemyActions > 0
                ? $"PLA PHASE COMPLETE • {tacticalHiddenEnemyActions} HIDDEN ACTION{(tacticalHiddenEnemyActions == 1 ? string.Empty : "S")} RESOLVED"
                : "PLA PHASE COMPLETE • ALL ACTIONS OBSERVED";
            yield return EnemyDelay(.62f);
            turnState.ActiveSide = "USMC";
            turnState.EndTurn(tacticalUnitState);
            tacticalUnit.Present(tacticalUnitState);
            tacticalFormationView.Present(tacticalUnitState);
            DecayTacticalReconMarkers();
            DecayPlaReconMarkers();
            RefreshTacticalObservation();
            EvaluateTacticalVictory();
            tacticalOrderFeedback = tacticalObjective.Outcome != TacticalBattleOutcome.InProgress
                ? "BATTLE CONCLUDED"
                : "USMC PHASE • AP RESTORED";
            tacticalEnemyTurnActive = false;
            tacticalEnemyActivityText = null;
            tacticalAudio.Play(TacticalSound.EndTurn);
            Debug.Log($"ALWAYS_FAITHFUL_ENEMY_TURN_COMPLETED turn={turnState.TurnNumber - 1} actions={orders} hidden={tacticalHiddenEnemyActions} next={turnState.ActiveSide} newTurn={turnState.TurnNumber}");
        }

        private void EvaluateTacticalVictory()
        {
            if (tacticalObjective == null || tacticalObjective.Outcome != TacticalBattleOutcome.InProgress) return;
            TacticalVictory.TrackObservation(tacticalObjective, tacticalContacts);
            TacticalBattleOutcome outcome = TacticalVictory.Evaluate(tacticalObjective, tacticalUnitState, tacticalEnemyStates,
                localMovementBoard, turnState.TurnNumber, out string summary);
            if (outcome == TacticalBattleOutcome.InProgress) return;
            tacticalObjective.Outcome = outcome;
            tacticalObjective.OutcomeTurn = turnState.TurnNumber;
            tacticalObjective.OutcomeSummary = summary;
            Debug.Log($"ALWAYS_FAITHFUL_BATTLE_OUTCOME outcome={outcome} turn={turnState.TurnNumber} summary={summary}");
            if (activeBattleRequest != null)
            {
                WriteBattleResult();
            }
            else if (battalionStatus != null)
            {
                int strengthBefore = battalionStatus.Strength;
                TacticalBattalion.ApplyEngagementResult(battalionStatus, outcome, tacticalUnitState.CombatStatus);
                battalionStatus.EngagementsCompleted++;
                battalionStatus.LastOutcome = outcome;
                battalionStatus.LastSummary = summary;
                battalionStatus.History.Add(new TacticalBattalionEngagementRecord
                {
                    Sequence = battalionStatus.EngagementsCompleted,
                    BattlefieldId = tacticalBattlefield.BattlefieldId,
                    MissionType = tacticalObjective.MissionType,
                    Outcome = outcome,
                    StrengthBefore = strengthBefore,
                    StrengthAfter = battalionStatus.Strength,
                    CompletedAtUtc = DateTime.UtcNow.ToString("o")
                });
                if (battalionStatus.Hand.Count < TacticalSupportCards.MaximumHandSize)
                {
                    int cardSeed = TacticalSupportCards.CreateSeed(tacticalBattlefield.BattlefieldId, turnState.TurnNumber, battalionStatus.EngagementsCompleted);
                    TacticalSupportAssetType drawnType = TacticalSupportCards.RollAssetType(cardSeed);
                    battalionStatus.Hand.Add(new TacticalSupportCard { CardId = ++battalionStatus.NextCardId, AssetType = drawnType });
                    Debug.Log($"ALWAYS_FAITHFUL_SUPPORT_CARD_DRAWN type={drawnType} handSize={battalionStatus.Hand.Count}");
                }
                else
                {
                    Debug.Log("ALWAYS_FAITHFUL_SUPPORT_CARD_HAND_FULL");
                }
                SaveBattalionStatus();
                if (activeOperationalBattalion != null)
                {
                    activeOperationalBattalion.Strength = battalionStatus.Strength;
                    OperationalBattalionState operationalEnemy = FindOperationalEnemyForBattle();
                    if (operationalEnemy != null)
                    {
                        int reduced = CountReduced(tacticalEnemyStates);
                        int loss = reduced * 12 + (outcome == TacticalBattleOutcome.UsmcVictory ? 8 : 0);
                        operationalEnemy.Strength = Math.Max(0, operationalEnemy.Strength - loss);
                    }
                    SaveOperationalScenario();
                }
                Debug.Log($"ALWAYS_FAITHFUL_BATTALION_STATUS_UPDATED strength={battalionStatus.Strength} engagements={battalionStatus.EngagementsCompleted}");
            }
            StartCoroutine(FadeResultScreen(0f, 1f, .45f));
        }

        private OperationalBattalionState FindOperationalEnemyForBattle()
        {
            if (tacticalBattlefield == null) return null;
            OperationalBattalionState best = null;
            int bestDistance = int.MaxValue;
            foreach (OperationalBattalionState enemy in operationalEnemyBattalions)
            {
                int distance = HexCoord.Distance(enemy.Position, tacticalBattlefield.ParentHex);
                if (distance >= bestDistance) continue;
                best = enemy;
                bestDistance = distance;
            }
            return bestDistance <= OperationalScenarioRules.PassiveContactRangeHexes ? best : null;
        }

        // Assembles and atomically writes the Phase I stub BattleResult, then
        // consumes the active request (one request, exactly one result) so
        // any further standalone play in this session can't overwrite it.
        private void WriteBattleResult()
        {
            bool usmcControls = localMovementBoard.TryGetValue(tacticalObjective.ObjectiveHex, out TacticalMovementCell cell) &&
                cell.OccupantId == tacticalUnitState.Id;
            var result = new BattleResult
            {
                RequestId = activeBattleRequest.RequestId,
                CampaignId = activeBattleRequest.CampaignId,
                BattlefieldId = tacticalBattlefield.BattlefieldId,
                Seed = activeBattleRequest.Seed,
                Outcome = tacticalObjective.Outcome,
                OutcomeSummary = tacticalObjective.OutcomeSummary,
                Posture = tacticalObjective.Posture,
                ObjectiveHex = tacticalObjective.ObjectiveHex,
                ObjectiveControlledByUsmc = usmcControls,
                TurnsTaken = tacticalObjective.OutcomeTurn - tacticalObjective.BattleStartTurn + 1,
                TurnLimit = tacticalObjective.TurnLimit,
                UsmcCasualties = tacticalUnitState.CombatStatus == TacticalCombatStatus.Reduced ? 1 : 0,
                PlaCasualties = CountReduced(tacticalEnemyStates),
                EventLog = BuildTacticalEventLog(int.MaxValue),
                CompletedAtUtc = DateTime.UtcNow.ToString("o")
            };
            result.Forces.Add(new BattleUnitResult
            {
                Role = "usmc-rifle-platoon",
                UnitId = tacticalUnitState.Id,
                DisplayName = tacticalUnitState.DisplayName,
                FinalStatus = tacticalUnitState.CombatStatus
            });
            // tacticalEnemyStates is always populated [rifle, support] in that
            // fixed order by BuildTacticalContacts; same convention the capture
            // and regression flags already rely on via tacticalEnemyStates[0].
            string[] enemyRoles = { "pla-rifle-squad", "pla-support-team" };
            for (int index = 0; index < tacticalEnemyStates.Count; index++)
            {
                TacticalUnitState enemy = tacticalEnemyStates[index];
                result.Forces.Add(new BattleUnitResult
                {
                    Role = index < enemyRoles.Length ? enemyRoles[index] : $"pla-unit-{index}",
                    UnitId = enemy.Id,
                    DisplayName = enemy.DisplayName,
                    FinalStatus = enemy.CombatStatus
                });
            }

            string outputPath = activeBattleRequest.OutputPath;
            string tempPath = outputPath + ".tmp";
            try
            {
                string directory = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                File.WriteAllText(tempPath, JsonUtility.ToJson(result, true));
                if (File.Exists(outputPath)) File.Delete(outputPath);
                File.Move(tempPath, outputPath);
                lastBattleResultPath = outputPath;
                Debug.Log($"ALWAYS_FAITHFUL_BATTLE_RESULT_WRITTEN path={outputPath} requestId={result.RequestId} outcome={result.Outcome}");
            }
            catch (Exception exception)
            {
                Debug.LogError($"ALWAYS_FAITHFUL_BATTLE_RESULT_WRITE_FAILED path={outputPath} error={exception.Message}");
            }
            finally
            {
                activeBattleRequest = null;
            }
        }

        private IEnumerator FadeResultScreen(float from, float to, float duration)
        {
            resultScreenActive = true;
            for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                resultScreenOpacity = Mathf.SmoothStep(from, to, elapsed / duration);
                yield return null;
            }
            resultScreenOpacity = to;
        }

        private string UnitDisplayName(string id)
        {
            if (tacticalUnitState != null && tacticalUnitState.Id == id) return tacticalUnitState.DisplayName;
            TacticalUnitState enemy = tacticalEnemyStates.Find(unit => unit.Id == id);
            return enemy != null ? enemy.DisplayName : id;
        }

        private List<string> BuildTacticalEventLog(int maximumEntries)
        {
            var entries = new List<(int Sequence, string Line)>();
            foreach (TacticalMovementEvent movement in tacticalBattlefield.MovementEvents)
            {
                string detailSuffix = string.IsNullOrEmpty(movement.Detail) ? string.Empty : $" — {movement.Detail}";
                entries.Add((movement.Sequence, $"MOVE • {UnitDisplayName(movement.UnitId)} {movement.Origin}→{movement.Destination} • {movement.Outcome}{detailSuffix}"));
            }
            foreach (TacticalFireEvent fire in tacticalBattlefield.FireEvents)
                entries.Add((fire.Sequence, $"FIRE • T{fire.Turn} • {UnitDisplayName(fire.AttackerId)}→{UnitDisplayName(fire.TargetId)} • {fire.Outcome} ({fire.HitChance}% hit)"));
            foreach (TacticalSuppressionEvent suppression in tacticalBattlefield.SuppressionEvents)
                entries.Add((suppression.Sequence, $"SUPPRESSION • T{suppression.Turn} • {UnitDisplayName(suppression.UnitId)} {suppression.StatusBefore}→{suppression.StatusAfter} ({suppression.Cause})"));
            foreach (TacticalReactionEvent reaction in tacticalBattlefield.ReactionEvents)
                entries.Add((reaction.Sequence, $"REACTION • T{reaction.Turn} • {UnitDisplayName(reaction.ReactorId)}→{UnitDisplayName(reaction.MoverId)} • {reaction.Outcome} • {reaction.Resolution}"));
            foreach (TacticalEnemyActionEvent enemyAction in tacticalBattlefield.EnemyActionEvents)
                entries.Add((enemyAction.Sequence, $"PLA • T{enemyAction.Turn} • {enemyAction.Summary}"));
            foreach (TacticalReconEvent recon in tacticalBattlefield.ReconEvents)
                entries.Add((recon.Sequence, $"RECON • T{recon.Turn} • {UnitDisplayName(recon.UnitId)} tasked {recon.Hex} ({recon.DurationTurns} turns)"));
            foreach (TacticalObjectiveEvent objectiveEvent in tacticalBattlefield.ObjectiveEvents)
                entries.Add((objectiveEvent.Sequence, $"OBJECTIVE • T{objectiveEvent.Turn} • {objectiveEvent.Hex} {(objectiveEvent.ControlledByUsmc ? "secured" : "lost")} by USMC"));
            foreach (TacticalSupportCardEvent supportCard in tacticalBattlefield.SupportCardEvents)
                entries.Add((supportCard.Sequence, $"SUPPORT • T{supportCard.Turn} • {supportCard.Summary}"));
            entries.Sort((a, b) => a.Sequence.CompareTo(b.Sequence));
            var lines = new List<string>();
            int start = Mathf.Max(0, entries.Count - maximumEntries);
            for (int index = start; index < entries.Count; index++) lines.Add(entries[index].Line);
            if (tacticalObjective != null && tacticalObjective.Outcome != TacticalBattleOutcome.InProgress)
                lines.Add($"RESULT • T{tacticalObjective.OutcomeTurn} • {tacticalObjective.OutcomeSummary}");
            return lines;
        }

        private int CountReduced(IEnumerable<TacticalUnitState> units)
        {
            int count = 0;
            foreach (TacticalUnitState unit in units)
                if (unit.CombatStatus == TacticalCombatStatus.Reduced) count++;
            return count;
        }

        private IEnumerator ExecuteEnemyOrder(TacticalUnitState enemy, TacticalWeaponState weapon,
            TacticalAiOrder order, TacticalContactState opponentContact, bool visible)
        {
            switch (order.Kind)
            {
                case TacticalAiOrderKind.Recover:
                    TacticalSuppressionEvent rally = TacticalSuppression.ApplyRally(enemy);
                    if (rally != null)
                    {
                        rally.Sequence = ++tacticalEventSequence;
                        rally.BattlefieldId = tacticalBattlefield.BattlefieldId;
                        rally.Turn = turnState.TurnNumber;
                        tacticalBattlefield.SuppressionEvents.Add(rally);
                        tacticalAudio.Play(TacticalSound.Rally);
                    }
                    break;
                case TacticalAiOrderKind.Fire:
                    yield return ExecuteEnemyFire(enemy, weapon, order, opponentContact, visible);
                    break;
                case TacticalAiOrderKind.Move:
                    yield return ExecuteEnemyMove(enemy, order, visible);
                    break;
                case TacticalAiOrderKind.Recon:
                    yield return ExecuteEnemyRecon(enemy, order, visible);
                    break;
                case TacticalAiOrderKind.Observe:
                    // opponentContact was already computed by RunEnemyTurn (with
                    // proper memory/recon-bonus applied) — Observe has no further
                    // effect to apply here beyond that, which RefreshTacticalObservation
                    // already persists into tacticalEnemyContacts after this order runs.
                    break;
            }
        }

        private IEnumerator ExecuteEnemyMove(TacticalUnitState enemy, TacticalAiOrder order, bool visible)
        {
            int before = enemy.RemainingActionPoints;
            if (!enemy.TryBeginMove(order.ActionPointCost)) yield break;
            HexCoord origin = enemy.Position;
            ContactMarkerView marker = tacticalContactViews[enemy.Id];
            for (int index = 1; index < order.Path.Count; index++)
            {
                if (!visible) continue;
                Vector3 start = marker.transform.position;
                Vector3 end = LocalCounterPosition(order.Path[index]) + Vector3.up * .03f;
                float duration = fastEnemyAnimation ? .035f : .18f * AnimationTimeScale();
                for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
                {
                    marker.transform.position = Vector3.Lerp(start, end, Mathf.SmoothStep(0f, 1f, elapsed / duration));
                    yield return null;
                }
                marker.transform.position = end;
            }
            localMovementBoard[origin].OccupantId = null;
            localMovementBoard[order.Destination].OccupantId = enemy.Id;
            enemy.CompleteMove(order.Destination);
            RecordTacticalMovement(enemy.Id, origin, order.Destination, order.ActionPointCost, before,
                enemy.RemainingActionPoints, "Completed", "AI objective movement", order.Path);
        }

        private IEnumerator ExecuteEnemyFire(TacticalUnitState enemy, TacticalWeaponState weapon,
            TacticalAiOrder order, TacticalContactState opponentContact, bool visible)
        {
            TacticalFirePreview preview = TacticalDirectFire.Preview(localMovementBoard, enemy.Id, enemy.Position,
                opponentContact, tacticalUnitState.Position, weapon, enemy.RemainingActionPoints);
            TacticalFireEvent fire = TacticalDirectFire.Resolve(preview, weapon, order.Seed);
            if (fire.Outcome == TacticalFireOutcome.Rejected || !enemy.TrySpendActionPoints(TacticalDirectFire.ActionPointCost)) yield break;
            fire.Sequence = ++tacticalEventSequence;
            fire.BattlefieldId = tacticalBattlefield.BattlefieldId;
            fire.Turn = turnState.TurnNumber;
            tacticalBattlefield.FireEvents.Add(fire);
            TacticalSuppressionEvent suppression = TacticalSuppression.ApplyFireOutcome(tacticalUnitState, fire.Outcome);
            if (suppression != null)
            {
                suppression.Sequence = ++tacticalEventSequence;
                suppression.BattlefieldId = tacticalBattlefield.BattlefieldId;
                suppression.Turn = turnState.TurnNumber;
                tacticalBattlefield.SuppressionEvents.Add(suppression);
            }
            tacticalUnit.Present(tacticalUnitState);
            tacticalFormationView.Present(tacticalUnitState);
            tacticalUnit.CueIncomingFire(fire.Outcome);
            tacticalSymbolView.FlashIcon(IncomingFireFlashColor(fire.Outcome));
            tacticalMiniatureView.FlashTint(IncomingFireFlashColor(fire.Outcome));
            tacticalFireLine.enabled = true;
            tacticalFireLine.startColor = visible ? new Color(1f, .36f, .20f, .96f) : new Color(1f, .58f, .20f, .72f);
            tacticalFireLine.endColor = tacticalFireLine.startColor;
            tacticalFireLine.SetPosition(0, localCells[enemy.Position].transform.position + Vector3.up * (CellSurfaceOffset + .38f));
            tacticalFireLine.SetPosition(1, localCells[tacticalUnitState.Position].transform.position + Vector3.up * (CellSurfaceOffset + .38f));
            tacticalAudio.Play(fire.Outcome == TacticalFireOutcome.Hit ? TacticalSound.FireHit
                : fire.Outcome == TacticalFireOutcome.Suppressed ? TacticalSound.FireSuppressed : TacticalSound.FireMiss);
            yield return EnemyDelay(.42f);
            tacticalFireLine.enabled = false;
            Debug.Log($"ALWAYS_FAITHFUL_ENEMY_FIRE unit={enemy.Id} seed={fire.Seed} roll={fire.Roll} outcome={fire.Outcome}");
        }

        // PLA-side counterpart of TryIssueTacticalRecon: tasks an active sensor
        // sweep on the platoon's last-known hex to try to reacquire lost contact.
        // Spends AP and places/refreshes a marker regardless of visibility (the
        // effect is real either way); only the ring/camera are gated on visible,
        // matching ExecuteEnemyMove/ExecuteEnemyFire's existing convention.
        private IEnumerator ExecuteEnemyRecon(TacticalUnitState enemy, TacticalAiOrder order, bool visible)
        {
            if (!enemy.TrySpendActionPoints(order.ActionPointCost)) yield break;
            TacticalReconMarker marker = null;
            foreach (TacticalReconMarker existing in tacticalPlaReconMarkers)
                if (existing.Hex.Equals(order.Destination)) { marker = existing; break; }
            if (marker == null)
            {
                marker = new TacticalReconMarker { Hex = order.Destination, IsVisibleToUsmc = visible };
                tacticalPlaReconMarkers.Add(marker);
                tacticalBattlefield.ActivePlaReconMarkers.Add(marker);
            }
            else if (visible)
            {
                // Once the player has observed the tasking, keep that knowledge
                // for the marker's remaining lifetime (including save/restore).
                marker.IsVisibleToUsmc = true;
            }
            marker.TurnsRemaining = TacticalRecon.DurationTurns;
            if (marker.IsVisibleToUsmc) BuildTacticalReconRing(order.Destination, tacticalPlaReconRings, PlaReconRingColor);
            Debug.Log($"ALWAYS_FAITHFUL_PLA_RECON_EVENT unit={enemy.Id} hex={order.Destination} duration={TacticalRecon.DurationTurns}");
            yield break;
        }

        private void RecordEnemyAction(TacticalUnitState enemy, TacticalAiOrder order, bool visible)
        {
            var action = new TacticalEnemyActionEvent
            {
                Sequence = ++tacticalEventSequence,
                BattlefieldId = tacticalBattlefield.BattlefieldId,
                Turn = turnState.TurnNumber,
                UnitId = enemy.Id,
                Kind = order.Kind,
                Origin = order.Origin,
                Destination = order.Destination,
                TargetId = order.TargetId,
                WasVisible = visible,
                Summary = visible ? $"{enemy.DisplayName}: {order.Kind}" : "Hidden enemy activity"
            };
            tacticalBattlefield.EnemyActionEvents.Add(action);
            Debug.Log($"ALWAYS_FAITHFUL_ENEMY_ACTION sequence={action.Sequence} unit={action.UnitId} kind={action.Kind} visible={action.WasVisible} from={action.Origin} to={action.Destination}");
        }

        private object EnemyDelay(float normalSeconds)
            => new WaitForSecondsRealtime(fastEnemyAnimation ? Mathf.Min(.04f, normalSeconds) : normalSeconds * AnimationTimeScale());

        private void SelectCell(HexCellView cell)
        {
            CancelUnitInteraction();
            if (selectedCell != null && selectedCell != cell)
            {
                selectedCell.SetHighlighted(false);
                selectedCell.SetSelected(false);
            }
            selectedCell = cell;
            selectedCell.SetSelected(true);
            tacticalAudio.Play(TacticalSound.Select);
        }

        private void RefreshReachable()
        {
            ClearReachable();
            foreach (KeyValuePair<HexCoord, int> pair in MovementPlanner.Reachable(board, unitState.Position, unitState.RemainingActionPoints))
            {
                reachable[pair.Key] = pair.Value;
                if (!pair.Key.Equals(unitState.Position) && cells.TryGetValue(pair.Key, out HexCellView cell)) cell.SetReachable(true);
            }
        }

        private void ClearReachable()
        {
            foreach (HexCoord coord in reachable.Keys)
                if (cells.TryGetValue(coord, out HexCellView cell)) cell.SetReachable(false);
            reachable.Clear();
        }

        private void PreviewMovementPath(HexCellView destination)
        {
            ClearPreviewPath();
            if (destination == null || !unitState.IsSelected || !reachable.ContainsKey(destination.Coord)) return;
            DisplayMovementPath(MovementPlanner.FindPath(board, unitState.Position, destination.Coord, unitState.RemainingActionPoints));
        }

        private void DisplayMovementPath(IReadOnlyList<HexCoord> path)
        {
            ClearPreviewPath();
            previewPath.AddRange(path);
            pathLine.positionCount = previewPath.Count;
            pathLine.enabled = previewPath.Count > 1;
            for (int index = 0; index < previewPath.Count; index++)
            {
                HexCellView cell = cells[previewPath[index]];
                cell.SetPath(index > 0);
                Vector3 point = cell.transform.position + Vector3.up * (CellSurfaceOffset + PathClearance);
                pathLine.SetPosition(index, point);
                if (index > 0) CreatePathMarker(point, index == previewPath.Count - 1);
            }
        }

        private void ClearPreviewPath()
        {
            foreach (HexCoord coord in previewPath)
                if (cells.TryGetValue(coord, out HexCellView cell)) cell.SetPath(false);
            previewPath.Clear();
            foreach (GameObject marker in pathMarkers)
                if (marker != null) Destroy(marker);
            pathMarkers.Clear();
            if (pathLine != null) pathLine.enabled = false;
        }

        private IEnumerator MoveUnit(HexCoord destination, List<HexCoord> path)
        {
            unitMoving = true;
            ClearReachable();
            DisplayMovementPath(path);
            for (int index = 1; index < path.Count; index++)
            {
                Vector3 start = unit.transform.position;
                Vector3 end = cells[path[index]].transform.position + Vector3.up * (CellSurfaceOffset + CounterClearance);
                float operationalStepDuration = .20f * AnimationTimeScale();
                for (float elapsed = 0f; elapsed < operationalStepDuration; elapsed += Time.deltaTime)
                {
                    float blend = Mathf.SmoothStep(0f, 1f, elapsed / operationalStepDuration);
                    unit.transform.position = Vector3.Lerp(start, end, blend);
                    yield return null;
                }
                unit.transform.position = end;
            }
            unitState.CompleteMove(destination);
            unit.Present(unitState);
            SyncOperationalFromUnit(unitState);
            UpdateOccupiedHexRing(destination);
            yield return new WaitForSeconds(.18f);
            ClearPreviewPath();
            unitMoving = false;
            movePlanning = false;
            RefreshOperationalObservation();
            SaveOperationalScenario();
            Debug.Log($"ALWAYS_FAITHFUL_MOVE_COMPLETED hex={unitState.Position} ap={unitState.RemainingActionPoints}/{unitState.MaximumActionPoints} readiness={unitState.Readiness} world={unit.transform.position}");
        }

        private IEnumerator RunMovementRegression()
        {
            // Exercise the same menu -> Move mode -> destination order path used
            // by the player, then prove model and rendered counter both changed.
            yield return null;
            if (unitState.IsSelected || movePlanning || counterMenuOpen || reachable.Count != 0)
            {
                Debug.LogError("ALWAYS_FAITHFUL_MOVEMENT_REGRESSION_FAILED initial state was not neutral");
                Application.Quit(1);
                yield break;
            }
            OpenCounterMenu(new Vector2(640f, 360f));
            if (!counterMenuOpen || movePlanning || reachable.Count != 0)
            {
                Debug.LogError("ALWAYS_FAITHFUL_MOVEMENT_REGRESSION_FAILED counter menu did not open cleanly");
                Application.Quit(1);
                yield break;
            }
            BeginMovePlanning();
            if (!ValidateMovementPicking(out string pickFailure))
            {
                Debug.LogError($"ALWAYS_FAITHFUL_MOVEMENT_REGRESSION_FAILED screen picking {pickFailure}");
                Application.Quit(1);
                yield break;
            }
            HexCoord origin = unitState.Position;
            Vector3 originWorld = unit.transform.position;
            int startingActionPoints = unitState.RemainingActionPoints;
            HexCoord destination = origin;
            int greatestCost = -1;
            foreach (KeyValuePair<HexCoord, int> pair in reachable)
            {
                if (pair.Key.Equals(origin) || pair.Value <= greatestCost) continue;
                destination = pair.Key;
                greatestCost = pair.Value;
            }
            if (destination.Equals(origin) || !TryIssueMove(destination))
            {
                Debug.LogError($"ALWAYS_FAITHFUL_MOVEMENT_REGRESSION_FAILED order origin={origin} destination={destination}");
                Application.Quit(1);
                yield break;
            }
            float deadline = Time.realtimeSinceStartup + 10f;
            bool routePreserved = pathLine.enabled && previewPath.Count > 1 && pathMarkers.Count > 0;
            while (unitMoving && Time.realtimeSinceStartup < deadline)
            {
                routePreserved &= pathLine.enabled && previewPath.Count > 1 && pathMarkers.Count > 0;
                yield return null;
            }
            float travelled = Vector3.Distance(originWorld, unit.transform.position);
            int expectedActionPoints = startingActionPoints - greatestCost;
            if (unitMoving || movePlanning || unitState.IsSelected || !routePreserved || !unitState.Position.Equals(destination) ||
                unitState.RemainingActionPoints != expectedActionPoints || !unit.Matches(unitState) || travelled < .5f)
            {
                Debug.LogError($"ALWAYS_FAITHFUL_MOVEMENT_REGRESSION_FAILED completion moving={unitMoving} planning={movePlanning} selected={unitState.IsSelected} routePreserved={routePreserved} origin={origin} actual={unitState.Position} expected={destination} ap={unitState.RemainingActionPoints}/{expectedActionPoints} viewMatches={unit.Matches(unitState)} travelled={travelled:0.000}");
                Application.Quit(1);
                yield break;
            }
            int previousTurn = turnState.TurnNumber;
            EndTurn();
            if (turnState.TurnNumber != previousTurn + 1 || unitState.RemainingActionPoints != unitState.MaximumActionPoints ||
                unitState.Readiness != UnitReadiness.Available || unitState.IsSelected || !unit.Matches(unitState))
            {
                Debug.LogError($"ALWAYS_FAITHFUL_MOVEMENT_REGRESSION_FAILED turn-reset turn={turnState.TurnNumber} ap={unitState.RemainingActionPoints} readiness={unitState.Readiness} viewMatches={unit.Matches(unitState)}");
                Application.Quit(1);
                yield break;
            }
            Debug.Log($"ALWAYS_FAITHFUL_MOVEMENT_REGRESSION_OK from={origin} to={destination} cost={greatestCost} apAfterMove={expectedActionPoints} turn={turnState.TurnNumber} resetAp={unitState.RemainingActionPoints} travelled={travelled:0.000}");
            Application.Quit(0);
        }

        private IEnumerator RunTacticalRegression()
        {
            yield return null;
            HexCellView parent = FindCoastalOperationalCell();
            Vector3 originalFocus = cameraFocus;
            float originalDistance = cameraDistance;
            TacticalBattlefieldState first = TacticalBattlefieldExtractor.Extract(
                parent.Coord, parent.Longitude, parent.Latitude,
                (longitude, latitude) => elevation.SampleMetres(longitude, latitude),
                (longitude, latitude) => coastline.ContainsLand(longitude, latitude));
            TacticalBattlefieldState second = TacticalBattlefieldExtractor.Extract(
                parent.Coord, parent.Longitude, parent.Latitude,
                (longitude, latitude) => elevation.SampleMetres(longitude, latitude),
                (longitude, latitude) => coastline.ContainsLand(longitude, latitude));
            if (!BattlefieldsMatch(first, second, out string deterministicFailure))
            {
                Debug.LogError("ALWAYS_FAITHFUL_TACTICAL_REGRESSION_FAILED deterministic " + deterministicFailure);
                Application.Quit(1);
                yield break;
            }
            foreach (TacticalBattlefieldCell cell in first.Cells)
            {
                HexCoord roundTrip = default;
                if (!first.Contains(cell.Longitude, cell.Latitude) ||
                    !first.TryGeographicToLocal(cell.Longitude, cell.Latitude, out roundTrip) ||
                    !roundTrip.Equals(cell.LocalCoord) || !cell.Id.StartsWith(first.BattlefieldId + ":", StringComparison.Ordinal))
                {
                    Debug.LogError($"ALWAYS_FAITHFUL_TACTICAL_REGRESSION_FAILED containment cell={cell.Id} roundTrip={roundTrip}");
                    Application.Quit(1);
                    yield break;
                }
            }
            TacticalBattlefieldState restored = JsonUtility.FromJson<TacticalBattlefieldState>(JsonUtility.ToJson(first));
            int coastalLand = 0;
            int coastalWater = 0;
            foreach (TacticalBattlefieldCell cell in first.Cells)
            {
                if (cell.Terrain == TacticalTerrain.Water) coastalWater++;
                else coastalLand++;
            }
            if (restored == null || restored.SchemaVersion != TacticalBattlefieldState.CurrentSchemaVersion ||
                restored.BattlefieldId != first.BattlefieldId || !restored.ParentHex.Equals(parent.Coord) ||
                restored.Cells.Count != first.Cells.Count || coastalLand == 0 || coastalWater == 0)
            {
                Debug.LogError("ALWAYS_FAITHFUL_TACTICAL_REGRESSION_FAILED serialized identity");
                Application.Quit(1);
                yield break;
            }
            for (int cycle = 0; cycle < 3; cycle++)
            {
                EnterTacticalMap(parent, false);
                if (!tacticalMode || tacticalRoot == null || !tacticalRoot.activeSelf || overviewRoot.activeSelf || localCells.Count != first.Cells.Count)
                {
                    Debug.LogError($"ALWAYS_FAITHFUL_TACTICAL_REGRESSION_FAILED enter cycle={cycle + 1}");
                    Application.Quit(1);
                    yield break;
                }
                ReturnToIsland(false);
                if (tacticalMode || !overviewRoot.activeSelf || tacticalRoot.activeSelf ||
                    Vector3.Distance(cameraFocus, originalFocus) > .0001f || Mathf.Abs(cameraDistance - originalDistance) > .0001f)
                {
                    Debug.LogError($"ALWAYS_FAITHFUL_TACTICAL_REGRESSION_FAILED drift cycle={cycle + 1} focus={cameraFocus}/{originalFocus} distance={cameraDistance}/{originalDistance}");
                    Application.Quit(1);
                    yield break;
                }
            }
            Debug.Log($"ALWAYS_FAITHFUL_TACTICAL_REGRESSION_OK battlefield={first.BattlefieldId} parent={first.ParentHex} cells={first.Cells.Count} land={coastalLand} water={coastalWater} cellSize={first.CellSizeMetres}m origin={first.OriginLatitude:0.00000},{first.OriginLongitude:0.00000} cycles=3");
            Application.Quit(0);
        }

        private IEnumerator RunTacticalMovementRegression()
        {
            yield return null;
            if (!ValidateTacticalMovementRules(out string ruleFailure))
            {
                Debug.LogError("ALWAYS_FAITHFUL_TACTICAL_MOVEMENT_REGRESSION_FAILED rules " + ruleFailure);
                Application.Quit(1);
                yield break;
            }
            EnterTacticalMap(FindCoastalOperationalCell(), false);
            HexCoord origin = tacticalUnitState.Position;
            Vector3 originWorld = tacticalUnit.transform.position;
            OpenTacticalMenu(new Vector2(640f, 360f));
            BeginTacticalMovePlanning();
            if (!tacticalMovePlanning || tacticalReachable.Count < 2 || !tacticalUnitState.IsSelected)
            {
                Debug.LogError("ALWAYS_FAITHFUL_TACTICAL_MOVEMENT_REGRESSION_FAILED planning transition");
                Application.Quit(1);
                yield break;
            }
            int eventsBeforeCancel = tacticalBattlefield.MovementEvents.Count;
            CancelTacticalInteraction(true);
            if (tacticalMovePlanning || tacticalUnitState.IsSelected || tacticalReachable.Count != 0 ||
                tacticalBattlefield.MovementEvents.Count != eventsBeforeCancel + 1 ||
                tacticalBattlefield.MovementEvents[tacticalBattlefield.MovementEvents.Count - 1].Outcome != "Cancelled")
            {
                Debug.LogError("ALWAYS_FAITHFUL_TACTICAL_MOVEMENT_REGRESSION_FAILED cancellation");
                Application.Quit(1);
                yield break;
            }

            BeginTacticalMovePlanning();
            HexCoord destination = origin;
            int expectedCost = -1;
            foreach (KeyValuePair<HexCoord, int> pair in tacticalReachable)
            {
                if (pair.Key.Equals(origin) || pair.Value <= expectedCost) continue;
                destination = pair.Key;
                expectedCost = pair.Value;
            }
            int actionPointsBefore = tacticalUnitState.RemainingActionPoints;
            if (destination.Equals(origin) || !TryIssueTacticalMove(destination))
            {
                Debug.LogError($"ALWAYS_FAITHFUL_TACTICAL_MOVEMENT_REGRESSION_FAILED order destination={destination}");
                Application.Quit(1);
                yield break;
            }
            bool routePreserved = tacticalRouteLine.enabled && tacticalPreviewPath.Count > 1 && tacticalDestinationGhost.activeSelf;
            float deadline = Time.realtimeSinceStartup + 12f;
            while (tacticalUnitMoving && Time.realtimeSinceStartup < deadline)
            {
                routePreserved &= tacticalRouteLine.enabled && tacticalPreviewPath.Count > 1 && tacticalDestinationGhost.activeSelf;
                yield return null;
            }
            // A reaction shot from an eligible enemy along the route (Pass 9) can legitimately
            // halt the unit short of its intended destination, so both outcomes are accepted here;
            // the dedicated reaction regression proves the interruption mechanics themselves.
            TacticalMovementEvent completed = tacticalBattlefield.MovementEvents[tacticalBattlefield.MovementEvents.Count - 1];
            HexCoord finalPosition = completed.Path.Count > 0 ? completed.Path[completed.Path.Count - 1] : origin;
            bool reachedFullDestination = finalPosition.Equals(destination);
            if (tacticalUnitMoving || !routePreserved || !tacticalUnitState.Position.Equals(finalPosition) ||
                tacticalUnitState.RemainingActionPoints != actionPointsBefore - expectedCost ||
                localMovementBoard[origin].OccupantId != null || localMovementBoard[finalPosition].OccupantId != tacticalUnitState.Id ||
                (completed.Outcome != "Completed" && completed.Outcome != "Interrupted") || completed.ActionPointCost != expectedCost ||
                !tacticalUnit.Matches(tacticalUnitState) ||
                (reachedFullDestination && Vector3.Distance(originWorld, tacticalUnit.transform.position) < .5f))
            {
                Debug.LogError($"ALWAYS_FAITHFUL_TACTICAL_MOVEMENT_REGRESSION_FAILED completion route={routePreserved} actual={tacticalUnitState.Position} expected={destination} final={finalPosition} ap={tacticalUnitState.RemainingActionPoints} event={completed.Outcome}");
                Application.Quit(1);
                yield break;
            }

            tacticalUnitState.BeginTurn();
            tacticalUnit.Present(tacticalUnitState);
            tacticalFormationView.Present(tacticalUnitState);
            BeginTacticalMovePlanning();
            HexCoord water = destination;
            foreach (KeyValuePair<HexCoord, TacticalMovementCell> pair in localMovementBoard)
                if (pair.Value.Terrain == TacticalTerrain.Water) { water = pair.Key; break; }
            int beforeRejected = tacticalBattlefield.MovementEvents.Count;
            bool acceptedWater = TryIssueTacticalMove(water);
            if (acceptedWater || tacticalBattlefield.MovementEvents.Count != beforeRejected + 1 ||
                tacticalBattlefield.MovementEvents[tacticalBattlefield.MovementEvents.Count - 1].Outcome != "Rejected")
            {
                Debug.LogError("ALWAYS_FAITHFUL_TACTICAL_MOVEMENT_REGRESSION_FAILED illegal destination");
                Application.Quit(1);
                yield break;
            }
            CancelTacticalInteraction(false);
            string serialized = JsonUtility.ToJson(tacticalBattlefield);
            if (string.IsNullOrEmpty(serialized) || !(serialized.Contains("Completed") || serialized.Contains("Interrupted")) ||
                !serialized.Contains("Cancelled") || !serialized.Contains("Rejected"))
            {
                Debug.LogError("ALWAYS_FAITHFUL_TACTICAL_MOVEMENT_REGRESSION_FAILED event serialization");
                Application.Quit(1);
                yield break;
            }
            Debug.Log($"ALWAYS_FAITHFUL_TACTICAL_MOVEMENT_REGRESSION_OK from={origin} to={finalPosition} cost={expectedCost} outcome={completed.Outcome} events={tacticalBattlefield.MovementEvents.Count} ap={tacticalUnitState.RemainingActionPoints}/{tacticalUnitState.MaximumActionPoints} routePreserved={routePreserved}");
            Application.Quit(0);
        }

        private static bool ValidateTacticalMovementRules(out string failure)
        {
            var origin = new HexCoord(0, 0);
            var rough = new HexCoord(1, 0);
            var water = new HexCoord(0, 1);
            var steep = new HexCoord(1, 1);
            var occupied = new HexCoord(0, -1);
            var fixture = new Dictionary<HexCoord, TacticalMovementCell>
            {
                [origin] = new TacticalMovementCell { Coord = origin, Terrain = TacticalTerrain.Open, ElevationMetres = 10f, OccupantId = "mover" },
                [rough] = new TacticalMovementCell { Coord = rough, Terrain = TacticalTerrain.Rough, ElevationMetres = 30f },
                [water] = new TacticalMovementCell { Coord = water, Terrain = TacticalTerrain.Water, ElevationMetres = 0f },
                [steep] = new TacticalMovementCell { Coord = steep, Terrain = TacticalTerrain.Highland, ElevationMetres = 150f },
                [occupied] = new TacticalMovementCell { Coord = occupied, Terrain = TacticalTerrain.Open, ElevationMetres = 10f, OccupantId = "other" }
            };
            if (!TacticalMovementPlanner.TryEdgeCost(fixture, origin, rough, "mover", out int cost, out _) || cost != 3)
            {
                failure = "terrain/slope cost";
                return false;
            }
            if (TacticalMovementPlanner.TryEdgeCost(fixture, origin, water, "mover", out _, out string waterReason) || waterReason != "Water is impassable" ||
                TacticalMovementPlanner.TryEdgeCost(fixture, origin, steep, "mover", out _, out string steepReason) || steepReason != "Slope too steep" ||
                TacticalMovementPlanner.TryEdgeCost(fixture, origin, occupied, "mover", out _, out string occupiedReason) || occupiedReason != "Destination occupied")
            {
                failure = "impassable edge or occupancy";
                return false;
            }
            TacticalRouteResult insufficient = TacticalMovementPlanner.FindRoute(fixture, origin, rough, 2, "mover");
            if (insufficient.IsValid || insufficient.RejectionReason != "Beyond remaining AP")
            {
                failure = "AP destination validation";
                return false;
            }
            var expensive = new HexCoord(0, 1);
            var destination = new HexCoord(0, 2);
            var flankOne = new HexCoord(1, 0);
            var flankTwo = new HexCoord(1, 1);
            var cheapestFixture = new Dictionary<HexCoord, TacticalMovementCell>
            {
                [origin] = new TacticalMovementCell { Coord = origin, Terrain = TacticalTerrain.Open, ElevationMetres = 0f, OccupantId = "mover" },
                [expensive] = new TacticalMovementCell { Coord = expensive, Terrain = TacticalTerrain.Highland, ElevationMetres = 0f },
                [destination] = new TacticalMovementCell { Coord = destination, Terrain = TacticalTerrain.Open, ElevationMetres = 0f },
                [flankOne] = new TacticalMovementCell { Coord = flankOne, Terrain = TacticalTerrain.Open, ElevationMetres = 0f },
                [flankTwo] = new TacticalMovementCell { Coord = flankTwo, Terrain = TacticalTerrain.Open, ElevationMetres = 0f }
            };
            TacticalRouteResult cheapest = TacticalMovementPlanner.FindRoute(cheapestFixture, origin, destination, 8, "mover");
            if (!cheapest.IsValid || cheapest.ActionPointCost != 3 || cheapest.Path.Contains(expensive))
            {
                failure = "cheapest-path selection";
                return false;
            }
            failure = null;
            return true;
        }

        private IEnumerator RunLosRegression()
        {
            yield return null;
            if (!ValidateLosRules(out string failure))
            {
                Debug.LogError("ALWAYS_FAITHFUL_LOS_REGRESSION_FAILED rules " + failure);
                Application.Quit(1);
                yield break;
            }
            EnterTacticalMap(FindHighReliefOperationalCell(), false);
            BeginTacticalLosPlanning();
            HexCellView target = FindLosCaptureTarget();
            PreviewTacticalLineOfSight(target);
            if (!tacticalLosPlanning || tacticalLosResult == null || !tacticalLosResult.IsValid ||
                tacticalLosResult.RangeHexes < 1 || tacticalLosSegments.Count != tacticalLosResult.RangeHexes ||
                tacticalLosCells.Count != tacticalLosResult.RangeHexes || !tacticalLosTarget.activeSelf)
            {
                Debug.LogError($"ALWAYS_FAITHFUL_LOS_REGRESSION_FAILED presentation state={tacticalLosResult?.State} range={tacticalLosResult?.RangeHexes} segments={tacticalLosSegments.Count} cells={tacticalLosCells.Count}");
                Application.Quit(1);
                yield break;
            }
            TacticalLosState inspectedState = tacticalLosResult.State;
            int inspectedRange = tacticalLosResult.RangeHexes;
            int modifierCount = tacticalLosResult.Modifiers.Count;
            string serialized = JsonUtility.ToJson(tacticalLosResult);
            CancelTacticalInteraction(false);
            if (tacticalLosPlanning || tacticalLosSegments.Count != 0 || tacticalLosCells.Count != 0 || tacticalLosTarget.activeSelf ||
                string.IsNullOrEmpty(serialized) || modifierCount == 0)
            {
                Debug.LogError("ALWAYS_FAITHFUL_LOS_REGRESSION_FAILED cleanup or serialization");
                Application.Quit(1);
                yield break;
            }
            Debug.Log($"ALWAYS_FAITHFUL_LOS_REGRESSION_OK state={inspectedState} range={inspectedRange} segments={inspectedRange} modifiers={modifierCount} maxRange={TacticalLineOfSight.MaximumInspectionRangeHexes}");
            Application.Quit(0);
        }

        // Group B proof: the LOS overlay populates and clears correctly and
        // stays mutually exclusive with the pre-existing single-target
        // INSPECT LOS tool (both drive the same HexCellView.SetLineOfSight
        // channel), and the live event log reflects a freshly recorded event.
        private IEnumerator RunHudRegression()
        {
            yield return null;
            EnterTacticalMap(FindHighReliefOperationalCell(), false);

            if (tacticalLosOverlayActive || tacticalLosOverlayCells.Count != 0)
            {
                Debug.LogError("ALWAYS_FAITHFUL_HUD_REGRESSION_FAILED overlay was already active on battle entry");
                Application.Quit(1);
                yield break;
            }

            ToggleTacticalLosOverlay();
            int overlayCellsAfterToggleOn = tacticalLosOverlayCells.Count;
            if (!tacticalLosOverlayActive || overlayCellsAfterToggleOn == 0)
            {
                Debug.LogError($"ALWAYS_FAITHFUL_HUD_REGRESSION_FAILED overlay did not populate active={tacticalLosOverlayActive} cells={overlayCellsAfterToggleOn}");
                Application.Quit(1);
                yield break;
            }

            BeginTacticalLosPlanning();
            if (tacticalLosOverlayActive || tacticalLosOverlayCells.Count != 0 || !tacticalLosPlanning)
            {
                Debug.LogError("ALWAYS_FAITHFUL_HUD_REGRESSION_FAILED overlay did not yield to single-target LOS planning");
                Application.Quit(1);
                yield break;
            }

            ToggleTacticalLosOverlay();
            if (tacticalLosPlanning || !tacticalLosOverlayActive || tacticalLosOverlayCells.Count == 0)
            {
                Debug.LogError("ALWAYS_FAITHFUL_HUD_REGRESSION_FAILED single-target LOS planning did not yield back to the overlay");
                Application.Quit(1);
                yield break;
            }

            ToggleTacticalLosOverlay();
            if (tacticalLosOverlayActive || tacticalLosOverlayCells.Count != 0)
            {
                Debug.LogError("ALWAYS_FAITHFUL_HUD_REGRESSION_FAILED overlay did not clear on toggle-off");
                Application.Quit(1);
                yield break;
            }

            int logEntriesBefore = BuildTacticalEventLog(int.MaxValue).Count;
            BeginTacticalMovePlanning();
            TryIssueTacticalMove(tacticalUnitState.Position);
            List<string> logAfter = BuildTacticalEventLog(int.MaxValue);
            if (logAfter.Count <= logEntriesBefore || !logAfter[logAfter.Count - 1].StartsWith("MOVE •", StringComparison.Ordinal))
            {
                Debug.LogError($"ALWAYS_FAITHFUL_HUD_REGRESSION_FAILED event log did not record the rejected move before={logEntriesBefore} after={logAfter.Count}");
                Application.Quit(1);
                yield break;
            }

            bool reactionReady = tacticalUnitState.CanFire && tacticalWeapon.RemainingAmmunition > 0;
            if (!reactionReady)
            {
                Debug.LogError("ALWAYS_FAITHFUL_HUD_REGRESSION_FAILED fresh platoon should read reaction-ready");
                Application.Quit(1);
                yield break;
            }

            Debug.Log($"ALWAYS_FAITHFUL_HUD_REGRESSION_OK overlayCells={overlayCellsAfterToggleOn} logEntries={logAfter.Count} reactionReady={reactionReady}");
            Application.Quit(0);
        }

        // Proof for the OPEN LOCAL MAP / RESOLVE CONTACT distinction: entering
        // a hex the operational layer has no detected contact at must not
        // manufacture an encounter, while a genuine RESOLVE CONTACT entry
        // keeps rolling the usual hash-driven roster.
        private IEnumerator RunNoContactRegression()
        {
            yield return null;
            EnterTacticalMap(FindHighReliefOperationalCell(), false, null, false);
            if (tacticalEnemyStates.Count != 0)
            {
                Debug.LogError($"ALWAYS_FAITHFUL_NO_CONTACT_REGRESSION_FAILED expected an empty roster with no known contact, got {tacticalEnemyStates.Count}");
                Application.Quit(1);
                yield break;
            }
            if (tacticalObjective == null || tacticalObjective.Outcome != TacticalBattleOutcome.InProgress)
            {
                Debug.LogError("ALWAYS_FAITHFUL_NO_CONTACT_REGRESSION_FAILED an enemy-free battle should still set up a normal in-progress objective");
                Application.Quit(1);
                yield break;
            }

            EnterTacticalMap(FindCoastalOperationalCell(), false, null, true);
            if (tacticalEnemyStates.Count < TacticalScenario.MinRosterSize || tacticalEnemyStates.Count > TacticalScenario.MaxRosterSize)
            {
                Debug.LogError($"ALWAYS_FAITHFUL_NO_CONTACT_REGRESSION_FAILED a known-contact entry should still roll the usual roster, got {tacticalEnemyStates.Count}");
                Application.Quit(1);
                yield break;
            }

            Debug.Log($"ALWAYS_FAITHFUL_NO_CONTACT_REGRESSION_OK knownContactRoster={tacticalEnemyStates.Count}");
            Application.Quit(0);
        }

        private IEnumerator RunObservationRegression()
        {
            yield return null;
            if (!ValidateObservationRules(out string failure))
            {
                Debug.LogError("ALWAYS_FAITHFUL_OBSERVATION_REGRESSION_FAILED rules " + failure);
                Application.Quit(1);
                yield break;
            }
            // Pinned so the roster deterministically includes both an Observed-tier
            // and a Contact-tier enemy (this test proves both presentation states
            // render); a fresh random scenario could otherwise roll a 1-unit roster.
            scenarioSeedOverride = 12345;
            EnterTacticalMap(FindHighReliefOperationalCell(), false);
            // This check proves the presentation pipeline can render every contact
            // state (detailed vs. uncertain) on a fixed scenario; it predates cover
            // and is intentionally independent of it. FindObservationDeployment
            // already chose enemy spawn hexes using cover-aware LOS, so cover must
            // be neutralized and deployment re-run, not just LOS re-evaluated in
            // place, or the enemy can still be stuck on a now-stale, cover-chosen
            // hex that happens to be genuinely terrain-hidden once cover is gone.
            foreach (TacticalMovementCell cell in localMovementBoard.Values) cell.Cover = TacticalCover.None;
            BuildTacticalContacts();
            RefreshTacticalObservation();
            float minimumFog = 1f;
            float maximumFog = 0f;
            foreach (HexCellView cell in localCells.Values)
            {
                minimumFog = Mathf.Min(minimumFog, cell.FogAmount);
                maximumFog = Mathf.Max(maximumFog, cell.FogAmount);
            }
            int visibleMarkers = 0;
            int detailedFormations = 0;
            int uncertainGlyphs = 0;
            foreach (KeyValuePair<string, ContactMarkerView> pair in tacticalContactViews)
            {
                if (pair.Value.gameObject.activeSelf && pair.Value.TransitionCount > 0 &&
                    pair.Value.PresentedState == tacticalContacts[pair.Key].State) visibleMarkers++;
                if (pair.Value.DetailedFormationVisible && !pair.Value.ContactGlyphVisible && pair.Value.FormationElementCount == 3) detailedFormations++;
                if (pair.Value.ContactGlyphVisible && !pair.Value.DetailedFormationVisible) uncertainGlyphs++;
            }
            var snapshot = new TacticalObservationSnapshot { Contacts = new List<TacticalContactState>(tacticalContacts.Values) };
            string serialized = JsonUtility.ToJson(snapshot);
            TacticalObservationSnapshot restored = JsonUtility.FromJson<TacticalObservationSnapshot>(serialized);
            // Standalone play now generates a 1-3 unit roster rather than always
            // exactly 2 (Pass: randomized scenario generation); this test's own
            // purpose is proving detailed/uncertain contact presentation, which
            // is independent of the exact headcount, so it only bounds-checks
            // the count rather than requiring the old fixed value.
            if (tacticalContacts.Count < TacticalScenario.MinRosterSize || tacticalContacts.Count > TacticalScenario.MaxRosterSize ||
                tacticalContactViews.Count != tacticalContacts.Count || visibleMarkers < 1 || detailedFormations < 1 || uncertainGlyphs < 1 ||
                tacticalFormationView == null || tacticalFormationView.ManeuverElementCount != 3 ||
                !tacticalFormationView.HasRecognitionStripe || !tacticalFormationView.HasCommandNode ||
                tacticalFormationView.Affiliation != TacticalFormationAffiliation.Usmc ||
                minimumFog > .01f || maximumFog < .60f || restored == null || restored.Contacts.Count != tacticalContacts.Count)
            {
                Debug.LogError($"ALWAYS_FAITHFUL_OBSERVATION_REGRESSION_FAILED presentation contacts={tacticalContacts.Count} views={tacticalContactViews.Count} visible={visibleMarkers} detailed={detailedFormations} uncertain={uncertainGlyphs} friendlyElements={tacticalFormationView?.ManeuverElementCount} fog={minimumFog:0.00}-{maximumFog:0.00} restored={restored?.Contacts.Count}");
                Application.Quit(1);
                yield break;
            }
            Debug.Log($"ALWAYS_FAITHFUL_OBSERVATION_REGRESSION_OK contacts={tacticalContacts.Count} visible={visibleMarkers} detailed={detailedFormations} uncertain={uncertainGlyphs} friendlyElements={tacticalFormationView.ManeuverElementCount} fog={minimumFog:0.00}-{maximumFog:0.00} serialized={serialized.Length}");
            Application.Quit(0);
        }

        private IEnumerator RunFireRegression()
        {
            yield return null;
            if (!ValidateFireRules(out string failure))
            {
                Debug.LogError("ALWAYS_FAITHFUL_FIRE_REGRESSION_FAILED rules " + failure);
                Application.Quit(1);
                yield break;
            }
            EnterTacticalMap(FindHighReliefOperationalCell(), false);
            HexCellView target = FindObservedEnemyCell();
            BeginTacticalFirePlanning();
            PreviewTacticalFire(target);
            if (target == null || !tacticalFirePlanning || tacticalFirePreview == null || !tacticalFirePreview.IsValid ||
                !tacticalFireLine.enabled || !tacticalFireReticle.activeSelf)
            {
                Debug.LogError($"ALWAYS_FAITHFUL_FIRE_REGRESSION_FAILED preview target={target != null} planning={tacticalFirePlanning} valid={tacticalFirePreview?.IsValid} line={tacticalFireLine.enabled} reticle={tacticalFireReticle.activeSelf}");
                Application.Quit(1);
                yield break;
            }
            string targetId = tacticalFirePreview.TargetId;
            int ammunitionBefore = tacticalWeapon.RemainingAmmunition;
            int actionPointsBefore = tacticalUnitState.RemainingActionPoints;
            int eventsBefore = tacticalBattlefield.FireEvents.Count;
            if (!TryIssueTacticalFire(target.Coord))
            {
                Debug.LogError("ALWAYS_FAITHFUL_FIRE_REGRESSION_FAILED fire rejected");
                Application.Quit(1);
                yield break;
            }
            float deadline = Time.realtimeSinceStartup + 2f;
            do { yield return null; } while (tacticalFireResolving && Time.realtimeSinceStartup < deadline);
            TacticalFireEvent fireEvent = tacticalBattlefield.FireEvents[tacticalBattlefield.FireEvents.Count - 1];
            ContactMarkerView targetView = tacticalContactViews[targetId];
            string serialized = JsonUtility.ToJson(tacticalBattlefield);
            TacticalBattlefieldState restored = JsonUtility.FromJson<TacticalBattlefieldState>(serialized);
            if (tacticalFireResolving || tacticalFirePlanning || tacticalFireLine.enabled || tacticalFireReticle.activeSelf ||
                tacticalWeapon.RemainingAmmunition != ammunitionBefore - 1 ||
                tacticalUnitState.RemainingActionPoints != actionPointsBefore - TacticalDirectFire.ActionPointCost ||
                tacticalBattlefield.FireEvents.Count != eventsBefore + 1 || fireEvent.Outcome == TacticalFireOutcome.Rejected ||
                targetView.FireCueCount != 1 || targetView.LastFireOutcome != fireEvent.Outcome ||
                restored == null || restored.FireEvents.Count != 1 || restored.FireEvents[0].Seed != fireEvent.Seed)
            {
                Debug.LogError($"ALWAYS_FAITHFUL_FIRE_REGRESSION_FAILED resolution resolving={tacticalFireResolving} ammo={tacticalWeapon.RemainingAmmunition}/{ammunitionBefore} ap={tacticalUnitState.RemainingActionPoints}/{actionPointsBefore} events={tacticalBattlefield.FireEvents.Count} cue={targetView.FireCueCount} restored={restored?.FireEvents.Count}");
                Application.Quit(1);
                yield break;
            }
            Debug.Log($"ALWAYS_FAITHFUL_FIRE_REGRESSION_OK target={fireEvent.TargetId} seed={fireEvent.Seed} chance={fireEvent.HitChance} roll={fireEvent.Roll} outcome={fireEvent.Outcome} ammo={fireEvent.AmmunitionBefore}->{fireEvent.AmmunitionAfter} ap={actionPointsBefore}->{tacticalUnitState.RemainingActionPoints} modifiers={fireEvent.Modifiers.Count}");
            Application.Quit(0);
        }

        private IEnumerator RunReactionRegression()
        {
            yield return null;
            if (!ValidateReactionRules(out string ruleFailure))
            {
                Debug.LogError("ALWAYS_FAITHFUL_REACTION_REGRESSION_FAILED rules " + ruleFailure);
                Application.Quit(1);
                yield break;
            }
            EnterTacticalMap(FindHighReliefOperationalCell(), false);
            HexCoord enemyPosition = tacticalEnemyStates[0].Position;
            string reactorId = tacticalEnemyStates[0].Id;
            OpenTacticalMenu(new Vector2(640f, 360f));
            BeginTacticalMovePlanning();
            if (!tacticalMovePlanning || tacticalReachable.Count < 2)
            {
                Debug.LogError("ALWAYS_FAITHFUL_REACTION_REGRESSION_FAILED planning transition");
                Application.Quit(1);
                yield break;
            }
            HexCoord origin = tacticalUnitState.Position;
            HexCoord destination = origin;
            int expectedCost = -1;
            int bestDistance = int.MaxValue;
            foreach (KeyValuePair<HexCoord, int> pair in tacticalReachable)
            {
                if (pair.Key.Equals(origin)) continue;
                int distance = HexCoord.Distance(pair.Key, enemyPosition);
                if (distance > bestDistance) continue;
                bestDistance = distance;
                destination = pair.Key;
                expectedCost = pair.Value;
            }
            if (destination.Equals(origin))
            {
                Debug.LogError("ALWAYS_FAITHFUL_REACTION_REGRESSION_FAILED no reachable destination near enemy");
                Application.Quit(1);
                yield break;
            }
            int eventsBefore = tacticalBattlefield.ReactionEvents.Count;
            int actionPointsBefore = tacticalUnitState.RemainingActionPoints;
            int ammunitionBefore = tacticalEnemyWeapons[reactorId].RemainingAmmunition;
            if (!TryIssueTacticalMove(destination))
            {
                Debug.LogError($"ALWAYS_FAITHFUL_REACTION_REGRESSION_FAILED order destination={destination}");
                Application.Quit(1);
                yield break;
            }
            float deadline = Time.realtimeSinceStartup + 12f;
            while (tacticalUnitMoving && Time.realtimeSinceStartup < deadline) yield return null;
            if (tacticalUnitMoving)
            {
                Debug.LogError("ALWAYS_FAITHFUL_REACTION_REGRESSION_FAILED move did not resolve in time");
                Application.Quit(1);
                yield break;
            }
            int eventsAfter = tacticalBattlefield.ReactionEvents.Count;
            if (eventsAfter != eventsBefore + 1)
            {
                Debug.LogError($"ALWAYS_FAITHFUL_REACTION_REGRESSION_FAILED expected exactly one reaction event, delta={eventsAfter - eventsBefore}");
                Application.Quit(1);
                yield break;
            }
            TacticalReactionEvent reactionEvent = tacticalBattlefield.ReactionEvents[tacticalBattlefield.ReactionEvents.Count - 1];
            bool halted = reactionEvent.Outcome != TacticalFireOutcome.Miss;
            HexCoord expectedPosition = halted ? reactionEvent.TriggerPosition : destination;
            TacticalWeaponState reactorWeapon = tacticalEnemyWeapons[reactionEvent.ReactorId];
            if (reactionEvent.Outcome == TacticalFireOutcome.Rejected || reactionEvent.ReactorId != reactorId ||
                reactionEvent.Resolution != (halted ? "Halted" : "Resumed") ||
                !tacticalUnitState.Position.Equals(expectedPosition) ||
                tacticalUnitState.RemainingActionPoints != actionPointsBefore - expectedCost ||
                reactorWeapon.RemainingAmmunition != ammunitionBefore - 1)
            {
                Debug.LogError($"ALWAYS_FAITHFUL_REACTION_REGRESSION_FAILED resolution outcome={reactionEvent.Outcome} position={tacticalUnitState.Position} expected={expectedPosition} ap={tacticalUnitState.RemainingActionPoints} ammo={reactorWeapon.RemainingAmmunition}");
                Application.Quit(1);
                yield break;
            }

            string serialized = JsonUtility.ToJson(tacticalBattlefield);
            TacticalBattlefieldState restored = JsonUtility.FromJson<TacticalBattlefieldState>(serialized);
            if (restored == null || restored.SchemaVersion != TacticalBattlefieldState.CurrentSchemaVersion ||
                restored.ReactionEvents.Count != eventsAfter ||
                restored.ReactionEvents[restored.ReactionEvents.Count - 1].Seed != reactionEvent.Seed)
            {
                Debug.LogError($"ALWAYS_FAITHFUL_REACTION_REGRESSION_FAILED persistence restored={restored?.ReactionEvents.Count}");
                Application.Quit(1);
                yield break;
            }
            Debug.Log($"ALWAYS_FAITHFUL_REACTION_REGRESSION_OK reactor={reactionEvent.ReactorId} range={reactionEvent.RangeHexes} hit={reactionEvent.HitChance} roll={reactionEvent.Roll} outcome={reactionEvent.Outcome} resolution={reactionEvent.Resolution} position={tacticalUnitState.Position} ammo={reactorWeapon.RemainingAmmunition}");
            Application.Quit(0);
        }

        private IEnumerator RunSuppressionRegression()
        {
            yield return null;
            if (!ValidateSuppressionRules(out string failure))
            {
                Debug.LogError("ALWAYS_FAITHFUL_SUPPRESSION_REGRESSION_FAILED rules " + failure);
                Application.Quit(1);
                yield break;
            }
            EnterTacticalMap(FindHighReliefOperationalCell(), false);
            HexCellView target = FindObservedEnemyCell();
            BeginTacticalFirePlanning();
            PreviewTacticalFire(target);
            TacticalUnitState enemy = tacticalFireTarget;
            if (target == null || enemy == null || !TryIssueTacticalFire(target.Coord))
            {
                Debug.LogError("ALWAYS_FAITHFUL_SUPPRESSION_REGRESSION_FAILED fire setup rejected");
                Application.Quit(1);
                yield break;
            }
            float deadline = Time.realtimeSinceStartup + 2f;
            do { yield return null; } while (tacticalFireResolving && Time.realtimeSinceStartup < deadline);
            TacticalFireEvent fireEvent = tacticalBattlefield.FireEvents[tacticalBattlefield.FireEvents.Count - 1];
            ContactMarkerView targetView = tacticalContactViews[enemy.Id];
            int expectedPoints = TacticalSuppression.PointsForFireOutcome(fireEvent.Outcome);
            int eventsAfterFire = expectedPoints > 0 ? 1 : 0;
            if (fireEvent.Outcome == TacticalFireOutcome.Rejected || enemy.SuppressionPoints != expectedPoints ||
                tacticalBattlefield.SuppressionEvents.Count != eventsAfterFire ||
                (expectedPoints > 0 && targetView.PresentedStatus != enemy.CombatStatus))
            {
                Debug.LogError($"ALWAYS_FAITHFUL_SUPPRESSION_REGRESSION_FAILED fire outcome={fireEvent.Outcome} points={enemy.SuppressionPoints} events={tacticalBattlefield.SuppressionEvents.Count} viewStatus={targetView.PresentedStatus}");
                Application.Quit(1);
                yield break;
            }

            TacticalSuppression.ApplyFireOutcome(tacticalUnitState, TacticalFireOutcome.Suppressed);
            TacticalSuppression.ApplyFireOutcome(tacticalUnitState, TacticalFireOutcome.Suppressed);
            TacticalSuppression.ApplyFireOutcome(tacticalUnitState, TacticalFireOutcome.Hit);
            tacticalUnit.Present(tacticalUnitState);
            tacticalFormationView.Present(tacticalUnitState);
            if (tacticalUnitState.SuppressionPoints != 95 || tacticalUnitState.CombatStatus != TacticalCombatStatus.Reduced ||
                tacticalUnitState.CanMove || tacticalUnitState.CanFire)
            {
                Debug.LogError($"ALWAYS_FAITHFUL_SUPPRESSION_REGRESSION_FAILED cumulative points={tacticalUnitState.SuppressionPoints} status={tacticalUnitState.CombatStatus}");
                Application.Quit(1);
                yield break;
            }
            BeginTacticalMovePlanning();
            BeginTacticalFirePlanning();
            if (tacticalMovePlanning || tacticalFirePlanning)
            {
                Debug.LogError("ALWAYS_FAITHFUL_SUPPRESSION_REGRESSION_FAILED reduced unit accepted an order");
                Application.Quit(1);
                yield break;
            }

            IssueTacticalRally();
            int eventsAfterRally = eventsAfterFire + 1;
            if (tacticalUnitState.SuppressionPoints != 55 || tacticalUnitState.CombatStatus != TacticalCombatStatus.Disrupted ||
                tacticalBattlefield.SuppressionEvents.Count != eventsAfterRally || tacticalUnitState.CanFire)
            {
                Debug.LogError($"ALWAYS_FAITHFUL_SUPPRESSION_REGRESSION_FAILED rally points={tacticalUnitState.SuppressionPoints} status={tacticalUnitState.CombatStatus} events={tacticalBattlefield.SuppressionEvents.Count}");
                Application.Quit(1);
                yield break;
            }

            tacticalUnitState.BeginTurn();
            enemy.BeginTurn();
            if (tacticalUnitState.SuppressionPoints != 40 || tacticalUnitState.CombatStatus != TacticalCombatStatus.Suppressed ||
                enemy.SuppressionPoints != Mathf.Max(0, expectedPoints - TacticalSuppression.PassiveRecoveryAmount))
            {
                Debug.LogError($"ALWAYS_FAITHFUL_SUPPRESSION_REGRESSION_FAILED passive recovery unit={tacticalUnitState.SuppressionPoints}/{tacticalUnitState.CombatStatus} enemy={enemy.SuppressionPoints}");
                Application.Quit(1);
                yield break;
            }

            string serialized = JsonUtility.ToJson(tacticalBattlefield);
            TacticalBattlefieldState restored = JsonUtility.FromJson<TacticalBattlefieldState>(serialized);
            TacticalSuppressionEvent restoredRally = restored?.SuppressionEvents.Find(item => item.Cause == "Rally");
            if (restored == null || restored.SchemaVersion != TacticalBattlefieldState.CurrentSchemaVersion ||
                restored.SuppressionEvents.Count != eventsAfterRally || restoredRally == null ||
                restoredRally.UnitId != tacticalUnitState.Id || restoredRally.PointsBefore != 95 || restoredRally.PointsAfter != 55)
            {
                Debug.LogError($"ALWAYS_FAITHFUL_SUPPRESSION_REGRESSION_FAILED persistence restored={restored?.SuppressionEvents.Count} schema={restored?.SchemaVersion}");
                Application.Quit(1);
                yield break;
            }
            Debug.Log($"ALWAYS_FAITHFUL_SUPPRESSION_REGRESSION_OK enemyPoints={enemy.SuppressionPoints} enemyStatus={enemy.CombatStatus} unitPoints={tacticalUnitState.SuppressionPoints} unitStatus={tacticalUnitState.CombatStatus} events={tacticalBattlefield.SuppressionEvents.Count}");
            Application.Quit(0);
        }

        private IEnumerator RunEnemyTurnRegression()
        {
            yield return null;
            if (!ValidateEnemyTurnRules(out string failure, out long elapsedMilliseconds))
            {
                Debug.LogError($"ALWAYS_FAITHFUL_ENEMY_TURN_REGRESSION_FAILED rules={failure} elapsedMs={elapsedMilliseconds}");
                Application.Quit(1);
                yield break;
            }

            EnterTacticalMap(FindHighReliefOperationalCell(), false);
            fastEnemyAnimation = true;
            int startingTurn = turnState.TurnNumber;
            int actionsBefore = tacticalBattlefield.EnemyActionEvents.Count;
            EndTacticalTurn();
            float deadline = Time.realtimeSinceStartup + 5f;
            do { yield return null; } while (tacticalEnemyTurnActive && Time.realtimeSinceStartup < deadline);
            string serialized = JsonUtility.ToJson(tacticalBattlefield);
            TacticalBattlefieldState restored = JsonUtility.FromJson<TacticalBattlefieldState>(serialized);
            if (tacticalEnemyTurnActive || turnState.ActiveSide != "USMC" || turnState.TurnNumber != startingTurn + 1 ||
                tacticalBattlefield.EnemyActionEvents.Count != actionsBefore + tacticalEnemyStates.Count ||
                tacticalUnitState.RemainingActionPoints != tacticalUnitState.MaximumActionPoints ||
                restored == null || restored.EnemyActionEvents.Count != tacticalEnemyStates.Count)
            {
                Debug.LogError($"ALWAYS_FAITHFUL_ENEMY_TURN_REGRESSION_FAILED presentation active={tacticalEnemyTurnActive} side={turnState.ActiveSide} turn={turnState.TurnNumber}/{startingTurn + 1} actions={tacticalBattlefield.EnemyActionEvents.Count - actionsBefore} restored={restored?.EnemyActionEvents.Count}");
                Application.Quit(1);
                yield break;
            }
            Debug.Log($"ALWAYS_FAITHFUL_ENEMY_TURN_REGRESSION_OK actions={tacticalEnemyStates.Count} hidden={tacticalHiddenEnemyActions} turn={startingTurn}->{turnState.TurnNumber} side={turnState.ActiveSide} planningMs={elapsedMilliseconds} schema={restored.SchemaVersion}");
            Application.Quit(0);
        }

        private static bool ValidateEnemyTurnRules(out string failure, out long elapsedMilliseconds)
        {
            Dictionary<HexCoord, TacticalMovementCell> board = BuildEnemyTurnFixture(16);
            var mover = new TacticalUnitState("enemy", "Enemy", new HexCoord(0, 0), 4);
            var opponent = new TacticalUnitState("friendly", "Friendly", new HexCoord(15, 0), 8);
            board[mover.Position].OccupantId = mover.Id;
            board[opponent.Position].OccupantId = opponent.Id;
            var weapon = new TacticalWeaponState("enemy-rifle", "Rifle", 6);
            TacticalContactState moverContact = TacticalObservation.Check(board, mover.Id, mover.Position,
                opponent.Id, opponent.DisplayName, opponent.Position, 1);
            var watch = System.Diagnostics.Stopwatch.StartNew();
            TacticalAiOrder first = TacticalEnemyTurn.PlanOrder(board, mover, weapon, opponent.Position, opponent, moverContact, 1, 404);
            TacticalAiOrder replay = TacticalEnemyTurn.PlanOrder(board, mover, weapon, opponent.Position, opponent, moverContact, 1, 404);
            for (int index = 0; index < 100; index++)
                TacticalEnemyTurn.PlanOrder(board, mover, weapon, opponent.Position, opponent, moverContact, 1, 404 + index);
            watch.Stop();
            elapsedMilliseconds = watch.ElapsedMilliseconds;
            if (first.Kind != TacticalAiOrderKind.Move || JsonUtility.ToJson(first) != JsonUtility.ToJson(replay) ||
                HexCoord.Distance(first.Destination, opponent.Position) >= HexCoord.Distance(mover.Position, opponent.Position) ||
                !TacticalEnemyTurn.ValidateOrder(board, first, mover, weapon, opponent, moverContact, 1, out _))
            {
                failure = "objective movement or fixed-seed replay";
                return false;
            }

            TacticalAiOrder illegal = new TacticalAiOrder
            {
                UnitId = mover.Id,
                Kind = TacticalAiOrderKind.Move,
                Origin = mover.Position,
                Destination = opponent.Position,
                ActionPointCost = 1
            };
            if (TacticalEnemyTurn.ValidateOrder(board, illegal, mover, weapon, opponent, moverContact, 1, out _))
            {
                failure = "illegal AI order accepted";
                return false;
            }

            var recovering = new TacticalUnitState("recover", "Recovering", new HexCoord(1, 0), 4);
            recovering.ApplySuppressionPoints(TacticalSuppression.DisruptedThreshold);
            TacticalContactState recoveringContact = TacticalObservation.Check(board, recovering.Id, recovering.Position,
                opponent.Id, opponent.DisplayName, opponent.Position, 1);
            TacticalAiOrder recovery = TacticalEnemyTurn.PlanOrder(board, recovering, weapon,
                opponent.Position, opponent, recoveringContact, 1, 11);
            if (recovery.Kind != TacticalAiOrderKind.Recover)
            {
                failure = "recovery priority";
                return false;
            }

            var firer = new TacticalUnitState("firer", "Firer", new HexCoord(13, 0), 4);
            TacticalContactState firerContact = TacticalObservation.Check(board, firer.Id, firer.Position,
                opponent.Id, opponent.DisplayName, opponent.Position, 1);
            TacticalAiOrder fire = TacticalEnemyTurn.PlanOrder(board, firer, weapon,
                opponent.Position, opponent, firerContact, 1, 12);
            if (fire.Kind != TacticalAiOrderKind.Fire ||
                !TacticalEnemyTurn.ValidateOrder(board, fire, firer, weapon, opponent, firerContact, 1, out _))
            {
                failure = "legal direct-fire selection";
                return false;
            }

            var observer = new TacticalUnitState("observer", "Observer", new HexCoord(6, 0), 4);
            TacticalContactState observerContact = TacticalObservation.Check(board, observer.Id, observer.Position,
                opponent.Id, opponent.DisplayName, opponent.Position, 1);
            TacticalAiOrder observe = TacticalEnemyTurn.PlanOrder(board, observer, weapon,
                observer.Position, opponent, observerContact, 1, 13);
            if (observe.Kind != TacticalAiOrderKind.Observe)
            {
                failure = "observation fallback";
                return false;
            }

            // A recently lost track (still within TacticalObservation.Check's
            // one-turn stale-carryover window) should send the AI to reacquire
            // it via Recon rather than blindly advancing toward the objective.
            var reconMover = new TacticalUnitState("recon-mover", "ReconMover", new HexCoord(2, 0), 4);
            var staleContact = new TacticalContactState
            {
                TargetId = opponent.Id,
                DisplayName = opponent.DisplayName,
                State = TacticalVisibilityState.Contact,
                LastKnownPosition = new HexCoord(4, 0),
                LastObservedTurn = 1,
                IsStale = true,
                ObserverId = reconMover.Id,
                RangeHexes = 2,
                LineOfSight = TacticalLosState.Blocked
            };
            TacticalAiOrder recon = TacticalEnemyTurn.PlanOrder(board, reconMover, weapon,
                opponent.Position, opponent, staleContact, 2, 14);
            if (recon.Kind != TacticalAiOrderKind.Recon || !recon.Destination.Equals(staleContact.LastKnownPosition) ||
                !TacticalEnemyTurn.ValidateOrder(board, recon, reconMover, weapon, opponent, staleContact, 2, out _))
            {
                failure = "PLA recon reacquire selection";
                return false;
            }
            if (elapsedMilliseconds > TacticalEnemyTurn.PlanningBudgetMilliseconds)
            {
                failure = $"planning budget exceeded ({elapsedMilliseconds} ms)";
                return false;
            }
            failure = null;
            return true;
        }

        private static Dictionary<HexCoord, TacticalMovementCell> BuildEnemyTurnFixture(int width)
        {
            var board = new Dictionary<HexCoord, TacticalMovementCell>();
            for (int q = 0; q < width; q++)
            {
                for (int r = -2; r <= 2; r++)
                {
                    var coord = new HexCoord(q, r);
                    board[coord] = new TacticalMovementCell
                    {
                        Coord = coord,
                        Terrain = q % 5 == 2 && r == 1 ? TacticalTerrain.Rough : TacticalTerrain.Open,
                        ElevationMetres = 20f
                    };
                }
            }
            return board;
        }

        private IEnumerator RunCoverRegression()
        {
            yield return null;
            if (!ValidateCoverRules(out string ruleFailure))
            {
                Debug.LogError("ALWAYS_FAITHFUL_COVER_REGRESSION_FAILED rules=" + ruleFailure);
                Application.Quit(1);
                yield break;
            }

            HexCellView parent = FindCoastalOperationalCell();
            TacticalBattlefieldState battlefield = TacticalBattlefieldExtractor.Extract(
                parent.Coord, parent.Longitude, parent.Latitude,
                (longitude, latitude) => elevation.SampleMetres(longitude, latitude),
                (longitude, latitude) => coastline.ContainsLand(longitude, latitude));

            var coversSeen = new HashSet<TacticalCover>();
            var waterCoords = new List<HexCoord>();
            foreach (TacticalBattlefieldCell cell in battlefield.Cells)
                if (cell.Terrain == TacticalTerrain.Water) waterCoords.Add(cell.LocalCoord);
            int builtUpCount = 0;
            foreach (TacticalBattlefieldCell cell in battlefield.Cells)
            {
                coversSeen.Add(cell.Cover);
                if (!cell.IsBuiltUp) continue;
                builtUpCount++;
                int shoreDistance = int.MaxValue;
                foreach (HexCoord water in waterCoords)
                    shoreDistance = Math.Min(shoreDistance, HexCoord.Distance(cell.LocalCoord, water));
                if (shoreDistance > TacticalBattlefieldExtractor.BuiltUpShoreBandHexes)
                {
                    Debug.LogError($"ALWAYS_FAITHFUL_COVER_REGRESSION_FAILED built-up cell {cell.LocalCoord} is {shoreDistance} hexes from shore, beyond the {TacticalBattlefieldExtractor.BuiltUpShoreBandHexes}-hex band");
                    Application.Quit(1);
                    yield break;
                }
            }
            if (coversSeen.Count < 4)
            {
                Debug.LogError($"ALWAYS_FAITHFUL_COVER_REGRESSION_FAILED only {coversSeen.Count}/4 cover levels appeared on {battlefield.BattlefieldId}");
                Application.Quit(1);
                yield break;
            }

            Debug.Log($"ALWAYS_FAITHFUL_COVER_REGRESSION_OK battlefield={battlefield.BattlefieldId} coverLevels={coversSeen.Count} builtUp={builtUpCount}");
            Application.Quit(0);
        }

        private static bool ValidateCoverRules(out string failure)
        {
            Dictionary<HexCoord, TacticalMovementCell> board = BuildCoverFixture(8);
            var attackerPosition = new HexCoord(2, 0);
            var noneTargetPosition = new HexCoord(4, 0);
            var heavyTargetPosition = new HexCoord(0, 0);
            board[heavyTargetPosition].Cover = TacticalCover.Heavy;
            var weapon = new TacticalWeaponState("cover-test-rifle", "Rifle", 6);

            var noneContact = new TacticalContactState { TargetId = "none-target", State = TacticalVisibilityState.Observed, LastKnownPosition = noneTargetPosition };
            var heavyContact = new TacticalContactState { TargetId = "heavy-target", State = TacticalVisibilityState.Observed, LastKnownPosition = heavyTargetPosition };
            TacticalFirePreview noneFire = TacticalDirectFire.Preview(board, "attacker", attackerPosition, noneContact, noneTargetPosition, weapon, 8);
            TacticalFirePreview heavyFire = TacticalDirectFire.Preview(board, "attacker", attackerPosition, heavyContact, heavyTargetPosition, weapon, 8);
            if (!noneFire.IsValid || !heavyFire.IsValid || heavyFire.HitChance != noneFire.HitChance + TacticalDirectFire.HeavyCoverHitPenalty)
            {
                failure = $"direct fire cover penalty none={noneFire.HitChance} heavy={heavyFire.HitChance}";
                return false;
            }

            var reactor = new TacticalUnitState("reactor", "Reactor", attackerPosition, 4);
            TacticalLosResult losToNone = TacticalLineOfSight.Inspect(board, attackerPosition, noneTargetPosition, TacticalReactionFire.ReactionRangeHexes);
            TacticalLosResult losToHeavy = TacticalLineOfSight.Inspect(board, attackerPosition, heavyTargetPosition, TacticalReactionFire.ReactionRangeHexes);
            TacticalFirePreview noneReaction = TacticalReactionFire.Preview(board, reactor, weapon, noneTargetPosition, losToNone);
            TacticalFirePreview heavyReaction = TacticalReactionFire.Preview(board, reactor, weapon, heavyTargetPosition, losToHeavy);
            if (!noneReaction.IsValid || !heavyReaction.IsValid || heavyReaction.HitChance != noneReaction.HitChance + TacticalDirectFire.HeavyCoverHitPenalty)
            {
                failure = $"reaction fire cover penalty none={noneReaction.HitChance} heavy={heavyReaction.HitChance}";
                return false;
            }

            var losObserver = new HexCoord(2, 0);
            var losTarget = new HexCoord(4, 0);
            List<HexCoord> line = TacticalLineOfSight.Trace(losObserver, losTarget);
            if (line.Count < 3)
            {
                failure = "LOS fixture too short for an intervening cell";
                return false;
            }
            HexCoord intervening = line[1];
            TacticalLosResult beforeCover = TacticalLineOfSight.Inspect(board, losObserver, losTarget);
            if (!beforeCover.IsValid || beforeCover.State != TacticalLosState.Clear)
            {
                failure = "LOS baseline was not clear before adding cover";
                return false;
            }
            board[intervening].Cover = TacticalCover.Light;
            TacticalLosResult afterCover = TacticalLineOfSight.Inspect(board, losObserver, losTarget);
            board[intervening].Cover = TacticalCover.None;
            if (!afterCover.IsValid || afterCover.State == TacticalLosState.Clear)
            {
                failure = "intervening cover alone did not degrade an otherwise-clear sightline";
                return false;
            }

            failure = null;
            return true;
        }

        private static Dictionary<HexCoord, TacticalMovementCell> BuildCoverFixture(int width)
        {
            var board = new Dictionary<HexCoord, TacticalMovementCell>();
            for (int q = 0; q < width; q++)
                for (int r = -2; r <= 2; r++)
                {
                    var coord = new HexCoord(q, r);
                    board[coord] = new TacticalMovementCell { Coord = coord, Terrain = TacticalTerrain.Open, ElevationMetres = 20f };
                }
            return board;
        }

        private IEnumerator RunVictoryRegression()
        {
            yield return null;
            if (!ValidateVictoryRules(out string ruleFailure))
            {
                Debug.LogError("ALWAYS_FAITHFUL_VICTORY_REGRESSION_FAILED rules=" + ruleFailure);
                Application.Quit(1);
                yield break;
            }

            EnterTacticalMap(FindHighReliefOperationalCell(), false);
            fastEnemyAnimation = true;
            tacticalObjective.TurnLimit = 1;
            EndTacticalTurn();
            float deadline = Time.realtimeSinceStartup + 5f;
            do { yield return null; } while ((tacticalEnemyTurnActive || !resultScreenActive || resultScreenOpacity < .98f) && Time.realtimeSinceStartup < deadline);

            if (tacticalObjective.Outcome == TacticalBattleOutcome.InProgress || !resultScreenActive || resultScreenOpacity < .98f)
            {
                Debug.LogError($"ALWAYS_FAITHFUL_VICTORY_REGRESSION_FAILED outcome={tacticalObjective.Outcome} screenActive={resultScreenActive} opacity={resultScreenOpacity}");
                Application.Quit(1);
                yield break;
            }

            int mergedEventCount = tacticalBattlefield.MovementEvents.Count + tacticalBattlefield.FireEvents.Count +
                tacticalBattlefield.SuppressionEvents.Count + tacticalBattlefield.ReactionEvents.Count + tacticalBattlefield.EnemyActionEvents.Count +
                tacticalBattlefield.ReconEvents.Count + tacticalBattlefield.ObjectiveEvents.Count +
                (tacticalObjective.Outcome != TacticalBattleOutcome.InProgress ? 1 : 0);
            List<string> log = BuildTacticalEventLog(int.MaxValue);
            if (log.Count != mergedEventCount)
            {
                Debug.LogError($"ALWAYS_FAITHFUL_VICTORY_REGRESSION_FAILED event log count mismatch merged={log.Count} expected={mergedEventCount}");
                Application.Quit(1);
                yield break;
            }

            string serialized = JsonUtility.ToJson(tacticalBattlefield);
            TacticalBattlefieldState restored = JsonUtility.FromJson<TacticalBattlefieldState>(serialized);
            if (restored?.Objective == null || restored.Objective.Outcome != tacticalObjective.Outcome ||
                restored.SchemaVersion != TacticalBattlefieldState.CurrentSchemaVersion)
            {
                Debug.LogError($"ALWAYS_FAITHFUL_VICTORY_REGRESSION_FAILED objective round-trip outcome={restored?.Objective?.Outcome} expected={tacticalObjective.Outcome} schema={restored?.SchemaVersion}");
                Application.Quit(1);
                yield break;
            }

            Debug.Log($"ALWAYS_FAITHFUL_VICTORY_REGRESSION_OK outcome={tacticalObjective.Outcome} posture={tacticalObjective.Posture} objective={tacticalObjective.ObjectiveHex} logEntries={log.Count} schema={restored.SchemaVersion}");
            Application.Quit(0);
        }

        private static bool ValidateVictoryRules(out string failure)
        {
            Dictionary<HexCoord, TacticalMovementCell> board = BuildVictoryFixture(14, -4, 4);
            var usmc = new TacticalUnitState("usmc", "USMC", new HexCoord(1, 0), 8);
            var enemyA = new TacticalUnitState("pla-a", "PLA A", new HexCoord(2, 0), 4);
            var enemyB = new TacticalUnitState("pla-b", "PLA B", new HexCoord(3, 0), 4);
            var enemies = new List<TacticalUnitState> { enemyA, enemyB };
            var objective = new TacticalObjectiveState
            {
                ObjectiveHex = new HexCoord(11, 0),
                Posture = TacticalPosture.Attack,
                TurnLimit = 4,
                BattleStartTurn = 1
            };

            TacticalBattleOutcome outcome = TacticalVictory.Evaluate(objective, usmc, enemies, board, 2, out _);
            if (outcome != TacticalBattleOutcome.InProgress)
            {
                failure = $"expected InProgress before the turn limit, got {outcome}";
                return false;
            }

            outcome = TacticalVictory.Evaluate(objective, usmc, enemies, board, objective.BattleStartTurn + objective.TurnLimit, out _);
            if (outcome != TacticalBattleOutcome.Stalemate)
            {
                failure = $"expected Stalemate at the turn limit without control, got {outcome}";
                return false;
            }

            board[objective.ObjectiveHex].OccupantId = usmc.Id;
            outcome = TacticalVictory.Evaluate(objective, usmc, enemies, board, objective.BattleStartTurn + objective.TurnLimit, out _);
            board[objective.ObjectiveHex].OccupantId = null;
            if (outcome != TacticalBattleOutcome.UsmcVictory)
            {
                failure = $"expected UsmcVictory at the turn limit with control, got {outcome}";
                return false;
            }

            usmc.ApplySuppressionPoints(TacticalSuppression.MaximumPoints);
            outcome = TacticalVictory.Evaluate(objective, usmc, enemies, board, 2, out _);
            if (outcome != TacticalBattleOutcome.UsmcDefeat)
            {
                failure = $"expected UsmcDefeat when USMC alone is reduced, got {outcome}";
                return false;
            }

            enemyA.ApplySuppressionPoints(TacticalSuppression.MaximumPoints);
            enemyB.ApplySuppressionPoints(TacticalSuppression.MaximumPoints);
            outcome = TacticalVictory.Evaluate(objective, usmc, enemies, board, 2, out _);
            if (outcome != TacticalBattleOutcome.Draw)
            {
                failure = $"expected Draw on mutual destruction, got {outcome}";
                return false;
            }

            var freshUsmc = new TacticalUnitState("usmc-2", "USMC 2", new HexCoord(1, 0), 8);
            outcome = TacticalVictory.Evaluate(objective, freshUsmc, enemies, board, 2, out _);
            if (outcome != TacticalBattleOutcome.UsmcVictory)
            {
                failure = $"expected UsmcVictory when all enemies alone are reduced, got {outcome}";
                return false;
            }

            var center = new HexCoord(6, 0);
            var enemyStarts = new List<HexCoord> { new HexCoord(7, 0), new HexCoord(5, 1) };
            HexCoord attackObjective = TacticalVictory.ChooseObjective(board, center, TacticalPosture.Attack, enemyStarts, "TEST-BATTLEFIELD");
            HexCoord attackObjectiveRepeat = TacticalVictory.ChooseObjective(board, center, TacticalPosture.Attack, enemyStarts, "TEST-BATTLEFIELD");
            if (!attackObjective.Equals(attackObjectiveRepeat))
            {
                failure = "attack objective selection is not deterministic";
                return false;
            }
            if (HexCoord.Distance(attackObjective, center) <= TacticalVictory.ObjectiveExclusionRadiusHexes)
            {
                failure = $"attack objective {attackObjective} too close to center {center}";
                return false;
            }
            foreach (HexCoord start in enemyStarts)
            {
                if (HexCoord.Distance(attackObjective, start) > TacticalVictory.ObjectiveExclusionRadiusHexes) continue;
                failure = $"attack objective {attackObjective} too close to enemy start {start}";
                return false;
            }

            HexCoord defendObjective = TacticalVictory.ChooseObjective(board, center, TacticalPosture.Defend, enemyStarts, "TEST-BATTLEFIELD");
            if (!defendObjective.Equals(center))
            {
                failure = $"defend objective {defendObjective} did not return center {center}";
                return false;
            }

            // Raid: victory latches once at least half the enemy roster is
            // reduced, and stays latched even if the enemy later recovers.
            var raidUsmc = new TacticalUnitState("raid-usmc", "USMC", new HexCoord(1, 0), 8);
            var raidEnemyA = new TacticalUnitState("raid-pla-a", "PLA A", new HexCoord(2, 0), 4);
            var raidEnemyB = new TacticalUnitState("raid-pla-b", "PLA B", new HexCoord(3, 0), 4);
            var raidEnemies = new List<TacticalUnitState> { raidEnemyA, raidEnemyB };
            var raidObjective = new TacticalObjectiveState
            {
                ObjectiveHex = new HexCoord(11, 0),
                MissionType = TacticalMissionType.Raid,
                TurnLimit = 4,
                BattleStartTurn = 1
            };
            outcome = TacticalVictory.Evaluate(raidObjective, raidUsmc, raidEnemies, board, 2, out _);
            if (outcome != TacticalBattleOutcome.InProgress || raidObjective.RaidObjectiveAchieved)
            {
                failure = $"expected Raid InProgress with no casualties yet, got {outcome} achieved={raidObjective.RaidObjectiveAchieved}";
                return false;
            }
            raidEnemyA.ApplySuppressionPoints(TacticalSuppression.MaximumPoints);
            outcome = TacticalVictory.Evaluate(raidObjective, raidUsmc, raidEnemies, board, 2, out _);
            if (outcome != TacticalBattleOutcome.UsmcVictory || !raidObjective.RaidObjectiveAchieved)
            {
                failure = $"expected Raid UsmcVictory once half the roster is reduced, got {outcome} achieved={raidObjective.RaidObjectiveAchieved}";
                return false;
            }
            raidEnemyA.ApplySuppressionPoints(-TacticalSuppression.MaximumPoints);
            outcome = TacticalVictory.Evaluate(raidObjective, raidUsmc, raidEnemies, board, 2, out _);
            if (outcome != TacticalBattleOutcome.UsmcVictory)
            {
                failure = $"expected Raid victory to stay latched after enemy recovery, got {outcome}";
                return false;
            }

            // ReconInForce: victory requires every enemy identified at least once.
            var reconUsmc = new TacticalUnitState("recon-usmc", "USMC", new HexCoord(1, 0), 8);
            var reconEnemyA = new TacticalUnitState("recon-pla-a", "PLA A", new HexCoord(2, 0), 4);
            var reconEnemyB = new TacticalUnitState("recon-pla-b", "PLA B", new HexCoord(3, 0), 4);
            var reconEnemies = new List<TacticalUnitState> { reconEnemyA, reconEnemyB };
            var reconObjective = new TacticalObjectiveState
            {
                ObjectiveHex = new HexCoord(11, 0),
                MissionType = TacticalMissionType.ReconInForce,
                TurnLimit = 4,
                BattleStartTurn = 1
            };
            outcome = TacticalVictory.Evaluate(reconObjective, reconUsmc, reconEnemies, board, 2, out _);
            if (outcome != TacticalBattleOutcome.InProgress)
            {
                failure = $"expected ReconInForce InProgress with nothing identified, got {outcome}";
                return false;
            }
            reconObjective.ObservedEnemyIds.Add(reconEnemyA.Id);
            outcome = TacticalVictory.Evaluate(reconObjective, reconUsmc, reconEnemies, board, 2, out _);
            if (outcome != TacticalBattleOutcome.InProgress)
            {
                failure = $"expected ReconInForce InProgress with only one of two identified, got {outcome}";
                return false;
            }
            reconObjective.ObservedEnemyIds.Add(reconEnemyB.Id);
            outcome = TacticalVictory.Evaluate(reconObjective, reconUsmc, reconEnemies, board, 2, out _);
            if (outcome != TacticalBattleOutcome.UsmcVictory)
            {
                failure = $"expected ReconInForce UsmcVictory once the full roster is identified, got {outcome}";
                return false;
            }

            // Withdrawal: victory at the turn limit requires only survival, not
            // holding any particular hex.
            var withdrawUsmc = new TacticalUnitState("withdraw-usmc", "USMC", new HexCoord(1, 0), 8);
            var withdrawEnemyA = new TacticalUnitState("withdraw-pla-a", "PLA A", new HexCoord(2, 0), 4);
            var withdrawEnemies = new List<TacticalUnitState> { withdrawEnemyA };
            var withdrawObjective = new TacticalObjectiveState
            {
                ObjectiveHex = new HexCoord(11, 0), // deliberately never occupied by withdrawUsmc
                MissionType = TacticalMissionType.Withdrawal,
                TurnLimit = 4,
                BattleStartTurn = 1
            };
            outcome = TacticalVictory.Evaluate(withdrawObjective, withdrawUsmc, withdrawEnemies, board, withdrawObjective.BattleStartTurn + withdrawObjective.TurnLimit, out _);
            if (outcome != TacticalBattleOutcome.UsmcVictory)
            {
                failure = $"expected Withdrawal UsmcVictory at the turn limit regardless of hex control, got {outcome}";
                return false;
            }

            failure = null;
            return true;
        }

        private static Dictionary<HexCoord, TacticalMovementCell> BuildVictoryFixture(int width, int rMin, int rMax)
        {
            var board = new Dictionary<HexCoord, TacticalMovementCell>();
            for (int q = 0; q < width; q++)
                for (int r = rMin; r <= rMax; r++)
                {
                    var coord = new HexCoord(q, r);
                    board[coord] = new TacticalMovementCell { Coord = coord, Terrain = TacticalTerrain.Open, ElevationMetres = 20f };
                }
            return board;
        }

        private IEnumerator RunReconRegression()
        {
            yield return null;
            if (!ValidateReconRules(out string ruleFailure))
            {
                Debug.LogError("ALWAYS_FAITHFUL_RECON_REGRESSION_FAILED rules=" + ruleFailure);
                Application.Quit(1);
                yield break;
            }

            EnterTacticalMap(FindHighReliefOperationalCell(), false);
            HexCoord target = tacticalEnemyStates[0].Position;
            int apBefore = tacticalUnitState.RemainingActionPoints;
            int eventsBefore = tacticalBattlefield.ReconEvents.Count;
            BeginTacticalReconPlanning();
            if (!tacticalReconPlanning || !TryIssueTacticalRecon(target))
            {
                Debug.LogError("ALWAYS_FAITHFUL_RECON_REGRESSION_FAILED order was not accepted");
                Application.Quit(1);
                yield break;
            }
            if (tacticalUnitState.RemainingActionPoints != apBefore - TacticalRecon.ActionPointCost ||
                tacticalBattlefield.ReconEvents.Count != eventsBefore + 1 ||
                tacticalReconMarkers.Count != 1 || tacticalReconMarkers[0].TurnsRemaining != TacticalRecon.DurationTurns ||
                !tacticalReconRings.ContainsKey(target))
            {
                Debug.LogError($"ALWAYS_FAITHFUL_RECON_REGRESSION_FAILED commit state ap={tacticalUnitState.RemainingActionPoints}/{apBefore - TacticalRecon.ActionPointCost} events={tacticalBattlefield.ReconEvents.Count}/{eventsBefore + 1} markers={tacticalReconMarkers.Count}");
                Application.Quit(1);
                yield break;
            }

            fastEnemyAnimation = true;
            for (int round = 0; round < TacticalRecon.DurationTurns; round++)
            {
                EndTacticalTurn();
                float deadline = Time.realtimeSinceStartup + 5f;
                do { yield return null; } while (tacticalEnemyTurnActive && Time.realtimeSinceStartup < deadline);
            }

            if (tacticalReconMarkers.Count != 0 || tacticalReconRings.ContainsKey(target))
            {
                Debug.LogError($"ALWAYS_FAITHFUL_RECON_REGRESSION_FAILED marker did not expire after {TacticalRecon.DurationTurns} rounds markers={tacticalReconMarkers.Count}");
                Application.Quit(1);
                yield break;
            }

            Debug.Log($"ALWAYS_FAITHFUL_RECON_REGRESSION_OK target={target} apCost={TacticalRecon.ActionPointCost} duration={TacticalRecon.DurationTurns}");
            Application.Quit(0);
        }

        private static bool ValidateReconRules(out string failure)
        {
            var markers = new List<TacticalReconMarker> { new TacticalReconMarker { Hex = new HexCoord(3, 0), TurnsRemaining = 2 } };
            if (!TacticalRecon.IsUnderActiveRecon(markers, new HexCoord(3, 0)))
            {
                failure = "expected the tasked hex to report as under active recon";
                return false;
            }
            if (TacticalRecon.IsUnderActiveRecon(markers, new HexCoord(4, 0)))
            {
                failure = "an untasked hex incorrectly reported as under active recon";
                return false;
            }

            TacticalVisibilityState upgraded = TacticalRecon.ApplyBonus(TacticalVisibilityState.Hidden, true);
            if (upgraded != TacticalVisibilityState.Contact)
            {
                failure = $"expected Hidden->Contact, got {upgraded}";
                return false;
            }
            upgraded = TacticalRecon.ApplyBonus(TacticalVisibilityState.Contact, true);
            if (upgraded != TacticalVisibilityState.Identified)
            {
                failure = $"expected Contact->Identified, got {upgraded}";
                return false;
            }
            upgraded = TacticalRecon.ApplyBonus(TacticalVisibilityState.Identified, true);
            if (upgraded != TacticalVisibilityState.Observed)
            {
                failure = $"expected Identified->Observed, got {upgraded}";
                return false;
            }
            upgraded = TacticalRecon.ApplyBonus(TacticalVisibilityState.Observed, true);
            if (upgraded != TacticalVisibilityState.Observed)
            {
                failure = $"expected Observed to stay capped, got {upgraded}";
                return false;
            }
            upgraded = TacticalRecon.ApplyBonus(TacticalVisibilityState.Hidden, false);
            if (upgraded != TacticalVisibilityState.Hidden)
            {
                failure = $"expected no bonus without active recon, got {upgraded}";
                return false;
            }

            failure = null;
            return true;
        }

        private IEnumerator RunBattleContractRegression()
        {
            yield return null;
            if (!ValidateBattleContractRules(out string ruleFailure))
            {
                Debug.LogError("ALWAYS_FAITHFUL_BATTLE_CONTRACT_REGRESSION_FAILED rules=" + ruleFailure);
                Application.Quit(1);
                yield break;
            }

            HexCoord theaterHex = FindHighReliefOperationalCell().Coord;
            string scratchDirectory = Path.Combine(Path.GetTempPath(), "AlwaysFaithfulBattleContractRegression");
            Directory.CreateDirectory(scratchDirectory);
            string requestPath = Path.Combine(scratchDirectory, "request.json");
            string outputPath = Path.Combine(scratchDirectory, "result.json");
            var request = new BattleRequest
            {
                RequestId = "regression-request-1",
                CampaignId = "regression-campaign-1",
                Seed = 4242,
                TheaterHex = theaterHex,
                HasPostureOverride = true,
                Posture = TacticalPosture.Defend,
                TurnLimitOverride = 1,
                OutputPath = outputPath,
                Forces = new List<BattleUnitImport>
                {
                    new BattleUnitImport { Role = "usmc-rifle-platoon", UnitId = "regression-usmc-1", DisplayName = "Regression Rifle Platoon" },
                    new BattleUnitImport { Role = "pla-rifle-squad", UnitId = "regression-pla-rifle-1", DisplayName = "Regression PLA Rifle" },
                    new BattleUnitImport { Role = "pla-support-team", UnitId = "regression-pla-support-1", DisplayName = "Regression PLA Support" }
                }
            };
            File.WriteAllText(requestPath, JsonUtility.ToJson(request));

            TryLoadBattleRequest(requestPath);
            if (battleRequestError != null || activeBattleRequest == null)
            {
                Debug.LogError("ALWAYS_FAITHFUL_BATTLE_CONTRACT_REGRESSION_FAILED valid request rejected: " + battleRequestError);
                Application.Quit(1);
                yield break;
            }
            if (tacticalUnitState.Id != "regression-usmc-1" || tacticalEnemyStates.Count != 2 ||
                tacticalEnemyStates[0].Id != "regression-pla-rifle-1" || tacticalEnemyStates[1].Id != "regression-pla-support-1" ||
                tacticalObjective.Posture != TacticalPosture.Defend || tacticalObjective.TurnLimit != 1)
            {
                Debug.LogError($"ALWAYS_FAITHFUL_BATTLE_CONTRACT_REGRESSION_FAILED import mismatch usmc={tacticalUnitState.Id} rifle={tacticalEnemyStates[0].Id} support={tacticalEnemyStates[1].Id} posture={tacticalObjective.Posture} limit={tacticalObjective.TurnLimit}");
                Application.Quit(1);
                yield break;
            }
            string liveBattlefieldId = tacticalBattlefield.BattlefieldId;
            DismissCampaignBriefing();
            fastEnemyAnimation = true;
            EndTacticalTurn();
            float deadline = Time.realtimeSinceStartup + 5f;
            do { yield return null; } while ((tacticalEnemyTurnActive || !File.Exists(outputPath)) && Time.realtimeSinceStartup < deadline);
            yield return null;

            if (!File.Exists(outputPath) || File.Exists(outputPath + ".tmp"))
            {
                Debug.LogError($"ALWAYS_FAITHFUL_BATTLE_CONTRACT_REGRESSION_FAILED result missing or temp file leftover exists={File.Exists(outputPath)} tmpExists={File.Exists(outputPath + ".tmp")}");
                Application.Quit(1);
                yield break;
            }
            BattleResult result = JsonUtility.FromJson<BattleResult>(File.ReadAllText(outputPath));
            if (result == null || result.RequestId != request.RequestId || result.CampaignId != request.CampaignId ||
                result.Forces.Count != 3 || result.Forces[0].UnitId != "regression-usmc-1" ||
                result.Forces[1].UnitId != "regression-pla-rifle-1" || result.Forces[2].UnitId != "regression-pla-support-1" ||
                result.Outcome == TacticalBattleOutcome.InProgress)
            {
                Debug.LogError($"ALWAYS_FAITHFUL_BATTLE_CONTRACT_REGRESSION_FAILED result content requestId={result?.RequestId} forces={result?.Forces.Count} outcome={result?.Outcome}");
                Application.Quit(1);
                yield break;
            }
            if (activeBattleRequest != null)
            {
                Debug.LogError("ALWAYS_FAITHFUL_BATTLE_CONTRACT_REGRESSION_FAILED request was not consumed after export");
                Application.Quit(1);
                yield break;
            }

            // Deterministic replay: re-extracting the same theater hex with the
            // same seed must reproduce the exact battlefield ID the live battle
            // just used, without re-driving the UI a second time.
            TacticalBattlefieldState replay = TacticalBattlefieldExtractor.Extract(
                theaterHex, cells[theaterHex].Longitude, cells[theaterHex].Latitude,
                (longitude, latitude) => elevation.SampleMetres(longitude, latitude),
                (longitude, latitude) => coastline.ContainsLand(longitude, latitude),
                request.Seed);
            if (replay.BattlefieldId != liveBattlefieldId)
            {
                Debug.LogError($"ALWAYS_FAITHFUL_BATTLE_CONTRACT_REGRESSION_FAILED replay mismatch live={liveBattlefieldId} replay={replay.BattlefieldId}");
                Application.Quit(1);
                yield break;
            }

            File.WriteAllText(requestPath, "{ not json");
            TryLoadBattleRequest(requestPath);
            if (battleRequestError == null)
            {
                Debug.LogError("ALWAYS_FAITHFUL_BATTLE_CONTRACT_REGRESSION_FAILED malformed JSON was not rejected");
                Application.Quit(1);
                yield break;
            }
            DismissCampaignBriefing();

            request.ContractVersion = BattleRequest.CurrentContractVersion + 1;
            File.WriteAllText(requestPath, JsonUtility.ToJson(request));
            TryLoadBattleRequest(requestPath);
            if (battleRequestError == null)
            {
                Debug.LogError("ALWAYS_FAITHFUL_BATTLE_CONTRACT_REGRESSION_FAILED unsupported contract version was not rejected");
                Application.Quit(1);
                yield break;
            }
            DismissCampaignBriefing();

            Debug.Log($"ALWAYS_FAITHFUL_BATTLE_CONTRACT_REGRESSION_OK requestId={result.RequestId} outcome={result.Outcome} battlefield={liveBattlefieldId}");
            Application.Quit(0);
        }

        private static bool ValidateBattleContractRules(out string failure)
        {
            var valid = new BattleRequest { RequestId = "r1", OutputPath = "out.json" };
            if (!TacticalBattleContract.ValidateStructure(valid, out _))
            {
                failure = "a structurally valid request was rejected";
                return false;
            }

            var badVersion = new BattleRequest { ContractVersion = BattleRequest.CurrentContractVersion + 1, RequestId = "r1", OutputPath = "out.json" };
            if (TacticalBattleContract.ValidateStructure(badVersion, out _))
            {
                failure = "an unsupported contract version was accepted";
                return false;
            }

            var missingRequestId = new BattleRequest { OutputPath = "out.json" };
            if (TacticalBattleContract.ValidateStructure(missingRequestId, out _))
            {
                failure = "a request missing RequestId was accepted";
                return false;
            }

            var missingOutputPath = new BattleRequest { RequestId = "r1" };
            if (TacticalBattleContract.ValidateStructure(missingOutputPath, out _))
            {
                failure = "a request missing OutputPath was accepted";
                return false;
            }

            HexCoord hex = new HexCoord(3, 4);
            string unseeded = TacticalBattlefieldExtractor.BuildBattlefieldId(hex);
            if (unseeded != $"TW-{hex.Q:D2}-{hex.R:D3}-250M")
            {
                failure = $"seed=0 did not reproduce the legacy battlefield id, got {unseeded}";
                return false;
            }
            string seededOnce = TacticalBattlefieldExtractor.BuildBattlefieldId(hex, 99);
            string seededTwice = TacticalBattlefieldExtractor.BuildBattlefieldId(hex, 99);
            if (seededOnce == unseeded || seededOnce != seededTwice)
            {
                failure = $"seeded battlefield id was not distinct/deterministic: {seededOnce} vs {seededTwice} (unseeded {unseeded})";
                return false;
            }

            failure = null;
            return true;
        }

        private IEnumerator RunSaveRestoreRegression()
        {
            yield return null;
            if (!ValidateSaveRestoreRules(out string ruleFailure))
            {
                Debug.LogError("ALWAYS_FAITHFUL_SAVE_RESTORE_REGRESSION_FAILED rules=" + ruleFailure);
                Application.Quit(1);
                yield break;
            }

            EnterTacticalMap(FindHighReliefOperationalCell(), false);
            BeginTacticalReconPlanning();
            if (!tacticalReconPlanning || !TryIssueTacticalRecon(tacticalEnemyStates[0].Position))
            {
                Debug.LogError("ALWAYS_FAITHFUL_SAVE_RESTORE_REGRESSION_FAILED setup recon order was not accepted");
                Application.Quit(1);
                yield break;
            }
            fastEnemyAnimation = true;
            EndTacticalTurn();
            float turnDeadline = Time.realtimeSinceStartup + 5f;
            do { yield return null; } while (tacticalEnemyTurnActive && Time.realtimeSinceStartup < turnDeadline);

            // Snapshot every save-relevant field from the live objects before saving.
            HexCoord snapUnitPosition = tacticalUnitState.Position;
            int snapUnitAp = tacticalUnitState.RemainingActionPoints;
            TacticalCombatStatus snapUnitStatus = tacticalUnitState.CombatStatus;
            int snapUnitSuppression = tacticalUnitState.SuppressionPoints;
            var snapEnemyPositions = new List<HexCoord>();
            var snapEnemyStatuses = new List<TacticalCombatStatus>();
            foreach (TacticalUnitState enemy in tacticalEnemyStates)
            {
                snapEnemyPositions.Add(enemy.Position);
                snapEnemyStatuses.Add(enemy.CombatStatus);
            }
            int snapContactCount = tacticalContacts.Count;
            int snapEnemyContactCount = tacticalEnemyContacts.Count;
            int snapTurnNumber = turnState.TurnNumber;
            string snapActiveSide = turnState.ActiveSide;
            TacticalPosture snapPosture = tacticalObjective.Posture;
            int snapTurnLimit = tacticalObjective.TurnLimit;
            HexCoord snapObjectiveHex = tacticalObjective.ObjectiveHex;
            TacticalBattleOutcome snapOutcome = tacticalObjective.Outcome;
            int snapReconMarkerCount = tacticalReconMarkers.Count;
            int snapReconTurnsRemaining = tacticalReconMarkers.Count > 0 ? tacticalReconMarkers[0].TurnsRemaining : -1;
            int snapEventSequence = tacticalEventSequence;
            string snapBattlefieldId = tacticalBattlefield.BattlefieldId;

            string scratchDirectory = Path.Combine(Path.GetTempPath(), "AlwaysFaithfulSaveRestoreRegression");
            Directory.CreateDirectory(scratchDirectory);
            string scratchPath = Path.Combine(scratchDirectory, "battle-save.json");
            if (File.Exists(scratchPath)) File.Delete(scratchPath);
            SaveTacticalBattle(scratchPath);
            if (!File.Exists(scratchPath) || File.Exists(scratchPath + ".tmp"))
            {
                Debug.LogError($"ALWAYS_FAITHFUL_SAVE_RESTORE_REGRESSION_FAILED save missing or temp file leftover exists={File.Exists(scratchPath)} tmpExists={File.Exists(scratchPath + ".tmp")}");
                Application.Quit(1);
                yield break;
            }

            TacticalBattleSaveState parsed = JsonUtility.FromJson<TacticalBattleSaveState>(File.ReadAllText(scratchPath));
            if (parsed == null || parsed.Battlefield.BattlefieldId != snapBattlefieldId || parsed.Turn.TurnNumber != snapTurnNumber ||
                parsed.UsmcUnit.Id != tacticalUnitState.Id || parsed.EnemyUnits.Count != tacticalEnemyStates.Count ||
                parsed.Contacts.Count != snapContactCount || parsed.EnemyContacts.Count != snapEnemyContactCount ||
                parsed.EventSequence != snapEventSequence || parsed.HasActiveBattleRequest)
            {
                Debug.LogError($"ALWAYS_FAITHFUL_SAVE_RESTORE_REGRESSION_FAILED parsed save mismatch battlefield={parsed?.Battlefield?.BattlefieldId} turn={parsed?.Turn?.TurnNumber} enemies={parsed?.EnemyUnits.Count} contacts={parsed?.Contacts.Count} enemyContacts={parsed?.EnemyContacts.Count}/{snapEnemyContactCount} sequence={parsed?.EventSequence}");
                Application.Quit(1);
                yield break;
            }

            if (!TryLoadTacticalBattle(scratchPath, out string loadError))
            {
                Debug.LogError("ALWAYS_FAITHFUL_SAVE_RESTORE_REGRESSION_FAILED load rejected: " + loadError);
                Application.Quit(1);
                yield break;
            }

            bool reconRingsOk = true;
            foreach (TacticalReconMarker marker in tacticalReconMarkers)
                if (!tacticalReconRings.ContainsKey(marker.Hex)) reconRingsOk = false;
            bool occupancyOk = localMovementBoard[tacticalUnitState.Position].OccupantId == tacticalUnitState.Id;
            foreach (TacticalUnitState enemy in tacticalEnemyStates)
                if (localMovementBoard[enemy.Position].OccupantId != enemy.Id) occupancyOk = false;

            bool enemiesMatch = tacticalEnemyStates.Count == snapEnemyPositions.Count;
            if (enemiesMatch)
                for (int index = 0; index < tacticalEnemyStates.Count; index++)
                    if (!tacticalEnemyStates[index].Position.Equals(snapEnemyPositions[index]) || tacticalEnemyStates[index].CombatStatus != snapEnemyStatuses[index])
                        enemiesMatch = false;

            if (!tacticalUnitState.Position.Equals(snapUnitPosition) || tacticalUnitState.RemainingActionPoints != snapUnitAp ||
                tacticalUnitState.CombatStatus != snapUnitStatus || tacticalUnitState.SuppressionPoints != snapUnitSuppression ||
                !enemiesMatch || tacticalContacts.Count != snapContactCount || tacticalEnemyContacts.Count != snapEnemyContactCount ||
                turnState.TurnNumber != snapTurnNumber || turnState.ActiveSide != snapActiveSide ||
                tacticalObjective.Posture != snapPosture || tacticalObjective.TurnLimit != snapTurnLimit ||
                !tacticalObjective.ObjectiveHex.Equals(snapObjectiveHex) || tacticalObjective.Outcome != snapOutcome ||
                tacticalReconMarkers.Count != snapReconMarkerCount ||
                (tacticalReconMarkers.Count > 0 && tacticalReconMarkers[0].TurnsRemaining != snapReconTurnsRemaining) ||
                tacticalEventSequence != snapEventSequence ||
                tacticalContactViews.Count != tacticalEnemyStates.Count || !reconRingsOk || !occupancyOk)
            {
                Debug.LogError($"ALWAYS_FAITHFUL_SAVE_RESTORE_REGRESSION_FAILED restored state mismatch unit={tacticalUnitState.Position}/{snapUnitPosition} ap={tacticalUnitState.RemainingActionPoints}/{snapUnitAp} enemiesMatch={enemiesMatch} contacts={tacticalContacts.Count}/{snapContactCount} enemyContacts={tacticalEnemyContacts.Count}/{snapEnemyContactCount} turn={turnState.TurnNumber}/{snapTurnNumber} markers={tacticalReconMarkers.Count}/{snapReconMarkerCount} sequence={tacticalEventSequence}/{snapEventSequence} reconRingsOk={reconRingsOk} occupancyOk={occupancyOk}");
                Application.Quit(1);
                yield break;
            }

            // A post-restore action must not collide with a saved Sequence number.
            BeginTacticalReconPlanning();
            if (!TryIssueTacticalRecon(tacticalEnemyStates[0].Position) ||
                tacticalBattlefield.ReconEvents[tacticalBattlefield.ReconEvents.Count - 1].Sequence != snapEventSequence + 1)
            {
                Debug.LogError($"ALWAYS_FAITHFUL_SAVE_RESTORE_REGRESSION_FAILED post-restore sequence collision expected={snapEventSequence + 1} actual={tacticalBattlefield.ReconEvents[tacticalBattlefield.ReconEvents.Count - 1].Sequence}");
                Application.Quit(1);
                yield break;
            }

            // Request-driven round trip: a save/restore mid-request-driven-battle
            // must still write a correct BattleResult when it eventually concludes.
            HexCoord requestTheaterHex = FindHighReliefOperationalCell().Coord;
            string requestOutputPath = Path.Combine(scratchDirectory, "result.json");
            if (File.Exists(requestOutputPath)) File.Delete(requestOutputPath);
            var request = new BattleRequest
            {
                RequestId = "save-restore-regression-request-1",
                CampaignId = "save-restore-regression-campaign-1",
                Seed = 5150,
                TheaterHex = requestTheaterHex,
                HasPostureOverride = true,
                Posture = TacticalPosture.Defend,
                TurnLimitOverride = 1,
                OutputPath = requestOutputPath
            };
            string requestPath = Path.Combine(scratchDirectory, "request.json");
            File.WriteAllText(requestPath, JsonUtility.ToJson(request));
            TryLoadBattleRequest(requestPath);
            if (battleRequestError != null || activeBattleRequest == null)
            {
                Debug.LogError("ALWAYS_FAITHFUL_SAVE_RESTORE_REGRESSION_FAILED request setup rejected: " + battleRequestError);
                Application.Quit(1);
                yield break;
            }
            DismissCampaignBriefing();
            string requestSavePath = Path.Combine(scratchDirectory, "battle-save-request.json");
            if (File.Exists(requestSavePath)) File.Delete(requestSavePath);
            SaveTacticalBattle(requestSavePath);
            activeBattleRequest = null;
            if (!TryLoadTacticalBattle(requestSavePath, out string requestLoadError))
            {
                Debug.LogError("ALWAYS_FAITHFUL_SAVE_RESTORE_REGRESSION_FAILED request-driven load rejected: " + requestLoadError);
                Application.Quit(1);
                yield break;
            }
            if (activeBattleRequest == null || activeBattleRequest.RequestId != request.RequestId || activeBattleRequest.OutputPath != request.OutputPath)
            {
                Debug.LogError($"ALWAYS_FAITHFUL_SAVE_RESTORE_REGRESSION_FAILED active battle request did not round-trip requestId={activeBattleRequest?.RequestId}");
                Application.Quit(1);
                yield break;
            }
            fastEnemyAnimation = true;
            EndTacticalTurn();
            float resultDeadline = Time.realtimeSinceStartup + 5f;
            do { yield return null; } while ((tacticalEnemyTurnActive || !File.Exists(requestOutputPath)) && Time.realtimeSinceStartup < resultDeadline);
            if (!File.Exists(requestOutputPath))
            {
                Debug.LogError("ALWAYS_FAITHFUL_SAVE_RESTORE_REGRESSION_FAILED restored request-driven battle did not write a result");
                Application.Quit(1);
                yield break;
            }
            BattleResult requestResult = JsonUtility.FromJson<BattleResult>(File.ReadAllText(requestOutputPath));
            if (requestResult == null || requestResult.RequestId != request.RequestId)
            {
                Debug.LogError($"ALWAYS_FAITHFUL_SAVE_RESTORE_REGRESSION_FAILED result requestId mismatch got={requestResult?.RequestId}");
                Application.Quit(1);
                yield break;
            }

            Debug.Log($"ALWAYS_FAITHFUL_SAVE_RESTORE_REGRESSION_OK battlefield={snapBattlefieldId} turn={snapTurnNumber} sequence={snapEventSequence}");
            Application.Quit(0);
        }

        private static bool ValidateSaveRestoreRules(out string failure)
        {
            if (TacticalBattleSave.Validate(null, out _))
            {
                failure = "a null save state was accepted";
                return false;
            }
            var wrongVersion = new TacticalBattleSaveState { SchemaVersion = TacticalBattleSaveState.CurrentSchemaVersion + 1 };
            if (TacticalBattleSave.Validate(wrongVersion, out _))
            {
                failure = "an unsupported save schema version was accepted";
                return false;
            }
            var missingBattlefield = new TacticalBattleSaveState { UsmcUnit = new TacticalUnitState("u", "U", new HexCoord(0, 0), 6), Turn = new TacticalTurnState() };
            if (TacticalBattleSave.Validate(missingBattlefield, out _))
            {
                failure = "a save missing battlefield state was accepted";
                return false;
            }
            var missingUnit = new TacticalBattleSaveState { Battlefield = new TacticalBattlefieldState { BattlefieldId = "b" }, Turn = new TacticalTurnState() };
            if (TacticalBattleSave.Validate(missingUnit, out _))
            {
                failure = "a save missing the USMC unit was accepted";
                return false;
            }
            var missingTurn = new TacticalBattleSaveState { Battlefield = new TacticalBattlefieldState { BattlefieldId = "b" }, UsmcUnit = new TacticalUnitState("u", "U", new HexCoord(0, 0), 6) };
            if (TacticalBattleSave.Validate(missingTurn, out _))
            {
                failure = "a save missing turn state was accepted";
                return false;
            }
            var valid = new TacticalBattleSaveState
            {
                Battlefield = new TacticalBattlefieldState { BattlefieldId = "b" },
                UsmcUnit = new TacticalUnitState("u", "U", new HexCoord(0, 0), 6),
                Turn = new TacticalTurnState()
            };
            if (!TacticalBattleSave.Validate(valid, out string validError))
            {
                failure = "a structurally valid save state was rejected: " + validError;
                return false;
            }
            failure = null;
            return true;
        }

        private IEnumerator RunSaveRestoreCapture()
        {
            string argument = Array.Find(Environment.GetCommandLineArgs(), value => value.StartsWith("--save-restore-capture-path=", StringComparison.Ordinal));
            if (argument == null) yield break;
            string path = argument.Substring("--save-restore-capture-path=".Length);

            EnterTacticalMap(FindHighReliefOperationalCell(), false);
            BeginTacticalReconPlanning();
            TryIssueTacticalRecon(tacticalEnemyStates[0].Position);
            fastEnemyAnimation = true;
            EndTacticalTurn();
            float deadline = Time.realtimeSinceStartup + 5f;
            do { yield return null; } while (tacticalEnemyTurnActive && Time.realtimeSinceStartup < deadline);

            string scratchDirectory = Path.Combine(Path.GetTempPath(), "AlwaysFaithfulSaveRestoreCapture");
            Directory.CreateDirectory(scratchDirectory);
            string scratchPath = Path.Combine(scratchDirectory, "battle-save.json");
            SaveTacticalBattle(scratchPath);
            TryLoadTacticalBattle(scratchPath, out _);
            yield return new WaitForSecondsRealtime(.5f);

            var target = new RenderTexture(1280, 720, 24);
            var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active;
            mapCamera.targetTexture = target;
            mapCamera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0f, 0f, target.width, target.height), 0, 0);
            image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
            mapCamera.targetTexture = null;
            RenderTexture.active = previous;
            Destroy(target);
            Destroy(image);
            Debug.Log($"ALWAYS_FAITHFUL_SAVE_RESTORE_CAPTURED {path} battlefield={tacticalBattlefield.BattlefieldId} turn={turnState.TurnNumber}");
            Application.Quit(0);
        }

        private static bool ValidateFeedbackRules(out string failure)
        {
            if (TacticalBattlefieldState.CurrentSchemaVersion != 13)
            {
                failure = $"expected schema version 13, got {TacticalBattlefieldState.CurrentSchemaVersion}";
                return false;
            }
            var battlefield = new TacticalBattlefieldState { BattlefieldId = "TEST" };
            battlefield.ObjectiveEvents.Add(new TacticalObjectiveEvent
            {
                Sequence = 1,
                BattlefieldId = "TEST",
                Turn = 2,
                Hex = new HexCoord(3, 4),
                ControlledByUsmc = true
            });
            string serialized = JsonUtility.ToJson(battlefield);
            TacticalBattlefieldState restored = JsonUtility.FromJson<TacticalBattlefieldState>(serialized);
            if (restored?.ObjectiveEvents.Count != 1 || restored.ObjectiveEvents[0].Turn != 2 ||
                !restored.ObjectiveEvents[0].Hex.Equals(new HexCoord(3, 4)) || !restored.ObjectiveEvents[0].ControlledByUsmc)
            {
                failure = "TacticalObjectiveEvent did not round-trip through JsonUtility";
                return false;
            }
            failure = null;
            return true;
        }

        private IEnumerator RunFeedbackRegression()
        {
            yield return null;
            if (!ValidateFeedbackRules(out string ruleFailure))
            {
                Debug.LogError("ALWAYS_FAITHFUL_FEEDBACK_REGRESSION_FAILED rules=" + ruleFailure);
                Application.Quit(1);
                yield break;
            }

            EnterTacticalMap(FindHighReliefOperationalCell(), false);

            // 1. Rejected Fire: self-targeting is always illegal, deterministic
            // regardless of RNG/posture/battlefield layout.
            BeginTacticalFirePlanning();
            PreviewTacticalFire(localCells[tacticalUnitState.Position]);
            string hoverFireFeedback = tacticalOrderFeedback;
            tacticalOrderFeedback = "SENTINEL";
            bool fireAccepted = TryIssueTacticalFire(tacticalUnitState.Position);
            if (fireAccepted || tacticalOrderFeedback == "SENTINEL" || tacticalOrderFeedback != hoverFireFeedback)
            {
                Debug.LogError($"ALWAYS_FAITHFUL_FEEDBACK_REGRESSION_FAILED rejected fire feedback accepted={fireAccepted} feedback='{tacticalOrderFeedback}' hover='{hoverFireFeedback}'");
                Application.Quit(1);
                yield break;
            }

            // 2. Rejected Recon: pure out-of-range math, no board-bounds dependency.
            BeginTacticalReconPlanning();
            HexCoord farHex = new HexCoord(tacticalUnitState.Position.Q + TacticalRecon.MaximumRangeHexes + 5, tacticalUnitState.Position.R);
            bool reconAccepted = TryIssueTacticalRecon(farHex);
            if (reconAccepted || !tacticalOrderFeedback.Contains("exceeds"))
            {
                Debug.LogError($"ALWAYS_FAITHFUL_FEEDBACK_REGRESSION_FAILED rejected recon feedback accepted={reconAccepted} feedback='{tacticalOrderFeedback}'");
                Application.Quit(1);
                yield break;
            }

            // 3. Objective control flip: directly mutate occupancy to simulate
            // arrival/departure without depending on a real, interruptible move.
            int objectiveEventsBefore = tacticalBattlefield.ObjectiveEvents.Count;
            bool initialControl = tacticalObjectiveControlledByUsmc.Value;
            HexCoord objectiveHex = tacticalObjective.ObjectiveHex;
            string originalOccupant = localMovementBoard[objectiveHex].OccupantId;
            localMovementBoard[objectiveHex].OccupantId = initialControl ? null : tacticalUnitState.Id;
            RefreshTacticalObservation();
            if (tacticalBattlefield.ObjectiveEvents.Count != objectiveEventsBefore + 1 ||
                tacticalBattlefield.ObjectiveEvents[tacticalBattlefield.ObjectiveEvents.Count - 1].ControlledByUsmc == initialControl)
            {
                Debug.LogError($"ALWAYS_FAITHFUL_FEEDBACK_REGRESSION_FAILED objective flip events={tacticalBattlefield.ObjectiveEvents.Count}/{objectiveEventsBefore + 1}");
                Application.Quit(1);
                yield break;
            }
            List<string> logAfterFlip = BuildTacticalEventLog(int.MaxValue);
            if (!logAfterFlip.Exists(line => line.StartsWith("OBJECTIVE")))
            {
                Debug.LogError("ALWAYS_FAITHFUL_FEEDBACK_REGRESSION_FAILED no OBJECTIVE line in merged log");
                Application.Quit(1);
                yield break;
            }
            localMovementBoard[objectiveHex].OccupantId = originalOccupant;
            RefreshTacticalObservation();

            // 4. Rejected move (same-hex) proves Detail text and DisplayName resolution.
            int movementEventsBefore = tacticalBattlefield.MovementEvents.Count;
            BeginTacticalMovePlanning();
            bool moveAccepted = TryIssueTacticalMove(tacticalUnitState.Position);
            List<string> logAfterMove = BuildTacticalEventLog(int.MaxValue);
            string moveLine = logAfterMove.Count > 0 ? logAfterMove[logAfterMove.Count - 1] : string.Empty;
            if (moveAccepted || tacticalBattlefield.MovementEvents.Count != movementEventsBefore + 1 ||
                !moveLine.Contains("Already occupying destination") || !moveLine.Contains(tacticalUnitState.DisplayName))
            {
                Debug.LogError($"ALWAYS_FAITHFUL_FEEDBACK_REGRESSION_FAILED rejected move accepted={moveAccepted} line='{moveLine}'");
                Application.Quit(1);
                yield break;
            }

            // 5. RESULT line appears once the battle concludes.
            fastEnemyAnimation = true;
            tacticalObjective.TurnLimit = 1;
            EndTacticalTurn();
            float deadline = Time.realtimeSinceStartup + 5f;
            do { yield return null; } while ((tacticalEnemyTurnActive || !resultScreenActive || resultScreenOpacity < .98f) && Time.realtimeSinceStartup < deadline);
            List<string> finalLog = BuildTacticalEventLog(int.MaxValue);
            if (tacticalObjective.Outcome == TacticalBattleOutcome.InProgress || finalLog.Count == 0 || !finalLog[finalLog.Count - 1].StartsWith("RESULT"))
            {
                Debug.LogError($"ALWAYS_FAITHFUL_FEEDBACK_REGRESSION_FAILED result line outcome={tacticalObjective.Outcome} lastLine='{(finalLog.Count > 0 ? finalLog[finalLog.Count - 1] : "<none>")}'");
                Application.Quit(1);
                yield break;
            }

            Debug.Log("ALWAYS_FAITHFUL_FEEDBACK_REGRESSION_OK");
            Application.Quit(0);
        }

        private static bool ValidateScenarioRules(out string failure)
        {
            if (TacticalBattlefieldState.CurrentSchemaVersion != 13)
            {
                failure = $"expected schema version 13 after adding support-card events and PLA recon markers, got {TacticalBattlefieldState.CurrentSchemaVersion}";
                return false;
            }
            const string id = "TW-TEST-SCENARIO";
            int limitA = TacticalScenario.ChooseTurnLimit(id);
            int limitB = TacticalScenario.ChooseTurnLimit(id);
            if (limitA != limitB || limitA < TacticalScenario.MinTurnLimit || limitA > TacticalScenario.MaxTurnLimit)
            {
                failure = $"turn limit not deterministic/bounded: {limitA} vs {limitB}";
                return false;
            }
            List<string> rosterA = TacticalScenario.ChooseEnemyRoster(id);
            List<string> rosterB = TacticalScenario.ChooseEnemyRoster(id);
            if (rosterA.Count != rosterB.Count || rosterA.Count < TacticalScenario.MinRosterSize || rosterA.Count > TacticalScenario.MaxRosterSize ||
                rosterA[0] != TacticalScenario.RifleRole)
            {
                failure = $"roster not deterministic/bounded or missing lead rifle squad: [{string.Join(",", rosterA)}]";
                return false;
            }
            for (int index = 0; index < rosterA.Count; index++)
                if (rosterA[index] != rosterB[index])
                {
                    failure = "roster not deterministic across calls";
                    return false;
                }
            TacticalMissionType missionA = TacticalScenario.ChooseMissionType(id);
            TacticalMissionType missionB = TacticalScenario.ChooseMissionType(id);
            if (missionA != missionB)
            {
                failure = $"mission type not deterministic: {missionA} vs {missionB}";
                return false;
            }
            failure = null;
            return true;
        }

        private IEnumerator RunScenarioRegression()
        {
            yield return null;
            if (!ValidateScenarioRules(out string ruleFailure))
            {
                Debug.LogError("ALWAYS_FAITHFUL_SCENARIO_REGRESSION_FAILED rules=" + ruleFailure);
                Application.Quit(1);
                yield break;
            }

            HexCellView parent = FindHighReliefOperationalCell();
            EnterTacticalMap(parent, false);
            string firstBattlefieldId = tacticalBattlefield.BattlefieldId;
            int firstSeed = standaloneScenarioSeed;
            int firstTurnLimit = tacticalObjective.TurnLimit;
            var firstRoster = new List<string>();
            foreach (TacticalUnitState enemy in tacticalEnemyStates) firstRoster.Add(enemy.Id);
            if (firstSeed == 0 || firstBattlefieldId == TacticalBattlefieldExtractor.BuildBattlefieldId(parent.Coord, 0) ||
                tacticalEnemyStates.Count < TacticalScenario.MinRosterSize || tacticalEnemyStates.Count > TacticalScenario.MaxRosterSize)
            {
                Debug.LogError($"ALWAYS_FAITHFUL_SCENARIO_REGRESSION_FAILED entropy point did not fire seed={firstSeed} battlefield={firstBattlefieldId} enemies={tacticalEnemyStates.Count}");
                Application.Quit(1);
                yield break;
            }

            // Return to Island mid-battle (still InProgress) then re-enter the SAME
            // hex must reproduce the identical battlefield/turn-limit/roster, not reroll.
            ReturnToIsland(false);
            EnterTacticalMap(parent, false);
            var resumedRoster = new List<string>();
            foreach (TacticalUnitState enemy in tacticalEnemyStates) resumedRoster.Add(enemy.Id);
            bool rosterMatches = resumedRoster.Count == firstRoster.Count;
            if (rosterMatches)
                for (int index = 0; index < resumedRoster.Count; index++)
                    if (resumedRoster[index] != firstRoster[index]) rosterMatches = false;
            if (tacticalBattlefield.BattlefieldId != firstBattlefieldId || standaloneScenarioSeed != firstSeed ||
                tacticalObjective.TurnLimit != firstTurnLimit || !rosterMatches)
            {
                Debug.LogError($"ALWAYS_FAITHFUL_SCENARIO_REGRESSION_FAILED resume rerolled battlefield={tacticalBattlefield.BattlefieldId}/{firstBattlefieldId} seed={standaloneScenarioSeed}/{firstSeed} limit={tacticalObjective.TurnLimit}/{firstTurnLimit} rosterMatches={rosterMatches}");
                Application.Quit(1);
                yield break;
            }

            // Entering a DIFFERENT hex must reroll (not reuse the previous seed).
            HexCellView otherParent = FindCoastalOperationalCell();
            if (!otherParent.Coord.Equals(parent.Coord))
            {
                EnterTacticalMap(otherParent, false);
                if (standaloneScenarioSeed == firstSeed)
                {
                    Debug.LogError("ALWAYS_FAITHFUL_SCENARIO_REGRESSION_FAILED different hex reused the previous seed");
                    Application.Quit(1);
                    yield break;
                }
                ReturnToIsland(false);
            }

            // A BattleRequest-driven entry must still produce exactly the legacy
            // 2-unit roster and TacticalVictory.DefaultTurnLimit when no override is set.
            string scratchDirectory = Path.Combine(Path.GetTempPath(), "AlwaysFaithfulScenarioRegression");
            Directory.CreateDirectory(scratchDirectory);
            string requestPath = Path.Combine(scratchDirectory, "request.json");
            var request = new BattleRequest
            {
                RequestId = "scenario-regression-request-1",
                CampaignId = "scenario-regression-campaign-1",
                Seed = 777,
                TheaterHex = parent.Coord,
                OutputPath = Path.Combine(scratchDirectory, "result.json")
            };
            File.WriteAllText(requestPath, JsonUtility.ToJson(request));
            TryLoadBattleRequest(requestPath);
            if (battleRequestError != null || activeBattleRequest == null ||
                tacticalEnemyStates.Count != 2 || tacticalEnemyStates[0].Id != "pla-rifle-squad-1" || tacticalEnemyStates[1].Id != "pla-support-team-1" ||
                tacticalObjective.TurnLimit != TacticalVictory.DefaultTurnLimit)
            {
                Debug.LogError($"ALWAYS_FAITHFUL_SCENARIO_REGRESSION_FAILED battle-request path altered enemies={tacticalEnemyStates.Count} limit={tacticalObjective.TurnLimit}");
                Application.Quit(1);
                yield break;
            }
            DismissCampaignBriefing();

            Debug.Log($"ALWAYS_FAITHFUL_SCENARIO_REGRESSION_OK seed={firstSeed} turnLimit={firstTurnLimit} roster=[{string.Join(",", firstRoster)}]");
            Application.Quit(0);
        }

        private static bool ValidateBattalionRules(out string failure)
        {
            TacticalBattalionStatus fresh = TacticalBattalion.CreateFresh();
            if (fresh.Strength != TacticalBattalion.MaximumStrength)
            {
                failure = $"expected fresh battalion status at {TacticalBattalion.MaximumStrength}%, got {fresh.Strength}%";
                return false;
            }
            var floorStatus = TacticalBattalion.CreateFresh();
            for (int index = 0; index < 10; index++) TacticalBattalion.ApplyEngagementResult(floorStatus, TacticalBattleOutcome.UsmcDefeat, TacticalCombatStatus.Reduced);
            if (floorStatus.Strength != TacticalBattalion.MinimumStrength)
            {
                failure = $"expected repeated defeats to clamp at the floor {TacticalBattalion.MinimumStrength}%, got {floorStatus.Strength}%";
                return false;
            }
            var ceilingStatus = TacticalBattalion.CreateFresh();
            for (int index = 0; index < 10; index++) TacticalBattalion.ApplyEngagementResult(ceilingStatus, TacticalBattleOutcome.UsmcVictory, TacticalCombatStatus.Ready);
            if (ceilingStatus.Strength != TacticalBattalion.MaximumStrength)
            {
                failure = $"expected repeated clean victories to clamp at the ceiling {TacticalBattalion.MaximumStrength}%, got {ceilingStatus.Strength}%";
                return false;
            }
            if (TacticalBattalion.Validate(null, out _))
            {
                failure = "a null battalion status was accepted";
                return false;
            }
            var badVersion = TacticalBattalion.CreateFresh();
            badVersion.SchemaVersion = TacticalBattalionStatus.CurrentSchemaVersion + 1;
            if (TacticalBattalion.Validate(badVersion, out _))
            {
                failure = "an unsupported battalion status schema version was accepted";
                return false;
            }
            var badStrength = TacticalBattalion.CreateFresh();
            badStrength.Strength = TacticalBattalion.MinimumStrength - 1;
            if (TacticalBattalion.Validate(badStrength, out _))
            {
                failure = "an out-of-range battalion strength was accepted";
                return false;
            }
            var badName = TacticalBattalion.CreateFresh();
            badName.BattalionName = "  ";
            if (TacticalBattalion.Validate(badName, out _))
            {
                failure = "a battalion status with a blank name was accepted";
                return false;
            }
            if (!TacticalBattalion.Validate(fresh, out string validError))
            {
                failure = "a structurally valid battalion status was rejected: " + validError;
                return false;
            }
            failure = null;
            return true;
        }

        private IEnumerator RunBattalionRegression()
        {
            yield return null;
            if (!ValidateBattalionRules(out string ruleFailure))
            {
                Debug.LogError("ALWAYS_FAITHFUL_BATTALION_REGRESSION_FAILED rules=" + ruleFailure);
                Application.Quit(1);
                yield break;
            }

            // Isolate this regression from whatever real save might already
            // exist at the default persistentDataPath location.
            string scratchDirectory = Path.Combine(Path.GetTempPath(), "AlwaysFaithfulBattalionRegression");
            Directory.CreateDirectory(scratchDirectory);
            battalionStatusPath = Path.Combine(scratchDirectory, "battalion-status.json");
            battalionStatus = TacticalBattalion.CreateFresh();
            SaveBattalionStatus();

            HexCellView parent = FindHighReliefOperationalCell();
            fastEnemyAnimation = true;
            EnterTacticalMap(parent, false);
            tacticalObjective.TurnLimit = 1;
            EndTacticalTurn();
            float deadline = Time.realtimeSinceStartup + 5f;
            do { yield return null; } while ((tacticalEnemyTurnActive || !resultScreenActive || resultScreenOpacity < .98f) && Time.realtimeSinceStartup < deadline);
            if (tacticalObjective.Outcome == TacticalBattleOutcome.InProgress || battalionStatus.EngagementsCompleted != 1 || battalionStatus.History.Count != 1)
            {
                Debug.LogError($"ALWAYS_FAITHFUL_BATTALION_REGRESSION_FAILED first engagement outcome={tacticalObjective.Outcome} engagements={battalionStatus.EngagementsCompleted} history={battalionStatus.History.Count}");
                Application.Quit(1);
                yield break;
            }
            if (!TryLoadBattalionStatus(battalionStatusPath, out TacticalBattalionStatus roundTripped) || roundTripped.Strength != battalionStatus.Strength)
            {
                Debug.LogError("ALWAYS_FAITHFUL_BATTALION_REGRESSION_FAILED status did not persist/round-trip correctly");
                Application.Quit(1);
                yield break;
            }

            // Re-entering the same hex after conclusion (Outcome != InProgress)
            // starts a genuinely new scenario and must update status again
            // exactly once, not skip or double-apply.
            int engagementsAfterFirstBattle = battalionStatus.EngagementsCompleted;
            ReturnToIsland(false);
            EnterTacticalMap(parent, false);
            tacticalObjective.TurnLimit = 1;
            EndTacticalTurn();
            deadline = Time.realtimeSinceStartup + 5f;
            do { yield return null; } while ((tacticalEnemyTurnActive || !resultScreenActive || resultScreenOpacity < .98f) && Time.realtimeSinceStartup < deadline);
            if (battalionStatus.EngagementsCompleted != engagementsAfterFirstBattle + 1)
            {
                Debug.LogError($"ALWAYS_FAITHFUL_BATTALION_REGRESSION_FAILED second engagement did not apply exactly once engagements={battalionStatus.EngagementsCompleted}/{engagementsAfterFirstBattle + 1}");
                Application.Quit(1);
                yield break;
            }

            // A BattleRequest-driven battle must never touch Battalion status.
            int engagementsBeforeRequest = battalionStatus.EngagementsCompleted;
            int strengthBeforeRequest = battalionStatus.Strength;
            ReturnToIsland(false);
            string requestPath = Path.Combine(scratchDirectory, "request.json");
            var request = new BattleRequest
            {
                RequestId = "battalion-regression-request-1",
                CampaignId = "battalion-regression-campaign-1",
                Seed = 4242,
                TheaterHex = parent.Coord,
                TurnLimitOverride = 1,
                OutputPath = Path.Combine(scratchDirectory, "result.json")
            };
            File.WriteAllText(requestPath, JsonUtility.ToJson(request));
            TryLoadBattleRequest(requestPath);
            if (battleRequestError != null || activeBattleRequest == null)
            {
                Debug.LogError("ALWAYS_FAITHFUL_BATTALION_REGRESSION_FAILED battle request setup rejected: " + battleRequestError);
                Application.Quit(1);
                yield break;
            }
            DismissCampaignBriefing();
            EndTacticalTurn();
            deadline = Time.realtimeSinceStartup + 5f;
            do { yield return null; } while ((tacticalEnemyTurnActive || !resultScreenActive || resultScreenOpacity < .98f) && Time.realtimeSinceStartup < deadline);
            if (battalionStatus.EngagementsCompleted != engagementsBeforeRequest || battalionStatus.Strength != strengthBeforeRequest)
            {
                Debug.LogError($"ALWAYS_FAITHFUL_BATTALION_REGRESSION_FAILED battle-request path touched battalion status engagements={battalionStatus.EngagementsCompleted}/{engagementsBeforeRequest} strength={battalionStatus.Strength}/{strengthBeforeRequest}");
                Application.Quit(1);
                yield break;
            }
            ReturnToIsland(false);

            // Forcing Strength under the degraded threshold before a fresh entry
            // must reduce the platoon's starting AP; restoring full strength removes it.
            battalionStatus.Strength = TacticalBattalion.DegradedStrengthThreshold - 1;
            activeOperationalBattalion.Strength = battalionStatus.Strength;
            SaveBattalionStatus();
            EnterTacticalMap(parent, false);
            if (tacticalUnitState.MaximumActionPoints != TacticalPlatoonActionPoints - TacticalBattalion.ActionPointPenalty)
            {
                Debug.LogError($"ALWAYS_FAITHFUL_BATTALION_REGRESSION_FAILED under-strength AP penalty not applied ap={tacticalUnitState.MaximumActionPoints}");
                Application.Quit(1);
                yield break;
            }
            // Conclude this battle before re-entering the same hex, or the next
            // EnterTacticalMap call would correctly treat it as resuming (not a
            // fresh scenario) and keep the stale AP value rather than recompute it.
            tacticalObjective.TurnLimit = 1;
            EndTacticalTurn();
            deadline = Time.realtimeSinceStartup + 5f;
            do { yield return null; } while ((tacticalEnemyTurnActive || !resultScreenActive || resultScreenOpacity < .98f) && Time.realtimeSinceStartup < deadline);
            ReturnToIsland(false);
            battalionStatus.Strength = TacticalBattalion.MaximumStrength;
            activeOperationalBattalion.Strength = battalionStatus.Strength;
            SaveBattalionStatus();
            EnterTacticalMap(parent, false);
            if (tacticalUnitState.MaximumActionPoints != TacticalPlatoonActionPoints)
            {
                Debug.LogError($"ALWAYS_FAITHFUL_BATTALION_REGRESSION_FAILED full-strength platoon incorrectly penalized ap={tacticalUnitState.MaximumActionPoints}");
                Application.Quit(1);
                yield break;
            }

            Debug.Log($"ALWAYS_FAITHFUL_BATTALION_REGRESSION_OK strength={battalionStatus.Strength} engagements={battalionStatus.EngagementsCompleted}");
            Application.Quit(0);
        }

        private static bool ValidateSettingsRules(out string failure)
        {
            AlwaysFaithfulSettings fresh = AlwaysFaithfulSettingsRules.CreateDefault();
            if (fresh.UiScale != UiScaleTier.Auto || fresh.RemapCancelKey != KeyCode.Escape || fresh.RemapResetCameraKey != KeyCode.R)
            {
                failure = $"expected default settings (Auto/Escape/R), got {fresh.UiScale}/{fresh.RemapCancelKey}/{fresh.RemapResetCameraKey}";
                return false;
            }
            if (fresh.GraphicsPreset != GraphicsPresetTier.Medium || fresh.ColorSafePalette || fresh.ReducedMotion ||
                fresh.AnimationSpeed != AnimationSpeedTier.Normal || fresh.CounterSkin != CounterSkinTier.Illustrated)
            {
                failure = $"expected default graphics settings (Medium/off/off/Normal/Illustrated), got {fresh.GraphicsPreset}/{fresh.ColorSafePalette}/{fresh.ReducedMotion}/{fresh.AnimationSpeed}/{fresh.CounterSkin}";
                return false;
            }
            if (AlwaysFaithfulSettingsRules.Validate(null, out _))
            {
                failure = "a null settings state was accepted";
                return false;
            }
            var badVersion = AlwaysFaithfulSettingsRules.CreateDefault();
            badVersion.SchemaVersion = AlwaysFaithfulSettings.CurrentSchemaVersion + 1;
            if (AlwaysFaithfulSettingsRules.Validate(badVersion, out _))
            {
                failure = "an unsupported settings schema version was accepted";
                return false;
            }
            var unboundCancel = AlwaysFaithfulSettingsRules.CreateDefault();
            unboundCancel.RemapCancelKey = KeyCode.None;
            if (AlwaysFaithfulSettingsRules.Validate(unboundCancel, out _))
            {
                failure = "an unbound Cancel key was accepted";
                return false;
            }
            var collision = AlwaysFaithfulSettingsRules.CreateDefault();
            collision.RemapCancelKey = KeyCode.R;
            if (AlwaysFaithfulSettingsRules.Validate(collision, out _))
            {
                failure = "Cancel and Reset Camera bound to the same key was accepted";
                return false;
            }
            if (!AlwaysFaithfulSettingsRules.Validate(fresh, out string validError))
            {
                failure = "a structurally valid settings state was rejected: " + validError;
                return false;
            }
            if (UiScaleValueFor(UiScaleTier.Small) != .85f || UiScaleValueFor(UiScaleTier.Normal) != 1.0f ||
                UiScaleValueFor(UiScaleTier.Large) != 1.2f || UiScaleValueFor(UiScaleTier.ExtraLarge) != 1.5f)
            {
                failure = "UI scale tier lookup did not return the expected literal values";
                return false;
            }
            failure = null;
            return true;
        }

        private IEnumerator RunSettingsRegression()
        {
            yield return null;
            if (!ValidateSettingsRules(out string ruleFailure))
            {
                Debug.LogError("ALWAYS_FAITHFUL_SETTINGS_REGRESSION_FAILED rules=" + ruleFailure);
                Application.Quit(1);
                yield break;
            }

            // Isolate this regression from whatever real settings file might
            // already exist at the default persistentDataPath location.
            string scratchDirectory = Path.Combine(Path.GetTempPath(), "AlwaysFaithfulSettingsRegression");
            Directory.CreateDirectory(scratchDirectory);
            settingsPath = Path.Combine(scratchDirectory, "settings.json");
            settings = AlwaysFaithfulSettingsRules.CreateDefault();
            settings.UiScale = UiScaleTier.Large;
            settings.RemapResetCameraKey = KeyCode.T;
            settings.GraphicsPreset = GraphicsPresetTier.Low;
            settings.ColorSafePalette = true;
            settings.ReducedMotion = true;
            settings.AnimationSpeed = AnimationSpeedTier.Fast;
            settings.CounterSkin = CounterSkinTier.Miniature;
            SaveSettings();

            if (!TryLoadSettings(settingsPath, out AlwaysFaithfulSettings roundTripped) ||
                roundTripped.UiScale != UiScaleTier.Large || roundTripped.RemapResetCameraKey != KeyCode.T || roundTripped.RemapCancelKey != KeyCode.Escape ||
                roundTripped.GraphicsPreset != GraphicsPresetTier.Low || !roundTripped.ColorSafePalette || !roundTripped.ReducedMotion ||
                roundTripped.AnimationSpeed != AnimationSpeedTier.Fast || roundTripped.CounterSkin != CounterSkinTier.Miniature)
            {
                Debug.LogError($"ALWAYS_FAITHFUL_SETTINGS_REGRESSION_FAILED persistence round trip failed scale={roundTripped?.UiScale} resetKey={roundTripped?.RemapResetCameraKey} graphics={roundTripped?.GraphicsPreset} colorSafe={roundTripped?.ColorSafePalette} reducedMotion={roundTripped?.ReducedMotion} animSpeed={roundTripped?.AnimationSpeed} counterSkin={roundTripped?.CounterSkin}");
                Application.Quit(1);
                yield break;
            }
            settings = roundTripped;
            if (Mathf.Abs(GetUiScale() - 1.2f) > .0001f)
            {
                Debug.LogError($"ALWAYS_FAITHFUL_SETTINGS_REGRESSION_FAILED GetUiScale did not reflect the loaded Large tier, got {GetUiScale()}");
                Application.Quit(1);
                yield break;
            }
            if (Mathf.Abs(AnimationTimeScale() - AnimationTimeScaleFor(AnimationSpeedTier.Fast)) > .0001f)
            {
                Debug.LogError($"ALWAYS_FAITHFUL_SETTINGS_REGRESSION_FAILED AnimationTimeScale did not reflect the loaded Fast tier, got {AnimationTimeScale()}");
                Application.Quit(1);
                yield break;
            }

            Debug.Log("ALWAYS_FAITHFUL_SETTINGS_REGRESSION_OK");
            Application.Quit(0);
        }

        private static bool ValidateCardRules(out string failure)
        {
            TacticalBattalionStatus fresh = TacticalBattalion.CreateFresh();
            if (fresh.Hand.Count != 0)
            {
                failure = $"expected a fresh battalion status to start with an empty hand, got {fresh.Hand.Count}";
                return false;
            }
            for (int seed = 1; seed <= 50; seed++)
            {
                TacticalSupportAssetType rolled = TacticalSupportCards.RollAssetType(seed);
                if (!Enum.IsDefined(typeof(TacticalSupportAssetType), rolled))
                {
                    failure = $"RollAssetType returned an undefined asset type {rolled} for seed {seed}";
                    return false;
                }
            }
            var overfull = TacticalBattalion.CreateFresh();
            for (int index = 0; index <= TacticalSupportCards.MaximumHandSize; index++)
                overfull.Hand.Add(new TacticalSupportCard { CardId = index + 1, AssetType = TacticalSupportAssetType.Isr });
            if (TacticalBattalion.Validate(overfull, out _))
            {
                failure = "a hand exceeding the maximum size was accepted";
                return false;
            }
            var badCardId = TacticalBattalion.CreateFresh();
            badCardId.Hand.Add(new TacticalSupportCard { CardId = 0, AssetType = TacticalSupportAssetType.Reserve });
            if (TacticalBattalion.Validate(badCardId, out _))
            {
                failure = "a card with a non-positive CardId was accepted";
                return false;
            }
            var badAssetType = TacticalBattalion.CreateFresh();
            badAssetType.Hand.Add(new TacticalSupportCard { CardId = 1, AssetType = (TacticalSupportAssetType)99 });
            if (TacticalBattalion.Validate(badAssetType, out _))
            {
                failure = "a card with an undefined asset type was accepted";
                return false;
            }
            var wellFormed = TacticalBattalion.CreateFresh();
            wellFormed.Hand.Add(new TacticalSupportCard { CardId = 1, AssetType = TacticalSupportAssetType.FireSupport });
            if (!TacticalBattalion.Validate(wellFormed, out string validError))
            {
                failure = "a structurally valid hand was rejected: " + validError;
                return false;
            }
            failure = null;
            return true;
        }

        private IEnumerator RunCardsRegression()
        {
            yield return null;
            if (!ValidateCardRules(out string ruleFailure))
            {
                Debug.LogError("ALWAYS_FAITHFUL_CARDS_REGRESSION_FAILED rules=" + ruleFailure);
                Application.Quit(1);
                yield break;
            }

            // Isolate this regression from whatever real Battalion status
            // might already exist at the default persistentDataPath location.
            string scratchDirectory = Path.Combine(Path.GetTempPath(), "AlwaysFaithfulCardsRegression");
            Directory.CreateDirectory(scratchDirectory);
            battalionStatusPath = Path.Combine(scratchDirectory, "battalion-status.json");
            battalionStatus = TacticalBattalion.CreateFresh();
            SaveBattalionStatus();

            HexCellView parent = FindHighReliefOperationalCell();
            fastEnemyAnimation = true;
            EnterTacticalMap(parent, false);
            tacticalObjective.TurnLimit = 1;
            EndTacticalTurn();
            float deadline = Time.realtimeSinceStartup + 5f;
            do { yield return null; } while ((tacticalEnemyTurnActive || !resultScreenActive || resultScreenOpacity < .98f) && Time.realtimeSinceStartup < deadline);
            if (battalionStatus.Hand.Count != 1)
            {
                Debug.LogError($"ALWAYS_FAITHFUL_CARDS_REGRESSION_FAILED expected exactly one drawn card after the first engagement, got {battalionStatus.Hand.Count}");
                Application.Quit(1);
                yield break;
            }

            // A BattleRequest-driven battle must never touch the hand.
            int handCountBeforeRequest = battalionStatus.Hand.Count;
            ReturnToIsland(false);
            string requestPath = Path.Combine(scratchDirectory, "request.json");
            var request = new BattleRequest
            {
                RequestId = "cards-regression-request-1",
                CampaignId = "cards-regression-campaign-1",
                Seed = 8181,
                TheaterHex = parent.Coord,
                TurnLimitOverride = 1,
                OutputPath = Path.Combine(scratchDirectory, "result.json")
            };
            File.WriteAllText(requestPath, JsonUtility.ToJson(request));
            TryLoadBattleRequest(requestPath);
            if (battleRequestError != null || activeBattleRequest == null)
            {
                Debug.LogError("ALWAYS_FAITHFUL_CARDS_REGRESSION_FAILED battle request setup rejected: " + battleRequestError);
                Application.Quit(1);
                yield break;
            }
            DismissCampaignBriefing();
            EndTacticalTurn();
            deadline = Time.realtimeSinceStartup + 5f;
            do { yield return null; } while ((tacticalEnemyTurnActive || !resultScreenActive || resultScreenOpacity < .98f) && Time.realtimeSinceStartup < deadline);
            if (battalionStatus.Hand.Count != handCountBeforeRequest)
            {
                Debug.LogError($"ALWAYS_FAITHFUL_CARDS_REGRESSION_FAILED battle-request path touched the hand count={battalionStatus.Hand.Count}/{handCountBeforeRequest}");
                Application.Quit(1);
                yield break;
            }
            ReturnToIsland(false);

            // Pre-seed the hand with a single FireSupport card, force an
            // enemy onto the objective hex (guaranteeing it is within the
            // prep-fire radius), then play the card through the same
            // DismissSupportCardModal method the PLAY button calls, and
            // confirm both the whole-battle effect and hand consumption.
            // (The modal itself only opens outside an automated run, exactly
            // like the campaign-briefing toast it stands in front of, so this
            // regression drives the effect directly rather than the OnGUI
            // button — matching this codebase's existing posture that
            // presentation-only behavior is eyeballed via capture, not
            // asserted headlessly.)
            battalionStatus.Hand.Clear();
            battalionStatus.Hand.Add(new TacticalSupportCard { CardId = ++battalionStatus.NextCardId, AssetType = TacticalSupportAssetType.FireSupport });
            SaveBattalionStatus();
            EnterTacticalMap(parent, false);
            if (battalionStatus.Hand.Count != 1)
            {
                Debug.LogError($"ALWAYS_FAITHFUL_CARDS_REGRESSION_FAILED pre-seeded hand was altered by EnterTacticalMap, count={battalionStatus.Hand.Count}");
                Application.Quit(1);
                yield break;
            }
            TacticalUnitState targetEnemy = tacticalEnemyStates[0];
            targetEnemy.Position = tacticalObjective.ObjectiveHex;
            TacticalSupportCard cardToPlay = battalionStatus.Hand[0];
            DismissSupportCardModal(cardToPlay);
            if (battalionStatus.Hand.Count != 0)
            {
                Debug.LogError($"ALWAYS_FAITHFUL_CARDS_REGRESSION_FAILED playing a card did not remove it from the hand, count={battalionStatus.Hand.Count}");
                Application.Quit(1);
                yield break;
            }
            if (targetEnemy.CombatStatus == TacticalCombatStatus.Ready)
            {
                Debug.LogError("ALWAYS_FAITHFUL_CARDS_REGRESSION_FAILED FireSupport card did not suppress the enemy at the objective");
                Application.Quit(1);
                yield break;
            }
            if (!BuildTacticalEventLog(int.MaxValue).Exists(line => line.StartsWith("SUPPORT •", StringComparison.Ordinal)))
            {
                Debug.LogError("ALWAYS_FAITHFUL_CARDS_REGRESSION_FAILED playing a card did not produce a SUPPORT event-log line");
                Application.Quit(1);
                yield break;
            }
            if (!TryLoadBattalionStatus(battalionStatusPath, out TacticalBattalionStatus roundTripped) || roundTripped.Hand.Count != 0)
            {
                Debug.LogError("ALWAYS_FAITHFUL_CARDS_REGRESSION_FAILED status did not persist/round-trip correctly");
                Application.Quit(1);
                yield break;
            }

            Debug.Log("ALWAYS_FAITHFUL_CARDS_REGRESSION_OK");
            Application.Quit(0);
        }

        // PLA active recon is a core tactical mechanic (not standalone-only
        // campaign meta like support cards), so this regression proves it
        // fires identically for a plain standalone battle AND a BattleRequest-
        // driven one, with no gating either way.
        private IEnumerator RunPlaReconRegression()
        {
            yield return null;
            EnterTacticalMap(FindHighReliefOperationalCell(), false);
            fastEnemyAnimation = true;
            tacticalObjective.TurnLimit = (turnState.TurnNumber - tacticalObjective.BattleStartTurn) + 20;

            if (tacticalUnitSpottedTier == TacticalVisibilityState.Hidden)
            {
                Debug.LogError("ALWAYS_FAITHFUL_PLA_RECON_REGRESSION_FAILED initial live enemy contact did not activate the tracking readout");
                Application.Quit(1);
                yield break;
            }

            if (!TryManufactureLostContact(tacticalEnemyStates[0], out TacticalUnitState targetEnemy))
            {
                Debug.LogError("ALWAYS_FAITHFUL_PLA_RECON_REGRESSION_FAILED could not manufacture an out-of-range platoon position");
                Application.Quit(1);
                yield break;
            }

            int markersBefore = tacticalPlaReconMarkers.Count;
            int actionsBefore = tacticalBattlefield.EnemyActionEvents.Count;
            string targetEnemyId = targetEnemy.Id;
            HexCoord expectedReconHex = targetEnemy.Position;
            EndTacticalTurn();
            float deadline = Time.realtimeSinceStartup + 5f;
            do { yield return null; } while (tacticalEnemyTurnActive && Time.realtimeSinceStartup < deadline);

            if (!EnemyIssuedRecon(targetEnemyId, actionsBefore) || tacticalPlaReconMarkers.Count != markersBefore + 1 ||
                !tacticalPlaReconMarkers.Exists(marker => marker.Hex.Equals(expectedReconHex) && marker.IsVisibleToUsmc) ||
                !tacticalPlaReconRings.ContainsKey(expectedReconHex))
            {
                Debug.LogError($"ALWAYS_FAITHFUL_PLA_RECON_REGRESSION_FAILED recon not issued, markers={tacticalPlaReconMarkers.Count}/{markersBefore + 1} rings={tacticalPlaReconRings.Count}");
                Application.Quit(1);
                yield break;
            }
            if (tacticalUnitSpottedTier != TacticalVisibilityState.Hidden)
            {
                Debug.LogError($"ALWAYS_FAITHFUL_PLA_RECON_REGRESSION_FAILED stale-only enemy contact kept tracking readout active tier={tacticalUnitSpottedTier}");
                Application.Quit(1);
                yield break;
            }

            string scratchDirectory = Path.Combine(Path.GetTempPath(), "AlwaysFaithfulPlaReconRegression");
            Directory.CreateDirectory(scratchDirectory);
            string savePath = Path.Combine(scratchDirectory, "battle-save.json");
            SaveTacticalBattle(savePath);
            TacticalBattleSaveState parsedSave = JsonUtility.FromJson<TacticalBattleSaveState>(File.ReadAllText(savePath));
            if (parsedSave == null || parsedSave.EnemyContacts.Count != tacticalEnemyContacts.Count ||
                parsedSave.Battlefield.ActivePlaReconMarkers.Count != markersBefore + 1)
            {
                Debug.LogError($"ALWAYS_FAITHFUL_PLA_RECON_REGRESSION_FAILED serialized save mismatch enemyContacts={parsedSave?.EnemyContacts.Count} markers={parsedSave?.Battlefield?.ActivePlaReconMarkers.Count}");
                Application.Quit(1);
                yield break;
            }
            if (!TryLoadTacticalBattle(savePath, out string loadError) ||
                !tacticalPlaReconMarkers.Exists(marker => marker.Hex.Equals(expectedReconHex) && marker.IsVisibleToUsmc) ||
                !tacticalPlaReconRings.ContainsKey(expectedReconHex))
            {
                Debug.LogError($"ALWAYS_FAITHFUL_PLA_RECON_REGRESSION_FAILED restored save mismatch error={loadError} markers={tacticalPlaReconMarkers.Count} rings={tacticalPlaReconRings.Count}");
                Application.Quit(1);
                yield break;
            }

            // Two further enemy-turn-ends should decay the marker away (DurationTurns=2).
            for (int index = 0; index < TacticalRecon.DurationTurns; index++)
            {
                EndTacticalTurn();
                deadline = Time.realtimeSinceStartup + 5f;
                do { yield return null; } while (tacticalEnemyTurnActive && Time.realtimeSinceStartup < deadline);
            }
            if (tacticalPlaReconMarkers.Count != markersBefore)
            {
                Debug.LogError($"ALWAYS_FAITHFUL_PLA_RECON_REGRESSION_FAILED marker did not decay, count={tacticalPlaReconMarkers.Count}");
                Application.Quit(1);
                yield break;
            }

            ReturnToIsland(false);
            string requestPath = Path.Combine(scratchDirectory, "request.json");
            var request = new BattleRequest
            {
                RequestId = "pla-recon-regression-request-1",
                CampaignId = "pla-recon-regression-campaign-1",
                Seed = 9191,
                TheaterHex = FindHighReliefOperationalCell().Coord,
                OutputPath = Path.Combine(scratchDirectory, "result.json")
            };
            File.WriteAllText(requestPath, JsonUtility.ToJson(request));
            TryLoadBattleRequest(requestPath);
            if (battleRequestError != null || activeBattleRequest == null)
            {
                Debug.LogError("ALWAYS_FAITHFUL_PLA_RECON_REGRESSION_FAILED battle request setup rejected: " + battleRequestError);
                Application.Quit(1);
                yield break;
            }
            DismissCampaignBriefing();
            tacticalObjective.TurnLimit = (turnState.TurnNumber - tacticalObjective.BattleStartTurn) + 20;

            if (!TryManufactureLostContact(tacticalEnemyStates[0], out TacticalUnitState requestEnemy))
            {
                Debug.LogError("ALWAYS_FAITHFUL_PLA_RECON_REGRESSION_FAILED could not manufacture a BattleRequest lost-contact fixture");
                Application.Quit(1);
                yield break;
            }
            int requestActionsBefore = tacticalBattlefield.EnemyActionEvents.Count;
            EndTacticalTurn();
            deadline = Time.realtimeSinceStartup + 5f;
            do { yield return null; } while (tacticalEnemyTurnActive && Time.realtimeSinceStartup < deadline);
            if (!EnemyIssuedRecon(requestEnemy.Id, requestActionsBefore))
            {
                Debug.LogError("ALWAYS_FAITHFUL_PLA_RECON_REGRESSION_FAILED BattleRequest-driven battle did not allow PLA recon");
                Application.Quit(1);
                yield break;
            }

            Debug.Log("ALWAYS_FAITHFUL_PLA_RECON_REGRESSION_OK");
            Application.Quit(0);
        }

        // Teleports the platoon to whichever board corner is farther from
        // targetEnemy (guaranteed beyond even the most permissive obscured-
        // contact range, so the enemy's next fresh observation check is
        // unconditionally Hidden), then manufactures a recent non-stale
        // contact in tacticalEnemyContacts so TacticalObservation.Check's
        // one-turn stale-carryover fires — exactly the "just lost contact"
        // condition PlanOrder's Recon branch looks for. Also manufactures the
        // player's own contact on that enemy so the resulting order's
        // presentation (ring, non-hidden log line) actually renders.
        private bool TryManufactureLostContact(TacticalUnitState targetEnemy, out TacticalUnitState enemy)
        {
            enemy = targetEnemy;
            HexCoord cornerA = new HexCoord(0, 0);
            HexCoord cornerB = new HexCoord(tacticalBattlefield.Width - 1, tacticalBattlefield.Height - 1);
            HexCoord farHex = HexCoord.Distance(cornerA, targetEnemy.Position) >= HexCoord.Distance(cornerB, targetEnemy.Position) ? cornerA : cornerB;
            if (HexCoord.Distance(farHex, targetEnemy.Position) <= TacticalObservation.ObscuredContactRangeHexes) return false;

            localMovementBoard[tacticalUnitState.Position].OccupantId = null;
            tacticalUnitState.Position = farHex;
            localMovementBoard[farHex].OccupantId = tacticalUnitState.Id;

            // Isolate this fixture to one intended recon-capable formation.
            // Without clearing the live pre-teleport picture, every formation
            // legitimately inherits a stale contact and the regression tests a
            // multi-unit sweep instead of the single order it is meant to prove.
            tacticalEnemyContacts.Clear();
            tacticalEnemyContacts[targetEnemy.Id] = new TacticalContactState
            {
                TargetId = tacticalUnitState.Id,
                DisplayName = tacticalUnitState.DisplayName,
                State = TacticalVisibilityState.Contact,
                LastKnownPosition = targetEnemy.Position,
                LastObservedTurn = turnState.TurnNumber,
                IsStale = false,
                ObserverId = targetEnemy.Id,
                RangeHexes = 1,
                LineOfSight = TacticalLosState.Clear
            };
            tacticalContacts[targetEnemy.Id] = new TacticalContactState
            {
                TargetId = targetEnemy.Id,
                DisplayName = targetEnemy.DisplayName,
                State = TacticalVisibilityState.Observed,
                LastKnownPosition = targetEnemy.Position,
                LastObservedTurn = turnState.TurnNumber,
                IsStale = false,
                ObserverId = tacticalUnitState.Id,
                RangeHexes = 1,
                LineOfSight = TacticalLosState.Clear
            };
            return true;
        }

        private bool EnemyIssuedRecon(string enemyId, int sinceIndex)
        {
            for (int index = sinceIndex; index < tacticalBattlefield.EnemyActionEvents.Count; index++)
                if (tacticalBattlefield.EnemyActionEvents[index].UnitId == enemyId && tacticalBattlefield.EnemyActionEvents[index].Kind == TacticalAiOrderKind.Recon)
                    return true;
            return false;
        }

        private IEnumerator RunOperationalScenarioRegression()
        {
            yield return null;
            if (!OperationalScenarioRules.Validate(operationalScenario, out string validationError) ||
                operationalFriendlyBattalions.Count != 2 || operationalEnemyBattalions.Count != 3 ||
                operationalContacts.Count != operationalEnemyBattalions.Count ||
                string.IsNullOrWhiteSpace(operationalScenario.Situation) || string.IsNullOrWhiteSpace(operationalScenario.Mission) ||
                string.IsNullOrWhiteSpace(operationalScenario.Execution) || string.IsNullOrWhiteSpace(operationalScenario.IntelligenceEstimate) ||
                operationalFriendlyBattalions.Exists(battalion => battalion.SubordinateUnits.Count == 0) ||
                operationalEnemyBattalions.Exists(battalion => battalion.SubordinateUnits.Count == 0))
            {
                Debug.LogError($"ALWAYS_FAITHFUL_OPERATIONAL_SCENARIO_REGRESSION_FAILED setup error={validationError} friendly={operationalFriendlyBattalions.Count} enemy={operationalEnemyBattalions.Count} contacts={operationalContacts.Count}");
                Application.Quit(1);
                yield break;
            }

            var observer = new OperationalBattalionState { Id = "observer", Position = new HexCoord(0, 0) };
            var target = new OperationalBattalionState { Id = "target", Position = new HexCoord(0, 11) };
            var observers = new List<OperationalBattalionState> { observer };
            OperationalContactState hidden = OperationalScenarioRules.Observe(target, observers, new List<OperationalReconMarker>(), 1);
            var recon = new List<OperationalReconMarker> { new OperationalReconMarker { Hex = target.Position, TurnsRemaining = 2 } };
            OperationalContactState swept = OperationalScenarioRules.Observe(target, observers, recon, 1);
            target.Position = new HexCoord(0, 2);
            OperationalContactState observed = OperationalScenarioRules.Observe(target, observers, new List<OperationalReconMarker>(), 1);
            target.Position = new HexCoord(0, 11);
            OperationalContactState stale = OperationalScenarioRules.Observe(target, observers, new List<OperationalReconMarker>(), 2, observed);
            OperationalContactState expired = OperationalScenarioRules.Observe(target, observers, new List<OperationalReconMarker>(), 3, stale);
            if (hidden.State != TacticalVisibilityState.Hidden || swept.State != TacticalVisibilityState.Contact ||
                observed.State != TacticalVisibilityState.Observed || !stale.IsStale || stale.State != TacticalVisibilityState.Contact ||
                expired.State != TacticalVisibilityState.Hidden)
            {
                Debug.LogError($"ALWAYS_FAITHFUL_OPERATIONAL_SCENARIO_REGRESSION_FAILED detection hidden={hidden.State} swept={swept.State} observed={observed.State} stale={stale.State}/{stale.IsStale} expired={expired.State}");
                Application.Quit(1);
                yield break;
            }

            var outcomeFixture = new OperationalScenarioState { PrimaryObjective = new HexCoord(4, 4) };
            outcomeFixture.FriendlyBattalions.Add(new OperationalBattalionState { Id = "friendly", DisplayName = "Friendly", Side = OperationalSide.Usmc });
            var objectiveEnemy = new OperationalBattalionState
                { Id = "enemy", DisplayName = "Enemy", Side = OperationalSide.Pla, Position = outcomeFixture.PrimaryObjective, Strength = 50 };
            outcomeFixture.EnemyBattalions.Add(objectiveEnemy);
            if (OperationalScenarioRules.Evaluate(outcomeFixture, out _) != OperationalScenarioOutcome.PlaVictory)
            {
                Debug.LogError("ALWAYS_FAITHFUL_OPERATIONAL_SCENARIO_REGRESSION_FAILED objective seizure did not produce PLA victory");
                Application.Quit(1);
                yield break;
            }
            objectiveEnemy.Strength = 0;
            if (OperationalScenarioRules.Evaluate(outcomeFixture, out _) != OperationalScenarioOutcome.UsmcVictory)
            {
                Debug.LogError("ALWAYS_FAITHFUL_OPERATIONAL_SCENARIO_REGRESSION_FAILED ineffective enemy force did not produce USMC victory");
                Application.Quit(1);
                yield break;
            }

            string json = JsonUtility.ToJson(operationalScenario);
            OperationalScenarioState restored = JsonUtility.FromJson<OperationalScenarioState>(json);
            if (!OperationalScenarioRules.Validate(restored, out _) || restored.FriendlyBattalions.Count != 2 ||
                restored.EnemyBattalions.Count != 3 || restored.PrimaryObjective.Equals(default))
            {
                Debug.LogError("ALWAYS_FAITHFUL_OPERATIONAL_SCENARIO_REGRESSION_FAILED scenario did not round-trip");
                Application.Quit(1);
                yield break;
            }

            int reconApBefore = unitState.RemainingActionPoints;
            HexCoord reconTarget = unitState.Position;
            BeginOperationalReconPlanning();
            if (!TryIssueOperationalRecon(reconTarget) ||
                unitState.RemainingActionPoints != reconApBefore - OperationalScenarioRules.ReconActionPointCost ||
                !operationalReconRings.ContainsKey(reconTarget))
            {
                Debug.LogError($"ALWAYS_FAITHFUL_OPERATIONAL_SCENARIO_REGRESSION_FAILED recon interaction ap={unitState.RemainingActionPoints}/{reconApBefore - OperationalScenarioRules.ReconActionPointCost} ring={operationalReconRings.ContainsKey(reconTarget)}");
                Application.Quit(1);
                yield break;
            }

            OperationalBattalionState liveEnemy = operationalEnemyBattalions[0];
            operationalReconMarkers.Add(new OperationalReconMarker { Hex = liveEnemy.Position, TurnsRemaining = 2 });
            RefreshOperationalObservation();
            if (operationalContacts[liveEnemy.Id].State == TacticalVisibilityState.Hidden || !operationalEnemyViews[liveEnemy.Id].gameObject.activeSelf)
            {
                Debug.LogError("ALWAYS_FAITHFUL_OPERATIONAL_SCENARIO_REGRESSION_FAILED live recon did not reveal an enemy contact");
                Application.Quit(1);
                yield break;
            }

            HexCoord enemyBefore = liveEnemy.Position;
            RunOperationalEnemyTurn();
            if (liveEnemy.Position.Equals(enemyBefore) || HexCoord.Distance(liveEnemy.Position, liveEnemy.ObjectiveHex) >= HexCoord.Distance(enemyBefore, liveEnemy.ObjectiveHex))
            {
                Debug.LogError($"ALWAYS_FAITHFUL_OPERATIONAL_SCENARIO_REGRESSION_FAILED enemy maneuver before={enemyBefore} after={liveEnemy.Position} objective={liveEnemy.ObjectiveHex}");
                Application.Quit(1);
                yield break;
            }

            Debug.Log($"ALWAYS_FAITHFUL_OPERATIONAL_SCENARIO_REGRESSION_OK friendly={operationalFriendlyBattalions.Count} enemy={operationalEnemyBattalions.Count} objective={operationalScenario.PrimaryObjective}");
            Application.Quit(0);
        }

        private IEnumerator CaptureOperationalScenarioWhenRequested()
        {
            string argument = Array.Find(Environment.GetCommandLineArgs(), value => value.StartsWith("--operational-scenario-capture-path=", StringComparison.Ordinal));
            if (argument == null) yield break;
            string path = argument.Substring("--operational-scenario-capture-path=".Length);
            yield return null;
            operationalBriefingActive = false;
            OperationalBattalionState enemy = operationalEnemyBattalions[0];
            operationalReconMarkers.Add(new OperationalReconMarker { Hex = enemy.Position, TurnsRemaining = 2 });
            BuildOperationalReconRing(enemy.Position);
            RefreshOperationalObservation();
            operationalOrderOfBattleOpen = true;
            cameraFocus = Vector3.Lerp(cells[activeOperationalBattalion.Position].transform.position, cells[enemy.Position].transform.position, .5f);
            cameraDistance = 78f;
            ApplyCamera();

            var target = new RenderTexture(1280, 720, 24);
            var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active;
            mapCamera.targetTexture = target;
            mapCamera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0f, 0f, target.width, target.height), 0, 0);
            image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
            mapCamera.targetTexture = null;
            RenderTexture.active = previous;
            Destroy(target);
            Destroy(image);
            int visibleContacts = 0;
            foreach (OperationalContactState contact in operationalContacts.Values)
                if (contact.State != TacticalVisibilityState.Hidden) visibleContacts++;
            Debug.Log($"ALWAYS_FAITHFUL_OPERATIONAL_SCENARIO_CAPTURED path={path} friendly={operationalFriendlyBattalions.Count} visibleContacts={visibleContacts}");
            Application.Quit(0);
        }

        private static bool ValidateFireRules(out string failure)
        {
            var flat = new Dictionary<HexCoord, TacticalMovementCell>();
            for (int row = 0; row <= 10; row++)
            {
                var coord = new HexCoord(0, row);
                flat[coord] = new TacticalMovementCell { Coord = coord, Terrain = TacticalTerrain.Open, ElevationMetres = 20f };
            }
            HexCoord origin = new HexCoord(0, 0);
            HexCoord target = new HexCoord(0, 3);
            var observed = new TacticalContactState
            {
                TargetId = "target", DisplayName = "Target", State = TacticalVisibilityState.Observed,
                LastKnownPosition = target, LastObservedTurn = 1, ObserverId = "attacker"
            };
            var firstWeapon = new TacticalWeaponState("rifle", "Rifle", 6);
            TacticalFirePreview preview = TacticalDirectFire.Preview(flat, "attacker", origin, observed, target, firstWeapon, 8);
            int modifierTotal = 0;
            foreach (TacticalFireModifier modifier in preview.Modifiers) modifierTotal += modifier.Value;
            if (!preview.IsValid || preview.RangeHexes != 3 || preview.HitChance != 66 || preview.SuppressionChance != 86 || modifierTotal != preview.HitChance ||
                preview.ExpectedEffect != "HIGH")
            {
                failure = "preview modifier accounting";
                return false;
            }
            int seed = TacticalDirectFire.CreateSeed("fixture", 2, 4);
            TacticalFireEvent first = TacticalDirectFire.Resolve(preview, firstWeapon, seed);
            var secondWeapon = new TacticalWeaponState("rifle", "Rifle", 6);
            TacticalFirePreview secondPreview = TacticalDirectFire.Preview(flat, "attacker", origin, observed, target, secondWeapon, 8);
            TacticalFireEvent second = TacticalDirectFire.Resolve(secondPreview, secondWeapon, seed);
            if (first.Roll != second.Roll || first.Outcome != second.Outcome || first.AmmunitionAfter != 5 ||
                second.AmmunitionAfter != 5 || first.Outcome == TacticalFireOutcome.Rejected)
            {
                failure = "identical-seed replay or ammunition expenditure";
                return false;
            }

            var rough = CopyMovementFixture(flat, 0, 3);
            rough[target].Terrain = TacticalTerrain.Rough;
            TacticalFirePreview roughPreview = TacticalDirectFire.Preview(rough, "attacker", origin, observed, target,
                new TacticalWeaponState("rifle", "Rifle", 1), 8);
            if (!roughPreview.IsValid || roughPreview.HitChance != 48 || roughPreview.Modifiers.Count != 3)
            {
                failure = "terrain modifier";
                return false;
            }

            var identified = new TacticalContactState { TargetId = "target", State = TacticalVisibilityState.Identified, LastKnownPosition = target };
            var illegalWeapon = new TacticalWeaponState("rifle", "Rifle", 2);
            TacticalFirePreview illegal = TacticalDirectFire.Preview(flat, "attacker", origin, identified, target, illegalWeapon, 8);
            TacticalFireEvent rejected = TacticalDirectFire.Resolve(illegal, illegalWeapon, seed);
            var blocked = CopyMovementFixture(flat, 0, 4);
            blocked[new HexCoord(0, 2)].ElevationMetres = 100f;
            var blockedContact = new TacticalContactState { TargetId = "target", State = TacticalVisibilityState.Observed, LastKnownPosition = new HexCoord(0, 4) };
            TacticalFirePreview blockedPreview = TacticalDirectFire.Preview(blocked, "attacker", origin, blockedContact,
                blockedContact.LastKnownPosition, illegalWeapon, 8);
            var empty = new TacticalWeaponState("rifle", "Rifle", 1) { RemainingAmmunition = 0 };
            TacticalFirePreview emptyPreview = TacticalDirectFire.Preview(flat, "attacker", origin, observed, target, empty, 8);
            var distantContact = new TacticalContactState { TargetId = "target", State = TacticalVisibilityState.Observed, LastKnownPosition = new HexCoord(0, 9) };
            TacticalFirePreview distant = TacticalDirectFire.Preview(flat, "attacker", origin, distantContact, distantContact.LastKnownPosition, illegalWeapon, 8);
            if (illegal.IsValid || rejected.Outcome != TacticalFireOutcome.Rejected || illegalWeapon.RemainingAmmunition != 2 ||
                blockedPreview.IsValid || emptyPreview.IsValid || distant.IsValid)
            {
                failure = "illegal target, LOS, range, or empty-ammunition gate";
                return false;
            }
            failure = null;
            return true;
        }

        private static bool ValidateSuppressionRules(out string failure)
        {
            var probe = new TacticalUnitState("suppression-test-unit", "Test Unit", new HexCoord(0, 0), 4);
            if (probe.CombatStatus != TacticalCombatStatus.Ready || !probe.CanMove || !probe.CanFire || probe.CanRally ||
                TacticalSuppression.ApplyFireOutcome(probe, TacticalFireOutcome.Miss) != null)
            {
                failure = "initial combat status or miss should not suppress";
                return false;
            }
            TacticalSuppressionEvent first = TacticalSuppression.ApplyFireOutcome(probe, TacticalFireOutcome.Suppressed);
            if (first == null || probe.SuppressionPoints != TacticalSuppression.SuppressedFirePoints ||
                probe.CombatStatus != TacticalCombatStatus.Suppressed || !probe.CanMove || !probe.CanFire || !probe.CanRally)
            {
                failure = "single suppression threshold";
                return false;
            }
            TacticalSuppressionEvent second = TacticalSuppression.ApplyFireOutcome(probe, TacticalFireOutcome.Suppressed);
            if (second == null || second.PointsBefore != TacticalSuppression.SuppressedFirePoints ||
                probe.CombatStatus != TacticalCombatStatus.Disrupted || !probe.CanMove || probe.CanFire)
            {
                failure = "cumulative suppression to disrupted";
                return false;
            }
            TacticalSuppressionEvent third = TacticalSuppression.ApplyFireOutcome(probe, TacticalFireOutcome.Hit);
            if (third == null || probe.CombatStatus != TacticalCombatStatus.Reduced || probe.CanMove || probe.CanFire || !probe.CanRally)
            {
                failure = "reduced restrictions";
                return false;
            }
            int pointsBeforeRally = probe.SuppressionPoints;
            TacticalSuppressionEvent rally = TacticalSuppression.ApplyRally(probe);
            if (rally == null || probe.RemainingActionPoints != 3 ||
                probe.SuppressionPoints != pointsBeforeRally - TacticalSuppression.RallyRecoveryAmount ||
                probe.CombatStatus != TacticalCombatStatus.Disrupted)
            {
                failure = "rally recovery amount or AP cost";
                return false;
            }
            TacticalSuppression.ApplyRally(probe);
            if (probe.CombatStatus != TacticalCombatStatus.Ready || probe.CanRally || TacticalSuppression.ApplyRally(probe) != null)
            {
                failure = "recovery back to ready or rally rejected once ready";
                return false;
            }

            var passiveProbe = new TacticalUnitState("passive-test-unit", "Passive Test Unit", new HexCoord(0, 0), 4);
            TacticalSuppression.ApplyFireOutcome(passiveProbe, TacticalFireOutcome.Suppressed);
            passiveProbe.TryBeginMove(2);
            passiveProbe.BeginTurn();
            if (passiveProbe.SuppressionPoints != TacticalSuppression.SuppressedFirePoints - TacticalSuppression.PassiveRecoveryAmount ||
                passiveProbe.CombatStatus != TacticalCombatStatus.Ready || passiveProbe.RemainingActionPoints != 4)
            {
                failure = "passive turn recovery";
                return false;
            }

            string serialized = JsonUtility.ToJson(first);
            TacticalSuppressionEvent restoredEvent = JsonUtility.FromJson<TacticalSuppressionEvent>(serialized);
            string unitSerialized = JsonUtility.ToJson(probe);
            TacticalUnitState restoredUnit = JsonUtility.FromJson<TacticalUnitState>(unitSerialized);
            if (restoredEvent == null || restoredEvent.PointsAfter != first.PointsAfter || restoredEvent.StatusAfter != first.StatusAfter ||
                restoredUnit == null || restoredUnit.SuppressionPoints != probe.SuppressionPoints || restoredUnit.CombatStatus != probe.CombatStatus)
            {
                failure = "suppression state serialization";
                return false;
            }
            failure = null;
            return true;
        }

        private static bool ValidateReactionRules(out string failure)
        {
            var flat = new Dictionary<HexCoord, TacticalMovementCell>();
            for (int row = 0; row <= 10; row++)
            {
                var coord = new HexCoord(0, row);
                flat[coord] = new TacticalMovementCell { Coord = coord, Terrain = TacticalTerrain.Open, ElevationMetres = 20f };
            }
            HexCoord reactorPosition = new HexCoord(0, 0);
            HexCoord moverPosition = new HexCoord(0, 3);
            var reactor = new TacticalUnitState("reactor-1", "Reactor One", reactorPosition, 4);
            var weapon = new TacticalWeaponState("rifle", "Rifle", 6);
            var soleCandidate = new List<TacticalReactionCandidate> { new TacticalReactionCandidate(reactor, weapon) };

            TacticalReactionCandidate? selected = TacticalReactionFire.SelectReactor(flat, soleCandidate, moverPosition, out TacticalLosResult los);
            if (!selected.HasValue || selected.Value.Unit.Id != reactor.Id || los == null || los.RangeHexes != 3)
            {
                failure = "eligible range/LOS selection";
                return false;
            }

            TacticalFirePreview preview = TacticalReactionFire.Preview(flat, reactor, weapon, moverPosition, los);
            int modifierTotal = 0;
            foreach (TacticalFireModifier modifier in preview.Modifiers) modifierTotal += modifier.Value;
            if (!preview.IsValid || modifierTotal != preview.HitChance || preview.HitChance >= TacticalDirectFire.BaseHitChance)
            {
                failure = "reaction preview modifier accounting";
                return false;
            }

            HexCoord farPosition = new HexCoord(0, 9);
            TacticalReactionCandidate? tooFar = TacticalReactionFire.SelectReactor(flat, soleCandidate, farPosition, out _);
            if (tooFar.HasValue)
            {
                failure = "out-of-range reactor was selected";
                return false;
            }

            var blocked = CopyMovementFixture(flat, 0, 4);
            blocked[new HexCoord(0, 2)].ElevationMetres = 100f;
            TacticalReactionCandidate? blockedSelection = TacticalReactionFire.SelectReactor(blocked, soleCandidate, new HexCoord(0, 4), out _);
            if (blockedSelection.HasValue)
            {
                failure = "blocked-LOS reactor was selected";
                return false;
            }

            var reducedReactor = new TacticalUnitState("reactor-reduced", "Reactor Reduced", reactorPosition, 4);
            reducedReactor.ApplySuppressionPoints(TacticalSuppression.ReducedThreshold);
            var reducedCandidate = new List<TacticalReactionCandidate> { new TacticalReactionCandidate(reducedReactor, new TacticalWeaponState("rifle", "Rifle", 6)) };
            if (TacticalReactionFire.SelectReactor(flat, reducedCandidate, moverPosition, out _).HasValue)
            {
                failure = "reduced reactor was eligible";
                return false;
            }

            var emptyReactor = new TacticalUnitState("reactor-empty", "Reactor Empty", reactorPosition, 4);
            var emptyCandidate = new List<TacticalReactionCandidate>
            {
                new TacticalReactionCandidate(emptyReactor, new TacticalWeaponState("rifle", "Rifle", 1) { RemainingAmmunition = 0 })
            };
            if (TacticalReactionFire.SelectReactor(flat, emptyCandidate, moverPosition, out _).HasValue)
            {
                failure = "reactor with no ammunition was eligible";
                return false;
            }

            var nearReactor = new TacticalUnitState("reactor-near", "Reactor Near", new HexCoord(0, 2), 4);
            var farReactor = new TacticalUnitState("reactor-far", "Reactor Far", new HexCoord(0, 0), 4);
            var byDistance = new List<TacticalReactionCandidate>
            {
                new TacticalReactionCandidate(farReactor, new TacticalWeaponState("rifle", "Rifle", 6)),
                new TacticalReactionCandidate(nearReactor, new TacticalWeaponState("rifle", "Rifle", 6))
            };
            TacticalReactionCandidate? nearest = TacticalReactionFire.SelectReactor(flat, byDistance, moverPosition, out _);
            if (!nearest.HasValue || nearest.Value.Unit.Id != nearReactor.Id)
            {
                failure = "nearest-reactor ordering";
                return false;
            }

            var tieA = new TacticalUnitState("reactor-a", "Reactor A", reactorPosition, 4);
            var tieB = new TacticalUnitState("reactor-b", "Reactor B", reactorPosition, 4);
            var tieOrderOne = new List<TacticalReactionCandidate>
            {
                new TacticalReactionCandidate(tieB, new TacticalWeaponState("rifle", "Rifle", 6)),
                new TacticalReactionCandidate(tieA, new TacticalWeaponState("rifle", "Rifle", 6))
            };
            var tieOrderTwo = new List<TacticalReactionCandidate>
            {
                new TacticalReactionCandidate(tieA, new TacticalWeaponState("rifle", "Rifle", 6)),
                new TacticalReactionCandidate(tieB, new TacticalWeaponState("rifle", "Rifle", 6))
            };
            TacticalReactionCandidate? tieResultOne = TacticalReactionFire.SelectReactor(flat, tieOrderOne, moverPosition, out _);
            TacticalReactionCandidate? tieResultTwo = TacticalReactionFire.SelectReactor(flat, tieOrderTwo, moverPosition, out _);
            if (!tieResultOne.HasValue || !tieResultTwo.HasValue ||
                tieResultOne.Value.Unit.Id != "reactor-a" || tieResultTwo.Value.Unit.Id != "reactor-a")
            {
                failure = "deterministic tie-break ordering";
                return false;
            }
            failure = null;
            return true;
        }

        private static bool ValidateObservationRules(out string failure)
        {
            var flat = new Dictionary<HexCoord, TacticalMovementCell>();
            for (int row = 0; row <= 12; row++)
            {
                var coord = new HexCoord(0, row);
                flat[coord] = new TacticalMovementCell { Coord = coord, Terrain = TacticalTerrain.Open, ElevationMetres = 20f };
            }
            HexCoord origin = new HexCoord(0, 0);
            TacticalContactState observed = TacticalObservation.Check(flat, "observer", origin, "target", "Target", new HexCoord(0, 3), 1);
            TacticalContactState identified = TacticalObservation.Check(flat, "observer", origin, "target", "Target", new HexCoord(0, 10), 1);
            var rough = CopyMovementFixture(flat, 0, 9);
            rough[new HexCoord(0, 2)].Terrain = TacticalTerrain.Rough;
            rough[new HexCoord(0, 2)].ElevationMetres = 0f;
            TacticalContactState contact = TacticalObservation.Check(rough, "observer", origin, "target", "Target", new HexCoord(0, 8), 1);
            var concealed = CopyMovementFixture(flat, 0, 10);
            concealed[new HexCoord(0, 10)].Terrain = TacticalTerrain.Highland;
            TacticalContactState terrainContact = TacticalObservation.Check(concealed, "observer", origin, "target", "Target", new HexCoord(0, 10), 1);
            if (observed.State != TacticalVisibilityState.Observed || !observed.CanAttack ||
                identified.State != TacticalVisibilityState.Identified || identified.CanAttack ||
                contact.State != TacticalVisibilityState.Contact || contact.CanAttack ||
                terrainContact.State != TacticalVisibilityState.Contact)
            {
                failure = "deterministic visibility bands or attack gate";
                return false;
            }

            var blocked = CopyMovementFixture(flat, 0, 6);
            blocked[new HexCoord(0, 2)].ElevationMetres = 100f;
            TacticalContactState stale = TacticalObservation.Check(blocked, "observer", origin, "target", "Target", new HexCoord(0, 6), 2, observed);
            TacticalContactState expired = TacticalObservation.Check(blocked, "observer", origin, "target", "Target", new HexCoord(0, 6), 3, stale);
            if (stale.State != TacticalVisibilityState.Contact || !stale.IsStale || stale.LastObservedTurn != 1 || stale.CanAttack ||
                expired.State != TacticalVisibilityState.Hidden || expired.IsStale ||
                TacticalObservation.CanAttack(observed, new HexCoord(0, 4)) || !TacticalObservation.CanAttack(observed, observed.LastKnownPosition))
            {
                failure = "stale-contact transition or reported-position attack gate";
                return false;
            }

            var snapshot = new TacticalObservationSnapshot { Contacts = new List<TacticalContactState> { observed, identified, contact, stale } };
            TacticalObservationSnapshot restored = JsonUtility.FromJson<TacticalObservationSnapshot>(JsonUtility.ToJson(snapshot));
            if (restored == null || restored.SchemaVersion != 1 || restored.Contacts.Count != 4 ||
                restored.Contacts[3].State != TacticalVisibilityState.Contact || !restored.Contacts[3].IsStale ||
                restored.Contacts[3].ObserverId != "observer")
            {
                failure = "visibility save/reload";
                return false;
            }
            failure = null;
            return true;
        }

        private static bool ValidateLosRules(out string failure)
        {
            var flat = new Dictionary<HexCoord, TacticalMovementCell>();
            for (int r = 0; r <= 13; r++)
            {
                var coord = new HexCoord(0, r);
                flat[coord] = new TacticalMovementCell { Coord = coord, Terrain = TacticalTerrain.Open, ElevationMetres = 20f };
            }
            TacticalLosResult adjacent = TacticalLineOfSight.Inspect(flat, new HexCoord(0, 0), new HexCoord(0, 1));
            TacticalLosResult sameHeight = TacticalLineOfSight.Inspect(flat, new HexCoord(0, 0), new HexCoord(0, 6));
            TacticalLosResult maximum = TacticalLineOfSight.Inspect(flat, new HexCoord(0, 0), new HexCoord(0, 12));
            TacticalLosResult beyond = TacticalLineOfSight.Inspect(flat, new HexCoord(0, 0), new HexCoord(0, 13));
            if (!adjacent.IsValid || adjacent.State != TacticalLosState.Clear || adjacent.Samples.Count != 2 ||
                !sameHeight.IsValid || sameHeight.State != TacticalLosState.Clear ||
                !maximum.IsValid || maximum.RangeHexes != TacticalLineOfSight.MaximumInspectionRangeHexes ||
                beyond.IsValid || !beyond.RejectionReason.StartsWith("Beyond", StringComparison.Ordinal))
            {
                failure = "adjacent, same-height, or maximum range";
                return false;
            }

            var ridge = CopyMovementFixture(flat, 0, 6);
            ridge[new HexCoord(0, 3)].ElevationMetres = 100f;
            TacticalLosResult ridgeResult = TacticalLineOfSight.Inspect(ridge, new HexCoord(0, 0), new HexCoord(0, 6));
            if (ridgeResult.State != TacticalLosState.Blocked || !ridgeResult.BlockingCell.Equals(new HexCoord(0, 3)) ||
                ridgeResult.Samples[2].State != TacticalLosState.Clear || ridgeResult.Samples[3].State != TacticalLosState.Blocked ||
                ridgeResult.Samples[6].State != TacticalLosState.Blocked)
            {
                failure = "ridge blocking or segment transition";
                return false;
            }

            var reverseSlope = CopyMovementFixture(flat, 0, 4);
            reverseSlope[new HexCoord(0, 0)].ElevationMetres = 100f;
            reverseSlope[new HexCoord(0, 1)].ElevationMetres = 80f;
            reverseSlope[new HexCoord(0, 4)].ElevationMetres = 0f;
            TacticalLosResult reverseResult = TacticalLineOfSight.Inspect(reverseSlope, new HexCoord(0, 0), new HexCoord(0, 4));
            if (reverseResult.State != TacticalLosState.Blocked || !reverseResult.BlockingCell.Equals(new HexCoord(0, 1)))
            {
                failure = "reverse-slope blocking";
                return false;
            }

            var obscured = CopyMovementFixture(flat, 0, 5);
            obscured[new HexCoord(0, 2)].Terrain = TacticalTerrain.Rough;
            obscured[new HexCoord(0, 2)].ElevationMetres = 0f;
            TacticalLosResult obscuredResult = TacticalLineOfSight.Inspect(obscured, new HexCoord(0, 0), new HexCoord(0, 5));
            if (obscuredResult.State != TacticalLosState.Obscured ||
                obscuredResult.Samples[1].State != TacticalLosState.Clear ||
                obscuredResult.Samples[2].State != TacticalLosState.Obscured ||
                obscuredResult.Samples[5].State != TacticalLosState.Obscured)
            {
                failure = "intervening terrain obscuration";
                return false;
            }

            var mapEdge = CopyMovementFixture(flat, 0, 4);
            mapEdge.Remove(new HexCoord(0, 2));
            TacticalLosResult edgeResult = TacticalLineOfSight.Inspect(mapEdge, new HexCoord(0, 0), new HexCoord(0, 4));
            if (edgeResult.IsValid || edgeResult.RejectionReason != "LOS crosses map edge")
            {
                failure = "map-edge containment";
                return false;
            }
            failure = null;
            return true;
        }

        private static Dictionary<HexCoord, TacticalMovementCell> CopyMovementFixture(
            IReadOnlyDictionary<HexCoord, TacticalMovementCell> source, int firstRow, int lastRow)
        {
            var copy = new Dictionary<HexCoord, TacticalMovementCell>();
            for (int row = firstRow; row <= lastRow; row++)
            {
                HexCoord coord = new HexCoord(0, row);
                TacticalMovementCell cell = source[coord];
                copy[coord] = new TacticalMovementCell
                {
                    Coord = coord,
                    Terrain = cell.Terrain,
                    ElevationMetres = cell.ElevationMetres,
                    OccupantId = cell.OccupantId
                };
            }
            return copy;
        }

        private static bool BattlefieldsMatch(TacticalBattlefieldState first, TacticalBattlefieldState second, out string failure)
        {
            if (first.BattlefieldId != second.BattlefieldId || !first.ParentHex.Equals(second.ParentHex) ||
                first.OriginLongitude != second.OriginLongitude || first.OriginLatitude != second.OriginLatitude ||
                first.Cells.Count != second.Cells.Count)
            {
                failure = "header mismatch";
                return false;
            }
            for (int index = 0; index < first.Cells.Count; index++)
            {
                TacticalBattlefieldCell a = first.Cells[index];
                TacticalBattlefieldCell b = second.Cells[index];
                if (a.Id != b.Id || !a.LocalCoord.Equals(b.LocalCoord) || a.Longitude != b.Longitude ||
                    a.Latitude != b.Latitude || a.ElevationMetres != b.ElevationMetres || a.Terrain != b.Terrain ||
                    a.Cover != b.Cover || a.IsBuiltUp != b.IsBuiltUp)
                {
                    failure = "cell mismatch at " + index;
                    return false;
                }
            }
            failure = null;
            return true;
        }

        private bool ValidateAuthoritativeState(out string failure)
        {
            var probe = new TacticalUnitState("test-unit", "Test Unit", new HexCoord(2, 3), 4);
            if (probe.TryBeginMove(0) || probe.TryBeginMove(5) || probe.RemainingActionPoints != 4)
            {
                failure = "illegal AP orders mutated state";
                return false;
            }
            if (!probe.TryBeginMove(3) || probe.RemainingActionPoints != 1 || probe.Readiness != UnitReadiness.Moving)
            {
                failure = "legal order was not committed";
                return false;
            }
            var destination = new HexCoord(4, 5);
            probe.CompleteMove(destination);
            if (!probe.Position.Equals(destination) || probe.Readiness != UnitReadiness.Available || probe.TryBeginMove(2))
            {
                failure = "completion or remaining-AP validation failed";
                return false;
            }
            var turnProbe = new TacticalTurnState();
            turnProbe.EndTurn(probe);
            string serialized = JsonUtility.ToJson(probe);
            TacticalUnitState restored = JsonUtility.FromJson<TacticalUnitState>(serialized);
            if (turnProbe.TurnNumber != 2 || restored == null || restored.RemainingActionPoints != 4 ||
                restored.Readiness != UnitReadiness.Available || !restored.Position.Equals(destination) || !unit.Matches(unitState))
            {
                failure = "turn reset, serialization, or view agreement failed";
                return false;
            }
            failure = null;
            return true;
        }

        private void DisplayCapturePath()
        {
            HexCoord destination = unitState.Position;
            int greatestCost = -1;
            foreach (KeyValuePair<HexCoord, int> pair in reachable)
            {
                if (pair.Value < greatestCost) continue;
                if (pair.Value == greatestCost && (pair.Key.Q < destination.Q || pair.Key.Q == destination.Q && pair.Key.R <= destination.R)) continue;
                destination = pair.Key;
                greatestCost = pair.Value;
            }
            DisplayMovementPath(MovementPlanner.FindPath(board, unitState.Position, destination, unitState.RemainingActionPoints));
        }

        private void CreatePathMarker(Vector3 position, bool destination)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = destination ? "Movement Destination" : "Movement Waypoint";
            marker.layer = LayerMask.NameToLayer("Ignore Raycast");
            marker.transform.SetParent(overviewRoot.transform, false);
            marker.transform.position = position;
            marker.transform.localScale = Vector3.one * (destination ? .24f : .15f);
            Color color = destination ? new Color(1f, .78f, .20f) : new Color(.52f, .96f, .79f);
            Material material = NewOverlayMaterial(color);
            marker.GetComponent<MeshRenderer>().sortingOrder = destination ? 56 : 55;
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * .65f);
            }
            marker.GetComponent<MeshRenderer>().sharedMaterial = material;
            Destroy(marker.GetComponent<Collider>());
            pathMarkers.Add(marker);
        }

        private void UpdateCamera()
        {
            float minimumDistance = tacticalMode ? 4f : 8f;
            float maximumDistance = tacticalMode ? 80f : 320f;
            cameraDistance = Mathf.Clamp(cameraDistance - Input.mouseScrollDelta.y * Mathf.Max(1.5f, cameraDistance * .08f), minimumDistance, maximumDistance);
            float horizontal = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1f : 0f) - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f);
            float vertical = (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1f : 0f) - (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1f : 0f);
            Vector3 rawPan = new Vector3(horizontal, 0f, vertical);
            bool mouseOrbit = (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) && Input.GetMouseButton(2);
            if (Input.GetMouseButton(2) && !mouseOrbit) rawPan += new Vector3(-Input.GetAxis("Mouse X") * 3f, 0f, -Input.GetAxis("Mouse Y") * 3f);
            // Pan is relative to the current yaw so WASD/drag still feels like
            // "up/down/left/right on screen" once the camera has been rotated,
            // rather than always moving along absolute world north/south.
            Vector3 pan = Quaternion.Euler(0f, cameraYaw, 0f) * rawPan;
            cameraFocus += pan * (cameraDistance * .55f * Time.unscaledDeltaTime);
            // Q/E orbit yaw; Page Up/Down tilt pitch. Deliberately not RMB-drag
            // (already claimed by orders) or MMB-drag (already pan) — free of
            // every existing binding, in both modes.
            float yawInput = (Input.GetKey(KeyCode.E) ? 1f : 0f) - (Input.GetKey(KeyCode.Q) ? 1f : 0f);
            float pitchInput = (Input.GetKey(KeyCode.PageUp) ? 1f : 0f) - (Input.GetKey(KeyCode.PageDown) ? 1f : 0f);
            const float RotationDegPerSecond = 90f;
            cameraYaw += yawInput * RotationDegPerSecond * Time.unscaledDeltaTime;
            cameraPitch = Mathf.Clamp(cameraPitch + pitchInput * RotationDegPerSecond * Time.unscaledDeltaTime, MinCameraPitch, MaxCameraPitch);
            if (mouseOrbit)
            {
                cameraYaw += Input.GetAxis("Mouse X") * 5f;
                cameraPitch = Mathf.Clamp(cameraPitch - Input.GetAxis("Mouse Y") * 5f, MinCameraPitch, MaxCameraPitch);
            }
            if (Input.GetKeyDown(settings.RemapResetCameraKey))
            {
                cameraFocus = tacticalMode ? Vector3.zero : HexToWorld(new HexCoord(Width / 2, Height / 2));
                cameraDistance = tacticalMode ? 33f : 190f;
                cameraYaw = 0f;
                cameraPitch = tacticalMode ? TacticalDefaultPitch : OperationalDefaultPitch;
            }
            ApplyCamera();
        }

        // Six degrees of freedom over the focus point: pan (X/Z), zoom
        // (distance), and free yaw/pitch orbit — no roll, so hexes, labels,
        // and counters never tip sideways. Both maps share this exact
        // formula; only the default pitch and pan/zoom clamps differ.
        private Vector3 CameraDirectionFromYawPitch()
        {
            float yawRad = cameraYaw * Mathf.Deg2Rad;
            float pitchRad = cameraPitch * Mathf.Deg2Rad;
            float cosPitch = Mathf.Cos(pitchRad);
            return new Vector3(Mathf.Sin(yawRad) * cosPitch, Mathf.Sin(pitchRad), -Mathf.Cos(yawRad) * cosPitch);
        }

        private void ApplyCamera()
        {
            if (mapCamera == null) return;
            Vector3 lookTarget = cameraFocus;
            lookTarget.y = Mathf.Max(lookTarget.y, CameraSurfaceHeightAt(lookTarget));
            Vector3 cameraPosition = lookTarget + CameraDirectionFromYawPitch() * cameraDistance;
            float minimumCameraHeight = CameraSurfaceHeightAt(cameraPosition) +
                                        (tacticalMode ? TacticalCameraGroundClearance : OperationalCameraGroundClearance);
            cameraPosition.y = Mathf.Max(cameraPosition.y, minimumCameraHeight);
            if (tacticalMode)
            {
                mapCamera.transform.position = cameraPosition;
                mapCamera.transform.LookAt(lookTarget, Vector3.up);
                if (tacticalUnit != null) tacticalUnit.transform.localScale = Vector3.one * Mathf.Clamp(cameraDistance / 30f, .82f, 1.55f);
                return;
            }
            mapCamera.transform.position = cameraPosition;
            mapCamera.transform.LookAt(lookTarget, Vector3.up);
            screenPickCacheValid = false;
            float counterScale = Mathf.Clamp(cameraDistance / 52f, 1f, 3.2f);
            foreach (UnitCounterView friendly in operationalFriendlyViews.Values)
                if (friendly != null) friendly.transform.localScale = Vector3.one * counterScale;
            foreach (ContactMarkerView contact in operationalEnemyViews.Values)
                if (contact != null) contact.SetDisplayScale(counterScale);
            UpdateGeographicLabels();
        }

        // Returns the rendered surface beneath an arbitrary X/Z position.
        // The inverse offset-coordinate estimate is refined over its 3x3
        // neighborhood, avoiding a per-frame scan of all 6,656 operational
        // cells while remaining stable at hex boundaries. Outside the board,
        // the recessed command-table top becomes the safety floor.
        private float CameraSurfaceHeightAt(Vector3 worldPosition)
        {
            IReadOnlyDictionary<HexCoord, HexCellView> source = tacticalMode ? localCells : cells;
            int centerQ = tacticalMode ? TacticalBattlefieldExtractor.DefaultWidth / 2 : 0;
            int centerR = tacticalMode ? TacticalBattlefieldExtractor.DefaultHeight / 2 : 0;
            int estimatedQ = Mathf.RoundToInt(worldPosition.x / (HexRadius * 1.5f)) + centerQ;
            int estimatedR = Mathf.RoundToInt(worldPosition.z / (HexRadius * Mathf.Sqrt(3f)) + centerR -
                                              (((estimatedQ & 1) - (centerQ & 1)) * .5f));
            HexCellView nearest = null;
            float nearestDistance = float.MaxValue;
            for (int qOffset = -1; qOffset <= 1; qOffset++)
            for (int rOffset = -1; rOffset <= 1; rOffset++)
            {
                var coord = new HexCoord(estimatedQ + qOffset, estimatedR + rOffset);
                if (!source.TryGetValue(coord, out HexCellView candidate)) continue;
                float deltaX = candidate.transform.position.x - worldPosition.x;
                float deltaZ = candidate.transform.position.z - worldPosition.z;
                float distance = deltaX * deltaX + deltaZ * deltaZ;
                if (distance >= nearestDistance) continue;
                nearest = candidate;
                nearestDistance = distance;
            }
            if (nearest != null && nearestDistance <= HexRadius * HexRadius * 1.35f)
                return nearest.transform.position.y + CellSurfaceOffset;
            return tacticalMode ? -.075f : -.06f;
        }

        private void UpdateGeographicLabels()
        {
            float constantScreenScale = Mathf.Clamp(cameraDistance / 190f, .08f, 1.18f);
            float overviewAlpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(58f, 105f, cameraDistance));
            foreach (GeographicLabel label in geographicLabels)
            {
                label.Transform.localScale = Vector3.one * constantScreenScale;
                Color color = label.BaseColor;
                if (label.OverviewOnly) color.a *= overviewAlpha;
                label.Text.color = color;
                label.Text.gameObject.SetActive(color.a > .015f);
            }
        }

        private void OnGUI()
        {
            EnsureStyles();
            float scale = GetUiScale();
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float uiWidth = Screen.width / scale;
            float uiHeight = Screen.height / scale;

            if (tacticalMode)
            {
                DrawTacticalInterface(uiWidth, uiHeight);
                DrawTransitionOverlay(uiWidth, uiHeight);
                DrawResultScreen(uiWidth, uiHeight);
                DrawCampaignBriefing(uiWidth, uiHeight);
                DrawSupportCardModal(uiWidth, uiHeight);
                DrawSettingsButtonAndPanel(uiWidth, uiHeight);
                DrawSupportButtonAndPanel(uiWidth, uiHeight);
                DrawEventLogButtonAndPanel(uiWidth, uiHeight);
                GUI.matrix = Matrix4x4.identity;
                return;
            }

            GUI.Box(new Rect(20f, 18f, 370f, 224f), GUIContent.none);
            GUI.Label(new Rect(38f, 30f, 235f, 30f), "ALWAYS FAITHFUL", titleStyle);
            GUI.Label(new Rect(273f, 34f, 98f, 22f), $"TURN {turnState.TurnNumber}  •  USMC", badgeStyle);
            GUI.Label(new Rect(38f, 61f, 320f, 20f),
                $"{operationalScenario.Title}  •  TURN {operationalScenario.TurnNumber}/{operationalScenario.TurnLimit}", badgeStyle);
            GUI.Label(new Rect(38f, 91f, 220f, 25f), activeOperationalBattalion.ShortName + " • INFANTRY BATTALION", unitNameStyle);
            stateStyle.normal.textColor = unitState.Readiness == UnitReadiness.Moving
                ? new Color(.34f, .96f, .82f)
                : unitState.Readiness == UnitReadiness.Spent
                    ? new Color(.48f, .52f, .48f)
                    : new Color(.96f, .73f, .20f);
            GUI.Label(new Rect(275f, 93f, 94f, 21f), unitState.Readiness.ToString().ToUpperInvariant(), stateStyle);

            HexCellView occupied = cells[unitState.Position];
            GUI.Label(new Rect(38f, 120f, 325f, 22f), $"{unitState.Position}  •  {occupied.Terrain}  •  {occupied.ElevationMetres:0} m  •  STR {activeOperationalBattalion.Strength}%", bodyStyle);
            GUI.Label(new Rect(38f, 145f, 74f, 22f), "ACTION", badgeStyle);
            DrawActionPointPips(new Rect(105f, 145f, 168f, 20f), unitState);

            string orderPrompt = counterMenuOpen
                ? "Choose a unit order."
                : movePlanning
                    ? hoveredCell != null && reachable.TryGetValue(hoveredCell.Coord, out int moveCost) && !hoveredCell.Coord.Equals(unitState.Position)
                        ? $"LMB confirm {hoveredCell.Coord}  •  Cost {moveCost} AP"
                        : "Hover a highlighted destination."
                    : operationalReconPlanning
                        ? $"Select recon hex within {OperationalScenarioRules.ReconMaximumRangeHexes}."
                    : unitState.CanMove
                        ? "RMB counter for orders."
                        : "Unit spent. End turn to restore AP.";
            GUI.Label(new Rect(38f, 177f, 214f, 32f), orderPrompt, bodyStyle);
            endTurnRect = new Rect(266f, 174f, 104f, 34f);
            GUI.enabled = !unitMoving && operationalScenario.Outcome == OperationalScenarioOutcome.InProgress;
            if (GUI.Button(endTurnRect, "END TURN", buttonStyle)) EndTurn();
            GUI.enabled = true;

            bool showResetCampaign = battalionStatus != null && battalionStatus.EngagementsCompleted > 0;
            if (hasSavedBattle)
            {
                Rect continueSavedBattleRect = new Rect(38f, 213f, showResetCampaign ? 200f : 332f, 26f);
                GUI.enabled = !unitMoving;
                if (GUI.Button(continueSavedBattleRect, "CONTINUE SAVED BATTLE", buttonStyle)) TryContinueSavedBattle();
                GUI.enabled = true;
            }
            if (showResetCampaign)
            {
                Rect resetCampaignRect = hasSavedBattle ? new Rect(246f, 213f, 124f, 26f) : new Rect(38f, 213f, 332f, 26f);
                GUI.enabled = !unitMoving;
                if (GUI.Button(resetCampaignRect, "RESET CAMPAIGN", buttonStyle)) ResetBattalionStatus();
                GUI.enabled = true;
            }

            operationalOrderOfBattleRect = new Rect(uiWidth - 158f, 18f, 136f, 32f);
            if (GUI.Button(operationalOrderOfBattleRect, "ORDER OF BATTLE", buttonStyle))
                operationalOrderOfBattleOpen = !operationalOrderOfBattleOpen;

            HexCellView inspected = selectedCell != null ? selectedCell : hoveredCell;
            enterTacticalRect = Rect.zero;
            if (inspected != null && !movePlanning)
            {
                OperationalContactState inspectedContact = OperationalContactAt(inspected.Coord);
                float inspectionHeight = inspected.IsLand && selectedCell == inspected ? 132f : 84f;
                GUI.Box(new Rect(20f, 250f, 370f, inspectionHeight), GUIContent.none);
                string contactLine = inspectedContact != null
                    ? $"\nINTEL • {(inspectedContact.IsStale ? "STALE " : string.Empty)}{inspectedContact.State.ToString().ToUpperInvariant()} CONTACT"
                    : string.Empty;
                GUI.Label(new Rect(38f, 259f, 330f, 68f), CellInspectionText(inspected, selectedCell != null ? "SELECTED" : "MAP INSPECT") + contactLine, bodyStyle);
                if (inspected.IsLand && selectedCell == inspected)
                {
                    OperationalBattalionState friendlyAtHex = FriendlyBattalionAt(inspected.Coord);
                    OperationalBattalionState resolvingBattalion = inspectedContact != null
                        ? FriendlyBattalionAbleToResolve(inspected.Coord)
                        : friendlyAtHex;
                    if (resolvingBattalion != null)
                    {
                        enterTacticalRect = new Rect(198f, 340f, 172f, 30f);
                        string tacticalLabel = inspectedContact != null ? "RESOLVE CONTACT" : "OPEN LOCAL MAP";
                        if (GUI.Button(enterTacticalRect, tacticalLabel, buttonStyle))
                            OpenOperationalTacticalMap(inspected, resolvingBattalion, inspectedContact != null);
                    }
                    else if (inspectedContact != null)
                    {
                        GUI.Label(new Rect(198f, 340f, 172f, 30f), "MANEUVER ADJACENT TO ENGAGE", badgeStyle);
                    }
                }
            }

            GUI.Box(new Rect(uiWidth - 310f, uiHeight - 100f, 288f, 78f), GUIContent.none);
            GUI.Label(new Rect(uiWidth - 294f, uiHeight - 87f, 272f, 62f), "RMB Unit Orders  •  LMB Confirm\nMMB/WASD Pan  •  Alt+MMB Orbit  •  Wheel Zoom\nQ/E Rotate  •  Page Up/Down Tilt", bodyStyle);

            if (counterMenuOpen)
            {
                GUI.Box(counterMenuRect, GUIContent.none);
                GUI.Label(new Rect(counterMenuRect.x + 12f, counterMenuRect.y + 7f, 154f, 22f), activeOperationalBattalion.ShortName + " • INF BN", badgeStyle);
                GUI.enabled = unitState.CanMove;
                string moveLabel = unitState.CanMove ? $"MOVE  •  {unitState.RemainingActionPoints} AP" : "MOVE  •  SPENT";
                if (GUI.Button(new Rect(counterMenuRect.x + 10f, counterMenuRect.y + 34f, 158f, 28f), moveLabel, buttonStyle))
                    BeginMovePlanning();
                GUI.enabled = unitState.CanMove && unitState.RemainingActionPoints >= OperationalScenarioRules.ReconActionPointCost;
                if (GUI.Button(new Rect(counterMenuRect.x + 10f, counterMenuRect.y + 66f, 158f, 28f),
                        $"RECON  •  {OperationalScenarioRules.ReconActionPointCost} AP", buttonStyle))
                    BeginOperationalReconPlanning();
                GUI.enabled = true;
            }
            DrawTransitionOverlay(uiWidth, uiHeight);
            DrawCampaignBriefing(uiWidth, uiHeight);
            DrawSupportCardModal(uiWidth, uiHeight);
            DrawSettingsButtonAndPanel(uiWidth, uiHeight);
            DrawSupportButtonAndPanel(uiWidth, uiHeight);
            DrawOperationalOrderOfBattle(uiWidth, uiHeight);
            DrawOperationalBriefing(uiWidth, uiHeight);
            DrawOperationalOutcome(uiWidth, uiHeight);
            GUI.matrix = Matrix4x4.identity;
        }

        private static readonly UiScaleTier[] UiScaleTierOptions =
            { UiScaleTier.Auto, UiScaleTier.Small, UiScaleTier.Normal, UiScaleTier.Large, UiScaleTier.ExtraLarge };
        private static readonly string[] UiScaleTierLabels = { "AUTO", "SMALL", "NORMAL", "LARGE", "X-LARGE" };
        private static readonly GraphicsPresetTier[] GraphicsPresetOptions =
            { GraphicsPresetTier.Low, GraphicsPresetTier.Medium, GraphicsPresetTier.High };
        private static readonly string[] GraphicsPresetLabels = { "LOW", "MEDIUM", "HIGH" };
        private static readonly AnimationSpeedTier[] AnimationSpeedOptions =
            { AnimationSpeedTier.Normal, AnimationSpeedTier.Fast, AnimationSpeedTier.Skip };
        private static readonly string[] AnimationSpeedLabels = { "NORMAL", "FAST", "SKIP" };
        private static readonly CounterSkinTier[] CounterSkinOptions =
            { CounterSkinTier.Illustrated, CounterSkinTier.Symbol, CounterSkinTier.Miniature };
        private static readonly string[] CounterSkinLabels = { "ILLUSTRATED", "NATO SYMBOL", "MINIATURE" };

        private OperationalContactState OperationalContactAt(HexCoord hex)
        {
            foreach (OperationalContactState contact in operationalContacts.Values)
                if (contact.State != TacticalVisibilityState.Hidden && contact.LastKnownPosition.Equals(hex)) return contact;
            return null;
        }

        private OperationalBattalionState FriendlyBattalionAt(HexCoord hex)
            => operationalFriendlyBattalions.Find(battalion => battalion.Position.Equals(hex));

        private OperationalBattalionState FriendlyBattalionAbleToResolve(HexCoord hex)
        {
            OperationalBattalionState best = null;
            int bestDistance = int.MaxValue;
            foreach (OperationalBattalionState battalion in operationalFriendlyBattalions)
            {
                int distance = HexCoord.Distance(battalion.Position, hex);
                if (distance >= bestDistance) continue;
                best = battalion;
                bestDistance = distance;
            }
            return bestDistance <= 1 ? best : null;
        }

        private void OpenOperationalTacticalMap(HexCellView inspected, OperationalBattalionState parentBattalion, bool hasKnownEnemyContact)
        {
            if (parentBattalion != null) SelectOperationalBattalion(operationalFriendlyViews[parentBattalion.Id]);
            RequestTacticalMap(inspected, hasKnownEnemyContact);
        }

        private void DrawOperationalBriefing(float uiWidth, float uiHeight)
        {
            if (!operationalBriefingActive || tacticalMode) return;
            Color previous = GUI.color;
            GUI.color = new Color(.015f, .025f, .035f, .92f);
            GUI.DrawTexture(new Rect(0f, 0f, uiWidth, uiHeight), Texture2D.whiteTexture);
            GUI.color = previous;

            float width = Mathf.Min(820f, uiWidth - 60f);
            float height = Mathf.Min(620f, uiHeight - 50f);
            operationalBriefingRect = new Rect((uiWidth - width) / 2f, (uiHeight - height) / 2f, width, height);
            GUI.Box(operationalBriefingRect, GUIContent.none);
            resultHeadlineStyle.normal.textColor = new Color(.96f, .73f, .20f);
            GUI.Label(new Rect(operationalBriefingRect.x, operationalBriefingRect.y + 16f, width, 42f),
                operationalScenario.Title, resultHeadlineStyle);
            GUI.Label(new Rect(operationalBriefingRect.x + 28f, operationalBriefingRect.y + 62f, width - 56f, 22f),
                operationalScenario.DateTimeGroup + "  •  4TH MARINE REGIMENT TASK FORCE", badgeStyle);

            if (!operationalBriefingIntelPage)
            {
                float y = operationalBriefingRect.y + 98f;
                DrawBriefingSection("SITUATION", operationalScenario.Situation, y, width); y += 104f;
                DrawBriefingSection("MISSION", operationalScenario.Mission, y, width); y += 104f;
                DrawBriefingSection("EXECUTION", operationalScenario.Execution, y, width); y += 112f;
                GUI.Label(new Rect(operationalBriefingRect.x + 28f, y, width - 56f, 70f),
                    $"COMMANDER'S INTENT\nFind the enemy before committing to close action. Hold {operationalScenario.PrimaryObjective}; preserve both battalions as a coherent force.", bodyStyle);
            }
            else
            {
                float y = operationalBriefingRect.y + 100f;
                GUI.Label(new Rect(operationalBriefingRect.x + 28f, y, width - 56f, 68f), "INTELLIGENCE ESTIMATE\n" + operationalScenario.IntelligenceEstimate, bodyStyle);
                y += 84f;
                GUI.Label(new Rect(operationalBriefingRect.x + 28f, y, width / 2f - 40f, 24f), "FRIENDLY ORDER OF BATTLE", badgeStyle);
                GUI.Label(new Rect(operationalBriefingRect.x + width / 2f, y, width / 2f - 28f, 24f), "ASSESSED ENEMY ORDER OF BATTLE", badgeStyle);
                y += 28f;
                for (int index = 0; index < operationalFriendlyBattalions.Count; index++)
                {
                    OperationalBattalionState battalion = operationalFriendlyBattalions[index];
                    GUI.Label(new Rect(operationalBriefingRect.x + 28f, y + index * 92f, width / 2f - 44f, 86f),
                        BattalionOrderOfBattleText(battalion, true), bodyStyle);
                }
                for (int index = 0; index < operationalEnemyBattalions.Count; index++)
                {
                    OperationalBattalionState battalion = operationalEnemyBattalions[index];
                    OperationalContactState contact = operationalContacts[battalion.Id];
                    string location = contact.State == TacticalVisibilityState.Hidden ? "LOCATION UNKNOWN" : contact.LastKnownPosition.ToString();
                    GUI.Label(new Rect(operationalBriefingRect.x + width / 2f, y + index * 76f, width / 2f - 28f, 70f),
                        $"{battalion.DisplayName}\n{location}  •  STRENGTH UNCONFIRMED", bodyStyle);
                }
            }

            Rect pageButton = new Rect(operationalBriefingRect.x + 28f, operationalBriefingRect.y + height - 52f, 220f, 34f);
            if (GUI.Button(pageButton, operationalBriefingIntelPage ? "BACK TO OPERATIONS ORDER" : "INTELLIGENCE / OOB", buttonStyle))
                operationalBriefingIntelPage = !operationalBriefingIntelPage;
            Rect acceptButton = new Rect(operationalBriefingRect.x + width - 248f, operationalBriefingRect.y + height - 52f, 220f, 34f);
            if (GUI.Button(acceptButton, "ACCEPT ORDERS", buttonStyle)) operationalBriefingActive = false;
        }

        private void DrawBriefingSection(string heading, string text, float y, float width)
        {
            GUI.Label(new Rect(operationalBriefingRect.x + 28f, y, width - 56f, 20f), heading, badgeStyle);
            GUI.Label(new Rect(operationalBriefingRect.x + 28f, y + 22f, width - 56f, 76f), text, bodyStyle);
        }

        private string BattalionOrderOfBattleText(OperationalBattalionState battalion, bool includeLocation)
        {
            string subordinates = string.Join(" • ", battalion.SubordinateUnits);
            return $"{battalion.DisplayName}\n{(includeLocation ? battalion.Position + "  •  " : string.Empty)}Strength {battalion.Strength}%\n{subordinates}";
        }

        private void DrawOperationalOrderOfBattle(float uiWidth, float uiHeight)
        {
            if (!operationalOrderOfBattleOpen || tacticalMode) return;
            float width = 520f;
            float height = Mathf.Min(610f, uiHeight - 70f);
            Rect panel = new Rect(uiWidth - width - 22f, 60f, width, height);
            GUI.Box(panel, GUIContent.none);
            GUI.Label(new Rect(panel.x + 20f, panel.y + 14f, 360f, 30f), "ORDER OF BATTLE", titleStyle);
            if (GUI.Button(new Rect(panel.x + width - 46f, panel.y + 12f, 30f, 28f), "X", buttonStyle))
                operationalOrderOfBattleOpen = false;
            float y = panel.y + 58f;
            GUI.Label(new Rect(panel.x + 20f, y, width - 40f, 20f), "4TH MARINE REGIMENT TASK FORCE", badgeStyle);
            y += 24f;
            foreach (OperationalBattalionState battalion in operationalFriendlyBattalions)
            {
                GUI.Label(new Rect(panel.x + 20f, y, width - 40f, 70f), BattalionOrderOfBattleText(battalion, true), bodyStyle);
                y += 76f;
            }
            GUI.Label(new Rect(panel.x + 20f, y, width - 40f, 20f), "PLA FORCES • CURRENT INTELLIGENCE", badgeStyle);
            y += 24f;
            foreach (OperationalBattalionState battalion in operationalEnemyBattalions)
            {
                OperationalContactState contact = operationalContacts[battalion.Id];
                string status = contact.State == TacticalVisibilityState.Hidden
                    ? "UNLOCATED • STRENGTH UNKNOWN"
                    : $"{contact.State.ToString().ToUpperInvariant()}{(contact.IsStale ? " • STALE" : string.Empty)} • LAST KNOWN {contact.LastKnownPosition}";
                GUI.Label(new Rect(panel.x + 20f, y, width - 40f, 52f), $"{battalion.DisplayName}\n{status}", bodyStyle);
                y += 58f;
            }
        }

        private void DrawOperationalOutcome(float uiWidth, float uiHeight)
        {
            if (tacticalMode || operationalScenario.Outcome == OperationalScenarioOutcome.InProgress || operationalBriefingActive) return;
            Color previous = GUI.color;
            GUI.color = new Color(.015f, .025f, .035f, .86f);
            GUI.DrawTexture(new Rect(0f, 0f, uiWidth, uiHeight), Texture2D.whiteTexture);
            GUI.color = previous;
            Rect box = new Rect(uiWidth / 2f - 300f, uiHeight / 2f - 140f, 600f, 280f);
            GUI.Box(box, GUIContent.none);
            resultHeadlineStyle.normal.textColor = operationalScenario.Outcome == OperationalScenarioOutcome.UsmcVictory
                ? new Color(.38f, .88f, .68f)
                : new Color(1f, .34f, .28f);
            GUI.Label(new Rect(box.x, box.y + 24f, box.width, 42f),
                operationalScenario.Outcome == OperationalScenarioOutcome.UsmcVictory ? "OPERATIONAL VICTORY" : "OPERATIONAL DEFEAT",
                resultHeadlineStyle);
            GUI.Label(new Rect(box.x + 34f, box.y + 88f, box.width - 68f, 80f), operationalScenario.OutcomeSummary, bodyStyle);
            if (GUI.Button(new Rect(box.x + box.width / 2f - 115f, box.y + box.height - 58f, 230f, 36f), "RESET SCENARIO", buttonStyle))
                ResetBattalionStatus();
        }

        private void DrawSettingsButtonAndPanel(float uiWidth, float uiHeight)
        {
            Rect settingsButtonRect = new Rect(20f, uiHeight - 44f, 96f, 34f);
            if (GUI.Button(settingsButtonRect, "SETTINGS", buttonStyle)) settingsPanelOpen = !settingsPanelOpen;
            if (!settingsPanelOpen) return;

            // Consume a keystroke for rebinding before anything else this frame
            // reads it — Event.current is only valid inside OnGUI, which is why
            // this capture lives here rather than in Update().
            if (awaitingRemapFor != null && Event.current.type == EventType.KeyDown && Event.current.keyCode != KeyCode.None)
            {
                KeyCode pressed = Event.current.keyCode;
                Event.current.Use();
                bool collision = (awaitingRemapFor == RemapTarget.Cancel && pressed == settings.RemapResetCameraKey) ||
                                  (awaitingRemapFor == RemapTarget.ResetCamera && pressed == settings.RemapCancelKey);
                if (collision)
                {
                    remapRejectionText = $"{pressed} is already bound to the other action";
                }
                else
                {
                    if (awaitingRemapFor == RemapTarget.Cancel) settings.RemapCancelKey = pressed;
                    else settings.RemapResetCameraKey = pressed;
                    remapRejectionText = null;
                }
                awaitingRemapFor = null;
            }

            Rect panel = new Rect(uiWidth / 2f - 260f, uiHeight / 2f - 300f, 520f, 600f);
            GUI.Box(panel, GUIContent.none);
            GUI.Label(new Rect(panel.x + 20f, panel.y + 14f, 300f, 30f), "SETTINGS", titleStyle);
            if (GUI.Button(new Rect(panel.x + panel.width - 44f, panel.y + 14f, 28f, 28f), "X", buttonStyle))
            {
                settingsPanelOpen = false;
                awaitingRemapFor = null;
                SaveSettings();
            }

            float rowY = panel.y + 64f;
            GUI.Label(new Rect(panel.x + 20f, rowY, 200f, 22f), "UI SCALE", badgeStyle);
            rowY += 26f;
            const float tierButtonWidth = 92f;
            for (int index = 0; index < UiScaleTierOptions.Length; index++)
            {
                Rect tierRect = new Rect(panel.x + 20f + index * (tierButtonWidth + 4f), rowY, tierButtonWidth, 30f);
                bool active = settings.UiScale == UiScaleTierOptions[index];
                string label = active ? $"[{UiScaleTierLabels[index]}]" : UiScaleTierLabels[index];
                if (GUI.Button(tierRect, label, buttonStyle)) settings.UiScale = UiScaleTierOptions[index];
            }

            rowY += 66f;
            GUI.Label(new Rect(panel.x + 20f, rowY, 150f, 22f), "CANCEL KEY", badgeStyle);
            string cancelLabel = awaitingRemapFor == RemapTarget.Cancel ? "PRESS A KEY..." : settings.RemapCancelKey.ToString().ToUpperInvariant();
            GUI.Label(new Rect(panel.x + 180f, rowY + 2f, 150f, 22f), cancelLabel, bodyStyle);
            if (GUI.Button(new Rect(panel.x + 340f, rowY - 4f, 140f, 30f), "REBIND", buttonStyle))
            {
                awaitingRemapFor = RemapTarget.Cancel;
                remapRejectionText = null;
            }

            rowY += 40f;
            GUI.Label(new Rect(panel.x + 20f, rowY, 150f, 22f), "RESET CAMERA KEY", badgeStyle);
            string resetLabel = awaitingRemapFor == RemapTarget.ResetCamera ? "PRESS A KEY..." : settings.RemapResetCameraKey.ToString().ToUpperInvariant();
            GUI.Label(new Rect(panel.x + 180f, rowY + 2f, 150f, 22f), resetLabel, bodyStyle);
            if (GUI.Button(new Rect(panel.x + 340f, rowY - 4f, 140f, 30f), "REBIND", buttonStyle))
            {
                awaitingRemapFor = RemapTarget.ResetCamera;
                remapRejectionText = null;
            }

            rowY += 38f;
            if (!string.IsNullOrEmpty(remapRejectionText))
            {
                GUIStyle rejectionStyle = new GUIStyle(bodyStyle) { normal = { textColor = new Color(.96f, .32f, .28f) } };
                GUI.Label(new Rect(panel.x + 20f, rowY, panel.width - 40f, 22f), remapRejectionText, rejectionStyle);
            }

            rowY += 40f;
            GUI.Label(new Rect(panel.x + 20f, rowY, 150f, 22f), "GRAPHICS PRESET", badgeStyle);
            for (int index = 0; index < GraphicsPresetOptions.Length; index++)
            {
                Rect optionRect = new Rect(panel.x + 180f + index * 88f, rowY - 4f, 84f, 30f);
                bool active = settings.GraphicsPreset == GraphicsPresetOptions[index];
                string label = active ? $"[{GraphicsPresetLabels[index]}]" : GraphicsPresetLabels[index];
                if (GUI.Button(optionRect, label, buttonStyle)) settings.GraphicsPreset = GraphicsPresetOptions[index];
            }

            rowY += 40f;
            GUI.Label(new Rect(panel.x + 20f, rowY, 150f, 22f), "ANIMATION SPEED", badgeStyle);
            for (int index = 0; index < AnimationSpeedOptions.Length; index++)
            {
                Rect optionRect = new Rect(panel.x + 180f + index * 88f, rowY - 4f, 84f, 30f);
                bool active = settings.AnimationSpeed == AnimationSpeedOptions[index];
                string label = active ? $"[{AnimationSpeedLabels[index]}]" : AnimationSpeedLabels[index];
                if (GUI.Button(optionRect, label, buttonStyle)) settings.AnimationSpeed = AnimationSpeedOptions[index];
            }

            rowY += 40f;
            GUI.Label(new Rect(panel.x + 20f, rowY, 220f, 22f), "COLOR-SAFE PALETTE", badgeStyle);
            if (GUI.Button(new Rect(panel.x + 340f, rowY - 4f, 140f, 30f), settings.ColorSafePalette ? "ON" : "OFF", buttonStyle))
                settings.ColorSafePalette = !settings.ColorSafePalette;

            rowY += 40f;
            GUI.Label(new Rect(panel.x + 20f, rowY, 220f, 22f), "REDUCED MOTION", badgeStyle);
            if (GUI.Button(new Rect(panel.x + 340f, rowY - 4f, 140f, 30f), settings.ReducedMotion ? "ON" : "OFF", buttonStyle))
                settings.ReducedMotion = !settings.ReducedMotion;

            rowY += 40f;
            GUI.Label(new Rect(panel.x + 20f, rowY, 150f, 22f), "COUNTER SKIN", badgeStyle);
            for (int index = 0; index < CounterSkinOptions.Length; index++)
            {
                Rect optionRect = new Rect(panel.x + 180f + index * 104f, rowY - 4f, 100f, 30f);
                bool active = settings.CounterSkin == CounterSkinOptions[index];
                string label = active ? $"[{CounterSkinLabels[index]}]" : CounterSkinLabels[index];
                if (GUI.Button(optionRect, label, buttonStyle))
                {
                    settings.CounterSkin = CounterSkinOptions[index];
                    ApplyCounterSkin();
                }
            }

            rowY += 40f;
            if (GUI.Button(new Rect(panel.x + 20f, rowY, panel.width - 40f, 34f), "RESET TO DEFAULTS", buttonStyle))
            {
                settings = AlwaysFaithfulSettingsRules.CreateDefault();
                awaitingRemapFor = null;
                remapRejectionText = null;
                ApplyCounterSkin();
            }

            rowY += 44f;
            if (GUI.Button(new Rect(panel.x + 20f, rowY, panel.width - 40f, 34f), "CLOSE", buttonStyle))
            {
                settingsPanelOpen = false;
                awaitingRemapFor = null;
                SaveSettings();
            }
        }

        // Read-only order-of-battle view of the Battalion's support-card
        // hand. Cards are only ever played through the pre-battle modal
        // (DrawSupportCardModal) — this panel exists so the player can check
        // what they're holding at any time, on either map.
        private void DrawSupportButtonAndPanel(float uiWidth, float uiHeight)
        {
            int handCount = battalionStatus?.Hand.Count ?? 0;
            Rect supportButtonRect = new Rect(124f, uiHeight - 44f, 96f, 34f);
            if (GUI.Button(supportButtonRect, $"SUPPORT • {handCount}", buttonStyle)) supportPanelOpen = !supportPanelOpen;
            if (!supportPanelOpen) return;

            Rect panel = new Rect(uiWidth / 2f - 260f, uiHeight / 2f - 200f, 520f, 400f);
            GUI.Box(panel, GUIContent.none);
            GUI.Label(new Rect(panel.x + 20f, panel.y + 14f, 300f, 30f), "ORDER OF BATTLE • SUPPORT", titleStyle);
            if (GUI.Button(new Rect(panel.x + panel.width - 44f, panel.y + 14f, 28f, 28f), "X", buttonStyle)) supportPanelOpen = false;

            float rowY = panel.y + 64f;
            if (handCount == 0)
            {
                GUI.Label(new Rect(panel.x + 20f, rowY, panel.width - 40f, 22f), "No support cards on hand. A new card is drawn after each engagement.", bodyStyle);
            }
            else
            {
                foreach (TacticalSupportCard card in battalionStatus.Hand)
                {
                    GUI.Label(new Rect(panel.x + 20f, rowY, panel.width - 40f, 22f), TacticalSupportCardCatalog.DisplayName(card.AssetType), badgeStyle);
                    rowY += 24f;
                    GUI.Label(new Rect(panel.x + 20f, rowY, panel.width - 40f, 22f), TacticalSupportCardCatalog.Description(card.AssetType), bodyStyle);
                    rowY += 36f;
                }
            }

            rowY = panel.y + panel.height - 54f;
            if (GUI.Button(new Rect(panel.x + 20f, rowY, panel.width - 40f, 34f), "CLOSE", buttonStyle)) supportPanelOpen = false;
        }

        // Live, collapsible view of the same chronological event log the
        // after-action screen builds from tacticalBattlefield's typed event
        // lists (BuildTacticalEventLog) — previously only visible once a
        // battle had already ended.
        private void DrawEventLogButtonAndPanel(float uiWidth, float uiHeight)
        {
            Rect logButtonRect = new Rect(228f, uiHeight - 44f, 96f, 34f);
            if (GUI.Button(logButtonRect, "LOG", buttonStyle)) eventLogPanelOpen = !eventLogPanelOpen;
            if (!eventLogPanelOpen) return;

            Rect panel = new Rect(uiWidth / 2f - 260f, uiHeight / 2f - 220f, 520f, 440f);
            GUI.Box(panel, GUIContent.none);
            GUI.Label(new Rect(panel.x + 20f, panel.y + 14f, 300f, 30f), "EVENT LOG", titleStyle);
            if (GUI.Button(new Rect(panel.x + panel.width - 44f, panel.y + 14f, 28f, 28f), "X", buttonStyle)) eventLogPanelOpen = false;

            List<string> entries = BuildTacticalEventLog(16);
            string logText = entries.Count > 0 ? string.Join("\n", entries) : "No events recorded yet this battle.";
            GUI.Label(new Rect(panel.x + 20f, panel.y + 56f, panel.width - 40f, panel.height - 110f), logText, bodyStyle);

            if (GUI.Button(new Rect(panel.x + 20f, panel.y + panel.height - 44f, panel.width - 40f, 34f), "CLOSE", buttonStyle))
                eventLogPanelOpen = false;
        }

        // Blocking pre-battle modal offered at the start of a fresh standalone
        // scenario whenever the Battalion's hand is non-empty. No fade timer
        // like DrawCampaignBriefing — this one waits on a player choice, not
        // a clock, so it shows/hides instantly.
        private void DrawSupportCardModal(float uiWidth, float uiHeight)
        {
            if (!supportCardModalActive || battalionStatus == null) return;

            GUI.color = new Color(.02f, .03f, .05f, .82f);
            GUI.DrawTexture(new Rect(0f, 0f, uiWidth, uiHeight), Texture2D.whiteTexture);
            GUI.color = Color.white;

            float panelHeight = 120f + battalionStatus.Hand.Count * 60f;
            Rect panel = new Rect(uiWidth / 2f - 260f, uiHeight / 2f - panelHeight / 2f, 520f, panelHeight);
            GUI.Box(panel, GUIContent.none);
            GUI.Label(new Rect(panel.x + 20f, panel.y + 14f, panel.width - 40f, 30f), "COMMIT SUPPORT?", titleStyle);
            GUI.Label(new Rect(panel.x + 20f, panel.y + 46f, panel.width - 40f, 20f), "Play one card for this battle, or skip.", bodyStyle);

            float rowY = panel.y + 78f;
            foreach (TacticalSupportCard card in battalionStatus.Hand)
            {
                GUI.Label(new Rect(panel.x + 20f, rowY + 6f, 300f, 22f), TacticalSupportCardCatalog.DisplayName(card.AssetType), badgeStyle);
                GUI.Label(new Rect(panel.x + 20f, rowY + 26f, 300f, 20f), TacticalSupportCardCatalog.Description(card.AssetType), bodyStyle);
                if (GUI.Button(new Rect(panel.x + panel.width - 140f, rowY + 10f, 120f, 34f), "PLAY", buttonStyle))
                    DismissSupportCardModal(card);
                rowY += 60f;
            }

            if (GUI.Button(new Rect(panel.x + 20f, panel.y + panel.height - 44f, panel.width - 40f, 34f), "SKIP", buttonStyle))
                DismissSupportCardModal(null);
        }

        private void DrawTacticalInterface(float uiWidth, float uiHeight)
        {
            GUI.Box(new Rect(20f, 18f, 405f, 318f), GUIContent.none);
            GUI.Label(new Rect(38f, 30f, 250f, 30f), "ALWAYS FAITHFUL", titleStyle);
            int battleTurn = tacticalObjective != null ? turnState.TurnNumber - tacticalObjective.BattleStartTurn + 1 : turnState.TurnNumber;
            int battleTurnLimit = tacticalObjective?.TurnLimit ?? TacticalVictory.DefaultTurnLimit;
            GUI.Label(new Rect(270f, 34f, 135f, 22f), $"TURN {battleTurn}/{battleTurnLimit} • {turnState.ActiveSide}", badgeStyle);
            GUI.Label(new Rect(38f, 63f, 350f, 24f), ObjectiveStatusText(), badgeStyle);
            GUI.Label(new Rect(38f, 94f, 340f, 25f), tacticalBattlefield.BattlefieldId, unitNameStyle);
            GUI.Label(new Rect(38f, 121f, 350f, 36f),
                $"Parent {tacticalBattlefield.ParentHex}  •  {TacticalWidthKilometres():0.00} × {(tacticalBattlefield.Height * tacticalBattlefield.CellSizeMetres / 1000f):0.00} km  •  Relief {localMinimumLandElevation:0}–{localMaximumLandElevation:0} m\n" +
                $"{tacticalBattlefield.CenterLatitude:0.00000}°N  •  {tacticalBattlefield.CenterLongitude:0.00000}°E", bodyStyle);
            string usmcNameLabel = tacticalUnitState.DisplayName.ToUpperInvariant() + (activeBattleRequest != null ? "  •  IMPORTED" : string.Empty);
            GUI.Label(new Rect(38f, 165f, 280f, 24f), usmcNameLabel, unitNameStyle);
            stateStyle.normal.textColor = tacticalUnitState.Readiness == UnitReadiness.Moving
                ? new Color(.34f, .96f, .82f)
                : tacticalUnitState.Readiness == UnitReadiness.Spent
                    ? new Color(.48f, .52f, .48f)
                    : new Color(.96f, .73f, .20f);
            GUI.Label(new Rect(286f, 166f, 101f, 22f), tacticalUnitState.Readiness.ToString().ToUpperInvariant(), stateStyle);
            if (tacticalUnitState.CombatStatus != TacticalCombatStatus.Ready)
            {
                GUIStyle statusStyle = new GUIStyle(badgeStyle) { alignment = TextAnchor.MiddleLeft };
                statusStyle.normal.textColor = TacticalStatusColor(tacticalUnitState.CombatStatus);
                GUI.Label(new Rect(38f, 183f, 260f, 14f), $"{tacticalUnitState.CombatStatus.ToString().ToUpperInvariant()} • {tacticalUnitState.SuppressionPoints}/{TacticalSuppression.MaximumPoints} PTS", statusStyle);
            }
            GUI.Label(new Rect(38f, 199f, 70f, 22f), "ACTION", badgeStyle);
            DrawActionPointPips(new Rect(105f, 199f, 168f, 20f), tacticalUnitState);
            GUIStyle ammoStyle = new GUIStyle(badgeStyle);
            bool ammoConcern = tacticalWeapon.MaximumAmmunition > 0 &&
                tacticalWeapon.RemainingAmmunition <= Mathf.Max(1, tacticalWeapon.MaximumAmmunition / 4);
            ammoStyle.normal.textColor = tacticalWeapon.RemainingAmmunition <= 0
                ? new Color(1f, .34f, .28f)
                : ammoConcern ? new Color(1f, .68f, .24f) : ammoStyle.normal.textColor;
            GUI.Label(new Rect(324f, 199f, 72f, 22f), $"AMMO {tacticalWeapon.RemainingAmmunition}/{tacticalWeapon.MaximumAmmunition}", ammoStyle);
            GUI.Label(new Rect(38f, 228f, 348f, 26f), tacticalOrderFeedback, bodyStyle);

            if (battalionStatus != null)
            {
                GUIStyle battalionStyle = new GUIStyle(badgeStyle) { alignment = TextAnchor.MiddleLeft };
                battalionStyle.normal.textColor = TacticalBattalion.IsUnderStrength(battalionStatus) ? new Color(.94f, .60f, .30f) : new Color(.60f, .84f, .68f);
                string battalionState = TacticalBattalion.IsUnderStrength(battalionStatus) ? "DEGRADED" : "READY";
                string isrSuffix = tacticalBattlefield.IsrCardActive ? " • ISR ACTIVE" : string.Empty;
                GUI.Label(new Rect(38f, 254f, 348f, 20f), $"{battalionStatus.BattalionName.ToUpperInvariant()} • STRENGTH {battalionStatus.Strength}% • {battalionState}{isrSuffix}", battalionStyle);
            }

            Rect tacticalSaveRect = new Rect(38f, 283f, 93f, 34f);
            Rect tacticalEndTurnRect = new Rect(137f, 283f, 104f, 34f);
            returnToIslandRect = new Rect(244f, 283f, 161f, 34f);
            bool tacticalControlsEnabled = !mapTransitionActive && !tacticalUnitMoving && !tacticalFireResolving && !tacticalEnemyTurnActive && !campaignBriefingActive && !settingsPanelOpen && !supportCardModalActive;
            GUI.enabled = tacticalControlsEnabled && !resultScreenActive && tacticalObjective != null && tacticalObjective.Outcome == TacticalBattleOutcome.InProgress;
            if (GUI.Button(tacticalSaveRect, "SAVE", buttonStyle)) SaveTacticalBattle();
            GUI.enabled = tacticalControlsEnabled && !resultScreenActive;
            if (GUI.Button(tacticalEndTurnRect, "END TURN", buttonStyle)) EndTacticalTurn();
            GUI.enabled = tacticalControlsEnabled;
            if (GUI.Button(returnToIslandRect, "RETURN TO ISLAND", buttonStyle)) ReturnToIsland(true);
            GUI.enabled = true;

            bool showSpottedBadge = tacticalUnitSpottedTier != TacticalVisibilityState.Hidden;
            Rect intelligenceRect = new Rect(uiWidth - 355f, 18f, 335f, 151f + tacticalContacts.Count * 25f + (showSpottedBadge ? 30f : 0f) + 68f);
            GUI.Box(intelligenceRect, GUIContent.none);
            GUI.Label(new Rect(intelligenceRect.x + 16f, intelligenceRect.y + 11f, 290f, 22f), "TACTICAL INTELLIGENCE", badgeStyle);
            GUI.Label(new Rect(intelligenceRect.x + 16f, intelligenceRect.y + 36f, 300f, 38f),
                "OBSERVER • USMC RIFLE PLATOON\nLOS + TERRAIN SENSOR PICTURE", bodyStyle);
            int contactLine = 0;
            foreach (TacticalUnitState enemy in tacticalEnemyStates)
            {
                if (!tacticalContacts.TryGetValue(enemy.Id, out TacticalContactState contact)) continue;
                string stale = contact.IsStale ? " • LAST KNOWN" : string.Empty;
                string range = contact.State == TacticalVisibilityState.Hidden ? "NO TRACK" : $"{contact.RangeHexes * 250} M";
                bool statusKnown = (contact.State == TacticalVisibilityState.Identified || contact.State == TacticalVisibilityState.Observed) &&
                    enemy.CombatStatus != TacticalCombatStatus.Ready;
                string status = statusKnown ? $"  •  {enemy.CombatStatus.ToString().ToUpperInvariant()}" : string.Empty;
                GUI.Label(new Rect(intelligenceRect.x + 16f, intelligenceRect.y + 78f + contactLine * 25f, 300f, 23f),
                    $"{contact.State.ToString().ToUpperInvariant()}{stale}  •  {range}{status}", bodyStyle);
                contactLine++;
            }
            Rect speedRect = new Rect(intelligenceRect.x + 16f, intelligenceRect.y + 82f + tacticalContacts.Count * 25f, 300f, 28f);
            if (GUI.Button(speedRect, fastEnemyAnimation ? "ENEMY SPEED • FAST" : "ENEMY SPEED • CINEMATIC", buttonStyle))
                fastEnemyAnimation = !fastEnemyAnimation;
            float nextRowY = speedRect.y + 34f;
            if (showSpottedBadge)
            {
                GUIStyle spottedStyle = new GUIStyle(badgeStyle) { alignment = TextAnchor.MiddleLeft };
                spottedStyle.normal.textColor = TacticalSpottedColor(tacticalUnitSpottedTier);
                GUI.Label(new Rect(intelligenceRect.x + 16f, nextRowY, 300f, 22f),
                    $"ENEMY TRACKING YOU • {tacticalUnitSpottedTier.ToString().ToUpperInvariant()}", spottedStyle);
                nextRowY += 30f;
            }

            bool reactionReady = tacticalUnitState.CanFire && tacticalWeapon.RemainingAmmunition > 0;
            GUIStyle reactionStyle = new GUIStyle(badgeStyle) { alignment = TextAnchor.MiddleLeft };
            reactionStyle.normal.textColor = reactionReady ? new Color(.48f, .78f, .58f) : new Color(.62f, .62f, .60f);
            GUI.Label(new Rect(intelligenceRect.x + 16f, nextRowY, 300f, 22f),
                reactionReady ? "REACTION FIRE • READY" : "REACTION FIRE • UNAVAILABLE", reactionStyle);
            nextRowY += 30f;

            if (tacticalObjective != null &&
                GUI.Button(new Rect(intelligenceRect.x + 16f, nextRowY, 300f, 28f), "FOCUS OBJECTIVE", buttonStyle))
            {
                cameraFocus = LocalCounterPosition(tacticalObjective.ObjectiveHex);
                ApplyCamera();
            }

            if (tacticalReactionActive && !string.IsNullOrEmpty(tacticalReactionBannerText))
            {
                Rect bannerRect = new Rect(uiWidth / 2f - 210f, 26f, 420f, 40f);
                GUI.Box(bannerRect, GUIContent.none);
                GUI.Label(bannerRect, tacticalReactionBannerText, reactionBannerStyle);
            }
            else if (tacticalEnemyTurnActive && !string.IsNullOrEmpty(tacticalEnemyActivityText))
            {
                Rect bannerRect = new Rect(uiWidth / 2f - 240f, 26f, 480f, 44f);
                GUI.Box(bannerRect, GUIContent.none);
                GUI.Label(bannerRect, tacticalEnemyActivityText, reactionBannerStyle);
            }

            if (hoveredLocalCell != null)
            {
                bool showTargetStatus = tacticalFirePlanning && tacticalFireTarget != null && tacticalFireTarget.CombatStatus != TacticalCombatStatus.Ready;
                // +14 over the pre-recon baseline: room for CellInspectionText's
                // optional "Recon active" line, which can appear in any mode.
                float height = tacticalFirePlanning && tacticalFirePreview != null ? (showTargetStatus ? 222f : 208f) : tacticalLosPlanning && tacticalLosResult != null ? 166f : 118f;
                GUI.Box(new Rect(20f, 321f, 405f, height), GUIContent.none);
                string inspection = CellInspectionText(hoveredLocalCell, tacticalFirePlanning ? "DIRECT FIRE TARGET" : tacticalLosPlanning ? "LOS TARGET" : tacticalReconPlanning ? "RECON TARGET" : "LOCAL INSPECT");
                if (tacticalLosPlanning && tacticalLosResult != null) inspection += "\n" + TacticalLosBreakdown(tacticalLosResult);
                if (tacticalFirePlanning && tacticalFirePreview != null) inspection += "\n" + TacticalFireBreakdown(tacticalFirePreview);
                if (showTargetStatus) inspection += $"\nTARGET {tacticalFireTarget.CombatStatus.ToString().ToUpperInvariant()} • {tacticalFireTarget.SuppressionPoints}/{TacticalSuppression.MaximumPoints} PTS";
                GUI.Label(new Rect(38f, 331f, 360f, height - 16f), inspection, bodyStyle);
            }
            if (tacticalMovePlanning)
            {
                Vector2 pointer = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y) / GetUiScale();
                GUI.Box(new Rect(pointer.x + 16f, pointer.y + 14f, 176f, 28f), tacticalPreviewCost > 0 ? $"ROUTE • {tacticalPreviewCost} AP" : tacticalOrderFeedback);
            }
            if (tacticalMenuOpen)
            {
                GUI.Box(tacticalMenuRect, GUIContent.none);
                GUI.Label(new Rect(tacticalMenuRect.x + 12f, tacticalMenuRect.y + 7f, 154f, 22f), "USMC RIFLE PLATOON", badgeStyle);
                GUI.enabled = tacticalUnitState.CanMove;
                if (GUI.Button(new Rect(tacticalMenuRect.x + 10f, tacticalMenuRect.y + 34f, 158f, 28f),
                        tacticalUnitState.CanMove ? $"MOVE • {tacticalUnitState.RemainingActionPoints} AP" : "MOVE • SPENT", buttonStyle))
                    BeginTacticalMovePlanning();
                GUI.enabled = true;
                if (GUI.Button(new Rect(tacticalMenuRect.x + 10f, tacticalMenuRect.y + 67f, 158f, 28f), "INSPECT LOS", buttonStyle))
                    BeginTacticalLosPlanning();
                GUI.enabled = tacticalUnitState.CanFire && tacticalUnitState.RemainingActionPoints >= TacticalDirectFire.ActionPointCost && tacticalWeapon.RemainingAmmunition > 0;
                if (GUI.Button(new Rect(tacticalMenuRect.x + 10f, tacticalMenuRect.y + 100f, 158f, 28f),
                        tacticalWeapon.RemainingAmmunition > 0 ? $"DIRECT FIRE • {tacticalWeapon.RemainingAmmunition}" : "DIRECT FIRE • EMPTY", buttonStyle))
                    BeginTacticalFirePlanning();
                GUI.enabled = tacticalUnitState.CanRally;
                if (GUI.Button(new Rect(tacticalMenuRect.x + 10f, tacticalMenuRect.y + 133f, 158f, 28f),
                        tacticalUnitState.CombatStatus == TacticalCombatStatus.Ready ? "RALLY • READY" : $"RALLY • {tacticalUnitState.CombatStatus.ToString().ToUpperInvariant()}", buttonStyle))
                    IssueTacticalRally();
                GUI.enabled = tacticalUnitState.RemainingActionPoints >= TacticalRecon.ActionPointCost;
                if (GUI.Button(new Rect(tacticalMenuRect.x + 10f, tacticalMenuRect.y + 166f, 158f, 28f),
                        $"RECON • {TacticalRecon.ActionPointCost} AP", buttonStyle))
                    BeginTacticalReconPlanning();
                GUI.enabled = true;
                if (GUI.Button(new Rect(tacticalMenuRect.x + 10f, tacticalMenuRect.y + 199f, 158f, 28f),
                        tacticalLosOverlayActive ? "LOS OVERLAY • ON" : "LOS OVERLAY • OFF", buttonStyle))
                    ToggleTacticalLosOverlay();
            }
            GUI.Box(new Rect(uiWidth - 310f, uiHeight - 100f, 288f, 78f), GUIContent.none);
            GUI.Label(new Rect(uiWidth - 294f, uiHeight - 87f, 272f, 62f), "RMB Orders  •  LMB Confirm\nMMB Pan  •  Alt+MMB Orbit  •  Wheel Zoom\nQ/E Rotate  •  Page Up/Down Tilt", bodyStyle);
        }

        // Default ramp is a single warm hue (red-orange-yellow), which reads
        // as pure lightness to red-green color-vision deficiency and loses
        // the severity distinction. The color-safe ramp swaps to a
        // blue-orange-magenta spread (Okabe-Ito-inspired) so each tier keeps
        // a distinct hue rather than only a brightness difference.
        private Color TacticalStatusColor(TacticalCombatStatus status)
        {
            if (settings.ColorSafePalette)
            {
                switch (status)
                {
                    case TacticalCombatStatus.Reduced: return new Color(.72f, .18f, .58f);
                    case TacticalCombatStatus.Disrupted: return new Color(.90f, .55f, .12f);
                    case TacticalCombatStatus.Suppressed: return new Color(.30f, .62f, .95f);
                    default: return new Color(.48f, .78f, .58f);
                }
            }
            switch (status)
            {
                case TacticalCombatStatus.Reduced: return new Color(1f, .38f, .34f);
                case TacticalCombatStatus.Disrupted: return new Color(1f, .62f, .24f);
                case TacticalCombatStatus.Suppressed: return new Color(1f, .84f, .30f);
                default: return new Color(.48f, .78f, .58f);
            }
        }

        private Color TacticalSpottedColor(TacticalVisibilityState tier)
        {
            if (settings.ColorSafePalette)
            {
                switch (tier)
                {
                    case TacticalVisibilityState.Observed: return new Color(.72f, .18f, .58f);
                    case TacticalVisibilityState.Identified: return new Color(.90f, .55f, .12f);
                    default: return new Color(.30f, .62f, .95f);
                }
            }
            switch (tier)
            {
                case TacticalVisibilityState.Observed: return new Color(1f, .38f, .34f);
                case TacticalVisibilityState.Identified: return new Color(1f, .62f, .24f);
                default: return new Color(1f, .84f, .30f);
            }
        }

        private static string MissionTypeLabel(TacticalMissionType missionType)
        {
            switch (missionType)
            {
                case TacticalMissionType.Defend: return "DEFEND";
                case TacticalMissionType.Raid: return "RAID";
                case TacticalMissionType.ReconInForce: return "RECON IN FORCE";
                case TacticalMissionType.Withdrawal: return "WITHDRAWAL";
                default: return "ATTACK";
            }
        }

        private string ObjectiveStatusText()
        {
            if (tacticalObjective == null) return "LOCAL BATTLEFIELD  •  250 M HEXES";
            string label = MissionTypeLabel(tacticalObjective.MissionType);
            if (tacticalObjective.MissionType == TacticalMissionType.Withdrawal)
            {
                int turnsRemaining = Mathf.Max(0, tacticalObjective.TurnLimit - (turnState.TurnNumber - tacticalObjective.BattleStartTurn));
                return $"{label}  •  HOLD {turnsRemaining} MORE TURN{(turnsRemaining == 1 ? "" : "S")}";
            }
            if (tacticalObjective.MissionType == TacticalMissionType.ReconInForce)
                return $"{label}  •  {tacticalObjective.ObservedEnemyIds.Count}/{tacticalEnemyStates.Count} PLA ELEMENTS IDENTIFIED";
            bool usmcControls = localMovementBoard.TryGetValue(tacticalObjective.ObjectiveHex, out TacticalMovementCell cell) &&
                cell.OccupantId == tacticalUnitState.Id;
            string verb = tacticalObjective.MissionType == TacticalMissionType.Defend ? "HOLD" : "SEIZE";
            string control = usmcControls ? "USMC" : "CONTESTED";
            return $"{label}  •  {verb} OBJECTIVE {tacticalObjective.ObjectiveHex}  •  {control}";
        }

        private static string TacticalLosBreakdown(TacticalLosResult result)
        {
            if (!result.IsValid) return result.RejectionReason;
            string text = $"{result.State.ToString().ToUpperInvariant()} • {result.RangeHexes * 250} m";
            int count = Mathf.Min(3, result.Modifiers.Count);
            for (int index = 0; index < count; index++) text += "\n" + result.Modifiers[index];
            return text;
        }

        private static string TacticalFireBreakdown(TacticalFirePreview preview)
        {
            if (!preview.IsValid) return "ILLEGAL • " + preview.RejectionReason;
            string text = $"{preview.HitChance}% HIT • {preview.SuppressionChance}% EFFECT • {preview.ExpectedEffect}";
            int count = Mathf.Min(4, preview.Modifiers.Count);
            for (int index = 0; index < count; index++)
            {
                TacticalFireModifier modifier = preview.Modifiers[index];
                text += $"\n{modifier.Label}: {(modifier.Value >= 0 ? "+" : string.Empty)}{modifier.Value}";
            }
            return text;
        }

        private void DrawTransitionOverlay(float uiWidth, float uiHeight)
        {
            if (mapTransitionOpacity <= .001f) return;
            Color previous = GUI.color;
            GUI.color = new Color(.025f, .055f, .052f, mapTransitionOpacity);
            GUI.DrawTexture(new Rect(0f, 0f, uiWidth, uiHeight), Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private void DrawResultScreen(float uiWidth, float uiHeight)
        {
            if (!resultScreenActive || resultScreenOpacity <= .001f || tacticalObjective == null) return;
            Color previousColor = GUI.color;
            GUI.color = new Color(.02f, .035f, .04f, resultScreenOpacity * .86f);
            GUI.DrawTexture(new Rect(0f, 0f, uiWidth, uiHeight), Texture2D.whiteTexture);
            GUI.color = previousColor;
            if (resultScreenOpacity < .98f) return;

            float boxWidth = 620f;
            float boxHeight = 460f;
            Rect box = new Rect(uiWidth / 2f - boxWidth / 2f, uiHeight / 2f - boxHeight / 2f, boxWidth, boxHeight);
            GUI.Box(box, GUIContent.none);

            string headline;
            Color headlineColor;
            switch (tacticalObjective.Outcome)
            {
                case TacticalBattleOutcome.UsmcVictory:
                    headline = "VICTORY";
                    headlineColor = new Color(.36f, .92f, .56f);
                    break;
                case TacticalBattleOutcome.UsmcDefeat:
                    headline = "DEFEAT";
                    headlineColor = new Color(.96f, .32f, .28f);
                    break;
                case TacticalBattleOutcome.Draw:
                    headline = "DRAW";
                    headlineColor = new Color(.94f, .78f, .30f);
                    break;
                default:
                    headline = "STALEMATE";
                    headlineColor = new Color(.82f, .70f, .40f);
                    break;
            }
            resultHeadlineStyle.normal.textColor = headlineColor;
            GUI.Label(new Rect(box.x, box.y + 20f, box.width, 46f), headline, resultHeadlineStyle);

            int usmcCasualties = tacticalUnitState.CombatStatus == TacticalCombatStatus.Reduced ? 1 : 0;
            int plaCasualties = CountReduced(tacticalEnemyStates);
            int battleTurnReached = tacticalObjective.OutcomeTurn - tacticalObjective.BattleStartTurn + 1;
            string summary = $"{MissionTypeLabel(tacticalObjective.MissionType)} • {tacticalBattlefield.BattlefieldId} • Objective {tacticalObjective.ObjectiveHex}\n" +
                $"Turn {battleTurnReached}/{tacticalObjective.TurnLimit} • USMC casualties {usmcCasualties}/1 • PLA casualties {plaCasualties}/{tacticalEnemyStates.Count}\n" +
                tacticalObjective.OutcomeSummary;
            if (lastBattleResultPath != null) summary += $"\nResult exported to {lastBattleResultPath}";
            GUI.Label(new Rect(box.x + 30f, box.y + 76f, box.width - 60f, 66f), summary, bodyStyle);

            if (lastBattleResultPath == null && battalionStatus != null && battalionStatus.History.Count > 0)
            {
                TacticalBattalionEngagementRecord lastEngagement = battalionStatus.History[battalionStatus.History.Count - 1];
                string battalionSummary = $"{battalionStatus.BattalionName} • Strength {lastEngagement.StrengthBefore}% → {lastEngagement.StrengthAfter}%";
                GUI.Label(new Rect(box.x + 30f, box.y + 142f, box.width - 60f, 20f), battalionSummary, bodyStyle);
            }

            GUI.Label(new Rect(box.x + 30f, box.y + 168f, box.width - 60f, 20f), "BATTLE LOG", badgeStyle);
            string logText = string.Join("\n", BuildTacticalEventLog(12));
            GUI.Label(new Rect(box.x + 30f, box.y + 190f, box.width - 60f, 210f), logText, bodyStyle);

            Rect dismissRect = new Rect(box.x + box.width / 2f - 90f, box.y + box.height - 50f, 180f, 34f);
            string dismissLabel = lastBattleResultPath != null ? "RETURN TO CAMPAIGN" : "RETURN TO ISLAND";
            if (GUI.Button(dismissRect, dismissLabel, buttonStyle)) ReturnToIsland(true);
        }

        private void DrawCampaignBriefing(float uiWidth, float uiHeight)
        {
            if (!campaignBriefingActive || campaignBriefingOpacity <= .001f) return;
            Color previousColor = GUI.color;
            GUI.color = new Color(.02f, .03f, .05f, campaignBriefingOpacity * .82f);
            GUI.DrawTexture(new Rect(0f, 0f, uiWidth, uiHeight), Texture2D.whiteTexture);
            GUI.color = previousColor;
            if (campaignBriefingOpacity < .98f) return;

            bool isError = battleRequestError != null;
            float boxWidth = 560f;
            float boxHeight = isError ? 220f : 260f;
            Rect box = new Rect(uiWidth / 2f - boxWidth / 2f, uiHeight / 2f - boxHeight / 2f, boxWidth, boxHeight);
            GUI.Box(box, GUIContent.none);

            if (isError)
            {
                resultHeadlineStyle.normal.textColor = new Color(.96f, .32f, .28f);
                GUI.Label(new Rect(box.x, box.y + 16f, box.width, 38f), campaignErrorTitle, resultHeadlineStyle);
                GUI.Label(new Rect(box.x + 30f, box.y + 64f, box.width - 60f, 100f), battleRequestError, bodyStyle);
                Rect dismissRect = new Rect(box.x + box.width / 2f - 110f, box.y + box.height - 48f, 220f, 34f);
                if (GUI.Button(dismissRect, "CONTINUE STANDALONE", buttonStyle)) DismissCampaignBriefing();
                return;
            }

            if (activeBattleRequest != null)
            {
                resultHeadlineStyle.normal.textColor = new Color(.42f, .82f, .96f);
                GUI.Label(new Rect(box.x, box.y + 16f, box.width, 38f), "SEA OF UNCERTAINTY", resultHeadlineStyle);
                string briefing = $"Request {activeBattleRequest.RequestId}" +
                    (string.IsNullOrEmpty(activeBattleRequest.CampaignId) ? string.Empty : $"  •  Campaign {activeBattleRequest.CampaignId}") +
                    $"\nTheater {activeBattleRequest.TheaterHex}  •  Seed {activeBattleRequest.Seed}" +
                    (tacticalObjective != null
                        ? $"\n{MissionTypeLabel(tacticalObjective.MissionType)}  •  Objective {tacticalObjective.ObjectiveHex}  •  Turn limit {tacticalObjective.TurnLimit}"
                        : string.Empty);
                GUI.Label(new Rect(box.x + 30f, box.y + 64f, box.width - 60f, 130f), briefing, bodyStyle);
                return;
            }

            if (tacticalObjective == null) return;
            resultHeadlineStyle.normal.textColor = new Color(.96f, .73f, .20f);
            GUI.Label(new Rect(box.x, box.y + 16f, box.width, 38f), MissionTypeLabel(tacticalObjective.MissionType) + " ORDERS", resultHeadlineStyle);
            string battalionLine = activeOperationalBattalion != null
                ? $"\nParent: {activeOperationalBattalion.DisplayName}  •  Strength {activeOperationalBattalion.Strength}%" +
                  (TacticalBattalion.IsUnderStrength(battalionStatus) ? "  •  UNDER STRENGTH — REDUCED AP" : string.Empty)
                : string.Empty;
            string standaloneBriefing = $"Objective {tacticalObjective.ObjectiveHex}  •  Turn limit {tacticalObjective.TurnLimit}{battalionLine}";
            GUI.Label(new Rect(box.x + 30f, box.y + 64f, box.width - 60f, 130f), standaloneBriefing, bodyStyle);
        }

        private void DrawActionPointPips(Rect area, TacticalUnitState state)
        {
            const float gap = 5f;
            float width = (area.width - gap * (state.MaximumActionPoints - 1)) / state.MaximumActionPoints;
            for (int index = 0; index < state.MaximumActionPoints; index++)
            {
                Color previous = GUI.color;
                GUI.color = index < state.RemainingActionPoints
                    ? new Color(.96f, .73f, .20f, 1f)
                    : new Color(.19f, .24f, .22f, 1f);
                GUI.Box(new Rect(area.x + index * (width + gap), area.y, width, area.height), GUIContent.none);
                GUI.color = previous;
            }
            GUI.Label(new Rect(area.x + area.width + 9f, area.y - 1f, 44f, 22f), $"{state.RemainingActionPoints}/{state.MaximumActionPoints}", bodyStyle);
        }

        private void EnsureStyles()
        {
            if (titleStyle != null) return;
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
            titleStyle.normal.textColor = new Color(.92f, .86f, .68f);
            bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true };
            bodyStyle.normal.textColor = new Color(.83f, .88f, .82f);
            badgeStyle = new GUIStyle(bodyStyle) { fontSize = 11, fontStyle = FontStyle.Bold };
            badgeStyle.normal.textColor = new Color(.34f, .78f, .73f);
            unitNameStyle = new GUIStyle(bodyStyle) { fontSize = 15, fontStyle = FontStyle.Bold };
            unitNameStyle.normal.textColor = new Color(.92f, .89f, .76f);
            stateStyle = new GUIStyle(badgeStyle) { alignment = TextAnchor.MiddleRight };
            stateStyle.normal.textColor = new Color(.96f, .73f, .20f);
            buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 12, fontStyle = FontStyle.Bold };
            reactionBannerStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            reactionBannerStyle.normal.textColor = new Color(1f, .40f, .32f);
            resultHeadlineStyle = new GUIStyle(GUI.skin.label) { fontSize = 36, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            resultHeadlineStyle.normal.textColor = Color.white;
        }

        private float GetUiScale()
        {
            if (settings.UiScale == UiScaleTier.Auto) return Mathf.Clamp(Screen.height / 720f, .90f, 1.35f);
            return UiScaleValueFor(settings.UiScale);
        }

        // Pure lookup, split out from GetUiScale() so a static regression can
        // assert the exact per-tier value without needing a live instance.
        private static float UiScaleValueFor(UiScaleTier tier)
        {
            switch (tier)
            {
                case UiScaleTier.Small: return .85f;
                case UiScaleTier.Normal: return 1.0f;
                case UiScaleTier.Large: return 1.2f;
                case UiScaleTier.ExtraLarge: return 1.5f;
                default: return 1.0f;
            }
        }

        private float AnimationTimeScale() => AnimationTimeScaleFor(settings.AnimationSpeed);

        // Pure lookup, split out so a static regression can assert the exact
        // per-tier value without needing a live instance. Multiplies every
        // fixed-duration presentation animation (movement steps, fire lines,
        // reaction-fire camera pans); it never changes what happens, only how
        // long it takes to watch. Skip is a small nonzero scale rather than 0
        // so duration-driven loops still resolve in a frame instead of stalling.
        private static float AnimationTimeScaleFor(AnimationSpeedTier tier)
        {
            switch (tier)
            {
                case AnimationSpeedTier.Fast: return .35f;
                case AnimationSpeedTier.Skip: return .08f;
                default: return 1f;
            }
        }

        private float TacticalWidthKilometres()
            => ((tacticalBattlefield.Width - 1) * Mathf.Sqrt(3f) * .5f + 1f) * tacticalBattlefield.CellSizeMetres / 1000f;

        private static Vector3 HexToWorld(HexCoord hex)
            => new Vector3(hex.Q * HexRadius * 1.5f, 0f, (hex.R + (hex.Q & 1) * .5f) * HexRadius * Mathf.Sqrt(3f));

        private static Vector3 LocalHexToWorld(HexCoord hex)
        {
            int centerQ = TacticalBattlefieldExtractor.DefaultWidth / 2;
            int centerR = TacticalBattlefieldExtractor.DefaultHeight / 2;
            return new Vector3(
                (hex.Q - centerQ) * HexRadius * 1.5f,
                0f,
                (hex.R - centerR + ((hex.Q & 1) - (centerQ & 1)) * .5f) * HexRadius * Mathf.Sqrt(3f));
        }

        private static Mesh CreateHexMesh(float radius, float thickness)
        {
            var vertices = new List<Vector3> { new Vector3(0f, thickness, 0f), new Vector3(0f, 0f, 0f) };
            for (int index = 0; index < 6; index++)
            {
                float angle = index * Mathf.PI / 3f;
                vertices.Add(new Vector3(Mathf.Cos(angle) * radius, thickness, Mathf.Sin(angle) * radius));
                vertices.Add(new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
            }
            var triangles = new List<int>();
            for (int index = 0; index < 6; index++)
            {
                int next = (index + 1) % 6;
                triangles.Add(0);
                triangles.Add(2 + next * 2);
                triangles.Add(2 + index * 2);
                triangles.Add(2 + index * 2);
                triangles.Add(3 + index * 2);
                triangles.Add(3 + next * 2);
                triangles.Add(2 + index * 2);
                triangles.Add(3 + next * 2);
                triangles.Add(2 + next * 2);
            }
            var mesh = new Mesh { name = "Flat Top Tactical Hex" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Material NewMaterial(Color color)
        {
            Shader shader = Resources.Load<Shader>("Shaders/MapTerrain") ?? Shader.Find("Sprites/Default");
            var material = new Material(shader) { color = color };
            return material;
        }

        // Separate shader/material from land so only water hexes carry the
        // time-scrolled ripple -- land keeps MapTerrain's static grain.
        private Material NewWaterMaterial()
        {
            Shader shader = Resources.Load<Shader>("Shaders/MapWater") ?? Resources.Load<Shader>("Shaders/MapTerrain") ?? Shader.Find("Sprites/Default");
            var material = new Material(shader) { color = Color.white };
            material.SetFloat("_Shimmer", ShimmerStrengthMultiplier());
            return material;
        }

        private static Material NewOverlayMaterial(Color color)
        {
            Shader shader = Resources.Load<Shader>("Shaders/MapOverlay") ?? Shader.Find("Sprites/Default");
            return new Material(shader) { color = color };
        }

        // Low drops elevation-contour shading entirely (fewer blended draw
        // calls' worth of visual complexity, plainer flat bands); High
        // deepens it for extra terrain definition. Medium is the original.
        private float ContourStrengthMultiplier()
        {
            switch (settings.GraphicsPreset)
            {
                case GraphicsPresetTier.Low: return 0f;
                case GraphicsPresetTier.High: return 1.35f;
                default: return 1f;
            }
        }

        // Low drops the animated ripple/sparkle entirely (water falls back
        // to a flat-shaded tile, matching Low's plainer land contour);
        // Medium/High keep it, High a little stronger.
        private float ShimmerStrengthMultiplier()
        {
            switch (settings.GraphicsPreset)
            {
                case GraphicsPresetTier.Low: return 0f;
                case GraphicsPresetTier.High: return 1.2f;
                default: return 1f;
            }
        }

        private float TacticalEdgeStrength()
        {
            switch (settings.GraphicsPreset)
            {
                case GraphicsPresetTier.Low: return .12f;
                case GraphicsPresetTier.High: return .42f;
                default: return .30f;
            }
        }

        private float TacticalAtmosphereStrength()
        {
            switch (settings.GraphicsPreset)
            {
                case GraphicsPresetTier.Low: return 0f;
                case GraphicsPresetTier.High: return .34f;
                default: return .24f;
            }
        }

        private Color LandColor(float metres)
        {
            float height = Mathf.InverseLerp(0f, 3900f, Mathf.Max(0f, metres));
            Color color;
            if (height < .28f)
                color = Color.Lerp(new Color(.18f, .33f, .20f), new Color(.35f, .43f, .24f), height / .28f);
            else if (height < .68f)
                color = Color.Lerp(new Color(.35f, .43f, .24f), new Color(.46f, .39f, .29f), (height - .28f) / .40f);
            else
                color = Color.Lerp(new Color(.46f, .39f, .29f), new Color(.72f, .69f, .60f), (height - .68f) / .32f);
            float contourDistance = Mathf.Abs(Mathf.Repeat(Mathf.Max(0f, metres) + 125f, 250f) - 125f);
            float contour = 1f - Mathf.SmoothStep(0f, 48f, contourDistance);
            return Color.Lerp(color, color * .68f, contour * .30f * ContourStrengthMultiplier());
        }

        private Color LocalLandColor(float metres)
        {
            float relief = Mathf.InverseLerp(localMinimumLandElevation, Mathf.Max(localMinimumLandElevation + 1f, localMaximumLandElevation), metres);
            Color low = new Color(.18f, .34f, .22f);
            Color high = new Color(.58f, .53f, .38f);
            Color color = Color.Lerp(low, high, relief);
            float contourDistance = Mathf.Abs(Mathf.Repeat(metres + 12.5f, 25f) - 12.5f);
            float contour = 1f - Mathf.SmoothStep(0f, 3.5f, contourDistance);
            return Color.Lerp(color, color * .69f, contour * .34f * ContourStrengthMultiplier());
        }

        // Gives cover-bearing hexes a subtle color signal even before the
        // discrete cover props (TacticalCoverView) are visible up close.
        private static Color CoverTint(Color color, TacticalCover cover)
        {
            switch (cover)
            {
                case TacticalCover.Light: return Color.Lerp(color, new Color(.24f, .32f, .18f), .12f);
                case TacticalCover.Medium: return Color.Lerp(color, new Color(.20f, .28f, .16f), .24f);
                case TacticalCover.Heavy: return Color.Lerp(color, new Color(.16f, .24f, .14f), .36f);
                default: return color;
            }
        }

        private Color WaterColor(float metres)
        {
            float depth = Mathf.Max(0f, -metres);
            Color color;
            if (depth < 200f)
                color = Color.Lerp(new Color(.16f, .43f, .43f), new Color(.10f, .32f, .37f), depth / 200f);
            else if (depth < 1500f)
                color = Color.Lerp(new Color(.10f, .32f, .37f), new Color(.055f, .20f, .29f), (depth - 200f) / 1300f);
            else
                color = Color.Lerp(new Color(.055f, .20f, .29f), new Color(.025f, .095f, .17f), Mathf.InverseLerp(1500f, 5000f, depth));
            float interval = depth < 500f ? 100f : depth < 2000f ? 500f : 1000f;
            float contourDistance = Mathf.Abs(Mathf.Repeat(depth + interval * .5f, interval) - interval * .5f);
            float contour = 1f - Mathf.SmoothStep(0f, interval * .10f, contourDistance);
            return Color.Lerp(color, color * .70f, contour * .20f * ContourStrengthMultiplier());
        }

        private static TacticalTerrain ClassifyTerrain(bool isLand, float metres)
        {
            if (!isLand) return TacticalTerrain.Water;
            if (metres >= 550f) return TacticalTerrain.Highland;
            return metres >= 160f ? TacticalTerrain.Rough : TacticalTerrain.Open;
        }

        private static string MovementCostLabel(TacticalTerrain terrain)
        {
            if (terrain == TacticalTerrain.Water) return "Impassable";
            return (terrain == TacticalTerrain.Open ? 1 : terrain == TacticalTerrain.Rough ? 2 : 3) + " AP";
        }

        private string CellInspectionText(HexCellView cell, string heading)
        {
            string vertical = cell.IsLand ? $"Elevation {cell.ElevationMetres:0} m" : $"Depth {Mathf.Max(0f, -cell.ElevationMetres):0} m";
            string text = $"{heading}  •  Hex {cell.Coord}\n{cell.Latitude:0.0000}°N  •  {cell.Longitude:0.0000}°E\n{cell.Terrain}  •  {vertical}  •  Move {MovementCostLabel(cell.Terrain)}";
            if (cell.Cover != TacticalCover.None)
                text += $"\nCover {cell.Cover}{(cell.IsBuiltUp ? "  •  Built-up" : string.Empty)}";
            if (tacticalMode)
                foreach (TacticalReconMarker marker in tacticalReconMarkers)
                {
                    if (!marker.Hex.Equals(cell.Coord)) continue;
                    text += $"\nRecon active • {marker.TurnsRemaining} turn{(marker.TurnsRemaining == 1 ? string.Empty : "s")} left";
                    break;
                }
            return text;
        }

        private void BuildPathLine()
        {
            GameObject lineObject = new GameObject("Movement Path Preview");
            lineObject.transform.SetParent(overviewRoot.transform, false);
            pathLine = lineObject.AddComponent<LineRenderer>();
            Shader overlayShader = Resources.Load<Shader>("Shaders/MapOverlay") ?? Shader.Find("Sprites/Default");
            pathLine.material = new Material(overlayShader);
            pathLine.alignment = LineAlignment.View;
            pathLine.numCapVertices = 6;
            pathLine.numCornerVertices = 5;
            pathLine.widthMultiplier = .13f;
            pathLine.startColor = new Color(1f, .82f, .28f, 1f);
            pathLine.endColor = new Color(.40f, 1f, .80f, 1f);
            pathLine.sortingOrder = 50;
            pathLine.enabled = false;
        }

        private void BuildOccupiedHexRing()
        {
            GameObject ringObject = new GameObject("Occupied Hex Outline");
            ringObject.transform.SetParent(overviewRoot.transform, false);
            occupiedHexRing = ringObject.AddComponent<LineRenderer>();
            Shader overlayShader = Resources.Load<Shader>("Shaders/MapOverlay") ?? Shader.Find("Sprites/Default");
            occupiedHexRing.material = new Material(overlayShader);
            occupiedHexRing.loop = true;
            occupiedHexRing.useWorldSpace = true;
            occupiedHexRing.positionCount = 6;
            occupiedHexRing.widthMultiplier = .075f;
            occupiedHexRing.numCornerVertices = 4;
            occupiedHexRing.startColor = new Color(1f, .76f, .20f, 1f);
            occupiedHexRing.endColor = occupiedHexRing.startColor;
            occupiedHexRing.sortingOrder = 58;
            occupiedHexRing.enabled = false;
        }

        private void UpdateOccupiedHexRing(HexCoord coord)
        {
            if (occupiedHexRing == null || !cells.TryGetValue(coord, out HexCellView cell)) return;
            Vector3 center = cell.transform.position + Vector3.up * (CellSurfaceOffset + .025f);
            for (int index = 0; index < 6; index++)
            {
                float angle = index * Mathf.PI / 3f;
                occupiedHexRing.SetPosition(index, center + new Vector3(Mathf.Cos(angle) * .86f, 0f, Mathf.Sin(angle) * .86f));
            }
            occupiedHexRing.enabled = true;
        }

        private void BuildCoastAccents()
        {
            Material coastMaterial = NewOverlayMaterial(Color.white);
            foreach (KeyValuePair<HexCoord, HexCellView> pair in cells)
            {
                if (!pair.Value.IsLand) continue;
                foreach (HexCoord neighbor in MovementPlanner.Neighbors(pair.Key))
                {
                    if (!cells.TryGetValue(neighbor, out HexCellView adjacent) || adjacent.IsLand) continue;
                    Vector3 landCenter = pair.Value.transform.position;
                    Vector3 direction = HexToWorld(neighbor) - HexToWorld(pair.Key);
                    direction.y = 0f;
                    direction.Normalize();
                    Vector3 tangent = new Vector3(-direction.z, 0f, direction.x);
                    Vector3 midpoint = landCenter + direction * (HexRadius * .855f);
                    Vector3 start = midpoint - tangent * (HexRadius * .49f);
                    Vector3 end = midpoint + tangent * (HexRadius * .49f);
                    CreateCoastStroke("Wet Shore " + pair.Key, start + Vector3.up * .115f, end + Vector3.up * .115f, .15f, new Color(.035f, .20f, .20f, .82f), coastMaterial, 20);
                    CreateCoastStroke("Coast Highlight " + pair.Key, start + Vector3.up * .145f, end + Vector3.up * .145f, .045f, new Color(.60f, .91f, .76f, .78f), coastMaterial, 21);
                }
            }
        }

        private void CreateCoastStroke(string objectName, Vector3 start, Vector3 end, float width, Color color, Material material, int sortingOrder)
        {
            GameObject accent = new GameObject(objectName);
            accent.transform.SetParent(overviewRoot.transform, false);
            LineRenderer line = accent.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.widthMultiplier = width;
            line.numCapVertices = 2;
            line.sharedMaterial = material;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.startColor = color;
            line.endColor = color;
            line.sortingOrder = sortingOrder;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
        }

        private void BuildGeographicLabels()
        {
            CreateMapLabel("TAIWAN", 120.96, 23.70, .80f, new Color(.91f, .85f, .66f, .46f), true);
            CreateMapLabel("TAIWAN STRAIT", 119.93, 23.55, .52f, new Color(.55f, .82f, .82f, .34f), true);
            CreateMapLabel("PHILIPPINE SEA", 121.90, 23.30, .52f, new Color(.55f, .82f, .82f, .30f), true);
            CreateMapLabel("TAIPEI", 121.565, 25.035, .42f, new Color(.92f, .88f, .72f, .68f), false);
            CreateMapLabel("KAOHSIUNG", 120.30, 22.63, .42f, new Color(.92f, .88f, .72f, .68f), false);
        }

        private void CreateMapLabel(string text, double longitude, double latitude, float size, Color color, bool overviewOnly)
        {
            float q = (float)((longitude - DemoWest) / (DemoEast - DemoWest) * (Width - 1));
            float r = (float)((latitude - DemoSouth) / (DemoNorth - DemoSouth) * (Height - 1));
            float measuredElevation = elevation == null ? 0f : elevation.SampleMetres(longitude, latitude);
            float y = coastline != null && coastline.ContainsLand(longitude, latitude)
                ? Mathf.Clamp(measuredElevation, 0f, 4000f) * .00072f + .34f
                : .22f;
            GameObject labelObject = new GameObject("Map Label " + text);
            labelObject.transform.SetParent(overviewRoot.transform, false);
            labelObject.transform.position = new Vector3(q * HexRadius * 1.5f, y, (r + .25f) * HexRadius * Mathf.Sqrt(3f));
            labelObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            TextMesh label = labelObject.AddComponent<TextMesh>();
            label.text = text;
            label.alignment = TextAlignment.Center;
            label.anchor = TextAnchor.MiddleCenter;
            label.fontSize = 64;
            label.characterSize = size;
            label.color = color;
            label.fontStyle = FontStyle.Bold;
            MeshRenderer labelRenderer = labelObject.GetComponent<MeshRenderer>();
            labelRenderer.sortingOrder = 18;
            labelRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            labelRenderer.receiveShadows = false;
            geographicLabels.Add(new GeographicLabel
            {
                Transform = labelObject.transform,
                Text = label,
                BaseColor = color,
                OverviewOnly = overviewOnly
            });
        }

        private static void BuildInfantrySymbol(Transform parent)
        {
            Color ink = new Color(.075f, .10f, .08f, 1f);
            CreateCounterStroke(parent, "Infantry Slash A", new Vector3(-.30f, .32f, -.20f), new Vector3(.30f, .32f, .20f), ink);
            CreateCounterStroke(parent, "Infantry Slash B", new Vector3(-.30f, .32f, .20f), new Vector3(.30f, .32f, -.20f), ink);
        }

        private static void CreateCounterStroke(Transform parent, string name, Vector3 start, Vector3 end, Color color)
        {
            GameObject stroke = new GameObject(name);
            stroke.transform.SetParent(parent, false);
            LineRenderer line = stroke.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = 2;
            line.widthMultiplier = .035f;
            Shader overlayShader = Resources.Load<Shader>("Shaders/MapOverlay") ?? Shader.Find("Sprites/Default");
            line.material = new Material(overlayShader);
            line.startColor = color;
            line.endColor = color;
            line.sortingOrder = 66;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
        }

        private HexCellView FindCoastalOperationalCell()
        {
            HexCellView best = cells[unitState.Position];
            int bestDistance = int.MaxValue;
            foreach (HexCellView candidate in cells.Values)
            {
                if (!candidate.IsLand) continue;
                bool coast = false;
                foreach (HexCoord neighbor in MovementPlanner.Neighbors(candidate.Coord))
                    if (cells.TryGetValue(neighbor, out HexCellView adjacent) && !adjacent.IsLand) coast = true;
                if (!coast) continue;
                int distance = HexCoord.Distance(unitState.Position, candidate.Coord);
                if (distance >= bestDistance) continue;
                best = candidate;
                bestDistance = distance;
            }
            return best;
        }

        private HexCellView FindHighReliefOperationalCell()
        {
            HexCellView best = cells[unitState.Position];
            foreach (HexCellView candidate in cells.Values)
                if (candidate.IsLand && candidate.ElevationMetres > best.ElevationMetres) best = candidate;
            return best;
        }

        private HexCellView FindLosCaptureTarget()
        {
            HexCellView best = localCells[tacticalUnitState.Position];
            int bestScore = -1;
            foreach (HexCellView candidate in localCells.Values)
            {
                int distance = HexCoord.Distance(tacticalUnitState.Position, candidate.Coord);
                if (distance == 0 || distance > TacticalLineOfSight.MaximumInspectionRangeHexes) continue;
                TacticalLosResult result = TacticalLineOfSight.Inspect(localMovementBoard, tacticalUnitState.Position, candidate.Coord);
                if (!result.IsValid) continue;
                int firstBlocked = result.Samples.FindIndex(sample => sample.State == TacticalLosState.Blocked);
                int visibleApproach = firstBlocked < 0 ? result.RangeHexes : Math.Max(0, firstBlocked - 1);
                bool readableTransition = result.State == TacticalLosState.Blocked && visibleApproach >= 2;
                int stateScore = readableTransition ? 3 : result.State == TacticalLosState.Obscured ? 2 : result.State == TacticalLosState.Clear ? 1 : 0;
                // The proof image should explain the system at a glance: prefer a blocked
                // sightline with a visible clear approach, then obscured, then fully clear.
                int score = stateScore * 10000 + visibleApproach * 100 + distance;
                if (score <= bestScore) continue;
                best = candidate;
                bestScore = score;
            }
            return best;
        }

        private HexCoord FindLocalDeploymentHex(HexCoord preferred)
        {
            if (localCells.TryGetValue(preferred, out HexCellView preferredCell) && preferredCell.IsLand) return preferred;
            HexCoord best = preferred;
            int distance = int.MaxValue;
            foreach (HexCellView cell in localCells.Values)
            {
                if (!cell.IsLand) continue;
                int candidate = HexCoord.Distance(preferred, cell.Coord);
                if (candidate >= distance) continue;
                best = cell.Coord;
                distance = candidate;
            }
            return best;
        }

        private Vector3 LocalCounterPosition(HexCoord coord)
            => localCells[coord].transform.position + Vector3.up * (CellSurfaceOffset + CounterClearance);

        private HexCoord FindDeploymentHex(HexCoord preferred)
        {
            HexCoord best = preferred;
            int bestDistance = int.MaxValue;
            bool found = false;
            foreach (KeyValuePair<HexCoord, TacticalCell> pair in board)
            {
                if (!pair.Value.IsPassable) continue;
                int distance = HexCoord.Distance(preferred, pair.Key);
                if (found && (distance > bestDistance || distance == bestDistance &&
                    (pair.Key.Q > best.Q || pair.Key.Q == best.Q && pair.Key.R >= best.R))) continue;
                best = pair.Key;
                bestDistance = distance;
                found = true;
            }
            if (!found) throw new InvalidOperationException("The prototype map contains no passable deployment hex.");
            return best;
        }
    }
}
