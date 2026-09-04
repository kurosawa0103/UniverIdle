using UniverIdle.Game;
using UnityEngine;

namespace UniverIdle.UI
{
  /// <summary>
  /// 拾荒工作根（WorkView_scavenge）：向 Host 注册 workId=scavenge。
  /// 各地图节点上的 <see cref="StandardWorkCenterView"/> 由本组件转发生命周期；
  /// 详情 <see cref="ScavengeDetailView"/> 只属于拾荒，由本 Hub 分发给当前地图。
  /// </summary>
  public sealed class ScavengeHubView : WorkCenterView
  {
    [SerializeField] private ScavengeDetailView detailPanel;

    private StandardWorkCenterView[] _maps;
    private bool _wired;

    public StandardWorkCenterView ActiveMap
    {
      get
      {
        EnsureMaps();
        StandardWorkCenterView first = null;
        for (var i = 0; i < _maps.Length; i++)
        {
          var map = _maps[i];
          if (map == null) continue;
          if (first == null) first = map;
          if (map.isActiveAndEnabled)
            return map;
        }
        return first;
      }
    }

    private void Awake() => EnsureMaps();

    public override void Wire(MainUIController host)
    {
      if (_wired) return;
      _wired = true;
      EnsureMaps();
      BindDetails();
      for (var i = 0; i < _maps.Length; i++)
        _maps[i]?.Wire(host);
    }

    public override void OnActivated(MainUIController host)
    {
      EnsureMaps();
      BindDetails();
      ActiveMap?.OnActivated(host);
    }

    public override void OnDeactivated()
    {
      EnsureMaps();
      for (var i = 0; i < _maps.Length; i++)
        _maps[i]?.OnDeactivated();
    }

    public override void Refresh(MainUIController host)
    {
      ActiveMap?.Refresh(host);
    }

    public override void TickProgress(MainUIController host)
    {
      ActiveMap?.TickProgress(host);
    }

    public override void OnActionCompleted(MainUIController host, ActionCompleteResult result)
    {
      ActiveMap?.OnActionCompleted(host, result);
    }

    public override void OnRunnerActionStopped(MainUIController host, WorkActionDefinition action)
    {
      EnsureMaps();
      for (var i = 0; i < _maps.Length; i++)
        _maps[i]?.OnRunnerActionStopped(host, action);
    }

    public override void OnInventoryChanged(MainUIController host) =>
      ActiveMap?.OnInventoryChanged(host);

    public override void OnWorkOrMasteryChanged(MainUIController host) =>
      ActiveMap?.OnWorkOrMasteryChanged(host);

    private void EnsureMaps()
    {
      if (_maps != null) return;
      _maps = GetComponentsInChildren<StandardWorkCenterView>(true);
    }

    private void BindDetails()
    {
      EnsureMaps();
      for (var i = 0; i < _maps.Length; i++)
        _maps[i]?.BindScavengeDetail(detailPanel);
    }
  }
}
