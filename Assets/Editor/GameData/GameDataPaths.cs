#if UNITY_EDITOR
namespace UniverIdle.Editor
{
  public static class GameDataPaths
  {
    public const string ItemsExcelRelative = "Excel/items.xlsx";
    public const string ScavengeExcelRelative = "Excel/scavenge.xlsx";
    public const string WoodcuttingExcelRelative = "Excel/woodcutting.xlsx";
    public const string MonsterExploreExcelRelative = "Excel/monster_explore.xlsx";

    public static string ItemsExcelAssetPath => "Assets/" + ItemsExcelRelative;
    public static string ScavengeExcelAssetPath => "Assets/" + ScavengeExcelRelative;
    public static string WoodcuttingExcelAssetPath => "Assets/" + WoodcuttingExcelRelative;
    public static string MonsterExploreExcelAssetPath => "Assets/" + MonsterExploreExcelRelative;
    public static string ItemsJsonAssetPath => "Assets/StreamingAssets/" + UniverIdle.Game.GameDataPaths.ItemsRelativePath;
    public static string ScavengeJsonAssetPath => "Assets/StreamingAssets/" + UniverIdle.Game.GameDataPaths.ScavengeRelativePath;
    public static string WoodcuttingJsonAssetPath => "Assets/StreamingAssets/" + UniverIdle.Game.GameDataPaths.WoodcuttingRelativePath;
    public static string MonsterExploreJsonAssetPath => "Assets/StreamingAssets/" + UniverIdle.Game.GameDataPaths.MonsterExploreRelativePath;
  }
}
#endif
