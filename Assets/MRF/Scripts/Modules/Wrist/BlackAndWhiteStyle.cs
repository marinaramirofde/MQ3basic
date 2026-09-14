using UnityEngine;
namespace MRF.Modules.Wrist
{
    public sealed class BlackAndWhiteStyle : WristStyle
    {
        [SerializeField] private Renderer screen;
        [SerializeField] private Shader unlitShader;
        private Material[] originals;
        private Material material;
        public override bool Activate(bool on, bool animate)
        {
            if (screen == null || unlitShader == null)
            {
                Debug.LogError("BlackAndWhite requires a screen renderer and an unlit shader.", this);
                return false;
            }
            if (material == null)
            {
                originals = screen.sharedMaterials;
                material = new Material(unlitShader);
                var replacements = new Material[originals.Length];
                for (int i = 0; i < replacements.Length; i++) replacements[i] = material;
                screen.sharedMaterials = replacements;
            }
            SetPower(on, false);
            return true;
        }
        public override void SetPower(bool on, bool animate)
        {
            if (material == null) return;
            Color color = on ? Color.white : Color.black;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        }
        public override void Deactivate()
        {
            if (screen != null && originals != null) screen.sharedMaterials = originals;
            originals = null;
            if (material != null) Destroy(material);
            material = null;
        }
    }
}
