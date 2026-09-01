using DesktopPet.Environment;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DesktopPet.Decor
{
    /// <summary>
    /// 装饰上的小夜灯：仅在 <see cref="DayNightPhase.Night"/> 逐渐点亮，其它时段熄灭。
    /// </summary>
    [DefaultExecutionOrder(20)]
    public sealed class DecorNightLight : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("留空则在子物体中查找 Light2D")]
        private Light2D light2D;

        [SerializeField]
        private Color nightColor = new Color(1f, 0.82f, 0.55f, 1f);

        [SerializeField]
        private float nightIntensity = 1.35f;

        [SerializeField]
        [Tooltip("点亮/熄灭过渡秒数")]
        private float transitionSeconds = 1.2f;

        private DayNightSystem _dayNight;
        private float _currentIntensity;
        private float _targetIntensity;
        private Color _targetColor;
        private bool _subscribed;

        private void Awake()
        {
            if (light2D == null)
                light2D = GetComponentInChildren<Light2D>(true);

            if (light2D != null)
            {
                light2D.intensity = 0f;
                _currentIntensity = 0f;
            }
        }

        private void OnEnable() => Subscribe();

        private void Start()
        {
            Subscribe();
            if (_dayNight != null)
                ApplyPhase(_dayNight.CurrentPhase, immediate: true);
            else
                ApplyPhase(DayNightPhase.Day, immediate: true);
        }

        private void OnDisable()
        {
            Unsubscribe();
            _dayNight = null;
        }

        private void Update()
        {
            if (light2D == null)
                return;

            float duration = Mathf.Max(0.01f, transitionSeconds);
            float t = 1f - Mathf.Exp(-Time.deltaTime * (4f / duration));
            _currentIntensity = Mathf.Lerp(_currentIntensity, _targetIntensity, t);
            light2D.intensity = _currentIntensity;
            light2D.color = Color.Lerp(light2D.color, _targetColor, t);

            if (_currentIntensity <= 0.001f && _targetIntensity <= 0f)
                light2D.intensity = 0f;
        }

        private void Subscribe()
        {
            if (_subscribed)
                return;

            EnvironmentManager env = DesktopPetServices.Environment;
            if (env == null)
                return;

            env.InitializeSystems();
            _dayNight = env.DayNight;
            if (_dayNight == null)
                return;

            _dayNight.PhaseChanged += OnPhaseChanged;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed || _dayNight == null)
            {
                _subscribed = false;
                return;
            }

            _dayNight.PhaseChanged -= OnPhaseChanged;
            _subscribed = false;
        }

        private void OnPhaseChanged(DayNightPhase phase) => ApplyPhase(phase, immediate: false);

        private void ApplyPhase(DayNightPhase phase, bool immediate)
        {
            bool on = phase == DayNightPhase.Night;
            _targetIntensity = on ? nightIntensity : 0f;
            _targetColor = nightColor;

            if (!immediate || light2D == null)
                return;

            _currentIntensity = _targetIntensity;
            light2D.intensity = _currentIntensity;
            light2D.color = _targetColor;
        }
    }
}
