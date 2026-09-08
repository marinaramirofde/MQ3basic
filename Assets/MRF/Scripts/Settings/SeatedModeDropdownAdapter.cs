using Oculus.Interaction.Locomotion;
using Oculus.Interaction.Samples;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class SeatedModeDropdownAdapter : MonoBehaviour
{
    [SerializeField] private DropDownGroup dropDownGroup;
    [SerializeField] private StandingSetting standingSetting;

    private void Awake()
    {
        if (dropDownGroup == null || standingSetting == null)
        {
            Debug.LogError(
                "Assign Drop Down Group and Standing Setting explicitly.",
                this);
            enabled = false;
            return;
        }

        ConfigureOptions();
    }

    private void OnEnable()
    {
        if (dropDownGroup != null)
        {
            dropDownGroup.WhenSelectionChanged.AddListener(SetMode);
        }
    }

    private void Start()
    {
        if (dropDownGroup != null && dropDownGroup.SelectedIndex >= 0)
        {
            SetMode(dropDownGroup.SelectedIndex);
        }
    }

    private void OnDisable()
    {
        if (dropDownGroup != null)
        {
            dropDownGroup.WhenSelectionChanged.RemoveListener(SetMode);
        }
    }

    public void SetMode(int value)
    {
        if (standingSetting == null || value < 0 || value > 1)
        {
            return;
        }

        standingSetting.Standing.Value =
            (StandingSetting.StandingMode)value;
    }

    private void ConfigureOptions()
    {
        if (dropDownGroup == null)
        {
            return;
        }

        ToggleGroup toggleGroup =
            dropDownGroup.GetComponentInChildren<ToggleGroup>(true);

        if (toggleGroup == null)
        {
            return;
        }

        Toggle standing = null;
        Toggle seating = null;

        foreach (Toggle toggle in
                 toggleGroup.GetComponentsInChildren<Toggle>(true))
        {
            if (toggle.group != toggleGroup)
            {
                continue;
            }

            if (toggle.name == "Standing")
            {
                standing = toggle;
            }
            else if (toggle.name == "Seating")
            {
                seating = toggle;
            }
            else
            {
                toggle.gameObject.SetActive(false);
            }
        }

        if (standing != null && seating != null)
        {
            dropDownGroup.InjectToggles(new[] { standing, seating });
        }
    }

}
