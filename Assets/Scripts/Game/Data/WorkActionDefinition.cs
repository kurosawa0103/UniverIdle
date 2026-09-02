using System;
using System.Collections.Generic;

namespace UniverIdle.Game
{
  public sealed class WorkActionDefinition
  {
    public string Id { get; set; }
    public string WorkId { get; set; }
    public string SceneId { get; set; }
    /// <summary>横幅/地点用短名，如「村口」——与 ActionCards 同属一个场景。</summary>
    public string SceneName { get; set; }
    /// <summary>子地点名，如「老王家」；动作卡标题优先使用。</summary>
    public string SpotName { get; set; }
    /// <summary>完整动作名，如「村口 · 老王家」；详情与日志用。</summary>
    public string DisplayName { get; set; }
    public float DurationSeconds { get; set; }
    public int XpReward { get; set; }
    /// <summary>解锁本地区所需的整项工作等级（如拾荒 Lv.2）。</summary>
    public int RequiredWorkLevel { get; set; }
    public string Description { get; set; }
    /// <summary>表内 thumbImage 列；空则按动作 id 在 ActionImage 目录查找。</summary>
    public string ThumbImage { get; set; }
    /// <summary>Resources 加载路径（无扩展名）；null 表示不尝试贴图。</summary>
    public string ThumbImageResourcePath =>
      WorkActionDefinition.ResolveThumbImageResourcePath(ThumbImage, Id);
    public string CostItemId { get; set; }
    public int CostAmount { get; set; }
    public bool HasCost => !string.IsNullOrEmpty(CostItemId) && CostAmount > 0;
    /// <summary>独立掷骰获得系统金币的概率（0～1）。</summary>
    public float GoldChance { get; set; }
    public int GoldMin { get; set; }
    public int GoldMax { get; set; }
    public bool HasGoldDrop => GoldChance > 0f && GoldMax > 0;
    public IReadOnlyList<LootEntry> LootTable { get; set; }

    internal static string ResolveThumbImageResourcePath(string thumbImage, string actionId)
    {
      if (!string.IsNullOrWhiteSpace(thumbImage))
      {
        var value = thumbImage.Trim();
        if (value.StartsWith("#", StringComparison.Ordinal))
          return null;
        if (value == "-" || value.Equals("none", StringComparison.OrdinalIgnoreCase))
          return null;
        return value.Contains("/")
          ? value
          : $"{GameDataPaths.ActionImageResourcesPrefix}/{value}";
      }

      if (string.IsNullOrEmpty(actionId)) return null;
      return $"{GameDataPaths.ActionImageResourcesPrefix}/{actionId}";
    }
  }
}
