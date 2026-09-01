using DesktopPet.Environment;
using Sirenix.OdinInspector;
using UnityEngine;

namespace DesktopPet.AI
{
    /// <summary>天气门闸：当前 weatherId 落在允许列表内才可进（选型 / 维持）。</summary>
    [CreateAssetMenu(menuName = "桌宠/AI/条件/天气 ID", fileName = "WeatherIdCondition")]
    public sealed class WeatherIdCondition : PetBehaviorCondition
    {
        [LabelText("允许的 weatherId")]
        [Tooltip("对应 WeatherDefinition.weatherId，如 rainy / stormy")]
        public string[] allowedWeatherIds = { "rainy", "stormy" };

        public override bool Evaluate(PetBehaviorContext context)
        {
            WeatherDefinition weather = ResolveCurrentWeather();
            if (weather == null || string.IsNullOrEmpty(weather.weatherId))
                return false;
            if (allowedWeatherIds == null || allowedWeatherIds.Length == 0)
                return false;

            string id = weather.weatherId;
            for (int i = 0; i < allowedWeatherIds.Length; i++)
            {
                if (!string.IsNullOrEmpty(allowedWeatherIds[i]) && allowedWeatherIds[i] == id)
                    return true;
            }

            return false;
        }

        private static WeatherDefinition ResolveCurrentWeather()
        {
            EnvironmentManager env = DesktopPetServices.Environment;
            return env?.Weather != null ? env.Weather.CurrentWeather : null;
        }
    }
}
