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
    private int _unlockedPageCount;
    private int _unlockedSlotCount;

    public event Action OnInventoryChanged;
    public event Action OnGoldChanged;
    public event Action<string> OnWorkChanged;
    public event Action<string, string> OnSceneProgressChanged;

    public IReadOnlyDictionary<string, long> Inventory => _inventory;
    public long Gold => _gold;
    public int UnlockedPageCount => _unlockedPageCount;
    public int UnlockedSlotCount => _unlockedSlotCount;
    public int SlotCapacity => _unlockedSlotCount;

    public PlayerState() => ApplyBagDefaults();

    public bool IsPageUnlocked(int pageIndex) => pageIndex >= 0 && pageIndex < _unlockedPageCount;

    public int OccupiedSlotCount
    {
      get
      {
        var n = 0;
        foreach (var kv in _inventory)
        {
          if (kv.Value > 0 && !LootRules.IsEmpty(kv.Key))
            n++;
        }
        return n;
      }
    }

    public bool TrySpendGold(long amount)
    {
      if (amount <= 0) return true;
      if (_gold < amount) return false;
      _gold -= amount;
      OnGoldChanged?.Invoke();
      return true;
    }

    public bool TryUnlockNextPage()
    {
      var bag = GameContent.Inventory;
      if (_unlockedPageCount >= bag.PageCount) return false;
      var cost = bag.PageUnlockCost(_unlockedPageCount);
      if (!TrySpendGold(cost)) return false;
      _unlockedPageCount++;
      OnInventoryChanged?.Invoke();
      return true;
    }

    public bool TryUnlockNextSlot()
    {
      var bag = GameContent.Inventory;
      var cap = bag.SlotCapForPages(_unlockedPageCount);
      if (_unlockedSlotCount >= cap) return false;
      var cost = bag.SlotUnlockCost(_unlockedSlotCount);
      if (!TrySpendGold(cost)) return false;
      _unlockedSlotCount++;
      OnInventoryChanged?.Invoke();
      return true;
    }

    public bool TryAddItem(string itemId, long amount)
    {
      if (string.IsNullOrEmpty(itemId) || amount <= 0 || LootRules.IsEmpty(itemId)) return false;
      if (_inventory.TryGetValue(itemId, out var current) && current > 0)
      {
        _inventory[itemId] = current + amount;
        OnInventoryChanged?.Invoke();
        return true;
      }

      if (OccupiedSlotCount >= _unlockedSlotCount) return false;
      _inventory[itemId] = amount;
      OnInventoryChanged?.Invoke();
      return true;
    }

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

    public void ResetToNewPlayer()
    {
      _inventory.Clear();
      _works.Clear();
      _sceneProgress.Clear();
      _gold = 0;
      ApplyBagDefaults();
    }

    public GameSaveFile ToSaveFile()
    {
      var items = new SaveItemRow[_inventory.Count];
      var i = 0;
      foreach (var kv in _inventory)
      {
        items[i] = new SaveItemRow { id = kv.Key, count = kv.Value };
        i++;
      }

      var works = new SaveWorkRow[_works.Count];
      i = 0;
      foreach (var kv in _works)
      {
        works[i] = new SaveWorkRow { id = kv.Key, level = kv.Value.Level, xp = kv.Value.Xp };
        i++;
      }

      var scenes = new SaveSceneRow[_sceneProgress.Count];
      i = 0;
      foreach (var kv in _sceneProgress)
      {
        SplitSceneKey(kv.Key, out var workId, out var sceneId);
        scenes[i] = new SaveSceneRow
        {
          workId = workId,
          sceneId = sceneId,
          level = kv.Value.Level,
          xp = kv.Value.Xp,
        };
        i++;
      }

      return new GameSaveFile
      {
        version = GameSave.CurrentVersion,
        gold = _gold,
        unlockedPageCount = _unlockedPageCount,
        unlockedSlotCount = _unlockedSlotCount,
        items = items,
        works = works,
        scenes = scenes,
      };
    }

    public void LoadFrom(GameSaveFile file)
    {
      ResetToNewPlayer();
      if (file == null) return;

      _gold = file.gold < 0 ? 0 : file.gold;
      ClampBagUnlocks(file.unlockedPageCount, file.unlockedSlotCount);

      if (file.items != null)
      {
        for (var i = 0; i < file.items.Length; i++)
        {
          var row = file.items[i];
          if (row == null || string.IsNullOrEmpty(row.id) || row.count <= 0 || LootRules.IsEmpty(row.id))
            continue;
          _inventory[row.id] = row.count;
        }
      }

      if (file.works != null)
      {
        for (var i = 0; i < file.works.Length; i++)
        {
          var row = file.works[i];
          if (row == null || string.IsNullOrEmpty(row.id)) continue;
          _works[row.id] = new WorkProgress
          {
            Level = row.level < 1 ? 1 : row.level,
            Xp = row.xp < 0 ? 0 : row.xp,
          };
        }
      }

      if (file.scenes != null)
      {
        for (var i = 0; i < file.scenes.Length; i++)
        {
          var row = file.scenes[i];
          if (row == null || string.IsNullOrEmpty(row.workId) || string.IsNullOrEmpty(row.sceneId))
            continue;
          _sceneProgress[$"{row.workId}:{row.sceneId}"] = new WorkProgress
          {
            Level = row.level < 1 ? 1 : row.level,
            Xp = row.xp < 0 ? 0 : row.xp,
          };
        }
      }
    }

    public void NotifyStateReplaced()
    {
      OnGoldChanged?.Invoke();
      OnInventoryChanged?.Invoke();
      OnWorkChanged?.Invoke("");
      OnSceneProgressChanged?.Invoke("", "");
    }

    private void ApplyBagDefaults()
    {
      var bag = GameContent.Inventory;
      _unlockedPageCount = 1;
      _unlockedSlotCount = bag.FreeSlotCount;
    }

    private void ClampBagUnlocks(int pages, int slots)
    {
      var bag = GameContent.Inventory;
      if (pages < 1) pages = 1;
      if (pages > bag.PageCount) pages = bag.PageCount;
      _unlockedPageCount = pages;

      var cap = bag.SlotCapForPages(_unlockedPageCount);
      if (slots < bag.FreeSlotCount) slots = bag.FreeSlotCount;
      if (slots > cap) slots = cap;
      _unlockedSlotCount = slots;
    }

    private static void SplitSceneKey(string key, out string workId, out string sceneId)
    {
      workId = key;
      sceneId = "";
      if (string.IsNullOrEmpty(key)) return;
      var colon = key.IndexOf(':');
      if (colon <= 0 || colon >= key.Length - 1) return;
      workId = key.Substring(0, colon);
      sceneId = key.Substring(colon + 1);
    }
  }
}
