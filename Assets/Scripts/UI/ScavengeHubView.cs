using System.Collections.Generic;
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
    [Tooltip("各地图节点上的 StandardWorkCenterView；一键绑定可补齐。")]
    [SerializeField] private List<StandardWorkCenterView> maps = new();

    private bool _wired;

    public StandardWorkCenterView ActiveMap
    {
      get
      {
        StandardWorkCenterView first = null;
        for (var i = 0; i < maps.Count; i++)
        {
          var map = maps[i];
          if (map == null) continue;
          if (first == null) first = map;
          if (map.isActiveAndEnabled)
            return map;
        }
        return first;
      }
    }

    public override void Wire(MainUIController host)
    {
      if (_wired) return;
      _wired = true;
      BindDetails();
      for (var i = 0; i < maps.Count; i++)
        maps[i]?.Wire(host);
    }

    public override void OnActivated(MainUIController host)
    {
      BindDetails();
      ActiveMap?.OnActivated(host);
    }

    public override void OnDeactivated()
    {
      for (var i = 0; i < maps.Count; i++)
        maps[i]?.OnDeactivated();
    }

    public override void Refresh(MainUIController host) =>
      ActiveMap?.Refresh(host);

    public override void TickProgress(MainUIController host) =>
      ActiveMap?.TickProgress(host);

    public override void OnActionCompleted(MainUIController host, ActionCompleteResult result) =>
      ActiveMap?.OnActionCompleted(host, result);

    public override void OnRunnerActionStopped(MainUIController host, WorkActionDefinition action)
    {
      for (var i = 0; i < maps.Count; i++)
        maps[i]?.OnRunnerActionStopped(host, action);
    }

    public override void OnInventoryChanged(MainUIController host) =>
      ActiveMap?.OnInventoryChanged(host);

    public override void OnWorkOrMasteryChanged(MainUIController host) =>
      ActiveMap?.OnWorkOrMasteryChanged(host);

    private void BindDetails()
    {
      for (var i = 0; i < maps.Count; i++)
        maps[i]?.BindScavengeDetail(detailPanel);
    }
  }
}
