using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UniverIdle.UI
{
  /// <summary>获得提示单行：图标、+N、仓库总数 / 纯文字。</summary>
  public sealed class LootToastLineView : MonoBehaviour
  {
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI gainText;
    [SerializeField] private TextMeshProUGUI totalText;
    [SerializeField] private TextMeshProUGUI messageText;

    public Image Icon => icon;
    public TextMeshProUGUI GainText => gainText;
    public TextMeshProUGUI TotalText => totalText;
    public TextMeshProUGUI MessageText => messageText;
    public GameObject Root => gameObject;

    private void Awake() => ResolveReferences();

    public void ResolveReferences()
    {
      if (icon == null)
      {
        var t = transform.Find("Row/Icon");
        if (t != null) icon = t.GetComponent<Image>();
      }

      if (gainText == null)
      {
        var t = transform.Find("Row/Gain");
        if (t != null) gainText = t.GetComponent<TextMeshProUGUI>();
      }

      if (totalText == null)
      {
        var t = transform.Find("Row/Total");
        if (t != null) totalText = t.GetComponent<TextMeshProUGUI>();
      }

      if (messageText == null)
      {
        var t = transform.Find("Row/Message");
        if (t != null) messageText = t.GetComponent<TextMeshProUGUI>();
      }
    }
  }
}
