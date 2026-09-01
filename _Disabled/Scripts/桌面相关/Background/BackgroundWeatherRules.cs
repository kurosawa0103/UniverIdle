using System.Collections.Generic;
using DesktopPet.Environment;
using UnityEngine;

namespace DesktopPet.Background
{
    /// <summary>当前背景下允许切换/随机的天气；透明桌面不受限。</summary>
    public static class BackgroundWeatherRules
    {
        public static BackgroundDefinition ResolveActiveDefinition()
        {
            BackgroundSystem sys = BackgroundSystem.Instance;
            if (sys?.Catalog == null)
                return null;

            if (sys.CurrentBackgroundId == BackgroundDefinition.TransparentId)
                return null;

            return sys.Catalog.FindById(sys.CurrentBackgroundId);
        }

        public static bool IsWeatherAllowed(BackgroundDefinition def, WeatherDefinition weather)
        {
            if (weather == null)
                return false;

            if (def == null)
                return true;

            if (def.allowedWeathers == null || def.allowedWeathers.Count == 0)
                return false;

            for (int i = 0; i < def.allowedWeathers.Count; i++)
            {
                WeatherDefinition w = def.allowedWeathers[i];
                if (w != null && w.weatherId == weather.weatherId)
                    return true;
            }

            return false;
        }

        public static List<WeatherDefinition> GetAllowedWeathers(
            BackgroundDefinition def,
            WeatherCatalog catalog)
        {
            var result = new List<WeatherDefinition>();
            if (def == null)
            {
                if (catalog?.weathers != null)
                {
                    for (int i = 0; i < catalog.weathers.Count; i++)
                    {
                        WeatherDefinition w = catalog.weathers[i];
                        if (w != null)
                            result.Add(w);
                    }
                }

                return result;
            }

            if (def.allowedWeathers == null)
                return result;

            for (int i = 0; i < def.allowedWeathers.Count; i++)
            {
                WeatherDefinition w = def.allowedWeathers[i];
                if (w == null || result.Contains(w))
                    continue;
                result.Add(w);
            }

            return result;
        }

        public static WeatherDefinition GetFallbackWeather(
            BackgroundDefinition def,
            WeatherCatalog catalog)
        {
            List<WeatherDefinition> allowed = GetAllowedWeathers(def, catalog);
            if (allowed.Count > 0)
                return allowed[0];

            return catalog?.GetDefault();
        }

        public static WeatherDefinition PickRandomAllowed(
            BackgroundDefinition def,
            WeatherCatalog catalog)
        {
            List<WeatherDefinition> allowed = GetAllowedWeathers(def, catalog);
            if (allowed.Count == 0)
                return catalog?.GetDefault();

            return allowed[Random.Range(0, allowed.Count)];
        }

        public static WeatherDefinition ResolveAllowedById(
            BackgroundDefinition def,
            WeatherCatalog catalog,
            string weatherId)
        {
            if (catalog == null || string.IsNullOrEmpty(weatherId))
                return GetFallbackWeather(def, catalog);

            WeatherDefinition weather = catalog.GetById(weatherId);
            if (IsWeatherAllowed(def, weather))
                return weather;

            return GetFallbackWeather(def, catalog);
        }
    }
}
