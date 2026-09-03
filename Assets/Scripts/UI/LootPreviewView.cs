using System.Collections.Generic;
using UniverIdle.Game;
using UnityEngine;
using UnityEngine.UI;

namespace UniverIdle.UI
{
  /// <summary>按当前动作掉落表生成 slot；未掉落前为 ?，掉落后揭示对应道具。最多显示 5 格（与 Grid 每行 5 列一致）。</summary>
  [RequireComponent(typeof(GridLayoutGroup), typeof(ContentSizeFitter))]
  public sealed class LootPreviewView : MonoBehaviour
  {
    private const int MaxPreviewSlots = 5;

    [SerializeField] private LootDropSlotView slotPrefab;
    [SerializeField] private Transform slotRoot;

    private RectTransform _rect;
    private readonly List<LootDropSlotView> _slots = new();
    private readonly Dictionary<string, HashSet<string>> _revealedByAction = new();
    private string _currentActionId;
    private bool _clearedEditorSlots;

    private void Awake()
    {
      _rect = transform as RectTransform;
      slotRoot = ResolveSlotRoot();
    }

    public void Bind(WorkActionDefinition action)
    {
      var root = ResolveSlotRoot();
      slotRoot = root;
      var table = action?.LootTable;
      var hasLoot = table != null && table.Count > 0;
      gameObject.SetActive(hasLoot);
      if (!hasLoot)
      {
        HideAllSlots();
        _currentActionId = null;
        return;
      }

      ClearEditorSlotsOnce();
      _currentActionId = action.Id;
      if (!_revealedByAction.TryGetValue(_currentActionId, out var revealed))
      {
        revealed = new HashSet<string>();
        _revealedByAction[_currentActionId] = revealed;
      }

      var slotCount = Mathf.Min(table.Count, MaxPreviewSlots);
      EnsureSlotCount(slotCount);
      for (var i = 0; i < slotCount && i < _slots.Count; i++)
      {
        if (_slots[i] == null) continue;
        var entry = table[i];
        var item = GameContent.GetItem(entry.ItemId);
        _slots[i].Bind(entry.ItemId, item, revealed.Contains(entry.ItemId));
      }

      RebuildLayout();
    }

    public void RevealLoot(ActionCompleteResult result)
    {
      if (result?.Action == null || result.Loot == null || result.Loot.Count == 0) return;

      var actionId = result.Action.Id;
      if (!_revealedByAction.TryGetValue(actionId, out var revealed))
      {
        revealed = new HashSet<string>();
        _revealedByAction[actionId] = revealed;
      }

      for (var i = 0; i < result.Loot.Count; i++)
        revealed.Add(result.Loot[i].ItemId);

      if (actionId == _currentActionId)
        Bind(result.Action);
    }

    private Transform ResolveSlotRoot()
    {
      return slotRoot ? slotRoot : transform;
    }

    private void ClearEditorSlotsOnce()
    {
      if (_clearedEditorSlots) return;
      _clearedEditorSlots = true;
      var root = ResolveSlotRoot();
      slotRoot = root;
      for (var i = root.childCount - 1; i >= 0; i--)
        Destroy(root.GetChild(i).gameObject);
      _slots.Clear();
    }

    private void EnsureSlotCount(int count)
    {
      if (!slotPrefab)
      {
        Debug.LogWarning("[UniverIdle] LootPreviewView 未绑定掉落slot预制体。");
        return;
      }

      var root = ResolveSlotRoot();
      while (_slots.Count < count)
      {
        var instance = Instantiate(slotPrefab, root);
        instance.name = $"掉落slot ({_slots.Count + 1})";
        _slots.Add(instance);
      }

      for (var i = 0; i < _slots.Count; i++)
      {
        if (_slots[i] != null)
          _slots[i].gameObject.SetActive(i < count);
      }
    }

    private void HideAllSlots()
    {
      for (var i = 0; i < _slots.Count; i++)
      {
        if (_slots[i] != null)
          _slots[i].gameObject.SetActive(false);
      }
      RebuildLayout();
    }

    private void OnTransformChildrenChanged() => RebuildLayout();

    public void RebuildLayout()
    {
      if (_rect == null) _rect = (RectTransform)transform;
      LayoutRebuilder.MarkLayoutForRebuild(_rect);
    }
  }
}
