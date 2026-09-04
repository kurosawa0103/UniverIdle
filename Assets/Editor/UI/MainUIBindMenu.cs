#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using TMPro;
using UniverIdle.Game;
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
      EnsureWoodcuttingActionList(root, log);
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

      BindGlobalLootToast(root, main, so, log);

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

    private static void BindGlobalLootToast(Transform root, MainUIController main, SerializedObject so, StringBuilder log)
    {
      var linePrefab = LoadComponent<LootToastLineView>(ToastLinePrefabPath);
      var floaterPrefab = LoadComponent<TextMeshProUGUI>(ToastFloaterPrefabPath);
      var host = main != null ? main.transform : root;

      var toast = so.FindProperty("lootToast")?.objectReferenceValue as LootToastView;
      if (toast == null)
      {
        var named = FindNamed(root, "获得提示区");
        if (named != null)
          toast = named.GetComponent<LootToastView>() ?? named.GetComponentInChildren<LootToastView>(true);
      }
      if (toast == null)
        toast = root.GetComponentInChildren<LootToastView>(true);
      if (toast == null)
        toast = CreateLootToastHost(host, log);

      EnsureToastIsGlobalOverlay(toast, host, log);
      RemoveDuplicateLootToasts(root, toast, log);

      AssignIfNull(so, "lootToast", toast, log, "MainUI.lootToast");

      if (linePrefab != null)
      {
        var lso = new SerializedObject(linePrefab);
        AssignIfNull(lso, "row", linePrefab.transform.Find("Row") as RectTransform, log, "获得提示.row");
        lso.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(linePrefab);
      }

      if (toast == null) return;
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

    /// <summary>全局一份：挂在 MainUI/App 下；若在 WorkView/Detail 下则挪到 App，不改已有锚点（仅挪父级）。</summary>
    private static void EnsureToastIsGlobalOverlay(LootToastView toast, Transform host, StringBuilder log)
    {
      if (toast == null || host == null) return;
      if (toast.transform.parent == host) return;

      Undo.RecordObject(toast.transform, "Move 获得提示区 to App");
      toast.transform.SetParent(host, worldPositionStays: true);
      toast.transform.SetAsLastSibling();
      EditorUtility.SetDirty(toast);
      log.AppendLine($"· 获得提示区 ← 挪到 {host.name}（全局一份，保留原位置）");
    }

    /// <summary>删掉 WorkView/Detail 下多余的获得提示，只保留 MainUI.lootToast 那一份。</summary>
    private static void RemoveDuplicateLootToasts(Transform root, LootToastView keep, StringBuilder log)
    {
      if (root == null || keep == null) return;
      var all = root.GetComponentsInChildren<LootToastView>(true);
      var removed = 0;
      for (var i = 0; i < all.Length; i++)
      {
        var other = all[i];
        if (other == null || other == keep) continue;
        Undo.DestroyObjectImmediate(other.gameObject);
        removed++;
      }

      if (removed > 0)
        log.AppendLine($"· 删除多余获得提示区 ×{removed}");
    }

    /// <summary>仅新建「获得提示区」时用的默认占位；已有节点勿再调用。</summary>
    private static void ApplyGlobalToastAnchors(RectTransform rt)
    {
      if (rt == null) return;
      rt.anchorMin = new Vector2(0.55f, 0f);
      rt.anchorMax = new Vector2(1f, 0.45f);
      rt.offsetMin = new Vector2(12f, 12f);
      rt.offsetMax = new Vector2(-12f, -12f);
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

    /// <summary>
    /// 砍树根误挂 <see cref="ScavengeHubView"/> 时换成 <see cref="ActionListWorkCenterView"/>，并补齐 actionCards。
    /// </summary>
    private static void EnsureWoodcuttingActionList(Transform root, StringBuilder log)
    {
      foreach (var hub in root.GetComponentsInChildren<ScavengeHubView>(true))
      {
        if (hub == null) continue;
        var so = new SerializedObject(hub);
        var workId = so.FindProperty("workId")?.stringValue;
        var name = hub.gameObject.name ?? string.Empty;
        var isWood = workId == GameContent.WorkWoodcuttingId
                     || name.IndexOf("woodcutting", System.StringComparison.OrdinalIgnoreCase) >= 0
                     || name.Contains("砍树");
        if (!isWood) continue;

        var go = hub.gameObject;
        Undo.DestroyObjectImmediate(hub);
        var list = Undo.AddComponent<ActionListWorkCenterView>(go);
        var lso = new SerializedObject(list);
        var workProp = lso.FindProperty("workId");
        if (workProp != null)
          workProp.stringValue = GameContent.WorkWoodcuttingId;
        lso.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(list);
        log.AppendLine($"· {go.name}：ScavengeHubView → ActionListWorkCenterView");
      }

      foreach (var list in root.GetComponentsInChildren<ActionListWorkCenterView>(true))
      {
        if (list == null) continue;
        var so = new SerializedObject(list);
        var cardsProp = so.FindProperty("actionCards");
        if (cardsProp == null || !cardsProp.isArray) continue;

        var needFill = cardsProp.arraySize == 0;
        if (!needFill)
        {
          for (var i = 0; i < cardsProp.arraySize; i++)
          {
            if (cardsProp.GetArrayElementAtIndex(i).objectReferenceValue == null)
            {
              needFill = true;
              break;
            }
          }
        }

        if (!needFill) continue;

        var found = list.GetComponentsInChildren<ActionCardView>(true);
        if (found == null || found.Length == 0) continue;

        cardsProp.arraySize = found.Length;
        for (var i = 0; i < found.Length; i++)
          cardsProp.GetArrayElementAtIndex(i).objectReferenceValue = found[i];
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(list);
        log.AppendLine($"· {list.name}.actionCards ← {found.Length} 张卡");
      }
    }

    private static void BindWorkDetails(Transform root, StringBuilder log)
    {
      var dropSlot = LoadComponent<LootDropSlotView>(DropSlotPrefabPath);

      foreach (var hub in root.GetComponentsInChildren<ScavengeHubView>(true))
      {
        var so = new SerializedObject(hub);
        var detail = hub.GetComponentInChildren<ScavengeDetailView>(true);
        AssignIfNull(so, "detailPanel", detail, log, $"{hub.name}.detailPanel");

        var mapsProp = so.FindProperty("maps");
        if (mapsProp != null && mapsProp.isArray)
        {
          var found = hub.GetComponentsInChildren<StandardWorkCenterView>(true);
          var list = new List<StandardWorkCenterView>();
          for (var i = 0; i < found.Length; i++)
          {
            if (found[i] != null)
              list.Add(found[i]);
          }

          var needFill = mapsProp.arraySize == 0;
          if (!needFill)
          {
            for (var i = 0; i < mapsProp.arraySize; i++)
            {
              if (mapsProp.GetArrayElementAtIndex(i).objectReferenceValue == null)
              {
                needFill = true;
                break;
              }
            }
          }

          if (needFill && list.Count > 0)
          {
            mapsProp.arraySize = list.Count;
            for (var i = 0; i < list.Count; i++)
              mapsProp.GetArrayElementAtIndex(i).objectReferenceValue = list[i];
            log.AppendLine($"· {hub.name}.maps ← {list.Count} 地图");
          }
        }

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
        BindOneDetail(detail, dropSlot, log);

      BindRunningBars(root, log);
    }

    private static void BindRunningBars(Transform root, StringBuilder log)
    {
      var fillSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/ItemIcon/ui_progress_fill.png");
      var trackSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/ItemIcon/ui_progress_track.png");

      foreach (var center in root.GetComponentsInChildren<StandardWorkCenterView>(true))
        BindRunningBarOn(center, fillSprite, trackSprite, log);

      foreach (var list in root.GetComponentsInChildren<ActionListWorkCenterView>(true))
        BindRunningBarOn(list, fillSprite, trackSprite, log);
    }

    private static void BindRunningBarOn(
      Component center,
      Sprite fillSprite,
      Sprite trackSprite,
      StringBuilder log)
    {
      var so = new SerializedObject(center);
      var barProp = so.FindProperty("runningBarRoot");
      Transform bar = null;
      if (barProp?.objectReferenceValue is GameObject go)
        bar = go.transform;
      if (bar == null)
      {
        bar = FindNamed(center.transform, "RunningBar");
        if (bar == null)
        {
          var detail = so.FindProperty("detailPanel")?.objectReferenceValue as ScavengeDetailView;
          if (detail != null)
            bar = FindNamed(detail.transform, "RunningBar");
        }
      }

      if (bar == null) return;

      if (barProp != null && barProp.objectReferenceValue == null)
      {
        barProp.objectReferenceValue = bar.gameObject;
        log.AppendLine($"· {center.name}.runningBarRoot ← RunningBar");
      }

      var fill = FindNamed(bar, "BarFill")?.GetComponent<Image>();
      var fillProp = so.FindProperty("progressFill");
      if (fillProp != null && fill != null && fillProp.objectReferenceValue == null)
      {
        fillProp.objectReferenceValue = fill;
        log.AppendLine($"· {center.name}.progressFill ← BarFill");
      }

      if (fill != null && fillSprite != null)
      {
        fill.sprite = fillSprite;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        EditorUtility.SetDirty(fill);
      }

      var barBg = FindNamed(bar, "BarBg")?.GetComponent<Image>();
      if (barBg != null && trackSprite != null)
      {
        barBg.sprite = trackSprite;
        EditorUtility.SetDirty(barBg);
      }

      var labelProp = so.FindProperty("progressLabelText");
      var label = FindNamed(bar, "Label")?.GetComponent<TextMeshProUGUI>();
      if (labelProp != null && label != null && labelProp.objectReferenceValue == null)
        labelProp.objectReferenceValue = label;

      var timeProp = so.FindProperty("progressTimeText");
      var time = FindNamed(bar, "Time")?.GetComponent<TextMeshProUGUI>();
      if (timeProp != null && time != null && timeProp.objectReferenceValue == null)
        timeProp.objectReferenceValue = time;

      so.ApplyModifiedPropertiesWithoutUndo();
      EditorUtility.SetDirty(center);
    }

    private static void BindOneDetail(
      WorkActionDetailView detail,
      LootDropSlotView dropSlot,
      StringBuilder log)
    {
      var so = new SerializedObject(detail);
      AssignTmpByNames(so, "titleText", detail.transform, log, $"{detail.name}.titleText", "Title", "标题");
      AssignTmpByNames(so, "bodyText", detail.transform, log, $"{detail.name}.bodyText", "Body", "正文", "Desc");

      var preview = detail.GetComponentInChildren<LootPreviewView>(true);
      AssignIfNull(so, "lootPreview", preview, log, $"{detail.name}.lootPreview");

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
    }

    /// <summary>仅在 App 下新建「获得提示区」；勿挂 WorkView/Detail。</summary>
    private static LootToastView CreateLootToastHost(Transform appRoot, StringBuilder log)
    {
      var existing = appRoot.Find("获得提示区");
      if (existing != null)
      {
        var view = existing.GetComponent<LootToastView>();
        if (view == null)
          view = Undo.AddComponent<LootToastView>(existing.gameObject);
        EnsureToastChildren(existing);
        log.AppendLine($"· 复用 {appRoot.name}/获得提示区");
        return view;
      }

      var go = new GameObject("获得提示区", typeof(RectTransform));
      Undo.RegisterCreatedObjectUndo(go, "Create 获得提示区");
      go.transform.SetParent(appRoot, false);
      ApplyGlobalToastAnchors((RectTransform)go.transform);

      EnsureToastChildren(go.transform);
      var toast = Undo.AddComponent<LootToastView>(go);
      log.AppendLine($"· 创建 {appRoot.name}/获得提示区（全局一份）");
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
      var masterySprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/ItemIcon/ui_mastery.png");
      for (var i = 0; i < cards.Length; i++)
      {
        var card = cards[i];
        var so = new SerializedObject(card);
        EnsureCardChrome(so, card, log);

        // 砍树 Card_二狗家 曾残留已删字段 thumb，易导致 masteryIcon 丢失；始终按子节点重绑
        EnsureMasteryNodes(card.transform, out var icon, out var level, ref created);
        var iconProp = so.FindProperty("masteryIcon");
        var levelProp = so.FindProperty("masteryLevelText");
        if (iconProp != null && icon != null && iconProp.objectReferenceValue != icon)
        {
          iconProp.objectReferenceValue = icon;
          wired++;
        }

        if (levelProp != null && level != null && levelProp.objectReferenceValue != level)
        {
          levelProp.objectReferenceValue = level;
          wired++;
        }

        if (icon != null && icon.sprite == null && masterySprite != null)
        {
          icon.sprite = masterySprite;
          icon.preserveAspect = true;
          EditorUtility.SetDirty(icon);
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
      AssignIfNull(so, "button", card.GetComponent<Button>(), log, null);

      var thumbRoot = card.transform.Find("Thumb") ?? card.transform.Find("ThumbInner");
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

    internal static Transform FindNamed(Transform root, string name)
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
