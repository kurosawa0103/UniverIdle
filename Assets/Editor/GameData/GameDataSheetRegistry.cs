#if UNITY_EDITOR
using System;

namespace UniverIdle.Editor
{
  public static class GameDataSheetRegistry
  {
    public const string ItemsExcelKey = "items";
    public const string ScavengeExcelKey = "scavenge";

    public readonly struct SheetInfo
    {
      public readonly string Id;
      public readonly string ExcelKey;
      public readonly string Title;
      public readonly string Description;

      public SheetInfo(string id, string excelKey, string title, string description)
      {
        Id = id;
        ExcelKey = excelKey;
        Title = title;
        Description = description;
      }
    }

    public static readonly SheetInfo[] All =
    {
      new SheetInfo(
        "items", ItemsExcelKey,
        "物品 · items", "全游戏共用道具；loot 表用 itemId 引用此处 id"),
      new SheetInfo(
        "works", ScavengeExcelKey,
        "拾荒 · works", "拾荒工作参数、经验公式"),
      new SheetInfo(
        "actions", ScavengeExcelKey,
        "拾荒 · actions", "各地区动作（不含掉落行）"),
      new SheetInfo(
        "loot", ScavengeExcelKey,
        "拾荒 · loot", "掉落：actionId + itemId；#itemName 列仅方便阅读，不导出"),
    };

    public static string[] AllIds
    {
      get
      {
        var ids = new string[All.Length];
        for (var i = 0; i < All.Length; i++)
          ids[i] = All[i].Id;
        return ids;
      }
    }

    public static SheetInfo Get(string sheetId)
    {
      foreach (var sheet in All)
      {
        if (string.Equals(sheet.Id, sheetId, StringComparison.OrdinalIgnoreCase))
          return sheet;
      }
      throw new ArgumentException("未知表格：" + sheetId, nameof(sheetId));
    }

    public static string GetExcelFileName(string excelKey) => excelKey + ".xlsx";

    public static string GetExcelRelativePath(string excelKey) =>
      excelKey switch
      {
        ItemsExcelKey => GameDataPaths.ItemsExcelRelative,
        ScavengeExcelKey => GameDataPaths.ScavengeExcelRelative,
        _ => throw new ArgumentException("未知 Excel：" + excelKey, nameof(excelKey)),
      };
  }
}
#endif
