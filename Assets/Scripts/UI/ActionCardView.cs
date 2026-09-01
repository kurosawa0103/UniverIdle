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

        public string DisplayName { get; private set; }
        public string Description { get; private set; }
        public bool IsLocked { get; private set; }

        public void Setup(Image bg, Outline outline, Image thumbImg, TextMeshProUGUI title, TextMeshProUGUI metaL, TextMeshProUGUI metaR,
            CanvasGroup group, string displayTitle, string metaLeft, string metaRight, string description, bool locked,
            Color thumbColor)
        {
            background = bg;
            border = outline;
            thumb = thumbImg;
            titleText = title;
            metaLeftText = metaL;
            metaRightText = metaR;
            canvasGroup = group;

            DisplayName = displayTitle;
            Description = description;
            IsLocked = locked;

            if (titleText != null) titleText.text = displayTitle;
            if (metaLeftText != null) metaLeftText.text = metaLeft;
            if (metaRightText != null) metaRightText.text = metaRight;
            if (thumb != null) thumb.color = thumbColor;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = locked ? 0.45f : 1f;
                canvasGroup.interactable = !locked;
            }
            SetSelected(false);
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
