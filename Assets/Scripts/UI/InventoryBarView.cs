using System.Collections.Generic;
using System.Linq;
using TMPro;
using UniverIdle.Game;
using UnityEngine;
using UnityEngine.UI;

namespace UniverIdle.UI
{
  /// <summary>底栏物品格：按背包数量刷新显示。</summary>
  public sealed class InventoryBarView : MonoBehaviour
  {
    [SerializeField] private RectTransform slotContainer;
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private int maxSlots = 10;

    private readonly List<InventorySlotView> _slots = new();

    public void Configure(RectTransform container, TMP_FontAsset fontAsset, int slotCount = 10)
    {
      slotContainer = container;
      font = fontAsset;
      maxSlots = slotCount;
    }

    public void Refresh(PlayerState player)
    {
      if (player == null || slotContainer == null) return;

      EnsureSlots();
      var entries = player.Inventory
        .Where(kv => kv.Value > 0)
        .OrderByDescending(kv => kv.Value)
        .ThenBy(kv => kv.Key)
        .Take(maxSlots)
        .ToList();

      for (var i = 0; i < _slots.Count; i++)
      {
        if (i < entries.Count)
        {
          var item = GameContent.GetItem(entries[i].Key);
          _slots[i].Show(item, entries[i].Value);
        }
        else
        {
          _slots[i].Hide();
        }
      }
    }

    private void EnsureSlots()
    {
      while (_slots.Count < maxSlots)
      {
        var slot = InventorySlotView.Create(slotContainer, font);
        _slots.Add(slot);
      }
    }
  }

  internal sealed class InventorySlotView
  {
    private readonly GameObject _root;
    private readonly Image _icon;
    private readonly TextMeshProUGUI _countText;

    private InventorySlotView(GameObject root, Image icon, TextMeshProUGUI countText)
    {
      _root = root;
      _icon = icon;
      _countText = countText;
    }

    public static InventorySlotView Create(RectTransform parent, TMP_FontAsset font)
    {
      const float size = 52f;
      const float iconSize = 32f;

      var rt = new GameObject("Slot", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
      rt.transform.SetParent(parent, false);
      var rect = rt.GetComponent<RectTransform>();
      var bg = rt.GetComponent<Image>();
      bg.color = UITheme.Panel;

      var le = rt.GetComponent<LayoutElement>();
      le.preferredWidth = size;
      le.preferredHeight = size;

      var outline = rt.AddComponent<Outline>();
      outline.effectColor = UITheme.Border;
      outline.effectDistance = new Vector2(1, -1);

      var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
      iconGo.transform.SetParent(rect, false);
      var iconRt = iconGo.GetComponent<RectTransform>();
      iconRt.anchorMin = iconRt.anchorMax = new Vector2(0.5f, 0.5f);
      iconRt.sizeDelta = new Vector2(iconSize, iconSize);
      var icon = iconGo.GetComponent<Image>();

      var countGo = new GameObject("Count", typeof(RectTransform));
      countGo.transform.SetParent(rect, false);
      var countRt = countGo.GetComponent<RectTransform>();
      countRt.anchorMin = new Vector2(1, 0);
      countRt.anchorMax = new Vector2(1, 0);
      countRt.pivot = new Vector2(1, 0);
      countRt.anchoredPosition = new Vector2(-4, 2);
      countRt.sizeDelta = new Vector2(30, 16);
      var count = countGo.AddComponent<TextMeshProUGUI>();
      count.font = font;
      count.fontSize = 10;
      count.color = UITheme.Cream;
      count.fontStyle = FontStyles.Bold;
      count.alignment = TextAlignmentOptions.BottomRight;

      return new InventorySlotView(rt, icon, count);
    }

    public void Show(ItemDefinition item, long count)
    {
      _root.SetActive(true);
      _icon.color = item != null ? item.DisplayColor : UITheme.Muted;
      _countText.text = FormatCount(count);
    }

    public void Hide()
    {
      _root.SetActive(false);
    }

    private static string FormatCount(long count) =>
      count >= 1000 ? (count / 1000f).ToString("0.#") + "k" : count.ToString();
  }
}
