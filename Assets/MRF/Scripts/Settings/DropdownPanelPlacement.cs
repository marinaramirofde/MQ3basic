using UnityEngine;
using UnityEngine.UI;

/// <summary>Keeps a Meta UI Set popup inside the panel's finite ray interaction surface.</summary>
[DisallowMultipleComponent]
public sealed class DropdownPanelPlacement : MonoBehaviour
{
    [Header("Explicit UI References")]
    [SerializeField] private RectTransform popup;
    [SerializeField] private RectTransform header;
    [SerializeField] private RectTransform interactionBounds;
    [Tooltip("Inset and header spacing, in interaction-surface UI units.")]
    [SerializeField, Min(0f)] private float margin = 6f;

    private readonly Vector3[] corners = new Vector3[4];

    /// <summary>Connect the header Toggle's dynamic Boolean event here, after Meta's visibility event.</summary>
    public void SetOpen(bool isOpen)
    {
        if (!isOpen) return;
        if (popup == null || header == null || interactionBounds == null)
        {
            Debug.LogError("Assign popup, header, and interaction bounds for dropdown placement.", this);
            return;
        }

        // Resolve the ContentSizeFitter before measuring; no per-frame searches or layout polling.
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(popup);
        Rect list = GetRect(popup);
        Rect button = GetRect(header);
        Rect area = interactionBounds.rect;
        area.xMin += margin;
        area.xMax -= margin;
        area.yMin += margin;
        area.yMax -= margin;

        if (list.width > area.width || list.height > area.height)
        {
            Debug.LogWarning("Dropdown exceeds its interaction panel. Reduce the option count or provide a scrollable list.", this);
        }

        float top = button.yMin - margin;
        if (top - list.height < area.yMin)
        {
            // Bottom-row controls open above the header while keeping option order unchanged.
            top = button.yMax + margin + list.height;
        }
        top = Mathf.Clamp(top, area.yMin + Mathf.Min(list.height, area.height), area.yMax);
        float left = Mathf.Clamp(button.xMin, area.xMin, Mathf.Max(area.xMin, area.xMax - list.width));
        Vector3 offset = new Vector3(left - list.xMin, top - list.yMax, 0f);
        popup.position += interactionBounds.TransformVector(offset);
    }

    private Rect GetRect(RectTransform target)
    {
        // Measure in the surface's coordinates so world-space panel scale and rotation are respected.
        target.GetWorldCorners(corners);
        Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        foreach (Vector3 corner in corners)
        {
            Vector2 point = interactionBounds.InverseTransformPoint(corner);
            min = Vector2.Min(min, point);
            max = Vector2.Max(max, point);
        }
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }
}
