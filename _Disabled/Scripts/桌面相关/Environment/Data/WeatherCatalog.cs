using System.Collections.Generic;
using UnityEngine;

namespace DesktopPet.Environment
{
    [CreateAssetMenu(menuName = "桌宠/环境/天气目录", fileName = "WeatherCatalog")]
    public sealed class WeatherCatalog : ScriptableObject
    {
        public List<WeatherDefinition> weathers = new List<WeatherDefinition>();

        public WeatherDefinition GetById(string weatherId)
        {
            if (string.IsNullOrEmpty(weatherId) || weathers == null)
                return null;

            for (int i = 0; i < weathers.Count; i++)
            {
                WeatherDefinition w = weathers[i];
                if (w != null && w.weatherId == weatherId)
                    return w;
            }

            return null;
        }

        public WeatherDefinition GetRandomConcrete()
        {
            if (weathers == null || weathers.Count == 0)
                return null;

            int index = Random.Range(0, weathers.Count);
            return weathers[index];
        }

        public WeatherDefinition GetDefault()
        {
            if (weathers != null && weathers.Count > 0)
                return weathers[0];
            return null;
        }
    }
}
