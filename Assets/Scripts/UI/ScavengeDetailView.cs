using System.Text;
using TMPro;
using UniverIdle.Game;
using UnityEngine;
using UnityEngine.UI;

namespace UniverIdle.UI
{
  /// <summary>拾荒页右侧详情：标题、正文、工作按钮、进度条、掉落预览。</summary>
  public sealed class ScavengeDetailView : MonoBehaviour
  {
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private Button workButton;
    [SerializeField] private TextMeshProUGUI workButtonText;
    [SerializeField] private LootPreviewView lootPreview;
    [SerializeField] private LootToastView lootToast;
    [SerializeField] private LootToastLineView lootLinePrefab;
    [SerializeField] private TextMeshProUGUI lootFloaterPrefab;
    [SerializeField] private GameObject runningBarRoot;
    [SerializeField] private Image progressFill;
    [SerializeField] private TextMeshProUGUI progressLabelText;
    [SerializeField] private TextMeshProUGUI progressTimeText;

    private StandardWorkCenterView _center;
    private bool _wired;
    private static Sprite _whiteSprite;

    private const string LabelStart = "拾荒";
    private const string LabelStop = "停止";

    private void Awake()
    {
      ResolveReferences();
      EnsureLootToast();
      EnsureProgressBarReady();
    }

    private void Update()
    {
      if (_center?.Host?.Session?.Runner == null) return;
      TickProgress(_center.Host.Session.Runner, GameContent.WorkScavengeId);
    }

    public void Wire(StandardWorkCenterView center)
    {
      _center = center;

      if (workButton != null && !_wired)
      {
        workButton.onClick.RemoveAllListeners();
        workButton.onClick.AddListener(OnWorkButtonClicked);
      }

      if (workButtonText == null && workButton != null)
        workButtonText = workButton.GetComponentInChildren<TextMeshProUGUI>(true);

      _wired = true;
      EnsureProgressBarReady();
      ClearProgress();
      RefreshWorkButton();
    }

    public void ShowAction(WorkActionDefinition action, PlayerState player)
    {
      if (action == null) return;
      var work = GameContent.GetWork(action.WorkId);
      if (titleText != null)
        titleText.text = action.DisplayName;
      if (bodyText != null)
        bodyText.text = BuildDetailBody(action, player, work);
      lootPreview?.Bind(action);
      RefreshWorkButton();
    }

    public void ShowStopped(WorkActionDefinition action, PlayerState player)
    {
      if (action == null) return;
      var work = GameContent.GetWork(action.WorkId);
      if (bodyText != null)
        bodyText.text = BuildDetailBody(action, player, work) + "\n\n请补充道具后重新开始。";
      SetProgressLabel("材料不足，已停止");
      ClearProgressFill();
      RefreshWorkButton();
    }

    public void OnManualStop()
    {
      SetProgressLabel("已停止");
      ClearProgressFill();
      if (progressTimeText != null)
        progressTimeText.text = "00:00";
      RefreshWorkButton();
    }

    private void OnWorkButtonClicked()
    {
      if (_center == null) return;
      if (_center.IsRunningThisWork())
        _center.TryStopCurrentAction();
      else
        _center.TryStartSelectedAction();
    }

    public void SetRunning(WorkActionDefinition action)
    {
      if (action == null) return;
      var spot = string.IsNullOrEmpty(action.SpotName) ? action.DisplayName : action.SpotName;
      SetProgressLabel("进行中 · " + spot);
      ClearProgressFill();
      if (runningBarRoot != null)
        runningBarRoot.SetActive(true);
      RefreshWorkButton();
    }

    public void OnActionCompleted(ActionCompleteResult result, PlayerState player)
    {
      if (result?.Action == null) return;
      EnsureLootToast();
      PushLootToasts(result, player);
      lootPreview?.RevealLoot(result);
      RefreshWorkButton();
    }

    private void PushLootToasts(ActionCompleteResult result, PlayerState player)
    {
      if (lootToast == null) return;

      if (result.Loot == null || result.Loot.Count == 0)
      {
        lootToast.PushText("这次什么也没捡到。");
      }
      else
      {
        for (var i = 0; i < result.Loot.Count; i++)
        {
          var drop = result.Loot[i];
          var total = player != null ? player.GetItemCount(drop.ItemId) : drop.Amount;
          lootToast.PushItem(drop.ItemId, drop.Amount, total);
        }
      }

      if (result.WorkLeveledUp)
      {
        var work = GameContent.GetWork(result.Action.WorkId);
        var workName = work != null ? work.DisplayName : "拾荒";
        lootToast.PushText($"{workName}升至 Lv.{result.WorkNewLevel}！");
      }

      if (result.LeveledUp)
      {
        var scene = string.IsNullOrEmpty(result.SceneName) ? "本地区" : result.SceneName;
        lootToast.PushText($"{scene}熟练度升至 Lv.{result.NewLevel}！");
      }
    }

    public void TickProgress(ActionRunner runner, string workId)
    {
      var active = runner != null && runner.CurrentAction != null && runner.CurrentAction.WorkId == workId;
      if (!active)
      {
        ClearProgressFill();
        if (progressTimeText != null)
          progressTimeText.text = "00:00";
        return;
      }

      EnsureProgressBarReady();
      if (runningBarRoot != null && !runningBarRoot.activeSelf)
        runningBarRoot.SetActive(true);

      if (progressFill != null)
        progressFill.fillAmount = runner.Progress;
      if (progressTimeText != null)
        progressTimeText.text = FormatTime(runner.SecondsRemaining);
    }

    public void RefreshWorkButton()
    {
      if (workButton == null || _center == null) return;
      var running = _center.IsRunningThisWork();
      if (workButtonText != null)
        workButtonText.text = running ? LabelStop : LabelStart;
      workButton.interactable = running || _center.CanStartSelectedAction();
    }

    private void ClearProgress()
    {
      SetProgressLabel("等待开始");
      ClearProgressFill();
      if (progressTimeText != null)
        progressTimeText.text = "00:00";
      if (runningBarRoot != null)
        runningBarRoot.SetActive(true);
    }

    private void ClearProgressFill()
    {
      if (progressFill != null)
        progressFill.fillAmount = 0f;
    }

    private void SetProgressLabel(string text)
    {
      if (progressLabelText != null)
        progressLabelText.text = text;
    }

    private void EnsureProgressBarReady()
    {
      if (progressFill == null && runningBarRoot != null)
      {
        var barFill = runningBarRoot.transform.Find("Mid/BarBg/BarFill");
        if (barFill != null)
          progressFill = barFill.GetComponent<Image>();
      }

      if (progressFill == null) return;

      progressFill.type = Image.Type.Filled;
      progressFill.fillMethod = Image.FillMethod.Horizontal;
      progressFill.fillOrigin = (int)Image.OriginHorizontal.Left;
      if (progressFill.sprite == null)
        progressFill.sprite = GetWhiteSprite();
    }

    private static Sprite GetWhiteSprite()
    {
      if (_whiteSprite != null) return _whiteSprite;
      _whiteSprite = Sprite.Create(
        Texture2D.whiteTexture,
        new Rect(0, 0, 4, 4),
        new Vector2(0.5f, 0.5f),
        4f);
      return _whiteSprite;
    }

    private void EnsureLootToast()
    {
      if (lootToast == null)
      {
        var host = transform.Find("获得提示区");
        if (host != null)
          lootToast = host.GetComponent<LootToastView>();
      }

      if (lootToast == null)
        lootToast = CreateLootToastHost();

      lootToast?.BindPrefabs(lootLinePrefab, lootFloaterPrefab);
    }

    private LootToastView CreateLootToastHost()
    {
      var go = new GameObject("获得提示区", typeof(RectTransform), typeof(LootToastView));
      go.transform.SetParent(transform, false);
      go.transform.SetAsLastSibling();

      var rt = (RectTransform)go.transform;
      rt.anchorMin = new Vector2(0f, 0f);
      rt.anchorMax = new Vector2(1f, 0f);
      rt.pivot = new Vector2(0.5f, 0f);
      rt.anchoredPosition = new Vector2(0f, 12f);
      rt.sizeDelta = new Vector2(-24f, 108f);

      var lines = new GameObject("Lines", typeof(RectTransform));
      lines.transform.SetParent(go.transform, false);
      var linesRt = (RectTransform)lines.transform;
      linesRt.anchorMin = Vector2.zero;
      linesRt.anchorMax = Vector2.one;
      linesRt.offsetMin = Vector2.zero;
      linesRt.offsetMax = Vector2.zero;

      var floatLayer = new GameObject("FloatLayer", typeof(RectTransform));
      floatLayer.transform.SetParent(go.transform, false);
      var floatRt = (RectTransform)floatLayer.transform;
      floatRt.anchorMin = Vector2.zero;
      floatRt.anchorMax = Vector2.one;
      floatRt.offsetMin = Vector2.zero;
      floatRt.offsetMax = Vector2.zero;

      return go.GetComponent<LootToastView>();
    }

    private void ResolveReferences()
    {
      if (lootPreview == null)
        lootPreview = GetComponentInChildren<LootPreviewView>(true);

      if (lootToast == null)
      {
        var host = transform.Find("获得提示区");
        if (host != null)
          lootToast = host.GetComponent<LootToastView>();
      }

      if (runningBarRoot == null)
      {
        var bar = transform.Find("RunningBar");
        if (bar != null)
          runningBarRoot = bar.gameObject;
      }

      if (runningBarRoot != null)
      {
        var texts = runningBarRoot.GetComponentsInChildren<TextMeshProUGUI>(true);
        if (progressLabelText == null && texts.Length > 0)
          progressLabelText = texts[0];
        if (progressTimeText == null && texts.Length > 1)
          progressTimeText = texts[texts.Length - 1];
      }

      if (titleText == null || bodyText == null || workButton == null)
      {
        TextMeshProUGUI firstText = null;
        for (var i = 0; i < transform.childCount; i++)
        {
          var child = transform.GetChild(i);
          if (child.name == "Btn_工作")
          {
            if (workButton == null)
              workButton = child.GetComponent<Button>();
            if (workButtonText == null)
              workButtonText = child.GetComponentInChildren<TextMeshProUGUI>(true);
            continue;
          }
          if (child.name == "RunningBar") continue;
          if (child.name != "Text") continue;
          var tmp = child.GetComponent<TextMeshProUGUI>();
          if (tmp == null) continue;
          if (firstText == null)
          {
            firstText = tmp;
            if (titleText == null) titleText = tmp;
          }
          else if (bodyText == null)
          {
            bodyText = tmp;
            break;
          }
        }
      }
    }

    private static string FormatTime(float seconds)
    {
      var total = Mathf.CeilToInt(seconds);
      var m = total / 60;
      var s = total % 60;
      return m > 0 ? $"{m:00}:{s:00}" : $"00:{s:00}";
    }

    private static string BuildDetailBody(WorkActionDefinition action, PlayerState player, WorkDefinition work)
    {
      if (action == null) return string.Empty;

      var sb = new StringBuilder();
      if (!string.IsNullOrWhiteSpace(action.Description))
        sb.Append(action.Description.Trim());
      else
        sb.Append("暂无描述。");

      var workName = work != null ? work.DisplayName : "工作";
      if (player != null && !SceneProgressRules.IsRegionUnlocked(player, action))
      {
        sb.Append("\n\n").Append(SceneProgressRules.FormatUnlockHint(action, workName));
        return sb.ToString();
      }

      if (action.HasCost && player != null)
      {
        var costItem = GameContent.GetItem(action.CostItemId);
        var costName = costItem != null ? costItem.DisplayName : action.CostItemId;
        var owned = player.GetItemCount(action.CostItemId);
        sb.Append($"\n\n每次消耗：{costName} ×{action.CostAmount}（持有 {owned}）");
        if (!SceneProgressRules.CanAffordCost(player, action))
          sb.Append("\n").Append(SceneProgressRules.FormatCostHint(action));
      }

      return sb.ToString();
    }
  }
}
