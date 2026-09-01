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
      if (action == null) return "🔒";
      var workName = string.IsNullOrEmpty(workDisplayName) ? "拾荒" : workDisplayName;
      return $"需{workName} Lv.{action.RequiredWorkLevel}";
    }
  }
}
