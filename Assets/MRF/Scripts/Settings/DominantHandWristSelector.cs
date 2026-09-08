using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>Owns the player's preferred hand and attaches the watch to its calibrated mount.</summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-200)]
public sealed class DominantHandWristSelector : MonoBehaviour
{
    public enum Hand { Left = 0, Right = 1 }
    private const string PreferenceKey = "dominant-hand";

    [Header("Wrist Mounts")]
    [Tooltip("Visual root only. Never assign the rig or a tracking anchor.")]
    [SerializeField] private Transform watchVisual;
    [SerializeField] private Transform leftWatchMount;
    [SerializeField] private Transform rightWatchMount;
    [SerializeField] private Hand defaultHand = Hand.Left;

    [Header("Optional tool/menu integrations (0 = Left, 1 = Right)")]
    [SerializeField] private UnityEvent<int> whenHandChanged = new UnityEvent<int>();

    public Hand CurrentHand { get; private set; }
    public event Action<Hand> HandChanged;

    private void Awake()
    {
        if (watchVisual == null || leftWatchMount == null || rightWatchMount == null ||
            leftWatchMount == rightWatchMount || leftWatchMount == watchVisual ||
            rightWatchMount == watchVisual || leftWatchMount.IsChildOf(watchVisual) ||
            rightWatchMount.IsChildOf(watchVisual))
        {
            Debug.LogError("Assign a watch visual and two distinct mounts outside its hierarchy.", this);
            enabled = false;
            return;
        }

        int savedHand = PlayerPrefs.GetInt(PreferenceKey, (int)defaultHand);
        // Invalid stored values fall back to the Inspector default without overwriting a valid preference.
        CurrentHand = savedHand == 0 || savedHand == 1 ? (Hand)savedHand : defaultHand;
        AttachWatch();
    }

    private void Start() => whenHandChanged.Invoke((int)CurrentHand);

    /// <summary>Inspector entry point. Invalid dropdown indices never change the preference.</summary>
    public void SetDominantHand(int index)
    {
        if (!enabled || index < 0 || index > 1 || CurrentHand == (Hand)index) return;

        CurrentHand = (Hand)index;
        AttachWatch();
        PlayerPrefs.SetInt(PreferenceKey, index);
        PlayerPrefs.Save();
        HandChanged?.Invoke(CurrentHand);
        whenHandChanged.Invoke(index);
        Debug.Log($"Dominant hand changed to {CurrentHand}.", this);
    }

    private void AttachWatch()
    {
        // The mount owns the per-wrist offset; preserve the visual's authored model scale.
        watchVisual.SetParent(CurrentHand == Hand.Left ? leftWatchMount : rightWatchMount, false);
        watchVisual.localPosition = Vector3.zero;
        watchVisual.localRotation = Quaternion.identity;
    }
}
