using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace UniverIdle.Game
{
  /// <summary>从 StreamingAssets 加载 JSON 配表并构建运行时索引。</summary>
  public static class GameDataLoader
  {
    public static void LoadInto(
      IDictionary<string, ItemDefinition> items,
      IDictionary<string, WorkDefinition> works,
      IDictionary<string, WorkActionDefinition> actions,
      IDictionary<string, List<WorkActionDefinition>> actionsByWork)
    {
      items.Clear();
      works.Clear();
      actions.Clear();
      actionsByWork.Clear();

      var itemsData = LoadItemsFile();
      RegisterItems(itemsData, items);
      RegisterWorkContent(LoadScavengeFile(), works, actions, actionsByWork, items);
      RegisterWorkContent(LoadWoodcuttingFile(), works, actions, actionsByWork, items);
      RegisterWorkContent(LoadMiningFile(), works, actions, actionsByWork, items);
      RegisterWorkContent(LoadMonsterExploreFile(), works, actions, actionsByWork, items);
    }

    public static ItemsDataFile LoadItemsFile() =>
      LoadJsonFile<ItemsDataFile>(GameDataPaths.ItemsRelativePath);

    public static WorkContentDataFile LoadScavengeFile() =>
      LoadJsonFile<WorkContentDataFile>(GameDataPaths.ScavengeRelativePath);

    public static WorkContentDataFile LoadWoodcuttingFile() =>
      LoadJsonFile<WorkContentDataFile>(GameDataPaths.WoodcuttingRelativePath);

    public static WorkContentDataFile LoadMiningFile() =>
      LoadJsonFile<WorkContentDataFile>(GameDataPaths.MiningRelativePath);

    public static WorkContentDataFile LoadMonsterExploreFile() =>
      LoadJsonFile<WorkContentDataFile>(GameDataPaths.MonsterExploreRelativePath);

    /// <summary>编辑器合并导出用：JSON 缺失时返回空表，不抛错。</summary>
    public static ItemsDataFile LoadItemsFileIfPresent() =>
      LoadJsonFileIfPresent<ItemsDataFile>(GameDataPaths.ItemsRelativePath) ?? new ItemsDataFile { version = 3 };

    public static WorkContentDataFile LoadScavengeFileIfPresent() =>
      LoadJsonFileIfPresent<WorkContentDataFile>(GameDataPaths.ScavengeRelativePath) ?? new WorkContentDataFile { version = 3 };

    public static WorkContentDataFile LoadWoodcuttingFileIfPresent() =>
      LoadJsonFileIfPresent<WorkContentDataFile>(GameDataPaths.WoodcuttingRelativePath) ?? new WorkContentDataFile { version = 3 };

    public static WorkContentDataFile LoadMiningFileIfPresent() =>
      LoadJsonFileIfPresent<WorkContentDataFile>(GameDataPaths.MiningRelativePath) ?? new WorkContentDataFile { version = 3 };

    public static WorkContentDataFile LoadMonsterExploreFileIfPresent() =>
      LoadJsonFileIfPresent<WorkContentDataFile>(GameDataPaths.MonsterExploreRelativePath) ?? new WorkContentDataFile { version = 3 };

    public static void Validate()
    {
      var items = new Dictionary<string, ItemDefinition>();
      var works = new Dictionary<string, WorkDefinition>();
      var actions = new Dictionary<string, WorkActionDefinition>();
      var actionsByWork = new Dictionary<string, List<WorkActionDefinition>>();
      LoadInto(items, works, actions, actionsByWork);
    }

    private static T LoadJsonFile<T>(string relativePath) where T : class
    {
      var data = LoadJsonFileIfPresent<T>(relativePath);
      if (data == null)
      {
        var path = Path.Combine(Application.streamingAssetsPath, relativePath);
        throw new FileNotFoundException($"[UniverIdle] 找不到配表：{path}（请确认 Assets/StreamingAssets/{relativePath} 存在）");
      }
      return data;
    }

    private static T LoadJsonFileIfPresent<T>(string relativePath) where T : class
    {
      var path = Path.Combine(Application.streamingAssetsPath, relativePath);
      if (!File.Exists(path))
        return null;

      var json = File.ReadAllText(path);
      var data = JsonUtility.FromJson<T>(json);
      if (data == null)
        throw new InvalidDataException($"[UniverIdle] 配表 JSON 解析失败：{relativePath}");
      return data;
    }

    private static void RegisterItems(ItemsDataFile data, IDictionary<string, ItemDefinition> items)
    {
      if (data?.items == null) return;
      foreach (var row in data.items)
      {
        if (string.IsNullOrEmpty(row.id))
        {
          Debug.LogWarning("[UniverIdle] 配表 items 存在空 id，已跳过。");
          continue;
        }
        if (items.ContainsKey(row.id))
          Debug.LogWarning($"[UniverIdle] 重复物品 id：{row.id}");

        items[row.id] = new ItemDefinition(
          row.id,
          row.name,
          GameColorUtility.Parse(row.color, Color.gray),
          row.description);
      }
    }

    private static void RegisterWorkContent(
      WorkContentDataFile data,
      IDictionary<string, WorkDefinition> works,
      IDictionary<string, WorkActionDefinition> actions,
      IDictionary<string, List<WorkActionDefinition>> actionsByWork,
      IDictionary<string, ItemDefinition> items)
    {
      if (data == null) return;
      RegisterWorks(data, works);
      RegisterActions(data, actions, actionsByWork, items, works);
    }

    private static void RegisterWorks(WorkContentDataFile data, IDictionary<string, WorkDefinition> works)
    {
      if (data?.works == null) return;
      foreach (var row in data.works)
      {
        if (string.IsNullOrEmpty(row.id)) continue;
        if (works.ContainsKey(row.id))
          Debug.LogWarning($"[UniverIdle] 重复工作 id：{row.id}");

        works[row.id] = new WorkDefinition
        {
          Id = row.id,
          DisplayName = row.name,
          LocationName = row.locationName,
          IconColor = GameColorUtility.Parse(row.iconColor, Color.white),
          XpBase = row.xpBase > 0 ? row.xpBase : 40,
          XpPerLevel = row.xpPerLevel > 0 ? row.xpPerLevel : 20,
          SceneXpBase = row.sceneXpBase,
          SceneXpPerLevel = row.sceneXpPerLevel,
          GrantWorkXp = row.grantWorkXp != 0,
          GrantSceneXp = row.grantSceneXp != 0,
        };
      }
    }

    private static void RegisterActions(
      WorkContentDataFile data,
      IDictionary<string, WorkActionDefinition> actions,
      IDictionary<string, List<WorkActionDefinition>> actionsByWork,
      IDictionary<string, ItemDefinition> items,
      IDictionary<string, WorkDefinition> works)
    {
      if (data?.actions == null) return;
      foreach (var row in data.actions)
      {
        if (string.IsNullOrEmpty(row.id)) continue;
        if (actions.ContainsKey(row.id))
          Debug.LogWarning($"[UniverIdle] 重复动作 id：{row.id}");

        if (!works.ContainsKey(row.workId))
          Debug.LogWarning($"[UniverIdle] 动作 {row.id} 引用未知工作：{row.workId}");

        var loot = BuildLoot(row, items);
        var action = new WorkActionDefinition
        {
          Id = row.id,
          WorkId = row.workId,
          SceneId = row.sceneId,
          SceneName = row.sceneName,
          SpotName = row.spotName,
          DisplayName = row.displayName,
          DurationSeconds = row.durationSeconds,
          XpReward = row.xpReward,
          RequiredWorkLevel = row.requiredWorkLevel <= 0 ? 1 : row.requiredWorkLevel,
          Description = row.description,
          ThumbColor = GameColorUtility.Parse(row.thumbColor, Color.white),
          CostItemId = string.IsNullOrWhiteSpace(row.costItemId) ? null : row.costItemId.Trim(),
          CostAmount = row.costAmount > 0 ? row.costAmount : 0,
          LootTable = loot,
        };

        actions[row.id] = action;
        if (!actionsByWork.TryGetValue(row.workId, out var list))
        {
          list = new List<WorkActionDefinition>();
          actionsByWork[row.workId] = list;
        }
        list.Add(action);
      }
    }

    private static List<LootEntry> BuildLoot(ActionRow row, IDictionary<string, ItemDefinition> items)
    {
      var loot = new List<LootEntry>();
      if (row.loot == null) return loot;

      foreach (var entry in row.loot)
      {
        if (string.IsNullOrEmpty(entry.itemId)) continue;
        if (!items.ContainsKey(entry.itemId))
          Debug.LogWarning($"[UniverIdle] 动作 {row.id} 掉落引用未知物品：{entry.itemId}");

        var min = entry.min <= 0 ? 1 : entry.min;
        var max = entry.max <= 0 ? min : entry.max;
        if (max < min) max = min;
        loot.Add(new LootEntry(entry.itemId, entry.chance, min, max));
      }
      return loot;
    }
  }
}
