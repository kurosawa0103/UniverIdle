namespace UniverIdle.Game
{
  public static class GameDataPaths
  {
    /// <summary>Resources.Load 用路径前缀，对应 Resources/ItemIcon/。</summary>
    public const string ItemIconResourcesPrefix = "ItemIcon";

    /// <summary>编辑器下直接读图：Assets/Resources/ItemIcon/</summary>
    public const string ItemIconEditorFolder = "Assets/Resources/ItemIcon";

    /// <summary>系统金币图标 Resources 路径（无扩展名）。</summary>
    public const string GoldIconResourcePath = "ItemIcon/item_gold";

    /// <summary>经验图标 Resources 路径（无扩展名）。</summary>
    public const string XpIconResourcePath = "ItemIcon/ui_xp";

    /// <summary>熟练度图标分档（铜 / 银 / 金），Resources 路径无扩展名。</summary>
    public const string MasteryIconTier1Path = "ItemIcon/ui_mastery";
    public const string MasteryIconTier2Path = "ItemIcon/ui_mastery_2";
    public const string MasteryIconTier3Path = "ItemIcon/ui_mastery_3";

    /// <summary>动作熟练度 ≥ 此级用第二档图标。</summary>
    public const int MasteryIconTier2MinLevel = 31;

    /// <summary>动作熟练度 ≥ 此级用第三档图标。</summary>
    public const int MasteryIconTier3MinLevel = 71;

    /// <summary>Resources.Load 用路径前缀，对应 ActionImage 目录。</summary>
    public const string ActionImageResourcesPrefix = "ActionImage";

    /// <summary>编辑器下直接读图：Assets/Resources/ActionImage/</summary>
    public const string ActionImageEditorFolder = "Assets/Resources/ActionImage";

    public const string ItemsRelativePath = "Game/items.json";
    public const string ScavengeRelativePath = "Game/scavenge.json";
    public const string WoodcuttingRelativePath = "Game/woodcutting.json";
    public const string MiningRelativePath = "Game/mining.json";
    public const string MonsterExploreRelativePath = "Game/monster_explore.json";
    public const string InventoryRelativePath = "Game/inventory.json";
  }
}
