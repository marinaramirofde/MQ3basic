using System;
using UnityEngine;
namespace MRF.Modules.Wrist
{
    /// <summary>Device-local input adapter; emits presses without owning power or visuals.</summary>
    public abstract class WristInputSource : MonoBehaviour
    {
        public event Action Pressed;
        protected void RaisePressed() => Pressed?.Invoke();
        public abstract bool SetInputEnabled(bool value);
    }
}
