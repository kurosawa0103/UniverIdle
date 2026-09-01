using DesktopPet.Background;
using DesktopPet.Environment;
using UnityEngine;
using TMPro;

namespace DesktopPet.Settings
{
    /// <summary>设置页：音频/显示/窗口/环境；壳层由 DesktopHub 管理。控件引用只读 SettingsPage 上的 SettingsPanelBinding。</summary>
    public sealed class SettingsUIController : MonoBehaviour
    {
        [SerializeField] private SettingsApplicator applicator;
        [SerializeField] private SettingsPanelBinding panel;

        private bool _suppressUiCallbacks;
        private bool _weatherDropdownHasRandomOption;
        private System.Collections.Generic.List<WeatherDefinition> _weatherDropdownWeathers = new();

        private EnvironmentApplicator EnvironmentApplicator =>
            applicator != null ? applicator.Environment : null;

        private void Awake()
        {
            if (applicator == null)
                applicator = GetComponent<SettingsApplicator>();

            DesktopPetServices.RegisterSettingsUi(this);
        }

        private void OnEnable()
        {
            if (panel == null)
                return;

            if (panel.resetButton != null)
                panel.resetButton.onClick.AddListener(OnResetClicked);

            if (panel.audioTabButton != null)
                panel.audioTabButton.onClick.AddListener(ShowAudio);
            if (panel.displayTabButton != null)
                panel.displayTabButton.onClick.AddListener(ShowDisplay);
            if (panel.windowTabButton != null)
                panel.windowTabButton.onClick.AddListener(ShowWindow);
            if (panel.environmentTabButton != null)
                panel.environmentTabButton.onClick.AddListener(ShowEnvironment);

            if (panel.masterVolumeSlider != null)
                panel.masterVolumeSlider.onValueChanged.AddListener(OnMasterChanged);
            if (panel.bgmVolumeSlider != null)
                panel.bgmVolumeSlider.onValueChanged.AddListener(OnBgmChanged);
            if (panel.zoomSpeedSlider != null)
                panel.zoomSpeedSlider.onValueChanged.AddListener(OnZoomSpeedChanged);
            if (panel.ignoreZoomOverUiToggle != null)
                panel.ignoreZoomOverUiToggle.onValueChanged.AddListener(OnIgnoreZoomChanged);
            if (panel.alwaysOnTopToggle != null)
                panel.alwaysOnTopToggle.onValueChanged.AddListener(OnAlwaysOnTopChanged);
            if (panel.refreshTransparentCullingButton != null)
                panel.refreshTransparentCullingButton.onClick.AddListener(OnRefreshTransparentCullingClicked);
            if (panel.dayNightPhaseDropdown != null)
                panel.dayNightPhaseDropdown.onValueChanged.AddListener(OnDayNightPhaseChanged);
            if (panel.dayNightAutoCycleToggle != null)
                panel.dayNightAutoCycleToggle.onValueChanged.AddListener(OnDayNightAutoCycleChanged);
            if (panel.weatherDropdown != null)
                panel.weatherDropdown.onValueChanged.AddListener(OnWeatherChanged);
        }

        private void OnDisable()
        {
            if (panel == null)
                return;

            if (panel.resetButton != null)
                panel.resetButton.onClick.RemoveAllListeners();

            if (panel.audioTabButton != null)
                panel.audioTabButton.onClick.RemoveAllListeners();
            if (panel.displayTabButton != null)
                panel.displayTabButton.onClick.RemoveAllListeners();
            if (panel.windowTabButton != null)
                panel.windowTabButton.onClick.RemoveAllListeners();
            if (panel.environmentTabButton != null)
                panel.environmentTabButton.onClick.RemoveAllListeners();

            if (panel.masterVolumeSlider != null)
                panel.masterVolumeSlider.onValueChanged.RemoveAllListeners();
            if (panel.bgmVolumeSlider != null)
                panel.bgmVolumeSlider.onValueChanged.RemoveAllListeners();
            if (panel.zoomSpeedSlider != null)
                panel.zoomSpeedSlider.onValueChanged.RemoveAllListeners();
            if (panel.ignoreZoomOverUiToggle != null)
                panel.ignoreZoomOverUiToggle.onValueChanged.RemoveAllListeners();
            if (panel.alwaysOnTopToggle != null)
                panel.alwaysOnTopToggle.onValueChanged.RemoveAllListeners();
            if (panel.refreshTransparentCullingButton != null)
                panel.refreshTransparentCullingButton.onClick.RemoveAllListeners();
            if (panel.dayNightPhaseDropdown != null)
                panel.dayNightPhaseDropdown.onValueChanged.RemoveAllListeners();
            if (panel.dayNightAutoCycleToggle != null)
                panel.dayNightAutoCycleToggle.onValueChanged.RemoveAllListeners();
            if (panel.weatherDropdown != null)
                panel.weatherDropdown.onValueChanged.RemoveAllListeners();
        }

        private void Start()
        {
            RebuildWeatherDropdownOptions();
            PullFromStoreToUi();
            SubscribeBackgroundWeather();
        }

        private void OnDestroy()
        {
            UnsubscribeBackgroundWeather();
            DesktopPetServices.UnregisterSettingsUi(this);
        }

        private void SubscribeBackgroundWeather()
        {
            if (BackgroundSystem.Instance != null)
                BackgroundSystem.Instance.BackgroundChanged += OnActiveBackgroundChanged;
        }

        private void UnsubscribeBackgroundWeather()
        {
            if (BackgroundSystem.Instance != null)
                BackgroundSystem.Instance.BackgroundChanged -= OnActiveBackgroundChanged;
        }

        private void OnActiveBackgroundChanged(string _)
        {
            if (!IsEnvironmentPageActive())
                return;

            RebuildWeatherDropdownOptions();
            if (panel?.weatherDropdown != null)
                panel.weatherDropdown.SetValueWithoutNotify(GetWeatherDropdownIndexFromStore());
        }

        public void OnPageShown()
        {
            PullFromStoreToUi();
            ShowAudio();
        }

        public void PersistAndSave()
        {
            EnvironmentApplicator?.PersistDayNightState();
            SettingsStore.Save();
        }

        private void ShowAudio() => SetActivePage(panel != null ? panel.audioPage : null);

        private void ShowDisplay() => SetActivePage(panel != null ? panel.displayPage : null);

        private void ShowWindow() => SetActivePage(panel != null ? panel.windowPage : null);

        private void ShowEnvironment()
        {
            SetActivePage(panel != null ? panel.environmentPage : null);
            RebuildWeatherDropdownOptions();
            if (panel?.weatherDropdown != null)
                panel.weatherDropdown.SetValueWithoutNotify(GetWeatherDropdownIndexFromStore());
        }

        private bool IsEnvironmentPageActive()
        {
            return panel != null
                && panel.environmentPage != null
                && panel.environmentPage.activeInHierarchy;
        }

        private void SetActivePage(GameObject active)
        {
            if (panel == null)
                return;

            if (panel.audioPage != null)
                panel.audioPage.SetActive(active == panel.audioPage);
            if (panel.displayPage != null)
                panel.displayPage.SetActive(active == panel.displayPage);
            if (panel.windowPage != null)
                panel.windowPage.SetActive(active == panel.windowPage);
            if (panel.environmentPage != null)
                panel.environmentPage.SetActive(active == panel.environmentPage);
        }

        private void PullFromStoreToUi()
        {
            if (panel == null)
                return;

            _suppressUiCallbacks = true;

            if (panel.masterVolumeSlider != null)
                panel.masterVolumeSlider.SetValueWithoutNotify(SettingsStore.MasterVolume);
            if (panel.bgmVolumeSlider != null)
                panel.bgmVolumeSlider.SetValueWithoutNotify(SettingsStore.BgmVolume);
            if (panel.zoomSpeedSlider != null)
                panel.zoomSpeedSlider.SetValueWithoutNotify(SettingsStore.ZoomSpeed);
            if (panel.ignoreZoomOverUiToggle != null)
                panel.ignoreZoomOverUiToggle.SetIsOnWithoutNotify(SettingsStore.IgnoreZoomOverUi);
            if (panel.alwaysOnTopToggle != null)
                panel.alwaysOnTopToggle.SetIsOnWithoutNotify(SettingsStore.AlwaysOnTop);
            if (panel.dayNightPhaseDropdown != null)
                panel.dayNightPhaseDropdown.SetValueWithoutNotify((int)SettingsStore.DayNightPhase);
            if (panel.dayNightAutoCycleToggle != null)
                panel.dayNightAutoCycleToggle.SetIsOnWithoutNotify(SettingsStore.DayNightAutoCycle);
            if (panel.weatherDropdown != null)
                panel.weatherDropdown.SetValueWithoutNotify(GetWeatherDropdownIndexFromStore());

            RefreshValueLabels();
            _suppressUiCallbacks = false;
        }

        private void RefreshValueLabels()
        {
            if (panel == null)
                return;

            SetPercentLabel(panel.masterVolumeValueText, SettingsStore.MasterVolume);
            SetPercentLabel(panel.bgmVolumeValueText, SettingsStore.BgmVolume);
            if (panel.zoomSpeedValueText != null)
                panel.zoomSpeedValueText.text = SettingsStore.ZoomSpeed.ToString("0.0");
        }

        private static void SetPercentLabel(TextMeshProUGUI label, float value01)
        {
            if (label != null)
                label.text = Mathf.RoundToInt(value01 * 100f) + "%";
        }

        private void OnMasterChanged(float v)
        {
            if (_suppressUiCallbacks || applicator == null || panel == null)
                return;
            applicator.ApplyMasterVolume(v);
            SetPercentLabel(panel.masterVolumeValueText, v);
        }

        private void OnBgmChanged(float v)
        {
            if (_suppressUiCallbacks || applicator == null || panel == null)
                return;
            applicator.ApplyBgmVolume(v);
            SetPercentLabel(panel.bgmVolumeValueText, v);
        }

        private void OnZoomSpeedChanged(float v)
        {
            if (_suppressUiCallbacks || applicator == null || panel == null)
                return;
            applicator.ApplyZoomSpeed(v);
            if (panel.zoomSpeedValueText != null)
                panel.zoomSpeedValueText.text = v.ToString("0.0");
        }

        private void OnIgnoreZoomChanged(bool v)
        {
            if (_suppressUiCallbacks || applicator == null)
                return;
            applicator.ApplyIgnoreZoomOverUi(v);
        }

        private void OnAlwaysOnTopChanged(bool v)
        {
            if (_suppressUiCallbacks || applicator == null)
                return;
            applicator.ApplyAlwaysOnTop(v);
        }

        private void OnRefreshTransparentCullingClicked()
        {
            if (applicator == null)
                return;
            applicator.RefreshTransparentCulling();
        }

        private void OnDayNightPhaseChanged(int index)
        {
            EnvironmentApplicator env = EnvironmentApplicator;
            if (_suppressUiCallbacks || env == null || panel == null)
                return;

            DayNightPhase phase = (DayNightPhase)Mathf.Clamp(index, 0, 2);
            env.ApplyDayNightPhase(phase, fromManual: true);
            if (panel.dayNightAutoCycleToggle != null)
                panel.dayNightAutoCycleToggle.SetIsOnWithoutNotify(false);
        }

        private void OnDayNightAutoCycleChanged(bool enabled)
        {
            if (_suppressUiCallbacks || EnvironmentApplicator == null)
                return;
            EnvironmentApplicator.ApplyDayNightAutoCycle(enabled);
        }

        private void OnWeatherChanged(int index)
        {
            EnvironmentApplicator env = EnvironmentApplicator;
            if (_suppressUiCallbacks || env == null)
                return;

            if (_weatherDropdownWeathers.Count == 0)
                return;

            if (_weatherDropdownHasRandomOption && index == _weatherDropdownWeathers.Count)
            {
                env.ApplyRandomWeather();
                return;
            }

            if (index < 0 || index >= _weatherDropdownWeathers.Count)
                return;

            WeatherDefinition weather = _weatherDropdownWeathers[index];
            if (weather == null)
                return;

            if (!env.ApplyConcreteWeather(weather) && panel.weatherDropdown != null)
                panel.weatherDropdown.SetValueWithoutNotify(GetWeatherDropdownIndexFromStore());
        }

        private void RebuildWeatherDropdownOptions()
        {
            EnvironmentApplicator env = EnvironmentApplicator;
            if (panel == null || panel.weatherDropdown == null || env == null)
                return;

            WeatherCatalog catalog = env.WeatherCatalog;
            panel.weatherDropdown.ClearOptions();
            _weatherDropdownWeathers.Clear();

            BackgroundDefinition def = BackgroundWeatherRules.ResolveActiveDefinition();
            _weatherDropdownWeathers = BackgroundWeatherRules.GetAllowedWeathers(def, catalog);

            if (_weatherDropdownWeathers.Count == 0)
            {
                _weatherDropdownHasRandomOption = false;
                return;
            }

            var options = new System.Collections.Generic.List<TMP_Dropdown.OptionData>();
            for (int i = 0; i < _weatherDropdownWeathers.Count; i++)
            {
                WeatherDefinition w = _weatherDropdownWeathers[i];
                options.Add(new TMP_Dropdown.OptionData(w != null ? w.displayName : "天气"));
            }

            if (_weatherDropdownWeathers.Count > 1)
            {
                options.Add(new TMP_Dropdown.OptionData("随机"));
                _weatherDropdownHasRandomOption = true;
            }
            else
            {
                _weatherDropdownHasRandomOption = false;
            }

            panel.weatherDropdown.AddOptions(options);
        }

        private int GetWeatherDropdownIndexFromStore()
        {
            if (_weatherDropdownWeathers.Count == 0)
                return 0;

            if (SettingsStore.WeatherIsRandom && _weatherDropdownHasRandomOption)
                return _weatherDropdownWeathers.Count;

            for (int i = 0; i < _weatherDropdownWeathers.Count; i++)
            {
                WeatherDefinition w = _weatherDropdownWeathers[i];
                if (w != null && w.weatherId == SettingsStore.ResolvedWeatherId)
                    return i;
            }

            return 0;
        }

        private void OnResetClicked()
        {
            if (applicator != null)
                applicator.ResetToDefaultsAndApply();
            RebuildWeatherDropdownOptions();
            PullFromStoreToUi();
        }
    }
}
