using System;
using UnityEngine;

/// <summary>Scene-owned configuration for watch activation, feedback and replaceable menus.</summary>
[DisallowMultipleComponent]
public sealed class WatchModuleManager : MonoBehaviour
{
    [Serializable]
    public sealed class MenuOption
    {
        public string label = "Menu";
        public GameObject root;
        [Tooltip("Optional presenter for the existing shortcut menu. Enable Controlled By Manager on it.")]
        public WatchShortcutMenuController shortcutPresenter;

        public void SetOpen(bool value)
        {
            if (shortcutPresenter != null) shortcutPresenter.SetOpen(value);
            else if (root != null) root.SetActive(value);
        }
    }

    [SerializeField] private WatchController watch;
    [SerializeField] private bool activationEnabled = true;
    [SerializeField] private WatchController.AnimationMode animationMode;
    [Tooltip("Animator on visual children only. Requires the Boolean parameter below.")]
    [SerializeField] private Animator customAnimator;
    [SerializeField] private string powerParameter = "IsOn";
    [SerializeField] private MenuOption[] menus = Array.Empty<MenuOption>();
    [SerializeField] private int selectedMenu;
    private bool started;

    private void Start()
    {
        if (watch == null)
        {
            Debug.LogError("Watch module requires an explicit Watch reference.", this);
            enabled = false;
            return;
        }
        started = true;
        watch.WhenPowerChanged += RefreshMenus;
        ApplyConfiguration();
    }

    private void OnEnable()
    {
        if (!started) return;
        watch.WhenPowerChanged += RefreshMenus;
        ApplyConfiguration();
    }

    public void SetActivationEnabled(bool value)
    {
        activationEnabled = value;
        if (started && isActiveAndEnabled) ApplyConfiguration();
    }

    public void SelectMenu(int index)
    {
        if (menus == null || index < 0 || index >= menus.Length)
        {
            Debug.LogWarning("Watch menu index is outside the configured options.", this);
            return;
        }
        selectedMenu = index;
        if (started) RefreshMenus(watch.IsOn);
    }

    public void SetAnimationMode(int index)
    {
        if (!Enum.IsDefined(typeof(WatchController.AnimationMode), index)) return;
        animationMode = (WatchController.AnimationMode)index;
        if (started && isActiveAndEnabled) ApplyConfiguration();
    }

    [ContextMenu("Apply Configuration (Play Mode)")]
    public void ApplyConfiguration()
    {
        if (!Application.isPlaying || !started || !isActiveAndEnabled) return;
        var mode = animationMode;
        if (mode == WatchController.AnimationMode.CustomAnimator && !HasPowerParameter())
        {
            Debug.LogWarning("Assign an Animator with the configured Boolean power parameter. Using immediate feedback.", this);
            mode = WatchController.AnimationMode.Immediate;
        }
        watch.ConfigureAnimation(mode, mode == WatchController.AnimationMode.CustomAnimator ? customAnimator : null, powerParameter);
        watch.SetActivationEnabled(activationEnabled);
        RefreshMenus(watch.IsOn);
    }

    private bool HasPowerParameter()
    {
        if (customAnimator == null || customAnimator.runtimeAnimatorController == null) return false;
        foreach (var parameter in customAnimator.parameters)
            if (parameter.name == powerParameter && parameter.type == AnimatorControllerParameterType.Bool) return true;
        return false;
    }

    private void RefreshMenus(bool open)
    {
        if (menus == null) return;
        // Close previous options first, including when two variants share a presenter.
        foreach (var menu in menus) menu?.SetOpen(false);
        if (open && activationEnabled && isActiveAndEnabled && selectedMenu >= 0 && selectedMenu < menus.Length)
            menus[selectedMenu]?.SetOpen(true);
    }

    private void OnDisable()
    {
        if (!started || watch == null) return;
        watch.WhenPowerChanged -= RefreshMenus;
        watch.SetActivationEnabled(false);
        RefreshMenus(false);
    }
}
