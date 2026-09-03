using TMPro;
using UniverIdle.Game;
using UnityEngine;
using UnityEngine.UI;

namespace UniverIdle.UI
{
  public class ActionCardView : MonoBehaviour
  {
    [SerializeField] private Image background;
    [SerializeField] private Outline border;
    [SerializeField] private Image thumb;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI metaLeftText;
    [SerializeField] private TextMeshProUGUI metaRightText;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image masteryIcon;
    [SerializeField] private TextMeshProUGUI masteryLevelText;

    public void Bind(string displayTitle, string metaLeft, string metaRight, bool locked,
      Sprite thumbSprite, int masteryLevel = 1, Sprite masterySprite = null)
    {
      if (titleText != null) titleText.text = displayTitle;
      if (metaLeftText != null) metaLeftText.text = metaLeft;
      if (metaRightText != null) metaRightText.text = metaRight;
      if (thumb != null)
      {
        if (thumbSprite != null)
        {
          thumb.sprite = thumbSprite;
          thumb.color = locked ? Color.black : Color.white;
          thumb.preserveAspect = true;
        }
        else
        {
          thumb.sprite = null;
          thumb.color = locked ? Color.black : UITheme.PanelLight;
        }
      }

      if (canvasGroup != null)
        canvasGroup.interactable = !locked;

      var button = GetComponent<Button>();
      if (button != null)
        button.interactable = !locked;

      BindMastery(masteryLevel, masterySprite);
    }

    public void BindMastery(int level, Sprite icon)
    {
      if (masteryLevelText != null)
        masteryLevelText.text = $"Lv.{Mathf.Max(1, level)}";

      if (masteryIcon == null) return;
      if (icon != null)
      {
        masteryIcon.enabled = true;
        masteryIcon.sprite = icon;
        masteryIcon.color = Color.white;
        masteryIcon.preserveAspect = true;
      }
      else
      {
        masteryIcon.sprite = null;
        masteryIcon.color = UITheme.Muted;
      }
    }

    public static Sprite ResolveMasteryIcon(int masteryLevel) =>
      ItemIconLoader.GetMastery(Mathf.Max(1, masteryLevel));

    public void SetSelected(bool selected)
    {
      if (background != null)
        background.color = selected ? UITheme.CardHover : UITheme.Panel;
      if (border != null)
        border.effectColor = selected ? UITheme.Accent : UITheme.BorderSubtle;
    }
  }
}
