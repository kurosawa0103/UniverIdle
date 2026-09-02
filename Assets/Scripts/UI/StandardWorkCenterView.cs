using System.Collections.Generic;
using System.Text;
using TMPro;
using UniverIdle.Game;
using UnityEngine;
using UnityEngine.UI;

namespace UniverIdle.UI
{
  /// <summary>
  /// 横幅 + 动作卡 + 进度条的标准工作 Center。
  /// LocationBanner 表示一个场景块：BannerArt（场景名/Tags）+ ActionCards（子地点卡）。
  /// </summary>
  public sealed class StandardWorkCenterView : WorkCenterView
  {
    [SerializeField] private TextMeshProUGUI locationTitleText;
    [SerializeField] private List<ActionCardView> actionCards = new();
    [SerializeField] private Image progressFill;
    [SerializeField] private TextMeshProUGUI progressLabelText;
    [SerializeField] private TextMeshProUGUI progressTimeText;

    private MainUIController _host;
    private string _activeActionId;
    private string _activeSceneId;
    private readonly List<WorkActionDefinition> _visibleActions = new();
    private readonly List<WorkSceneGroup> _sceneGroups = new();
    private Transform _sceneTagsRoot;
    private readonly List<GameObject> _sceneTagObjects = new();
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
      _sceneTagsRoot = locationTitleText != null
        ? locationTitleText.transform.parent?.Find("Tags")
        : null;
    }

    public override void OnActivated(MainUIController host)
    {
      _host = host;
      _activeSceneId = null;
      _activeActionId = null;
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
      RebuildSceneGroups();
      EnsureActiveScene();
      UpdateLocationBannerForScene();
      RefreshSceneTags();

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
      RefreshSceneTags();
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

      if (!string.IsNullOrEmpty(action.SceneId) && action.SceneId != _activeSceneId)
        SetActiveScene(action.SceneId, refreshCards: false);

      _activeActionId = actionId;
      UpdateActionSelectionUi();
      _host.ShowActionDetail(action);
      UpdateLocationBannerForScene();

      if (autoStart && SceneProgressRules.CanPerform(_host.Session.Player, action) &&
          _host.Session.Runner.TryStart(action) && progressLabelText != null)
        progressLabelText.text = "进行中 · " + FormatSpotTitle(action);
    }

    private void RebuildSceneGroups()
    {
      _sceneGroups.Clear();
      foreach (var group in GameContent.GetSceneGroupsForWork(WorkId))
        _sceneGroups.Add(group);
    }

    private void EnsureActiveScene()
    {
      if (_sceneGroups.Count == 0)
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

      _activeSceneId = FindFirstAccessibleSceneId() ?? _sceneGroups[0].SceneId;
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
      if (_visibleActions.Count > 0)
        _host?.ShowActionDetail(_visibleActions[0]);
    }

    private WorkSceneGroup FindSceneGroup(string sceneId)
    {
      foreach (var group in _sceneGroups)
      {
        if (group.SceneId == sceneId) return group;
      }
      return null;
    }

    private string FindFirstAccessibleSceneId()
    {
      if (_host == null) return null;
      var player = _host.Session.Player;
      foreach (var group in _sceneGroups)
      {
        if (player.GetWork(WorkId).Level >= group.MinRequiredWorkLevel)
          return group.SceneId;
      }
      return _sceneGroups.Count > 0 ? _sceneGroups[0].SceneId : null;
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
          FormatSpotTitle(action),
          metaLeft,
          metaRight,
          WorkActionUiFormatter.BuildDescription(action, player, work),
          !canPerform,
          action.ThumbColor);
      }
    }

    private void RefreshSceneTags()
    {
      if (_sceneTagsRoot == null) return;

      ClearSceneTags();
      if (_sceneGroups.Count <= 1) return;

      var player = _host?.Session?.Player;
      foreach (var group in _sceneGroups)
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

      if (_sceneTagsRoot == null) return;
      for (var i = _sceneTagsRoot.childCount - 1; i >= 0; i--)
        Destroy(_sceneTagsRoot.GetChild(i).gameObject);
    }

    private GameObject CreateSceneTag(string label, bool selected, bool unlocked, string sceneId)
    {
      var rt = new GameObject($"SceneTag_{sceneId}", typeof(RectTransform)).GetComponent<RectTransform>();
      rt.SetParent(_sceneTagsRoot, false);
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

    private static string FormatSpotTitle(WorkActionDefinition action)
    {
      if (action == null) return "";
      if (!string.IsNullOrEmpty(action.SpotName)) return action.SpotName;

      var name = action.DisplayName;
      if (!string.IsNullOrEmpty(name))
      {
        var sep = name.IndexOf('·');
        if (sep >= 0 && sep < name.Length - 1)
          return name.Substring(sep + 1).Trim();
      }
      return name ?? action.Id;
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
