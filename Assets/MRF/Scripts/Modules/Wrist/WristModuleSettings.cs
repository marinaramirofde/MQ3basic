using System;
using UnityEngine;

namespace MRF.Modules.Wrist
{
    [Serializable]
    public sealed class WristModuleSettings
    {
        public bool deviceEnabled = true;
        public bool activationEnabled = true;
        public bool animationsEnabled = true;
        public bool menuEnabled = true;
        [HideInInspector] public string deviceId = "watch";
        [HideInInspector] public string styleId = "led-cubes";
        [HideInInspector] public string menuId = "shortcuts";
        public WristModuleSettings Copy() => JsonUtility.FromJson<WristModuleSettings>(JsonUtility.ToJson(this));
    }

    /// <summary>Pure activation state; knows nothing about devices, styles, or menus.</summary>
    public sealed class WristActivationState
    {
        public bool IsOn { get; private set; }
        public bool CanActivate { get; private set; }
        public bool SetAvailable(bool available)
        {
            CanActivate = available;
            return !available && SetPower(false);
        }
        public bool SetPower(bool on)
        {
            if ((on && !CanActivate) || IsOn == on) return false;
            IsOn = on;
            return true;
        }
    }
}
