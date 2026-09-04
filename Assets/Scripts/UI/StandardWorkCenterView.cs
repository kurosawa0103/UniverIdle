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
    [SerializeField] private Transform sceneTagsRoot;
    [SerializeField] private ScavengeDetailView detailPanel;

    private MainUIController _host;
    private string _activeActionId;
    private string _activeSceneId;
    private readonly List<WorkActionDefinition> _visibleActions = new();
    private readonly List<GameObject> _sceneTagObjects = new();
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
      RefreshSceneTags();

      RefreshActionCardBindings();
      if (string.IsNullOrEmpty(_activeActionId) ||
          !SceneProgressRules.CanPerform(host.Session.Player, GameContent.GetAction(_activeActionId)))
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
        progressLabelText.text = "进行中 · " + SceneProgressRules.FormatActionTitle(runner.CurrentAction);
      if (progressFill != null)
        progressFill.fillAmount = runner.Progress;
      if (progressTimeText != null)
        progressTimeText.text = SceneProgressRules.FormatRemainingTime(runner.SecondsRemaining);
    }

    public override void OnInventoryChanged(MainUIController host) => RefreshCenterState(host, refreshSceneTags: false);

    public override void OnWorkOrSceneChanged(MainUIController host) => RefreshCenterState(host, refreshSceneTags: true);

    private void RefreshCenterState(MainUIController host, bool refreshSceneTags)
    {
      _host = host;
      RefreshActionCardBindings();
      if (refreshSceneTags)
        RefreshSceneTags();
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
      if (action == null || !SceneProgressRules.IsRegionUnlocked(_host.Session.Player, action))
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
      _host.Session.Runner.Stop();
      detailPanel?.RefreshWorkButton();
      return true;
    }

    public bool CanStartSelectedAction()
    {
      if (_host == null || string.IsNullOrEmpty(_activeActionId)) return false;
      if (IsShowingRunningAction()) return false;
      var action = GameContent.GetAction(_activeActionId);
      return action != null && action.WorkId == WorkId &&
             SceneProgressRules.CanPerform(_host.Session.Player, action);
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
      RefreshSceneTags();
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
        var unlocked = SceneProgressRules.IsRegionUnlocked(player, action);
        var metaLeft = SceneProgressRules.FormatDurationSeconds(action.DurationSeconds);
        var metaRight = SceneProgressRules.FormatYieldHint(action);
        var unlockHint = unlocked
          ? null
          : SceneProgressRules.FormatUnlockHint(action, work?.DisplayName);

        var mastery = player.GetActionMastery(action.Id).Level;
        actionCards[i].gameObject.SetActive(true);
        actionCards[i].Bind(
          SceneProgressRules.FormatSpotTitle(action),
          metaLeft,
          metaRight,
          !unlocked,
          ActionImageLoader.Get(action),
          mastery,
          ActionCardView.ResolveMasteryIcon(mastery),
          unlockHint);
      }
    }

    private void RefreshSceneTags()
    {
      if (BoundToMap || sceneTagsRoot == null) return;

      ClearSceneTags();
      var groups = SceneGroups;
      if (groups.Count <= 1) return;

      var player = _host?.Session?.Player;
      foreach (var group in groups)
      {
        var sceneId = group.SceneId;
        var unlocked = player != null && player.GetWork(WorkId).Level >= group.MinRequiredWorkLevel;
        var selected = sceneId == _activeSceneId;
        var label = group.SceneName;
        if (!unlocked && player != null)
          label = $"🔒{label}";

        var tag = CreateSceneTag(label, selected, unlocked, sceneId);
        _sceneTagObjects.Add(tag);
      }
    }

    private void ClearSceneTags()
    {
      for (var i = 0; i < _sceneTagObjects.Count; i++)
      {
        if (_sceneTagObjects[i] != null)
          Destroy(_sceneTagObjects[i]);
      }
      _sceneTagObjects.Clear();

      if (sceneTagsRoot == null) return;
      for (var i = sceneTagsRoot.childCount - 1; i >= 0; i--)
        Destroy(sceneTagsRoot.GetChild(i).gameObject);
    }

    private GameObject CreateSceneTag(string label, bool selected, bool unlocked, string sceneId)
    {
      var rt = new GameObject($"SceneTag_{sceneId}", typeof(RectTransform)).GetComponent<RectTransform>();
      rt.SetParent(sceneTagsRoot, false);
      rt.sizeDelta = new Vector2(0f, 22f);

      var le = rt.gameObject.AddComponent<LayoutElement>();
      le.minHeight = 22f;
      le.preferredHeight = 22f;
      le.minWidth = 48f;

      var img = rt.gameObject.AddComponent<Image>();
      img.color = selected
        ? new Color(UITheme.Teal.r, UITheme.Teal.g, UITheme.Teal.b, 0.45f)
        : UITheme.TagBg;
      if (!unlocked)
        img.color = new Color(img.color.r, img.color.g, img.color.b, 0.55f);

      var btn = rt.gameObject.AddComponent<Button>();
      btn.targetGraphic = img;
      btn.interactable = unlocked;
      btn.onClick.AddListener(() => SetActiveScene(sceneId, refreshCards: true));

      var textGo = new GameObject("Label", typeof(RectTransform));
      var textRt = textGo.GetComponent<RectTransform>();
      textRt.SetParent(rt, false);
      textRt.anchorMin = Vector2.zero;
      textRt.anchorMax = Vector2.one;
      textRt.offsetMin = Vector2.zero;
      textRt.offsetMax = Vector2.zero;

      var tmp = textGo.AddComponent<TextMeshProUGUI>();
      if (locationTitleText != null)
      {
        tmp.font = locationTitleText.font;
        tmp.fontSharedMaterial = locationTitleText.fontSharedMaterial;
      }
      tmp.fontSize = 11f;
      tmp.alignment = TextAlignmentOptions.Center;
      tmp.color = selected ? UITheme.TealBright : UITheme.TagText;
      tmp.margin = new Vector4(8f, 3f, 8f, 3f);
      tmp.raycastTarget = false;
      tmp.text = label;

      return rt.gameObject;
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
        progressLabelText.text = "进行中 · " + SceneProgressRules.FormatActionTitle(action);
      if (progressFill != null)
        progressFill.fillAmount = 0f;
      if (progressTimeText != null)
        progressTimeText.text = SceneProgressRules.FormatRemainingTime(action.DurationSeconds);
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
