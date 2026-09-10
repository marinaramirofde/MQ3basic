using Oculus.Interaction.Samples;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

/// <summary>
/// Maps the language dropdown options to explicitly assigned Unity Locales.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-100)]
public sealed class LanguageDropdownAdapter : MonoBehaviour
{
    private const string SelectedLocalePreferenceKey = "selected-locale";

    [Header("Explicit References")]
    [SerializeField] private DropDownGroup dropDownGroup;
    [SerializeField] private Locale englishLocale;
    [SerializeField] private Locale spanishLocale;

    [Header("Options (Locale Order)")]
    [SerializeField] private Toggle englishToggle;
    [SerializeField] private Toggle spanishToggle;

    private AsyncOperationHandle<LocalizationSettings> initializationOperation;
    private bool localizationReady;
    private bool synchronizingSelection;

    private void Awake()
    {
        if (dropDownGroup == null || spanishLocale == null || englishLocale == null ||
            spanishToggle == null || englishToggle == null)
        {
            Debug.LogError("Assign every Language dropdown reference explicitly.", this);
            enabled = false;
            return;
        }

        // Keep English first because it is also the project fallback language.
        dropDownGroup.InjectToggles(new[] { englishToggle, spanishToggle });
    }

    private void OnEnable()
    {
        localizationReady = false;
        LocalizationSettings.SelectedLocaleChanged += SynchronizeSelection;
        // Locale selection completes before table preloading. Wait for BOTH before
        // allowing UI callbacks to change locale and invalidate the preload handle.
        initializationOperation = LocalizationSettings.InitializationOperation;
        if (initializationOperation.IsDone)
            OnLocalizationInitialized(initializationOperation);
        else
            initializationOperation.Completed += OnLocalizationInitialized;
    }

    private void OnDisable()
    {
        localizationReady = false;
        LocalizationSettings.SelectedLocaleChanged -= SynchronizeSelection;

        if (initializationOperation.IsValid() && !initializationOperation.IsDone)
        {
            initializationOperation.Completed -= OnLocalizationInitialized;
        }
    }

    /// <summary>
    /// Connect DropDownGroup.WhenSelectionChanged(Int32) to this method in the Inspector.
    /// </summary>
    public void SetLanguage(int selectedIndex)
    {
        if (!isActiveAndEnabled || !localizationReady || synchronizingSelection) return;

        Locale selectedLocale = selectedIndex switch
        {
            0 => englishLocale,
            1 => spanishLocale,
            _ => null
        };

        if (selectedLocale != null && LocalizationSettings.SelectedLocale != selectedLocale)
        {
            LocalizationSettings.SelectedLocale = selectedLocale;
            PlayerPrefs.SetString(SelectedLocalePreferenceKey, selectedLocale.Identifier.Code);
            PlayerPrefs.Save();
        }
    }

    private void OnLocalizationInitialized(AsyncOperationHandle<LocalizationSettings> operation)
    {
        if (!isActiveAndEnabled || operation.Status != AsyncOperationStatus.Succeeded) return;
        localizationReady = true;
        SynchronizeSelection(LocalizationSettings.SelectedLocale);
    }

    private void SynchronizeSelection(Locale locale)
    {
        if (!localizationReady || locale == null) return;

        Toggle selectedToggle = locale == spanishLocale ? spanishToggle : englishToggle;

        if (selectedToggle != null && !selectedToggle.isOn)
        {
            // Notify DropDownGroup so its selected index and visible header stay synchronized.
            synchronizingSelection = true;
            try { selectedToggle.isOn = true; }
            finally { synchronizingSelection = false; }
        }
    }
}
