using UnityEngine;
namespace MRF.Modules.Wrist
{
    /// <summary>A device-local visual implementation. Owns its materials, effects and animation.</summary>
    public abstract class WristStyle : MonoBehaviour
    {
        [SerializeField, HideInInspector] private string styleId;
        [SerializeField, HideInInspector] private string displayName;
        protected virtual void Reset() => styleId = System.Guid.NewGuid().ToString("N");
        public string Id => styleId;
        public string DisplayName => gameObject.name;
        public virtual bool IsAnimating => false;
        public abstract bool Activate(bool on, bool animate);
        public abstract void SetPower(bool on, bool animate);
        public abstract void Deactivate();
        protected virtual void OnDisable() => Deactivate();
    }
}
