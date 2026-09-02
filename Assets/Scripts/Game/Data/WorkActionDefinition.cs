using System.Collections.Generic;
using UnityEngine;

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
    public Color ThumbColor { get; set; }
    public string CostItemId { get; set; }
    public int CostAmount { get; set; }
    public bool HasCost => !string.IsNullOrEmpty(CostItemId) && CostAmount > 0;
    /// <summary>独立掷骰获得系统金币的概率（0～1）。</summary>
    public float GoldChance { get; set; }
    public int GoldMin { get; set; }
    public int GoldMax { get; set; }
    public bool HasGoldDrop => GoldChance > 0f && GoldMax > 0;
    public IReadOnlyList<LootEntry> LootTable { get; set; }
  }
}
