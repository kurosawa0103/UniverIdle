using System;
using System.Collections.Generic;

namespace UniverIdle.Game
{
  public sealed class PlayerState
  {
    private readonly Dictionary<string, long> _inventory = new();
    private readonly Dictionary<string, WorkProgress> _works = new();
    private readonly Dictionary<string, WorkProgress> _sceneProgress = new();

    private long _gold;

    public event Action OnInventoryChanged;
    public event Action OnGoldChanged;
    public event Action<string> OnWorkChanged;
    public event Action<string, string> OnSceneProgressChanged;

    public IReadOnlyDictionary<string, long> Inventory => _inventory;
    public long Gold => _gold;

    public WorkProgress GetWork(string workId)
    {
      if (!_works.TryGetValue(workId, out var progress))
      {
        progress = new WorkProgress();
        _works[workId] = progress;
      }
      return progress;
    }

    public WorkProgress GetSceneProgress(string workId, string sceneId)
    {
      var key = $"{workId}:{sceneId}";
      if (!_sceneProgress.TryGetValue(key, out var progress))
      {
        progress = new WorkProgress();
        _sceneProgress[key] = progress;
      }
      return progress;
    }

    public long GetItemCount(string itemId)
    {
      return _inventory.TryGetValue(itemId, out var count) ? count : 0;
    }

    public void AddItem(string itemId, long amount)
    {
      if (string.IsNullOrEmpty(itemId) || amount <= 0 || LootRules.IsEmpty(itemId)) return;
      _inventory.TryGetValue(itemId, out var current);
      _inventory[itemId] = current + amount;
      OnInventoryChanged?.Invoke();
    }

    public void AddGold(long amount)
    {
      if (amount <= 0) return;
      _gold += amount;
      OnGoldChanged?.Invoke();
    }

    public bool TryConsumeItem(string itemId, long amount)
    {
      if (amount <= 0) return true;
      if (string.IsNullOrEmpty(itemId) || GetItemCount(itemId) < amount) return false;
      var remaining = GetItemCount(itemId) - amount;
      if (remaining <= 0)
        _inventory.Remove(itemId);
      else
        _inventory[itemId] = remaining;
      OnInventoryChanged?.Invoke();
      return true;
    }

    public void AddWorkXp(string workId, int xp)
    {
      if (string.IsNullOrEmpty(workId) || xp <= 0) return;
      var work = GameContent.GetWork(workId);
      if (work == null || !work.GrantWorkXp) return;
      GetWork(workId).AddXp(xp, work, forScene: false);
      OnWorkChanged?.Invoke(workId);
    }

    public void AddSceneXp(string workId, string sceneId, int xp)
    {
      if (string.IsNullOrEmpty(workId) || string.IsNullOrEmpty(sceneId) || xp <= 0) return;
      var work = GameContent.GetWork(workId);
      if (work == null || !work.GrantSceneXp) return;
      GetSceneProgress(workId, sceneId).AddXp(xp, work, forScene: true);
      OnSceneProgressChanged?.Invoke(workId, sceneId);
    }
  }
}
