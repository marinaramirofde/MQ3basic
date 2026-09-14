using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
namespace MRF.Modules.Wrist
{
    /// <summary>Composes one device, its style and an optional menu using explicit scene bindings.</summary>
    [DefaultExecutionOrder(-150)]
    public sealed class SceneModuleManager : MonoBehaviour
    {
        [SerializeField] private WristModuleSettings settings = new WristModuleSettings();
        [SerializeField] private Transform deviceSlot;
        [SerializeField] private WristDevice[] devices = Array.Empty<WristDevice>();
        [SerializeField] private DeviceAnchoredMenuPresenter[] menus = Array.Empty<DeviceAnchoredMenuPresenter>();
        [SerializeField] private UnityEvent<bool> whenPowerChanged = new UnityEvent<bool>();
        public WristModuleSettings Settings => settings;
        public WristDevice[] Devices => devices;
        public DeviceAnchoredMenuPresenter[] Menus => menus;
        public bool IsOn => state.IsOn;
        public WristDevice ActiveDevice => activeDevice;
        public WristStyle ActiveStyle => activeStyle;
        public DeviceAnchoredMenuPresenter ActiveMenu => activeMenu;
        private readonly WristActivationState state = new WristActivationState();
        private WristDevice activeDevice;
        private WristStyle activeStyle;
        private DeviceAnchoredMenuPresenter activeMenu;
        private bool started;

        private void Start() { started = true; ApplyConfiguration(); }
        private void OnEnable() { if (started) ApplyConfiguration(); }
        private void OnDisable() => Shutdown();

        public void LoadPreset(WristModulePreset preset)
        {
            if (preset == null) return;
            settings = preset.CreateSettings();
            ApplyConfiguration();
        }
        public void SetDeviceEnabled(bool value) { settings.deviceEnabled = value; ApplyConfiguration(); }
        public void SetActivationEnabled(bool value) { settings.activationEnabled = value; ApplyConfiguration(); }
        public void SetAnimationsEnabled(bool value) { settings.animationsEnabled = value; ApplyConfiguration(); }
        public void SetMenuEnabled(bool value) { settings.menuEnabled = value; ApplyConfiguration(); }
        public void SelectStyle(string id) { settings.styleId = id; ApplyConfiguration(); }
        public void SelectMenu(string id) { settings.menuId = id; ApplyConfiguration(); }
        public void SelectDevice(string id)
        {
            var device = FindDevice(id);
            if (device == null) { Debug.LogWarning("Unknown wrist device: " + id, this); return; }
            settings.deviceId = id;
            // A device owns its style catalogue. Retain a compatible choice or select its first style.
            if (device.FindStyle(settings.styleId) == null)
                settings.styleId = device.Styles != null && device.Styles.Length > 0 && device.Styles[0] != null ? device.Styles[0].Id : "";
            ApplyConfiguration();
        }
        public void TogglePower() => SetPower(!state.IsOn);
        public void SetPower(bool value)
        {
            if (!isActiveAndEnabled || !state.SetPower(value)) return;
            activeStyle?.SetPower(state.IsOn, settings.animationsEnabled);
            activeMenu?.SetOpen(state.IsOn && settings.menuEnabled);
            whenPowerChanged.Invoke(state.IsOn);
        }

        public void ApplyConfiguration()
        {
            if (!Application.isPlaying || !started || !isActiveAndEnabled) return;
            if (!ValidateConfiguration(out string error))
            {
                Shutdown();
                Debug.LogError(error, this);
                return;
            }
            bool wasOn = state.IsOn;
            var nextDevice = settings.deviceEnabled ? FindDevice(settings.deviceId) : null;
            bool preservePower = wasOn && nextDevice == activeDevice && settings.activationEnabled;
            ReleaseBindings();
            state.SetAvailable(false);
            if (nextDevice == null)
            {
                if (wasOn) whenPowerChanged.Invoke(false);
                return;
            }
            activeDevice = nextDevice;
            activeDevice.gameObject.SetActive(true);
            activeStyle = activeDevice.FindStyle(settings.styleId);
            if (!activeStyle.Activate(preservePower, false))
            {
                Shutdown();
                if (wasOn) whenPowerChanged.Invoke(false);
                return;
            }
            if (settings.menuEnabled)
            {
                activeMenu = FindMenu(settings.menuId);
                if (!activeMenu.AttachTo(activeDevice.MenuAnchor))
                {
                    Debug.LogError("The selected menu cannot attach to this device.", this);
                    Shutdown();
                    if (wasOn) whenPowerChanged.Invoke(false);
                    return;
                }
            }
            state.SetAvailable(settings.activationEnabled);
            state.SetPower(preservePower);
            if (activeDevice.Input != null)
            {
                activeDevice.Input.Pressed += TogglePower;
                if (!activeDevice.Input.SetInputEnabled(settings.activationEnabled))
                {
                    Shutdown();
                    if (wasOn) whenPowerChanged.Invoke(false);
                    return;
                }
            }
            activeMenu?.SetOpen(state.IsOn);
            if (wasOn != state.IsOn) whenPowerChanged.Invoke(state.IsOn);
        }

        private void ReleaseBindings()
        {
            if (activeDevice != null && activeDevice.Input != null)
            {
                activeDevice.Input.Pressed -= TogglePower;
                activeDevice.Input.SetInputEnabled(false);
            }
            if (activeMenu != null) activeMenu.SetOpen(false);
            if (menus != null) foreach (var menu in menus) if (menu != null) menu.SetOpen(false);
            // Restore the previous style before another style captures the same renderer's materials.
            if (activeStyle != null) activeStyle.Deactivate();
            if (IsOwnedDevice(activeDevice)) activeDevice.gameObject.SetActive(false);
            if (devices != null) foreach (var device in devices)
            {
                if (!IsOwnedDevice(device)) continue;
                device.Input?.SetInputEnabled(false);
                device.gameObject.SetActive(false);
            }
            activeMenu = null;
            activeStyle = null;
            activeDevice = null;
        }
        private bool IsOwnedDevice(WristDevice device) => device != null && deviceSlot != null &&
            device.transform.parent == deviceSlot && !transform.IsChildOf(device.transform);

        private void Shutdown()
        {
            bool changed = state.SetAvailable(false);
            ReleaseBindings();
            if (changed) whenPowerChanged.Invoke(false);
        }
        public WristDevice FindDevice(string id)
        {
            if (devices != null) foreach (var device in devices) if (device != null && device.Id == id) return device;
            return null;
        }
        private DeviceAnchoredMenuPresenter FindMenu(string id)
        {
            if (menus != null) foreach (var menu in menus) if (menu != null && menu.Id == id) return menu;
            return null;
        }
        public bool ValidateConfiguration(out string error)
        {
            error = null;
            if (settings == null || deviceSlot == null) error = "Assign wrist settings and the device slot.";
            var deviceIds = new HashSet<string>();
            if (devices != null) foreach (var device in devices)
            {
                if (device == null || string.IsNullOrWhiteSpace(device.Id) || !deviceIds.Add(device.Id))
                { error = "Device entries require unique IDs and non-null references."; break; }
                if (device.transform == deviceSlot || device.transform.parent != deviceSlot || transform.IsChildOf(device.transform))
                { error = "Each device root must be a direct child of the slot, outside scene services."; break; }
                var styleIds = new HashSet<string>();
                if (device.Styles != null) foreach (var style in device.Styles)
                    if (style == null || string.IsNullOrWhiteSpace(style.Id) || !styleIds.Add(style.Id) || !style.transform.IsChildOf(device.transform))
                        error = "Each device requires distinct, device-local style bindings.";
            }
            var menuIds = new HashSet<string>();
            if (menus != null) foreach (var menu in menus)
                if (menu == null || string.IsNullOrWhiteSpace(menu.Id) || !menuIds.Add(menu.Id) || !menu.IsConfigured ||
                    menu.transform.IsChildOf(transform) ||
                    (deviceSlot != null && menu.transform.IsChildOf(deviceSlot)))
                    error = "Menus require distinct IDs, panel references and a host outside SceneModules and the device slot.";
            if (error != null) return false;
            if (!settings.deviceEnabled) return true;
            var selected = FindDevice(settings.deviceId);
            if (selected == null || selected.FindStyle(settings.styleId) == null)
                error = "Select an installed device and one of its styles.";
            else if (settings.activationEnabled && selected.Input == null)
                error = "The selected device needs an input source when activation is enabled.";
            else if (settings.menuEnabled && (selected.MenuAnchor == null || FindMenu(settings.menuId) == null))
                error = "Select an installed menu and a device with a menu anchor.";
            return error == null;
        }
    }
}
