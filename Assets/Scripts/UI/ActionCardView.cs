using TMPro;
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

        public void Bind(string displayTitle, string metaLeft, string metaRight, bool locked,
            Sprite thumbSprite)
        {
            if (titleText != null) titleText.text = displayTitle;
            if (metaLeftText != null) metaLeftText.text = metaLeft;
            if (metaRightText != null) metaRightText.text = metaRight;
            if (thumb != null)
            {
                if (thumbSprite != null)
                {
                    thumb.sprite = thumbSprite;
                    thumb.color = Color.white;
                    thumb.preserveAspect = true;
                }
                else
                {
                    thumb.sprite = null;
                    thumb.color = UITheme.PanelLight;
                }
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = locked ? 0.45f : 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
        }

        public void SetSelected(bool selected)
        {
            if (background != null)
                background.color = selected ? UITheme.CardHover : UITheme.Panel;
            if (border != null)
                border.effectColor = selected ? UITheme.Accent : UITheme.BorderSubtle;
        }
    }
}
