using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

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

