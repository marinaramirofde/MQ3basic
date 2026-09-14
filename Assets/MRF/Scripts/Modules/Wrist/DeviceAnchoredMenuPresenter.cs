using UnityEngine;
namespace MRF.Modules.Wrist
{
    /// <summary>Menu visibility and attachment. Content and visual styling belong to its own prefab.</summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "MRF.Modules.Wrist", sourceAssembly: "Assembly-CSharp", sourceClassName: "WristMenuModule")]
    public sealed class DeviceAnchoredMenuPresenter : MonoBehaviour
    {
        [SerializeField, HideInInspector] private string menuId = "shortcuts";
        [SerializeField, HideInInspector] private string displayName = "Shortcuts";
        [SerializeField] private Transform attachment;
        [SerializeField] private RectTransform panel;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Vector3 offsetMeters;
        [SerializeField] private Vector3 rotationOffset;
        [SerializeField, Min(0.01f)] private float widthMeters = 0.12f;
        private void Reset() => menuId = System.Guid.NewGuid().ToString("N");
        public string Id => menuId;
        public string DisplayName => gameObject.name;
        public bool IsOpen { get; private set; }
        public Transform Attachment => attachment;
        public bool IsConfigured => attachment != null && panel != null && canvasGroup != null &&
            panel.transform.IsChildOf(attachment) && panel.transform != transform && !transform.IsChildOf(panel) &&
            transform != attachment && !transform.IsChildOf(attachment);
        public bool AttachTo(Transform anchor)
        {
            if (!IsConfigured || anchor == null || anchor == attachment || anchor.IsChildOf(attachment)) return false;
            attachment.SetParent(anchor, false);
            attachment.position = anchor.position + anchor.rotation * offsetMeters;
            attachment.rotation = anchor.rotation * Quaternion.Euler(rotationOffset);
            attachment.localScale = Vector3.one;
            float scale = widthMeters / Mathf.Max(1f, panel.rect.width);
            Vector3 parentScale = panel.parent != null ? panel.parent.lossyScale : Vector3.one;
            panel.localScale = new Vector3(scale / Mathf.Max(0.00001f, Mathf.Abs(parentScale.x)),
                scale / Mathf.Max(0.00001f, Mathf.Abs(parentScale.y)), scale / Mathf.Max(0.00001f, Mathf.Abs(parentScale.z)));
            return true;
        }
        public void SetOpen(bool value)
        {
            IsOpen = value && IsConfigured;
            if (!IsConfigured) return;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = IsOpen ? 1 : 0;
                canvasGroup.interactable = IsOpen;
                canvasGroup.blocksRaycasts = IsOpen;
            }
            if (panel != null) panel.gameObject.SetActive(IsOpen);
        }
        private void OnDisable() => SetOpen(false);
    }
}
