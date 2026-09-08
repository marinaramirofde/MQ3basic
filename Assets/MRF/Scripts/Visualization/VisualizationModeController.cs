using System.Collections;
using UnityEngine;

/// <summary>
/// Owns the three mutually exclusive ways in which the XR environment is shown.
/// Scene dependencies are assigned explicitly in the Inspector.
/// </summary>
public sealed class VisualizationModeController : MonoBehaviour
{
    private static readonly int InvertedAlphaId = Shader.PropertyToID("_InvertedAlpha");

    [Header("Explicit References")]
    [SerializeField] private OVRPassthroughLayer passthroughLayer;
    [SerializeField] private Camera xrCamera;
    [SerializeField] private Shader faderShader;
    [SerializeField] private Renderer[] virtualEnvironmentRenderers;

    [Header("Initial State")]
    [SerializeField] private VisualizationMode initialMode = VisualizationMode.Virtual;

    [Header("Transition")]
    [SerializeField, Min(0.01f)] private float fadeDuration = 0.75f;

    private MeshRenderer faderRenderer;
    private Material faderMaterial;
    private Coroutine transition;
    private bool passthroughReady;

    public VisualizationMode CurrentMode { get; private set; }

    private void Awake()
    {
        if (passthroughLayer == null || xrCamera == null || faderShader == null)
        {
            Debug.LogError(
                "Assign Passthrough Layer, XR Camera and Fader Shader explicitly.", this);
            enabled = false;
            return;
        }

        CreateFader();
        passthroughLayer.passthroughLayerResumed.AddListener(OnPassthroughLayerResumed);
        OVRManager.eyeFovPremultipliedAlphaModeEnabled = false;
        ApplyImmediate(initialMode);
    }

    private void OnDestroy()
    {
        if (passthroughLayer != null)
        {
            passthroughLayer.passthroughLayerResumed.RemoveListener(OnPassthroughLayerResumed);
        }

        if (faderMaterial != null)
        {
            Destroy(faderMaterial);
        }
    }

    /// <summary>Receives the integer emitted by a dropdown.</summary>
    public void SetMode(int value)
    {
        if (value >= (int)VisualizationMode.Virtual &&
            value <= (int)VisualizationMode.Focus)
        {
            SetMode((VisualizationMode)value);
        }
    }

    /// <summary>Changes to exactly one visualization mode.</summary>
    public void SetMode(VisualizationMode mode)
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        StopTransition();
        CurrentMode = mode;

        if (mode == VisualizationMode.Focus)
        {
            ApplyFocus();
            return;
        }

        transition = StartCoroutine(
            TransitionPassthrough(mode == VisualizationMode.Passthrough));
    }

    /// <summary>Convenience method for a Button On Click event.</summary>
    public void SetVirtual() => SetMode(VisualizationMode.Virtual);

    /// <summary>Use this overload from a Toggle On Value Changed event.</summary>
    public void SetVirtual(bool isSelected)
    {
        if (isSelected)
        {
            SetVirtual();
        }
    }

    /// <summary>Convenience method for a Button On Click event.</summary>
    public void SetPassthrough() => SetMode(VisualizationMode.Passthrough);

    /// <summary>Use this overload from a Toggle On Value Changed event.</summary>
    public void SetPassthrough(bool isSelected)
    {
        if (isSelected)
        {
            SetPassthrough();
        }
    }

    /// <summary>Convenience method for a Button On Click event.</summary>
    public void SetFocus() => SetMode(VisualizationMode.Focus);

    /// <summary>Use this overload from a Toggle On Value Changed event.</summary>
    public void SetFocus(bool isSelected)
    {
        if (isSelected)
        {
            SetFocus();
        }
    }

    private IEnumerator TransitionPassthrough(bool enabled)
    {
        if (enabled)
        {
            passthroughReady = OVRManager.IsInsightPassthroughInitialized();
            passthroughLayer.hidden = false;
            passthroughLayer.textureOpacity = 1f;
            passthroughLayer.enabled = true;

            // Passthrough starts asynchronously, so virtual content remains visible until ready.
            float timeout = 2f;
            while (!passthroughReady && timeout > 0f)
            {
                passthroughReady = OVRManager.IsInsightPassthroughInitialized();
                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }
        }
        else
        {
            SetVirtualEnvironmentVisible(true);
            xrCamera.clearFlags = CameraClearFlags.Skybox;
        }

        faderRenderer.enabled = true;
        float start = faderMaterial.GetFloat(InvertedAlphaId);
        float target = enabled ? 1f : 0f;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.SmoothStep(
                0f, 1f, Mathf.Clamp01(elapsed / fadeDuration));
            faderMaterial.SetFloat(
                InvertedAlphaId, Mathf.Lerp(start, target, progress));
            yield return null;
        }

        faderMaterial.SetFloat(InvertedAlphaId, target);

        if (enabled)
        {
            SetVirtualEnvironmentVisible(false);
            xrCamera.clearFlags = CameraClearFlags.SolidColor;
            xrCamera.backgroundColor = Color.clear;
        }
        else
        {
            passthroughLayer.enabled = false;
        }

        faderRenderer.enabled = false;
        transition = null;
    }

    private void ApplyFocus()
    {
        passthroughLayer.enabled = false;
        SetVirtualEnvironmentVisible(false);
        faderMaterial.SetFloat(InvertedAlphaId, 0f);
        faderRenderer.enabled = false;
        xrCamera.clearFlags = CameraClearFlags.SolidColor;
        xrCamera.backgroundColor = Color.black;
    }

    private void ApplyImmediate(VisualizationMode mode)
    {
        CurrentMode = mode;
        bool passthrough = mode == VisualizationMode.Passthrough;

        faderMaterial.SetFloat(InvertedAlphaId, passthrough ? 1f : 0f);
        faderRenderer.enabled = false;
        SetVirtualEnvironmentVisible(mode == VisualizationMode.Virtual);

        passthroughLayer.hidden = false;
        passthroughLayer.textureOpacity = 1f;
        passthroughLayer.enabled = passthrough;

        xrCamera.clearFlags = mode == VisualizationMode.Virtual
            ? CameraClearFlags.Skybox
            : CameraClearFlags.SolidColor;
        xrCamera.backgroundColor = mode == VisualizationMode.Focus
            ? Color.black
            : Color.clear;
    }

    private void StopTransition()
    {
        if (transition == null)
        {
            return;
        }

        StopCoroutine(transition);
        transition = null;
        faderRenderer.enabled = false;
    }

    private void CreateFader()
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "PassthroughFader (Runtime)";
        sphere.transform.SetParent(xrCamera.transform, false);
        sphere.transform.localScale = Vector3.one * 2f;
        sphere.layer = xrCamera.gameObject.layer;

        Collider sphereCollider = sphere.GetComponent<Collider>();
        if (sphereCollider != null)
        {
            Destroy(sphereCollider);
        }

        faderRenderer = sphere.GetComponent<MeshRenderer>();
        faderMaterial = new Material(faderShader)
        {
            name = "Passthrough Fader (Runtime)"
        };
        faderRenderer.sharedMaterial = faderMaterial;
    }

    private void SetVirtualEnvironmentVisible(bool visible)
    {
        if (virtualEnvironmentRenderers == null)
        {
            return;
        }

        foreach (Renderer virtualRenderer in virtualEnvironmentRenderers)
        {
            if (virtualRenderer != null)
            {
                // Hide only presentation; colliders and locomotion surfaces remain active.
                virtualRenderer.enabled = visible;
            }
        }
    }

    private void OnPassthroughLayerResumed(OVRPassthroughLayer layer)
    {
        passthroughReady = true;
    }
}
