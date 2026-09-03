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

    private AsyncOperationHandle<Locale> initializationOperation;

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
        LocalizationSettings.SelectedLocaleChanged += SynchronizeSelection;
    }

    private void Start()
    {
        // SelectedLocaleAsync waits until Unity Localization has loaded its locales.
        initializationOperation = LocalizationSettings.SelectedLocaleAsync;
        if (initializationOperation.IsDone)
        {
            SynchronizeSelection(initializationOperation.Result);
        }
        else
        {
            initializationOperation.Completed += OnLocalizationInitialized;
        }
    }

    private void OnDisable()
    {
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

    private void OnLocalizationInitialized(AsyncOperationHandle<Locale> operation)
    {
        SynchronizeSelection(operation.Result);
    }

    private void SynchronizeSelection(Locale locale)
    {
        Toggle selectedToggle = locale == spanishLocale ? spanishToggle : englishToggle;

        if (selectedToggle != null && !selectedToggle.isOn)
        {
            // Notify DropDownGroup so its selected index and visible header stay synchronized.
            selectedToggle.isOn = true;
        }
    }
}
