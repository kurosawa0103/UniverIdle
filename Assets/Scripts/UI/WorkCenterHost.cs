using System.Collections.Generic;
using UnityEngine;

namespace UniverIdle.UI
{
  /// <summary>Body/Center 容器：按 workId 切换工作界面。</summary>
  [DefaultExecutionOrder(-50)]
  public sealed class WorkCenterHost : MonoBehaviour
  {
    private readonly Dictionary<string, WorkCenterView> _views = new();

    public WorkCenterView Active { get; private set; }

    private void Awake() => EnsureRegistered();

    public void Register(WorkCenterView view)
    {
      if (view == null || string.IsNullOrEmpty(view.WorkId)) return;
      if (!ShouldRegister(view)) return;
      _views[view.WorkId] = view;
      view.gameObject.SetActive(false);
    }

    /// <summary>
    /// 只注册工作根。地图节点上的 StandardWorkCenterView 由 Hub 转发，不单独占 workId。
    /// </summary>
    private static bool ShouldRegister(WorkCenterView view)
    {
      var parent = view.transform.parent;
      if (parent == null) return true;
      return parent.GetComponentInParent<WorkCenterView>() == null;
    }

    public void WireAll(MainUIController host)
    {
      EnsureRegistered();
      foreach (var view in _views.Values)
        view.Wire(host);
    }

    public void EnsureRegistered()
    {
      if (_views.Count > 0) return;

      foreach (var view in GetComponentsInChildren<WorkCenterView>(true))
        Register(view);
    }

    public bool TryShow(string workId, MainUIController host)
    {
      EnsureRegistered();
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
  }
}
