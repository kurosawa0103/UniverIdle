using DesktopPet.Background;
using DesktopPet.Environment;
using UnityEngine;

namespace DesktopPet.Settings
{
    /// <summary>把环境相关设置应用到 EnvironmentManager。</summary>
    public sealed class EnvironmentApplicator : MonoBehaviour
    {
        [SerializeField]
        private EnvironmentManager environmentManager;

        private void Awake()
        {
            EnsureEnvironmentManager();
        }

        private void Start()
        {
            ApplyAllEnvironment();
            SubscribeBackgroundWeather();
        }

        private void OnDestroy()
        {
            UnsubscribeBackgroundWeather();
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
            SyncWeatherToActiveBackground();
        }

        public void ApplyAllEnvironment()
        {
            EnvironmentManager env = EnsureEnvironmentManager();
            if (env == null)
                return;

            env.ApplyPersistedState(
                SettingsStore.DayNightPhase,
                SettingsStore.DayNightAutoCycle,
                SettingsStore.DayNightElapsedInPhase,
                SettingsStore.ResolvedWeatherId);
        }

        public void ApplyDayNightPhase(DayNightPhase phase, bool fromManual)
        {
            SettingsStore.DayNightPhase = phase;
            if (fromManual)
            {
                SettingsStore.DayNightAutoCycle = false;
                SettingsStore.DayNightElapsedInPhase = 0f;
            }

            EnvironmentManager env = EnsureEnvironmentManager();
            if (env != null)
                env.SetDayNightPhase(phase, fromManual);
        }

        public void ApplyDayNightAutoCycle(bool enabled)
        {
            SettingsStore.DayNightAutoCycle = enabled;
            SettingsStore.DayNightElapsedInPhase = 0f;

            EnvironmentManager env = EnsureEnvironmentManager();
            if (env != null)
                env.SetDayNightAutoCycle(enabled);
        }

        public bool ApplyRandomWeather()
        {
            EnvironmentManager env = EnsureEnvironmentManager();
            WeatherCatalog catalog = WeatherCatalog;
            if (env?.Weather == null || catalog == null)
                return false;

            BackgroundDefinition def = BackgroundWeatherRules.ResolveActiveDefinition();
            WeatherDefinition picked = BackgroundWeatherRules.PickRandomAllowed(def, catalog);
            if (picked == null)
                return false;

            SettingsStore.WeatherIsRandom = true;
            SettingsStore.ResolvedWeatherId = picked.weatherId;
            env.Weather.SetConcreteWeather(picked);
            return true;
        }

        public bool ApplyConcreteWeather(WeatherDefinition weather)
        {
            if (weather == null)
                return false;

            BackgroundDefinition def = BackgroundWeatherRules.ResolveActiveDefinition();
            if (!BackgroundWeatherRules.IsWeatherAllowed(def, weather))
            {
                Debug.LogWarning(
                    $"[Environment] 天气「{weather.displayName}」在当前背景不可用。",
                    this);
                return false;
            }

            SettingsStore.WeatherIsRandom = false;
            SettingsStore.ResolvedWeatherId = weather.weatherId;

            EnvironmentManager env = EnsureEnvironmentManager();
            env?.Weather?.SetConcreteWeather(weather);
            return true;
        }

        /// <summary>切背景后：把当前天气 clamp 到该背景允许列表（或重掷随机）。</summary>
        public void SyncWeatherToActiveBackground()
        {
            EnvironmentManager env = EnsureEnvironmentManager();
            WeatherCatalog catalog = WeatherCatalog;
            if (env?.Weather == null || catalog == null)
                return;

            BackgroundDefinition def = BackgroundWeatherRules.ResolveActiveDefinition();

            if (SettingsStore.WeatherIsRandom)
            {
                ApplyRandomWeather();
                return;
            }

            WeatherDefinition resolved = BackgroundWeatherRules.ResolveAllowedById(
                def,
                catalog,
                SettingsStore.ResolvedWeatherId);

            if (resolved == null)
                return;

            SettingsStore.WeatherIsRandom = false;
            SettingsStore.ResolvedWeatherId = resolved.weatherId;
            env.Weather.SetConcreteWeather(resolved);
        }

        public WeatherCatalog WeatherCatalog =>
            EnsureEnvironmentManager()?.WeatherCatalog;

        public void PersistDayNightState()
        {
            EnvironmentManager env = EnsureEnvironmentManager();
            if (env?.DayNight == null)
                return;

            SettingsStore.DayNightPhase = env.DayNight.CurrentPhase;
            SettingsStore.DayNightAutoCycle = env.DayNight.AutoCycleEnabled;
            SettingsStore.DayNightElapsedInPhase = env.DayNight.ElapsedInPhase;
        }

        private EnvironmentManager EnsureEnvironmentManager()
        {
            if (environmentManager == null)
                environmentManager = DesktopPetServices.Environment;
            return environmentManager;
        }
    }
}
