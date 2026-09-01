namespace UniverIdle.Game
{
  public sealed class WorkDefinition
  {
    public string Id { get; set; }
    public string DisplayName { get; set; }
    public string LocationName { get; set; }
    public UnityEngine.Color IconColor { get; set; }

    /// <summary>工作等级升级：所需经验 = xpBase + 当前等级 × xpPerLevel。</summary>
    public int XpBase { get; set; } = 40;
    public int XpPerLevel { get; set; } = 20;

    /// <summary>地区熟练度升级公式；为 0 时沿用工作等级参数。</summary>
    public int SceneXpBase { get; set; }
    public int SceneXpPerLevel { get; set; }

    public bool GrantWorkXp { get; set; } = true;
    public bool GrantSceneXp { get; set; } = true;

    public int ResolveSceneXpBase() => SceneXpBase > 0 ? SceneXpBase : XpBase;
    public int ResolveSceneXpPerLevel() => SceneXpPerLevel > 0 ? SceneXpPerLevel : XpPerLevel;
  }
}
