using System.Collections.Generic;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UniverIdle.Game
{
  /// <summary>按动作表 thumbImage 字段加载缩略图；空则按动作 id 在 ActionImage 目录查找。</summary>
  internal static class ActionImageLoader
  {
    private static readonly Dictionary<string, Sprite> Cache = new();

    public static Sprite Get(WorkActionDefinition action)
    {
      if (action == null) return null;
      var path = action.ThumbImageResourcePath;
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
      var editorPath = $"{GameDataPaths.ActionImageEditorFolder}/{fileName}.png";
      sprite = AssetDatabase.LoadAssetAtPath<Sprite>(editorPath);
      if (sprite != null) return sprite;

      editorPath = $"{GameDataPaths.ActionImageEditorFolder}/{fileName}.jpg";
      sprite = AssetDatabase.LoadAssetAtPath<Sprite>(editorPath);
#endif
      return sprite;
    }
  }
}
