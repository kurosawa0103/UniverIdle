using System.Collections.Generic;
using UniverIdle.Game;
using UnityEngine;
using UnityEngine.UI;

namespace UniverIdle.UI
{
  /// <summary>主界面：工作切换、各工作独立 Center、背包、全局获得提示。</summary>
  [DefaultExecutionOrder(0)]
  public class MainUIController : MonoBehaviour
  {
    [Header("技能导航")]
    [SerializeField] private List<SkillNavItemView> skillItems = new();

    [Header("工作 Center")]
    [SerializeField] private WorkCenterHost workCenterHost;

    [Header("背包")]
    [SerializeField] private InventoryPanelView inventoryPanel;
    [SerializeField] private Button inventoryButton;

    [Header("顶栏")]
    [SerializeField] private TopBarGoldView topBarGold;

    [Header("全局获得提示")]
    [SerializeField] private LootToastView lootToast;

    private GameSession _session;
    private string _activeWorkId;
    private bool _buttonsWired;

    public GameSession Session => _session;

    private void Awake()
    {
      _session = GetComponent<GameSession>();
      if (_session == null)
        Debug.LogError("[MainUI] App 上缺少 GameSession，请手配。", this);
    }

    private void Start()
    {
      WireButtons();

      if (_session?.Player != null)
      {
        _session.Player.OnInventoryChanged += OnInventoryChanged;
        _session.Player.OnWorkChanged += OnActiveWorkProgressChanged;
        _session.Player.OnActionMasteryChanged += OnActionMasteryChanged;
      }
      if (_session?.Runner != null)
      {
        _session.Runner.OnActionCompleted += OnActionCompleted;
        _session.Runner.OnActionStopped += OnActionStopped;
      }

      topBarGold?.Bind(_session?.Player);
      SelectWork(GetDefaultWorkId());
      RefreshInventory();
      RefreshWorkNav();
    }

    private string GetDefaultWorkId()
    {
      for (var i = 0; i < skillItems.Count; i++)
      {
        var item = skillItems[i];
        if (item != null && item.IsAvailable && !string.IsNullOrEmpty(item.WorkId))
          return item.WorkId;
      }

      return GameContent.WorkScavengeId;
    }

    private void WireButtons()
    {
      if (_buttonsWired) return;
      _buttonsWired = true;

      for (var i = 0; i < skillItems.Count; i++)
      {
        var item = skillItems[i];
        if (item == null) continue;
        var btn = item.GetComponent<Button>();
        if (btn != null) BindSkillButton(i, btn);
      }

      workCenterHost?.WireAll(this);

      if (inventoryButton != null)
      {
        inventoryButton.onClick.RemoveAllListeners();
        inventoryButton.onClick.AddListener(ToggleInventoryPanel);
      }
    }

    private void OnDestroy()
    {
      if (_session?.Player != null)
      {
        _session.Player.OnInventoryChanged -= OnInventoryChanged;
        _session.Player.OnWorkChanged -= OnActiveWorkProgressChanged;
        _session.Player.OnActionMasteryChanged -= OnActionMasteryChanged;
      }
      if (_session?.Runner != null)
      {
        _session.Runner.OnActionCompleted -= OnActionCompleted;
        _session.Runner.OnActionStopped -= OnActionStopped;
      }
    }

    private void Update()
    {
      workCenterHost?.Active?.TickProgress(this);
    }

    public void SelectWork(string workId)
    {
      var work = GameContent.GetWork(workId);
      if (work == null || workCenterHost == null) return;

      // 切工作只换界面，不停止后台挂机；点另一工作的开始才会换 Runner 当前动作
      _activeWorkId = workId;
      for (var i = 0; i < skillItems.Count; i++)
      {
        var item = skillItems[i];
        if (item != null)
          item.SetSelected(item.WorkId == workId);
      }

      workCenterHost.TryShow(workId, this);
      RefreshWorkNav();
    }

    private void RefreshInventory() => inventoryPanel?.Refresh(_session?.Player);

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

    private void OnActiveWorkProgressChanged(string workId)
    {
      RefreshWorkNav();
      if (workId != _activeWorkId) return;
      workCenterHost?.Active?.OnWorkOrSceneChanged(this);
    }

    private void OnActionMasteryChanged(string actionId) =>
      OnActiveWorkProgressChanged(_activeWorkId);

    private void OnInventoryChanged()
    {
      RefreshInventory();
      if (_session?.Runner?.CurrentAction != null &&
          !SceneProgressRules.CanAffordCost(_session.Player, _session.Runner.CurrentAction))
        _session.Runner.Stop();

      workCenterHost?.Active?.OnInventoryChanged(this);
    }

    private void OnActionStopped(WorkActionDefinition action)
    {
      if (action == null) return;
      if (workCenterHost != null && workCenterHost.TryGet(action.WorkId, out var owner))
        owner.OnRunnerActionStopped(this, action);
      else
        workCenterHost?.Active?.OnRunnerActionStopped(this, action);
    }

    private void OnActionCompleted(ActionCompleteResult result)
    {
      lootToast?.PushResult(result, _session?.Player);

      if (result?.Action != null &&
          workCenterHost != null &&
          workCenterHost.TryGet(result.Action.WorkId, out var owner))
        owner.OnActionCompleted(this, result);

      RefreshWorkNav();
    }

    private void ToggleInventoryPanel() => inventoryPanel?.Toggle(_session?.Player);

    private void BindSkillButton(int index, Button button)
    {
      if (index < 0 || index >= skillItems.Count) return;
      var item = skillItems[index];
      if (!item.IsAvailable || string.IsNullOrEmpty(item.WorkId))
      {
        button.interactable = false;
        return;
      }
      var workId = item.WorkId;
      button.onClick.RemoveAllListeners();
      button.onClick.AddListener(() => SelectWork(workId));
    }
  }
}
