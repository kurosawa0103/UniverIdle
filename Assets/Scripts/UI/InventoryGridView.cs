using System;
using System.Collections.Generic;
using UniverIdle.Game;
using UnityEngine;

namespace UniverIdle.UI
{
  /// <summary>当前页格子：已解锁显示物品/空格，未解锁显示价格或锁。</summary>
  public sealed class InventoryGridView : MonoBehaviour
  {
    [SerializeField] private RectTransform slotContainer;
    [SerializeField] private InventorySlotView slotPrefab;

    private readonly List<InventorySlotView> _slots = new();
    private Action<int> _onSlotClicked;

    public void SetSlotClickHandler(Action<int> onLocalIndexClicked) =>
      _onSlotClicked = onLocalIndexClicked;

    public void Refresh(PlayerState player, int pageIndex)
    {
      if (player == null || slotContainer == null) return;

      var bag = GameContent.Inventory;
      var entries = new List<(ItemDefinition Item, string Id, long Count)>();
      foreach (var kv in player.Inventory)
      {
        if (kv.Value <= 0 || LootRules.IsEmpty(kv.Key)) continue;
        var item = GameContent.GetItem(kv.Key);
        if (item == null) continue;
        entries.Add((item, kv.Key, kv.Value));
      }

      entries.Sort((a, b) =>
      {
        var byName = string.CompareOrdinal(a.Item.DisplayName, b.Item.DisplayName);
        return byName != 0 ? byName : string.CompareOrdinal(a.Id, b.Id);
      });

      EnsureSlots(bag.SlotsPerPage);
      var pageUnlocked = player.IsPageUnlocked(pageIndex);
      var pageStart = pageIndex * bag.SlotsPerPage;

      for (var i = 0; i < bag.SlotsPerPage; i++)
      {
        var slot = _slots[i];
        var global = pageStart + i;
        if (!pageUnlocked)
        {
          var cost = bag.PageUnlockCost(pageIndex);
          var nextPage = pageIndex == player.UnlockedPageCount;
          slot.ShowLocked(nextPage ? $"解锁本页 {cost}金" : "未解锁", nextPage);
          continue;
        }

        if (global < player.UnlockedSlotCount)
        {
          if (global < entries.Count)
            slot.ShowItem(entries[global].Item, entries[global].Count);
          else
            slot.ShowEmpty();
          continue;
        }

        var isNext = global == player.UnlockedSlotCount;
        var slotCost = bag.SlotUnlockCost(global);
        slot.ShowLocked(isNext ? $"解锁 {slotCost}金" : "🔒", isNext);
      }
    }

    private void EnsureSlots(int needed)
    {
      if (slotPrefab == null)
      {
        Debug.LogError("[InventoryGrid] 未拖 slotPrefab（背包slot）。", this);
        return;
      }

      while (_slots.Count < needed)
      {
        var index = _slots.Count;
        var slot = Instantiate(slotPrefab, slotContainer);
        slot.name = $"Slot_{index}";
        slot.SetClickHandler(() => _onSlotClicked?.Invoke(index));
        _slots.Add(slot);
      }

      for (var i = 0; i < _slots.Count; i++)
        _slots[i].SetVisible(i < needed);
    }
  }
}
