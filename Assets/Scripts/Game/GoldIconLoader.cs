using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UniverIdle.Game
{
  internal static class GoldIconLoader
  {
    private static Sprite _cached;

    public static Sprite Get()
    {
      if (_cached != null) return _cached;

      _cached = Resources.Load<Sprite>(GameDataPaths.GoldIconResourcePath);
#if UNITY_EDITOR
      if (_cached == null)
        _cached = AssetDatabase.LoadAssetAtPath<Sprite>($"{GameDataPaths.ItemIconEditorFolder}/item_gold.png");
#endif
      return _cached;
    }
  }
}
