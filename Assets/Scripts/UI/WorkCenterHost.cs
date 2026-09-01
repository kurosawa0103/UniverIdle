using System.Collections.Generic;
using UnityEngine;

namespace UniverIdle.UI
{
  /// <summary>Body/Center 容器：按 workId 切换工作界面。</summary>
  public sealed class WorkCenterHost : MonoBehaviour
  {
    private readonly Dictionary<string, WorkCenterView> _views = new();

    public WorkCenterView Active { get; private set; }

    public void Register(WorkCenterView view)
    {
      if (view == null || string.IsNullOrEmpty(view.WorkId)) return;
      _views[view.WorkId] = view;
      view.gameObject.SetActive(false);
    }

    public bool TryShow(string workId, MainUIController host)
    {
      if (!_views.TryGetValue(workId, out var next) || next == null) return false;

      if (Active != null && Active != next)
      {
        Active.OnDeactivated();
        Active.gameObject.SetActive(false);
      }

      Active = next;
      Active.gameObject.SetActive(true);
      Active.OnActivated(host);
      Active.Refresh(host);
      return true;
    }

    public bool TryGet(string workId, out WorkCenterView view) => _views.TryGetValue(workId, out view);
  }
}
