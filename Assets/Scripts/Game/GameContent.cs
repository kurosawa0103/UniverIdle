using System.Collections.Generic;
using UnityEngine;

namespace UniverIdle.Game
{
  /// <summary>运行时内容索引；数据来自 StreamingAssets JSON 配表。</summary>
  public static class GameContent
  {
    public const string WorkScavengeId = "scavenge";

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
