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

    public string WorkId { get; private set; }
    public string LocationName { get; private set; }
    public bool IsAvailable { get; private set; } = true;

    private Color _iconTint;

    public void Setup(Image bg, Outline outline, Image accent, Image iconBg, TextMeshProUGUI name, TextMeshProUGUI lv, Image xp,
      string workId, string skillName, string locationName, int level, float xpRatio, Color iconTint, bool available = true)
    {
      background = bg;
      border = outline;
      accentBar = accent;
      iconBackground = iconBg;
      nameText = name;
      levelText = lv;
      xpFill = xp;
      ApplyConfig(workId, skillName, locationName, level, xpRatio, iconTint, available);
    }

    /// <summary>预制体已挂好引用时，仅写入运行时数据。</summary>
    public void Configure(string workId, string skillName, string locationName, int level, float xpRatio, Color iconTint,
      bool available = true)
    {
      ApplyConfig(workId, skillName, locationName, level, xpRatio, iconTint, available);
    }

    private void ApplyConfig(string workId, string skillName, string locationName, int level, float xpRatio,
      Color iconTint, bool available)
    {
      WorkId = workId;
      LocationName = locationName;
      _iconTint = iconTint;
      IsAvailable = available && !string.IsNullOrEmpty(workId);
      if (nameText != null) nameText.text = skillName;
      ApplyLockedVisual(!available);
      if (available)
        UpdateProgress(level, xpRatio);
      else if (levelText != null)
        levelText.text = "敬请期待";
      SetSelected(false);
    }

    private void ApplyLockedVisual(bool locked)
    {
      if (iconBackground != null)
        iconBackground.color = locked ? Dim(_iconTint) : _iconTint;
      if (nameText != null)
        nameText.color = locked ? UITheme.Muted : UITheme.Text;
      if (levelText != null)
        levelText.color = locked ? UITheme.Muted : UITheme.Muted;
      if (xpFill != null)
        xpFill.fillAmount = locked ? 0f : xpFill.fillAmount;
    }

    private static Color Dim(Color c) => new Color(c.r, c.g, c.b, 0.45f);

    public void UpdateProgress(int level, float xpRatio, string sceneLabel = null)
    {
      if (!IsAvailable) return;
      if (levelText != null)
      {
        levelText.text = string.IsNullOrEmpty(sceneLabel)
          ? $"Lv. {level}"
          : $"{sceneLabel} Lv.{level}";
      }
      if (xpFill != null) xpFill.fillAmount = xpRatio;
    }

    public void SetSelected(bool selected)
    {
      if (background != null)
        background.color = selected ? UITheme.PanelLight : UITheme.ClickableClear;
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
