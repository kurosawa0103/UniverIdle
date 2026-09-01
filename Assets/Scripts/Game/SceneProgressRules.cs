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

    public static bool CanPerform(PlayerState player, WorkActionDefinition action) =>
      IsRegionUnlocked(player, action);

    public static string FormatUnlockHint(WorkActionDefinition action, string workDisplayName = null)
    {
      if (action == null) return "🔒";
      var workName = string.IsNullOrEmpty(workDisplayName) ? "拾荒" : workDisplayName;
      return $"需{workName} Lv.{action.RequiredWorkLevel}";
    }
  }
}
