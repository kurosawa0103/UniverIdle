using System;
using TMPro;
using UniverIdle.Game;
using UnityEngine;
using UnityEngine.UI;

namespace UniverIdle.UI
{
  /// <summary>背包单格；预制体 <c>背包slot</c>，由 <see cref="InventoryGridView"/> Instantiate。</summary>
  public sealed class InventorySlotView : MonoBehaviour
  {
    [SerializeField] private Image background;
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI countText;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Button button;

    private void Awake()
    {
      if (background == null) background = GetComponent<Image>();
      if (button == null) button = GetComponent<Button>();
    }

    public void SetClickHandler(Action onClick)
    {
      if (button == null) button = GetComponent<Button>();
      if (button == null) return;
      button.onClick.RemoveAllListeners();
      if (onClick != null)
        button.onClick.AddListener(() => onClick());
    }

    public void SetVisible(bool visible) => gameObject.SetActive(visible);

    public void ShowItem(ItemDefinition item, long count)
    {
      gameObject.SetActive(true);
      if (button != null) button.interactable = false;
      if (background != null) background.color = UITheme.PanelLight;
      ApplyIcon(item);
      if (countText != null) countText.text = FormatCount(count);
      if (nameText != null)
      {
        nameText.text = item != null ? item.DisplayName : "";
        nameText.color = UITheme.Muted;
      }
    }

    public void ShowEmpty()
    {
      gameObject.SetActive(true);
      if (button != null) button.interactable = false;
      if (background != null) background.color = UITheme.PanelLight;
      if (icon != null)
      {
        icon.sprite = null;
        icon.color = UITheme.BorderSubtle;
      }
      if (countText != null) countText.text = "";
      if (nameText != null)
      {
        nameText.text = "";
        nameText.color = UITheme.Muted;
      }
    }

    public void ShowLocked(string label, bool canUnlock)
    {
      gameObject.SetActive(true);
      if (button != null) button.interactable = canUnlock;
      if (background != null) background.color = canUnlock ? UITheme.Panel : UITheme.BorderSubtle;
      if (icon != null)
      {
        icon.sprite = null;
        icon.color = UITheme.Muted;
      }
      if (countText != null) countText.text = "";
      if (nameText != null)
      {
        nameText.text = label;
        nameText.color = canUnlock ? UITheme.Gold : UITheme.Muted;
      }
    }

    private void ApplyIcon(ItemDefinition item)
    {
      if (icon == null) return;
      var sprite = item != null ? ItemIconLoader.Get(item) : null;
      if (sprite != null)
      {
        icon.sprite = sprite;
        icon.color = Color.white;
        return;
      }

      icon.sprite = null;
      icon.color = UITheme.Muted;
    }

    private static string FormatCount(long count) =>
      count >= 1000 ? (count / 1000f).ToString("0.#") + "k" : count.ToString();
  }
}
