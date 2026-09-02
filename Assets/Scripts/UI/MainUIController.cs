using System.Collections.Generic;
using TMPro;
using UniverIdle.Game;
using UnityEngine;
using UnityEngine.UI;

namespace UniverIdle.UI
{
  /// <summary>主界面：工作切换、各工作独立 Center、右侧详情与背包。</summary>
  [DefaultExecutionOrder(0)]
  public class MainUIController : MonoBehaviour
  {
    [Header("技能导航")]
    [SerializeField] private List<SkillNavItemView> skillItems = new();

    [Header("工作 Center")]
    [SerializeField] private WorkCenterHost workCenterHost;

    [Header("右侧详情")]
    [SerializeField] private TextMeshProUGUI detailTitleText;
    [SerializeField] private TextMeshProUGUI detailBodyText;
    [SerializeField] private Button detailWorkButton;
    [SerializeField] private LootPreviewView lootPreview;

    [Header("背包")]
    [SerializeField] private InventoryPanelView inventoryPanel;
    [SerializeField] private Button inventoryButton;

    private GameSession _session;
    private string _activeWorkId;
    private bool _buttonsWired;

    public GameSession Session => _session;

    private void Awake()
    {
      _session = GetComponent<GameSession>();
      if (_session == null)
        _session = gameObject.AddComponent<GameSession>();
      ResolveReferences();
    }

    private void Start()
    {
      ResolveReferences();
      WireButtons();

      if (_session?.Player != null)
      {
        _session.Player.OnInventoryChanged += OnInventoryChanged;
        _session.Player.OnWorkChanged += OnActiveWorkProgressChanged;
        _session.Player.OnSceneProgressChanged += OnSceneProgressChanged;
      }
      if (_session?.Runner != null)
      {
        _session.Runner.OnActionCompleted += OnActionCompleted;
        _session.Runner.OnActionStopped += OnActionStopped;
      }

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

      if (workCenterHost != null)
      {
        foreach (var view in workCenterHost.GetComponentsInChildren<WorkCenterView>(true))
          view.Wire(this);
      }

      if (inventoryButton != null)
      {
        inventoryButton.onClick.RemoveAllListeners();
        inventoryButton.onClick.AddListener(ToggleInventoryPanel);
      }

      if (detailWorkButton != null)
      {
        detailWorkButton.onClick.RemoveAllListeners();
        detailWorkButton.onClick.AddListener(OnDetailWorkClicked);
      }
    }

    private void ResolveReferences()
    {
      if (skillItems == null || skillItems.Count == 0)
        skillItems = new List<SkillNavItemView>(GetComponentsInChildren<SkillNavItemView>(true));

      if (workCenterHost == null)
        workCenterHost = GetComponentInChildren<WorkCenterHost>(true);

      var canvas = GetComponentInParent<Canvas>();
      if (canvas != null)
      {
        if (inventoryPanel == null)
          inventoryPanel = canvas.GetComponentInChildren<InventoryPanelView>(true);

        if (inventoryButton == null)
        {
          foreach (var btn in canvas.GetComponentsInChildren<Button>(true))
          {
            if (btn.gameObject.name == "Btn_背包")
            {
              inventoryButton = btn;
              break;
            }
          }
        }
      }

      if (detailTitleText == null)
        detailTitleText = FindTmp("Detail/Text") ?? FindTmp("Body/Detail/Text");

      if (detailBodyText == null)
        detailBodyText = FindDetailBodyText();

      if (detailWorkButton == null)
      {
        var detail = FindDetailTransform();
        if (detail != null)
        {
          foreach (var btn in detail.GetComponentsInChildren<Button>(true))
          {
            if (btn.gameObject.name == "Btn_工作")
            {
              detailWorkButton = btn;
              break;
            }
          }
        }
      }

      if (lootPreview == null)
        lootPreview = GetComponentInChildren<LootPreviewView>(true);
    }

    private Transform FindDetailTransform()
    {
      var detail = transform.Find("Detail");
      if (detail != null) return detail;
      return transform.Find("Body/Detail");
    }

    private TextMeshProUGUI FindDetailBodyText()
    {
      var detail = FindDetailTransform();
      if (detail == null) return null;
      for (var i = 0; i < detail.childCount; i++)
      {
        var child = detail.GetChild(i);
        if (child.name != "Text") continue;
        var tmp = child.GetComponent<TextMeshProUGUI>();
        if (tmp != null && tmp != detailTitleText) return tmp;
      }
      return null;
    }

    private TextMeshProUGUI FindTmp(string path)
    {
      var t = transform.Find(path);
      return t != null ? t.GetComponent<TextMeshProUGUI>() : null;
    }

    private void OnDestroy()
    {
      if (_session?.Player != null)
      {
        _session.Player.OnInventoryChanged -= OnInventoryChanged;
        _session.Player.OnWorkChanged -= OnActiveWorkProgressChanged;
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
      workCenterHost?.Active?.TickProgress(this);
    }

    public void SelectWork(string workId)
    {
      var work = GameContent.GetWork(workId);
      if (work == null || workCenterHost == null) return;

      var workChanged = _activeWorkId != workId;
      if (workChanged && _session?.Runner != null && _session.Runner.IsRunning)
      {
        var running = _session.Runner.CurrentAction;
        if (running != null && running.WorkId != workId)
          _session.Runner.Stop();
      }

      _activeWorkId = workId;
      for (var i = 0; i < skillItems.Count; i++)
      {
        var item = skillItems[i];
        if (item != null)
          item.SetSelected(item.WorkId == workId);
      }

      workCenterHost.TryShow(workId, this);
      RefreshWorkNav();
      RefreshDetailWorkButton();
    }

    public void ShowActionDetail(WorkActionDefinition action)
    {
      if (action == null) return;
      var work = GameContent.GetWork(action.WorkId);
      if (detailTitleText != null)
        detailTitleText.text = action.DisplayName;
      if (detailBodyText != null)
        detailBodyText.text = WorkActionUiFormatter.BuildDescription(action, _session.Player, work);
      lootPreview?.Bind(action);
      RefreshDetailWorkButton();
    }

    public void ShowActionStoppedDetail(WorkActionDefinition action)
    {
      if (detailBodyText != null && action != null)
        detailBodyText.text = SceneProgressRules.FormatCostHint(action) + "\n\n请补充道具后重新开始。";
      RefreshDetailWorkButton();
    }

    public void RefreshDetailWorkButton()
    {
      if (detailWorkButton == null) return;
      var center = GetActiveStandardCenter();
      detailWorkButton.interactable = center != null && center.CanStartSelectedAction();
    }

    private void OnDetailWorkClicked() => GetActiveStandardCenter()?.TryStartSelectedAction();

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

    private StandardWorkCenterView GetActiveStandardCenter() =>
      workCenterHost?.Active as StandardWorkCenterView;

    private void OnActiveWorkProgressChanged(string workId)
    {
      RefreshWorkNav();
      if (workId != _activeWorkId) return;
      GetActiveStandardCenter()?.OnWorkOrSceneChanged(this);
    }

    private void OnSceneProgressChanged(string workId, string sceneId) =>
      OnActiveWorkProgressChanged(workId);

    private void OnInventoryChanged()
    {
      RefreshInventory();
      if (_session?.Runner?.CurrentAction != null &&
          !SceneProgressRules.CanAffordCost(_session.Player, _session.Runner.CurrentAction))
        _session.Runner.Stop();

      GetActiveStandardCenter()?.OnInventoryChanged(this);
      RefreshDetailWorkButton();
    }

    private void OnActionStopped(WorkActionDefinition action)
    {
      if (action == null) return;
      if (action.WorkId == _activeWorkId)
        GetActiveStandardCenter()?.OnActionStopped(action, this);
    }

    private void OnActionCompleted(ActionCompleteResult result)
    {
      if (detailBodyText != null && result?.Action != null)
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
      lootPreview?.RevealLoot(result);
      RefreshInventory();
      RefreshWorkNav();
      RefreshDetailWorkButton();
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
