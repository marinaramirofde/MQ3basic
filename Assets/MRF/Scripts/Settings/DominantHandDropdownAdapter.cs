using Oculus.Interaction.Samples;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

/// <summary>Synchronizes one menu with shared handedness, including localized option and header text.</summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-100)]
public sealed class DominantHandDropdownAdapter : MonoBehaviour
{
    [SerializeField] private DominantHandWristSelector selector;
    [SerializeField] private DropDownGroup dropDownGroup;
    [SerializeField] private Toggle leftToggle;
    [SerializeField] private Toggle rightToggle;
    [SerializeField] private TMPro.TMP_Text leftLabel;
    [SerializeField] private TMPro.TMP_Text rightLabel;
    [SerializeField] private LocalizedString leftText = new LocalizedString("Settings UI", "LEFT");
    [SerializeField] private LocalizedString rightText = new LocalizedString("Settings UI", "RIGHT");
    private bool synchronizing;
    private string localizedLeft = "Left";
    private string localizedRight = "Right";

    private void Awake()
    {
        if (selector == null || dropDownGroup == null || leftToggle == null ||
            rightToggle == null || leftLabel == null || rightLabel == null)
        {
            Debug.LogError("Assign all dominant-hand dropdown references in the Inspector.", this);
            enabled = false;
            return;
        }
        dropDownGroup.InjectToggles(new[] { leftToggle, rightToggle });
    }

    private void OnEnable()
    {
        selector.HandChanged += Synchronize;
        leftText.StringChanged += UpdateLeftText;
        rightText.StringChanged += UpdateRightText;
        Synchronize(selector.CurrentHand);
    }

    private void OnDisable()
    {
        if (selector != null) selector.HandChanged -= Synchronize;
        leftText.StringChanged -= UpdateLeftText;
        rightText.StringChanged -= UpdateRightText;
    }

    /// <summary>Connect the dropdown's dynamic Int32 event here, visibly in the Inspector.</summary>
    public void SetDominantHand(int index)
    {
        if (!synchronizing) selector.SetDominantHand(index);
        RefreshHeader();
    }

    private void Synchronize(DominantHandWristSelector.Hand hand)
    {
        // Suppress persistence during UI refresh; still notify the SDK to update its selected index.
        synchronizing = true;
        leftToggle.isOn = hand == DominantHandWristSelector.Hand.Left;
        rightToggle.isOn = hand == DominantHandWristSelector.Hand.Right;
        synchronizing = false;
        RefreshHeader();
    }

    private void UpdateLeftText(string value)
    {
        localizedLeft = value;
        leftLabel.text = value;
        RefreshHeader();
    }

    private void UpdateRightText(string value)
    {
        localizedRight = value;
        rightLabel.text = value;
        RefreshHeader();
    }

    private void RefreshHeader()
    {
        // SDK headers copy text only on selection; locale changes must also refresh the closed header.
        if (dropDownGroup.Title != null)
            dropDownGroup.Title.text = selector.CurrentHand == DominantHandWristSelector.Hand.Left
                ? localizedLeft : localizedRight;
    }
}
