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

    public bool TryGet(string workId, out WorkCenterView view)
    {
      EnsureRegistered();
      if (string.IsNullOrEmpty(workId))
      {
        view = null;
        return false;
      }
      return _views.TryGetValue(workId, out view) && view != null;
    }

    private void Awake() => EnsureRegistered();

    public void Register(WorkCenterView view)
    {
      if (view == null || string.IsNullOrEmpty(view.WorkId)) return;
      if (!ShouldRegister(view)) return;
      if (_views.TryGetValue(view.WorkId, out var existing) && existing is WorkMapHubView &&
          !(view is WorkMapHubView))
        return;

      _views[view.WorkId] = view;
      view.gameObject.SetActive(false);
    }

    /// <summary>
    /// 只注册工作根。地图节点上的 StandardWorkCenterView 由 WorkMapHubView 转发，不单独占 workId。
    /// </summary>
    private static bool ShouldRegister(WorkCenterView view)
    {
      if (view is StandardWorkCenterView map && !string.IsNullOrEmpty(map.BoundSceneId))
        return false;

      var parent = view.transform.parent;
      if (parent == null) return true;
      return parent.GetComponentInParent<WorkCenterView>(true) == null;
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
      ActivateChain(Active.transform);
      Active.OnActivated(host);
      Active.Refresh(host);
      return true;
    }

    private void ActivateChain(Transform target)
    {
      var stop = transform;
      var stack = new List<Transform>();
      for (var t = target; t != null && t != stop; t = t.parent)
        stack.Add(t);
      for (var i = stack.Count - 1; i >= 0; i--)
        stack[i].gameObject.SetActive(true);
    }
  }
}
