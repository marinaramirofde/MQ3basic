using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public enum WatchMenuAnchorMode
{
    Wrist,
    LeftController,
    RightController
}

[Serializable]
public sealed class WatchShortcutDefinition
{
    [SerializeField] private string label = "Shortcut";
    [SerializeField] private string description = "Text try";
    [SerializeField] private Sprite icon;
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Image iconImage;
    [SerializeField] private UnityEvent onInvoked = new UnityEvent();

    public UnityEvent OnInvoked => onInvoked;

    public void ApplyPresentation()
    {
        if (labelText != null) labelText.text = label;
        if (descriptionText != null) descriptionText.text = description;
        if (iconImage != null && icon != null) iconImage.sprite = icon;
    }

    public void Bind()
    {
        if (button != null) button.onClick.AddListener(Invoke);
    }

    public void Unbind()
    {
        if (button != null) button.onClick.RemoveListener(Invoke);
    }

    private void Invoke()
    {
        onInvoked.Invoke();
    }
}

/// <summary>Controls the Meta UI shortcut panel attached to the watch or either controller.</summary>
[DisallowMultipleComponent]
public sealed class WatchShortcutMenuController : MonoBehaviour
{
    [Header("Watch")]
    [SerializeField] private WatchController watch;

    [Header("Panel")]
    [SerializeField] private RectTransform menuRoot;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Graphic panelBackground;
    [SerializeField] private Graphic subtleGreenBorder;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private string title = "Shortcuts";

    [Header("Anchor")]
    [SerializeField] private WatchMenuAnchorMode anchorMode = WatchMenuAnchorMode.Wrist;
    [SerializeField] private Transform menuAttachment;
    [SerializeField] private Transform wristMenuAnchor;
    [SerializeField] private Transform leftControllerMenuAnchor;
    [SerializeField] private Transform rightControllerMenuAnchor;
    [Tooltip("Additional position in the selected anchor's local space.")]
    [SerializeField] private Vector3 positionOffset;
    [Tooltip("Additional Euler rotation in the selected anchor's local space.")]
    [SerializeField] private Vector3 rotationOffset;
    [Tooltip("Distance from the device surface along the anchor's forward axis.")]
    [SerializeField, Min(0)] private float distanceAboveDevice = 0.08f;
    [Tooltip("Physical width of the shortcut panel in metres.")]
    [SerializeField, Range(0.1f, 0.3f)] private float panelWidthMeters = 0.12f;
    [Tooltip("Fine adjustment for the physical panel size.")]
    [SerializeField, Range(0.5f, 2f)] private float panelScaleMultiplier = 1f;

    [Header("Appearance")]
    [SerializeField] private Color offColor = new Color(0.015f, 0.65f, 1f, 1f);
    [SerializeField] private Color onColor = new Color(0.08f, 1f, 0.28f, 1f);
    [SerializeField] private Color panelColor = new Color(0.018f, 0.025f, 0.035f, 0.94f);
    [SerializeField, Range(0, 3)] private float borderGlowIntensity = 0.65f;
    [SerializeField, Range(0.002f, 0.08f)] private float borderWidth = 0.014f;

    [Header("Shortcuts")]
    [SerializeField] private WatchShortcutDefinition[] shortcuts = new WatchShortcutDefinition[3];

    public WatchMenuAnchorMode AnchorMode => anchorMode;
    public bool IsOpen { get; private set; }
    public WatchShortcutDefinition[] Shortcuts => shortcuts;

    private Material backgroundMaterial;
    private Material borderMaterial;
    private bool initialized;

    private void Awake()
    {
        if (!ValidateReferences())
        {
            enabled = false;
            return;
        }

        CreateRuntimeMaterials();
        ApplyMaterialProperties();
        foreach (WatchShortcutDefinition shortcut in shortcuts) shortcut?.Bind();
        ApplyInitialAnchor();
        SetOpen(false);
        initialized = true;
    }

    private void OnEnable()
    {
        if (!initialized) return;
        watch.WhenPowerChanged += HandlePowerChanged;
        SetOpen(watch.IsOn);
    }

    public void SetAnchorMode(WatchMenuAnchorMode mode)
    {
        anchorMode = mode;
        ApplyAnchor();
    }

    [ContextMenu("Refresh Inspector Content")]
    public void ApplyInspectorContent()
    {
        if (titleText != null) titleText.text = title;
        if (shortcuts != null)
            foreach (WatchShortcutDefinition shortcut in shortcuts) shortcut?.ApplyPresentation();
        ApplyMaterialProperties();
    }

    private void HandlePowerChanged(bool isOn)
    {
        SetOpen(isOn);
    }

    private void SetOpen(bool open)
    {
        IsOpen = open;
        menuRoot.gameObject.SetActive(open);
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = open;
        canvasGroup.blocksRaycasts = open;
    }

    private void ApplyInitialAnchor()
    {
        Transform selected = GetSelectedAnchor();
        if (selected != null && menuAttachment.parent != selected)
        {
            ApplyAnchor();
            return;
        }

        // Preserve the pose authored on the prefab instance.
        ApplyPhysicalPanelScale();
    }

    private Transform GetSelectedAnchor()
    {
        Transform selected = anchorMode switch
        {
            WatchMenuAnchorMode.LeftController => leftControllerMenuAnchor,
            WatchMenuAnchorMode.RightController => rightControllerMenuAnchor,
            _ => wristMenuAnchor
        };

        return selected != null ? selected : wristMenuAnchor;
    }

    private void ApplyAnchor()
    {
        if (menuAttachment == null || menuRoot == null) return;
        Transform selected = GetSelectedAnchor();
        if (selected == null) return;

        if (menuAttachment.parent != selected) menuAttachment.SetParent(selected, false);

        // Treat these values as real-world metres so imported model scale cannot shrink the menu.
        menuAttachment.position = selected.position
            + selected.rotation * positionOffset
            + selected.forward * distanceAboveDevice;
        menuAttachment.rotation = selected.rotation * Quaternion.Euler(rotationOffset);
        menuAttachment.localScale = Vector3.one;
        menuRoot.localPosition = Vector3.zero;
        menuRoot.localRotation = Quaternion.identity;
        ApplyPhysicalPanelScale();
    }

    private void ApplyPhysicalPanelScale()
    {
        float canvasWidth = Mathf.Max(1f, menuRoot.rect.width);
        float targetWorldScale = panelWidthMeters * panelScaleMultiplier / canvasWidth;
        Vector3 parentScale = menuRoot.parent != null ? menuRoot.parent.lossyScale : Vector3.one;

        menuRoot.localScale = new Vector3(
            targetWorldScale / Mathf.Max(0.00001f, Mathf.Abs(parentScale.x)),
            targetWorldScale / Mathf.Max(0.00001f, Mathf.Abs(parentScale.y)),
            targetWorldScale / Mathf.Max(0.00001f, Mathf.Abs(parentScale.z)));
    }

    private void CreateRuntimeMaterials()
    {
        if (panelBackground != null && panelBackground.material != null)
        {
            backgroundMaterial = new Material(panelBackground.material);
            panelBackground.material = backgroundMaterial;
        }
        if (subtleGreenBorder != null && subtleGreenBorder.material != null)
        {
            borderMaterial = new Material(subtleGreenBorder.material);
            subtleGreenBorder.material = borderMaterial;
        }
    }

    private void ApplyMaterialProperties()
    {
        SetMaterialColor(backgroundMaterial, "_PanelColor", panelColor);
        SetMaterialColor(backgroundMaterial, "_BorderColor", Color.clear);
        SetMaterialColor(borderMaterial, "_PanelColor", Color.clear);
        SetMaterialColor(borderMaterial, "_BorderColor", onColor);
        SetMaterialFloat(borderMaterial, "_GlowStrength", borderGlowIntensity);
        SetMaterialFloat(backgroundMaterial, "_BorderWidth", borderWidth);
        SetMaterialFloat(borderMaterial, "_BorderWidth", borderWidth);
    }

    private bool ValidateReferences()
    {
        bool valid = watch != null && menuRoot != null && canvasGroup != null &&
            wristMenuAnchor != null && menuAttachment != null && shortcuts != null && shortcuts.Length == 3;
        if (!valid) Debug.LogError("Watch shortcut menu has missing Inspector references.", this);
        return valid;
    }

    private static void SetMaterialColor(Material material, string property, Color value)
    {
        if (material != null && material.HasProperty(property)) material.SetColor(property, value);
    }

    private static void SetMaterialFloat(Material material, string property, float value)
    {
        if (material != null && material.HasProperty(property)) material.SetFloat(property, value);
    }

    private void OnValidate()
    {
        panelWidthMeters = Mathf.Clamp(panelWidthMeters, 0.1f, 0.3f);
        panelScaleMultiplier = Mathf.Clamp(panelScaleMultiplier, 0.5f, 2f);
        if (!Application.isPlaying)
        {
            // Keep authored text and rotation intact while refreshing visual properties.
            ApplyMaterialProperties();
            ApplyPhysicalPanelScale();
        }
    }

    private void OnDisable()
    {
        if (!initialized) return;
        watch.WhenPowerChanged -= HandlePowerChanged;
        SetOpen(false);
    }

    private void OnDestroy()
    {
        if (shortcuts != null)
            foreach (WatchShortcutDefinition shortcut in shortcuts) shortcut?.Unbind();
        if (backgroundMaterial != null) Destroy(backgroundMaterial);
        if (borderMaterial != null) Destroy(borderMaterial);
    }
}
