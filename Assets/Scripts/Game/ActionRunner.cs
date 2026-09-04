using System;
using System.Collections.Generic;

namespace UniverIdle.Game
{
  public sealed class ActionCompleteResult
  {
    public WorkActionDefinition Action { get; set; }
    public IReadOnlyList<LootResult> Loot { get; set; }
    public int XpGained { get; set; }
    public int GoldGained { get; set; }
    public bool ActionMasteryLeveledUp { get; set; }
    public int ActionMasteryNewLevel { get; set; }
    public bool WorkLeveledUp { get; set; }
    public int WorkNewLevel { get; set; }
    public bool BagFull { get; set; }
    public string SceneName { get; set; }
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
      if (action == null || !WorkActionRules.CanPerform(_player, action)) return false;
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
      if (!WorkActionRules.CanAffordCost(_player, CurrentAction)) return false;
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
      var workBefore = _player.GetWork(workId).Level;
      var masteryBefore = _player.GetActionMastery(action.Id).Level;

      var loot = LootRoller.Roll(action.LootTable, _rng);
      var granted = new List<LootResult>();
      var bagFull = false;
      for (var i = 0; i < loot.Count; i++)
      {
        var drop = loot[i];
        if (LootRules.IsEmpty(drop.ItemId)) continue;
        if (_player.TryAddItem(drop.ItemId, drop.Amount))
          granted.Add(drop);
        else
          bagFull = true;
      }

      var goldGained = 0;
      if (action.HasGoldDrop)
      {
        goldGained = LootRoller.RollGold(action.GoldChance, action.GoldMin, action.GoldMax, _rng);
        if (goldGained > 0)
          _player.AddGold(goldGained);
      }

      _player.AddWorkXp(workId, action.XpReward);
      _player.AddActionXp(workId, action.Id, action.XpReward);

      var workAfter = _player.GetWork(workId).Level;
      var masteryAfter = _player.GetActionMastery(action.Id).Level;
      OnActionCompleted?.Invoke(new ActionCompleteResult
      {
        Action = action,
        Loot = granted,
        XpGained = action.XpReward,
        GoldGained = goldGained,
        ActionMasteryLeveledUp = masteryAfter > masteryBefore,
        ActionMasteryNewLevel = masteryAfter,
        WorkLeveledUp = workAfter > workBefore,
        WorkNewLevel = workAfter,
        BagFull = bagFull,
        SceneName = action.SceneName,
      });
    }
  }
}
