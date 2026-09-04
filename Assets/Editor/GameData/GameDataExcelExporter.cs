#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UniverIdle.Game;
using UnityEditor;
using UnityEngine;

namespace UniverIdle.Editor
{
  public static class GameDataExcelExporter
  {
    private static readonly string[] ItemHeaders = { "id", "name", "category", "icon", "description" };
    private static readonly string[] ItemHeadersWithoutCategory = { "id", "name", "icon", "description" };
    private static readonly string[] ItemHeadersLegacyColorIcon = { "id", "name", "color", "icon", "description" };
    private static readonly string[] ItemHeadersLegacyColorNoIcon = { "id", "name", "color", "description" };
    private static readonly string[] WorkHeaders =
    {
      "id", "name", "locationName",
      "xpBase", "xpPerLevel", "sceneXpBase", "sceneXpPerLevel", "grantWorkXp", "grantSceneXp",
    };
    private static readonly string[] ActionHeaders =
    {
      "id", "workId", "sceneId", "sceneName", "spotName", "displayName",
      "durationSeconds", "xpReward", "requiredWorkLevel", "description", "thumbImage",
      "costItemId", "costAmount", "goldChance", "goldMin", "goldMax",
    };
    private static readonly string[] ActionHeadersLegacyWithCostNoGold =
    {
      "id", "workId", "sceneId", "sceneName", "spotName", "displayName",
      "durationSeconds", "xpReward", "requiredWorkLevel", "description", "thumbImage",
      "costItemId", "costAmount",
    };
    private static readonly string[] ActionHeadersLegacyNoCost =
    {
      "id", "workId", "sceneId", "sceneName", "spotName", "displayName",
      "durationSeconds", "xpReward", "requiredWorkLevel", "description", "thumbImage",
    };
    private static readonly string[] LootHeaders = { "actionId", "itemId", "chance", "min", "max" };
    private static readonly string[] LootExcelHeaders = { "actionId", "itemId", "#itemName", "chance", "min", "max" };

    private static readonly string[] ItemHeaderComments =
      { "道具ID", "显示名称", "分类(junk/wood/ore/monster/herb/tool/relic/system)", "图标(空=自动)", "描述" };
    private static readonly string[] WorkHeaderComments =
    {
      "工作ID", "工作名称", "地点名称",
      "工作经验基数", "每级额外工作经验", "地区熟练度基数", "地区熟练度每级增量", "是否加工作XP(1/0)", "是否加地区XP(1/0)",
    };
    private static readonly string[] ActionHeaderComments =
    {
      "动作ID", "所属工作", "地区ID", "地区名称", "子地点名", "卡片标题",
      "时长(秒)", "完成经验", "解锁所需工作等级", "详情文案(右侧)", "缩略图(空=动作ID)",
      "消耗道具ID", "消耗数量", "金币概率0~1", "金币最少", "金币最多",
    };
    private static readonly string[] LootExcelHeaderComments =
      { "动作ID", "道具ID", "道具名(不导出)", "概率0~1", "最少数量", "最多数量" };

    private enum ActionSheetLayout
    {
      Standard,
      LegacyWithCostNoGold,
      LegacyNoCost,
    }

    public readonly struct ExportBundle
    {
      public readonly ItemsDataFile Items;
      public readonly WorkContentDataFile Scavenge;
      public readonly WorkContentDataFile Woodcutting;
      public readonly WorkContentDataFile Mining;
      public readonly WorkContentDataFile MonsterExplore;

      public ExportBundle(ItemsDataFile items, WorkContentDataFile scavenge, WorkContentDataFile woodcutting, WorkContentDataFile mining, WorkContentDataFile monsterExplore)
      {
        Items = items;
        Scavenge = scavenge;
        Woodcutting = woodcutting;
        Mining = mining;
        MonsterExplore = monsterExplore;
      }
    }

    private static ExportBundle LoadCurrentJsonBundle() =>
      new ExportBundle(
        GameDataLoader.LoadItemsFileIfPresent(),
        GameDataLoader.LoadScavengeFileIfPresent(),
        GameDataLoader.LoadWoodcuttingFileIfPresent(),
        GameDataLoader.LoadMiningFileIfPresent(),
        GameDataLoader.LoadMonsterExploreFileIfPresent());

    public static void ExportExcelToJson(IEnumerable<string> sheetIds)
    {
      var selected = new HashSet<string>(sheetIds ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
      if (selected.Count == 0)
        throw new InvalidOperationException("未选择任何表格。");

      EnsureExcelFiles();
      var excelCache = ReadExcelCache(selected);
      var bundle = MergeExport(excelCache, selected);
      WriteJsonFiles(selected, bundle);
      AssetDatabase.Refresh();

      if (Application.isPlaying)
        GameContent.ReloadForEditor();

      Debug.Log($"[UniverIdle] 导表完成（{string.Join(", ", selected)}）→ items / scavenge / woodcutting / mining / monster_explore JSON");
    }

    public static Dictionary<string, Dictionary<string, List<string[]>>> ReadExcelCache(ISet<string> selectedSheetIds)
    {
      var excelKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
      foreach (var sheetId in selectedSheetIds)
        excelKeys.Add(GameDataSheetRegistry.Get(sheetId).ExcelKey);

      var cache = new Dictionary<string, Dictionary<string, List<string[]>>>(StringComparer.OrdinalIgnoreCase);
      foreach (var excelKey in excelKeys)
      {
        var path = GetExcelFullPath(excelKey);
        if (!File.Exists(path))
        {
          var entries = GameDataSheetRegistry.All
            .Where(s => s.ExcelKey == excelKey && selectedSheetIds.Contains(s.Id))
            .Select(s => new GameDataExportException.Entry(s.Id, "找不到 Excel 文件：" + path))
            .ToArray();
          if (entries.Length == 0)
          {
            entries = new[]
            {
              new GameDataExportException.Entry(
                GameDataSheetRegistry.All.First(s => s.ExcelKey == excelKey).Id,
                "找不到 Excel 文件：" + path),
            };
          }
          throw new GameDataExportException(entries, "找不到 Excel 文件：" + path);
        }
        cache[excelKey] = SimpleXlsx.Read(path);
      }
      return cache;
    }

    public static ExportBundle MergeExport(
      IReadOnlyDictionary<string, Dictionary<string, List<string[]>>> excelCache,
      ISet<string> selected)
    {
      if (excelCache == null) throw new ArgumentNullException(nameof(excelCache));
      if (selected == null || selected.Count == 0)
        throw new InvalidOperationException("未选择任何表格。");

      var bundle = LoadCurrentJsonBundle();
      var items = bundle.Items;
      var scavenge = bundle.Scavenge;
      var woodcutting = bundle.Woodcutting;
      var mining = bundle.Mining;
      var monsterExplore = bundle.MonsterExplore;

      if (selected.Contains("items"))
        items.items = ReadSheet("items", () => ReadItemSheet(RequireSheet(excelCache, "items"))).ToArray();

      MergeWorkContent(scavenge, excelCache, selected, "scavenge_works", "scavenge_actions", "scavenge_loot", "scavenge.json");
      MergeWorkContent(woodcutting, excelCache, selected, "woodcutting_works", "woodcutting_actions", "woodcutting_loot", "woodcutting.json");
      MergeWorkContent(mining, excelCache, selected, "mining_works", "mining_actions", "mining_loot", "mining.json");
      MergeWorkContent(monsterExplore, excelCache, selected, "monster_explore_works", "monster_explore_actions", "monster_explore_loot", "monster_explore.json");

      items.version = Math.Max(items.version, 3);
      ValidateItemReferences(items, scavenge, "scavenge_actions", "scavenge_loot");
      ValidateItemReferences(items, woodcutting, "woodcutting_actions", "woodcutting_loot");
      ValidateItemReferences(items, mining, "mining_actions", "mining_loot");
      ValidateItemReferences(items, monsterExplore, "monster_explore_actions", "monster_explore_loot");
      return new ExportBundle(items, scavenge, woodcutting, mining, monsterExplore);
    }

    private static void MergeWorkContent(
      WorkContentDataFile data,
      IReadOnlyDictionary<string, Dictionary<string, List<string[]>>> excelCache,
      ISet<string> selected,
      string worksSheetId,
      string actionsSheetId,
      string lootSheetId,
      string jsonFileName)
    {
      if (data == null) return;

      var exportActions = selected.Contains(actionsSheetId);
      var exportLoot = selected.Contains(lootSheetId);

      if (selected.Contains(worksSheetId))
        data.works = ReadSheet(worksSheetId, () => ReadWorkSheet(RequireSheet(excelCache, worksSheetId))).ToArray();

      if (!exportActions && !exportLoot) return;

      var actions = exportActions
        ? ReadSheet(actionsSheetId, () => ReadActionSheet(RequireSheet(excelCache, actionsSheetId)))
        : (data.actions ?? Array.Empty<ActionRow>()).ToList();

      var lootByAction = exportLoot
        ? ReadSheet(lootSheetId, () => ReadLootSheet(RequireSheet(excelCache, lootSheetId)))
        : null;

      var oldLoot = BuildLootLookup(data.actions);

      foreach (var action in actions)
      {
        if (lootByAction != null)
          action.loot = lootByAction.TryGetValue(action.id, out var loot) ? loot.ToArray() : Array.Empty<LootRow>();
        else if (!exportActions && exportLoot)
          throw new GameDataExportException(lootSheetId, $"仅导出 loot 时，{jsonFileName} 中需已有 actions 数据。");
        else if (oldLoot.TryGetValue(action.id, out var preserved))
          action.loot = preserved;
        else
          action.loot = Array.Empty<LootRow>();
      }

      data.actions = actions.ToArray();
      data.version = Math.Max(data.version, 3);
    }

    public static void EnsureExcelFiles()
    {
      var itemsPath = GetExcelFullPath(GameDataSheetRegistry.ItemsExcelKey);
      var scavengePath = GetExcelFullPath(GameDataSheetRegistry.ScavengeExcelKey);
      var woodcuttingPath = GetExcelFullPath(GameDataSheetRegistry.WoodcuttingExcelKey);
      var miningPath = GetExcelFullPath(GameDataSheetRegistry.MiningExcelKey);
      var monsterExplorePath = GetExcelFullPath(GameDataSheetRegistry.MonsterExploreExcelKey);
      if (File.Exists(itemsPath) && File.Exists(scavengePath) && File.Exists(woodcuttingPath) && File.Exists(miningPath) && File.Exists(monsterExplorePath))
        return;

      Debug.LogWarning("[UniverIdle] Excel 缺失，正在从 JSON 生成模板…");
      CreateExcelFromJson();
    }

    public static void CreateExcelFromJson()
    {
      var bundle = LoadCurrentJsonBundle();
      WriteExcelTemplates(bundle.Items, bundle.Scavenge, bundle.Woodcutting, bundle.Mining, bundle.MonsterExplore);
    }

    /// <summary>供 Unity batchmode：-executeMethod UniverIdle.Editor.GameDataExcelExporter.CreateExcelFromJsonBatch</summary>
    public static void CreateExcelFromJsonBatch()
    {
      CreateExcelFromJson();
      AssetDatabase.Refresh();
    }

    public static void OpenExcel(string excelKey)
    {
      EnsureExcelFiles();
      AssetDatabase.Refresh();
      var path = GetExcelFullPath(excelKey);
      if (!File.Exists(path))
        throw new FileNotFoundException("找不到 Excel 文件：" + path, path);
      EditorUtility.OpenWithDefaultApp(path);
    }

    public static void WriteExcelTemplates(ItemsDataFile items, WorkContentDataFile scavenge, WorkContentDataFile woodcutting, WorkContentDataFile mining, WorkContentDataFile monsterExplore)
    {
      WriteExcelFile(
        GetExcelFullPath(GameDataSheetRegistry.ItemsExcelKey),
        new Dictionary<string, IList<string[]>>
        {
          ["items"] = BuildItemRows(items?.items),
        });
      WriteExcelFile(
        GetExcelFullPath(GameDataSheetRegistry.ScavengeExcelKey),
        BuildWorkbookSheetRows(scavenge, items?.items));
      WriteExcelFile(
        GetExcelFullPath(GameDataSheetRegistry.WoodcuttingExcelKey),
        BuildWorkbookSheetRows(woodcutting, items?.items));
      WriteExcelFile(
        GetExcelFullPath(GameDataSheetRegistry.MiningExcelKey),
        BuildWorkbookSheetRows(mining, items?.items));
      WriteExcelFile(
        GetExcelFullPath(GameDataSheetRegistry.MonsterExploreExcelKey),
        BuildWorkbookSheetRows(monsterExplore, items?.items));
    }

    public static void WriteJsonFiles(ISet<string> selected, ExportBundle bundle)
    {
      EnsureJsonDirectory();
      var written = new List<string>();

      if (selected.Contains("items"))
      {
        File.WriteAllText(GameDataPaths.GetJsonFullPath(UniverIdle.Game.GameDataPaths.ItemsRelativePath), ToItemsJson(bundle.Items), new UTF8Encoding(false));
        written.Add("items.json");
      }

      if (SelectedTouchesWorkbook(selected, GameDataSheetRegistry.ScavengeExcelKey))
      {
        File.WriteAllText(GameDataPaths.GetJsonFullPath(UniverIdle.Game.GameDataPaths.ScavengeRelativePath), ToWorkContentJson(bundle.Scavenge), new UTF8Encoding(false));
        written.Add("scavenge.json");
      }

      if (SelectedTouchesWorkbook(selected, GameDataSheetRegistry.WoodcuttingExcelKey))
      {
        File.WriteAllText(GameDataPaths.GetJsonFullPath(UniverIdle.Game.GameDataPaths.WoodcuttingRelativePath), ToWorkContentJson(bundle.Woodcutting), new UTF8Encoding(false));
        written.Add("woodcutting.json");
      }

      if (SelectedTouchesWorkbook(selected, GameDataSheetRegistry.MiningExcelKey))
      {
        File.WriteAllText(GameDataPaths.GetJsonFullPath(UniverIdle.Game.GameDataPaths.MiningRelativePath), ToWorkContentJson(bundle.Mining), new UTF8Encoding(false));
        written.Add("mining.json");
      }

      if (SelectedTouchesWorkbook(selected, GameDataSheetRegistry.MonsterExploreExcelKey))
      {
        File.WriteAllText(GameDataPaths.GetJsonFullPath(UniverIdle.Game.GameDataPaths.MonsterExploreRelativePath), ToWorkContentJson(bundle.MonsterExplore), new UTF8Encoding(false));
        written.Add("monster_explore.json");
      }

      if (written.Count == 0)
        throw new InvalidOperationException("没有可写入的 JSON 文件。");

      try
      {
        GameDataLoader.Validate();
      }
      catch (Exception ex)
      {
        var entries = selected
          .Select(id => new GameDataExportException.Entry(id, ex.Message))
          .ToArray();
        throw new GameDataExportException(entries, "导出 JSON 后校验失败：" + ex.Message, ex);
      }
    }

    public static string ToItemsJson(ItemsDataFile data)
    {
      var sb = new StringBuilder();
      sb.Append("{\n  \"version\": ").Append(data.version).Append(",\n");
      sb.Append("  \"items\": [\n");
      WriteItemRows(sb, data.items);
      sb.Append("  ]\n}");
      return sb.ToString();
    }

    public static string ToWorkContentJson(WorkContentDataFile data)
    {
      var sb = new StringBuilder();
      sb.Append("{\n  \"version\": ").Append(data.version).Append(",\n");
      sb.Append("  \"works\": [\n");
      WriteWorkRows(sb, data.works);
      sb.Append("  ],\n");
      sb.Append("  \"actions\": [\n");
      WriteActionRows(sb, data.actions);
      sb.Append("  ]\n}");
      return sb.ToString();
    }

    public static Dictionary<string, IList<string[]>> BuildWorkbookSheetRows(WorkContentDataFile data, ItemRow[] items)
    {
      return new Dictionary<string, IList<string[]>>(StringComparer.OrdinalIgnoreCase)
      {
        ["works"] = BuildWorkRows(data?.works),
        ["actions"] = BuildActionRows(data?.actions),
        ["loot"] = BuildLootRows(items, data?.actions),
      };
    }

    public static string GetExcelFullPath(string excelKey) =>
      Path.Combine(Application.dataPath, GameDataSheetRegistry.GetExcelRelativePath(excelKey).Replace('/', Path.DirectorySeparatorChar));

    public static int CountDataRows(Dictionary<string, List<string[]>> workbookSheets, GameDataSheetRegistry.SheetInfo sheet) =>
      CountDataRows(workbookSheets, sheet.TabName, sheet.Kind);

    public static int CountDataRows(string excelKey, string sheetId)
    {
      var path = GetExcelFullPath(excelKey);
      if (!File.Exists(path)) return -1;
      return CountDataRows(SimpleXlsx.Read(path), sheetId);
    }

    public static int CountDataRows(Dictionary<string, List<string[]>> workbookSheets, string sheetTab, GameDataSheetRegistry.WorkSheetKind kind)
    {
      if (workbookSheets == null ||
          !workbookSheets.TryGetValue(sheetTab, out var rows) ||
          rows == null ||
          rows.Count == 0)
        return -1;

      var headerIndex = kind switch
      {
        GameDataSheetRegistry.WorkSheetKind.Items => ResolveItemHeaderIndex(rows).headerIndex,
        GameDataSheetRegistry.WorkSheetKind.Works => FindHeaderRowIndex(rows, WorkHeaders),
        GameDataSheetRegistry.WorkSheetKind.Actions => TryResolveActionHeaderIndex(rows, out var actionHeaderIndex, out _)
          ? actionHeaderIndex
          : -1,
        GameDataSheetRegistry.WorkSheetKind.Loot => FindLootHeaderRowIndex(rows),
        _ => 0,
      };
      if (headerIndex < 0) return -1;
      return Math.Max(0, rows.Count - headerIndex - 1);
    }

    public static int CountDataRows(Dictionary<string, List<string[]>> workbookSheets, string sheetId)
    {
      var info = GameDataSheetRegistry.Get(sheetId);
      return CountDataRows(workbookSheets, info.TabName, info.Kind);
    }

    private static void WriteExcelFile(string path, IReadOnlyDictionary<string, IList<string[]>> sheets)
    {
      var dir = Path.GetDirectoryName(path);
      if (!string.IsNullOrEmpty(dir))
        Directory.CreateDirectory(dir);
      SimpleXlsx.Write(path, sheets);
    }

    private static void EnsureJsonDirectory()
    {
      var dir = Path.GetDirectoryName(GameDataPaths.GetJsonFullPath(UniverIdle.Game.GameDataPaths.ItemsRelativePath));
      if (!string.IsNullOrEmpty(dir))
        Directory.CreateDirectory(dir);
    }

    private static bool SelectedTouchesWorkbook(ISet<string> selected, string excelKey)
    {
      foreach (var sheet in GameDataSheetRegistry.All)
      {
        if (sheet.ExcelKey == excelKey && selected.Contains(sheet.Id))
          return true;
      }
      return false;
    }

    private static void ValidateItemReferences(
      ItemsDataFile items,
      WorkContentDataFile workContent,
      string actionsSheetId,
      string lootSheetId)
    {
      var itemIds = new HashSet<string>(StringComparer.Ordinal);
      if (items?.items != null)
      {
        foreach (var item in items.items)
        {
          if (!string.IsNullOrWhiteSpace(item?.id))
            itemIds.Add(item.id);
        }
      }

      if (workContent?.actions == null) return;
      foreach (var action in workContent.actions)
      {
        if (!string.IsNullOrWhiteSpace(action?.costItemId) && !itemIds.Contains(action.costItemId))
        {
          var msg = $"引用了未知道具 costItemId：{action.costItemId}（action {action.id}）";
          throw new GameDataExportException(
            new[]
            {
              new GameDataExportException.Entry("items", "道具表中缺少该 id"),
              new GameDataExportException.Entry(actionsSheetId, msg),
            },
            msg);
        }

        if (action?.loot == null) continue;
        foreach (var loot in action.loot)
        {
          if (string.IsNullOrWhiteSpace(loot?.itemId)) continue;
          if (!itemIds.Contains(loot.itemId))
          {
            var msg = $"引用了未知道具 itemId：{loot.itemId}（action {action.id}）";
            throw new GameDataExportException(
              new[]
              {
                new GameDataExportException.Entry("items", "道具表中缺少该 id"),
                new GameDataExportException.Entry(lootSheetId, msg),
              },
              msg);
          }
        }
      }
    }

    private static Dictionary<string, LootRow[]> BuildLootLookup(ActionRow[] actions)
    {
      var map = new Dictionary<string, LootRow[]>(StringComparer.Ordinal);
      if (actions == null) return map;
      foreach (var action in actions)
      {
        if (action?.id == null) continue;
        map[action.id] = action.loot ?? Array.Empty<LootRow>();
      }
      return map;
    }

    private static List<string[]> RequireSheet(
      IReadOnlyDictionary<string, Dictionary<string, List<string[]>>> excelCache,
      string sheetId)
    {
      var info = GameDataSheetRegistry.Get(sheetId);
      if (!excelCache.TryGetValue(info.ExcelKey, out var fileSheets) || fileSheets == null)
        throw new GameDataExportException(sheetId, $"未加载 Excel：{GameDataSheetRegistry.GetExcelFileName(info.ExcelKey)}");

      if (fileSheets.TryGetValue(info.TabName, out var rows)) return rows;
      throw new GameDataExportException(sheetId,
        $"{GameDataSheetRegistry.GetExcelFileName(info.ExcelKey)} 缺少工作表：{info.TabName}");
    }

    private static T ReadSheet<T>(string sheetId, Func<T> read)
    {
      try
      {
        return read();
      }
      catch (GameDataExportException)
      {
        throw;
      }
      catch (Exception ex)
      {
        throw new GameDataExportException(sheetId, ex.Message, ex);
      }
    }

    private enum ItemSheetLayout
    {
      Standard,
      WithoutCategory,
      LegacyWithColorIcon,
      LegacyWithColorNoIcon,
    }

    private static List<ItemRow> ReadItemSheet(List<string[]> rows)
    {
      if (rows == null || rows.Count == 0)
        throw new InvalidDataException("工作表为空。");

      var (headerIndex, layout) = ResolveItemHeaderIndex(rows);
      var expectedHeaders = ExpectedItemHeaders(layout);
      ValidateHeaders(NormalizeRow(rows[headerIndex]), expectedHeaders);

      var list = new List<ItemRow>();
      for (var i = headerIndex + 1; i < rows.Count; i++)
      {
        var raw = rows[i] ?? Array.Empty<string>();
        if (IsCommentOrEmpty(raw)) continue;
        if (IsHeaderEchoRow(raw, expectedHeaders)) continue;

        var data = new string[expectedHeaders.Length];
        for (var c = 0; c < expectedHeaders.Length; c++)
          data[c] = c < raw.Length ? (raw[c] ?? string.Empty).Trim() : string.Empty;
        list.Add(ParseItemRow(data, layout));
      }
      return list;
    }

    private static ItemRow ParseItemRow(string[] r, ItemSheetLayout layout) =>
      layout switch
      {
        ItemSheetLayout.Standard => new ItemRow
        {
          id = r[0],
          name = r[1],
          category = r[2],
          icon = r[3],
          description = r[4],
        },
        ItemSheetLayout.WithoutCategory => new ItemRow
        {
          id = r[0],
          name = r[1],
          category = string.Empty,
          icon = r[2],
          description = r[3],
        },
        ItemSheetLayout.LegacyWithColorIcon => new ItemRow
        {
          id = r[0],
          name = r[1],
          icon = r[3],
          description = r[4],
        },
        ItemSheetLayout.LegacyWithColorNoIcon => new ItemRow
        {
          id = r[0],
          name = r[1],
          icon = string.Empty,
          description = r[3],
        },
        _ => throw new InvalidDataException("未知 items 表布局。"),
      };

    private static string[] ExpectedItemHeaders(ItemSheetLayout layout) =>
      layout switch
      {
        ItemSheetLayout.Standard => ItemHeaders,
        ItemSheetLayout.WithoutCategory => ItemHeadersWithoutCategory,
        ItemSheetLayout.LegacyWithColorIcon => ItemHeadersLegacyColorIcon,
        ItemSheetLayout.LegacyWithColorNoIcon => ItemHeadersLegacyColorNoIcon,
        _ => ItemHeaders,
      };

    private static (int headerIndex, ItemSheetLayout layout) ResolveItemHeaderIndex(List<string[]> rows)
    {
      for (var i = 0; i < rows.Count && i < 5; i++)
      {
        var header = NormalizeRow(rows[i]);
        if (HeadersMatch(header, ItemHeaders))
          return (i, ItemSheetLayout.Standard);
        if (HeadersMatch(header, ItemHeadersWithoutCategory))
          return (i, ItemSheetLayout.WithoutCategory);
        if (HeadersMatch(header, ItemHeadersLegacyColorIcon))
          return (i, ItemSheetLayout.LegacyWithColorIcon);
        if (HeadersMatch(header, ItemHeadersLegacyColorNoIcon))
          return (i, ItemSheetLayout.LegacyWithColorNoIcon);
      }
      throw new InvalidDataException($"找不到有效 items 表头行（应含 {ItemHeaders[0]} 等英文字段名）。");
    }

    private static List<WorkRow> ReadWorkSheet(List<string[]> rows) =>
      MapRows(rows, WorkHeaders, r => new WorkRow
      {
        id = r[0],
        name = r[1],
        locationName = r[2],
        xpBase = ParseInt(r[3], 40),
        xpPerLevel = ParseInt(r[4], 20),
        sceneXpBase = ParseInt(r[5], 0),
        sceneXpPerLevel = ParseInt(r[6], 0),
        grantWorkXp = ParseInt(r[7], 1),
        grantSceneXp = ParseInt(r[8], 1),
      });

    private static List<ActionRow> ReadActionSheet(List<string[]> rows)
    {
      if (rows == null || rows.Count == 0)
        throw new InvalidDataException("工作表为空。");

      var (headerIndex, layout) = ResolveActionHeaderIndexWithLayout(rows);
      ValidateHeaders(NormalizeRow(rows[headerIndex]), ExpectedActionHeaders(layout));

      var list = new List<ActionRow>();
      for (var i = headerIndex + 1; i < rows.Count; i++)
      {
        var raw = rows[i] ?? Array.Empty<string>();
        if (IsCommentOrEmpty(raw)) continue;
        if (IsHeaderEchoRow(raw, ActionHeaders) ||
            IsHeaderEchoRow(raw, ActionHeadersLegacyWithCostNoGold) ||
            IsHeaderEchoRow(raw, ActionHeadersLegacyNoCost))
          continue;

        var data = new string[ActionHeaders.Length];
        for (var c = 0; c < ActionHeaders.Length; c++)
          data[c] = c < raw.Length ? (raw[c] ?? string.Empty).Trim() : string.Empty;
        list.Add(ParseActionRow(data));
      }
      return list;
    }

    private static ActionRow ParseActionRow(string[] r) =>
      new ActionRow
      {
        id = r[0],
        workId = r[1],
        sceneId = r[2],
        sceneName = r[3],
        spotName = r[4],
        displayName = r[5],
        durationSeconds = ParseFloat(r[6]),
        xpReward = ParseInt(r[7]),
        requiredWorkLevel = ParseInt(r[8], 1),
        description = r[9],
        thumbImage = NormalizeThumbImageCell(r[10]),
        costItemId = r.Length > 11 ? r[11] : string.Empty,
        costAmount = r.Length > 12 ? ParseInt(r[12]) : 0,
        goldChance = r.Length > 13 ? ParseFloat(r[13]) : 0f,
        goldMin = r.Length > 14 ? ParseInt(r[14]) : 0,
        goldMax = r.Length > 15 ? ParseInt(r[15]) : 0,
      };

    private static bool TryResolveActionHeaderIndex(List<string[]> rows, out int headerIndex, out ActionSheetLayout layout)
    {
      for (var i = 0; i < rows.Count && i < 5; i++)
      {
        var header = NormalizeRow(rows[i]);
        if (HeadersMatch(header, ActionHeaders))
        {
          headerIndex = i;
          layout = ActionSheetLayout.Standard;
          return true;
        }

        if (HeadersMatch(header, ActionHeadersLegacyWithCostNoGold))
        {
          headerIndex = i;
          layout = ActionSheetLayout.LegacyWithCostNoGold;
          return true;
        }

        if (HeadersMatch(header, ActionHeadersLegacyNoCost))
        {
          headerIndex = i;
          layout = ActionSheetLayout.LegacyNoCost;
          return true;
        }
      }

      headerIndex = -1;
      layout = ActionSheetLayout.Standard;
      return false;
    }

    private static (int headerIndex, ActionSheetLayout layout) ResolveActionHeaderIndexWithLayout(List<string[]> rows)
    {
      if (TryResolveActionHeaderIndex(rows, out var headerIndex, out var layout))
        return (headerIndex, layout);
      throw new InvalidDataException($"找不到有效 actions 表头行（应含 {ActionHeaders[0]} 等英文字段名，且含 spotName 列）。");
    }

    private static int ResolveActionHeaderIndex(List<string[]> rows) =>
      ResolveActionHeaderIndexWithLayout(rows).headerIndex;

    private static string[] ExpectedActionHeaders(ActionSheetLayout layout) =>
      layout switch
      {
        ActionSheetLayout.LegacyWithCostNoGold => ActionHeadersLegacyWithCostNoGold,
        ActionSheetLayout.LegacyNoCost => ActionHeadersLegacyNoCost,
        _ => ActionHeaders,
      };

    private static string NormalizeThumbImageCell(string value)
    {
      if (string.IsNullOrWhiteSpace(value)) return string.Empty;
      var trimmed = value.Trim();
      return trimmed.StartsWith("#", StringComparison.Ordinal) ? string.Empty : trimmed;
    }

    private static Dictionary<string, List<LootRow>> ReadLootSheet(List<string[]> rows)
    {
      if (rows == null || rows.Count == 0)
        throw new InvalidDataException("工作表为空。");

      var (headerIndex, extended) = ResolveLootHeader(rows);
      var header = NormalizeRow(rows[headerIndex]);
      ValidateLootHeaders(header, extended);

      var result = new Dictionary<string, List<LootRow>>();
      for (var i = headerIndex + 1; i < rows.Count; i++)
      {
        var raw = rows[i] ?? Array.Empty<string>();
        if (IsCommentOrEmpty(raw)) continue;

        var actionId = GetLootCell(raw, extended, "actionId");
        if (string.IsNullOrWhiteSpace(actionId)) continue;
        if (string.Equals(actionId, "actionId", StringComparison.OrdinalIgnoreCase)) continue;
        if (!result.TryGetValue(actionId, out var list))
        {
          list = new List<LootRow>();
          result[actionId] = list;
        }
        list.Add(new LootRow
        {
          itemId = GetLootCell(raw, extended, "itemId"),
          chance = ParseFloat(GetLootCell(raw, extended, "chance")),
          min = ParseInt(GetLootCell(raw, extended, "min"), 1),
          max = ParseInt(GetLootCell(raw, extended, "max"), 1),
        });
      }
      return result;
    }

    private static (int headerIndex, bool extended) ResolveLootHeader(List<string[]> rows)
    {
      for (var i = 0; i < rows.Count && i < 5; i++)
      {
        var header = NormalizeRow(rows[i]);
        if (HeadersMatch(header, LootExcelHeaders))
          return (i, true);
        if (HeadersMatch(header, LootHeaders))
          return (i, false);
      }
      throw new InvalidDataException("loot 表找不到有效表头行（第 2 行应为英文字段名）。");
    }

    private static int FindLootHeaderRowIndex(List<string[]> rows)
    {
      for (var i = 0; i < rows.Count && i < 5; i++)
      {
        var header = NormalizeRow(rows[i]);
        if (HeadersMatch(header, LootExcelHeaders) || HeadersMatch(header, LootHeaders))
          return i;
      }
      return 0;
    }

    private static void ValidateLootHeaders(string[] header, bool extended)
    {
      var expected = extended ? LootExcelHeaders : LootHeaders;
      ValidateHeaders(header, expected);
    }

    private static string GetLootCell(string[] row, bool extended, string column)
    {
      var index = column switch
      {
        "actionId" => 0,
        "itemId" => 1,
        "chance" => extended ? 3 : 2,
        "min" => extended ? 4 : 3,
        "max" => extended ? 5 : 4,
        _ => -1,
      };
      if (index < 0 || index >= row.Length) return string.Empty;
      return (row[index] ?? string.Empty).Trim();
    }

    private static List<T> MapRows<T>(List<string[]> rows, string[] headers, Func<string[], T> map)
    {
      var list = new List<T>();
      foreach (var r in MapDataRows(rows, headers))
        list.Add(map(r));
      return list;
    }

    private static IEnumerable<string[]> MapDataRows(List<string[]> rows, string[] expectedHeaders)
    {
      if (rows == null || rows.Count == 0)
        throw new InvalidDataException("工作表为空。");

      var headerIndex = FindHeaderRowIndex(rows, expectedHeaders);
      var header = NormalizeRow(rows[headerIndex]);
      ValidateHeaders(header, expectedHeaders);

      for (var i = headerIndex + 1; i < rows.Count; i++)
      {
        var raw = rows[i] ?? Array.Empty<string>();
        if (IsCommentOrEmpty(raw)) continue;
        if (IsHeaderEchoRow(raw, expectedHeaders)) continue;
        var data = new string[expectedHeaders.Length];
        for (var c = 0; c < expectedHeaders.Length; c++)
          data[c] = c < raw.Length ? (raw[c] ?? string.Empty).Trim() : string.Empty;
        yield return data;
      }
    }

    private static int FindHeaderRowIndex(List<string[]> rows, string[] expectedHeaders)
    {
      for (var i = 0; i < rows.Count && i < 5; i++)
      {
        if (HeadersMatch(NormalizeRow(rows[i]), expectedHeaders))
          return i;
      }
      throw new InvalidDataException($"找不到有效表头行（应含 {expectedHeaders[0]} 等英文字段名）。");
    }

    private static bool HeadersMatch(string[] header, string[] expected)
    {
      for (var i = 0; i < expected.Length; i++)
      {
        var actual = i < header.Length ? header[i] : string.Empty;
        if (!string.Equals(actual, expected[i], StringComparison.OrdinalIgnoreCase))
          return false;
      }
      return true;
    }

    private static bool IsHeaderEchoRow(string[] row, string[] expectedHeaders)
    {
      if (row == null || row.Length == 0) return false;
      return string.Equals((row[0] ?? string.Empty).Trim(), expectedHeaders[0], StringComparison.OrdinalIgnoreCase);
    }

    private static void ValidateHeaders(string[] header, string[] expected)
    {
      for (var i = 0; i < expected.Length; i++)
      {
        var actual = i < header.Length ? header[i] : string.Empty;
        if (!string.Equals(actual, expected[i], StringComparison.OrdinalIgnoreCase))
          throw new InvalidDataException($"表头第 {i + 1} 列应为 {expected[i]}，实际为 {actual}");
      }

      for (var i = expected.Length; i < header.Length; i++)
      {
        var extra = header[i];
        if (string.IsNullOrEmpty(extra)) continue;
        if (!extra.StartsWith("#", StringComparison.Ordinal))
          throw new InvalidDataException(
            $"表头第 {i + 1} 列「{extra}」为扩展列，请以 # 开头（仅策划阅读，不导出），例如 #itemName");
      }
    }

    private static string[] NormalizeRow(string[] row) =>
      row?.Select(c => (c ?? string.Empty).Trim()).ToArray() ?? Array.Empty<string>();

    private static bool IsCommentOrEmpty(string[] row)
    {
      if (row == null || row.Length == 0) return true;
      var first = (row[0] ?? string.Empty).Trim();
      return first.Length == 0 || first.StartsWith("#");
    }

    private static int ParseInt(string s, int fallback = 0) =>
      int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : fallback;

    private static float ParseFloat(string s) =>
      float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : 0f;

    private static List<string[]> BuildItemRows(ItemRow[] items)
    {
      var rows = new List<string[]> { ItemHeaderComments, ItemHeaders };
      if (items == null) return rows;
      foreach (var item in items)
      {
        rows.Add(new[]
        {
          item.id, item.name, item.category ?? string.Empty, item.icon ?? string.Empty, item.description,
        });
      }
      return rows;
    }

    private static List<string[]> BuildWorkRows(WorkRow[] works)
    {
      var rows = new List<string[]> { WorkHeaderComments, WorkHeaders };
      if (works == null) return rows;
      foreach (var work in works)
      {
        rows.Add(new[]
        {
          work.id, work.name, work.locationName,
          work.xpBase.ToString(CultureInfo.InvariantCulture),
          work.xpPerLevel.ToString(CultureInfo.InvariantCulture),
          work.sceneXpBase.ToString(CultureInfo.InvariantCulture),
          work.sceneXpPerLevel.ToString(CultureInfo.InvariantCulture),
          work.grantWorkXp.ToString(CultureInfo.InvariantCulture),
          work.grantSceneXp.ToString(CultureInfo.InvariantCulture),
        });
      }
      return rows;
    }

    private static List<string[]> BuildActionRows(ActionRow[] actions)
    {
      var rows = new List<string[]> { ActionHeaderComments, ActionHeaders };
      if (actions == null) return rows;
      foreach (var action in actions)
      {
        rows.Add(new[]
        {
          action.id, action.workId, action.sceneId, action.sceneName, action.spotName, action.displayName,
          action.durationSeconds.ToString(CultureInfo.InvariantCulture),
          action.xpReward.ToString(CultureInfo.InvariantCulture),
          action.requiredWorkLevel.ToString(CultureInfo.InvariantCulture),
          action.description, action.thumbImage ?? string.Empty,
          action.costItemId ?? string.Empty,
          action.costAmount.ToString(CultureInfo.InvariantCulture),
          action.goldChance.ToString(CultureInfo.InvariantCulture),
          action.goldMin.ToString(CultureInfo.InvariantCulture),
          action.goldMax.ToString(CultureInfo.InvariantCulture),
        });
      }
      return rows;
    }

    private static List<string[]> BuildLootRows(ItemRow[] items, ActionRow[] actions)
    {
      var itemNames = BuildItemNameLookup(items);
      var rows = new List<string[]> { LootExcelHeaderComments, LootExcelHeaders };
      if (actions == null) return rows;
      foreach (var action in actions)
      {
        if (action.loot == null) continue;
        foreach (var loot in action.loot)
        {
          rows.Add(new[]
          {
            action.id,
            loot.itemId,
            LookupItemDisplayName(itemNames, loot.itemId),
            loot.chance.ToString(CultureInfo.InvariantCulture),
            loot.min.ToString(CultureInfo.InvariantCulture),
            loot.max.ToString(CultureInfo.InvariantCulture),
          });
        }
      }
      return rows;
    }

    private static Dictionary<string, string> BuildItemNameLookup(ItemRow[] items)
    {
      var map = new Dictionary<string, string>(StringComparer.Ordinal);
      if (items == null) return map;
      foreach (var item in items)
      {
        if (string.IsNullOrWhiteSpace(item?.id)) continue;
        map[item.id] = item.name ?? string.Empty;
      }
      return map;
    }

    private static string LookupItemDisplayName(IReadOnlyDictionary<string, string> itemNames, string itemId)
    {
      if (string.IsNullOrWhiteSpace(itemId)) return string.Empty;
      return itemNames.TryGetValue(itemId, out var name) ? name : string.Empty;
    }

    private static void WriteItemRows(StringBuilder sb, ItemRow[] items)
    {
      if (items == null || items.Length == 0) return;
      for (var i = 0; i < items.Length; i++)
      {
        var item = items[i];
        sb.Append("    {\n");
        sb.Append("      \"id\": ").Append(Q(item.id)).Append(",\n");
        sb.Append("      \"name\": ").Append(Q(item.name)).Append(",\n");
        if (!string.IsNullOrEmpty(item.category))
          sb.Append("      \"category\": ").Append(Q(item.category)).Append(",\n");
        if (!string.IsNullOrEmpty(item.icon))
          sb.Append("      \"icon\": ").Append(Q(item.icon)).Append(",\n");
        sb.Append("      \"description\": ").Append(Q(item.description)).Append("\n");
        sb.Append("    }");
        if (i < items.Length - 1) sb.Append(",");
        sb.Append("\n");
      }
    }

    private static void WriteWorkRows(StringBuilder sb, WorkRow[] works)
    {
      if (works == null || works.Length == 0) return;
      for (var i = 0; i < works.Length; i++)
      {
        var work = works[i];
        sb.Append("    {\n");
        sb.Append("      \"id\": ").Append(Q(work.id)).Append(",\n");
        sb.Append("      \"name\": ").Append(Q(work.name)).Append(",\n");
        sb.Append("      \"locationName\": ").Append(Q(work.locationName)).Append(",\n");
        sb.Append("      \"xpBase\": ").Append(work.xpBase).Append(",\n");
        sb.Append("      \"xpPerLevel\": ").Append(work.xpPerLevel).Append(",\n");
        sb.Append("      \"sceneXpBase\": ").Append(work.sceneXpBase).Append(",\n");
        sb.Append("      \"sceneXpPerLevel\": ").Append(work.sceneXpPerLevel).Append(",\n");
        sb.Append("      \"grantWorkXp\": ").Append(work.grantWorkXp).Append(",\n");
        sb.Append("      \"grantSceneXp\": ").Append(work.grantSceneXp).Append("\n");
        sb.Append("    }");
        if (i < works.Length - 1) sb.Append(",");
        sb.Append("\n");
      }
    }

    private static void WriteActionRows(StringBuilder sb, ActionRow[] actions)
    {
      if (actions == null || actions.Length == 0) return;
      for (var i = 0; i < actions.Length; i++)
      {
        var action = actions[i];
        sb.Append("    {\n");
        sb.Append("      \"id\": ").Append(Q(action.id)).Append(",\n");
        sb.Append("      \"workId\": ").Append(Q(action.workId)).Append(",\n");
        sb.Append("      \"sceneId\": ").Append(Q(action.sceneId)).Append(",\n");
        sb.Append("      \"sceneName\": ").Append(Q(action.sceneName)).Append(",\n");
        sb.Append("      \"spotName\": ").Append(Q(action.spotName)).Append(",\n");
        sb.Append("      \"displayName\": ").Append(Q(action.displayName)).Append(",\n");
        sb.Append("      \"durationSeconds\": ").Append(action.durationSeconds.ToString(CultureInfo.InvariantCulture)).Append(",\n");
        sb.Append("      \"xpReward\": ").Append(action.xpReward).Append(",\n");
        sb.Append("      \"requiredWorkLevel\": ").Append(action.requiredWorkLevel).Append(",\n");
        sb.Append("      \"description\": ").Append(Q(action.description));
        if (!string.IsNullOrWhiteSpace(action.thumbImage))
          sb.Append(",\n      \"thumbImage\": ").Append(Q(action.thumbImage));
        if (!string.IsNullOrWhiteSpace(action.costItemId) && action.costAmount > 0)
        {
          sb.Append(",\n      \"costItemId\": ").Append(Q(action.costItemId));
          sb.Append(",\n      \"costAmount\": ").Append(action.costAmount);
        }
        if (action.goldChance > 0f && action.goldMax > 0)
        {
          sb.Append(",\n      \"goldChance\": ").Append(action.goldChance.ToString(CultureInfo.InvariantCulture));
          sb.Append(",\n      \"goldMin\": ").Append(action.goldMin > 0 ? action.goldMin : 1);
          sb.Append(",\n      \"goldMax\": ").Append(action.goldMax);
        }
        sb.Append(",\n      \"loot\": [\n");
        WriteLootRows(sb, action.loot);
        sb.Append("      ]\n");
        sb.Append("    }");
        if (i < actions.Length - 1) sb.Append(",");
        sb.Append("\n");
      }
    }

    private static void WriteLootRows(StringBuilder sb, LootRow[] loot)
    {
      if (loot == null || loot.Length == 0) return;
      for (var i = 0; i < loot.Length; i++)
      {
        var row = loot[i];
        sb.Append("        { \"itemId\": ").Append(Q(row.itemId))
          .Append(", \"chance\": ").Append(row.chance.ToString(CultureInfo.InvariantCulture));
        if (row.min > 0 && row.min != 1)
          sb.Append(", \"min\": ").Append(row.min);
        if (row.max > 0 && row.max != 1 && row.max != row.min)
          sb.Append(", \"max\": ").Append(row.max);
        sb.Append(" }");
        if (i < loot.Length - 1) sb.Append(",");
        sb.Append("\n");
      }
    }

    private static string Q(string value) => "\"" + EscapeJson(value ?? string.Empty) + "\"";

    private static string EscapeJson(string s) =>
      s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "");
  }
}
#endif
