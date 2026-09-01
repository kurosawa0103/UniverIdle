using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UniverIdle.UI
{
    public class SkillNavItemView : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private Outline border;
        [SerializeField] private Image accentBar;
        [SerializeField] private Image iconBackground;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private Image xpFill;

        public string LocationName { get; private set; }

        public void Setup(Image bg, Outline outline, Image accent, Image iconBg, TextMeshProUGUI name, TextMeshProUGUI lv, Image xp,
            string skillName, string locationName, int level, float xpRatio, Color iconTint)
        {
            background = bg;
            border = outline;
            accentBar = accent;
            iconBackground = iconBg;
            nameText = name;
            levelText = lv;
            xpFill = xp;

            LocationName = locationName;
            if (nameText != null) nameText.text = skillName;
            if (levelText != null) levelText.text = $"Lv. {level}";
            if (xpFill != null) xpFill.fillAmount = xpRatio;
            if (iconBackground != null) iconBackground.color = iconTint;
            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            if (background != null)
                background.color = selected ? UITheme.PanelLight : UITheme.Transparent;
            if (border != null)
            {
                border.enabled = selected;
                border.effectColor = UITheme.Teal;
            }
            if (accentBar != null)
                accentBar.enabled = selected;
        }
    }
}
