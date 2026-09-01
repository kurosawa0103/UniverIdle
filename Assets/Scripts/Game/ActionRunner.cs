using System;
using System.Collections.Generic;
using System.Text;

namespace UniverIdle.Game
{
  public sealed class ActionCompleteResult
  {
    public WorkActionDefinition Action { get; set; }
    public IReadOnlyList<LootResult> Loot { get; set; }
    public int XpGained { get; set; }
    public bool LeveledUp { get; set; }
    public int NewLevel { get; set; }
    public bool WorkLeveledUp { get; set; }
    public int WorkNewLevel { get; set; }
    public string SceneName { get; set; }

    public string FormatLootSummary()
    {
      if (Loot == null || Loot.Count == 0)
        return "这次什么也没捡到。";

      var sb = new StringBuilder();
      for (var i = 0; i < Loot.Count; i++)
      {
        if (i > 0) sb.Append("，");
        var item = GameContent.GetItem(Loot[i].ItemId);
        var name = item != null ? item.DisplayName : Loot[i].ItemId;
        sb.Append(name).Append(" ×").Append(Loot[i].Amount);
      }
      return "获得：" + sb;
    }
  }

  public sealed class ActionRunner
  {
    private readonly PlayerState _player;
    private readonly Random _rng = new();

    public WorkActionDefinition CurrentAction { get; private set; }
    public float Progress { get; private set; }
    public float SecondsRemaining { get; private set; }
    public bool IsRunning => CurrentAction != null;

    public event Action<ActionCompleteResult> OnActionCompleted;

    public ActionRunner(PlayerState player)
    {
      _player = player;
    }

    public void Start(WorkActionDefinition action)
    {
      if (action == null) return;
      CurrentAction = action;
      Progress = 0f;
      SecondsRemaining = action.DurationSeconds;
    }

    public void Tick(float deltaTime)
    {
      if (CurrentAction == null || deltaTime <= 0f) return;

      var duration = Math.Max(0.01f, CurrentAction.DurationSeconds);
      Progress = Math.Min(1f, Progress + deltaTime / duration);
      SecondsRemaining = Math.Max(0f, duration * (1f - Progress));

      if (Progress < 1f) return;

      CompleteCurrentAction();
      Progress = 0f;
      SecondsRemaining = CurrentAction.DurationSeconds;
    }

    private void CompleteCurrentAction()
    {
      var action = CurrentAction;
      var workId = action.WorkId;
      var sceneId = action.SceneId;
      var workBefore = _player.GetWork(workId).Level;
      var sceneBefore = _player.GetSceneProgress(workId, sceneId).Level;

      var loot = LootRoller.Roll(action.LootTable, _rng);
      foreach (var drop in loot)
        _player.AddItem(drop.ItemId, drop.Amount);

      _player.AddWorkXp(workId, action.XpReward);
      _player.AddSceneXp(workId, sceneId, action.XpReward);

      var workAfter = _player.GetWork(workId).Level;
      var sceneAfter = _player.GetSceneProgress(workId, sceneId).Level;
      OnActionCompleted?.Invoke(new ActionCompleteResult
      {
        Action = action,
        Loot = loot,
        XpGained = action.XpReward,
        LeveledUp = sceneAfter > sceneBefore,
        NewLevel = sceneAfter,
        WorkLeveledUp = workAfter > workBefore,
        WorkNewLevel = workAfter,
        SceneName = action.SceneName,
      });
    }
  }
}
