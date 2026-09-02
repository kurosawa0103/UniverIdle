using System.Collections.Generic;
using UnityEngine;

namespace UniverIdle.Game
{
  /// <summary>运行时内容索引；数据来自 StreamingAssets JSON 配表。</summary>
  public static class GameContent
  {
    public const string WorkScavengeId = "scavenge";
    public const string WorkWoodcuttingId = "woodcutting";
    public const string WorkMiningId = "mining";
    public const string WorkMonsterExploreId = "monster_explore";

    private static readonly Dictionary<string, ItemDefinition> Items = new();
    private static readonly Dictionary<string, WorkDefinition> Works = new();
    private static readonly Dictionary<string, WorkActionDefinition> Actions = new();
    private static readonly Dictionary<string, List<WorkActionDefinition>> ActionsByWork = new();

    private static bool _loaded;

    public static void EnsureLoaded()
    {
      if (_loaded) return;
      GameDataLoader.LoadInto(Items, Works, Actions, ActionsByWork);
      _loaded = true;
    }

    static GameContent() => EnsureLoaded();

    public static ItemDefinition GetItem(string id)
    {
      EnsureLoaded();
      return Items.TryGetValue(id, out var item) ? item : null;
    }

    public static WorkDefinition GetWork(string id)
    {
      EnsureLoaded();
      return Works.TryGetValue(id, out var work) ? work : null;
    }

    public static WorkActionDefinition GetAction(string id)
    {
      EnsureLoaded();
      return Actions.TryGetValue(id, out var action) ? action : null;
    }

    public static IReadOnlyList<WorkActionDefinition> GetActionsForWork(string workId)
    {
      EnsureLoaded();
      return ActionsByWork.TryGetValue(workId, out var list) ? list : System.Array.Empty<WorkActionDefinition>();
    }

    /// <summary>按配表顺序将动作按 sceneId 分组；单动作地区仍为一组。</summary>
    public static IReadOnlyList<WorkSceneGroup> GetSceneGroupsForWork(string workId)
    {
      EnsureLoaded();
      var actions = GetActionsForWork(workId);
      if (actions.Count == 0) return System.Array.Empty<WorkSceneGroup>();

      var groups = new List<WorkSceneGroup>();
      var indexByScene = new Dictionary<string, int>();
      var actionsByScene = new Dictionary<string, List<WorkActionDefinition>>();

      foreach (var action in actions)
      {
        if (action == null || string.IsNullOrEmpty(action.SceneId)) continue;
        if (!actionsByScene.TryGetValue(action.SceneId, out var list))
        {
          indexByScene[action.SceneId] = groups.Count;
          list = new List<WorkActionDefinition>();
          actionsByScene[action.SceneId] = list;
          groups.Add(new WorkSceneGroup
          {
            SceneId = action.SceneId,
            SceneName = action.SceneName,
            Actions = list,
          });
        }
        else if (string.IsNullOrEmpty(groups[indexByScene[action.SceneId]].SceneName) &&
                 !string.IsNullOrEmpty(action.SceneName))
        {
          groups[indexByScene[action.SceneId]] = new WorkSceneGroup
          {
            SceneId = action.SceneId,
            SceneName = action.SceneName,
            Actions = list,
          };
        }

        list.Add(action);
      }

      return groups;
    }

#if UNITY_EDITOR
    /// <summary>编辑器改 JSON 后可在菜单强制重载（Play 模式有效）。</summary>
    public static void ReloadForEditor()
    {
      Items.Clear();
      Works.Clear();
      Actions.Clear();
      ActionsByWork.Clear();
      _loaded = false;
      EnsureLoaded();
    }
#endif
  }
}
