using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DesktopPet.Environment
{
    /// <summary>
    /// 订阅 <see cref="DayNightSystem.PhaseChanged"/>，驱动场景 Global <see cref="Light2D"/>。
    /// 光照参数取自 <see cref="EnvironmentManager.DayNightConfig"/>。
    /// </summary>
    [DefaultExecutionOrder(10)]
    public sealed class DayNightLightPresenter : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("须预绑 Global Light 2D；缺绑只报错，不运行时扫描")]
        private Light2D globalLight2D;

        /// <summary>场景已绑的全局灯（雷闪等表现复用，勿再扫 Light2D）。</summary>
        public Light2D BoundGlobalLight => globalLight2D;

        private DayNightSystem _dayNight;
        private DayNightConfig _config;
        private PhaseLightSettings _current;
        private PhaseLightSettings _from;
        private PhaseLightSettings _to;
        private float _blend;
        private float _blendDuration;
        private bool _blending;
        private bool _hasApplied;
        private bool _subscribed;

        private void OnEnable() => Subscribe();

        private void Start()
        {
            if (!TryBind())
            {
                Debug.LogWarning("[DayNightLight] 缺少 EnvironmentManager / DayNightConfig。");
                return;
            }

            if (globalLight2D == null)
            {
                Debug.LogError("[DayNightLight] 请在 Inspector 绑定 Global Light2D。");
                return;
            }

            Subscribe();

            if (!_hasApplied)
                ApplyPhase(_dayNight.CurrentPhase, immediate: true);
        }

        private void OnDisable()
        {
            Unsubscribe();
            _dayNight = null;
            _config = null;
        }

        private void Update()
        {
            if (!_blending || globalLight2D == null)
                return;

            _blend += Time.deltaTime / _blendDuration;
            if (_blend >= 1f)
            {
                ApplyLight(_to);
                _blending = false;
                return;
            }

            ApplyLight(Lerp(_from, _to, _blend));
        }

        private void Subscribe()
        {
            if (_subscribed || !TryBind())
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

        private bool TryBind()
        {
            EnvironmentManager env = DesktopPetServices.Environment;
            if (env == null)
                env = GetComponent<EnvironmentManager>();
            if (env == null)
                return false;

            env.InitializeSystems();
            _dayNight = env.DayNight;
            _config = env.DayNightConfig;
            return _dayNight != null && _config != null;
        }

        private void OnPhaseChanged(DayNightPhase phase) => ApplyPhase(phase, immediate: false);

        private void ApplyPhase(DayNightPhase phase, bool immediate)
        {
            if (_config == null || globalLight2D == null)
                return;

            PhaseLightSettings target = _config.GetLight(phase);
            float duration = Mathf.Max(0f, _config.lightTransitionSeconds);

            if (immediate || !_hasApplied || duration <= 0.0001f)
            {
                ApplyLight(target);
                _blending = false;
                return;
            }

            _from = _current;
            _to = target;
            _blend = 0f;
            _blendDuration = duration;
            _blending = true;
        }

        private void ApplyLight(PhaseLightSettings settings)
        {
            _current = settings;
            _hasApplied = true;
            globalLight2D.color = settings.lightColor;
            globalLight2D.intensity = settings.lightIntensity;
        }

        private static PhaseLightSettings Lerp(PhaseLightSettings a, PhaseLightSettings b, float t)
        {
            return new PhaseLightSettings
            {
                lightColor = Color.Lerp(a.lightColor, b.lightColor, t),
                lightIntensity = Mathf.Lerp(a.lightIntensity, b.lightIntensity, t)
            };
        }
    }
}
