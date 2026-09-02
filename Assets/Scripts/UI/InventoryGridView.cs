using System.Collections.Generic;
using System.Linq;
using TMPro;
using UniverIdle.Game;
using UnityEngine;
using UnityEngine.UI;

namespace UniverIdle.UI
{
  /// <summary>背包物品格网格：按玩家背包刷新。</summary>
  public sealed class InventoryGridView : MonoBehaviour
  {
    [SerializeField] private RectTransform slotContainer;
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private TextMeshProUGUI emptyLabel;

    private readonly List<InventorySlotView> _slots = new();

    public void Refresh(PlayerState player)
    {
      if (player == null || slotContainer == null) return;

      var entries = player.Inventory
        .Where(kv => kv.Value > 0)
        .Select(kv => (Item: GameContent.GetItem(kv.Key), Id: kv.Key, Count: kv.Value))
        .Where(e => e.Item != null)
        .OrderBy(e => e.Item.DisplayName)
        .ThenBy(e => e.Id)
        .ToList();

      if (emptyLabel != null)
        emptyLabel.gameObject.SetActive(entries.Count == 0);

      EnsureSlots(entries.Count);
      for (var i = 0; i < _slots.Count; i++)
      {
        if (i < entries.Count)
          _slots[i].Show(entries[i].Item, entries[i].Count);
        else
          _slots[i].Hide();
      }
    }

    private void EnsureSlots(int needed)
    {
      while (_slots.Count < needed)
        _slots.Add(InventorySlotView.Create(slotContainer, font));
    }
  }

  internal sealed class InventorySlotView
  {
    private readonly GameObject _root;
    private readonly Image _icon;
    private readonly TextMeshProUGUI _countText;
    private readonly TextMeshProUGUI _nameText;

    private InventorySlotView(GameObject root, Image icon, TextMeshProUGUI countText, TextMeshProUGUI nameText)
    {
      _root = root;
      _icon = icon;
      _countText = countText;
      _nameText = nameText;
    }

    public static InventorySlotView Create(RectTransform parent, TMP_FontAsset font)
    {
      const float width = 88f;
      const float height = 96f;
      const float iconSize = 40f;

      var rt = new GameObject("Slot", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
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

      var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
      iconGo.transform.SetParent(rect, false);
      var iconRt = iconGo.GetComponent<RectTransform>();
      iconRt.anchorMin = iconRt.anchorMax = new Vector2(0.5f, 1f);
      iconRt.pivot = new Vector2(0.5f, 1f);
      iconRt.anchoredPosition = new Vector2(0, -10);
      iconRt.sizeDelta = new Vector2(iconSize, iconSize);
      var icon = iconGo.GetComponent<Image>();

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

      return new InventorySlotView(rt, icon, count, name);
    }

    public void Show(ItemDefinition item, long count)
    {
      _root.SetActive(true);
      ApplyIcon(item);
      _countText.text = FormatCount(count);
      if (_nameText != null)
        _nameText.text = item != null ? item.DisplayName : "";
    }

    public void Hide() => _root.SetActive(false);

    private void ApplyIcon(ItemDefinition item)
    {
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
