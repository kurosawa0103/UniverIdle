using System.Collections.Generic;
using System.Text;
using TMPro;
using UniverIdle.Game;
using UnityEngine;
using UnityEngine.UI;

namespace UniverIdle.UI
{
  /// <summary>横幅 + 动作卡 + 进度条的标准工作 Center（砍树/挖矿/魔物探索等）。</summary>
  public sealed class StandardWorkCenterView : WorkCenterView
  {
    [SerializeField] private TextMeshProUGUI locationTitleText;
    [SerializeField] private List<ActionCardView> actionCards = new();
    [SerializeField] private Image progressFill;
    [SerializeField] private TextMeshProUGUI progressLabelText;
    [SerializeField] private TextMeshProUGUI progressTimeText;

    private MainUIController _host;
    private string _activeActionId;
    private readonly List<WorkActionDefinition> _visibleActions = new();
    private bool _wired;

    public void Configure(
      string workId,
      TextMeshProUGUI locationTitle,
      List<ActionCardView> cards,
      Image progress,
      TextMeshProUGUI progressLabel,
      TextMeshProUGUI progressTime)
    {
      BindWork(workId);
      locationTitleText = locationTitle;
      actionCards = cards;
      progressFill = progress;
      progressLabelText = progressLabel;
      progressTimeText = progressTime;
    }

    public override void Wire(MainUIController host)
    {
      if (_wired) return;
      _wired = true;
      _host = host;

      for (var i = 0; i < actionCards.Count; i++)
      {
        var card = actionCards[i];
        if (card == null) continue;
        var btn = card.GetComponent<Button>();
        if (btn == null) continue;
        var index = i;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => OnActionCardClicked(index));
      }
    }

    public override void Refresh(MainUIController host)
    {
      _host = host;
      var work = GameContent.GetWork(WorkId);
      if (locationTitleText != null && work != null)
        locationTitleText.text = work.LocationName;

      RefreshActionCardBindings();
      if (string.IsNullOrEmpty(_activeActionId) ||
          !SceneProgressRules.CanPerform(host.Session.Player, GameContent.GetAction(_activeActionId)))
        _activeActionId = FindFirstUnlockedActionId();

      if (!string.IsNullOrEmpty(_activeActionId))
        SelectAction(_activeActionId, autoStart: true);
      else if (_visibleActions.Count > 0)
        host.ShowActionDetail(_visibleActions[0]);
    }

    public override void TickProgress(MainUIController host)
    {
      var runner = host.Session?.Runner;
      if (runner == null || runner.CurrentAction == null || runner.CurrentAction.WorkId != WorkId)
      {
        if (progressFill != null) progressFill.fillAmount = 0f;
        if (progressTimeText != null) progressTimeText.text = "00:00";
        return;
      }

      if (progressFill != null)
        progressFill.fillAmount = runner.Progress;
      if (progressTimeText != null)
        progressTimeText.text = FormatTime(runner.SecondsRemaining);
    }

    public void OnInventoryChanged(MainUIController host)
    {
      _host = host;
      RefreshActionCardBindings();
      UpdateActionSelectionUi();
      if (!string.IsNullOrEmpty(_activeActionId))
      {
        var action = GameContent.GetAction(_activeActionId);
        if (action != null)
          host.ShowActionDetail(action);
      }
    }

    public void OnWorkOrSceneChanged(MainUIController host)
    {
      _host = host;
      RefreshActionCardBindings();
      UpdateActionSelectionUi();
      if (!string.IsNullOrEmpty(_activeActionId))
      {
        var action = GameContent.GetAction(_activeActionId);
        if (action != null)
          host.ShowActionDetail(action);
      }
    }

    public void OnActionStopped(WorkActionDefinition action, MainUIController host)
    {
      if (action == null || action.WorkId != WorkId) return;
      _activeActionId = null;
      if (progressLabelText != null)
        progressLabelText.text = "材料不足，已停止";
      host.ShowActionStoppedDetail(action);
      RefreshActionCardBindings();
      UpdateActionSelectionUi();
    }

    private void OnActionCardClicked(int index)
    {
      if (_host == null || index >= _visibleActions.Count) return;
      SelectAction(_visibleActions[index].Id, autoStart: true);
    }

    private void SelectAction(string actionId, bool autoStart)
    {
      var action = GameContent.GetAction(actionId);
      if (action == null || action.WorkId != WorkId || _host == null) return;

      _activeActionId = actionId;
      UpdateActionSelectionUi();
      _host.ShowActionDetail(action);
      UpdateLocationBanner(action);

      if (autoStart && SceneProgressRules.CanPerform(_host.Session.Player, action) &&
          _host.Session.Runner.TryStart(action) && progressLabelText != null)
        progressLabelText.text = "进行中 · " + action.DisplayName;
    }

    private void RefreshActionCardBindings()
    {
      if (_host == null) return;

      _visibleActions.Clear();
      foreach (var action in GameContent.GetActionsForWork(WorkId))
        _visibleActions.Add(action);

      var work = GameContent.GetWork(WorkId);
      var player = _host.Session.Player;
      for (var i = 0; i < actionCards.Count; i++)
      {
        if (i >= _visibleActions.Count)
        {
          actionCards[i].gameObject.SetActive(false);
          continue;
        }

        var action = _visibleActions[i];
        var unlocked = SceneProgressRules.IsRegionUnlocked(player, action);
        var canPerform = SceneProgressRules.CanPerform(player, action);

        string metaLeft;
        string metaRight;
        if (!unlocked)
        {
          metaLeft = SceneProgressRules.FormatUnlockHint(action, work?.DisplayName);
          metaRight = "";
        }
        else
        {
          metaLeft = FormatDuration(action.DurationSeconds);
          metaRight = FormatYieldHint(action);
        }

        actionCards[i].gameObject.SetActive(true);
        actionCards[i].Bind(
          action.DisplayName,
          metaLeft,
          metaRight,
          WorkActionUiFormatter.BuildDescription(action, player, work),
          !canPerform,
          action.ThumbColor);
      }
    }

    private string FindFirstUnlockedActionId()
    {
      if (_host == null) return null;
      foreach (var action in _visibleActions)
      {
        if (SceneProgressRules.CanPerform(_host.Session.Player, action))
          return action.Id;
      }
      return _visibleActions.Count > 0 ? _visibleActions[0].Id : null;
    }

    private void UpdateActionSelectionUi()
    {
      for (var i = 0; i < actionCards.Count; i++)
        actionCards[i].SetSelected(i < _visibleActions.Count && _visibleActions[i].Id == _activeActionId);
    }

    private void UpdateLocationBanner(WorkActionDefinition action)
    {
      if (locationTitleText == null || action == null) return;
      locationTitleText.text = string.IsNullOrEmpty(action.SceneName) ? action.DisplayName : action.SceneName;
    }

    private static string FormatDuration(float seconds) => $"{seconds:0.#}s";

    private static string FormatYieldHint(WorkActionDefinition action)
    {
      if (action.LootTable == null || action.LootTable.Count == 0)
        return action.HasCost ? SceneProgressRules.FormatCostHint(action) : "—";

      var best = action.LootTable[0];
      for (var i = 1; i < action.LootTable.Count; i++)
      {
        if (action.LootTable[i].Chance > best.Chance)
          best = action.LootTable[i];
      }

      var item = GameContent.GetItem(best.ItemId);
      var name = item != null ? item.DisplayName : best.ItemId;
      if (Mathf.Approximately(best.Chance, 1f) && best.MinAmount == best.MaxAmount)
        return $"+{best.MinAmount} {name}";
      if (Mathf.Approximately(best.Chance, 1f))
        return $"+{best.MinAmount}-{best.MaxAmount} {name}";
      return $"{Mathf.RoundToInt(best.Chance * 100f)}% {name}";
    }

    private static string FormatTime(float seconds)
    {
      var total = Mathf.CeilToInt(seconds);
      var m = total / 60;
      var s = total % 60;
      return m > 0 ? $"{m:00}:{s:00}" : $"00:{s:00}";
    }
  }

  internal static class WorkActionUiFormatter
  {
    public static string BuildDescription(WorkActionDefinition action, PlayerState player, WorkDefinition work)
    {
      var sb = new StringBuilder();
      sb.Append(action.Description);
      var workName = work != null ? work.DisplayName : "拾荒";
      var workLevel = player.GetWork(action.WorkId).Level;
      var sceneLevel = player.GetSceneProgress(action.WorkId, action.SceneId).Level;
      sb.Append($"\n\n{workName}等级：Lv.{workLevel}（升级需 {WorkProgression.XpRequiredForWorkLevel(workLevel, work)} 经验）");
      sb.Append($"\n{action.SceneName}熟练度：Lv.{sceneLevel}（升级需 {WorkProgression.XpRequiredForSceneLevel(sceneLevel, work)} 经验）");
      sb.Append($"\n解锁条件：{workName} Lv.{action.RequiredWorkLevel}");
      if (action.HasCost)
      {
        var costItem = GameContent.GetItem(action.CostItemId);
        var costName = costItem != null ? costItem.DisplayName : action.CostItemId;
        var owned = player.GetItemCount(action.CostItemId);
        sb.Append($"\n每次消耗：{costName} ×{action.CostAmount}（持有 {owned}）");
      }
      sb.Append("\n\n可能掉落：");
      if (action.LootTable == null || action.LootTable.Count == 0)
      {
        sb.Append("无");
        return sb.ToString();
      }

      for (var i = 0; i < action.LootTable.Count; i++)
      {
        if (i > 0) sb.Append("、");
        var entry = action.LootTable[i];
        var item = GameContent.GetItem(entry.ItemId);
        var name = item != null ? item.DisplayName : entry.ItemId;
        sb.Append(name).Append(" ").Append(Mathf.RoundToInt(entry.Chance * 100f)).Append("%");
      }
      return sb.ToString();
    }
  }
}
