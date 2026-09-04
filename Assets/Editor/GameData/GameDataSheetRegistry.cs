#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace UniverIdle.Editor
{
  public static class GameDataSheetRegistry
  {
    public const string ItemsExcelKey = "items";
    public const string ScavengeExcelKey = "scavenge";
    public const string WoodcuttingExcelKey = "woodcutting";
    public const string MiningExcelKey = "mining";
    public const string MonsterExploreExcelKey = "monster_explore";

    public enum WorkSheetKind
    {
      Items,
      Works,
      Actions,
      Loot,
      WorkLevels,
      ActionLevels,
    }

    public readonly struct SheetInfo
    {
      public readonly string Id;
      public readonly string ExcelKey;
      public readonly string TabName;
      public readonly WorkSheetKind Kind;
      public readonly string Title;
      public readonly string Description;

      public SheetInfo(string id, string excelKey, string tabName, WorkSheetKind kind, string title, string description)
      {
        Id = id;
        ExcelKey = excelKey;
        TabName = tabName;
        Kind = kind;
        Title = title;
        Description = description;
      }
    }

    public static readonly SheetInfo[] All =
    {
      new SheetInfo(
        "items", ItemsExcelKey, "items", WorkSheetKind.Items,
        "物品 · items", "全游戏共用道具；category 分类；icon 列填图标名（如 item_rag）或 Resources 路径，空则自动 item_{id}"),
      new SheetInfo(
        "scavenge_works", ScavengeExcelKey, "works", WorkSheetKind.Works,
        "拾荒 · works", "拾荒工作参数；xp 公式仅作无等级表时兜底"),
      new SheetInfo(
        "scavenge_work_levels", ScavengeExcelKey, "work_levels", WorkSheetKind.WorkLevels,
        "拾荒 · work_levels", "拾荒总等级 1–160：每级升到下级所需经验"),
      new SheetInfo(
        "scavenge_action_levels", ScavengeExcelKey, "action_levels", WorkSheetKind.ActionLevels,
        "拾荒 · action_levels", "各动作独立熟练度曲线 1–160（进度按 actionId）"),
      new SheetInfo(
        "scavenge_actions", ScavengeExcelKey, "actions", WorkSheetKind.Actions,
        "拾荒 · actions", "各地区子地点；description=右侧详情文案，每条不同；cost 列可留空"),
      new SheetInfo(
        "scavenge_loot", ScavengeExcelKey, "loot", WorkSheetKind.Loot,
        "拾荒 · loot", "掉落：actionId + itemId；chance=权重，每次随机 1 种；#itemName 列仅方便阅读，不导出"),
      new SheetInfo(
        "woodcutting_works", WoodcuttingExcelKey, "works", WorkSheetKind.Works,
        "砍树 · works", "砍树工作参数；xp 公式仅作无等级表时兜底"),
      new SheetInfo(
        "woodcutting_work_levels", WoodcuttingExcelKey, "work_levels", WorkSheetKind.WorkLevels,
        "砍树 · work_levels", "砍树总等级 1–160：每级升到下级所需经验"),
      new SheetInfo(
        "woodcutting_action_levels", WoodcuttingExcelKey, "action_levels", WorkSheetKind.ActionLevels,
        "砍树 · action_levels", "各树种动作独立熟练度曲线 1–160"),
      new SheetInfo(
        "woodcutting_actions", WoodcuttingExcelKey, "actions", WorkSheetKind.Actions,
        "砍树 · actions", "各树种动作；cost 列可留空"),
      new SheetInfo(
        "woodcutting_loot", WoodcuttingExcelKey, "loot", WorkSheetKind.Loot,
        "砍树 · loot", "掉落：actionId + itemId；#itemName 列仅方便阅读，不导出"),
      new SheetInfo(
        "mining_works", MiningExcelKey, "works", WorkSheetKind.Works,
        "挖矿 · works", "挖矿工作参数；xp 公式仅作无等级表时兜底"),
      new SheetInfo(
        "mining_work_levels", MiningExcelKey, "work_levels", WorkSheetKind.WorkLevels,
        "挖矿 · work_levels", "挖矿总等级 1–160：每级升到下级所需经验"),
      new SheetInfo(
        "mining_action_levels", MiningExcelKey, "action_levels", WorkSheetKind.ActionLevels,
        "挖矿 · action_levels", "各矿脉动作独立熟练度曲线 1–160"),
      new SheetInfo(
        "mining_actions", MiningExcelKey, "actions", WorkSheetKind.Actions,
        "挖矿 · actions", "各矿脉动作；cost 列可留空"),
      new SheetInfo(
        "mining_loot", MiningExcelKey, "loot", WorkSheetKind.Loot,
        "挖矿 · loot", "掉落：actionId + itemId；#itemName 列仅方便阅读，不导出"),
      new SheetInfo(
        "monster_explore_works", MonsterExploreExcelKey, "works", WorkSheetKind.Works,
        "魔物探索 · works", "魔物探索工作参数；xp 公式仅作无等级表时兜底"),
      new SheetInfo(
        "monster_explore_work_levels", MonsterExploreExcelKey, "work_levels", WorkSheetKind.WorkLevels,
        "魔物探索 · work_levels", "探索总等级 1–160：每级升到下级所需经验"),
      new SheetInfo(
        "monster_explore_action_levels", MonsterExploreExcelKey, "action_levels", WorkSheetKind.ActionLevels,
        "魔物探索 · action_levels", "各场景动作独立熟练度曲线 1–160"),
      new SheetInfo(
        "monster_explore_actions", MonsterExploreExcelKey, "actions", WorkSheetKind.Actions,
        "魔物探索 · actions", "各场景动作；costItemId / costAmount 为每次消耗"),
      new SheetInfo(
        "monster_explore_loot", MonsterExploreExcelKey, "loot", WorkSheetKind.Loot,
        "魔物探索 · loot", "随机掉落：actionId + itemId + chance"),
    };

    public static SheetInfo Get(string sheetId)
    {
      foreach (var sheet in All)
      {
        if (string.Equals(sheet.Id, sheetId, StringComparison.OrdinalIgnoreCase))
          return sheet;
      }
      throw new ArgumentException("未知表格：" + sheetId, nameof(sheetId));
    }

    public static SheetInfo[] SheetsOf(string excelKey)
    {
      var list = new List<SheetInfo>();
      foreach (var sheet in All)
      {
        if (sheet.ExcelKey == excelKey)
          list.Add(sheet);
      }
      return list.ToArray();
    }

    public static string GetExcelFileName(string excelKey) => excelKey + ".xlsx";

    public static string GetExcelRelativePath(string excelKey) =>
      excelKey switch
      {
        ItemsExcelKey => GameDataPaths.ItemsExcelRelative,
        ScavengeExcelKey => GameDataPaths.ScavengeExcelRelative,
        WoodcuttingExcelKey => GameDataPaths.WoodcuttingExcelRelative,
        MiningExcelKey => GameDataPaths.MiningExcelRelative,
        MonsterExploreExcelKey => GameDataPaths.MonsterExploreExcelRelative,
        _ => throw new ArgumentException("未知 Excel：" + excelKey, nameof(excelKey)),
      };

    public readonly struct WorkbookInfo
    {
      public readonly string ExcelKey;
      public readonly string Title;
      public readonly string ExcelAssetPath;
      public readonly string JsonAssetPath;
      public readonly SheetInfo[] Sheets;

      public WorkbookInfo(string excelKey, string title, string excelAssetPath, string jsonAssetPath, SheetInfo[] sheets)
      {
        ExcelKey = excelKey;
        Title = title;
        ExcelAssetPath = excelAssetPath;
        JsonAssetPath = jsonAssetPath;
        Sheets = sheets;
      }

      public string ExcelFileName => GetExcelFileName(ExcelKey);
    }

    public static readonly WorkbookInfo[] Workbooks =
    {
      new WorkbookInfo(
        ItemsExcelKey, "道具表",
        GameDataPaths.ItemsExcelAssetPath, GameDataPaths.ItemsJsonAssetPath,
        SheetsOf(ItemsExcelKey)),
      new WorkbookInfo(
        ScavengeExcelKey, "拾荒表",
        GameDataPaths.ScavengeExcelAssetPath, GameDataPaths.ScavengeJsonAssetPath,
        SheetsOf(ScavengeExcelKey)),
      new WorkbookInfo(
        WoodcuttingExcelKey, "砍树表",
        GameDataPaths.WoodcuttingExcelAssetPath, GameDataPaths.WoodcuttingJsonAssetPath,
        SheetsOf(WoodcuttingExcelKey)),
      new WorkbookInfo(
        MiningExcelKey, "挖矿表",
        GameDataPaths.MiningExcelAssetPath, GameDataPaths.MiningJsonAssetPath,
        SheetsOf(MiningExcelKey)),
      new WorkbookInfo(
        MonsterExploreExcelKey, "魔物探索表",
        GameDataPaths.MonsterExploreExcelAssetPath, GameDataPaths.MonsterExploreJsonAssetPath,
        SheetsOf(MonsterExploreExcelKey)),
    };
  }
}
#endif
