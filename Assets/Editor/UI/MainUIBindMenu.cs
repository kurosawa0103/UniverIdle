#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using TMPro;
using UniverIdle.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace UniverIdle.Editor
{
  /// <summary>
  /// 按节点名约定补齐主界面 SerializeField；缺「获得提示区 / Mastery」时在编辑器里创建（不在运行时造）。
  /// 菜单：UniverIdle → 一键绑定主界面引用
  /// </summary>
  public static class MainUIBindMenu
  {
    private const string MainPrefabPath = "Assets/Resources/Prefab/UniverIdle_MainUI.prefab";
    private const string ToastLinePrefabPath = "Assets/Resources/Prefab/获得提示.prefab";
    private const string ToastFloaterPrefabPath = "Assets/Resources/Prefab/获得提示飘字.prefab";
    private const string DropSlotPrefabPath = "Assets/Resources/Prefab/掉落slot.prefab";
    private const string BagSlotPrefabPath = "Assets/Resources/Prefab/背包slot.prefab";

    [MenuItem("UniverIdle/一键绑定主界面引用", false, 10)]
    public static void BindSelectedOrMainPrefab()
    {
      var root = FindBindRoot();
      if (root == null)
      {
        EditorUtility.DisplayDialog(
          "一键绑定",
          "请选中 UniverIdle_MainUI（场景实例或预制体），或确保存在：\n" + MainPrefabPath,
          "确定");
        return;
      }

      var log = new StringBuilder();
      Undo.SetCurrentGroupName("Bind MainUI References");
      var group = Undo.GetCurrentGroup();

      BindMainController(root, log);
      BindTopBarGold(root, log);
      BindInventory(root, log);
      BindWorkDetails(root, log);
      BindActionCards(root, log);

      Undo.CollapseUndoOperations(group);
      EditorUtility.SetDirty(root.gameObject);
      if (PrefabUtility.IsPartOfPrefabAsset(root.gameObject))
        AssetDatabase.SaveAssets();

      Debug.Log("[UniverIdle] 一键绑定完成\n" + log);
      EditorUtility.DisplayDialog("一键绑定", "完成。详情见 Console。\n\n记得保存预制体/场景。", "确定");
    }

    private static Transform FindBindRoot()
    {
      var go = Selection.activeGameObject;
      if (go != null)
      {
        var main = go.GetComponentInParent<MainUIController>(true);
        if (main != null) return main.transform;
        if (go.name.Contains("UniverIdle_MainUI") || go.name == "App")
          return go.transform;
      }

      var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MainPrefabPath);
      return prefab != null ? prefab.transform : null;
    }

    private static void BindMainController(Transform root, StringBuilder log)
    {
      var main = root.GetComponentInChildren<MainUIController>(true);
      if (main == null)
      {
        log.AppendLine("· MainUIController：未找到");
        return;
      }

      var so = new SerializedObject(main);
      AssignIfNull(so, "workCenterHost", root.GetComponentInChildren<WorkCenterHost>(true), log, "MainUI.workCenterHost");
      AssignIfNull(so, "inventoryPanel", root.GetComponentInChildren<InventoryPanelView>(true), log, "MainUI.inventoryPanel");
      AssignIfNull(so, "topBarGold", root.GetComponentInChildren<TopBarGoldView>(true), log, "MainUI.topBarGold");

      var bagBtn = FindNamed(root, "Btn_背包")?.GetComponent<Button>();
      AssignIfNull(so, "inventoryButton", bagBtn, log, "MainUI.inventoryButton");

      var skillProp = so.FindProperty("skillItems");
      if (skillProp != null && skillProp.isArray && skillProp.arraySize == 0)
      {
        var skills = root.GetComponentsInChildren<SkillNavItemView>(true);
        skillProp.arraySize = skills.Length;
        for (var i = 0; i < skills.Length; i++)
          skillProp.GetArrayElementAtIndex(i).objectReferenceValue = skills[i];
        log.AppendLine($"· MainUI.skillItems ← {skills.Length} 项");
      }

      so.ApplyModifiedPropertiesWithoutUndo();
      EditorUtility.SetDirty(main);
    }

    private static void BindTopBarGold(Transform root, StringBuilder log)
    {
      var gold = root.GetComponentInChildren<TopBarGoldView>(true);
      if (gold == null)
      {
        log.AppendLine("· TopBarGoldView：未找到");
        return;
      }

      var so = new SerializedObject(gold);
      var icon = gold.transform.Find("Icon")?.GetComponent<Image>();
      var text = gold.transform.Find("Text")?.GetComponent<TextMeshProUGUI>()
                 ?? gold.GetComponentInChildren<TextMeshProUGUI>(true);
      AssignIfNull(so, "icon", icon, log, "TopBarGold.icon");
      AssignIfNull(so, "amountText", text, log, "TopBarGold.amountText");
      so.ApplyModifiedPropertiesWithoutUndo();
      EditorUtility.SetDirty(gold);
    }

    private static void BindInventory(Transform root, StringBuilder log)
    {
      var panel = root.GetComponentInChildren<InventoryPanelView>(true);
      if (panel == null)
      {
        log.AppendLine("· InventoryPanelView：未找到");
        return;
      }

      var so = new SerializedObject(panel);
      AssignIfNull(so, "overlayRoot", panel.gameObject, log, "Inventory.overlayRoot");
      AssignIfNull(so, "grid", panel.GetComponentInChildren<InventoryGridView>(true), log, "Inventory.grid");

      var tabRoot = so.FindProperty("tabRoot")?.objectReferenceValue as Transform;
      if (tabRoot == null)
      {
        var tabs = FindNamed(panel.transform, "Tabs");
        if (tabs != null)
        {
          so.FindProperty("tabRoot").objectReferenceValue = tabs;
          tabRoot = tabs;
          log.AppendLine("· Inventory.tabRoot ← Tabs");
        }
      }

      var pageTabs = so.FindProperty("pageTabs");
      if (pageTabs != null && pageTabs.isArray && pageTabs.arraySize == 0 && tabRoot != null)
      {
        var buttons = new List<Button>();
        for (var i = 0; i < tabRoot.childCount; i++)
        {
          var btn = tabRoot.GetChild(i).GetComponent<Button>();
          if (btn != null) buttons.Add(btn);
        }

        pageTabs.arraySize = buttons.Count;
        for (var i = 0; i < buttons.Count; i++)
          pageTabs.GetArrayElementAtIndex(i).objectReferenceValue = buttons[i];
        log.AppendLine($"· Inventory.pageTabs ← {buttons.Count} 个");
      }

      AssignTmpByNames(so, "pageLabelText", panel.transform, log, "Inventory.pageLabelText", "格数", "PageLabel", "容量");
      AssignTmpByNames(so, "goldText", panel.transform, log, "Inventory.goldText", "金币", "Gold");
      AssignButtonByNames(so, "closeButton", panel.transform, log, "Inventory.closeButton", "Btn_关闭", "Close");
      AssignButtonByNames(so, "backdropButton", panel.transform, log, "Inventory.backdropButton", "Backdrop", "Dim");

      so.ApplyModifiedPropertiesWithoutUndo();
      EditorUtility.SetDirty(panel);

      var grid = panel.GetComponentInChildren<InventoryGridView>(true);
      if (grid != null)
      {
        var gso = new SerializedObject(grid);
        var bagSlot = LoadComponent<InventorySlotView>(BagSlotPrefabPath);
        AssignIfNull(gso, "slotPrefab", bagSlot, log, "InventoryGrid.slotPrefab");
        var container = FindNamed(grid.transform, "Content")
                        ?? FindNamed(grid.transform, "Slots")
                        ?? grid.transform as Transform;
        AssignIfNull(gso, "slotContainer", container, log, "InventoryGrid.slotContainer");
        gso.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(grid);
      }
    }

    private static void BindWorkDetails(Transform root, StringBuilder log)
    {
      var linePrefab = LoadComponent<LootToastLineView>(ToastLinePrefabPath);
      var floaterPrefab = LoadComponent<TextMeshProUGUI>(ToastFloaterPrefabPath);
      var dropSlot = LoadComponent<LootDropSlotView>(DropSlotPrefabPath);

      var hub = root.GetComponentInChildren<ScavengeHubView>(true);
      if (hub != null)
      {
        var so = new SerializedObject(hub);
        var detail = hub.GetComponentInChildren<ScavengeDetailView>(true);
        AssignIfNull(so, "detailPanel", detail, log, "ScavengeHub.detailPanel");
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(hub);
      }

      foreach (var list in root.GetComponentsInChildren<ActionListWorkCenterView>(true))
      {
        var so = new SerializedObject(list);
        var detail = list.GetComponentInChildren<WorkActionDetailView>(true);
        // 砍树详情是 WorkActionDetailView，不要绑到拾荒 ScavengeDetailView
        if (detail is ScavengeDetailView)
          detail = null;
        if (detail == null)
        {
          foreach (var d in list.GetComponentsInChildren<WorkActionDetailView>(true))
          {
            if (d is ScavengeDetailView) continue;
            detail = d;
            break;
          }
        }

        AssignIfNull(so, "detailPanel", detail, log, $"{list.name}.detailPanel");
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(list);
      }

      foreach (var detail in root.GetComponentsInChildren<WorkActionDetailView>(true))
        BindOneDetail(detail, linePrefab, floaterPrefab, dropSlot, log);
    }

    private static void BindOneDetail(
      WorkActionDetailView detail,
      LootToastLineView linePrefab,
      TextMeshProUGUI floaterPrefab,
      LootDropSlotView dropSlot,
      StringBuilder log)
    {
      var so = new SerializedObject(detail);
      AssignTmpByNames(so, "titleText", detail.transform, log, $"{detail.name}.titleText", "Title", "标题");
      AssignTmpByNames(so, "bodyText", detail.transform, log, $"{detail.name}.bodyText", "Body", "正文", "Desc");

      var preview = detail.GetComponentInChildren<LootPreviewView>(true);
      AssignIfNull(so, "lootPreview", preview, log, $"{detail.name}.lootPreview");

      var toast = detail.GetComponentInChildren<LootToastView>(true);
      if (toast == null)
        toast = CreateLootToastHost(detail.transform, log);

      AssignIfNull(so, "lootToast", toast, log, $"{detail.name}.lootToast");
      AssignIfNull(so, "lootLinePrefab", linePrefab, log, $"{detail.name}.lootLinePrefab");
      AssignIfNull(so, "lootFloaterPrefab", floaterPrefab, log, $"{detail.name}.lootFloaterPrefab");

      if (detail is ScavengeDetailView)
      {
        AssignButtonByNames(so, "workButton", detail.transform, log, $"{detail.name}.workButton", "Btn_工作");
        var workBtn = so.FindProperty("workButton")?.objectReferenceValue as Button;
        if (workBtn != null)
          AssignIfNull(so, "workButtonText", workBtn.GetComponentInChildren<TextMeshProUGUI>(true), log,
            $"{detail.name}.workButtonText");
      }

      so.ApplyModifiedPropertiesWithoutUndo();
      EditorUtility.SetDirty(detail);

      if (preview != null && dropSlot != null)
      {
        var pso = new SerializedObject(preview);
        AssignIfNull(pso, "slotPrefab", dropSlot, log, $"{preview.name}.slotPrefab");
        pso.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(preview);
      }

      if (toast != null)
      {
        var tso = new SerializedObject(toast);
        var lines = toast.transform.Find("Lines") as RectTransform;
        var floats = toast.transform.Find("FloatLayer") as RectTransform;
        AssignIfNull(tso, "lineRoot", lines, log, $"{toast.name}.lineRoot");
        AssignIfNull(tso, "floatLayer", floats, log, $"{toast.name}.floatLayer");
        AssignIfNull(tso, "linePrefab", linePrefab, log, $"{toast.name}.linePrefab");
        AssignIfNull(tso, "floaterPrefab", floaterPrefab, log, $"{toast.name}.floaterPrefab");
        tso.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(toast);
      }
    }

    private static LootToastView CreateLootToastHost(Transform detail, StringBuilder log)
    {
      var existing = detail.Find("获得提示区");
      if (existing != null)
      {
        var view = existing.GetComponent<LootToastView>();
        if (view == null)
          view = Undo.AddComponent<LootToastView>(existing.gameObject);
        EnsureToastChildren(existing);
        log.AppendLine($"· 复用 {DetailPath(detail)}/获得提示区");
        return view;
      }

      var go = new GameObject("获得提示区", typeof(RectTransform));
      Undo.RegisterCreatedObjectUndo(go, "Create 获得提示区");
      go.transform.SetParent(detail, false);
      var rt = (RectTransform)go.transform;
      rt.anchorMin = new Vector2(0f, 0f);
      rt.anchorMax = new Vector2(1f, 0.35f);
      rt.offsetMin = new Vector2(8f, 8f);
      rt.offsetMax = new Vector2(-8f, -8f);

      EnsureToastChildren(go.transform);
      var toast = Undo.AddComponent<LootToastView>(go);
      log.AppendLine($"· 创建 {DetailPath(detail)}/获得提示区");
      return toast;
    }

    private static void EnsureToastChildren(Transform toastRoot)
    {
      if (toastRoot.Find("Lines") == null)
      {
        var lines = new GameObject("Lines", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(lines, "Create Lines");
        lines.transform.SetParent(toastRoot, false);
        StretchFull((RectTransform)lines.transform);
      }

      if (toastRoot.Find("FloatLayer") == null)
      {
        var layer = new GameObject("FloatLayer", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(layer, "Create FloatLayer");
        layer.transform.SetParent(toastRoot, false);
        StretchFull((RectTransform)layer.transform);
      }
    }

    private static void BindActionCards(Transform root, StringBuilder log)
    {
      var cards = root.GetComponentsInChildren<ActionCardView>(true);
      var created = 0;
      var wired = 0;
      for (var i = 0; i < cards.Length; i++)
      {
        var card = cards[i];
        var so = new SerializedObject(card);
        EnsureCardChrome(so, card, log);

        var icon = so.FindProperty("masteryIcon")?.objectReferenceValue as Image;
        var level = so.FindProperty("masteryLevelText")?.objectReferenceValue as TextMeshProUGUI;
        if (icon == null || level == null)
        {
          EnsureMasteryNodes(card.transform, out icon, out level, ref created);
          if (icon != null)
            so.FindProperty("masteryIcon").objectReferenceValue = icon;
          if (level != null)
            so.FindProperty("masteryLevelText").objectReferenceValue = level;
          wired++;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(card);
      }

      if (created > 0 || wired > 0)
        log.AppendLine($"· ActionCard Mastery：新建 {created} 对，补绑 {wired} 张卡");
    }

    private static void EnsureCardChrome(SerializedObject so, ActionCardView card, StringBuilder log)
    {
      AssignIfNull(so, "background", card.GetComponent<Image>(), log, null);
      AssignIfNull(so, "border", card.GetComponent<Outline>(), log, null);
      AssignIfNull(so, "canvasGroup", card.GetComponent<CanvasGroup>(), log, null);

      var thumbRoot = card.transform.Find("Thumb") ?? card.transform.Find("ThumbInner");
      var thumb = thumbRoot?.GetComponent<Image>();
      AssignIfNull(so, "thumb", thumb, log, null);

      Image thumbArt = null;
      if (thumbRoot != null)
      {
        var art = thumbRoot.Find("Image") ?? thumbRoot.Find("Art") ?? thumbRoot.Find("Icon");
        if (art == null)
        {
          for (var i = 0; i < thumbRoot.childCount; i++)
          {
            var child = thumbRoot.GetChild(i);
            if (child.GetComponent<Image>() != null)
            {
              art = child;
              break;
            }
          }
        }
        thumbArt = art != null ? art.GetComponent<Image>() : null;
      }
      AssignIfNull(so, "thumbArt", thumbArt, log, null);

      AssignTmpByNames(so, "titleText", card.transform, null, null, "name", "Title", "Name");
      AssignTmpByNames(so, "metaLeftText", card.transform, null, null, "CD", "MetaLeft", "Time");
      AssignTmpByNames(so, "metaRightText", card.transform, null, null, "Yield", "MetaRight", "产量");
      AssignTmpByNames(so, "unlockText", card.transform, null, null, "Unlock", "解锁", "UnlockText", "LockHint");
    }

    private static void EnsureMasteryNodes(
      Transform card,
      out Image icon,
      out TextMeshProUGUI levelText,
      ref int created)
    {
      icon = card.Find("MasteryIcon")?.GetComponent<Image>();
      levelText = card.Find("MasteryLevel")?.GetComponent<TextMeshProUGUI>();

      var cd = card.Find("CD") as RectTransform;
      var anchor = cd != null ? cd.anchoredPosition : new Vector2(40f, -40f);

      if (icon == null)
      {
        var go = new GameObject("MasteryIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        Undo.RegisterCreatedObjectUndo(go, "Create MasteryIcon");
        go.transform.SetParent(card, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.sizeDelta = new Vector2(16f, 16f);
        rt.anchoredPosition = cd != null
          ? anchor + new Vector2(cd.sizeDelta.x + 4f, 0f)
          : new Vector2(80f, -40f);
        icon = go.GetComponent<Image>();
        icon.raycastTarget = false;
        icon.preserveAspect = true;
        created++;
      }

      if (levelText == null)
      {
        var go = new GameObject("MasteryLevel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        Undo.RegisterCreatedObjectUndo(go, "Create MasteryLevel");
        go.transform.SetParent(card, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.sizeDelta = new Vector2(48f, 18f);
        var iconRt = icon != null ? (RectTransform)icon.transform : null;
        rt.anchoredPosition = iconRt != null
          ? iconRt.anchoredPosition + new Vector2(iconRt.sizeDelta.x + 2f, 1f)
          : new Vector2(100f, -40f);
        levelText = go.GetComponent<TextMeshProUGUI>();
        levelText.text = "Lv.1";
        levelText.fontSize = 12f;
        levelText.raycastTarget = false;
        created++;
      }
    }

    private static void AssignIfNull(SerializedObject so, string field, Object value, StringBuilder log, string label)
    {
      if (value == null) return;
      var prop = so.FindProperty(field);
      if (prop == null || prop.objectReferenceValue != null) return;
      prop.objectReferenceValue = value;
      if (log != null && !string.IsNullOrEmpty(label))
        log.AppendLine($"· {label} ← {value.name}");
    }

    private static void AssignTmpByNames(
      SerializedObject so, string field, Transform root, StringBuilder log, string label, params string[] names)
    {
      var prop = so.FindProperty(field);
      if (prop == null || prop.objectReferenceValue != null) return;
      for (var i = 0; i < names.Length; i++)
      {
        var t = FindNamed(root, names[i]);
        var tmp = t != null ? t.GetComponent<TextMeshProUGUI>() : null;
        if (tmp == null) continue;
        prop.objectReferenceValue = tmp;
        if (log != null && !string.IsNullOrEmpty(label))
          log.AppendLine($"· {label} ← {tmp.name}");
        return;
      }
    }

    private static void AssignButtonByNames(
      SerializedObject so, string field, Transform root, StringBuilder log, string label, params string[] names)
    {
      var prop = so.FindProperty(field);
      if (prop == null || prop.objectReferenceValue != null) return;
      for (var i = 0; i < names.Length; i++)
      {
        var t = FindNamed(root, names[i]);
        var btn = t != null ? t.GetComponent<Button>() : null;
        if (btn == null) continue;
        prop.objectReferenceValue = btn;
        if (log != null && !string.IsNullOrEmpty(label))
          log.AppendLine($"· {label} ← {btn.name}");
        return;
      }
    }

    private static Transform FindNamed(Transform root, string name)
    {
      if (root == null || string.IsNullOrEmpty(name)) return null;
      if (root.name == name) return root;
      for (var i = 0; i < root.childCount; i++)
      {
        var hit = FindNamed(root.GetChild(i), name);
        if (hit != null) return hit;
      }

      return null;
    }

    private static void StretchFull(RectTransform rt)
    {
      rt.anchorMin = Vector2.zero;
      rt.anchorMax = Vector2.one;
      rt.offsetMin = Vector2.zero;
      rt.offsetMax = Vector2.zero;
    }

    private static string DetailPath(Transform detail) =>
      detail != null ? detail.name : "?";

    private static T LoadComponent<T>(string assetPath) where T : Component
    {
      var go = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
      return go != null ? go.GetComponent<T>() : null;
    }
  }
}
#endif
