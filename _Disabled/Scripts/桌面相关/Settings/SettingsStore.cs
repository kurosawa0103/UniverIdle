using DesktopPet.Environment;
using UnityEngine;

namespace DesktopPet.Settings
{
    /// <summary>
    /// 设置读写（PlayerPrefs）。默认值与 UI / 应用层共用。
    /// </summary>
    public static class SettingsStore
    {
        public const string KeyMasterVolume = "DesktopPet.MasterVolume";
        public const string KeyBgmVolume = "DesktopPet.BgmVolume";
        public const string KeyZoomSpeed = "DesktopPet.ZoomSpeed";
        public const string KeyIgnoreZoomOverUi = "DesktopPet.IgnoreZoomOverUi";
        public const string KeyAlwaysOnTop = "DesktopPet.AlwaysOnTop";
        public const string KeyDayNightPhase = "DesktopPet.DayNightPhase";
        public const string KeyDayNightAutoCycle = "DesktopPet.DayNightAutoCycle";
        public const string KeyDayNightElapsedInPhase = "DesktopPet.DayNightElapsedInPhase";
        public const string KeyWeatherIsRandom = "DesktopPet.WeatherIsRandom";
        public const string KeyResolvedWeatherId = "DesktopPet.ResolvedWeatherId";

        /// <summary>旧键：仅迁移 WeatherIsRandom（值为 3 = 曾经的 Random）。</summary>
        const string KeyWeatherSelectionLegacy = "DesktopPet.WeatherSelection";
        const int LegacyWeatherSelectionRandom = 3;

        public const float DefaultMasterVolume = 0.8f;
        public const float DefaultBgmVolume = 0.75f;
        public const float DefaultZoomSpeed = 2f;
        public const bool DefaultIgnoreZoomOverUi = true;
        public const bool DefaultAlwaysOnTop = true;
        public const DayNightPhase DefaultDayNightPhase = DayNightPhase.Day;
        public const bool DefaultDayNightAutoCycle = true;
        public const float DefaultDayNightElapsedInPhase = 0f;
        public const bool DefaultWeatherIsRandom = false;
        public const string DefaultResolvedWeatherId = "sunny";

        public static float MasterVolume
        {
            get => PlayerPrefs.GetFloat(KeyMasterVolume, DefaultMasterVolume);
            set => PlayerPrefs.SetFloat(KeyMasterVolume, Mathf.Clamp01(value));
        }

        public static float BgmVolume
        {
            get => PlayerPrefs.GetFloat(KeyBgmVolume, DefaultBgmVolume);
            set => PlayerPrefs.SetFloat(KeyBgmVolume, Mathf.Clamp01(value));
        }

        public static float ZoomSpeed
        {
            get => PlayerPrefs.GetFloat(KeyZoomSpeed, DefaultZoomSpeed);
            set => PlayerPrefs.SetFloat(KeyZoomSpeed, Mathf.Clamp(value, 0.2f, 10f));
        }

        public static bool IgnoreZoomOverUi
        {
            get => PlayerPrefs.GetInt(KeyIgnoreZoomOverUi, DefaultIgnoreZoomOverUi ? 1 : 0) != 0;
            set => PlayerPrefs.SetInt(KeyIgnoreZoomOverUi, value ? 1 : 0);
        }

        public static bool AlwaysOnTop
        {
            get => PlayerPrefs.GetInt(KeyAlwaysOnTop, DefaultAlwaysOnTop ? 1 : 0) != 0;
            set => PlayerPrefs.SetInt(KeyAlwaysOnTop, value ? 1 : 0);
        }

        public static DayNightPhase DayNightPhase
        {
            get => (DayNightPhase)PlayerPrefs.GetInt(KeyDayNightPhase, (int)DefaultDayNightPhase);
            set => PlayerPrefs.SetInt(KeyDayNightPhase, (int)value);
        }

        public static bool DayNightAutoCycle
        {
            get => PlayerPrefs.GetInt(KeyDayNightAutoCycle, DefaultDayNightAutoCycle ? 1 : 0) != 0;
            set => PlayerPrefs.SetInt(KeyDayNightAutoCycle, value ? 1 : 0);
        }

        public static float DayNightElapsedInPhase
        {
            get => PlayerPrefs.GetFloat(KeyDayNightElapsedInPhase, DefaultDayNightElapsedInPhase);
            set => PlayerPrefs.SetFloat(KeyDayNightElapsedInPhase, Mathf.Max(0f, value));
        }

        /// <summary>下拉是否停在「随机」；具体天气以 <see cref="ResolvedWeatherId"/> 为准。</summary>
        public static bool WeatherIsRandom
        {
            get
            {
                if (PlayerPrefs.HasKey(KeyWeatherIsRandom))
                    return PlayerPrefs.GetInt(KeyWeatherIsRandom, DefaultWeatherIsRandom ? 1 : 0) != 0;

                bool random = PlayerPrefs.HasKey(KeyWeatherSelectionLegacy) &&
                              PlayerPrefs.GetInt(KeyWeatherSelectionLegacy, 0) == LegacyWeatherSelectionRandom;
                PlayerPrefs.SetInt(KeyWeatherIsRandom, random ? 1 : 0);
                return random;
            }
            set => PlayerPrefs.SetInt(KeyWeatherIsRandom, value ? 1 : 0);
        }

        public static string ResolvedWeatherId
        {
            get => PlayerPrefs.GetString(KeyResolvedWeatherId, DefaultResolvedWeatherId);
            set => PlayerPrefs.SetString(KeyResolvedWeatherId, string.IsNullOrEmpty(value) ? DefaultResolvedWeatherId : value);
        }

        public static void Save()
        {
            PlayerPrefs.Save();
        }

        public static void ResetToDefaults()
        {
            MasterVolume = DefaultMasterVolume;
            BgmVolume = DefaultBgmVolume;
            ZoomSpeed = DefaultZoomSpeed;
            IgnoreZoomOverUi = DefaultIgnoreZoomOverUi;
            AlwaysOnTop = DefaultAlwaysOnTop;
            DayNightPhase = DefaultDayNightPhase;
            DayNightAutoCycle = DefaultDayNightAutoCycle;
            DayNightElapsedInPhase = DefaultDayNightElapsedInPhase;
            WeatherIsRandom = DefaultWeatherIsRandom;
            ResolvedWeatherId = DefaultResolvedWeatherId;
            Save();
        }
    }
}
