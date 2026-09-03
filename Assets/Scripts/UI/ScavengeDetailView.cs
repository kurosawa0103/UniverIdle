using TMPro;
using UniverIdle.Game;
using UnityEngine;
using UnityEngine.UI;

namespace UniverIdle.UI
{
  /// <summary>
  /// 拾荒专用详情：在通用详情之上管理「开始/停止」按钮，只 Wire 拾荒地图 Center。
  /// 不挂到砍树等其它工作。
  /// </summary>
  public sealed class ScavengeDetailView : WorkActionDetailView
  {
    [SerializeField] private Button workButton;
    [SerializeField] private TextMeshProUGUI workButtonText;

    private StandardWorkCenterView _center;
    private bool _wired;

    private const string LabelStart = "拾荒";
    private const string LabelStop = "停止";

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
      RefreshWorkButton();
    }

    public override void ShowAction(WorkActionDefinition action, PlayerState player, bool revealGuaranteedLoot = false)
    {
      base.ShowAction(action, player, revealGuaranteedLoot);
      RefreshWorkButton();
    }

    public override void ShowStopped(WorkActionDefinition action, PlayerState player)
    {
      base.ShowStopped(action, player);
      RefreshWorkButton();
    }

    public override void OnActionCompleted(ActionCompleteResult result, PlayerState player)
    {
      base.OnActionCompleted(result, player);
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
  }
}
