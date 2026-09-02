namespace UniverIdle.Game
{
  public static class GameDataPaths
  {
    /// <summary>Resources.Load 用路径前缀，对应 Resources/ItemIcon/。</summary>
    public const string ItemIconResourcesPrefix = "ItemIcon";

    /// <summary>编辑器下直接读图：Assets/GameResources/ItemIcon/</summary>
    public const string ItemIconEditorFolder = "Assets/GameResources/ItemIcon";

    /// <summary>系统金币图标 Resources 路径（无扩展名）。</summary>
    public const string GoldIconResourcePath = "ItemIcon/item_gold";

    /// <summary>Resources.Load 用路径前缀，对应 ActionImage 目录。</summary>
    public const string ActionImageResourcesPrefix = "ActionImage";

    /// <summary>编辑器下直接读图：Assets/GameResources/ActionImage/</summary>
    public const string ActionImageEditorFolder = "Assets/GameResources/ActionImage";

    public const string ItemsRelativePath = "Game/items.json";
    public const string ScavengeRelativePath = "Game/scavenge.json";
    public const string WoodcuttingRelativePath = "Game/woodcutting.json";
    public const string MiningRelativePath = "Game/mining.json";
    public const string MonsterExploreRelativePath = "Game/monster_explore.json";
  }
}
