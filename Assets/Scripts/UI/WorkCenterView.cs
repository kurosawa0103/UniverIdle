using UniverIdle.Game;
using UnityEngine;

namespace UniverIdle.UI
{
  /// <summary>单个工作的 Center 界面；切换工作时显示/隐藏对应实例。</summary>
  public abstract class WorkCenterView : MonoBehaviour
  {
    [SerializeField] private string workId;

    public string WorkId => workId;

    public virtual void OnActivated(MainUIController host) { }

    public virtual void OnDeactivated() { }

    public virtual void Wire(MainUIController host) { }

    public virtual void Refresh(MainUIController host) { }

    public virtual void TickProgress(MainUIController host) { }

    public virtual void OnActionCompleted(MainUIController host, ActionCompleteResult result) { }

    public virtual void OnRunnerActionStopped(MainUIController host, WorkActionDefinition action) { }

    public virtual void OnInventoryChanged(MainUIController host) { }

    public virtual void OnWorkOrMasteryChanged(MainUIController host) { }
  }
}
