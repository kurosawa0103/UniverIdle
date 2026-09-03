using System;
using System.Collections.Generic;
using TMPro;
using UniverIdle.Game;
using UnityEngine;
using UnityEngine.UI;

namespace UniverIdle.UI
{
  /// <summary>当前页格子：已解锁显示物品/空格，未解锁显示价格或锁。</summary>
  public sealed class InventoryGridView : MonoBehaviour
  {
    [SerializeField] private RectTransform slotContainer;
    [SerializeField] private TMP_FontAsset font;

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
      while (_slots.Count < needed)
      {
        var index = _slots.Count;
        _slots.Add(InventorySlotView.Create(slotContainer, font, () => _onSlotClicked?.Invoke(index)));
      }

      for (var i = 0; i < _slots.Count; i++)
        _slots[i].SetVisible(i < needed);
    }
  }

  internal sealed class InventorySlotView
  {
    private readonly GameObject _root;
    private readonly Image _background;
    private readonly Image _icon;
    private readonly TextMeshProUGUI _countText;
    private readonly TextMeshProUGUI _nameText;
    private readonly Button _button;

    private InventorySlotView(
      GameObject root,
      Image background,
      Image icon,
      TextMeshProUGUI countText,
      TextMeshProUGUI nameText,
      Button button)
    {
      _root = root;
      _background = background;
      _icon = icon;
      _countText = countText;
      _nameText = nameText;
      _button = button;
    }

    public static InventorySlotView Create(RectTransform parent, TMP_FontAsset font, Action onClick)
    {
      const float width = 88f;
      const float height = 96f;
      const float iconSize = 40f;

      var rt = new GameObject("Slot", typeof(RectTransform), typeof(Image), typeof(LayoutElement), typeof(Button));
      rt.transform.SetParent(parent, false);
      var rect = rt.GetComponent<RectTransform>();
      var bg = rt.GetComponent<Image>();
      bg.color = UITheme.PanelLight;

      var le = rt.GetComponent<LayoutElement>();
      le.preferredWidth = width;
      le.preferredHeight = height;

      var outline = rt.AddComponent<Outline>();
      outline.effectColor = UITheme.Border;
      outline.effectDistance = new Vector2(1, -1);

      var button = rt.GetComponent<Button>();
      button.targetGraphic = bg;
      button.onClick.AddListener(() => onClick?.Invoke());

      var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
      iconGo.transform.SetParent(rect, false);
      var iconRt = iconGo.GetComponent<RectTransform>();
      iconRt.anchorMin = iconRt.anchorMax = new Vector2(0.5f, 1f);
      iconRt.pivot = new Vector2(0.5f, 1f);
      iconRt.anchoredPosition = new Vector2(0, -10);
      iconRt.sizeDelta = new Vector2(iconSize, iconSize);
      var icon = iconGo.GetComponent<Image>();
      icon.raycastTarget = false;

      var countGo = new GameObject("Count", typeof(RectTransform));
      countGo.transform.SetParent(iconRt, false);
      var countRt = countGo.GetComponent<RectTransform>();
      countRt.anchorMin = new Vector2(1, 0);
      countRt.anchorMax = new Vector2(1, 0);
      countRt.pivot = new Vector2(1, 0);
      countRt.anchoredPosition = new Vector2(2, -2);
      countRt.sizeDelta = new Vector2(34, 14);
      var count = countGo.AddComponent<TextMeshProUGUI>();
      count.font = font;
      count.fontSize = 10;
      count.color = UITheme.Cream;
      count.fontStyle = FontStyles.Bold;
      count.alignment = TextAlignmentOptions.BottomRight;
      count.raycastTarget = false;

      var nameGo = new GameObject("Name", typeof(RectTransform));
      nameGo.transform.SetParent(rect, false);
      var nameRt = nameGo.GetComponent<RectTransform>();
      nameRt.anchorMin = new Vector2(0, 0);
      nameRt.anchorMax = new Vector2(1, 0);
      nameRt.pivot = new Vector2(0.5f, 0);
      nameRt.anchoredPosition = new Vector2(0, 6);
      nameRt.sizeDelta = new Vector2(-8, 28);
      var name = nameGo.AddComponent<TextMeshProUGUI>();
      name.font = font;
      name.fontSize = 10;
      name.color = UITheme.Muted;
      name.alignment = TextAlignmentOptions.Top;
      name.enableWordWrapping = true;
      name.overflowMode = TextOverflowModes.Ellipsis;
      name.raycastTarget = false;

      return new InventorySlotView(rt, bg, icon, count, name, button);
    }

    public void SetVisible(bool visible) => _root.SetActive(visible);

    public void ShowItem(ItemDefinition item, long count)
    {
      _root.SetActive(true);
      _button.interactable = false;
      _background.color = UITheme.PanelLight;
      ApplyIcon(item);
      _countText.text = FormatCount(count);
      _nameText.text = item != null ? item.DisplayName : "";
    }

    public void ShowEmpty()
    {
      _root.SetActive(true);
      _button.interactable = false;
      _background.color = UITheme.PanelLight;
      _icon.sprite = null;
      _icon.color = UITheme.BorderSubtle;
      _countText.text = "";
      _nameText.text = "";
    }

    public void ShowLocked(string label, bool canUnlock)
    {
      _root.SetActive(true);
      _button.interactable = canUnlock;
      _background.color = canUnlock ? UITheme.Panel : UITheme.BorderSubtle;
      _icon.sprite = null;
      _icon.color = UITheme.Muted;
      _countText.text = "";
      _nameText.text = label;
      _nameText.color = canUnlock ? UITheme.Gold : UITheme.Muted;
    }

    private void ApplyIcon(ItemDefinition item)
    {
      _nameText.color = UITheme.Muted;
      var sprite = item != null ? ItemIconLoader.Get(item) : null;
      if (sprite != null)
      {
        _icon.sprite = sprite;
        _icon.color = Color.white;
        return;
      }

      _icon.sprite = null;
      _icon.color = UITheme.Muted;
    }

    private static string FormatCount(long count) =>
      count >= 1000 ? (count / 1000f).ToString("0.#") + "k" : count.ToString();
  }
}
