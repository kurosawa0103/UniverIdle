using System;
using UnityEngine;

namespace DesktopPet.Environment
{
    /// <summary>单个昼夜阶段的 Global Light2D 参数。</summary>
    [Serializable]
    public struct PhaseLightSettings
    {
        [Tooltip("Light2D 颜色")]
        public Color lightColor;

        [Min(0f)]
        [Tooltip("Light2D 强度")]
        public float lightIntensity;

        public static PhaseLightSettings DayDefault => new PhaseLightSettings
        {
            lightColor = new Color(1f, 0.98f, 0.92f, 1f),
            lightIntensity = 1f
        };

        public static PhaseLightSettings DuskDefault => new PhaseLightSettings
        {
            lightColor = new Color(1f, 0.45f, 0.2f, 1f),
            lightIntensity = 0.75f
        };

        public static PhaseLightSettings NightDefault => new PhaseLightSettings
        {
            lightColor = new Color(0.25f, 0.35f, 0.75f, 1f),
            lightIntensity = 0.28f
        };
    }

    [CreateAssetMenu(menuName = "桌宠/环境/昼夜配置", fileName = "DayNightConfig")]
    public sealed class DayNightConfig : ScriptableObject
    {
        [Header("阶段时长（秒）")]
        [Min(1f)]
        public float dayDuration = 300f;

        [Min(1f)]
        public float duskDuration = 300f;

        [Min(1f)]
        public float nightDuration = 300f;

        [Header("Global Light2D")]
        public PhaseLightSettings dayLight = PhaseLightSettings.DayDefault;
        public PhaseLightSettings duskLight = PhaseLightSettings.DuskDefault;
        public PhaseLightSettings nightLight = PhaseLightSettings.NightDefault;

        [Min(0f)]
        [Tooltip("阶段切换时颜色/强度插值秒数；0 = 瞬间")]
        public float lightTransitionSeconds = 0.8f;

        public float GetDuration(DayNightPhase phase)
        {
            switch (phase)
            {
                case DayNightPhase.Dusk:
                    return duskDuration;
                case DayNightPhase.Night:
                    return nightDuration;
                default:
                    return dayDuration;
            }
        }

        public DayNightPhase GetNextPhase(DayNightPhase phase)
        {
            switch (phase)
            {
                case DayNightPhase.Day:
                    return DayNightPhase.Dusk;
                case DayNightPhase.Dusk:
                    return DayNightPhase.Night;
                default:
                    return DayNightPhase.Day;
            }
        }

        public PhaseLightSettings GetLight(DayNightPhase phase)
        {
            switch (phase)
            {
                case DayNightPhase.Dusk:
                    return duskLight;
                case DayNightPhase.Night:
                    return nightLight;
                default:
                    return dayLight;
            }
        }
    }
}
