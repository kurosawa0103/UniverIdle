using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UniverIdle.UI
{
    public class SkillNavItemView : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private Image accentBar;
        [SerializeField] private Image iconBackground;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private Image xpFill;

        public string LocationName { get; private set; }

        public void Setup(string skillName, string locationName, int level, float xpRatio, Color iconTint)
        {
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
                background.color = selected ? UITheme.PanelLight : new Color(0, 0, 0, 0);
            if (accentBar != null)
                accentBar.enabled = selected;
        }

#if UNITY_EDITOR
        public void Bind(Image bg, Image accent, Image iconBg, TextMeshProUGUI name, TextMeshProUGUI lv, Image xp)
        {
            background = bg;
            accentBar = accent;
            iconBackground = iconBg;
            nameText = name;
            levelText = lv;
            xpFill = xp;
        }
#endif
    }
}
