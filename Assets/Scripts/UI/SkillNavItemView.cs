using TMPro;
using UniverIdle.Game;
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

    [SerializeField] private string workId;
    [SerializeField] private bool available = true;

    public string WorkId => workId;
    public string LocationName { get; private set; }
    public bool IsAvailable => available && !string.IsNullOrEmpty(workId);

    private Color _iconTint;

    private void Awake() => RestoreWorkIdFromObjectNameIfNeeded();

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

    private void ApplyConfig(string id, string skillName, string locationName, int level, float xpRatio,
      Color iconTint, bool isAvailable)
    {
      workId = id;
      LocationName = locationName;
      _iconTint = iconTint;
      available = isAvailable && !string.IsNullOrEmpty(id);
      if (nameText != null) nameText.text = skillName;
      ApplyLockedVisual(!available);
      if (available)
        UpdateProgress(level, xpRatio);
      else if (levelText != null)
        levelText.text = "敬请期待";
      SetSelected(false);
    }

    private void RestoreWorkIdFromObjectNameIfNeeded()
    {
      if (!string.IsNullOrEmpty(workId)) return;
      if (!TryParseWorkIdFromObjectName(gameObject.name, out var id, out var isAvailable)) return;
      workId = id;
      available = isAvailable;
    }

    private static bool TryParseWorkIdFromObjectName(string objectName, out string id, out bool isAvailable)
    {
      id = null;
      isAvailable = false;
      const string prefix = "Skill_";
      if (!objectName.StartsWith(prefix)) return false;

      switch (objectName.Substring(prefix.Length))
      {
        case "拾荒":
          id = GameContent.WorkScavengeId;
          isAvailable = true;
          return true;
        case "砍树":
        case "砍伐":
          id = GameContent.WorkWoodcuttingId;
          isAvailable = true;
          return true;
        case "挖矿":
          id = GameContent.WorkMiningId;
          isAvailable = true;
          return true;
        case "魔物探索":
          id = GameContent.WorkMonsterExploreId;
          isAvailable = true;
          return true;
        default:
          return true;
      }
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
