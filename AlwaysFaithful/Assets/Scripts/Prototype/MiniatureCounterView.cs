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
        // Native models are ~2 world units tall (a standing human figure);
        // scaled down to read as a counter on a 1-unit-radius hex rather
        // than a literal-scale figure. Tuned empirically against a capture,
        // not derived from any real-world scale — this is a wargame counter
        // convention (grossly oversized icon per unit), not a diorama.
        private const float ModelScale = .62f;

        // Slightly above white: this scene's two directional lights plus a
        // fully matte (zero-glossiness) material still rendered these
        // painted textures darker than expected at full brightness.
        public static readonly Color FullBrightTint = new Color(1.25f, 1.25f, 1.25f);

        private GameObject modelInstance;
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

        private static string ModelPathFor(bool friendly, bool isSupportRole)
        {
            if (friendly) return "Models/OneStar/USMC Rifleman";
            return isSupportRole ? "Models/OneStar/PLANMC Mortar Team" : "Models/OneStar/PLANMC Rifleman";
        }

        public void Initialize(bool friendly, bool isSupportRole, string labelText)
        {
            string path = ModelPathFor(friendly, isSupportRole);
            GameObject prefab = Resources.Load<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning($"ALWAYS_FAITHFUL_MINIATURE_MISSING {path}");
                return;
            }
            modelInstance = Instantiate(prefab, transform);
            modelInstance.name = "Miniature Model";
            modelInstance.transform.localPosition = Vector3.zero;
            modelInstance.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            modelInstance.transform.localScale = Vector3.one * ModelScale;
            foreach (Collider stale in modelInstance.GetComponentsInChildren<Collider>()) Destroy(stale);

            Texture2D texture = Resources.Load<Texture2D>(path + " Texture");
            Shader litShader = Shader.Find("Standard") ?? Shader.Find("Diffuse");
            renderers = modelInstance.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                Material material = new Material(litShader);
                if (texture != null) material.mainTexture = texture;
                // Matte, non-specular response: painted-miniature look under
                // this scene's two directional lights, and noticeably
                // brighter than the Standard shader's glossy default, which
                // read almost silhouette-dark at the default 0.5 smoothness.
                material.SetFloat("_Glossiness", 0f);
                material.SetFloat("_Metallic", 0f);
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.sharedMaterial = material;
            }
            loaded = true;

            Shader overlay = Resources.Load<Shader>("Shaders/MapOverlay") ?? Shader.Find("Sprites/Default");
            statusBadge = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            statusBadge.name = "Miniature Status Badge";
            statusBadge.layer = LayerMask.NameToLayer("Ignore Raycast");
            statusBadge.transform.SetParent(transform, false);
            statusBadge.transform.localPosition = new Vector3(0f, .78f, .10f);
            statusBadge.transform.localScale = Vector3.one * .13f;
            statusBadgeRenderer = statusBadge.GetComponent<MeshRenderer>();
            statusBadgeRenderer.sharedMaterial = new Material(overlay);
            Destroy(statusBadge.GetComponent<Collider>());
            statusBadge.SetActive(false);
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
