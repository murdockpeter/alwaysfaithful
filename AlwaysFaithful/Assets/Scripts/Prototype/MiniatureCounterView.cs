using AlwaysFaithful.Core;
using UnityEngine;

namespace AlwaysFaithful.Prototype
{
    // Third counter skin: sculpted USMC/PLANMC miniature figures (OBJ models
    // under Assets/Resources/Models/OneStar — © Nicholas Royer / Down Range,
    // CC BY-NC-SA 4.0, credited in NOTICE.md alongside them) instead of
    // procedurally built primitives. Unlike every other piece in this game,
    // these are lit by the scene's existing directional lights via Unity's
    // Standard shader rather than a flat/unlit overlay shader.
    public sealed class MiniatureCounterView : MonoBehaviour
    {
        // Native models are ~2 world units tall (a standing human figure).
        // A rifle role is represented by a small cluster of this same
        // figure — matching TacticalFormationView's own three-element wedge
        // convention, and how a real tabletop stand usually mounts several
        // identical sculpts rather than one giant soldier — while a support
        // role uses its single Mortar Team model as-is, since that sculpt
        // is already a multi-figure crew, not a lone soldier. Recon gets
        // its own vehicle-plus-figures vignette (see BuildReconVignette)
        // rather than either convention. Tuned empirically against a
        // capture, not derived from any real-world scale — a wargame
        // counter convention, not a diorama.
        //
        // Spread widely enough to read as a small tabletop vignette rather
        // than a tight clump — deliberately allowed to overhang the hex a
        // little, matching how the NATO billboard and cluster footprint
        // already read a bit larger than the hex itself at this game's
        // "counter," not literal, scale.
        private const float ClusterMemberScale = .34f;
        private const float SoloModelScale = .62f;

        private static readonly Vector3[] ClusterOffsets =
        {
            new Vector3(-.52f, 0f, -.34f),
            new Vector3(.40f, 0f, -.50f),
            new Vector3(-.20f, 0f, .46f),
            new Vector3(.50f, 0f, .18f)
        };
        private static readonly float[] ClusterYaw = { 168f, 194f, 152f, 208f };

        // These role-specific teams use the same figures and pairings already
        // established by Down Range's One Star digital scenario. Weapons gets
        // the source game's MAAWS/M249 team; engineers get a technical command
        // element built around its EW specialist and officer sculpts.
        private static readonly string[] WeaponsModelPaths =
        {
            "Models/OneStar/USMC M249 Gunner",
            "Models/OneStar/USMC MAAWS Gunner",
            "Models/OneStar/USMC M249 Gunner",
            "Models/OneStar/USMC MAAWS Gunner"
        };
        private static readonly string[] EngineerModelPaths =
        {
            "Models/OneStar/USMC EW Operator",
            "Models/OneStar/USMC Officer",
            "Models/OneStar/USMC Rifleman",
            "Models/OneStar/USMC Rifleman"
        };
        // Recon reads as a scouting tableau rather than a massed squad: the
        // ACV-30 recon vehicle plus two dismounted figures, deliberately
        // NOT placed on the mirrored 4-point ClusterOffsets grid every other
        // role uses — staggered at uneven distances/facings off one
        // another so it reads as a candid vignette (vehicle overwatch,
        // leader dismounted, sensor operator working forward) instead of a
        // formation.
        // Native mesh is ~9.65 units long (vs. a Rifleman's 2.26) — nearly
        // identical native size to PLANMC ZBL-09 (8.35 units), which stays
        // hex-sized as a background prop at VehicleVignetteScale (.155).
        // This one's meant to be the vignette's focal point rather than set
        // dressing, so it runs a bit larger, but the first pass (.55) was
        // sized as if the mesh matched a human figure's scale and came out
        // spanning three-plus hexes.
        private const string ReconVehiclePath = "Models/OneStar/USMC ACV-30";
        private const float ReconVehicleScale = .19f;
        private static readonly Vector3 ReconVehicleOffset = new Vector3(-.28f, 0f, .32f);
        private const float ReconVehicleYaw = 200f;
        private const string ReconLeaderPath = "Models/OneStar/USMC Officer";
        private static readonly Vector3 ReconLeaderOffset = new Vector3(.46f, 0f, -.20f);
        private const float ReconLeaderYaw = 255f;
        private const string ReconObserverPath = "Models/OneStar/USMC EW Operator";
        private static readonly Vector3 ReconObserverOffset = new Vector3(.10f, 0f, -.54f);
        private const float ReconObserverYaw = 300f;
        private const string UsmcFieldCamoPath = "Models/OneStar/USMC Field Camo Texture";

        // USMC has no ground vehicle in the source pack, only a drone. Every
        // identified PLANMC miniature vignette carries a ZBL-09 so the force
        // reads as mechanized even when a generated roster contains rifle
        // squads but no support team. The vehicle is counter dressing, not
        // an independently targetable gameplay unit.
        private const string UsmcVignettePath = "Models/OneStar/USMC Black Hornet";
        private const string PlaSupportVignettePath = "Models/OneStar/PLANMC ZBL-09";
        private const float DroneVignetteScale = .17f;
        private const float VehicleVignetteScale = .155f;
        private static readonly Vector3 DroneVignetteOffset = new Vector3(.10f, .58f, -.08f);
        private static readonly Vector3 VehicleVignetteOffset = new Vector3(.58f, 0f, -.62f);

        // Slightly above white: this scene's two directional lights plus a
        // fully matte (zero-glossiness) material still rendered these
        // painted textures darker than expected at full brightness.
        public static readonly Color FullBrightTint = new Color(1.25f, 1.25f, 1.25f);

        private Renderer[] renderers;
        private GameObject statusBadge;
        private MeshRenderer statusBadgeRenderer;
        private bool loaded;
        private TacticalUnitState boundUnit;
        private Color flashTint;
        private float flashTimer;

        public TacticalCombatStatus PresentedStatus { get; private set; }

        public static bool HasModelFor(bool friendly, bool isSupportRole)
            => Resources.Load<GameObject>(ModelPathFor(friendly, isSupportRole)) != null;

        public static bool HasModelsFor(TacticalPlatoonRole role)
        {
            if (role == TacticalPlatoonRole.Reconnaissance)
                return Resources.Load<GameObject>(ReconVehiclePath) != null &&
                    Resources.Load<GameObject>(ReconLeaderPath) != null &&
                    Resources.Load<GameObject>(ReconObserverPath) != null &&
                    Resources.Load<Texture2D>(UsmcFieldCamoPath) != null &&
                    Resources.Load<Shader>("Shaders/OneStarTriplanarPaint") != null;
            string[] paths = ClusterModelPathsFor(role);
            if (paths == null) return HasModelFor(true, false);
            foreach (string path in paths)
                if (Resources.Load<GameObject>(path) == null) return false;
            bool needsFieldCamo = false;
            foreach (string path in paths)
                if (path.EndsWith("USMC EW Operator")) needsFieldCamo = true;
            return !needsFieldCamo ||
                Resources.Load<Texture2D>(UsmcFieldCamoPath) != null && Resources.Load<Shader>("Shaders/OneStarTriplanarPaint") != null;
        }

        private static string[] ClusterModelPathsFor(TacticalPlatoonRole role)
            => role == TacticalPlatoonRole.Weapons ? WeaponsModelPaths
                : role == TacticalPlatoonRole.Engineers ? EngineerModelPaths
                : null;

        private static string ModelPathFor(bool friendly, bool isSupportRole)
        {
            if (friendly) return "Models/OneStar/USMC Rifleman";
            return isSupportRole ? "Models/OneStar/PLANMC Mortar Team" : "Models/OneStar/PLANMC Rifleman";
        }

        public void Initialize(bool friendly, bool isSupportRole, string labelText,
            TacticalPlatoonRole friendlyRole = TacticalPlatoonRole.Rifle)
        {
            string path = ModelPathFor(friendly, isSupportRole);
            GameObject prefab = Resources.Load<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning($"ALWAYS_FAITHFUL_MINIATURE_MISSING {path}");
                return;
            }
            Texture2D texture = Resources.Load<Texture2D>(path + " Texture");
            Shader litShader = Shader.Find("Standard") ?? Shader.Find("Diffuse");
            Shader triplanarShader = Resources.Load<Shader>("Shaders/OneStarTriplanarPaint");

            GameObject clusterRoot = new GameObject("Miniature Cluster");
            clusterRoot.transform.SetParent(transform, false);
            float badgeHeight;
            string[] modelPaths = friendly ? ClusterModelPathsFor(friendlyRole) : null;
            if (modelPaths != null)
            {
                for (int index = 0; index < modelPaths.Length; index++)
                {
                    string modelPath = modelPaths[index];
                    GameObject rolePrefab = Resources.Load<GameObject>(modelPath);
                    if (rolePrefab == null)
                    {
                        Debug.LogWarning($"ALWAYS_FAITHFUL_MINIATURE_MISSING {modelPath}");
                        continue;
                    }
                    bool generatedPaint = modelPath.EndsWith("USMC EW Operator");
                    Texture2D roleTexture = Resources.Load<Texture2D>(generatedPaint ? UsmcFieldCamoPath : modelPath + " Texture");
                    SpawnFigure(rolePrefab, clusterRoot.transform, ClusterOffsets[index], ClusterYaw[index],
                        ClusterMemberScale, roleTexture, generatedPaint && triplanarShader != null ? triplanarShader : litShader,
                        generatedPaint);
                }
                badgeHeight = ClusterMemberScale * 1.15f;
            }
            else if (friendly && friendlyRole == TacticalPlatoonRole.Reconnaissance)
            {
                badgeHeight = BuildReconVignette(clusterRoot.transform, litShader, triplanarShader);
            }
            else if (isSupportRole)
            {
                SpawnFigure(prefab, clusterRoot.transform, Vector3.zero, 180f, SoloModelScale, texture, litShader);
                badgeHeight = SoloModelScale * 1.05f;
            }
            else
            {
                for (int index = 0; index < ClusterOffsets.Length; index++)
                    SpawnFigure(prefab, clusterRoot.transform, ClusterOffsets[index], ClusterYaw[index], ClusterMemberScale, texture, litShader);
                badgeHeight = ClusterMemberScale * 1.15f;
            }

            // Vignette prop: a drone hovering over the USMC scene, or a
            // vehicle beside the PLA support crew — set dressing for the
            // "small tabletop scene" look, not a separate gameplay unit.
            string vignettePath = friendly ? UsmcVignettePath : PlaSupportVignettePath;
            if (vignettePath != null)
            {
                GameObject vignettePrefab = Resources.Load<GameObject>(vignettePath);
                if (vignettePrefab != null)
                {
                    Texture2D vignetteTexture = Resources.Load<Texture2D>(vignettePath + " Texture");
                    bool isDrone = friendly;
                    SpawnFigure(vignettePrefab, clusterRoot.transform,
                        isDrone ? DroneVignetteOffset : VehicleVignetteOffset,
                        isDrone ? 40f : 205f,
                        isDrone ? DroneVignetteScale : VehicleVignetteScale,
                        vignetteTexture, litShader);
                }
                else
                {
                    Debug.LogWarning($"ALWAYS_FAITHFUL_MINIATURE_MISSING {vignettePath}");
                }
            }

            renderers = clusterRoot.GetComponentsInChildren<Renderer>();
            loaded = true;

            Shader overlay = Resources.Load<Shader>("Shaders/MapOverlay") ?? Shader.Find("Sprites/Default");
            statusBadge = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            statusBadge.name = "Miniature Status Badge";
            statusBadge.layer = LayerMask.NameToLayer("Ignore Raycast");
            statusBadge.transform.SetParent(transform, false);
            statusBadge.transform.localPosition = new Vector3(0f, badgeHeight, .10f);
            statusBadge.transform.localScale = Vector3.one * .13f;
            statusBadgeRenderer = statusBadge.GetComponent<MeshRenderer>();
            statusBadgeRenderer.sharedMaterial = new Material(overlay);
            Destroy(statusBadge.GetComponent<Collider>());
            statusBadge.SetActive(false);
        }

        private static float BuildReconVignette(Transform clusterRoot, Shader litShader, Shader triplanarShader)
        {
            GameObject vehiclePrefab = Resources.Load<GameObject>(ReconVehiclePath);
            if (vehiclePrefab != null)
            {
                Texture2D vehicleTexture = Resources.Load<Texture2D>(ReconVehiclePath + " Texture");
                SpawnFigure(vehiclePrefab, clusterRoot, ReconVehicleOffset, ReconVehicleYaw, ReconVehicleScale, vehicleTexture, litShader);
            }
            else
            {
                Debug.LogWarning($"ALWAYS_FAITHFUL_MINIATURE_MISSING {ReconVehiclePath}");
            }

            GameObject leaderPrefab = Resources.Load<GameObject>(ReconLeaderPath);
            if (leaderPrefab != null)
            {
                Texture2D leaderTexture = Resources.Load<Texture2D>(ReconLeaderPath + " Texture");
                SpawnFigure(leaderPrefab, clusterRoot, ReconLeaderOffset, ReconLeaderYaw, ClusterMemberScale, leaderTexture, litShader);
            }
            else
            {
                Debug.LogWarning($"ALWAYS_FAITHFUL_MINIATURE_MISSING {ReconLeaderPath}");
            }

            // The EW Operator sculpt has no baked texture of its own (see
            // the Engineers cluster above) — it's always painted with the
            // generated field-camo triplanar material instead.
            GameObject observerPrefab = Resources.Load<GameObject>(ReconObserverPath);
            if (observerPrefab != null)
            {
                Texture2D observerTexture = Resources.Load<Texture2D>(UsmcFieldCamoPath);
                SpawnFigure(observerPrefab, clusterRoot, ReconObserverOffset, ReconObserverYaw, ClusterMemberScale,
                    observerTexture, triplanarShader != null ? triplanarShader : litShader, true);
            }
            else
            {
                Debug.LogWarning($"ALWAYS_FAITHFUL_MINIATURE_MISSING {ReconObserverPath}");
            }

            return ClusterMemberScale * 1.15f;
        }

        private static void SpawnFigure(GameObject prefab, Transform parent, Vector3 localOffset, float yaw, float scale,
            Texture2D texture, Shader litShader, bool triplanarPaint = false)
        {
            GameObject instance = Instantiate(prefab, parent);
            instance.name = prefab.name + " Miniature Figure";
            instance.transform.localPosition = localOffset;
            instance.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            instance.transform.localScale = Vector3.one * scale;
            foreach (Collider stale in instance.GetComponentsInChildren<Collider>()) Destroy(stale);
            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>())
            {
                Material material = new Material(litShader);
                if (texture != null) material.mainTexture = texture;
                // Matte, non-specular response: painted-miniature look under
                // this scene's two directional lights, and noticeably
                // brighter than the Standard shader's glossy default, which
                // read almost silhouette-dark at the default 0.5 smoothness.
                if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0f);
                if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
                if (triplanarPaint)
                {
                    material.SetFloat("_CamoScale", 1.7f);
                    material.SetFloat("_ModelHeight", 2f);
                    material.SetFloat("_ModelRadius", .8f);
                    material.SetFloat("_PaintMode", 1f);
                    material.SetFloat("_BaseCutoff", .31f);
                    material.SetColor("_EquipmentColor", new Color(.50f, .38f, .22f));
                    material.SetColor("_WeaponColor", new Color(.08f, .09f, .095f));
                    material.SetColor("_SkinColor", new Color(.76f, .53f, .41f));
                    material.SetFloat("_Smoothness", .08f);
                }
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.sharedMaterial = material;
            }
        }

        // Tints multiply the Standard shader's albedo texture (unlike the
        // other two skins' flat colors), so a value below white darkens the
        // painted figure rather than desaturating a solid tint.
        public void SetTint(Color tint)
        {
            if (!loaded) return;
            foreach (Renderer renderer in renderers) renderer.material.color = tint;
        }

        // Momentary override; Update() re-asserts it for a short window so
        // a same-frame reactive recompute (see BindReactive) can't
        // immediately stomp it, mirroring NatoSymbolView.FlashIcon.
        public void FlashTint(Color tint)
        {
            flashTint = tint;
            flashTimer = .35f;
            SetTint(tint);
        }

        public void UpdateStatusBadge(bool visible, TacticalCombatStatus status)
        {
            PresentedStatus = status;
            if (statusBadge == null) return;
            statusBadge.SetActive(visible);
            if (visible) statusBadgeRenderer.material.color = TacticalStatusVisuals.BadgeColor(status);
        }

        // Self-driving alternative to the explicit Present()/Cue* push
        // pattern, mirroring NatoSymbolView.BindReactive: binds once to the
        // live TacticalUnitState (mutated in place for the rest of the
        // battle) so the player's counter doesn't need wiring into the ~20
        // scattered tacticalUnit.Present() call sites. Only used for the
        // player's own unit; PLA contacts go through ContactMarkerView's
        // explicit SetTint calls instead.
        public void BindReactive(TacticalUnitState unit)
        {
            boundUnit = unit;
        }

        private void Update()
        {
            if (flashTimer > 0f)
            {
                flashTimer -= Time.unscaledDeltaTime;
                SetTint(flashTint);
            }
            else if (boundUnit != null)
            {
                float readiness = boundUnit.Readiness == UnitReadiness.Spent ? .55f : boundUnit.Readiness == UnitReadiness.Moving ? .85f : 1f;
                float desaturation = TacticalStatusVisuals.DesaturationFor(boundUnit.CombatStatus);
                Color tint = Color.Lerp(FullBrightTint, new Color(.35f, .35f, .35f), desaturation) * readiness;
                tint.a = 1f;
                SetTint(tint);
                UpdateStatusBadge(boundUnit.CombatStatus != TacticalCombatStatus.Ready, boundUnit.CombatStatus);
            }
            if (statusBadge != null && statusBadge.activeSelf)
                statusBadge.transform.localScale = Vector3.one * TacticalStatusVisuals.PulseScale(PresentedStatus, .13f);
        }
    }
}
