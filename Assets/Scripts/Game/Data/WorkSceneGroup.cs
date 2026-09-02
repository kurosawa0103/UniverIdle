using System.Collections.Generic;

namespace UniverIdle.Game
{
  /// <summary>同一 sceneId 下的动作集合；横幅显示 SceneName，动作卡显示各 Spot。</summary>
  public sealed class WorkSceneGroup
  {
    public string SceneId { get; set; }
    public string SceneName { get; set; }
    public IReadOnlyList<WorkActionDefinition> Actions { get; set; }

    public int MinRequiredWorkLevel
    {
      get
      {
        var min = int.MaxValue;
        if (Actions == null) return 1;
        foreach (var action in Actions)
        {
          if (action == null) continue;
          var level = action.RequiredWorkLevel <= 0 ? 1 : action.RequiredWorkLevel;
          if (level < min) min = level;
        }
        return min == int.MaxValue ? 1 : min;
      }
    }
  }
}
