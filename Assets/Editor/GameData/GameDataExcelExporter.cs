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
    private static readonly string[] ItemHeaders = { "id", "name", "color", "description" };
    private static readonly string[] WorkHeaders =
    {
      "id", "name", "locationName", "iconColor",
      "xpBase", "xpPerLevel", "sceneXpBase", "sceneXpPerLevel", "grantWorkXp", "grantSceneXp",
    };
    private static readonly string[] ActionHeaders =
    {
      "id", "workId", "sceneId", "sceneName", "displayName",
      "durationSeconds", "xpReward", "requiredWorkLevel", "description", "thumbColor",
      "costItemId", "costAmount",
    };
    private static readonly string[] LootHeaders = { "actionId", "itemId", "chance", "min", "max" };
    private static readonly string[] LootExcelHeaders = { "actionId", "itemId", "#itemName", "chance", "min", "max" };

    private static readonly string[] ItemHeaderComments = { "道具ID", "显示名称", "颜色", "描述" };
    private static readonly string[] WorkHeaderComments =
    {
      "工作ID", "工作名称", "地点名称", "图标颜色",
      "工作经验基数", "每级额外工作经验", "地区熟练度基数", "地区熟练度每级增量", "是否加工作XP(1/0)", "是否加地区XP(1/0)",
    };
    private static readonly string[] ActionHeaderComments =
    {
      "动作ID", "所属工作", "地区ID", "地区名称", "卡片标题",
      "时长(秒)", "完成经验", "解锁所需工作等级", "描述", "缩略图颜色",
      "消耗道具ID", "消耗数量",
    };
    private static readonly string[] LootExcelHeaderComments =
      { "动作ID", "道具ID", "道具名(不导出)", "概率0~1", "最少数量", "最多数量" };

    public readonly struct ExportBundle
    {
      public readonly ItemsDataFile Items;
      public readonly ScavengeDataFile Scavenge;
      public readonly WoodcuttingDataFile Woodcutting;
      public readonly MonsterExploreDataFile MonsterExplore;

      public ExportBundle(ItemsDataFile items, ScavengeDataFile scavenge, WoodcuttingDataFile woodcutting, MonsterExploreDataFile monsterExplore)
      {
        Items = items;
        Scavenge = scavenge;
        Woodcutting = woodcutting;
        MonsterExplore = monsterExplore;
      }
    }

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

      Debug.Log($"[UniverIdle] 导表完成（{string.Join(", ", selected)}）→ items / scavenge / woodcutting / monster_explore JSON");
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
          throw new FileNotFoundException("找不到 Excel 文件：" + path, path);
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

      var items = GameDataLoader.LoadItemsFileIfPresent();
      var scavenge = GameDataLoader.LoadScavengeFileIfPresent();
      var woodcutting = GameDataLoader.LoadWoodcuttingFileIfPresent();
      var monsterExplore = GameDataLoader.LoadMonsterExploreFileIfPresent();

      if (selected.Contains("items"))
        items.items = ReadItemSheet(RequireSheet(excelCache, "items")).ToArray();

      MergeWorkContent(scavenge, excelCache, selected, "scavenge_works", "scavenge_actions", "scavenge_loot", "scavenge.json");
      MergeWorkContent(woodcutting, excelCache, selected, "woodcutting_works", "woodcutting_actions", "woodcutting_loot", "woodcutting.json");
      MergeWorkContent(monsterExplore, excelCache, selected, "monster_explore_works", "monster_explore_actions", "monster_explore_loot", "monster_explore.json");

      items.version = Math.Max(items.version, 3);
      ValidateItemReferences(items, scavenge);
      ValidateItemReferences(items, woodcutting);
      ValidateItemReferences(items, monsterExplore);
      return new ExportBundle(items, scavenge, woodcutting, monsterExplore);
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
        data.works = ReadWorkSheet(RequireSheet(excelCache, worksSheetId)).ToArray();

      if (!exportActions && !exportLoot) return;

      var actions = exportActions
        ? ReadActionSheet(RequireSheet(excelCache, actionsSheetId))
        : (data.actions ?? Array.Empty<ActionRow>()).ToList();

      var lootByAction = exportLoot
        ? ReadLootSheet(RequireSheet(excelCache, lootSheetId))
        : null;

      var oldLoot = BuildLootLookup(data.actions);

      foreach (var action in actions)
      {
        if (lootByAction != null)
          action.loot = lootByAction.TryGetValue(action.id, out var loot) ? loot.ToArray() : Array.Empty<LootRow>();
        else if (!exportActions && exportLoot)
          throw new InvalidOperationException($"仅导出 loot 时，{jsonFileName} 中需已有 actions 数据。");
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
      var monsterExplorePath = GetExcelFullPath(GameDataSheetRegistry.MonsterExploreExcelKey);
      if (File.Exists(itemsPath) && File.Exists(scavengePath) && File.Exists(woodcuttingPath) && File.Exists(monsterExplorePath))
        return;

      Debug.LogWarning("[UniverIdle] Excel 缺失，正在从 JSON 生成模板…");
      CreateExcelFromJson();
    }

    public static void CreateExcelFromJson()
    {
      var items = GameDataLoader.LoadItemsFileIfPresent();
      var scavenge = GameDataLoader.LoadScavengeFileIfPresent();
      var woodcutting = GameDataLoader.LoadWoodcuttingFileIfPresent();
      var monsterExplore = GameDataLoader.LoadMonsterExploreFileIfPresent();
      WriteExcelTemplates(items, scavenge, woodcutting, monsterExplore);
    }

    public static void OpenExcel(string excelKey)
    {
      EnsureExcelFiles();
      AssetDatabase.Refresh();
      EditorUtility.RevealInFinder(GetExcelFullPath(excelKey));
    }

    public static void WriteExcelTemplates(ItemsDataFile items, ScavengeDataFile scavenge, WoodcuttingDataFile woodcutting, MonsterExploreDataFile monsterExplore)
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
        GetExcelFullPath(GameDataSheetRegistry.MonsterExploreExcelKey),
        BuildWorkbookSheetRows(monsterExplore, items?.items));
    }

    public static void WriteJsonFiles(ISet<string> selected, ExportBundle bundle)
    {
      EnsureJsonDirectory();
      var written = new List<string>();

      if (selected.Contains("items"))
      {
        File.WriteAllText(GetItemsJsonFullPath(), ToItemsJson(bundle.Items), new UTF8Encoding(false));
        written.Add("items.json");
      }

      if (SelectedTouchesWorkbook(selected, GameDataSheetRegistry.ScavengeExcelKey))
      {
        File.WriteAllText(GetScavengeJsonFullPath(), ToWorkContentJson(bundle.Scavenge), new UTF8Encoding(false));
        written.Add("scavenge.json");
      }

      if (SelectedTouchesWorkbook(selected, GameDataSheetRegistry.WoodcuttingExcelKey))
      {
        File.WriteAllText(GetWoodcuttingJsonFullPath(), ToWorkContentJson(bundle.Woodcutting), new UTF8Encoding(false));
        written.Add("woodcutting.json");
      }

      if (SelectedTouchesWorkbook(selected, GameDataSheetRegistry.MonsterExploreExcelKey))
      {
        File.WriteAllText(GetMonsterExploreJsonFullPath(), ToWorkContentJson(bundle.MonsterExplore), new UTF8Encoding(false));
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
        throw new InvalidDataException("导出 JSON 后校验失败：" + ex.Message, ex);
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

    public static string GetExcelFullPath(string excelKey)
    {
      var relative = GameDataSheetRegistry.GetExcelRelativePath(excelKey);
      return Path.Combine(Application.dataPath, relative.Replace('/', Path.DirectorySeparatorChar));
    }

    public static string GetItemsJsonFullPath() =>
      Path.Combine(Application.streamingAssetsPath, UniverIdle.Game.GameDataPaths.ItemsRelativePath);

    public static string GetScavengeJsonFullPath() =>
      Path.Combine(Application.streamingAssetsPath, UniverIdle.Game.GameDataPaths.ScavengeRelativePath);

    public static string GetWoodcuttingJsonFullPath() =>
      Path.Combine(Application.streamingAssetsPath, UniverIdle.Game.GameDataPaths.WoodcuttingRelativePath);

    public static string GetMonsterExploreJsonFullPath() =>
      Path.Combine(Application.streamingAssetsPath, UniverIdle.Game.GameDataPaths.MonsterExploreRelativePath);

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
        GameDataSheetRegistry.WorkSheetKind.Items => FindHeaderRowIndex(rows, ItemHeaders),
        GameDataSheetRegistry.WorkSheetKind.Works => FindHeaderRowIndex(rows, WorkHeaders),
        GameDataSheetRegistry.WorkSheetKind.Actions => FindHeaderRowIndex(rows, ActionHeaders),
        GameDataSheetRegistry.WorkSheetKind.Loot => FindLootHeaderRowIndex(rows),
        _ => 0,
      };
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
      var dir = Path.GetDirectoryName(GetItemsJsonFullPath());
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

    private static void ValidateItemReferences(ItemsDataFile items, WorkContentDataFile workContent)
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
          throw new InvalidDataException($"actions 引用了未知道具 costItemId：{action.costItemId}（action {action.id}）");

        if (action?.loot == null) continue;
        foreach (var loot in action.loot)
        {
          if (string.IsNullOrWhiteSpace(loot?.itemId)) continue;
          if (!itemIds.Contains(loot.itemId))
            throw new InvalidDataException($"loot 引用了未知道具 itemId：{loot.itemId}（action {action.id}）");
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
        throw new InvalidDataException($"未加载 Excel：{GameDataSheetRegistry.GetExcelFileName(info.ExcelKey)}");

      if (fileSheets.TryGetValue(info.TabName, out var rows)) return rows;
      throw new InvalidDataException($"{GameDataSheetRegistry.GetExcelFileName(info.ExcelKey)} 缺少工作表：{info.TabName}");
    }

    private static List<ItemRow> ReadItemSheet(List<string[]> rows) =>
      MapRows(rows, ItemHeaders, r => new ItemRow
      {
        id = r[0],
        name = r[1],
        color = r[2],
        description = r[3],
      });

    private static List<WorkRow> ReadWorkSheet(List<string[]> rows) =>
      MapRows(rows, WorkHeaders, r => new WorkRow
      {
        id = r[0],
        name = r[1],
        locationName = r[2],
        iconColor = r[3],
        xpBase = ParseInt(r[4], 40),
        xpPerLevel = ParseInt(r[5], 20),
        sceneXpBase = ParseInt(r[6], 0),
        sceneXpPerLevel = ParseInt(r[7], 0),
        grantWorkXp = ParseInt(r[8], 1),
        grantSceneXp = ParseInt(r[9], 1),
      });

    private static List<ActionRow> ReadActionSheet(List<string[]> rows) =>
      MapRows(rows, ActionHeaders, r => new ActionRow
      {
        id = r[0],
        workId = r[1],
        sceneId = r[2],
        sceneName = r[3],
        displayName = r[4],
        durationSeconds = ParseFloat(r[5]),
        xpReward = ParseInt(r[6]),
        requiredWorkLevel = ParseInt(r[7], 1),
        description = r[8],
        thumbColor = r[9],
        costItemId = r.Length > 10 ? r[10] : string.Empty,
        costAmount = r.Length > 11 ? ParseInt(r[11]) : 0,
      });

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
          item.id, item.name, item.color, item.description,
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
          work.id, work.name, work.locationName, work.iconColor,
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
          action.id, action.workId, action.sceneId, action.sceneName, action.displayName,
          action.durationSeconds.ToString(CultureInfo.InvariantCulture),
          action.xpReward.ToString(CultureInfo.InvariantCulture),
          action.requiredWorkLevel.ToString(CultureInfo.InvariantCulture),
          action.description, action.thumbColor,
          action.costItemId ?? string.Empty,
          action.costAmount.ToString(CultureInfo.InvariantCulture),
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
        sb.Append("      \"color\": ").Append(Q(item.color)).Append(",\n");
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
        sb.Append("      \"iconColor\": ").Append(Q(work.iconColor)).Append(",\n");
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
        sb.Append("      \"displayName\": ").Append(Q(action.displayName)).Append(",\n");
        sb.Append("      \"durationSeconds\": ").Append(action.durationSeconds.ToString(CultureInfo.InvariantCulture)).Append(",\n");
        sb.Append("      \"xpReward\": ").Append(action.xpReward).Append(",\n");
        sb.Append("      \"requiredWorkLevel\": ").Append(action.requiredWorkLevel).Append(",\n");
        sb.Append("      \"description\": ").Append(Q(action.description)).Append(",\n");
        sb.Append("      \"thumbColor\": ").Append(Q(action.thumbColor));
        if (!string.IsNullOrWhiteSpace(action.costItemId) && action.costAmount > 0)
        {
          sb.Append(",\n      \"costItemId\": ").Append(Q(action.costItemId));
          sb.Append(",\n      \"costAmount\": ").Append(action.costAmount);
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
