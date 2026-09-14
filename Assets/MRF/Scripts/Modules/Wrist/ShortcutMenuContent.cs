using UnityEngine;
namespace MRF.Modules.Wrist
{
    /// <summary>Shortcut content and appearance. Has no reference to any watch, input or power state.</summary>
    public sealed class ShortcutMenuContent : MonoBehaviour
    {
    [SerializeField] private UnityEngine.UI.Graphic panelBackground;
    [SerializeField] private UnityEngine.UI.Graphic subtleGreenBorder;
    [SerializeField] private TMPro.TMP_Text titleText;
    [SerializeField] private string title = "Shortcuts";
    [Header("Appearance")]
    [SerializeField] private Color offColor = new Color(0.015f, 0.65f, 1f, 1f);
    [SerializeField] private Color onColor = new Color(0.08f, 1f, 0.28f, 1f);
    [SerializeField] private Color panelColor = new Color(0.018f, 0.025f, 0.035f, 0.94f);
    [SerializeField, Range(0, 3)] private float borderGlowIntensity = 0.65f;
    [SerializeField, Range(0.002f, 0.08f)] private float borderWidth = 0.014f;

    [Header("Shortcuts")]
    [SerializeField] private WatchShortcutDefinition[] shortcuts = new WatchShortcutDefinition[3];

        private Material backgroundMaterial, borderMaterial;
        private Material originalBackground, originalBorder;
        private void Awake()
        {
            originalBackground = panelBackground != null ? panelBackground.material : null;
            originalBorder = subtleGreenBorder != null ? subtleGreenBorder.material : null;
            CreateRuntimeMaterials();
            ApplyContent();
        }
        private void OnEnable()
        {
            if (shortcuts != null) foreach (var shortcut in shortcuts) shortcut?.Bind();
        }
        private void OnDisable()
        {
            if (shortcuts != null) foreach (var shortcut in shortcuts) shortcut?.Unbind();
        }
        [ContextMenu("Apply Content")]
        public void ApplyContent()
        {
            if (titleText != null) titleText.text = title;
            if (shortcuts != null) foreach (var shortcut in shortcuts) shortcut?.ApplyPresentation();
            ApplyMaterialProperties();
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

    private static void SetMaterialColor(Material material, string property, Color value)
    {
        if (material != null && material.HasProperty(property)) material.SetColor(property, value);
    }

    private static void SetMaterialFloat(Material material, string property, float value)
    {
        if (material != null && material.HasProperty(property)) material.SetFloat(property, value);
    }

        private void OnDestroy()
        {
            if (panelBackground != null) panelBackground.material = originalBackground;
            if (subtleGreenBorder != null) subtleGreenBorder.material = originalBorder;
            if (backgroundMaterial != null) Destroy(backgroundMaterial);
            if (borderMaterial != null) Destroy(borderMaterial);
        }
    }
}
