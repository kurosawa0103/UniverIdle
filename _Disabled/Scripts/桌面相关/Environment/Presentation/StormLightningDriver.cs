using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DesktopPet.Environment
{
    /// <summary>
    /// 雷雨天挂在风暴粒子预制体上：间歇闪一下 Global Light2D。
    /// 灯取自场景 <see cref="DayNightLightPresenter.BoundGlobalLight"/>（与昼夜同一盏）。
    /// </summary>
    [DefaultExecutionOrder(20)]
    public sealed class StormLightningDriver : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("可选覆盖；空则用 DayNightLightPresenter 已绑的全局灯")]
        private Light2D globalLight2D;

        [SerializeField]
        private float minInterval = 2.5f;

        [SerializeField]
        private float maxInterval = 7f;

        [SerializeField]
        private float flashDuration = 0.07f;

        [SerializeField]
        private float flashIntensity = 3.2f;

        [SerializeField]
        private Color flashColor = new Color(0.92f, 0.95f, 1f, 1f);

        private Light2D _light;
        private float _baseIntensity;
        private Color _baseColor;
        private float _nextFlashAt;
        private float _flashEndsAt;
        private bool _flashing;

        private void OnEnable()
        {
            _light = ResolveGlobalLight();
            if (_light == null)
                Debug.LogError("[StormLightning] 未找到已绑定的 Global Light2D（请绑 DayNightLightPresenter）。", this);

            _flashing = false;
            ScheduleNext();
        }

        private void OnDisable()
        {
            RestoreLight();
            _light = null;
        }

        private void LateUpdate()
        {
            if (_light == null)
                return;

            float now = Time.time;
            if (!_flashing && now >= _nextFlashAt)
            {
                _baseIntensity = _light.intensity;
                _baseColor = _light.color;
                _flashing = true;
                _flashEndsAt = now + Mathf.Max(0.02f, flashDuration);
            }

            if (!_flashing)
                return;

            _light.intensity = flashIntensity;
            _light.color = flashColor;

            if (now >= _flashEndsAt)
            {
                RestoreLight();
                ScheduleNext();
            }
        }

        private Light2D ResolveGlobalLight()
        {
            if (globalLight2D != null)
                return globalLight2D;

            EnvironmentManager env = DesktopPetServices.Environment;
            if (env == null)
            {
                Debug.LogError("[StormLightning] 未找到 EnvironmentManager（Services 未注册）。", this);
                return null;
            }

            DayNightLightPresenter presenter = env.GetComponent<DayNightLightPresenter>();
            if (presenter == null)
            {
                Debug.LogError("[StormLightning] EnvironmentSystem 上缺少 DayNightLightPresenter。", this);
                return null;
            }

            return presenter.BoundGlobalLight;
        }

        private void ScheduleNext()
        {
            float min = Mathf.Max(0.5f, minInterval);
            float max = Mathf.Max(min, maxInterval);
            _nextFlashAt = Time.time + Random.Range(min, max);
        }

        private void RestoreLight()
        {
            if (!_flashing || _light == null)
            {
                _flashing = false;
                return;
            }

            _light.intensity = _baseIntensity;
            _light.color = _baseColor;
            _flashing = false;
        }
    }
}
