using System.Collections;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UniverIdle.UI
{
  /// <summary>获得提示单行：图标、+N、仓库总数 / 纯文字；宽度随内容收缩。</summary>
  public sealed class LootToastLineView : MonoBehaviour
  {
    [FoldoutGroup("引用", Expanded = true)]
    [LabelText("图标")]
    [SerializeField] private Image icon;

    [FoldoutGroup("引用")]
    [LabelText("获得数量文案")]
    [SerializeField] private TextMeshProUGUI gainText;

    [FoldoutGroup("引用")]
    [LabelText("仓库总数字案")]
    [SerializeField] private TextMeshProUGUI totalText;

    [FoldoutGroup("引用")]
    [LabelText("纯文字提示")]
    [SerializeField] private TextMeshProUGUI messageText;

    [FoldoutGroup("引用")]
    [LabelText("行容器 Row")]
    [SerializeField] private RectTransform row;

    [FoldoutGroup("布局")]
    [LabelText("左右内边距")]
    [SerializeField] private float horizontalPadding = 12f;

    [FoldoutGroup("布局")]
    [LabelText("图标 ↔ 获得数量")]
    [SerializeField] private float iconToGainGap = 8f;

    [FoldoutGroup("布局")]
    [LabelText("获得数量 ↔ 仓库总数")]
    [SerializeField] private float gainToTotalGap = 8f;

    [FoldoutGroup("布局")]
    [LabelText("图标边长")]
    [SerializeField] private float iconSize = 28f;

    [FoldoutGroup("布局")]
    [LabelText("行高")]
    [SerializeField] private float rowHeight = 36f;

    [FoldoutGroup("布局")]
    [LabelText("最小宽度")]
    [SerializeField] private float minWidth = 96f;

    [FoldoutGroup("布局")]
    [LabelText("最大宽度")]
    [SerializeField] private float maxWidth = 420f;

    [FoldoutGroup("动效")]
    [LabelText("+N 放大峰值")]
    [SerializeField] private float gainPunchPeak = 1.32f;

    [FoldoutGroup("动效")]
    [LabelText("总数放大峰值")]
    [SerializeField] private float totalPunchPeak = 1.18f;

    [FoldoutGroup("动效")]
    [LabelText("放大耗时（秒）")]
    [SerializeField] private float punchUpDuration = 0.07f;

    [FoldoutGroup("动效")]
    [LabelText("回落耗时（秒）")]
    [SerializeField] private float punchDownDuration = 0.13f;

    private RectTransform _rootRect;
    private Coroutine _gainPunchCo;
    private Coroutine _totalPunchCo;

    public Image Icon => icon;
    public TextMeshProUGUI GainText => gainText;
    public TextMeshProUGUI TotalText => totalText;
    public TextMeshProUGUI MessageText => messageText;
    public GameObject Root => gameObject;

    private void Awake() => CacheRoot();

    private void CacheRoot() => _rootRect ??= transform as RectTransform;

    /// <summary>根据当前可见内容重算行宽并摆放子节点。</summary>
    public void RefreshLayout()
    {
      CacheRoot();
      if (_rootRect == null || row == null) return;

      var width = messageText != null && messageText.gameObject.activeSelf
        ? LayoutMessageMode()
        : LayoutItemMode();

      var cap = maxWidth > 0f ? maxWidth : width;
      width = Mathf.Clamp(width, minWidth, cap);

      _rootRect.anchorMin = new Vector2(0.5f, _rootRect.anchorMin.y);
      _rootRect.anchorMax = new Vector2(0.5f, _rootRect.anchorMax.y);
      _rootRect.pivot = new Vector2(0.5f, 0.5f);
      _rootRect.sizeDelta = new Vector2(width, rowHeight);

      row.anchorMin = Vector2.zero;
      row.anchorMax = Vector2.one;
      row.offsetMin = Vector2.zero;
      row.offsetMax = Vector2.zero;
    }

    /// <summary>获得时 +N 与总数做一次 scale punch。</summary>
    public void PunchGainNumbers()
    {
      // 须 activeInHierarchy：父级关掉时子节点 activeSelf 仍可能为 true，StartCoroutine 会报错
      if (!isActiveAndEnabled) return;

      if (gainText != null && gainText.gameObject.activeInHierarchy)
        _gainPunchCo = RestartPunch(_gainPunchCo, gainText.rectTransform, gainPunchPeak);
      if (totalText != null && totalText.gameObject.activeInHierarchy)
        _totalPunchCo = RestartPunch(_totalPunchCo, totalText.rectTransform, totalPunchPeak);
    }

    private Coroutine RestartPunch(Coroutine running, RectTransform rt, float peak)
    {
      if (!isActiveAndEnabled) return null;
      if (running != null) StopCoroutine(running);
      return StartCoroutine(PunchScale(rt, peak));
    }

    private IEnumerator PunchScale(RectTransform rt, float peak)
    {
      var pivot = rt.pivot;
      SetPivotKeepingPosition(rt, new Vector2(0.5f, 0.5f));
      rt.localScale = Vector3.one;

      var t = 0f;
      while (t < punchUpDuration)
      {
        t += Time.deltaTime;
        var k = Mathf.Clamp01(t / punchUpDuration);
        var eased = 1f - (1f - k) * (1f - k);
        rt.localScale = Vector3.one * Mathf.Lerp(1f, peak, eased);
        yield return null;
      }

      t = 0f;
      while (t < punchDownDuration)
      {
        t += Time.deltaTime;
        var k = Mathf.Clamp01(t / punchDownDuration);
        var eased = k * k;
        rt.localScale = Vector3.one * Mathf.Lerp(peak, 1f, eased);
        yield return null;
      }

      rt.localScale = Vector3.one;
      SetPivotKeepingPosition(rt, pivot);
    }

    private static void SetPivotKeepingPosition(RectTransform rt, Vector2 pivot)
    {
      var size = rt.rect.size;
      var delta = new Vector2(
        (pivot.x - rt.pivot.x) * size.x,
        (pivot.y - rt.pivot.y) * size.y);
      rt.pivot = pivot;
      rt.anchoredPosition += delta;
    }

    private float LayoutItemMode()
    {
      var x = horizontalPadding;

      if (icon != null && icon.gameObject.activeSelf)
      {
        PlaceIcon(icon.rectTransform, x);
        x += iconSize + iconToGainGap;
      }

      if (gainText != null && gainText.gameObject.activeSelf)
      {
        var w = MeasureText(gainText);
        PlaceTextLeft(gainText.rectTransform, x, w);
        x += w + gainToTotalGap;
      }

      if (totalText != null && totalText.gameObject.activeSelf)
      {
        var w = MeasureText(totalText);
        PlaceTextLeft(totalText.rectTransform, x, w);
        x += w;
      }

      return x + horizontalPadding;
    }

    private float LayoutMessageMode()
    {
      var textMax = maxWidth > 0f ? maxWidth - horizontalPadding * 2f : 0f;
      var textWidth = MeasureText(messageText, textMax);
      var width = horizontalPadding * 2f + textWidth;

      var rt = messageText.rectTransform;
      rt.anchorMin = new Vector2(0f, 0f);
      rt.anchorMax = new Vector2(0f, 1f);
      rt.pivot = new Vector2(0f, 0.5f);
      rt.anchoredPosition = new Vector2(horizontalPadding, 0f);
      rt.sizeDelta = new Vector2(textWidth, 0f);

      return width;
    }

    private void PlaceIcon(RectTransform rt, float left)
    {
      rt.anchorMin = new Vector2(0f, 0.5f);
      rt.anchorMax = new Vector2(0f, 0.5f);
      rt.pivot = new Vector2(0.5f, 0.5f);
      rt.sizeDelta = new Vector2(iconSize, iconSize);
      rt.anchoredPosition = new Vector2(left + iconSize * 0.5f, 0f);
    }

    private static void PlaceTextLeft(RectTransform rt, float left, float width)
    {
      rt.anchorMin = new Vector2(0f, 0.5f);
      rt.anchorMax = new Vector2(0f, 0.5f);
      rt.pivot = new Vector2(0f, 0.5f);
      rt.sizeDelta = new Vector2(width, rt.sizeDelta.y);
      rt.anchoredPosition = new Vector2(left, 0f);
    }

    private static float MeasureText(TextMeshProUGUI tmp, float maxTextWidth = 0f)
    {
      if (tmp == null) return 0f;
      tmp.enableWordWrapping = maxTextWidth > 0f;
      if (maxTextWidth > 0f)
        tmp.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, maxTextWidth);
      tmp.ForceMeshUpdate();
      var w = tmp.preferredWidth;
      if (maxTextWidth > 0f) w = Mathf.Min(w, maxTextWidth);
      tmp.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w);
      return w;
    }
  }
}
