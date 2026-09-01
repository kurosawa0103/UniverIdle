using System.Collections.Generic;
using System.Text;
using TMPro;
using UniverIdle.Game;
using UnityEngine;
using UnityEngine.UI;

namespace UniverIdle.UI
{
  /// <summary>主界面：工作切换、动作挂机、背包与进度展示。</summary>
  public class MainUIController : MonoBehaviour
  {
    [Header("技能导航")]
    [SerializeField] private List<SkillNavItemView> skillItems = new();

    [Header("中部")]
    [SerializeField] private TextMeshProUGUI locationTitleText;
    [SerializeField] private List<ActionCardView> actionCards = new();
    [SerializeField] private Image progressFill;
    [SerializeField] private TextMeshProUGUI progressLabelText;
    [SerializeField] private TextMeshProUGUI progressTimeText;

    [Header("右侧详情")]
    [SerializeField] private TextMeshProUGUI detailTitleText;
    [SerializeField] private TextMeshProUGUI detailBodyText;

    [Header("底栏")]
    [SerializeField] private InventoryBarView inventoryBar;

    private GameSession _session;
    private string _activeWorkId = GameContent.WorkScavengeId;
    private string _activeActionId;
    private readonly List<WorkActionDefinition> _visibleActions = new();

    private void Awake()
    {
      _session = GetComponent<GameSession>();
      if (_session == null)
        _session = gameObject.AddComponent<GameSession>();
    }

    private void Start()
    {
      if (_session?.Player != null)
      {
        _session.Player.OnInventoryChanged += OnInventoryChanged;
        _session.Player.OnWorkChanged += OnWorkChanged;
        _session.Player.OnSceneProgressChanged += OnSceneProgressChanged;
      }
      if (_session?.Runner != null)
      {
        _session.Runner.OnActionCompleted += OnActionCompleted;
        _session.Runner.OnActionStopped += OnActionStopped;
      }

      SelectWork(_activeWorkId);
      RefreshInventory();
      RefreshWorkNav();
    }

    private void OnDestroy()
    {
      if (_session?.Player != null)
      {
        _session.Player.OnInventoryChanged -= OnInventoryChanged;
        _session.Player.OnWorkChanged -= OnWorkChanged;
        _session.Player.OnSceneProgressChanged -= OnSceneProgressChanged;
      }
      if (_session?.Runner != null)
      {
        _session.Runner.OnActionCompleted -= OnActionCompleted;
        _session.Runner.OnActionStopped -= OnActionStopped;
      }
    }

    private void Update()
    {
      if (_session?.Runner == null) return;

      var runner = _session.Runner;
      if (progressFill != null)
        progressFill.fillAmount = runner.Progress;
      if (progressTimeText != null)
        progressTimeText.text = FormatTime(runner.SecondsRemaining);
    }

    public void SelectWork(string workId)
    {
      var work = GameContent.GetWork(workId);
      if (work == null) return;

      var workChanged = _activeWorkId != workId;
      _activeWorkId = workId;
      if (workChanged)
        _activeActionId = null;
      for (var i = 0; i < skillItems.Count; i++)
        skillItems[i].SetSelected(skillItems[i].WorkId == workId);

      if (locationTitleText != null)
        locationTitleText.text = work.LocationName;

      RefreshActionCards();
      RefreshWorkNav();
    }

    public void SelectAction(string actionId)
    {
      var action = GameContent.GetAction(actionId);
      if (action == null || action.WorkId != _activeWorkId) return;
      if (!SceneProgressRules.CanPerform(_session.Player, action)) return;
      if (!_session.Runner.TryStart(action)) return;

      _activeActionId = actionId;

      for (var i = 0; i < actionCards.Count; i++)
        actionCards[i].SetSelected(i < _visibleActions.Count && _visibleActions[i].Id == actionId);

      RefreshActionDetail(action);
      UpdateLocationBanner(action);
      RefreshWorkNav();
      if (progressLabelText != null)
        progressLabelText.text = "进行中 · " + action.DisplayName;
    }

    private void RefreshActionCards()
    {
      RefreshActionCardBindings();

      if (string.IsNullOrEmpty(_activeActionId) || !SceneProgressRules.CanPerform(_session.Player, GameContent.GetAction(_activeActionId)))
        _activeActionId = FindFirstUnlockedActionId();

      if (!string.IsNullOrEmpty(_activeActionId))
        SelectAction(_activeActionId);
      else if (_visibleActions.Count > 0)
        RefreshActionDetail(_visibleActions[0]);
    }

    private void RefreshActionCardBindings()
    {
      _visibleActions.Clear();
      foreach (var action in GameContent.GetActionsForWork(_activeWorkId))
        _visibleActions.Add(action);

      var work = GameContent.GetWork(_activeWorkId);
      var player = _session.Player;
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
        var sceneProgress = player.GetSceneProgress(action.WorkId, action.SceneId);
        var metaRight = unlocked
          ? SceneProgressRules.CanAffordCost(player, action)
            ? $"熟练 Lv.{sceneProgress.Level}"
            : SceneProgressRules.FormatCostHint(action)
          : SceneProgressRules.FormatUnlockHint(action, work?.DisplayName);

        actionCards[i].gameObject.SetActive(true);
        actionCards[i].Bind(
          action.DisplayName,
          FormatDuration(action.DurationSeconds),
          metaRight,
          BuildActionDescription(action, player, work),
          !canPerform,
          action.ThumbColor);
      }
    }

    private string FindFirstUnlockedActionId()
    {
      foreach (var action in _visibleActions)
      {
        if (SceneProgressRules.CanPerform(_session.Player, action))
          return action.Id;
      }
      return _visibleActions.Count > 0 ? _visibleActions[0].Id : null;
    }

    private void UpdateActionSelectionUi()
    {
      for (var i = 0; i < actionCards.Count; i++)
        actionCards[i].SetSelected(i < _visibleActions.Count && _visibleActions[i].Id == _activeActionId);
    }

    private static string BuildActionDescription(WorkActionDefinition action, PlayerState player, WorkDefinition work)
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

    private void RefreshActionDetail(WorkActionDefinition action)
    {
      if (action == null) return;
      var work = GameContent.GetWork(action.WorkId);
      if (detailTitleText != null)
        detailTitleText.text = action.DisplayName;
      if (detailBodyText != null)
        detailBodyText.text = BuildActionDescription(action, _session.Player, work);
    }

    private void UpdateLocationBanner(WorkActionDefinition action)
    {
      if (locationTitleText == null || action == null) return;
      locationTitleText.text = string.IsNullOrEmpty(action.SceneName) ? action.DisplayName : action.SceneName;
    }

    private void RefreshInventory() => inventoryBar?.Refresh(_session?.Player);

    private void RefreshWorkNav()
    {
      for (var i = 0; i < skillItems.Count; i++)
      {
        var workId = skillItems[i].WorkId;
        if (string.IsNullOrEmpty(workId)) continue;

        var work = GameContent.GetWork(workId);
        var workProgress = _session.Player.GetWork(workId);
        skillItems[i].UpdateProgress(workProgress.Level, workProgress.XpRatio(work), work?.DisplayName);
      }
    }

    private void OnWorkChanged(string workId)
    {
      RefreshWorkNav();
      if (workId != _activeWorkId) return;

      RefreshActionCardBindings();
      UpdateActionSelectionUi();

      if (!string.IsNullOrEmpty(_activeActionId))
      {
        var action = GameContent.GetAction(_activeActionId);
        if (action != null)
          RefreshActionDetail(action);
      }
    }

    private void OnSceneProgressChanged(string workId, string sceneId)
    {
      RefreshWorkNav();
      if (workId != _activeWorkId) return;

      RefreshActionCardBindings();
      UpdateActionSelectionUi();

      if (!string.IsNullOrEmpty(_activeActionId))
      {
        var action = GameContent.GetAction(_activeActionId);
        if (action != null)
          RefreshActionDetail(action);
      }
    }

    private void OnInventoryChanged()
    {
      RefreshInventory();
      if (_session?.Runner?.CurrentAction != null &&
          !SceneProgressRules.CanAffordCost(_session.Player, _session.Runner.CurrentAction))
        _session.Runner.Stop();

      RefreshActionCardBindings();
      UpdateActionSelectionUi();
      if (!string.IsNullOrEmpty(_activeActionId))
      {
        var action = GameContent.GetAction(_activeActionId);
        if (action != null)
          RefreshActionDetail(action);
      }
    }

    private void OnActionStopped(WorkActionDefinition action)
    {
      if (action == null || action.WorkId != _activeWorkId) return;
      _activeActionId = null;
      if (progressLabelText != null)
        progressLabelText.text = "材料不足，已停止";
      if (detailBodyText != null)
        detailBodyText.text = SceneProgressRules.FormatCostHint(action) + "\n\n请补充道具后重新开始。";
      RefreshActionCardBindings();
      UpdateActionSelectionUi();
    }

    private void OnActionCompleted(ActionCompleteResult result)
    {
      if (detailBodyText != null)
      {
        var text = result.FormatLootSummary();
        if (result.WorkLeveledUp)
        {
          var work = GameContent.GetWork(result.Action.WorkId);
          var workName = work != null ? work.DisplayName : "拾荒";
          text += $"\n\n{workName}升至 Lv.{result.WorkNewLevel}！";
        }
        if (result.LeveledUp)
        {
          var scene = string.IsNullOrEmpty(result.SceneName) ? "本地区" : result.SceneName;
          text += $"\n\n{scene}熟练度升至 Lv.{result.NewLevel}！";
        }
        detailBodyText.text = text;
      }
      RefreshInventory();
    }

    public void BindSkillButton(int index, Button button)
    {
      if (index < 0 || index >= skillItems.Count) return;
      var workId = skillItems[index].WorkId;
      button.onClick.AddListener(() => SelectWork(workId));
    }

    public void BindActionCard(int index, Button button)
    {
      var captured = index;
      button.onClick.AddListener(() =>
      {
        if (captured < _visibleActions.Count)
          SelectAction(_visibleActions[captured].Id);
      });
    }

    private static string FormatDuration(float seconds) => $"{seconds:0.#} 秒";

    private static string FormatTime(float seconds)
    {
      var total = Mathf.CeilToInt(seconds);
      var m = total / 60;
      var s = total % 60;
      return m > 0 ? $"{m:00}:{s:00}" : $"00:{s:00}";
    }

#if UNITY_EDITOR
    public void SetReferences(
      List<SkillNavItemView> skills,
      TextMeshProUGUI locationTitle,
      List<ActionCardView> actions,
      Image progress,
      TextMeshProUGUI progressLabel,
      TextMeshProUGUI progressTime,
      TextMeshProUGUI detailTitle,
      TextMeshProUGUI detailBody,
      InventoryBarView inventory)
    {
      skillItems = skills;
      locationTitleText = locationTitle;
      actionCards = actions;
      progressFill = progress;
      progressLabelText = progressLabel;
      progressTimeText = progressTime;
      detailTitleText = detailTitle;
      detailBodyText = detailBody;
      inventoryBar = inventory;
    }
#endif
  }
}
