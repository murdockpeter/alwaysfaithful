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
        private const int Width = 14;
        private const int Height = 10;
        private const float HexRadius = 1f;
        private const float TacticalHexMetres = 250f;
        private const float CellSurfaceOffset = .10f;
        private const float CounterClearance = .04f;
        private const float PathClearance = .18f;

        // A visual-reference crop on the northeast Luzon coastline. The inherited
        // operational datasets are intentionally not authoritative tactical terrain.
        private const double DemoWest = 122.05;
        private const double DemoEast = 122.31;
        private const double DemoSouth = 18.32;
        private const double DemoNorth = 18.55;

        private readonly Dictionary<HexCoord, HexCellView> cells = new Dictionary<HexCoord, HexCellView>();
        private readonly Dictionary<HexCoord, TacticalCell> board = new Dictionary<HexCoord, TacticalCell>();
        private readonly Dictionary<HexCoord, int> reachable = new Dictionary<HexCoord, int>();
        private readonly List<HexCoord> previewPath = new List<HexCoord>();
        private readonly List<GameObject> pathMarkers = new List<GameObject>();
        private Camera mapCamera;
        private UnitCounterView unit;
        private HexCellView selectedCell;
        private HexCellView hoveredCell;
        private Vector3 cameraFocus;
        private float cameraDistance = 18f;
        private GeographicElevationGrid elevation;
        private CoastlineData coastline;
        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private GUIStyle badgeStyle;
        private LineRenderer pathLine;
        private LineRenderer occupiedHexRing;
        private bool unitMoving;
        private bool automatedCapture;
        private bool automatedMovementRegression;
        private bool counterMenuOpen;
        private bool movePlanning;
        private Rect counterMenuRect;

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
            LoadGeography();
            BuildLightingAndCamera();
            BuildCommandTable();
            BuildBoard();
            BuildUnit();
            if (automatedCapture)
            {
                BeginMovePlanning();
                DisplayCapturePath();
            }
            CompleteSmokeTestWhenRequested();
            if (automatedMovementRegression) StartCoroutine(RunMovementRegression());
            StartCoroutine(CaptureScreenshotWhenRequested());
        }

        private void Update()
        {
            if (automatedCapture || automatedMovementRegression) return;
            UpdateCamera();
            UpdatePointer();
        }

        private void LoadGeography()
        {
            TextAsset elevationAsset = Resources.Load<TextAsset>("Geography/luzon-strait-etopo-2022");
            if (!GeographicElevationGrid.TryLoad(elevationAsset, out elevation, out string error)) Debug.LogWarning(error);
            coastline = CoastlineData.Load(Resources.Load<TextAsset>("Geography/luzon-strait-coastline"));
        }

        private void BuildLightingAndCamera()
        {
            RenderSettings.ambientLight = new Color(.42f, .46f, .40f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(.15f, .22f, .23f);
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 22f;
            RenderSettings.fogEndDistance = 45f;

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
            mapCamera.farClipPlane = 100f;
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
            table.transform.SetParent(transform, false);
            table.transform.position = (first + last) * .5f + Vector3.down * .27f;
            table.transform.localScale = new Vector3(last.x - first.x + 4f, .42f, last.z - first.z + 4f);
            table.GetComponent<MeshRenderer>().sharedMaterial = NewMaterial(new Color(.045f, .065f, .062f));
            Destroy(table.GetComponent<Collider>());
        }

        private void BuildBoard()
        {
            for (int q = 0; q < Width; q++)
            {
                for (int r = 0; r < Height; r++)
                {
                    var coord = new HexCoord(q, r);
                    Vector3 center = HexToWorld(coord);
                    double longitude = Mathf.Lerp((float)DemoWest, (float)DemoEast, q / (float)(Width - 1));
                    double latitude = Mathf.Lerp((float)DemoSouth, (float)DemoNorth, r / (float)(Height - 1));
                    float measuredElevation = elevation != null ? elevation.SampleMetres(longitude, latitude) : 0f;
                    bool isLand = coastline == null || coastline.ContainsLand(longitude, latitude);
                    TacticalTerrain terrain = ClassifyTerrain(isLand, measuredElevation);
                    center.y = isLand ? Mathf.Clamp(measuredElevation, 0f, 1800f) * .00032f : 0f;

                    GameObject cellObject = new GameObject("Hex " + coord);
                    cellObject.transform.SetParent(transform, false);
                    cellObject.transform.position = center;
                    Mesh mesh = CreateHexMesh(HexRadius * .965f, .10f);
                    cellObject.AddComponent<MeshFilter>().sharedMesh = mesh;
                    var renderer = cellObject.AddComponent<MeshRenderer>();
                    Material material = NewMaterial(isLand ? LandColor(measuredElevation) : WaterColor(measuredElevation));
                    renderer.sharedMaterial = material;
                    cellObject.AddComponent<MeshCollider>().sharedMesh = mesh;
                    HexCellView view = cellObject.AddComponent<HexCellView>();
                    view.Initialize(coord, terrain, measuredElevation, material, material.color);
                    cells.Add(coord, view);
                    board.Add(coord, new TacticalCell(coord, terrain));
                }
            }
            BuildCoastAccents();
            BuildPathLine();
            BuildOccupiedHexRing();
        }

        private void BuildUnit()
        {
            HexCoord start = FindDeploymentHex(new HexCoord(Width / 2, Height / 3));
            Vector3 position = HexToWorld(start);
            if (cells.TryGetValue(start, out HexCellView cell)) position.y = cell.transform.position.y;

            GameObject counterRoot = new GameObject("USMC Rifle Platoon");
            counterRoot.transform.SetParent(transform, false);
            // The counter is physically anchored to its hex; the overlay shader, rather than
            // a large altitude offset, guarantees that terrain cannot hide critical state.
            counterRoot.transform.position = position + Vector3.up * (CellSurfaceOffset + CounterClearance);
            SphereCollider counterCollider = counterRoot.AddComponent<SphereCollider>();
            counterCollider.radius = .65f;
            counterCollider.center = Vector3.up * .12f;
            unit = counterRoot.AddComponent<UnitCounterView>();
            unit.Initialize("USMC Rifle Platoon", start);
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
            if (cells.Count != Width * Height || board.Count != Width * Height || unit == null || elevation == null || coastline == null)
            {
                Debug.LogError($"ALWAYS_FAITHFUL_SMOKE_FAILED cells={cells.Count}, board={board.Count}, unit={unit != null}, elevation={elevation != null}, coastline={coastline != null}");
                Application.Quit(1);
                return;
            }
            if (unit.IsSelected || movePlanning || counterMenuOpen || reachable.Count != 0)
            {
                Debug.LogError("ALWAYS_FAITHFUL_SMOKE_FAILED prototype did not start neutral");
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
            if (reachable.Count < 2 || MovementPlanner.FindPath(board, unit.Position, unit.Position, PlatoonMovementPoints).Count != 1)
            {
                Debug.LogError($"ALWAYS_FAITHFUL_SMOKE_FAILED movement reachable={reachable.Count}");
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
            Debug.Log($"ALWAYS_FAITHFUL_SMOKE_OK cells={cells.Count}, reachable={reachable.Count}, pathMarkers={pathMarkers.Count}, unit={unit.UnitName}, hex={unit.Position}, scale={TacticalHexMetres:0}m");
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

        private void UpdatePointer()
        {
            // Do not let hover bookkeeping clear the committed route while its
            // movement coroutine is animating the counter.
            if (mapCamera == null || unitMoving) return;
            Vector2 guiPointer = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            if (counterMenuOpen && counterMenuRect.Contains(guiPointer)) return;
            bool rightClick = Input.GetMouseButtonDown(1);
            Ray ray = mapCamera.ScreenPointToRay(Input.mousePosition);
            HexCellView nextHover = null;
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                UnitCounterView pointedUnit = hit.collider.GetComponentInParent<UnitCounterView>();
                nextHover = hit.collider.GetComponentInParent<HexCellView>();
                if (Input.GetMouseButtonDown(0))
                {
                    if (movePlanning && pointedUnit == null && nextHover != null && TryIssueMove(nextHover.Coord)) return;
                    if (pointedUnit == null && nextHover != null) SelectCell(nextHover);
                }
                if (rightClick)
                {
                    string target = pointedUnit != null ? unit.UnitName : nextHover != null ? nextHover.Coord.ToString() : hit.collider.name;
                    Debug.Log($"ALWAYS_FAITHFUL_RMB target={target} selected={unit.IsSelected}");
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

        private bool TryIssueMove(HexCoord destination)
        {
            if (unitMoving || !movePlanning || !unit.IsSelected || !reachable.ContainsKey(destination)) return false;
            List<HexCoord> path = MovementPlanner.FindPath(board, unit.Position, destination, PlatoonMovementPoints);
            if (path.Count < 2) return false;
            Debug.Log($"ALWAYS_FAITHFUL_MOVE_ACCEPTED from={unit.Position} to={destination} steps={path.Count - 1}");
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
            unit.SetSelected(true);
            counterMenuRect = new Rect(
                Mathf.Clamp(pointer.x, 8f, Screen.width - 190f),
                Mathf.Clamp(pointer.y, 8f, Screen.height - 90f),
                178f,
                72f);
            counterMenuOpen = true;
        }

        private void BeginMovePlanning()
        {
            counterMenuOpen = false;
            movePlanning = true;
            unit.SetSelected(true);
            RefreshReachable();
        }

        private void CancelUnitInteraction()
        {
            counterMenuOpen = false;
            movePlanning = false;
            unit.SetSelected(false);
            ClearReachable();
            ClearPreviewPath();
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
            foreach (KeyValuePair<HexCoord, int> pair in MovementPlanner.Reachable(board, unit.Position, PlatoonMovementPoints))
            {
                reachable[pair.Key] = pair.Value;
                if (!pair.Key.Equals(unit.Position) && cells.TryGetValue(pair.Key, out HexCellView cell)) cell.SetReachable(true);
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
            if (destination == null || !unit.IsSelected || !reachable.ContainsKey(destination.Coord)) return;
            DisplayMovementPath(MovementPlanner.FindPath(board, unit.Position, destination.Coord, PlatoonMovementPoints));
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
            unit.SetPosition(destination);
            UpdateOccupiedHexRing(destination);
            yield return new WaitForSeconds(.18f);
            ClearPreviewPath();
            unitMoving = false;
            movePlanning = false;
            unit.SetSelected(false);
            Debug.Log($"ALWAYS_FAITHFUL_MOVE_COMPLETED hex={unit.Position} world={unit.transform.position}");
        }

        private IEnumerator RunMovementRegression()
        {
            // Exercise the same menu -> Move mode -> destination order path used
            // by the player, then prove model and rendered counter both changed.
            yield return null;
            if (unit.IsSelected || movePlanning || counterMenuOpen || reachable.Count != 0)
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
            HexCoord origin = unit.Position;
            Vector3 originWorld = unit.transform.position;
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
            if (unitMoving || movePlanning || unit.IsSelected || !routePreserved || !unit.Position.Equals(destination) || travelled < .5f)
            {
                Debug.LogError($"ALWAYS_FAITHFUL_MOVEMENT_REGRESSION_FAILED completion moving={unitMoving} planning={movePlanning} selected={unit.IsSelected} routePreserved={routePreserved} origin={origin} actual={unit.Position} expected={destination} travelled={travelled:0.000}");
                Application.Quit(1);
                yield break;
            }
            Debug.Log($"ALWAYS_FAITHFUL_MOVEMENT_REGRESSION_OK from={origin} to={destination} travelled={travelled:0.000}");
            Application.Quit(0);
        }

        private void DisplayCapturePath()
        {
            HexCoord destination = unit.Position;
            int greatestCost = -1;
            foreach (KeyValuePair<HexCoord, int> pair in reachable)
            {
                if (pair.Value < greatestCost) continue;
                if (pair.Value == greatestCost && (pair.Key.Q < destination.Q || pair.Key.Q == destination.Q && pair.Key.R <= destination.R)) continue;
                destination = pair.Key;
                greatestCost = pair.Value;
            }
            DisplayMovementPath(MovementPlanner.FindPath(board, unit.Position, destination, PlatoonMovementPoints));
        }

        private void CreatePathMarker(Vector3 position, bool destination)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = destination ? "Movement Destination" : "Movement Waypoint";
            marker.layer = LayerMask.NameToLayer("Ignore Raycast");
            marker.transform.SetParent(transform, false);
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
            cameraDistance = Mathf.Clamp(cameraDistance - Input.mouseScrollDelta.y * 1.5f, 8f, 28f);
            float horizontal = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1f : 0f) - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f);
            float vertical = (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1f : 0f) - (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1f : 0f);
            Vector3 pan = new Vector3(horizontal, 0f, vertical);
            if (Input.GetMouseButton(2)) pan += new Vector3(-Input.GetAxis("Mouse X") * 3f, 0f, -Input.GetAxis("Mouse Y") * 3f);
            cameraFocus += pan * (cameraDistance * .55f * Time.unscaledDeltaTime);
            if (Input.GetKeyDown(KeyCode.R))
            {
                cameraFocus = HexToWorld(new HexCoord(Width / 2, Height / 2));
                cameraDistance = 18f;
            }
            ApplyCamera();
        }

        private void ApplyCamera()
        {
            if (mapCamera == null) return;
            mapCamera.transform.position = cameraFocus + new Vector3(0f, cameraDistance * 1.08f, -cameraDistance * .92f);
            mapCamera.transform.LookAt(cameraFocus);
        }

        private void OnGUI()
        {
            EnsureStyles();
            GUI.Box(new Rect(22f, 20f, 330f, 150f), GUIContent.none);
            GUI.Label(new Rect(40f, 34f, 290f, 32f), "ALWAYS FAITHFUL", titleStyle);
            GUI.Label(new Rect(40f, 66f, 290f, 24f), "2030 TACTICAL INTERACTION SPIKE", badgeStyle);
            string selection = counterMenuOpen
                ? "UNIT ORDERS  •  USMC Rifle Platoon\nChoose an order from the counter menu."
                : movePlanning
                ? "MOVE ORDER  •  USMC Rifle Platoon\nHex: " + unit.Position + "  •  4 AP  •  " + TacticalHexMetres + " m/hex" +
                  (hoveredCell != null && reachable.TryGetValue(hoveredCell.Coord, out int moveCost) && !hoveredCell.Coord.Equals(unit.Position)
                      ? "\nLMB CONFIRM  •  Cost " + moveCost + " AP"
                      : "\nHover a highlighted hex; LMB confirms")
                : selectedCell != null
                    ? $"SELECTED  •  Hex {selectedCell.Coord}\n{selectedCell.Terrain}  •  Move {MovementCostLabel(selectedCell.Terrain)}  •  ETOPO preview {selectedCell.ElevationMetres:0} m"
                    : "No unit selected.\nRMB the counter to open unit orders.";
            GUI.Label(new Rect(40f, 98f, 290f, 58f), selection, bodyStyle);

            GUI.Box(new Rect(Screen.width - 310f, Screen.height - 83f, 288f, 61f), GUIContent.none);
            GUI.Label(new Rect(Screen.width - 294f, Screen.height - 70f, 256f, 45f), "RMB Unit Orders  •  LMB Confirm\nMMB/WASD Pan  •  Wheel Zoom  •  R Reset", bodyStyle);

            if (counterMenuOpen)
            {
                GUI.Box(counterMenuRect, GUIContent.none);
                GUI.Label(new Rect(counterMenuRect.x + 12f, counterMenuRect.y + 7f, 154f, 22f), "USMC RIFLE PLATOON", badgeStyle);
                if (GUI.Button(new Rect(counterMenuRect.x + 10f, counterMenuRect.y + 34f, 158f, 28f), "MOVE  •  4 AP"))
                    BeginMovePlanning();
            }
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
        }

        private static Vector3 HexToWorld(HexCoord hex)
            => new Vector3(hex.Q * HexRadius * 1.5f, 0f, (hex.R + (hex.Q & 1) * .5f) * HexRadius * Mathf.Sqrt(3f));

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
                triangles.Add(2 + index * 2);
                triangles.Add(2 + next * 2);
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
            Shader shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Sprites/Default");
            var material = new Material(shader) { color = color };
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", .08f);
            return material;
        }

        private static Material NewOverlayMaterial(Color color)
        {
            Shader shader = Resources.Load<Shader>("Shaders/MapOverlay") ?? Shader.Find("Sprites/Default");
            return new Material(shader) { color = color };
        }

        private static Color LandColor(float metres)
        {
            float height = Mathf.InverseLerp(0f, 1600f, Mathf.Max(0f, metres));
            return Color.Lerp(new Color(.26f, .34f, .22f), new Color(.43f, .42f, .31f), height);
        }

        private static Color WaterColor(float metres)
        {
            float depth = Mathf.InverseLerp(0f, -3500f, Mathf.Min(0f, metres));
            return Color.Lerp(new Color(.12f, .33f, .35f), new Color(.055f, .16f, .22f), depth);
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

        private void BuildPathLine()
        {
            GameObject lineObject = new GameObject("Movement Path Preview");
            lineObject.transform.SetParent(transform, false);
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
            ringObject.transform.SetParent(transform, false);
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
            foreach (KeyValuePair<HexCoord, HexCellView> pair in cells)
            {
                if (!pair.Value.IsLand) continue;
                bool coastal = false;
                foreach (HexCoord neighbor in MovementPlanner.Neighbors(pair.Key))
                    if (cells.TryGetValue(neighbor, out HexCellView adjacent) && !adjacent.IsLand) { coastal = true; break; }
                if (!coastal) continue;
                GameObject accent = new GameObject("Coast Accent " + pair.Key);
                accent.transform.SetParent(transform, false);
                LineRenderer line = accent.AddComponent<LineRenderer>();
                line.loop = true;
                line.positionCount = 6;
                line.widthMultiplier = .035f;
                line.material = new Material(Shader.Find("Sprites/Default"));
                line.startColor = new Color(.52f, .85f, .72f, .58f);
                line.endColor = line.startColor;
                for (int index = 0; index < 6; index++)
                {
                    float angle = index * Mathf.PI / 3f;
                    line.SetPosition(index, pair.Value.transform.position + new Vector3(Mathf.Cos(angle) * .96f, .135f, Mathf.Sin(angle) * .96f));
                }
            }
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
