using UnityEngine;
namespace MRF.Modules.Wrist
{
    [CreateAssetMenu(menuName = "MRF/Modules/Wrist Preset", fileName = "WristPreset")]
    public sealed class WristModulePreset : ScriptableObject
    {
        [SerializeField] private WristModuleSettings settings = new WristModuleSettings();
        public WristModuleSettings CreateSettings() => settings.Copy();
        public void Capture(WristModuleSettings source) => settings = source.Copy();
    }
}
