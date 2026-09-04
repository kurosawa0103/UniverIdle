using UnityEngine;

namespace UniverIdle.Game
{
  public static class SceneProgressRules
  {
    /// <summary>地区是否已解锁：看整项工作的等级（拾荒等级）。</summary>
    public static bool IsRegionUnlocked(PlayerState player, WorkActionDefinition action)
    {
      if (player == null || action == null) return false;
      return player.GetWork(action.WorkId).Level >= action.RequiredWorkLevel;
    }

    public static bool CanAffordCost(PlayerState player, WorkActionDefinition action)
    {
      if (player == null || action == null || !action.HasCost) return true;
      return player.GetItemCount(action.CostItemId) >= action.CostAmount;
    }

    public static bool CanPerform(PlayerState player, WorkActionDefinition action) =>
      IsRegionUnlocked(player, action) && CanAffordCost(player, action);

    public static string FormatCostHint(WorkActionDefinition action)
    {
      if (action == null || !action.HasCost) return string.Empty;
      var item = GameContent.GetItem(action.CostItemId);
      var name = item != null ? item.DisplayName : action.CostItemId;
      return $"需 {name} ×{action.CostAmount}";
    }

    public static string FormatUnlockHint(WorkActionDefinition action, string workDisplayName = null)
    {
      if (action == null) return "未解锁";
      var workName = string.IsNullOrEmpty(workDisplayName) ? "工作" : workDisplayName;
      return $"{workName}总等级达到{action.RequiredWorkLevel}解锁";
    }

    public static string FormatDurationSeconds(float seconds) => $"{seconds:0.#}s";

    /// <summary>动作卡标题：优先子地点名。</summary>
    public static string FormatSpotTitle(WorkActionDefinition action)
    {
      if (action == null) return "";
      return string.IsNullOrEmpty(action.SpotName) ? action.Id : action.SpotName;
    }

    /// <summary>进度条 / 详情标题：优先展示名，再子地点 / 场景。</summary>
    public static string FormatActionTitle(WorkActionDefinition action)
    {
      if (action == null) return "";
      if (!string.IsNullOrEmpty(action.DisplayName)) return action.DisplayName;
      if (!string.IsNullOrEmpty(action.SpotName)) return action.SpotName;
      if (!string.IsNullOrEmpty(action.SceneName)) return action.SceneName;
      return action.Id;
    }

    public static string FormatRemainingTime(float seconds)
    {
      var total = Mathf.CeilToInt(seconds);
      var m = total / 60;
      var s = total % 60;
      return m > 0 ? $"{m:00}:{s:00}" : $"00:{s:00}";
    }

    public static string FormatYieldHint(WorkActionDefinition action)
    {
      if (action == null) return "—";
      if (action.LootTable == null || action.LootTable.Count == 0)
        return action.HasCost ? FormatCostHint(action) : "—";

      var best = action.LootTable[0];
      for (var i = 1; i < action.LootTable.Count; i++)
      {
        if (action.LootTable[i].Chance > best.Chance)
          best = action.LootTable[i];
      }

      var item = GameContent.GetItem(best.ItemId);
      var name = item != null ? item.DisplayName : best.ItemId;
      if (Mathf.Approximately(best.Chance, 1f) && best.MinAmount == best.MaxAmount)
        return $"+{best.MinAmount} {name}";
      if (Mathf.Approximately(best.Chance, 1f))
        return $"+{best.MinAmount}-{best.MaxAmount} {name}";
      return $"{Mathf.RoundToInt(best.Chance * 100f)}% {name}";
    }
  }
}
