using Oculus.Interaction.Locomotion;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[DefaultExecutionOrder(100)]
public sealed class LocomotionModeSelector : MonoBehaviour
{
    [Header("Exclusive locomotion toggles")]
    [SerializeField] private Toggle teleportToggle;
    [SerializeField] private Toggle continuousToggle;

    private readonly List<MovingSetting> movingSettings = new();
    private bool updatingToggles;
    private bool missingSettingReported;

    private void Awake()
    {
        ResolveToggles();
        ConfigureToggleGroup();
    }

    private void Start()
    {
        ResolveMovingSettings();

        MovingSetting.MovementStyle initialStyle =
            continuousToggle != null && continuousToggle.isOn
                ? MovingSetting.MovementStyle.Slide
                : MovingSetting.MovementStyle.Teleport;

        SelectStyle(initialStyle, forceRefresh: true);
    }

    public void SelectTeleport()
    {
        SelectStyle(MovingSetting.MovementStyle.Teleport, forceRefresh: false);
    }

    public void SelectContinuous()
    {
        SelectStyle(MovingSetting.MovementStyle.Slide, forceRefresh: false);
    }

    /// <summary>Receives the option index emitted by the locomotion dropdown.</summary>
    public void SetMode(int selectedIndex)
    {
        switch (selectedIndex)
        {
            case 0:
                SelectTeleport();
                break;
            case 1:
                SelectContinuous();
                break;
        }
    }

    public void OnTeleportValueChanged(bool isOn)
    {
        if (isOn && !updatingToggles)
        {
            SelectTeleport();
        }
    }

    public void OnContinuousValueChanged(bool isOn)
    {
        if (isOn && !updatingToggles)
        {
            SelectContinuous();
        }
    }

    private void SelectStyle(MovingSetting.MovementStyle style, bool forceRefresh)
    {
        SetToggleState(style);
        ResolveMovingSettings();

        int appliedCount = 0;

        foreach (MovingSetting movingSetting in movingSettings)
        {
            if (movingSetting == null || !movingSetting.isActiveAndEnabled)
            {
                continue;
            }

            // MovingSetting only subscribes to ControllerMovement changes from
            // OnEnable after its Start has completed. A rig that starts enabled
            // can therefore miss that subscription, leaving both locomotion
            // object sets active even though the ReactiveValue changes.
            movingSetting.enabled = false;
            movingSetting.enabled = true;

            if (forceRefresh && movingSetting.ControllerMovement.Value == style)
            {
                movingSetting.ControllerMovement.Value =
                    style == MovingSetting.MovementStyle.Teleport
                        ? MovingSetting.MovementStyle.Slide
                        : MovingSetting.MovementStyle.Teleport;
            }

            movingSetting.ControllerMovement.Value = style;
            appliedCount++;
        }

        SetTeleportInteractorsActive(
            style == MovingSetting.MovementStyle.Teleport);

        if (appliedCount == 0 && !missingSettingReported)
        {
            Debug.LogError("No active MovingSetting was found in the scene.", this);
            missingSettingReported = true;
        }
        else if (appliedCount > 0)
        {
            missingSettingReported = false;
        }
    }

    private void SetToggleState(MovingSetting.MovementStyle style)
    {
        updatingToggles = true;

        if (teleportToggle != null)
        {
            teleportToggle.SetIsOnWithoutNotify(
                style == MovingSetting.MovementStyle.Teleport);
        }

        if (continuousToggle != null)
        {
            continuousToggle.SetIsOnWithoutNotify(
                style == MovingSetting.MovementStyle.Slide);
        }

        updatingToggles = false;
    }

    private void ResolveToggles()
    {
        if (teleportToggle != null && continuousToggle != null)
        {
            return;
        }

        Toggle[] toggles = GetComponentsInChildren<Toggle>(includeInactive: true);

        foreach (Toggle toggle in toggles)
        {
            if (teleportToggle == null && toggle.name == "TeleportButton")
            {
                teleportToggle = toggle;
            }
            else if (continuousToggle == null && toggle.name == "ContinuousButton")
            {
                continuousToggle = toggle;
            }
        }
    }

    private void ConfigureToggleGroup()
    {
        if (teleportToggle == null || continuousToggle == null)
        {
            Debug.LogError(
                "TeleportButton and ContinuousButton must both use Toggle components.",
                this);
            return;
        }

        ToggleGroup group = teleportToggle.group != null
            ? teleportToggle.group
            : continuousToggle.group;

        if (group == null)
        {
            group = teleportToggle.GetComponentInParent<ToggleGroup>();
        }

        if (group == null)
        {
            group = gameObject.AddComponent<ToggleGroup>();
        }

        group.allowSwitchOff = false;
        teleportToggle.group = group;
        continuousToggle.group = group;
    }

    private void ResolveMovingSettings()
    {
        movingSettings.Clear();
        movingSettings.AddRange(
            FindObjectsByType<MovingSetting>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None));
    }

    private static void SetTeleportInteractorsActive(bool active)
    {
        TeleportInteractor[] teleportInteractors =
            FindObjectsByType<TeleportInteractor>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

        foreach (TeleportInteractor teleportInteractor in teleportInteractors)
        {
            GameObject teleportObject = teleportInteractor.gameObject;

            if (teleportObject.activeSelf != active)
            {
                teleportObject.SetActive(active);
            }
        }
    }
}
