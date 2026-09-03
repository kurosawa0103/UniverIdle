using TMPro;
using UniverIdle.Game;
using UnityEngine;
using UnityEngine.UI;

namespace UniverIdle.UI
{
  /// <summary>掉落预览单个格：未揭示显示 ?，掉落后显示道具图标。布局在 掉落slot 预制体。</summary>
  public sealed class LootDropSlotView : MonoBehaviour
  {
    [SerializeField] private Image background;
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI unknownMark;

    private static readonly Color UnknownBg = new(0.49f, 0.49f, 0.49f, 1f);

    private void Awake()
    {
      if (background == null) background = GetComponent<Image>();
    }

    public void Bind(string itemId, ItemDefinition item, bool revealed)
    {
      if (revealed && LootRules.IsEmpty(itemId))
        ShowEmpty();
      else if (revealed && item != null)
        ShowRevealed(item);
      else
        ShowUnknown();
    }

    private void ShowUnknown()
    {
      if (background != null) background.color = UnknownBg;
      if (icon != null) icon.gameObject.SetActive(false);
      if (unknownMark != null)
      {
        unknownMark.gameObject.SetActive(true);
        unknownMark.text = "?";
        unknownMark.color = UITheme.Muted;
      }
    }

    private void ShowEmpty()
    {
      if (background != null) background.color = UITheme.PanelLight;
      if (icon != null) icon.gameObject.SetActive(false);
      if (unknownMark != null)
      {
        unknownMark.gameObject.SetActive(true);
        unknownMark.text = "—";
        unknownMark.color = UITheme.Muted;
      }
    }

    private void ShowRevealed(ItemDefinition item)
    {
      if (background != null) background.color = UITheme.PanelLight;
      if (unknownMark != null) unknownMark.gameObject.SetActive(false);
      if (icon == null) return;

      icon.gameObject.SetActive(true);
      var sprite = ItemIconLoader.Get(item);
      if (sprite != null)
      {
        icon.sprite = sprite;
        icon.color = Color.white;
        return;
      }

      icon.sprite = null;
      icon.color = UITheme.Muted;
    }
  }
}
