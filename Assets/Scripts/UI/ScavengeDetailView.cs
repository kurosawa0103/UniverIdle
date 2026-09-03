using System.Text;
using TMPro;
using UniverIdle.Game;
using UnityEngine;
using UnityEngine.UI;

namespace UniverIdle.UI
{
  /// <summary>工作页右侧详情：标题、正文、掉落预览；拾荒另有工作按钮。进度条由对应 Center 驱动。</summary>
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
      HideProgressBar();
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
      HideProgressBar();
      RefreshWorkButton();
    }

    public void ShowAction(WorkActionDefinition action, PlayerState player, bool revealGuaranteedLoot = false)
    {
      if (action == null) return;
      var work = GameContent.GetWork(action.WorkId);
      if (titleText != null)
        titleText.text = action.DisplayName;
      if (bodyText != null)
        bodyText.text = BuildDetailBody(action, player, work);
      lootPreview?.Bind(action, revealGuaranteedLoot);
      RefreshWorkButton();
    }

    public void ShareProgressBar(
      ref GameObject root,
      ref Image fill,
      ref TextMeshProUGUI label,
      ref TextMeshProUGUI time)
    {
      ResolveReferences();
      EnsureProgressBarReady();
      if (root == null) root = runningBarRoot;
      if (fill == null) fill = progressFill;
      if (label == null) label = progressLabelText;
      if (time == null) time = progressTimeText;
    }

    public void ShowStopped(WorkActionDefinition action, PlayerState player)
    {
      if (action == null) return;
      var work = GameContent.GetWork(action.WorkId);
      if (bodyText != null)
        bodyText.text = BuildDetailBody(action, player, work) + "\n\n请补充道具后重新开始。";
      HideProgressBar();
      RefreshWorkButton();
    }

    public void OnManualStop()
    {
      HideProgressBar();
      RefreshWorkButton();
    }

    private void OnWorkButtonClicked()
    {
      if (_center == null) return;
      if (_center.IsShowingRunningAction())
        _center.TryStopCurrentAction();
      else
        _center.TryStartSelectedAction();
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

      var hasLoot = false;
      if (result.Loot != null)
      {
        for (var i = 0; i < result.Loot.Count; i++)
        {
          if (LootRules.IsEmpty(result.Loot[i].ItemId)) continue;
          hasLoot = true;
          break;
        }
      }
      var hasGold = result.GoldGained > 0;
      if (!hasLoot && !hasGold)
      {
        lootToast.PushText(EmptyLootLine(result.Action.WorkId));
      }
      else
      {
        if (hasLoot)
        {
          for (var i = 0; i < result.Loot.Count; i++)
          {
            var drop = result.Loot[i];
            if (LootRules.IsEmpty(drop.ItemId)) continue;
            var total = player != null ? player.GetItemCount(drop.ItemId) : drop.Amount;
            lootToast.PushItem(drop.ItemId, drop.Amount, total);
          }
        }

        if (hasGold)
        {
          var goldTotal = player != null ? player.Gold : result.GoldGained;
          lootToast.PushGold(result.GoldGained, goldTotal);
        }
      }

      if (result.BagFull)
        lootToast.PushText("背包已满，装不下新道具。");

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

    public void HideProgressBar()
    {
      ClearProgressFill();
      if (progressTimeText != null)
        progressTimeText.text = "00:00";
      if (runningBarRoot != null)
        runningBarRoot.SetActive(false);
    }

    public void RefreshWorkButton()
    {
      if (workButton == null || _center == null) return;
      var showingRunning = _center.IsShowingRunningAction();
      if (workButtonText != null)
        workButtonText.text = showingRunning ? LabelStop : GetStartLabel();
      workButton.interactable = showingRunning || _center.CanStartSelectedAction();
    }

    private string GetStartLabel()
    {
      var work = GameContent.GetWork(_center.WorkId);
      return work != null && !string.IsNullOrEmpty(work.DisplayName) ? work.DisplayName : LabelStart;
    }

    private void ClearProgressFill()
    {
      if (progressFill != null)
        progressFill.fillAmount = 0f;
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

    private static string BuildDetailBody(WorkActionDefinition action, PlayerState player, WorkDefinition work)
    {
      if (action == null) return string.Empty;

      var sb = new StringBuilder();
      if (!string.IsNullOrWhiteSpace(action.Description))
        sb.Append(action.Description.Trim());
      else
        sb.Append("暂无描述。");

      var workName = work != null ? work.DisplayName : "拾荒";
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

      AppendGuaranteedLoot(sb, action);

      return sb.ToString();
    }

    private static void AppendGuaranteedLoot(StringBuilder sb, WorkActionDefinition action)
    {
      if (action?.LootTable == null || action.LootTable.Count == 0) return;

      var first = true;
      for (var i = 0; i < action.LootTable.Count; i++)
      {
        var entry = action.LootTable[i];
        if (LootRules.IsEmpty(entry.ItemId) || !Mathf.Approximately(entry.Chance, 1f))
          continue;
        var item = GameContent.GetItem(entry.ItemId);
        var name = item != null ? item.DisplayName : entry.ItemId;
        var amount = entry.MinAmount == entry.MaxAmount
          ? $"×{entry.MinAmount}"
          : $"×{entry.MinAmount}-{entry.MaxAmount}";
        if (first)
        {
          sb.Append("\n\n必定获得：");
          first = false;
        }
        else
          sb.Append("、");
        sb.Append(name).Append(amount);
      }
    }

    private static string EmptyLootLine(string workId)
    {
      return workId == "woodcutting" ? "这次没砍下原木。" : "这次什么也没捡到。";
    }
  }
}
