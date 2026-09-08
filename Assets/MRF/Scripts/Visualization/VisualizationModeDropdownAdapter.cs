using Oculus.Interaction.Samples;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Maps three explicitly assigned Meta dropdown options to VisualizationMode values.
/// </summary>
[DisallowMultipleComponent]
public sealed class VisualizationModeDropdownAdapter : MonoBehaviour
{
    [Header("Explicit References")]
    [SerializeField] private DropDownGroup dropDownGroup;
    [SerializeField] private VisualizationModeController modeController;

    [Header("Options (Enum Order)")]
    [SerializeField] private Toggle virtualToggle;
    [SerializeField] private Toggle passthroughToggle;
    [SerializeField] private Toggle focusToggle;

    private void Awake()
    {
        if (dropDownGroup == null || modeController == null ||
            virtualToggle == null || passthroughToggle == null || focusToggle == null)
        {
            Debug.LogError("Assign every Visualization Mode dropdown reference.", this);
            enabled = false;
            return;
        }

        // The array order is the enum order: Virtual, Passthrough, Focus.
        dropDownGroup.InjectToggles(
            new[] { virtualToggle, passthroughToggle, focusToggle });
    }

    private void Start()
    {
        SetMode(dropDownGroup.SelectedIndex);
    }

    /// <summary>
    /// Connect DropDownGroup.WhenSelectionChanged(Int32) to this method in the Inspector.
    /// </summary>
    public void SetMode(int selectedIndex)
    {
        if (selectedIndex >= (int)VisualizationMode.Virtual &&
            selectedIndex <= (int)VisualizationMode.Focus)
        {
            modeController.SetMode((VisualizationMode)selectedIndex);
        }
    }
}
