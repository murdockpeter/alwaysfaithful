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
        private Camera mapCamera;
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
        private Vector3 overviewCameraFocus;
        private float overviewCameraDistance;
        private float localMinimumLandElevation;
        private float localMaximumLandElevation;
        private bool tacticalMode;
        private bool mapTransitionActive;
        private float mapTransitionOpacity;
        private bool automatedTacticalRegression;
        private bool automatedTacticalCapture;
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
            if (automatedTacticalCapture)
            {
                EnterTacticalMap(FindCoastalOperationalCell(), false);
                StartCoroutine(CaptureTacticalScreenshotWhenRequested());
            }
            StartCoroutine(CaptureScreenshotWhenRequested());
        }

        private void Update()
        {
            if (automatedCapture || automatedMovementRegression || automatedTacticalRegression || automatedTacticalCapture || mapTransitionActive) return;
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

        private void RequestTacticalMap(HexCellView parentCell)
        {
            if (parentCell == null || !parentCell.IsLand || mapTransitionActive) return;
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
            if (!tacticalMode || mapTransitionActive && animate) return;
            if (animate)
            {
                StartCoroutine(TransitionToOverview());
                return;
            }
            if (hoveredLocalCell != null) hoveredLocalCell.SetHighlighted(false);
            hoveredLocalCell = null;
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
                HexCellView view = cellObject.AddComponent<HexCellView>();
                view.Initialize(source.LocalCoord, source.Terrain, source.ElevationMetres, source.Longitude, source.Latitude, renderer, color);
                localCells.Add(source.LocalCoord, view);
            }
            BuildTacticalTable();
            BuildTacticalShoreline();
            BuildTacticalReferenceMarks();
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
            HexCoord centerCoord = new HexCoord(tacticalBattlefield.Width / 2, tacticalBattlefield.Height / 2);
            HexCellView centerCell = localCells[centerCoord];
            GameObject centerObject = new GameObject("Parent Hex Center");
            centerObject.layer = LayerMask.NameToLayer("Ignore Raycast");
            centerObject.transform.SetParent(tacticalRoot.transform, false);
            LineRenderer ring = centerObject.AddComponent<LineRenderer>();
            ring.loop = true;
            ring.useWorldSpace = true;
            ring.positionCount = 48;
            ring.widthMultiplier = .075f;
            ring.material = NewOverlayMaterial(new Color(.98f, .73f, .19f, .92f));
            ring.startColor = new Color(.98f, .73f, .19f, .92f);
            ring.endColor = ring.startColor;
            for (int index = 0; index < ring.positionCount; index++)
            {
                float angle = index / (float)ring.positionCount * Mathf.PI * 2f;
                ring.SetPosition(index, centerCell.transform.position + new Vector3(Mathf.Cos(angle) * .54f, CellSurfaceOffset + .09f, Mathf.Sin(angle) * .54f));
            }

            GameObject northObject = new GameObject("North Reference");
            northObject.transform.SetParent(tacticalRoot.transform, false);
            HexCoord northCoord = new HexCoord(tacticalBattlefield.Width - 2, tacticalBattlefield.Height - 2);
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

        private void UpdateTacticalPointer()
        {
            Vector2 guiPointer = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y) / GetUiScale();
            if (returnToIslandRect.Contains(guiPointer)) return;
            HexCellView next = PickLocalHexAtScreenPoint(Input.mousePosition);
            if (next == hoveredLocalCell) return;
            if (hoveredLocalCell != null) hoveredLocalCell.SetHighlighted(false);
            hoveredLocalCell = next;
            if (hoveredLocalCell != null) hoveredLocalCell.SetHighlighted(true);
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
                    if (pointedUnit == null && nextHover != null) SelectCell(nextHover);
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
        }

        private void BeginMovePlanning()
        {
            if (!unitState.CanMove) return;
            counterMenuOpen = false;
            movePlanning = true;
            unitState.IsSelected = true;
            unit.Present(unitState);
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
            unit.Present(unitState);
            Debug.Log($"ALWAYS_FAITHFUL_TURN_STARTED turn={turnState.TurnNumber} side={turnState.ActiveSide} ap={unitState.RemainingActionPoints}/{unitState.MaximumActionPoints}");
        }

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
                    a.Latitude != b.Latitude || a.ElevationMetres != b.ElevationMetres || a.Terrain != b.Terrain)
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
            DrawActionPointPips(new Rect(105f, 145f, 168f, 20f));

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
            GUI.Box(new Rect(20f, 18f, 405f, 244f), GUIContent.none);
            GUI.Label(new Rect(38f, 30f, 250f, 30f), "ALWAYS FAITHFUL", titleStyle);
            GUI.Label(new Rect(287f, 34f, 118f, 22f), "TACTICAL LAYER", badgeStyle);
            GUI.Label(new Rect(38f, 63f, 350f, 24f), "LOCAL BATTLEFIELD  •  250 M HEXES", badgeStyle);
            GUI.Label(new Rect(38f, 94f, 340f, 25f), tacticalBattlefield.BattlefieldId, unitNameStyle);
            GUI.Label(new Rect(38f, 124f, 340f, 40f),
                $"Parent hex {tacticalBattlefield.ParentHex}  •  {tacticalBattlefield.Cells.Count} cells\n" +
                $"Center {tacticalBattlefield.CenterLatitude:0.00000}°N  •  {tacticalBattlefield.CenterLongitude:0.00000}°E", bodyStyle);
            GUI.Label(new Rect(38f, 170f, 340f, 34f),
                $"Area {TacticalWidthKilometres():0.00} × {(tacticalBattlefield.Height * tacticalBattlefield.CellSizeMetres / 1000f):0.00} km  •  Relief {localMinimumLandElevation:0}–{localMaximumLandElevation:0} m", bodyStyle);
            returnToIslandRect = new Rect(244f, 207f, 161f, 34f);
            GUI.enabled = !mapTransitionActive;
            if (GUI.Button(returnToIslandRect, "RETURN TO ISLAND", buttonStyle)) ReturnToIsland(true);
            GUI.enabled = true;

            if (hoveredLocalCell != null)
            {
                GUI.Box(new Rect(20f, 272f, 405f, 76f), GUIContent.none);
                GUI.Label(new Rect(38f, 282f, 360f, 54f), CellInspectionText(hoveredLocalCell, "LOCAL INSPECT"), bodyStyle);
            }
            GUI.Box(new Rect(uiWidth - 310f, uiHeight - 83f, 288f, 61f), GUIContent.none);
            GUI.Label(new Rect(uiWidth - 294f, uiHeight - 70f, 256f, 45f), "Hover Hex  •  Return to Island\nMMB/WASD Pan  •  Wheel Zoom  •  R Reset", bodyStyle);
        }

        private void DrawTransitionOverlay(float uiWidth, float uiHeight)
        {
            if (mapTransitionOpacity <= .001f) return;
            Color previous = GUI.color;
            GUI.color = new Color(.025f, .055f, .052f, mapTransitionOpacity);
            GUI.DrawTexture(new Rect(0f, 0f, uiWidth, uiHeight), Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private void DrawActionPointPips(Rect area)
        {
            const float gap = 5f;
            float width = (area.width - gap * (unitState.MaximumActionPoints - 1)) / unitState.MaximumActionPoints;
            for (int index = 0; index < unitState.MaximumActionPoints; index++)
            {
                Color previous = GUI.color;
                GUI.color = index < unitState.RemainingActionPoints
                    ? new Color(.96f, .73f, .20f, 1f)
                    : new Color(.19f, .24f, .22f, 1f);
                GUI.Box(new Rect(area.x + index * (width + gap), area.y, width, area.height), GUIContent.none);
                GUI.color = previous;
            }
            GUI.Label(new Rect(area.x + area.width + 9f, area.y - 1f, 44f, 22f), $"{unitState.RemainingActionPoints}/{unitState.MaximumActionPoints}", bodyStyle);
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
            return $"{heading}  •  Hex {cell.Coord}\n{cell.Latitude:0.0000}°N  •  {cell.Longitude:0.0000}°E\n{cell.Terrain}  •  {vertical}  •  Move {MovementCostLabel(cell.Terrain)}";
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
