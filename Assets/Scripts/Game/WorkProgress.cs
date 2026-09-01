namespace UniverIdle.Game
{
  public sealed class WorkProgress
  {
    public int Level { get; set; } = 1;
    public int Xp { get; set; }

    public int XpToNextLevel(WorkDefinition work, bool forScene = false)
    {
      return forScene
        ? WorkProgression.XpRequiredForSceneLevel(Level, work)
        : WorkProgression.XpRequiredForWorkLevel(Level, work);
    }

    public float XpRatio(WorkDefinition work, bool forScene = false)
    {
      var need = XpToNextLevel(work, forScene);
      return need <= 0 ? 0f : (float)Xp / need;
    }

    public void AddXp(int amount, WorkDefinition work, bool forScene = false)
    {
      if (amount <= 0 || work == null) return;
      Xp += amount;
      while (true)
      {
        var need = XpToNextLevel(work, forScene);
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
      return work.XpBase + level * work.XpPerLevel;
    }

    public static int XpRequiredForSceneLevel(int level, WorkDefinition work)
    {
      if (work == null) return 40 + level * 20;
      return work.ResolveSceneXpBase() + level * work.ResolveSceneXpPerLevel();
    }
  }
}
