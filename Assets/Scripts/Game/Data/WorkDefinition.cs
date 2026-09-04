namespace UniverIdle.Game
{
  public sealed class WorkDefinition
  {
    public string Id { get; set; }
    public string DisplayName { get; set; }
    public string LocationName { get; set; }

    /// <summary>总等级升级公式兜底：所需经验 = xpBase + 当前等级 × xpPerLevel。</summary>
    public int XpBase { get; set; } = 40;
    public int XpPerLevel { get; set; } = 20;

    /// <summary>动作熟练度升级公式兜底；为 0 时沿用总等级参数。</summary>
    public int ActionXpBase { get; set; }
    public int ActionXpPerLevel { get; set; }

    public bool GrantWorkXp { get; set; } = true;
    public bool GrantActionXp { get; set; } = true;

    /// <summary>下标=等级；有表时查表；空则走公式。</summary>
    public int[] WorkXpByLevel { get; set; }
    public int[] ActionXpByLevel { get; set; }
    public int MaxWorkLevel { get; set; }
    public int MaxActionLevel { get; set; }

    public bool HasWorkLevelTable => WorkXpByLevel != null && MaxWorkLevel > 0;
    public bool HasActionLevelTable => ActionXpByLevel != null && MaxActionLevel > 0;

    public int ResolveActionXpBase() => ActionXpBase > 0 ? ActionXpBase : XpBase;
    public int ResolveActionXpPerLevel() => ActionXpPerLevel > 0 ? ActionXpPerLevel : XpPerLevel;
  }
}
