using UnityEngine;

namespace DesktopPet.Environment
{
    /// <summary>
    /// 订阅 <see cref="WeatherSystem.WeatherChanged"/>，按 <see cref="WeatherDefinition.effectPrefab"/> 开关雨雪粒子。
    /// X 跟随相机；Y = 地面 + spawnHeight；Z 锁天气平面。
    /// </summary>
    [DefaultExecutionOrder(10)]
    public sealed class WeatherFxPresenter : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("跟随用相机；空则用 Camera.main")]
        private Camera followCamera;

        [SerializeField]
        [Tooltip("发射器相对地面的高度（世界 Y = ResolveGroundY + 此值；可被 WeatherDefinition 覆盖）")]
        private float spawnHeightAboveGround = 22f;

        [SerializeField]
        [Tooltip("粒子所在世界 Z；与 DesktopCameraZoom 的 zoomPlaneZ / 精灵平面一致，默认 0")]
        private float weatherPlaneZ;

        private WeatherSystem _weather;
        private WeatherDefinition _currentWeather;
        private GameObject _instance;
        private ParticleSystem _particles;
        private GameObject _activePrefab;
        private bool _subscribed;

        private void OnEnable() => Subscribe();

        private void Start()
        {
            if (!TryBind())
            {
                Debug.LogWarning("[WeatherFx] 缺少 EnvironmentManager / WeatherSystem。");
                return;
            }

            Subscribe();
            ApplyWeather(_weather.CurrentWeather);
        }

        private void OnDisable()
        {
            Unsubscribe();
            _weather = null;
            StopFx();
        }

        private void LateUpdate()
        {
            if (_instance == null)
                return;

            Camera cam = followCamera != null ? followCamera : Camera.main;
            if (cam == null)
                return;

            float groundY = DesktopPetServices.ResolveGroundY();
            float height = spawnHeightAboveGround;
            if (_currentWeather != null && _currentWeather.spawnHeightAboveGround > 0.01f)
                height = _currentWeather.spawnHeightAboveGround;

            _instance.transform.position = new Vector3(
                cam.transform.position.x,
                groundY + height,
                weatherPlaneZ);
        }

        private void Subscribe()
        {
            if (_subscribed || !TryBind())
                return;

            _weather.WeatherChanged += OnWeatherChanged;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed || _weather == null)
            {
                _subscribed = false;
                return;
            }

            _weather.WeatherChanged -= OnWeatherChanged;
            _subscribed = false;
        }

        private bool TryBind()
        {
            EnvironmentManager env = DesktopPetServices.Environment;
            if (env == null)
                env = GetComponent<EnvironmentManager>();
            if (env == null)
                return false;

            env.InitializeSystems();
            _weather = env.Weather;
            return _weather != null;
        }

        private void OnWeatherChanged(WeatherDefinition weather) => ApplyWeather(weather);

        private void ApplyWeather(WeatherDefinition weather)
        {
            _currentWeather = weather;
            GameObject prefab = weather != null ? weather.effectPrefab : null;
            if (prefab == null)
            {
                StopFx();
                return;
            }

            if (_activePrefab == prefab && _instance != null)
            {
                if (_particles != null && !_particles.isPlaying)
                    _particles.Play(true);
                return;
            }

            StopFx();
            _activePrefab = prefab;
            _instance = Instantiate(prefab);
            _instance.name = prefab.name + " (Runtime)";
            _particles = _instance.GetComponent<ParticleSystem>();
            if (_particles == null)
                _particles = _instance.GetComponentInChildren<ParticleSystem>(true);

            if (_particles != null)
                _particles.Play(true);
        }

        private void StopFx()
        {
            if (_particles != null)
            {
                _particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                _particles = null;
            }

            if (_instance != null)
            {
                Destroy(_instance);
                _instance = null;
            }

            _activePrefab = null;
        }
    }
}
