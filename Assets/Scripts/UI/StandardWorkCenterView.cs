using System.Collections.Generic;
using TMPro;
using UniverIdle.Game;
using UnityEngine;
using UnityEngine.UI;

namespace UniverIdle.UI
{
  /// <summary>
  /// 横幅 + 动作卡的工作 Center。
  /// 拾荒：挂地图节点（sceneId 如 gate），工作根另挂 <see cref="ScavengeHubView"/> 注入 <see cref="ScavengeDetailView"/>。
  /// 挖矿/魔物：可挂工作根（sceneId 空）。砍树用 <see cref="ActionListWorkCenterView"/>。
  /// </summary>
  public sealed class StandardWorkCenterView : WorkCenterView
  {
    [SerializeField] private string sceneId;
    [SerializeField] private TextMeshProUGUI locationTitleText;
    [SerializeField] private List<ActionCardView> actionCards = new();
    [SerializeField] private GameObject runningBarRoot;
    [SerializeField] private Image progressFill;
    [SerializeField] private TextMeshProUGUI progressLabelText;
    [SerializeField] private TextMeshProUGUI progressTimeText;
    [SerializeField] private ScavengeDetailView detailPanel;

    private MainUIController _host;
    private string _activeActionId;
    private string _activeSceneId;
    private readonly List<WorkActionDefinition> _visibleActions = new();
    private bool _wired;

    public MainUIController Host => _host;

    public string BoundSceneId => sceneId;

    private bool BoundToMap => !string.IsNullOrEmpty(sceneId);

    private IReadOnlyList<WorkSceneGroup> SceneGroups => GameContent.GetSceneGroupsForWork(WorkId);

    private void Awake() => HideCenterProgressBar();

    public override void OnDeactivated() => HideCenterProgressBar();

    /// <summary>仅由 <see cref="ScavengeHubView"/> 注入拾荒详情，勿给其它工作复用。</summary>
    public void BindScavengeDetail(ScavengeDetailView detail)
    {
      if (detail != null)
        detailPanel = detail;
    }

    public override void OnActivated(MainUIController host)
    {
      _host = host;
      if (BoundToMap)
        _activeSceneId = sceneId;
      else
        _activeSceneId = null;
      _activeActionId = null;
      detailPanel?.Wire(this);
      SyncProgressBarVisibility(host);
    }

    public override void Wire(MainUIController host)
    {
      if (_wired) return;
      _wired = true;
      _host = host;
      detailPanel?.Wire(this);

      for (var i = 0; i < actionCards.Count; i++)
      {
        var card = actionCards[i];
        if (card == null) continue;
        var index = i;
        var button = card.ClickButton;
        if (button == null) continue;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => OnActionSelected(index));
      }
    }

    public override void Refresh(MainUIController host)
    {
      _host = host;
      EnsureActiveScene();
      UpdateLocationBannerForScene();

      RefreshActionCardBindings();
      if (string.IsNullOrEmpty(_activeActionId) ||
          !WorkActionRules.CanPerform(host.Session.Player, GameContent.GetAction(_activeActionId)))
        _activeActionId = FindFirstUnlockedActionId();

      if (!string.IsNullOrEmpty(_activeActionId))
        SelectAction(_activeActionId);
      else
      {
        UpdateActionSelectionUi();
        detailPanel?.RefreshWorkButton();
      }

      SyncProgressBarVisibility(host);
    }

    public override void OnActionCompleted(MainUIController host, ActionCompleteResult result)
    {
      if (result?.Action == null || result.Action.WorkId != WorkId) return;
      _host = host;
      detailPanel?.OnActionCompleted(result, host.Session?.Player);
    }

    public override void OnRunnerActionStopped(MainUIController host, WorkActionDefinition action)
    {
      if (action == null || action.WorkId != WorkId) return;
      _host = host;
      _activeActionId = null;
      HideCenterProgressBar();
      if (detailPanel != null)
        detailPanel.ShowStopped(action, host.Session?.Player);
      RefreshActionCardBindings();
      UpdateActionSelectionUi();
      detailPanel?.RefreshWorkButton();
    }

    public override void TickProgress(MainUIController host)
    {
      var runner = host.Session?.Runner;
      // 切到其它 action 时隐藏条，后台仍跑；回到正在执行的 action 再显示并滚动
      if (!IsShowingRunningAction() || runner?.CurrentAction == null)
      {
        HideCenterProgressBar();
        return;
      }

      EnsureProgressBarReady();
      if (runningBarRoot != null && !runningBarRoot.activeSelf)
        runningBarRoot.SetActive(true);

      if (progressLabelText != null)
        progressLabelText.text = "进行中 · " + WorkActionRules.FormatActionTitle(runner.CurrentAction);
      if (progressFill != null)
        progressFill.fillAmount = runner.Progress;
      if (progressTimeText != null)
        progressTimeText.text = WorkActionRules.FormatRemainingTime(runner.SecondsRemaining);
    }

    public override void OnInventoryChanged(MainUIController host) => RefreshCenterState(host);

    public override void OnWorkOrMasteryChanged(MainUIController host) => RefreshCenterState(host);

    private void RefreshCenterState(MainUIController host)
    {
      _host = host;
      RefreshActionCardBindings();
      UpdateActionSelectionUi();
      if (string.IsNullOrEmpty(_activeActionId)) return;

      var action = GameContent.GetAction(_activeActionId);
      if (action != null)
        ShowDetail(action);
      detailPanel?.RefreshWorkButton();
    }

    private void OnActionSelected(int index)
    {
      if (_host == null || index >= _visibleActions.Count) return;
      var action = _visibleActions[index];
      if (action == null || !WorkActionRules.IsRegionUnlocked(_host.Session.Player, action))
        return;
      SelectAction(action.Id);
    }

    public bool IsRunningThisWork()
    {
      var runner = _host?.Session?.Runner;
      if (runner == null || !runner.IsRunning || runner.CurrentAction == null) return false;
      return runner.CurrentAction.WorkId == EffectiveWorkId;
    }

    private string EffectiveWorkId
    {
      get
      {
        if (!string.IsNullOrEmpty(WorkId)) return WorkId;
        var hub = GetComponentInParent<ScavengeHubView>();
        return hub != null ? hub.WorkId : WorkId;
      }
    }

    /// <summary>当前选中的动作卡是否就是 Runner 正在执行的那条。</summary>
    public bool IsShowingRunningAction()
    {
      if (!IsRunningThisWork()) return false;
      var runner = _host.Session.Runner;
      return !string.IsNullOrEmpty(_activeActionId) && runner.CurrentAction.Id == _activeActionId;
    }

    public string SelectedActionId => _activeActionId;

    public bool TryStopCurrentAction()
    {
      if (!IsRunningThisWork()) return false;
      _host.Session.Runner.Stop(); // OnRunnerActionStopped 收口进度条 / 卡 / 工作按钮
      return true;
    }

    public bool CanStartSelectedAction()
    {
      if (_host == null || string.IsNullOrEmpty(_activeActionId)) return false;
      if (IsShowingRunningAction()) return false;
      var action = GameContent.GetAction(_activeActionId);
      return action != null && action.WorkId == WorkId &&
             WorkActionRules.CanPerform(_host.Session.Player, action);
    }

    public bool TryStartSelectedAction()
    {
      if (!CanStartSelectedAction()) return false;
      var action = GameContent.GetAction(_activeActionId);
      if (action == null || !_host.Session.Runner.TryStart(action)) return false;
      ShowCenterProgressBar(action);
      detailPanel?.RefreshWorkButton();
      return true;
    }

    private void SelectAction(string actionId)
    {
      var action = GameContent.GetAction(actionId);
      if (action == null || action.WorkId != WorkId || _host == null) return;

      if (!string.IsNullOrEmpty(action.SceneId) && action.SceneId != _activeSceneId)
      {
        if (BoundToMap)
          return;
        SetActiveScene(action.SceneId, refreshCards: false);
      }

      _activeActionId = actionId;
      UpdateActionSelectionUi();
      ShowDetail(action);
      UpdateLocationBannerForScene();
      detailPanel?.RefreshWorkButton();
      SyncProgressBarVisibility(_host);
    }

    private void EnsureActiveScene()
    {
      if (BoundToMap)
      {
        _activeSceneId = sceneId;
        return;
      }
      var groups = SceneGroups;
      if (groups.Count == 0)
      {
        _activeSceneId = null;
        return;
      }

      if (!string.IsNullOrEmpty(_activeActionId))
      {
        var action = GameContent.GetAction(_activeActionId);
        if (action != null && !string.IsNullOrEmpty(action.SceneId))
        {
          _activeSceneId = action.SceneId;
          return;
        }
      }

      if (!string.IsNullOrEmpty(_activeSceneId) && FindSceneGroup(_activeSceneId) != null)
        return;

      var runnerAction = _host?.Session?.Runner?.CurrentAction;
      if (runnerAction != null && runnerAction.WorkId == WorkId && !string.IsNullOrEmpty(runnerAction.SceneId))
      {
        _activeSceneId = runnerAction.SceneId;
        return;
      }

      _activeSceneId = FindFirstAccessibleSceneId() ?? groups[0].SceneId;
    }

    private void SetActiveScene(string sceneId, bool refreshCards)
    {
      if (string.IsNullOrEmpty(sceneId) || sceneId == _activeSceneId) return;
      if (FindSceneGroup(sceneId) == null) return;

      _activeSceneId = sceneId;
      if (!string.IsNullOrEmpty(_activeActionId))
      {
        var current = GameContent.GetAction(_activeActionId);
        if (current == null || current.SceneId != sceneId)
          _activeActionId = null;
      }

      UpdateLocationBannerForScene();
      if (!refreshCards) return;

      RefreshActionCardBindings();
      UpdateActionSelectionUi();
      SelectDefaultVisibleAction();
    }

    private void SelectDefaultVisibleAction()
    {
      if (_visibleActions.Count == 0)
      {
        _activeActionId = null;
        UpdateActionSelectionUi();
        return;
      }

      var id = FindFirstUnlockedActionId();
      if (!string.IsNullOrEmpty(id))
        SelectAction(id);
    }

    private void ShowDetail(WorkActionDefinition action)
    {
      if (action == null || detailPanel == null || _host?.Session?.Player == null) return;
      detailPanel.ShowAction(action, _host.Session.Player);
    }

    private WorkSceneGroup FindSceneGroup(string sceneId)
    {
      if (string.IsNullOrEmpty(sceneId)) return null;
      foreach (var group in SceneGroups)
      {
        if (group.SceneId == sceneId) return group;
      }
      return null;
    }

    private string FindFirstAccessibleSceneId()
    {
      if (_host == null) return null;
      var player = _host.Session.Player;
      foreach (var group in SceneGroups)
      {
        if (player.GetWork(WorkId).Level >= group.MinRequiredWorkLevel)
          return group.SceneId;
      }
      var groups = SceneGroups;
      return groups.Count > 0 ? groups[0].SceneId : null;
    }

    private void RefreshActionCardBindings()
    {
      if (_host == null) return;

      _visibleActions.Clear();
      var group = FindSceneGroup(_activeSceneId);
      if (group?.Actions != null)
      {
        foreach (var action in group.Actions)
          _visibleActions.Add(action);
      }

      var work = GameContent.GetWork(WorkId);
      var player = _host.Session.Player;
      for (var i = 0; i < actionCards.Count; i++)
      {
        var card = actionCards[i];
        if (card == null) continue;
        if (i >= _visibleActions.Count)
        {
          card.gameObject.SetActive(false);
          continue;
        }

        var action = _visibleActions[i];
        var unlocked = WorkActionRules.IsRegionUnlocked(player, action);
        var metaLeft = WorkActionRules.FormatDurationSeconds(action.DurationSeconds);
        var metaRight = WorkActionRules.FormatYieldHint(action);
        var unlockHint = unlocked
          ? null
          : WorkActionRules.FormatUnlockHint(action, work?.DisplayName);

        var mastery = player.GetActionMastery(action.Id).Level;
        actionCards[i].gameObject.SetActive(true);
        actionCards[i].Bind(
          WorkActionRules.FormatSpotTitle(action),
          metaLeft,
          metaRight,
          !unlocked,
          ActionImageLoader.Get(action),
          mastery,
          ActionCardView.ResolveMasteryIcon(mastery),
          unlockHint);
      }
    }

    private string FindFirstUnlockedActionId()
    {
      if (_host == null) return null;
      foreach (var action in _visibleActions)
      {
        if (WorkActionRules.CanPerform(_host.Session.Player, action))
          return action.Id;
      }
      return _visibleActions.Count > 0 ? _visibleActions[0].Id : null;
    }

    private void UpdateActionSelectionUi()
    {
      for (var i = 0; i < actionCards.Count; i++)
        actionCards[i].SetSelected(i < _visibleActions.Count && _visibleActions[i].Id == _activeActionId);
    }

    private void UpdateLocationBannerForScene()
    {
      if (locationTitleText == null) return;
      var group = FindSceneGroup(_activeSceneId);
      if (group != null && !string.IsNullOrEmpty(group.SceneName))
      {
        locationTitleText.text = group.SceneName;
        return;
      }

      var work = GameContent.GetWork(WorkId);
      if (work != null && !string.IsNullOrEmpty(work.LocationName))
        locationTitleText.text = work.LocationName;
    }

    private void SyncProgressBarVisibility(MainUIController host)
    {
      if (IsShowingRunningAction())
      {
        var action = host?.Session?.Runner?.CurrentAction;
        if (action != null)
          ShowCenterProgressBar(action);
        else
          HideCenterProgressBar();
      }
      else
        HideCenterProgressBar();
    }

    private void ShowCenterProgressBar(WorkActionDefinition action)
    {
      if (action == null) return;
      EnsureProgressBarReady();
      if (runningBarRoot != null)
        runningBarRoot.SetActive(true);
      if (progressLabelText != null)
        progressLabelText.text = "进行中 · " + WorkActionRules.FormatActionTitle(action);
      if (progressFill != null)
        progressFill.fillAmount = 0f;
      if (progressTimeText != null)
        progressTimeText.text = WorkActionRules.FormatRemainingTime(action.DurationSeconds);
    }

    private void HideCenterProgressBar()
    {
      if (progressFill != null)
        progressFill.fillAmount = 0f;
      if (progressTimeText != null)
        progressTimeText.text = "00:00";
      if (runningBarRoot != null)
        runningBarRoot.SetActive(false);
    }

    /// <summary>Filled 必须有 sprite；预制体应已绑 ui_progress_fill，此处仅兜底 Resources。</summary>
    private void EnsureProgressBarReady()
    {
      if (progressFill == null) return;
      progressFill.type = Image.Type.Filled;
      progressFill.fillMethod = Image.FillMethod.Horizontal;
      progressFill.fillOrigin = (int)Image.OriginHorizontal.Left;
      if (progressFill.sprite == null)
        progressFill.sprite = ItemIconLoader.GetProgressFill();
    }
  }
}
