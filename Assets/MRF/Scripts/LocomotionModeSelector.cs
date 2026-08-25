using Oculus.Interaction.Locomotion;
using UnityEngine;

/// <summary>
/// Selects the Meta XR locomotion mode from Button OnClick events.
/// MovingSetting is found automatically at runtime.
/// </summary>
[DisallowMultipleComponent]
public sealed class LocomotionModeSelector : MonoBehaviour
{
    private MovingSetting movingSetting;
    private bool missingSettingReported;

    private void Awake()
    {
        ResolveMovingSetting();
    }

    public void SelectTeleport()
    {
        SetMovementStyle(MovingSetting.MovementStyle.Teleport);
    }

    public void SelectContinuous()
    {
        SetMovementStyle(MovingSetting.MovementStyle.Slide);
    }

    private void SetMovementStyle(MovingSetting.MovementStyle style)
    {
        if (ResolveMovingSetting())
        {
            movingSetting.ControllerMovement.Value = style;
        }
    }

    private bool ResolveMovingSetting()
    {
        if (movingSetting != null)
        {
            return true;
        }

        movingSetting = FindFirstObjectByType<MovingSetting>(FindObjectsInactive.Include);

        if (movingSetting != null)
        {
            return true;
        }

        if (!missingSettingReported)
        {
            Debug.LogError("MovingSetting was not found in the scene.", this);
            missingSettingReported = true;
        }

        return false;
    }
}
