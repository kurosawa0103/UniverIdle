namespace UniverIdle.Game
{
  public sealed class WorkProgress
  {
    public int Level { get; set; } = 1;
    public int Xp { get; set; }

    public int XpToNextLevel(WorkDefinition work, bool forActionMastery = false)
    {
      return forActionMastery
        ? WorkProgression.XpRequiredForActionLevel(Level, work)
        : WorkProgression.XpRequiredForWorkLevel(Level, work);
    }

    public float XpRatio(WorkDefinition work, bool forActionMastery = false)
    {
      var need = XpToNextLevel(work, forActionMastery);
      return need <= 0 ? 1f : (float)Xp / need;
    }

    public void AddXp(int amount, WorkDefinition work, bool forActionMastery = false)
    {
      if (amount <= 0 || work == null) return;
      if (XpToNextLevel(work, forActionMastery) <= 0) return;
      Xp += amount;
      while (true)
      {
        var need = XpToNextLevel(work, forActionMastery);
        if (need <= 0 || Xp < need) break;
        Xp -= need;
        Level++;
      }
    }
  }

  public static class WorkProgression
  {
    public static int XpRequiredForWorkLevel(int level, WorkDefinition work)
    {
      if (work == null) return 40 + level * 20;
      if (work.HasWorkLevelTable)
        return LookupTableXp(level, work.WorkXpByLevel, work.MaxWorkLevel);
      return work.XpBase + level * work.XpPerLevel;
    }

    public static int XpRequiredForActionLevel(int level, WorkDefinition work)
    {
      if (work == null) return 40 + level * 20;
      if (work.HasActionLevelTable)
        return LookupTableXp(level, work.ActionXpByLevel, work.MaxActionLevel);
      return work.ResolveActionXpBase() + level * work.ResolveActionXpPerLevel();
    }

    private static int LookupTableXp(int level, int[] table, int maxLevel)
    {
      if (level >= maxLevel) return 0;
      if (level < 1) level = 1;
      if (table == null || level >= table.Length) return 0;
      return table[level];
    }

    public static int BuildXpTable(LevelXpRow[] rows, out int[] table)
    {
      table = null;
      if (rows == null || rows.Length == 0) return 0;

      var maxLevel = 0;
      for (var i = 0; i < rows.Length; i++)
      {
        var row = rows[i];
        if (row == null || row.level < 1) continue;
        if (row.level > maxLevel) maxLevel = row.level;
      }
      if (maxLevel <= 0) return 0;

      table = new int[maxLevel + 1];
      for (var i = 0; i < rows.Length; i++)
      {
        var row = rows[i];
        if (row == null || row.level < 1 || row.level > maxLevel) continue;
        table[row.level] = row.xp < 0 ? 0 : row.xp;
      }
      return maxLevel;
    }
  }
}
