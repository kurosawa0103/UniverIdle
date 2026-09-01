#if UNITY_EDITOR
namespace UniverIdle.Editor
{
  public static class GameDataPaths
  {
    public const string ItemsExcelRelative = "Excel/items.xlsx";
    public const string ScavengeExcelRelative = "Excel/scavenge.xlsx";

    public static string ItemsExcelAssetPath => "Assets/" + ItemsExcelRelative;
    public static string ScavengeExcelAssetPath => "Assets/" + ScavengeExcelRelative;
    public static string ItemsJsonAssetPath => "Assets/StreamingAssets/" + UniverIdle.Game.GameDataPaths.ItemsRelativePath;
    public static string ScavengeJsonAssetPath => "Assets/StreamingAssets/" + UniverIdle.Game.GameDataPaths.ScavengeRelativePath;
  }
}
#endif
