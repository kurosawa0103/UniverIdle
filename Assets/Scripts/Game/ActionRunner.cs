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
    public int GoldGained { get; set; }
    public bool LeveledUp { get; set; }
    public int NewLevel { get; set; }
    public bool WorkLeveledUp { get; set; }
    public int WorkNewLevel { get; set; }
    public string SceneName { get; set; }

    public string FormatLootSummary()
    {
      if (Loot == null || Loot.Count == 0)
        return GoldGained <= 0 ? "这次什么也没捡到。" : "获得：金币 ×" + GoldGained;

      var sb = new StringBuilder();
      var wrote = false;
      if (Loot != null)
      {
        for (var i = 0; i < Loot.Count; i++)
        {
          if (LootRules.IsEmpty(Loot[i].ItemId)) continue;
          if (wrote) sb.Append("，");
          var item = GameContent.GetItem(Loot[i].ItemId);
          var name = item != null ? item.DisplayName : Loot[i].ItemId;
          sb.Append(name).Append(" ×").Append(Loot[i].Amount);
          wrote = true;
        }
      }

      if (GoldGained > 0)
      {
        if (wrote) sb.Append("，");
        sb.Append("金币 ×").Append(GoldGained);
        wrote = true;
      }

      return wrote ? "获得：" + sb : "这次什么也没捡到。";
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
    public event Action<WorkActionDefinition> OnActionStopped;

    public ActionRunner(PlayerState player)
    {
      _player = player;
    }

    public bool TryStart(WorkActionDefinition action)
    {
      if (action == null || !SceneProgressRules.CanPerform(_player, action)) return false;
      CurrentAction = action;
      return BeginCycle();
    }

    public void Stop()
    {
      CurrentAction = null;
      Progress = 0f;
      SecondsRemaining = 0f;
    }

    public void Tick(float deltaTime)
    {
      if (CurrentAction == null || deltaTime <= 0f) return;

      var duration = Math.Max(0.01f, CurrentAction.DurationSeconds);
      Progress = Math.Min(1f, Progress + deltaTime / duration);
      SecondsRemaining = Math.Max(0f, duration * (1f - Progress));

      if (Progress < 1f) return;

      CompleteCurrentAction();
      if (CurrentAction == null) return;
      if (!BeginCycle())
        StopDueToCost(CurrentAction);
    }

    private bool BeginCycle()
    {
      if (CurrentAction == null) return false;
      if (!SceneProgressRules.CanAffordCost(_player, CurrentAction)) return false;
      if (CurrentAction.HasCost && !_player.TryConsumeItem(CurrentAction.CostItemId, CurrentAction.CostAmount))
        return false;

      Progress = 0f;
      SecondsRemaining = CurrentAction.DurationSeconds;
      return true;
    }

    private void StopDueToCost(WorkActionDefinition action)
    {
      CurrentAction = null;
      Progress = 0f;
      SecondsRemaining = 0f;
      OnActionStopped?.Invoke(action);
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
      {
        if (LootRules.IsEmpty(drop.ItemId)) continue;
        _player.AddItem(drop.ItemId, drop.Amount);
      }

      var goldGained = 0;
      if (action.HasGoldDrop)
      {
        goldGained = LootRoller.RollGold(action.GoldChance, action.GoldMin, action.GoldMax, _rng);
        if (goldGained > 0)
          _player.AddGold(goldGained);
      }

      _player.AddWorkXp(workId, action.XpReward);
      _player.AddSceneXp(workId, sceneId, action.XpReward);

      var workAfter = _player.GetWork(workId).Level;
      var sceneAfter = _player.GetSceneProgress(workId, sceneId).Level;
      OnActionCompleted?.Invoke(new ActionCompleteResult
      {
        Action = action,
        Loot = loot,
        XpGained = action.XpReward,
        GoldGained = goldGained,
        LeveledUp = sceneAfter > sceneBefore,
        NewLevel = sceneAfter,
        WorkLeveledUp = workAfter > workBefore,
        WorkNewLevel = workAfter,
        SceneName = action.SceneName,
      });
    }
  }
}
