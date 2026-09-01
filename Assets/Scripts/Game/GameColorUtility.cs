using UnityEngine;

namespace UniverIdle.Game
{
  internal static class GameColorUtility
  {
    public static Color Parse(string hex, Color fallback)
    {
      if (string.IsNullOrEmpty(hex)) return fallback;
      if (!hex.StartsWith("#")) hex = "#" + hex;
      return ColorUtility.TryParseHtmlString(hex, out var color) ? color : fallback;
    }
  }
}
