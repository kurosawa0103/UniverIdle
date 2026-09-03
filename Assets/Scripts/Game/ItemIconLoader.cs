using System.Collections.Generic;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UniverIdle.Game
{
  /// <summary>按 Resources 路径加载道具/金币图标；找不到时由 UI 使用占位色。</summary>
  internal static class ItemIconLoader
  {
    private static readonly Dictionary<string, Sprite> Cache = new();

    public static Sprite Get(ItemDefinition item)
    {
      if (item == null) return null;
      return GetByResourcePath(item.IconResourcePath);
    }

    public static Sprite GetGold() => GetByResourcePath(GameDataPaths.GoldIconResourcePath);

    /// <summary>按地区熟练度等级取分档图标：1–30 铜、31–70 银、71+ 金。</summary>
    public static Sprite GetMastery(int level)
    {
      var path = level >= GameDataPaths.MasteryIconTier3MinLevel
        ? GameDataPaths.MasteryIconTier3Path
        : level >= GameDataPaths.MasteryIconTier2MinLevel
          ? GameDataPaths.MasteryIconTier2Path
          : GameDataPaths.MasteryIconTier1Path;
      return GetByResourcePath(path);
    }

    public static Sprite GetByResourcePath(string resourcePath)
    {
      if (string.IsNullOrEmpty(resourcePath)) return null;

      if (Cache.TryGetValue(resourcePath, out var cached))
        return cached;

      var sprite = LoadSprite(resourcePath);
      if (sprite != null)
        Cache[resourcePath] = sprite;
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
