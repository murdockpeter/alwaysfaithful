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

        // Whole-island operational layer shared with Sea of Uncertainty. Tactical
        // engagements will resolve into separate 250 m local maps in a later pass.
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
        private TacticalTurnState turnState;
        private HexCellView selectedCell;
        private HexCellView hoveredCell;
        private Vector3 cameraFocus;
        private float cameraDistance = 190f;
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
        private bool resultScreenActive;
        private float resultScreenOpacity;
        private GUIStyle resultHeadlineStyle;
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
            fastEnemyAnimation = Array.IndexOf(Environment.GetCommandLineArgs(), "--fast-enemy") >= 0;
            LoadGeography();
            BuildLightingAndCamera();
            overviewRoot = new GameObject("Taiwan Operational Map");
            overviewRoot.transform.SetParent(transform, false);
            BuildCommandTable();
            BuildBoard();
            BuildUnit();
            ApplyCamera();
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
            if (automatedObservationRegression) StartCoroutine(RunObservationRegression());
            if (automatedFireRegression) StartCoroutine(RunFireRegression());
            if (automatedSuppressionRegression) StartCoroutine(RunSuppressionRegression());
            if (automatedReactionRegression) StartCoroutine(RunReactionRegression());
            if (automatedEnemyTurnRegression) StartCoroutine(RunEnemyTurnRegression());
            if (automatedCoverRegression) StartCoroutine(RunCoverRegression());
            if (automatedVictoryRegression) StartCoroutine(RunVictoryRegression());
            if (automatedTacticalCapture)
            {
                EnterTacticalMap(FindCoastalOperationalCell(), false);
                BeginTacticalMovePlanning();
                DisplayTacticalCaptureRoute();
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
            StartCoroutine(CaptureScreenshotWhenRequested());
        }

        private void Update()
        {
            if (automatedCapture || automatedMovementRegression || automatedTacticalRegression || automatedTacticalMovementRegression || automatedLosRegression || automatedObservationRegression || automatedFireRegression || automatedSuppressionRegression || automatedReactionRegression || automatedEnemyTurnRegression || automatedCoverRegression || automatedVictoryRegression || automatedTacticalCapture || automatedLosCapture || automatedObservationCapture || automatedFireCapture || automatedSuppressionCapture || automatedReactionCapture || automatedEnemyTurnCapture || automatedResultCapture || mapTransitionActive) return;
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
            HexCoord start = FindDeploymentHex(new HexCoord(Width / 3, Height / 4));
            Vector3 position = HexToWorld(start);
            if (cells.TryGetValue(start, out HexCellView cell)) position.y = cell.transform.position.y;

            GameObject counterRoot = new GameObject("USMC Rifle Platoon");
            counterRoot.transform.SetParent(overviewRoot.transform, false);
            // The counter is physically anchored to its hex; the overlay shader, rather than
            // a large altitude offset, guarantees that terrain cannot hide critical state.
            counterRoot.transform.position = position + Vector3.up * (CellSurfaceOffset + CounterClearance);
            SphereCollider counterCollider = counterRoot.AddComponent<SphereCollider>();
            counterCollider.radius = .65f;
            counterCollider.center = Vector3.up * .12f;
            unit = counterRoot.AddComponent<UnitCounterView>();
            unitState = new TacticalUnitState("usmc-rifle-platoon-1", "USMC Rifle Platoon", start, PlatoonMovementPoints);
            turnState = new TacticalTurnState();
            unit.Initialize(unitState.DisplayName);
            UpdateOccupiedHexRing(start);

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
            unit.BindRenderers(baseRenderer, faceRenderer);
            unit.Present(unitState);

            GameObject symbolObject = new GameObject("Unit Label");
            symbolObject.transform.SetParent(counterRoot.transform, false);
            symbolObject.transform.localPosition = Vector3.up * .276f;
            symbolObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            symbolObject.transform.localScale = Vector3.one * .22f;
            TextMesh label = symbolObject.AddComponent<TextMesh>();
            label.text = "USMC\nRIFLE PLT";
            label.alignment = TextAlignment.Center;
            label.anchor = TextAnchor.MiddleCenter;
            label.fontSize = 42;
            label.characterSize = .10f;
            label.color = new Color(.08f, .12f, .10f);
            BuildInfantrySymbol(counterRoot.transform);
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

        private void RequestTacticalMap(HexCellView parentCell)
        {
            if (parentCell == null || !parentCell.IsLand || mapTransitionActive) return;
            tacticalAudio.Play(TacticalSound.MapTransition);
            StartCoroutine(TransitionToTactical(parentCell));
        }

        private IEnumerator TransitionToTactical(HexCellView parentCell)
        {
            mapTransitionActive = true;
            yield return FadeMapTransition(0f, 1f, .22f);
            EnterTacticalMap(parentCell, false);
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

        private void EnterTacticalMap(HexCellView parentCell, bool animate)
        {
            if (animate)
            {
                RequestTacticalMap(parentCell);
                return;
            }
            if (!tacticalMode)
            {
                overviewCameraFocus = cameraFocus;
                overviewCameraDistance = cameraDistance;
            }
            CancelUnitInteraction();
            string requestedId = $"TW-{parentCell.Coord.Q:D2}-{parentCell.Coord.R:D3}-250M";
            if (tacticalBattlefield == null || tacticalBattlefield.BattlefieldId != requestedId) BuildTacticalBattlefield(parentCell);
            tacticalMode = true;
            overviewRoot.SetActive(false);
            tacticalRoot.SetActive(true);
            cameraFocus = Vector3.zero;
            cameraDistance = 33f;
            hoveredLocalCell = null;
            ApplyCamera();
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
            tacticalRoot.SetActive(false);
            overviewRoot.SetActive(true);
            tacticalMode = false;
            cameraFocus = overviewCameraFocus;
            cameraDistance = overviewCameraDistance;
            ApplyCamera();
            Debug.Log($"ALWAYS_FAITHFUL_TACTICAL_EXIT battlefield={tacticalBattlefield.BattlefieldId} parent={tacticalBattlefield.ParentHex}");
        }

        private void BuildTacticalBattlefield(HexCellView parentCell)
        {
            if (tacticalRoot != null) Destroy(tacticalRoot);
            localCells.Clear();
            localMovementBoard.Clear();
            tacticalEventSequence = 0;
            tacticalBattlefield = TacticalBattlefieldExtractor.Extract(
                parentCell.Coord,
                parentCell.Longitude,
                parentCell.Latitude,
                (longitude, latitude) => elevation.SampleMetres(longitude, latitude),
                (longitude, latitude) => coastline.ContainsLand(longitude, latitude));
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
            Shader overlayShader = Resources.Load<Shader>("Shaders/MapOverlay") ?? Shader.Find("Sprites/Default");
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
                renderer.sharedMaterial = sharedMaterial;
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
                TacticalCoverView.Build(cellObject.transform, source.LocalCoord, source.Cover, source.IsBuiltUp, overlayShader);
            }
            BuildTacticalTable();
            BuildTacticalShoreline();
            BuildTacticalReferenceMarks();
            BuildTacticalUnit();
            BuildTacticalMovementVisuals();
            BuildTacticalLosVisuals();
            BuildTacticalFireVisuals();
            BuildTacticalContacts();
            BuildTacticalObjective();
            RefreshTacticalObservation();
            tacticalOrderFeedback = "RMB platoon for tactical orders";
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

        private void BuildTacticalObjective()
        {
            string[] args = Environment.GetCommandLineArgs();
            string postureArgument = Array.Find(args, value => value.StartsWith("--posture=", StringComparison.Ordinal));
            string postureValue = postureArgument?.Substring("--posture=".Length);
            TacticalPosture posture = string.Equals(postureValue, "defend", StringComparison.OrdinalIgnoreCase)
                ? TacticalPosture.Defend
                : string.Equals(postureValue, "attack", StringComparison.OrdinalIgnoreCase)
                    ? TacticalPosture.Attack
                    : TacticalVictory.ChoosePosture(tacticalBattlefield.BattlefieldId);

            string turnLimitArgument = Array.Find(args, value => value.StartsWith("--turn-limit=", StringComparison.Ordinal));
            int turnLimit = turnLimitArgument != null &&
                int.TryParse(turnLimitArgument.Substring("--turn-limit=".Length), out int parsedLimit) && parsedLimit > 0
                ? parsedLimit
                : TacticalVictory.DefaultTurnLimit;

            var enemyStarts = new List<HexCoord>();
            foreach (TacticalUnitState enemy in tacticalEnemyStates) enemyStarts.Add(enemy.Position);
            HexCoord center = new HexCoord(tacticalBattlefield.Width / 2, tacticalBattlefield.Height / 2);
            HexCoord objectiveHex = TacticalVictory.ChooseObjective(localMovementBoard, center, posture, enemyStarts, tacticalBattlefield.BattlefieldId);

            tacticalObjective = new TacticalObjectiveState
            {
                ObjectiveHex = objectiveHex,
                Posture = posture,
                TurnLimit = turnLimit,
                BattleStartTurn = turnState.TurnNumber
            };
            tacticalBattlefield.Objective = tacticalObjective;
            BuildTacticalObjectiveMarker(objectiveHex);
            // Only draw a separate deployment-zone ring when it wouldn't just sit
            // on top of the objective marker (Defend's objective is the deployment
            // hex itself, so the objective ring already communicates that setup).
            if (!objectiveHex.Equals(center)) BuildTacticalDeploymentZone(center);
            Debug.Log($"ALWAYS_FAITHFUL_OBJECTIVE_SET battlefield={tacticalBattlefield.BattlefieldId} posture={posture} hex={objectiveHex} turnLimit={turnLimit} startTurn={tacticalObjective.BattleStartTurn}");
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
        }

        private void BuildTacticalUnit()
        {
            HexCoord start = FindLocalDeploymentHex(new HexCoord(tacticalBattlefield.Width / 2, tacticalBattlefield.Height / 2));
            GameObject root = new GameObject("Tactical USMC Rifle Platoon");
            root.transform.SetParent(tacticalRoot.transform, false);
            root.transform.position = LocalCounterPosition(start);
            SphereCollider collider = root.AddComponent<SphereCollider>();
            collider.radius = .65f;
            collider.center = Vector3.up * .12f;
            tacticalUnit = root.AddComponent<UnitCounterView>();
            tacticalUnitState = new TacticalUnitState("usmc-rifle-platoon-1", "USMC Rifle Platoon", start, TacticalPlatoonActionPoints);
            tacticalWeapon = new TacticalWeaponState("m27-small-arms", "M27 Small Arms", 6);
            tacticalUnit.Initialize(tacticalUnitState.DisplayName);
            tacticalFormationView = root.AddComponent<TacticalFormationView>();
            tacticalFormationView.Initialize(TacticalFormationAffiliation.Usmc, "USMC", "RIFLE PLT");
            tacticalUnit.BindRenderers(tacticalFormationView.CommandDeckRenderer, tacticalFormationView.DesignationRenderer);
            tacticalUnit.Present(tacticalUnitState);
            tacticalFormationView.Present(tacticalUnitState);
            localMovementBoard[start].OccupantId = tacticalUnitState.Id;
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

        private void BuildTacticalContacts()
        {
            tacticalEnemyStates.Clear();
            tacticalContacts.Clear();
            tacticalContactViews.Clear();
            tacticalEnemyWeapons.Clear();
            var occupied = new HashSet<HexCoord> { tacticalUnitState.Position };
            HexCoord riflePosition = FindObservationDeployment(TacticalVisibilityState.Observed, occupied);
            occupied.Add(riflePosition);
            HexCoord supportPosition = FindObservationDeployment(TacticalVisibilityState.Contact, occupied);
            TacticalUnitState rifle = new TacticalUnitState("pla-rifle-squad-1", "PLA Rifle Squad", riflePosition, 4);
            TacticalUnitState support = new TacticalUnitState("pla-support-team-1", "PLA Support Team", supportPosition, 4);
            tacticalEnemyStates.Add(rifle);
            tacticalEnemyStates.Add(support);
            foreach (TacticalUnitState enemy in tacticalEnemyStates)
            {
                localMovementBoard[enemy.Position].OccupantId = enemy.Id;
                GameObject markerObject = new GameObject("Contact " + enemy.Id);
                markerObject.transform.SetParent(tacticalRoot.transform, false);
                markerObject.transform.position = LocalCounterPosition(enemy.Position) + Vector3.up * .03f;
                ContactMarkerView marker = markerObject.AddComponent<ContactMarkerView>();
                marker.Initialize(enemy.DisplayName.Contains("Support") ? "SUPPORT" : "RIFLE");
                tacticalContactViews.Add(enemy.Id, marker);
                tacticalEnemyWeapons.Add(enemy.Id, new TacticalWeaponState(enemy.Id + "-weapon", "Squad Small Arms", 6));
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
            UpdateObjectiveMarkerColor();
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
            if (tacticalUnitMoving || tacticalFireResolving || tacticalEnemyTurnActive || resultScreenActive) return;
            Vector2 guiPointer = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y) / GetUiScale();
            if (returnToIslandRect.Contains(guiPointer) || tacticalMenuOpen && tacticalMenuRect.Contains(guiPointer) ||
                new Rect(20f, 18f, 405f, 294f).Contains(guiPointer)) return;
            if (Input.GetKeyDown(KeyCode.Escape))
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
                if (tacticalMovePlanning || tacticalLosPlanning || tacticalFirePlanning)
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
                Mathf.Clamp(pointer.y, 8f, Screen.height / GetUiScale() - 190f),
                178f, 172f);
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
            if (!tacticalFirePlanning || tacticalFirePreview == null || !tacticalFirePreview.IsValid ||
                !tacticalFirePreview.TargetPosition.Equals(targetPosition)) return false;
            int sequence = tacticalEventSequence + 1;
            int seed = TacticalDirectFire.CreateSeed(tacticalBattlefield.BattlefieldId, turnState.TurnNumber, sequence);
            TacticalFireEvent fireEvent = TacticalDirectFire.Resolve(tacticalFirePreview, tacticalWeapon, seed);
            if (fireEvent.Outcome == TacticalFireOutcome.Rejected || !tacticalUnitState.TrySpendActionPoints(TacticalDirectFire.ActionPointCost)) return false;
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
            for (float elapsed = 0f; elapsed < .46f; elapsed += Time.deltaTime)
            {
                float progress = Mathf.Clamp01(elapsed / .46f);
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

        private static Color LosColor(TacticalLosState state, float alpha)
        {
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
                for (float elapsed = 0f; elapsed < .22f; elapsed += Time.deltaTime)
                {
                    float progress = Mathf.Clamp01(elapsed / .22f);
                    Vector3 position = Vector3.Lerp(start, end, Mathf.SmoothStep(0f, 1f, progress));
                    position.y += Mathf.Sin(progress * Mathf.PI) * .16f;
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
            float panElapsed = 0f;
            while (panElapsed < .35f)
            {
                panElapsed += Time.unscaledDeltaTime;
                float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(panElapsed / .35f));
                cameraFocus = Vector3.Lerp(savedFocus, reactorFocus, progress);
                cameraDistance = Mathf.Lerp(savedDistance, Mathf.Min(savedDistance, 16f), progress);
                ApplyCamera();
                yield return null;
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

            panElapsed = 0f;
            Vector3 fromFocus = cameraFocus;
            float fromDistance = cameraDistance;
            while (panElapsed < .35f)
            {
                panElapsed += Time.unscaledDeltaTime;
                float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(panElapsed / .35f));
                cameraFocus = Vector3.Lerp(fromFocus, savedFocus, progress);
                cameraDistance = Mathf.Lerp(fromDistance, savedDistance, progress);
                ApplyCamera();
                yield return null;
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
            for (float elapsed = 0f; elapsed < .46f; elapsed += Time.deltaTime)
            {
                float progress = Mathf.Clamp01(elapsed / .46f);
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
            if (recordCancellation && (tacticalMovePlanning || tacticalLosPlanning || tacticalFirePlanning || tacticalMenuOpen))
                tacticalAudio.Play(TacticalSound.OrderCancel);
            if (recordCancellation && tacticalMovePlanning)
                RecordTacticalMovement(tacticalUnitState.Position, tacticalUnitState.Position, 0,
                    tacticalUnitState.RemainingActionPoints, tacticalUnitState.RemainingActionPoints,
                    "Cancelled", "Move planning cancelled", new List<HexCoord>());
            tacticalMovePlanning = false;
            tacticalLosPlanning = false;
            tacticalFirePlanning = false;
            tacticalMenuOpen = false;
            tacticalUnitState.IsSelected = false;
            tacticalUnit.Present(tacticalUnitState);
            ClearTacticalReachable();
            ClearTacticalPreview();
            ClearTacticalLineOfSight();
            ClearTacticalFirePreview();
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
            if (mapCamera == null || unitMoving) return;
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
                    if (movePlanning && pointedUnit == null && nextHover != null && TryIssueMove(nextHover.Coord)) return;
                    if (nextHover != null) SelectCell(nextHover);
                }
                if (rightClick)
                {
                    string target = pointedUnit != null ? unitState.DisplayName : nextHover.Coord.ToString();
                    Debug.Log($"ALWAYS_FAITHFUL_RMB target={target} selected={unitState.IsSelected}");
                    if (pointedUnit != null)
                    {
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
                Mathf.Clamp(pointer.y, 8f, Screen.height / GetUiScale() - 90f),
                178f,
                72f);
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
            unitState.IsSelected = false;
            unit.Present(unitState);
            ClearReachable();
            ClearPreviewPath();
        }

        private void EndTurn()
        {
            if (unitMoving) return;
            CancelUnitInteraction();
            turnState.EndTurn(unitState);
            if (tacticalUnitState != null)
            {
                tacticalUnitState.BeginTurn();
                tacticalUnit.Present(tacticalUnitState);
            }
            unit.Present(unitState);
            tacticalAudio.Play(TacticalSound.EndTurn);
            Debug.Log($"ALWAYS_FAITHFUL_TURN_STARTED turn={turnState.TurnNumber} side={turnState.ActiveSide} ap={unitState.RemainingActionPoints}/{unitState.MaximumActionPoints}");
        }

        private void EndTacticalTurn()
        {
            if (tacticalUnitMoving || tacticalFireResolving || tacticalEnemyTurnActive || resultScreenActive) return;
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
                TacticalAiOrder order = TacticalEnemyTurn.PlanOrder(localMovementBoard, enemy, weapon,
                    tacticalUnitState.Position, tacticalUnitState, turnState.TurnNumber, seed);
                if (!TacticalEnemyTurn.ValidateOrder(localMovementBoard, order, enemy, weapon,
                        tacticalUnitState, turnState.TurnNumber, out string rejection))
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
                yield return ExecuteEnemyOrder(enemy, weapon, order, visible);
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
            unitState.BeginTurn();
            unit.Present(unitState);
            tacticalUnit.Present(tacticalUnitState);
            tacticalFormationView.Present(tacticalUnitState);
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
            TacticalBattleOutcome outcome = TacticalVictory.Evaluate(tacticalObjective, tacticalUnitState, tacticalEnemyStates,
                localMovementBoard, turnState.TurnNumber, out string summary);
            if (outcome == TacticalBattleOutcome.InProgress) return;
            tacticalObjective.Outcome = outcome;
            tacticalObjective.OutcomeTurn = turnState.TurnNumber;
            tacticalObjective.OutcomeSummary = summary;
            Debug.Log($"ALWAYS_FAITHFUL_BATTLE_OUTCOME outcome={outcome} turn={turnState.TurnNumber} summary={summary}");
            StartCoroutine(FadeResultScreen(0f, 1f, .45f));
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

        private List<string> BuildTacticalEventLog(int maximumEntries)
        {
            var entries = new List<(int Sequence, string Line)>();
            foreach (TacticalMovementEvent movement in tacticalBattlefield.MovementEvents)
                entries.Add((movement.Sequence, $"MOVE • {movement.UnitId} {movement.Origin}→{movement.Destination} • {movement.Outcome}"));
            foreach (TacticalFireEvent fire in tacticalBattlefield.FireEvents)
                entries.Add((fire.Sequence, $"FIRE • T{fire.Turn} • {fire.AttackerId}→{fire.TargetId} • {fire.Outcome} ({fire.HitChance}% hit)"));
            foreach (TacticalSuppressionEvent suppression in tacticalBattlefield.SuppressionEvents)
                entries.Add((suppression.Sequence, $"SUPPRESSION • T{suppression.Turn} • {suppression.UnitId} {suppression.StatusBefore}→{suppression.StatusAfter} ({suppression.Cause})"));
            foreach (TacticalReactionEvent reaction in tacticalBattlefield.ReactionEvents)
                entries.Add((reaction.Sequence, $"REACTION • T{reaction.Turn} • {reaction.ReactorId}→{reaction.MoverId} • {reaction.Outcome} • {reaction.Resolution}"));
            foreach (TacticalEnemyActionEvent enemyAction in tacticalBattlefield.EnemyActionEvents)
                entries.Add((enemyAction.Sequence, $"PLA • T{enemyAction.Turn} • {enemyAction.Summary}"));
            entries.Sort((a, b) => a.Sequence.CompareTo(b.Sequence));
            var lines = new List<string>();
            int start = Mathf.Max(0, entries.Count - maximumEntries);
            for (int index = start; index < entries.Count; index++) lines.Add(entries[index].Line);
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
            TacticalAiOrder order, bool visible)
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
                    yield return ExecuteEnemyFire(enemy, weapon, order, visible);
                    break;
                case TacticalAiOrderKind.Move:
                    yield return ExecuteEnemyMove(enemy, order, visible);
                    break;
                case TacticalAiOrderKind.Observe:
                    TacticalObservation.Check(localMovementBoard, enemy.Id, enemy.Position,
                        tacticalUnitState.Id, tacticalUnitState.DisplayName, tacticalUnitState.Position, turnState.TurnNumber);
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
                float duration = fastEnemyAnimation ? .035f : .18f;
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
            TacticalAiOrder order, bool visible)
        {
            TacticalContactState target = TacticalObservation.Check(localMovementBoard, enemy.Id, enemy.Position,
                tacticalUnitState.Id, tacticalUnitState.DisplayName, tacticalUnitState.Position, turnState.TurnNumber);
            TacticalFirePreview preview = TacticalDirectFire.Preview(localMovementBoard, enemy.Id, enemy.Position,
                target, tacticalUnitState.Position, weapon, enemy.RemainingActionPoints);
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
            => new WaitForSecondsRealtime(fastEnemyAnimation ? Mathf.Min(.04f, normalSeconds) : normalSeconds);

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
                for (float elapsed = 0f; elapsed < .20f; elapsed += Time.deltaTime)
                {
                    float blend = Mathf.SmoothStep(0f, 1f, elapsed / .20f);
                    unit.transform.position = Vector3.Lerp(start, end, blend);
                    yield return null;
                }
                unit.transform.position = end;
            }
            unitState.CompleteMove(destination);
            unit.Present(unitState);
            UpdateOccupiedHexRing(destination);
            yield return new WaitForSeconds(.18f);
            ClearPreviewPath();
            unitMoving = false;
            movePlanning = false;
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

        private IEnumerator RunObservationRegression()
        {
            yield return null;
            if (!ValidateObservationRules(out string failure))
            {
                Debug.LogError("ALWAYS_FAITHFUL_OBSERVATION_REGRESSION_FAILED rules " + failure);
                Application.Quit(1);
                yield break;
            }
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
            if (tacticalContacts.Count != 2 || tacticalContactViews.Count != 2 || visibleMarkers < 1 || detailedFormations < 1 || uncertainGlyphs < 1 ||
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
            var watch = System.Diagnostics.Stopwatch.StartNew();
            TacticalAiOrder first = TacticalEnemyTurn.PlanOrder(board, mover, weapon, opponent.Position, opponent, 1, 404);
            TacticalAiOrder replay = TacticalEnemyTurn.PlanOrder(board, mover, weapon, opponent.Position, opponent, 1, 404);
            for (int index = 0; index < 100; index++)
                TacticalEnemyTurn.PlanOrder(board, mover, weapon, opponent.Position, opponent, 1, 404 + index);
            watch.Stop();
            elapsedMilliseconds = watch.ElapsedMilliseconds;
            if (first.Kind != TacticalAiOrderKind.Move || JsonUtility.ToJson(first) != JsonUtility.ToJson(replay) ||
                HexCoord.Distance(first.Destination, opponent.Position) >= HexCoord.Distance(mover.Position, opponent.Position) ||
                !TacticalEnemyTurn.ValidateOrder(board, first, mover, weapon, opponent, 1, out _))
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
            if (TacticalEnemyTurn.ValidateOrder(board, illegal, mover, weapon, opponent, 1, out _))
            {
                failure = "illegal AI order accepted";
                return false;
            }

            var recovering = new TacticalUnitState("recover", "Recovering", new HexCoord(1, 0), 4);
            recovering.ApplySuppressionPoints(TacticalSuppression.DisruptedThreshold);
            TacticalAiOrder recovery = TacticalEnemyTurn.PlanOrder(board, recovering, weapon,
                opponent.Position, opponent, 1, 11);
            if (recovery.Kind != TacticalAiOrderKind.Recover)
            {
                failure = "recovery priority";
                return false;
            }

            var firer = new TacticalUnitState("firer", "Firer", new HexCoord(13, 0), 4);
            TacticalAiOrder fire = TacticalEnemyTurn.PlanOrder(board, firer, weapon,
                opponent.Position, opponent, 1, 12);
            if (fire.Kind != TacticalAiOrderKind.Fire ||
                !TacticalEnemyTurn.ValidateOrder(board, fire, firer, weapon, opponent, 1, out _))
            {
                failure = "legal direct-fire selection";
                return false;
            }

            var observer = new TacticalUnitState("observer", "Observer", new HexCoord(6, 0), 4);
            TacticalAiOrder observe = TacticalEnemyTurn.PlanOrder(board, observer, weapon,
                observer.Position, opponent, 1, 13);
            if (observe.Kind != TacticalAiOrderKind.Observe)
            {
                failure = "observation fallback";
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
                tacticalBattlefield.SuppressionEvents.Count + tacticalBattlefield.ReactionEvents.Count + tacticalBattlefield.EnemyActionEvents.Count;
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
            float maximumDistance = tacticalMode ? 48f : 240f;
            cameraDistance = Mathf.Clamp(cameraDistance - Input.mouseScrollDelta.y * Mathf.Max(1.5f, cameraDistance * .08f), 10f, maximumDistance);
            float horizontal = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1f : 0f) - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f);
            float vertical = (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1f : 0f) - (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1f : 0f);
            Vector3 pan = new Vector3(horizontal, 0f, vertical);
            if (Input.GetMouseButton(2)) pan += new Vector3(-Input.GetAxis("Mouse X") * 3f, 0f, -Input.GetAxis("Mouse Y") * 3f);
            cameraFocus += pan * (cameraDistance * .55f * Time.unscaledDeltaTime);
            if (tacticalMode)
            {
                cameraFocus.x = Mathf.Clamp(cameraFocus.x, -14f, 14f);
                cameraFocus.z = Mathf.Clamp(cameraFocus.z, -11f, 11f);
            }
            if (Input.GetKeyDown(KeyCode.R))
            {
                cameraFocus = tacticalMode ? Vector3.zero : HexToWorld(new HexCoord(Width / 2, Height / 2));
                cameraDistance = tacticalMode ? 33f : 190f;
            }
            ApplyCamera();
        }

        private void ApplyCamera()
        {
            if (mapCamera == null) return;
            if (tacticalMode)
            {
                mapCamera.transform.position = cameraFocus + new Vector3(0f, cameraDistance * 1.08f, -cameraDistance * .56f);
                mapCamera.transform.LookAt(cameraFocus);
                if (tacticalUnit != null) tacticalUnit.transform.localScale = Vector3.one * Mathf.Clamp(cameraDistance / 30f, .82f, 1.55f);
                return;
            }
            float overview = Mathf.InverseLerp(42f, 190f, cameraDistance);
            float height = Mathf.Lerp(1.08f, 1.48f, overview);
            float setback = Mathf.Lerp(.92f, .34f, overview);
            mapCamera.transform.position = cameraFocus + new Vector3(0f, cameraDistance * height, -cameraDistance * setback);
            mapCamera.transform.LookAt(cameraFocus);
            screenPickCacheValid = false;
            if (unit != null) unit.transform.localScale = Vector3.one * Mathf.Clamp(cameraDistance / 52f, 1f, 3.2f);
            UpdateGeographicLabels();
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
                GUI.matrix = Matrix4x4.identity;
                return;
            }

            GUI.Box(new Rect(20f, 18f, 370f, 224f), GUIContent.none);
            GUI.Label(new Rect(38f, 30f, 235f, 30f), "ALWAYS FAITHFUL", titleStyle);
            GUI.Label(new Rect(273f, 34f, 98f, 22f), $"TURN {turnState.TurnNumber}  •  USMC", badgeStyle);
            GUI.Label(new Rect(38f, 61f, 320f, 20f), "TAIWAN 2030  •  WHOLE-ISLAND MAP", badgeStyle);
            GUI.Label(new Rect(38f, 91f, 220f, 25f), unitState.DisplayName.ToUpperInvariant(), unitNameStyle);
            stateStyle.normal.textColor = unitState.Readiness == UnitReadiness.Moving
                ? new Color(.34f, .96f, .82f)
                : unitState.Readiness == UnitReadiness.Spent
                    ? new Color(.48f, .52f, .48f)
                    : new Color(.96f, .73f, .20f);
            GUI.Label(new Rect(275f, 93f, 94f, 21f), unitState.Readiness.ToString().ToUpperInvariant(), stateStyle);

            HexCellView occupied = cells[unitState.Position];
            GUI.Label(new Rect(38f, 120f, 325f, 22f), $"{unitState.Position}  •  {occupied.Terrain}  •  {occupied.ElevationMetres:0} m", bodyStyle);
            GUI.Label(new Rect(38f, 145f, 74f, 22f), "ACTION", badgeStyle);
            DrawActionPointPips(new Rect(105f, 145f, 168f, 20f), unitState);

            string orderPrompt = counterMenuOpen
                ? "Choose a unit order."
                : movePlanning
                    ? hoveredCell != null && reachable.TryGetValue(hoveredCell.Coord, out int moveCost) && !hoveredCell.Coord.Equals(unitState.Position)
                        ? $"LMB confirm {hoveredCell.Coord}  •  Cost {moveCost} AP"
                        : "Hover a highlighted destination."
                    : unitState.CanMove
                        ? "RMB counter for orders."
                        : "Unit spent. End turn to restore AP.";
            GUI.Label(new Rect(38f, 177f, 214f, 32f), orderPrompt, bodyStyle);
            endTurnRect = new Rect(266f, 174f, 104f, 34f);
            GUI.enabled = !unitMoving;
            if (GUI.Button(endTurnRect, "END TURN", buttonStyle)) EndTurn();
            GUI.enabled = true;

            HexCellView inspected = selectedCell != null ? selectedCell : hoveredCell;
            enterTacticalRect = Rect.zero;
            if (inspected != null && !movePlanning)
            {
                float inspectionHeight = inspected.IsLand && selectedCell == inspected ? 110f : 72f;
                GUI.Box(new Rect(20f, 250f, 370f, inspectionHeight), GUIContent.none);
                GUI.Label(new Rect(38f, 259f, 330f, 50f), CellInspectionText(inspected, selectedCell != null ? "SELECTED" : "MAP INSPECT"), bodyStyle);
                if (inspected.IsLand && selectedCell == inspected)
                {
                    enterTacticalRect = new Rect(218f, 320f, 152f, 30f);
                    if (GUI.Button(enterTacticalRect, "OPEN 250 M MAP", buttonStyle)) RequestTacticalMap(inspected);
                }
            }

            GUI.Box(new Rect(uiWidth - 310f, uiHeight - 83f, 288f, 61f), GUIContent.none);
            GUI.Label(new Rect(uiWidth - 294f, uiHeight - 70f, 256f, 45f), "RMB Unit Orders  •  LMB Confirm\nMMB/WASD Pan  •  Wheel Zoom  •  R Reset", bodyStyle);

            if (counterMenuOpen)
            {
                GUI.Box(counterMenuRect, GUIContent.none);
                GUI.Label(new Rect(counterMenuRect.x + 12f, counterMenuRect.y + 7f, 154f, 22f), "USMC RIFLE PLATOON", badgeStyle);
                GUI.enabled = unitState.CanMove;
                string moveLabel = unitState.CanMove ? $"MOVE  •  {unitState.RemainingActionPoints} AP" : "MOVE  •  SPENT";
                if (GUI.Button(new Rect(counterMenuRect.x + 10f, counterMenuRect.y + 34f, 158f, 28f), moveLabel, buttonStyle))
                    BeginMovePlanning();
                GUI.enabled = true;
            }
            DrawTransitionOverlay(uiWidth, uiHeight);
            GUI.matrix = Matrix4x4.identity;
        }

        private void DrawTacticalInterface(float uiWidth, float uiHeight)
        {
            GUI.Box(new Rect(20f, 18f, 405f, 294f), GUIContent.none);
            GUI.Label(new Rect(38f, 30f, 250f, 30f), "ALWAYS FAITHFUL", titleStyle);
            int battleTurn = tacticalObjective != null ? turnState.TurnNumber - tacticalObjective.BattleStartTurn + 1 : turnState.TurnNumber;
            int battleTurnLimit = tacticalObjective?.TurnLimit ?? TacticalVictory.DefaultTurnLimit;
            GUI.Label(new Rect(270f, 34f, 135f, 22f), $"TURN {battleTurn}/{battleTurnLimit} • {turnState.ActiveSide}", badgeStyle);
            GUI.Label(new Rect(38f, 63f, 350f, 24f), ObjectiveStatusText(), badgeStyle);
            GUI.Label(new Rect(38f, 94f, 340f, 25f), tacticalBattlefield.BattlefieldId, unitNameStyle);
            GUI.Label(new Rect(38f, 121f, 350f, 36f),
                $"Parent {tacticalBattlefield.ParentHex}  •  {TacticalWidthKilometres():0.00} × {(tacticalBattlefield.Height * tacticalBattlefield.CellSizeMetres / 1000f):0.00} km  •  Relief {localMinimumLandElevation:0}–{localMaximumLandElevation:0} m\n" +
                $"{tacticalBattlefield.CenterLatitude:0.00000}°N  •  {tacticalBattlefield.CenterLongitude:0.00000}°E", bodyStyle);
            GUI.Label(new Rect(38f, 165f, 220f, 24f), tacticalUnitState.DisplayName.ToUpperInvariant(), unitNameStyle);
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
            GUI.Label(new Rect(324f, 199f, 72f, 22f), $"AMMO {tacticalWeapon.RemainingAmmunition}/{tacticalWeapon.MaximumAmmunition}", badgeStyle);
            GUI.Label(new Rect(38f, 228f, 348f, 26f), tacticalOrderFeedback, bodyStyle);

            Rect tacticalEndTurnRect = new Rect(137f, 259f, 104f, 34f);
            returnToIslandRect = new Rect(244f, 259f, 161f, 34f);
            bool tacticalControlsEnabled = !mapTransitionActive && !tacticalUnitMoving && !tacticalFireResolving && !tacticalEnemyTurnActive;
            GUI.enabled = tacticalControlsEnabled && !resultScreenActive;
            if (GUI.Button(tacticalEndTurnRect, "END TURN", buttonStyle)) EndTacticalTurn();
            GUI.enabled = tacticalControlsEnabled;
            if (GUI.Button(returnToIslandRect, "RETURN TO ISLAND", buttonStyle)) ReturnToIsland(true);
            GUI.enabled = true;

            Rect intelligenceRect = new Rect(uiWidth - 355f, 18f, 335f, 151f + tacticalContacts.Count * 25f);
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
                // +28 over the pre-cover baseline: one line for the cell's own
                // "Cover X" fragment, one for the extra fire/LOS modifier cap below.
                float height = tacticalFirePlanning && tacticalFirePreview != null ? (showTargetStatus ? 208f : 194f) : tacticalLosPlanning && tacticalLosResult != null ? 152f : 104f;
                GUI.Box(new Rect(20f, 321f, 405f, height), GUIContent.none);
                string inspection = CellInspectionText(hoveredLocalCell, tacticalFirePlanning ? "DIRECT FIRE TARGET" : tacticalLosPlanning ? "LOS TARGET" : "LOCAL INSPECT");
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
                GUI.enabled = true;
            }
            GUI.Box(new Rect(uiWidth - 310f, uiHeight - 83f, 288f, 61f), GUIContent.none);
            GUI.Label(new Rect(uiWidth - 294f, uiHeight - 70f, 256f, 45f), "RMB Orders  •  LMB Confirm\nMove / LOS / Fire  •  RMB/Escape Cancel", bodyStyle);
        }

        private static Color TacticalStatusColor(TacticalCombatStatus status)
        {
            switch (status)
            {
                case TacticalCombatStatus.Reduced: return new Color(1f, .38f, .34f);
                case TacticalCombatStatus.Disrupted: return new Color(1f, .62f, .24f);
                case TacticalCombatStatus.Suppressed: return new Color(1f, .84f, .30f);
                default: return new Color(.48f, .78f, .58f);
            }
        }

        private string ObjectiveStatusText()
        {
            if (tacticalObjective == null) return "LOCAL BATTLEFIELD  •  250 M HEXES";
            bool usmcControls = localMovementBoard.TryGetValue(tacticalObjective.ObjectiveHex, out TacticalMovementCell cell) &&
                cell.OccupantId == tacticalUnitState.Id;
            string verb = tacticalObjective.Posture == TacticalPosture.Attack ? "SEIZE" : "HOLD";
            string control = usmcControls ? "USMC" : "CONTESTED";
            return $"{verb} OBJECTIVE {tacticalObjective.ObjectiveHex}  •  {control}";
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
            string summary = $"{tacticalObjective.Posture.ToString().ToUpperInvariant()} • {tacticalBattlefield.BattlefieldId} • Objective {tacticalObjective.ObjectiveHex}\n" +
                $"Turn {battleTurnReached}/{tacticalObjective.TurnLimit} • USMC casualties {usmcCasualties}/1 • PLA casualties {plaCasualties}/{tacticalEnemyStates.Count}\n" +
                tacticalObjective.OutcomeSummary;
            GUI.Label(new Rect(box.x + 30f, box.y + 76f, box.width - 60f, 66f), summary, bodyStyle);

            GUI.Label(new Rect(box.x + 30f, box.y + 148f, box.width - 60f, 20f), "BATTLE LOG", badgeStyle);
            string logText = string.Join("\n", BuildTacticalEventLog(12));
            GUI.Label(new Rect(box.x + 30f, box.y + 170f, box.width - 60f, 230f), logText, bodyStyle);

            Rect dismissRect = new Rect(box.x + box.width / 2f - 90f, box.y + box.height - 50f, 180f, 34f);
            if (GUI.Button(dismissRect, "RETURN TO ISLAND", buttonStyle)) ReturnToIsland(true);
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

        private static float GetUiScale() => Mathf.Clamp(Screen.height / 720f, .90f, 1.35f);

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

        private static Material NewOverlayMaterial(Color color)
        {
            Shader shader = Resources.Load<Shader>("Shaders/MapOverlay") ?? Shader.Find("Sprites/Default");
            return new Material(shader) { color = color };
        }

        private static Color LandColor(float metres)
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
            return Color.Lerp(color, color * .68f, contour * .30f);
        }

        private Color LocalLandColor(float metres)
        {
            float relief = Mathf.InverseLerp(localMinimumLandElevation, Mathf.Max(localMinimumLandElevation + 1f, localMaximumLandElevation), metres);
            Color low = new Color(.18f, .34f, .22f);
            Color high = new Color(.58f, .53f, .38f);
            Color color = Color.Lerp(low, high, relief);
            float contourDistance = Mathf.Abs(Mathf.Repeat(metres + 12.5f, 25f) - 12.5f);
            float contour = 1f - Mathf.SmoothStep(0f, 3.5f, contourDistance);
            return Color.Lerp(color, color * .69f, contour * .34f);
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

        private static Color WaterColor(float metres)
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
            return Color.Lerp(color, color * .70f, contour * .20f);
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

        private static string CellInspectionText(HexCellView cell, string heading)
        {
            string vertical = cell.IsLand ? $"Elevation {cell.ElevationMetres:0} m" : $"Depth {Mathf.Max(0f, -cell.ElevationMetres):0} m";
            string text = $"{heading}  •  Hex {cell.Coord}\n{cell.Latitude:0.0000}°N  •  {cell.Longitude:0.0000}°E\n{cell.Terrain}  •  {vertical}  •  Move {MovementCostLabel(cell.Terrain)}";
            if (cell.Cover != TacticalCover.None)
                text += $"\nCover {cell.Cover}{(cell.IsBuiltUp ? "  •  Built-up" : string.Empty)}";
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
