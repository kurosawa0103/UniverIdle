#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace UniverIdle.Editor
{
  public static class DetailWorkButtonInstaller
  {
    const string PrefabPath = "Assets/GameResources/Prefab/UniverIdle_MainUI.prefab";
    const string ButtonName = "Btn_工作";

    static Transform FindDetailPanel(GameObject root)
    {
      var controller = root.GetComponentInChildren<UniverIdle.UI.MainUIController>(true);
      if (controller != null)
      {
        var detail = controller.transform.Find("Detail");
        if (detail != null) return detail;
      }

      foreach (var t in root.GetComponentsInChildren<Transform>(true))
      {
        if (t.name == "Detail")
          return t;
      }

      return null;
    }

    [MenuItem("UniverIdle/安装 Detail 工作按钮")]
    public static void Install()
    {
      var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
      if (prefab == null)
      {
        Debug.LogError($"找不到预制体：{PrefabPath}");
        return;
      }

      var root = PrefabUtility.LoadPrefabContents(PrefabPath);
      try
      {
        var detail = FindDetailPanel(root);
        if (detail == null)
        {
          Debug.LogError("找不到 Detail 面板（应在 MainUIController 下名为 Detail 的节点）");
          return;
        }

        var existing = detail.Find(ButtonName);
        if (existing != null)
        {
          WireMainUiController(root, existing.GetComponent<Button>());
          PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
          Debug.Log("Detail 工作按钮已存在，已重新绑定 MainUIController。");
          return;
        }

        var title = detail.Find("Text")?.GetComponent<TextMeshProUGUI>();
        var buttonGo = CreateWorkButton(detail, title);
        WireMainUiController(root, buttonGo.GetComponent<Button>());
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Debug.Log("已在 Detail 面板添加「工作」按钮。");
      }
      finally
      {
        PrefabUtility.UnloadPrefabContents(root);
      }
    }

    static GameObject CreateWorkButton(Transform detail, TextMeshProUGUI fontSource)
    {
      var go = new GameObject(ButtonName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
        typeof(Button));
      var rt = go.GetComponent<RectTransform>();
      rt.SetParent(detail, false);
      rt.anchorMin = new Vector2(0f, 1f);
      rt.anchorMax = new Vector2(0f, 1f);
      rt.pivot = new Vector2(0.5f, 0.5f);
      rt.anchoredPosition = new Vector2(194.09221f, -940f);
      rt.sizeDelta = new Vector2(360.18442f, 48f);

      var img = go.GetComponent<Image>();
      img.color = new Color(0.18431373f, 0.23529412f, 0.21176471f, 1f);
      img.raycastTarget = true;

      var btn = go.GetComponent<Button>();
      btn.targetGraphic = img;
      var colors = btn.colors;
      colors.normalColor = img.color;
      colors.highlightedColor = new Color(0.22745098f, 0.28235295f, 0.25882354f, 1f);
      colors.pressedColor = new Color(0.2901961f, 0.34509805f, 0.32156864f, 1f);
      colors.selectedColor = colors.highlightedColor;
      btn.colors = colors;

      var labelGo = new GameObject("Text", typeof(RectTransform));
      var labelRt = labelGo.GetComponent<RectTransform>();
      labelRt.SetParent(rt, false);
      labelRt.anchorMin = Vector2.zero;
      labelRt.anchorMax = Vector2.one;
      labelRt.offsetMin = Vector2.zero;
      labelRt.offsetMax = Vector2.zero;

      var tmp = labelGo.AddComponent<TextMeshProUGUI>();
      if (fontSource != null)
      {
        tmp.font = fontSource.font;
        tmp.fontSharedMaterial = fontSource.fontSharedMaterial;
      }
      tmp.text = "工作";
      tmp.fontSize = 22f;
      tmp.alignment = TextAlignmentOptions.Center;
      tmp.color = new Color(0.78431374f, 0.92156863f, 0.8862745f, 1f);
      tmp.raycastTarget = false;

      go.transform.SetSiblingIndex(2);
      return go;
    }

    static void WireMainUiController(GameObject root, Button button)
    {
      if (button == null) return;
      var controller = root.GetComponentInChildren<UniverIdle.UI.MainUIController>(true);
      if (controller == null) return;

      var so = new SerializedObject(controller);
      so.FindProperty("detailWorkButton").objectReferenceValue = button;
      so.ApplyModifiedPropertiesWithoutUndo();
    }
  }
}
#endif
