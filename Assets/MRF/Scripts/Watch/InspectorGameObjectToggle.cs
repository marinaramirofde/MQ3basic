using UnityEngine;
using UnityEngine.Events;

/// <summary>Toggles a scene object assigned through the Inspector.</summary>
[DisallowMultipleComponent]
public sealed class InspectorGameObjectToggle : MonoBehaviour
{
    [SerializeField] private GameObject target;
    [SerializeField] private bool startInactive = true;
    [SerializeField] private UnityEvent<bool> onToggle = new UnityEvent<bool>();

    public GameObject Target => target;
    public bool IsOn => target != null && target.activeSelf;

    private void Awake()
    {
        if (target == null)
        {
            Debug.LogError("Inspector game object toggle has no target assigned.", this);
            enabled = false;
            return;
        }

        if (startInactive) target.SetActive(false);
    }

    /// <summary>Switches the assigned target and publishes the resulting state.</summary>
    public void OnToggle()
    {
        if (target == null) return;

        bool nextState = !target.activeSelf;
        target.SetActive(nextState);
        onToggle.Invoke(nextState);
    }
}
