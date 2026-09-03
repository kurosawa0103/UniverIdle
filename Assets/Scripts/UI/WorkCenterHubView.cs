using UnityEngine;

namespace UniverIdle.UI
{
  /// <summary>
  /// 工作根（如 WorkView_scavenge）：向 Host 注册 workId。
  /// 各地图节点上的 <see cref="StandardWorkCenterView"/> 由本组件转发生命周期。
  /// </summary>
  public sealed class WorkCenterHubView : WorkCenterView
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

    private void Awake()
    {
      if (detailPanel == null)
        detailPanel = GetComponentInChildren<ScavengeDetailView>(true);
      EnsureMaps();
    }

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
      for (var i = 0; i < _maps.Length; i++)
        _maps[i]?.OnActivated(host);
    }

    public override void OnDeactivated()
    {
      EnsureMaps();
      for (var i = 0; i < _maps.Length; i++)
        _maps[i]?.OnDeactivated();
    }

    public override void Refresh(MainUIController host)
    {
      EnsureMaps();
      for (var i = 0; i < _maps.Length; i++)
        _maps[i]?.Refresh(host);
    }

    public override void TickProgress(MainUIController host)
    {
      ActiveMap?.TickProgress(host);
    }

    public override void OnActionCompleted(MainUIController host, Game.ActionCompleteResult result)
    {
      ActiveMap?.OnActionCompleted(host, result);
    }

    public override void OnRunnerActionStopped(MainUIController host, Game.WorkActionDefinition action)
    {
      EnsureMaps();
      for (var i = 0; i < _maps.Length; i++)
        _maps[i]?.OnRunnerActionStopped(host, action);
    }

    public override void OnInventoryChanged(MainUIController host)
    {
      EnsureMaps();
      for (var i = 0; i < _maps.Length; i++)
        _maps[i]?.OnInventoryChanged(host);
    }

    public override void OnWorkOrSceneChanged(MainUIController host)
    {
      EnsureMaps();
      for (var i = 0; i < _maps.Length; i++)
        _maps[i]?.OnWorkOrSceneChanged(host);
    }

    private void EnsureMaps()
    {
      if (_maps != null) return;
      _maps = GetComponentsInChildren<StandardWorkCenterView>(true);
    }

    private void BindDetails()
    {
      EnsureMaps();
      for (var i = 0; i < _maps.Length; i++)
        _maps[i]?.BindSharedDetail(detailPanel);
    }
  }
}
