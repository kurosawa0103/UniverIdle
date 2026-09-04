#if UNITY_EDITOR
using System.Text;
using TMPro;
using UniverIdle.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace UniverIdle.Editor
{
  /// <summary>
  /// 生成/刷新「进度条」预制体，并把填充图写进 MainUI 里已有 RunningBar。
  /// 菜单：UniverIdle → 安装进度条预制体
  /// </summary>
  public static class RunningBarPrefabMenu
  {
    public const string PrefabPath = "Assets/Resources/Prefab/进度条.prefab";
    private const string FillSpritePath = "Assets/Resources/ItemIcon/ui_progress_fill.png";
    private const string TrackSpritePath = "Assets/Resources/ItemIcon/ui_progress_track.png";
    private const string MainPrefabPath = "Assets/Resources/Prefab/UniverIdle_MainUI.prefab";

    [MenuItem("UniverIdle/安装进度条预制体", false, 11)]
    public static void Install()
    {
      var fill = AssetDatabase.LoadAssetAtPath<Sprite>(FillSpritePath);
      var track = AssetDatabase.LoadAssetAtPath<Sprite>(TrackSpritePath);
      if (fill == null || track == null)
      {
        EditorUtility.DisplayDialog(
          "进度条",
          "缺少填充/槽位图：\n" + FillSpritePath + "\n" + TrackSpritePath +
          "\n请确认已导入为 Sprite。",
          "确定");
        return;
      }

      var prefab = CreateOrUpdatePrefab(fill, track);
      var log = new StringBuilder();
      log.AppendLine("· 预制体 ← " + PrefabPath);

      var root = FindRoot();
      if (root != null)
      {
        ApplySpritesUnder(root, fill, track, log);
        BindCenters(root, log);
        EditorUtility.SetDirty(root.gameObject);
        if (PrefabUtility.IsPartOfPrefabAsset(root.gameObject))
          AssetDatabase.SaveAssets();
      }
      else
        log.AppendLine("· 未找到 MainUI，仅生成了预制体。");

      AssetDatabase.SaveAssets();
      AssetDatabase.Refresh();
      Debug.Log("[UniverIdle] 进度条安装完成\n" + log);
      EditorUtility.DisplayDialog("进度条", log.ToString(), "确定");
    }

    private static Transform FindRoot()
    {
      if (Selection.activeTransform != null)
      {
        var t = Selection.activeTransform;
        if (t.GetComponent<MainUIController>() != null || t.name.Contains("MainUI"))
          return t;
        var inParent = t.GetComponentInParent<MainUIController>();
        if (inParent != null) return inParent.transform;
      }

      var inScene = Object.FindObjectOfType<MainUIController>();
      if (inScene != null) return inScene.transform;

      var asset = AssetDatabase.LoadAssetAtPath<GameObject>(MainPrefabPath);
      return asset != null ? asset.transform : null;
    }

    private static GameObject CreateOrUpdatePrefab(Sprite fill, Sprite track)
    {
      var root = new GameObject("进度条", typeof(RectTransform));
      var rootRt = root.GetComponent<RectTransform>();
      rootRt.anchorMin = new Vector2(0f, 0f);
      rootRt.anchorMax = new Vector2(1f, 0f);
      rootRt.pivot = new Vector2(0.5f, 0f);
      rootRt.sizeDelta = new Vector2(0f, 84f);
      rootRt.anchoredPosition = Vector2.zero;

      var rootImg = root.AddComponent<Image>();
      rootImg.sprite = track;
      rootImg.type = Image.Type.Simple;
      rootImg.color = new Color(0.14f, 0.19f, 0.16f, 1f);
      rootImg.raycastTarget = false;
      root.AddComponent<Outline>().effectColor = new Color(0.29f, 0.36f, 0.33f, 1f);

      var hlg = root.AddComponent<HorizontalLayoutGroup>();
      hlg.padding = new RectOffset(16, 16, 14, 14);
      hlg.spacing = 14f;
      hlg.childAlignment = TextAnchor.MiddleLeft;
      hlg.childControlWidth = true;
      hlg.childControlHeight = true;
      hlg.childForceExpandWidth = true;
      hlg.childForceExpandHeight = true;

      var le = root.AddComponent<LayoutElement>();
      le.preferredHeight = 84f;
      le.flexibleHeight = 0f;

      var label = CreateTmp(root.transform, "Label", "进行中", TextAlignmentOptions.MidlineLeft, 22);
      var labelLe = label.gameObject.AddComponent<LayoutElement>();
      labelLe.minWidth = 120f;
      labelLe.flexibleWidth = 0.35f;

      var mid = new GameObject("Mid", typeof(RectTransform));
      mid.transform.SetParent(root.transform, false);
      var midLe = mid.AddComponent<LayoutElement>();
      midLe.flexibleWidth = 1f;
      midLe.minWidth = 80f;

      var barBg = new GameObject("BarBg", typeof(RectTransform));
      barBg.transform.SetParent(mid.transform, false);
      var barBgRt = barBg.GetComponent<RectTransform>();
      barBgRt.anchorMin = Vector2.zero;
      barBgRt.anchorMax = Vector2.one;
      barBgRt.offsetMin = Vector2.zero;
      barBgRt.offsetMax = Vector2.zero;
      var barBgImg = barBg.AddComponent<Image>();
      barBgImg.sprite = track;
      barBgImg.type = Image.Type.Simple;
      barBgImg.color = Color.white;
      barBgImg.raycastTarget = false;

      var barFill = new GameObject("BarFill", typeof(RectTransform));
      barFill.transform.SetParent(barBg.transform, false);
      var fillRt = barFill.GetComponent<RectTransform>();
      fillRt.anchorMin = Vector2.zero;
      fillRt.anchorMax = Vector2.one;
      fillRt.offsetMin = new Vector2(2f, 2f);
      fillRt.offsetMax = new Vector2(-2f, -2f);
      var fillImg = barFill.AddComponent<Image>();
      fillImg.sprite = fill;
      fillImg.type = Image.Type.Filled;
      fillImg.fillMethod = Image.FillMethod.Horizontal;
      fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
      fillImg.fillAmount = 0.35f;
      fillImg.color = new Color(0.91f, 0.53f, 0.38f, 1f);
      fillImg.raycastTarget = false;

      var time = CreateTmp(root.transform, "Time", "00:00", TextAlignmentOptions.MidlineRight, 20);
      var timeLe = time.gameObject.AddComponent<LayoutElement>();
      timeLe.minWidth = 64f;
      timeLe.flexibleWidth = 0f;

      // Prefab 根名与场景约定一致，便于一键绑定找 RunningBar
      root.name = "RunningBar";

      var dir = System.IO.Path.GetDirectoryName(PrefabPath);
      if (!System.IO.Directory.Exists(dir))
        System.IO.Directory.CreateDirectory(dir);

      var saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
      Object.DestroyImmediate(root);
      return saved;
    }

    private static TextMeshProUGUI CreateTmp(
      Transform parent,
      string name,
      string text,
      TextAlignmentOptions align,
      float size)
    {
      var go = new GameObject(name, typeof(RectTransform));
      go.transform.SetParent(parent, false);
      var tmp = go.AddComponent<TextMeshProUGUI>();
      tmp.text = text;
      tmp.fontSize = size;
      tmp.alignment = align;
      tmp.color = new Color(0.93f, 0.91f, 0.89f, 1f);
      tmp.raycastTarget = false;
      tmp.enableWordWrapping = false;
      tmp.overflowMode = TextOverflowModes.Ellipsis;
      return tmp;
    }

    private static void ApplySpritesUnder(Transform root, Sprite fill, Sprite track, StringBuilder log)
    {
      var filled = 0;
      var tracked = 0;
      foreach (var t in root.GetComponentsInChildren<Transform>(true))
      {
        if (t.name != "BarFill" && t.name != "BarBg") continue;
        var img = t.GetComponent<Image>();
        if (img == null) continue;
        if (t.name == "BarFill")
        {
          img.sprite = fill;
          img.type = Image.Type.Filled;
          img.fillMethod = Image.FillMethod.Horizontal;
          img.fillOrigin = (int)Image.OriginHorizontal.Left;
          EditorUtility.SetDirty(img);
          filled++;
        }
        else
        {
          img.sprite = track;
          EditorUtility.SetDirty(img);
          tracked++;
        }
      }

      log.AppendLine($"· 写入 BarFill×{filled} / BarBg×{tracked}");
    }

    private static void BindCenters(Transform root, StringBuilder log)
    {
      foreach (var center in root.GetComponentsInChildren<StandardWorkCenterView>(true))
        BindOne(center, log);
      foreach (var list in root.GetComponentsInChildren<ActionListWorkCenterView>(true))
        BindOne(list, log);
    }

    private static void BindOne(Component center, StringBuilder log)
    {
      var so = new SerializedObject(center);
      var barProp = so.FindProperty("runningBarRoot");
      var fillProp = so.FindProperty("progressFill");
      var labelProp = so.FindProperty("progressLabelText");
      var timeProp = so.FindProperty("progressTimeText");

      Transform bar = null;
      if (barProp?.objectReferenceValue is GameObject go)
        bar = go.transform;
      if (bar == null)
        bar = MainUIBindMenu.FindNamed(center.transform, "RunningBar");

      if (bar == null)
      {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null) return;
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, center.transform);
        instance.name = "RunningBar";
        Undo.RegisterCreatedObjectUndo(instance, "Install RunningBar");
        bar = instance.transform;
        log.AppendLine($"· {center.name} 新建 RunningBar 实例");
      }

      if (barProp != null && barProp.objectReferenceValue == null)
        barProp.objectReferenceValue = bar.gameObject;

      var fill = MainUIBindMenu.FindNamed(bar, "BarFill")?.GetComponent<Image>();
      if (fillProp != null && fill != null)
        fillProp.objectReferenceValue = fill;

      var label = MainUIBindMenu.FindNamed(bar, "Label")?.GetComponent<TextMeshProUGUI>();
      if (labelProp != null && label != null)
        labelProp.objectReferenceValue = label;

      var time = MainUIBindMenu.FindNamed(bar, "Time")?.GetComponent<TextMeshProUGUI>();
      if (timeProp != null && time != null)
        timeProp.objectReferenceValue = time;

      so.ApplyModifiedPropertiesWithoutUndo();
      EditorUtility.SetDirty(center);
      log.AppendLine($"· {center.name} 已绑 runningBar / fill / label / time");
    }
  }
}
#endif
