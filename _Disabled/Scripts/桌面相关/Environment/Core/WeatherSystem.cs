using System;
using UnityEngine;

namespace DesktopPet.Environment
{
    public sealed class WeatherSystem
    {
        private readonly WeatherCatalog _catalog;
        private WeatherDefinition _current;

        public WeatherDefinition CurrentWeather => _current;

        public event Action<WeatherDefinition> WeatherChanged;

        public WeatherSystem(WeatherCatalog catalog)
        {
            _catalog = catalog;
        }

        public void SetRandom()
        {
            ApplyWeather(_catalog?.GetRandomConcrete());
        }

        public void SetConcreteWeather(WeatherDefinition weather)
        {
            ApplyWeather(weather ?? _catalog?.GetDefault());
        }

        public void RestoreConcreteWeather(string weatherId)
        {
            WeatherDefinition weather = _catalog != null
                ? _catalog.GetById(weatherId) ?? _catalog.GetDefault()
                : null;
            ApplyWeather(weather, notify: false);
        }

        private void ApplyWeather(WeatherDefinition weather, bool notify = true)
        {
            if (weather == null)
                return;

            if (_current == weather)
                return;

            _current = weather;
            if (notify)
                WeatherChanged?.Invoke(_current);
        }
    }
}
