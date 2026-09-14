using System;
using UnityEngine;
namespace MRF.Modules.Wrist
{
    /// <summary>Prefab-local bindings. A watch, band or bracelet supplies its own compatible styles.</summary>
    public sealed class WristDevice : MonoBehaviour
    {
        [SerializeField, HideInInspector] private string deviceId = "watch";
        [SerializeField, HideInInspector] private string displayName = "Watch";
        [SerializeField] private WristInputSource input;
        [SerializeField] private Transform menuAnchor;
        [SerializeField] private WristStyle[] styles = Array.Empty<WristStyle>();
        private void Reset() => deviceId = System.Guid.NewGuid().ToString("N");
        public string Id => deviceId;
        public string DisplayName => gameObject.name;
        public WristInputSource Input => input;
        public Transform MenuAnchor => menuAnchor;
        public WristStyle[] Styles => styles;
        public WristStyle FindStyle(string id)
        {
            if (styles != null) foreach (var style in styles) if (style != null && style.Id == id) return style;
            return null;
        }
    }
}
