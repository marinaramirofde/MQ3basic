using System.Collections;
using System.Collections.Generic;
using Oculus.Interaction;
using Oculus.Interaction.Surfaces;
using UnityEngine;

/// <summary>Self-contained visual watch prototype. Uses the scene's Meta ISDK interactors.</summary>
[DisallowMultipleComponent]
public sealed class WatchController : MonoBehaviour
{
    [Header("Model (resolved under this watch if empty)")]
    [SerializeField] private Transform screen;
    [SerializeField] private Renderer screenRenderer;
    [SerializeField] private Renderer indicator;
    [SerializeField] private Shader indicatorShader = null;
    [Header("Feedback")]
    [SerializeField, Min(0.01f)] private float duration = 4f;
    [SerializeField] private Color offColor = new Color(0.015f, 0.65f, 1f, 1f);
    [SerializeField] private Color onColor = new Color(0.08f, 1f, 0.28f, 1f);
    [SerializeField, Min(0)] private float emissionIntensity = 1.8f;
    [SerializeField, Range(0.1f, 0.8f)] private float indicatorSize = 0.5f;
    [SerializeField, Min(0)] private float surfaceOffset = 0.003f;
    public bool IsOn { get; private set; }
    public bool IsAnimating => transition != null;
    private Coroutine transition;
    private Quaternion restRotation;
    private Vector3 restPosition;
    private GameObject interactionRoot, indicatorObject;
    private PokeInteractable poke;
    private RayInteractable ray;
    private Material[] originals, materials;
    private Material indicatorMaterial, originalIndicatorMaterial;
    private Mesh indicatorMesh;
    private readonly HashSet<int> heldPointers = new HashSet<int>();
    private bool initialized;
    private GameObject glowObject, tilesRoot;
    private Material glowMaterial, tileMaterial;
    private readonly Transform[] tiles = new Transform[4];
    private readonly Vector3[] tilePositions = new Vector3[4];
    private float screenSize;
    [SerializeField, Min(0)] private float liftHeight = 0.28f;

    private void Awake()
    {
        if (screen == null)
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
                if (child.name == "Screen") { screen = child; break; }
        if (screen != null && screenRenderer == null) screenRenderer = screen.GetComponent<Renderer>();
        if (screen == null || screenRenderer == null || indicatorShader == null)
        {
            Debug.LogError("Watch: Screen, renderer or indicator shader is missing.", this);
            enabled = false;
            return;
        }
        restRotation = screen.localRotation;
        restPosition = screen.localPosition;
        Bounds bounds = screenRenderer.localBounds;
        
        originals = screenRenderer.sharedMaterials;
        materials = new Material[originals.Length];
        for (int i = 0; i < originals.Length; i++)
        {
            if (originals[i] == null) continue;
            materials[i] = new Material(originals[i]);
            materials[i].EnableKeyword("_EMISSION");
        }
        screenRenderer.sharedMaterials = materials;

        // The imported Screen lies in XZ. Keep the hit surface outside the rotating transform.
        interactionRoot = new GameObject("Screen Interaction (Meta ISDK)");
        interactionRoot.SetActive(false);
        interactionRoot.transform.SetParent(screen.parent, false);
        interactionRoot.transform.localPosition = restPosition + restRotation *
            Vector3.Scale(new Vector3(bounds.center.x, bounds.max.y + surfaceOffset, bounds.center.z), screen.localScale);
        interactionRoot.transform.localRotation = restRotation * Quaternion.Euler(-90, 0, 0);
        interactionRoot.transform.localScale = screen.localScale;
        var plane = interactionRoot.AddComponent<PlaneSurface>();
        plane.InjectAllPlaneSurface(PlaneSurface.NormalFacing.Forward, false);
        var circle = interactionRoot.AddComponent<CircleSurface>();
        circle.InjectAllCircleSurface(plane);
        circle.InjectOptionalRadius(Mathf.Min(bounds.extents.x, bounds.extents.z));
        poke = interactionRoot.AddComponent<PokeInteractable>();
        poke.InjectAllPokeInteractable(circle);
        ray = interactionRoot.AddComponent<RayInteractable>();
        ray.InjectAllRayInteractable(circle);

        indicatorMaterial = new Material(indicatorShader);
        if (indicator == null)
        {
            indicatorObject = new GameObject("Power Indicator");
            indicatorObject.transform.SetParent(screen, false);
            indicatorObject.transform.localPosition = new Vector3(bounds.center.x, bounds.max.y + surfaceOffset, bounds.center.z);
            indicatorObject.transform.localRotation = Quaternion.Euler(-90, 0, 0);
            float size = Mathf.Min(bounds.size.x, bounds.size.z) * indicatorSize;
            indicatorObject.transform.localScale = new Vector3(size, size, size);
            indicatorMesh = new Mesh { name = "Watch Indicator Quad" };
            indicatorMesh.vertices = new[] { new Vector3(-.5f,-.5f,0), new Vector3(.5f,-.5f,0), new Vector3(.5f,.5f,0), new Vector3(-.5f,.5f,0) };
            indicatorMesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
            indicatorMesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            indicatorMesh.RecalculateBounds();
            indicatorObject.AddComponent<MeshFilter>().sharedMesh = indicatorMesh;
            indicator = indicatorObject.AddComponent<MeshRenderer>();
            indicator.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            indicator.receiveShadows = false;
        }
        originalIndicatorMaterial = indicator.sharedMaterial;
        indicator.sharedMaterial = indicatorMaterial;
        CreateScreenGlowAndTiles(bounds);
        IsOn = false;
        ApplyFeedback(offColor, 0);
        initialized = true;
        interactionRoot.SetActive(true);
    }

    private void OnEnable()
    {
        if (!initialized) return;
        poke.WhenPointerEventRaised += OnPointer;
        ray.WhenPointerEventRaised += OnPointer;
        interactionRoot.SetActive(true);
    }

    private void OnPointer(PointerEvent evt)
    {
        if (evt.Type == PointerEventType.Select)
        {
            bool wasReleased = heldPointers.Count == 0;
            if (heldPointers.Add(evt.Identifier) && wasReleased) Toggle();
        }
        else if (evt.Type == PointerEventType.Unselect || evt.Type == PointerEventType.Cancel)
            heldPointers.Remove(evt.Identifier);
    }

    [ContextMenu("Toggle Watch (Play Mode)")]
    public void Toggle()
    {
        if (!Application.isPlaying || !isActiveAndEnabled || !initialized || IsAnimating) return;
        IsOn = !IsOn;
        Debug.Log(IsOn ? "Watch ON" : "Watch OFF", this);
        transition = StartCoroutine(Animate());
    }

    private IEnumerator Animate()
    {
        Color from = IsOn ? offColor : onColor;
        Color to = IsOn ? onColor : offColor;
        float elapsed = 0;
        float seconds = Mathf.Max(4f, duration);
        tilesRoot.SetActive(true);
        while (elapsed < seconds)
        {
            float t = elapsed / seconds;
            float eased = Mathf.SmoothStep(0, 1, t);
            Color transitionColor = Color.Lerp(from, to, eased);
            AnimateTiles(t);
            ApplyFeedback(transitionColor, Mathf.Sin(Mathf.PI * t), IsOn ? eased : 1 - eased);
            elapsed += Time.deltaTime;
            yield return null;
        }
        AnimateTiles(0);
        ApplyFeedback(to, 0);
        tilesRoot.SetActive(false);
        transition = null;
    }

    // Generated overlays stay on the face while the four independent tiles lift and spin.
    private void CreateScreenGlowAndTiles(Bounds bounds)
    {
        screenSize = Mathf.Min(bounds.size.x, bounds.size.z);
        glowMaterial = new Material(indicatorShader);
        glowMaterial.SetFloat("_Mode", 1);
        glowObject = new GameObject("Full Screen Inner Glow");
        glowObject.transform.SetParent(screen, false);
        glowObject.transform.localPosition = new Vector3(bounds.center.x, bounds.max.y + surfaceOffset * 0.5f, bounds.center.z);
        glowObject.transform.localRotation = Quaternion.Euler(-90, 0, 0);
        glowObject.transform.localScale = Vector3.one * screenSize;
        // Use the same quad for both overlays, including when an indicator is assigned externally.
        Mesh quad = indicator.GetComponent<MeshFilter>()?.sharedMesh;
        if (quad != null)
        {
            glowObject.AddComponent<MeshFilter>().sharedMesh = quad;
            var glowRenderer = glowObject.AddComponent<MeshRenderer>();
            glowRenderer.sharedMaterial = glowMaterial;
            glowRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            glowRenderer.receiveShadows = false;
        }
        tileMaterial = new Material(indicatorShader);
        tileMaterial.SetFloat("_Mode", 2);
        tilesRoot = new GameObject("Animated Power Tiles");
        tilesRoot.transform.SetParent(screen, false);
        tilesRoot.SetActive(false);
        for (int i = 0; i < tiles.Length; i++)
        {
            var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tile.name = "Power Tile " + (i + 1);
            var collider = tile.GetComponent<Collider>();
            collider.enabled = false;
            Destroy(collider);
            tiles[i] = tile.transform;
            tiles[i].SetParent(tilesRoot.transform, false);
            float spacing = screenSize * indicatorSize * 0.11f;
            tilePositions[i] = new Vector3(bounds.center.x + (i % 2 == 0 ? -spacing : spacing),
                bounds.max.y + surfaceOffset * 1.5f,
                bounds.center.z + (i < 2 ? -spacing : spacing));
            tiles[i].localPosition = tilePositions[i];
            tiles[i].localScale = new Vector3(spacing * 1.45f, screenSize * 0.018f, spacing * 1.45f);
            var renderer = tile.GetComponent<Renderer>();
            renderer.sharedMaterial = tileMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }

    private void AnimateTiles(float t)
    {
        // Rise, remain suspended while spinning, then settle exactly over the flat symbol.
        float rise = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / 0.15f));
        float fall = Mathf.SmoothStep(0, 1, Mathf.Clamp01((t - 0.78f) / 0.22f));
        float lift = rise * (1 - fall);
        float spin = Mathf.SmoothStep(0, 1, t);
        for (int i = 0; i < tiles.Length; i++)
        {
            tiles[i].localPosition = tilePositions[i] + Vector3.up * (screenSize * liftHeight * lift);

            // Whole numbers of turns make every cube land flush without a final rotation snap.
            float xTurns = i % 2 == 0 ? 2 : 3;
            float zTurns = i < 2 ? 3 : 2;
            tiles[i].localRotation = Quaternion.Euler(360 * xTurns * spin, 360 * (i + 2) * spin,
                360 * zTurns * spin);
        }
    }
    private void ApplyFeedback(Color color, float pulse, float power = -1)
    {
        if (power < 0) power = IsOn ? 1 : 0;
        Color faceColor = Color.Lerp(Color.black, color * 0.55f, power);
        faceColor.a = 1;
        Color emission = color * emissionIntensity * (power * 0.55f + pulse * 0.7f);
        foreach (Material material in materials)
        {
            if (material == null) continue;
            SetColor(material, "_BaseColor", faceColor);
            SetColor(material, "_Color", faceColor);
            SetColor(material, "baseColorFactor", faceColor);
            SetColor(material, "_EmissionColor", emission);
            SetColor(material, "emissiveFactor", emission);
        }
        indicatorMaterial.SetColor("_Color", color);
        indicatorMaterial.SetFloat("_Intensity", emissionIntensity * Mathf.Lerp(0.72f, 1f, power) * (1 + pulse * 0.35f));
        glowMaterial.SetColor("_Color", color);
        glowMaterial.SetFloat("_Intensity", power * 0.85f + pulse * 0.35f);
        tileMaterial.SetColor("_Color", color);
        tileMaterial.SetFloat("_Intensity", Mathf.Lerp(0.16f, 1.4f, power) + pulse * 0.4f);
    }

    private static void SetColor(Material material, string property, Color value)
    {
        if (material.HasProperty(property)) material.SetColor(property, value);
    }

    private void OnDisable()
    {
        if (!initialized) return;
        poke.WhenPointerEventRaised -= OnPointer;
        ray.WhenPointerEventRaised -= OnPointer;
        interactionRoot.SetActive(false);
        heldPointers.Clear();
        if (transition != null) StopCoroutine(transition);
        transition = null;
        screen.localRotation = restRotation;
        screen.localPosition = restPosition;
        AnimateTiles(0);
        ApplyFeedback(IsOn ? onColor : offColor, 0);
    }

    private void OnDestroy()
    {
        if (screenRenderer != null && originals != null) screenRenderer.sharedMaterials = originals;
        if (materials != null) foreach (Material material in materials) if (material != null) Destroy(material);
        if (indicator != null) indicator.sharedMaterial = originalIndicatorMaterial;
        if (glowObject != null) Destroy(glowObject);
        if (tilesRoot != null) Destroy(tilesRoot);
        if (glowMaterial != null) Destroy(glowMaterial);
        if (tileMaterial != null) Destroy(tileMaterial);
        if (indicatorMaterial != null) Destroy(indicatorMaterial);
        if (indicatorMesh != null) Destroy(indicatorMesh);
        if (indicatorObject != null) Destroy(indicatorObject);
        if (interactionRoot != null) Destroy(interactionRoot);
    }
}
