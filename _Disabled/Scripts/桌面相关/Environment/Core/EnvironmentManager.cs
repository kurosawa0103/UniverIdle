using UnityEngine;

namespace DesktopPet.Environment
{
    /// <summary>
    /// 昼夜与天气运行时门面。场景挂 EnvironmentSystem 并绑定配置资产。
    /// 表现层请订阅 <see cref="DayNight"/>.<c>PhaseChanged</c> / <see cref="Weather"/>.<c>WeatherChanged</c>。
    /// </summary>
    public sealed class EnvironmentManager : MonoBehaviour
    {
        [SerializeField]
        private DayNightConfig dayNightConfig;

        [SerializeField]
        private WeatherCatalog weatherCatalog;

        private DayNightSystem _dayNight;
        private WeatherSystem _weather;
        private bool _initialized;

        public DayNightConfig DayNightConfig => dayNightConfig;
        public WeatherCatalog WeatherCatalog => weatherCatalog;
        public DayNightSystem DayNight => _dayNight;
        public WeatherSystem Weather => _weather;

        private void Awake()
        {
            if (DesktopPetServices.Environment != null && DesktopPetServices.Environment != this)
            {
                Debug.LogWarning("[EnvironmentManager] 场景中已有 EnvironmentManager，销毁重复实例。");
                Destroy(gameObject);
                return;
            }

            DesktopPetServices.RegisterEnvironment(this);
            InitializeSystems();
        }

        private void OnDestroy()
        {
            DesktopPetServices.UnregisterEnvironment(this);
        }

        private void Update()
        {
            if (_dayNight != null)
                _dayNight.Tick(Time.deltaTime);
        }

        public void InitializeSystems()
        {
            if (_initialized)
                return;

            if (dayNightConfig == null)
                Debug.LogWarning("[EnvironmentManager] 未绑定 DayNightConfig。");
            if (weatherCatalog == null)
                Debug.LogWarning("[EnvironmentManager] 未绑定 WeatherCatalog。");

            _dayNight = new DayNightSystem(dayNightConfig);
            _weather = new WeatherSystem(weatherCatalog);
            _initialized = true;
        }

        public void ApplyPersistedState(
            DayNightPhase phase,
            bool autoCycleEnabled,
            float elapsedInPhase,
            string resolvedWeatherId)
        {
            InitializeSystems();

            _dayNight.RestoreState(phase, autoCycleEnabled, elapsedInPhase);
            _weather.RestoreConcreteWeather(resolvedWeatherId);
        }

        public void SetDayNightPhase(DayNightPhase phase, bool fromManual)
        {
            InitializeSystems();
            _dayNight.SetPhase(phase, fromManual);
        }

        public void SetDayNightAutoCycle(bool enabled)
        {
            InitializeSystems();
            _dayNight.SetAutoCycleEnabled(enabled);
        }
    }
}
