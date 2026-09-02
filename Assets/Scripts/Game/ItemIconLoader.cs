using System.Collections.Generic;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UniverIdle.Game
{
  /// <summary>按道具表 icon 字段加载 Sprite；找不到时由 UI 回退到 DisplayColor。</summary>
  internal static class ItemIconLoader
  {
    private static readonly Dictionary<string, Sprite> Cache = new();

    public static Sprite Get(ItemDefinition item)
    {
      if (item == null) return null;
      var path = item.IconResourcePath;
      if (string.IsNullOrEmpty(path)) return null;

      if (Cache.TryGetValue(path, out var cached))
        return cached;

      var sprite = LoadSprite(path);
      if (sprite != null)
        Cache[path] = sprite;
      return sprite;
    }

    private static Sprite LoadSprite(string resourcePath)
    {
      var sprite = Resources.Load<Sprite>(resourcePath);
      if (sprite != null) return sprite;

#if UNITY_EDITOR
      var fileName = Path.GetFileName(resourcePath);
      var editorPath = $"{GameDataPaths.ItemIconEditorFolder}/{fileName}.png";
      sprite = AssetDatabase.LoadAssetAtPath<Sprite>(editorPath);
#endif
      return sprite;
    }
  }
}
