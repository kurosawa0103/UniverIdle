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
    [Tooltip("底板；不要往这里塞动作图。")]
    [SerializeField] private Image thumb;
    [Tooltip("Thumb 下的动作图（常见节点名 Image）。剪影与 sprite 都打在这里。")]
    [SerializeField] private Image thumbArt;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI metaLeftText;
    [SerializeField] private TextMeshProUGUI metaRightText;
    [SerializeField] private TextMeshProUGUI unlockText;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image masteryIcon;
    [SerializeField] private TextMeshProUGUI masteryLevelText;

    private void Awake() => ResolveReferences();

    public void Bind(
      string displayTitle,
      string metaLeft,
      string metaRight,
      bool locked,
      Sprite thumbSprite,
      int masteryLevel = 1,
      Sprite masterySprite = null,
      string unlockHint = null)
    {
      ResolveReferences();

      if (locked)
      {
        ApplyLocked(unlockHint, thumbSprite);
        return;
      }

      ApplyUnlocked(displayTitle, metaLeft, metaRight, thumbSprite, masteryLevel, masterySprite);
    }

    private void ApplyLocked(string unlockHint, Sprite thumbSprite)
    {
      SetThumbArt(thumbSprite, locked: true);

      SetActive(titleText, false);
      SetActive(metaLeftText, false);
      SetActive(metaRightText, false);
      SetActive(masteryIcon, false);
      SetActive(masteryLevelText, false);

      if (unlockText != null)
      {
        unlockText.gameObject.SetActive(true);
        if (!string.IsNullOrEmpty(unlockHint))
          unlockText.text = unlockHint;
      }
      else if (titleText != null)
      {
        // 预制体尚未拖 Unlock 时兜底，仍不改 CD
        titleText.gameObject.SetActive(true);
        if (!string.IsNullOrEmpty(unlockHint))
          titleText.text = unlockHint;
      }

      if (canvasGroup != null)
        canvasGroup.interactable = false;

      var button = GetComponent<Button>();
      if (button != null)
        button.interactable = false;
    }

    private void ApplyUnlocked(
      string displayTitle,
      string metaLeft,
      string metaRight,
      Sprite thumbSprite,
      int masteryLevel,
      Sprite masterySprite)
    {
      SetActive(titleText, true);
      SetActive(metaLeftText, true);
      SetActive(metaRightText, true);
      SetActive(unlockText, false);

      if (titleText != null) titleText.text = displayTitle;
      if (metaLeftText != null) metaLeftText.text = metaLeft;
      if (metaRightText != null) metaRightText.text = metaRight;

      SetThumbArt(thumbSprite, locked: false);
      BindMastery(masteryLevel, masterySprite);

      if (canvasGroup != null)
        canvasGroup.interactable = true;

      var button = GetComponent<Button>();
      if (button != null)
        button.interactable = true;
    }

    /// <summary>动作图打在 Thumb/Image（thumbArt）；Thumb 本身只做底板，不改色不换图。</summary>
    private void SetThumbArt(Sprite thumbSprite, bool locked)
    {
      if (thumbArt == null) return;
      thumbArt.gameObject.SetActive(true);
      if (thumbSprite != null)
      {
        thumbArt.sprite = thumbSprite;
        thumbArt.color = locked ? Color.black : Color.white;
        thumbArt.preserveAspect = true;
      }
      else
      {
        thumbArt.sprite = null;
        thumbArt.color = locked ? Color.black : UITheme.PanelLight;
      }
    }

    public void BindMastery(int level, Sprite icon)
    {
      if (masteryLevelText != null)
      {
        masteryLevelText.gameObject.SetActive(true);
        masteryLevelText.text = $"Lv.{Mathf.Max(1, level)}";
      }

      if (masteryIcon == null) return;
      masteryIcon.gameObject.SetActive(true);
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

    private void ResolveReferences()
    {
      if (thumb == null)
      {
        var t = transform.Find("Thumb") ?? transform.Find("ThumbInner");
        if (t != null) thumb = t.GetComponent<Image>();
      }

      if (thumbArt == null)
      {
        Transform art = null;
        if (thumb != null)
          art = thumb.transform.Find("Image")
                ?? thumb.transform.Find("Art")
                ?? thumb.transform.Find("Icon")
                ?? FindFirstChildImage(thumb.transform);
        if (art == null)
          art = transform.Find("Thumb/Image")
                ?? transform.Find("Thumb/Art")
                ?? transform.Find("Thumb/Icon");
        if (art != null)
          thumbArt = art.GetComponent<Image>();
      }

      if (titleText == null)
        titleText = FindTmp("name", "Title", "Name");
      if (metaLeftText == null)
        metaLeftText = FindTmp("CD", "MetaLeft", "Time");
      if (metaRightText == null)
        metaRightText = FindTmp("Yield", "MetaRight", "产量");
      if (unlockText == null)
        unlockText = FindTmp("Unlock", "解锁", "UnlockText", "LockHint");
    }

    private static Transform FindFirstChildImage(Transform parent)
    {
      if (parent == null) return null;
      for (var i = 0; i < parent.childCount; i++)
      {
        var child = parent.GetChild(i);
        if (child.GetComponent<Image>() != null)
          return child;
      }
      return null;
    }

    private TextMeshProUGUI FindTmp(params string[] names)
    {
      for (var i = 0; i < names.Length; i++)
      {
        var t = transform.Find(names[i]);
        if (t == null) continue;
        var tmp = t.GetComponent<TextMeshProUGUI>();
        if (tmp != null) return tmp;
      }
      return null;
    }

    private static void SetActive(Component c, bool active)
    {
      if (c != null)
        c.gameObject.SetActive(active);
    }
  }
}
