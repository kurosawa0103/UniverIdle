using System.Collections.Generic;
using TMPro;
using UniverIdle.Game;
using UnityEngine;
using UnityEngine.UI;

namespace UniverIdle.UI
{
  /// <summary>
  /// 工作根上的纯动作列表（砍树）：不按地图/scene 分组，点卡开始或停止。
  /// </summary>
  public sealed class ActionListWorkCenterView : WorkCenterView
  {
    [SerializeField] private List<ActionCardView> actionCards = new();
    [SerializeField] private GameObject runningBarRoot;
    [SerializeField] private Image progressFill;
    [SerializeField] private TextMeshProUGUI progressLabelText;
    [SerializeField] private TextMeshProUGUI progressTimeText;
    [SerializeField] private WorkActionDetailView detailPanel;

    private MainUIController _host;
    private readonly List<WorkActionDefinition> _actions = new();
    private string _detailActionId;
    private bool _wired;

    private void Awake() => HideProgressBar();

    public override void Wire(MainUIController host)
    {
      if (_wired) return;
      _wired = true;
      _host = host;

      for (var i = 0; i < actionCards.Count; i++)
      {
        var card = actionCards[i];
        if (card == null) continue;
        var index = i;
        var button = card.ClickButton;
        if (button == null) continue;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => OnCardClicked(index));
      }
    }

    public override void OnActivated(MainUIController host) => Refresh(host);

    public override void OnDeactivated() => HideProgressBar();

    public override void Refresh(MainUIController host)
    {
      _host = host;
      BindCards();
      UpdateCardSelection();
      SyncProgressBar();
    }

    public override void OnInventoryChanged(MainUIController host) => Refresh(host);

    public override void OnWorkOrSceneChanged(MainUIController host) => Refresh(host);

    public override void OnActionCompleted(MainUIController host, ActionCompleteResult result)
    {
      if (result?.Action == null || result.Action.WorkId != WorkId) return;
      _host = host;
      BindCards();
      UpdateCardSelection();
      detailPanel?.OnActionCompleted(result, host.Session?.Player);
    }

    public override void OnRunnerActionStopped(MainUIController host, WorkActionDefinition action)
    {
      if (action == null || action.WorkId != WorkId) return;
      _host = host;
      BindCards();
      UpdateCardSelection();
      HideProgressBar();
    }

    public override void TickProgress(MainUIController host)
    {
      var runner = host.Session?.Runner;
      if (!IsRunningThisWork() || runner?.CurrentAction == null)
      {
        HideProgressBar();
        return;
      }

      EnsureProgressBarReady();
      if (runningBarRoot != null && !runningBarRoot.activeSelf)
        runningBarRoot.SetActive(true);

      if (progressLabelText != null)
        progressLabelText.text = "进行中 · " + SceneProgressRules.FormatActionTitle(runner.CurrentAction);
      if (progressFill != null)
        progressFill.fillAmount = runner.Progress;
      if (progressTimeText != null)
        progressTimeText.text = SceneProgressRules.FormatRemainingTime(runner.SecondsRemaining);
    }

    private void OnCardClicked(int index)
    {
      if (_host == null || index < 0 || index >= _actions.Count) return;

      var action = _actions[index];
      if (action == null) return;
      if (!SceneProgressRules.IsRegionUnlocked(_host.Session.Player, action))
        return;

      ShowDetail(action);
      if (!SceneProgressRules.CanPerform(_host.Session.Player, action))
        return;

      var runner = _host.Session.Runner;
      if (runner.IsRunning && runner.CurrentAction?.Id == action.Id)
      {
        runner.Stop();
        BindCards();
        UpdateCardSelection();
        HideProgressBar();
        return;
      }

      if (runner.IsRunning)
        runner.Stop();

      if (!runner.TryStart(action)) return;

      BindCards();
      UpdateCardSelection();
      ShowProgressBar(action);
    }

    private void BindCards()
    {
      if (_host == null) return;

      _actions.Clear();
      var table = GameContent.GetActionsForWork(WorkId);
      for (var i = 0; i < table.Count; i++)
      {
        if (table[i] != null)
          _actions.Add(table[i]);
      }

      var work = GameContent.GetWork(WorkId);
      var player = _host.Session.Player;
      for (var i = 0; i < actionCards.Count; i++)
      {
        var card = actionCards[i];
        if (card == null) continue;
        if (i >= _actions.Count)
        {
          card.gameObject.SetActive(false);
          continue;
        }

        var action = _actions[i];
        var unlocked = SceneProgressRules.IsRegionUnlocked(player, action);
        var metaLeft = SceneProgressRules.FormatDurationSeconds(action.DurationSeconds);
        var metaRight = SceneProgressRules.FormatYieldHint(action);
        var unlockHint = unlocked
          ? null
          : SceneProgressRules.FormatUnlockHint(action, work?.DisplayName);

        var mastery = player.GetActionMastery(action.Id).Level;
        card.gameObject.SetActive(true);
        card.Bind(
          SceneProgressRules.FormatActionTitle(action),
          metaLeft,
          metaRight,
          !unlocked,
          ActionImageLoader.Get(action),
          mastery,
          ActionCardView.ResolveMasteryIcon(mastery),
          unlockHint);
      }

      RefreshDetail();
    }

    private void RefreshDetail()
    {
      if (_host == null) return;
      WorkActionDefinition action = null;
      if (IsRunningThisWork())
        action = _host.Session.Runner.CurrentAction;
      else if (!string.IsNullOrEmpty(_detailActionId))
        action = GameContent.GetAction(_detailActionId);
      if (action == null && _actions.Count > 0)
        action = _actions[0];
      ShowDetail(action);
    }

    private void ShowDetail(WorkActionDefinition action)
    {
      if (action == null || detailPanel == null || _host?.Session?.Player == null) return;
      _detailActionId = action.Id;
      detailPanel.ShowAction(action, _host.Session.Player, revealGuaranteedLoot: true);
    }

    private void UpdateCardSelection()
    {
      var runningId = IsRunningThisWork() ? _host.Session.Runner.CurrentAction.Id : null;
      for (var i = 0; i < actionCards.Count; i++)
      {
        if (actionCards[i] == null) continue;
        actionCards[i].SetSelected(i < _actions.Count && _actions[i].Id == runningId);
      }
    }

    private bool IsRunningThisWork()
    {
      var runner = _host?.Session?.Runner;
      return runner != null && runner.IsRunning && runner.CurrentAction?.WorkId == WorkId;
    }

    private void SyncProgressBar()
    {
      if (IsRunningThisWork())
        ShowProgressBar(_host.Session.Runner.CurrentAction);
      else
        HideProgressBar();
    }

    private void ShowProgressBar(WorkActionDefinition action)
    {
      if (action == null) return;
      EnsureProgressBarReady();
      if (runningBarRoot != null)
        runningBarRoot.SetActive(true);
      if (progressLabelText != null)
        progressLabelText.text = "进行中 · " + SceneProgressRules.FormatActionTitle(action);
      if (progressFill != null)
        progressFill.fillAmount = 0f;
      if (progressTimeText != null)
        progressTimeText.text = SceneProgressRules.FormatRemainingTime(action.DurationSeconds);
    }

    private void HideProgressBar()
    {
      if (progressFill != null)
        progressFill.fillAmount = 0f;
      if (progressTimeText != null)
        progressTimeText.text = "00:00";
      if (runningBarRoot != null)
        runningBarRoot.SetActive(false);
    }

    /// <summary>预制体 fill 常无 sprite；Filled + 白图才能看得见滚动。</summary>
    private void EnsureProgressBarReady()
    {
      if (progressFill == null) return;
      progressFill.type = Image.Type.Filled;
      progressFill.fillMethod = Image.FillMethod.Horizontal;
      progressFill.fillOrigin = (int)Image.OriginHorizontal.Left;
      if (progressFill.sprite == null)
        progressFill.sprite = GetWhiteSprite();
    }

    private static Sprite _whiteSprite;

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
  }
}
