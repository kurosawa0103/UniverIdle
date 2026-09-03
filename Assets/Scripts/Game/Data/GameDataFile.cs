using System;
using UnityEngine;

namespace UniverIdle.Game
{
  [Serializable]
  public class ItemsDataFile
  {
    public int version = 3;
    public ItemRow[] items;
  }

  [Serializable]
  public class WorkContentDataFile
  {
    public int version = 3;
    public WorkRow[] works;
    public ActionRow[] actions;
  }

  [Serializable]
  public class ItemRow
  {
    public string id;
    public string name;
    /// <summary>分类：junk / wood / ore / monster / herb / tool / relic / system。</summary>
    public string category;
    /// <summary>图标文件名或 Resources 路径（无扩展名）；空=ItemIcon/item_{id}；-=无图标。</summary>
    public string icon;
    public string description;
  }

  [Serializable]
  public class WorkRow
  {
    public string id;
    public string name;
    public string locationName;
    public string iconColor;
    public int xpBase;
    public int xpPerLevel;
    public int sceneXpBase;
    public int sceneXpPerLevel;
    public int grantWorkXp = 1;
    public int grantSceneXp = 1;
  }

  [Serializable]
  public class ActionRow
  {
    public string id;
    public string workId;
    public string sceneId;
    public string sceneName;
    /// <summary>子地点名，如「老王家」；横幅用 sceneName，卡面用 spotName。</summary>
    public string spotName;
    public string displayName;
    public float durationSeconds;
    public int xpReward;
    public int requiredWorkLevel;
    public string description;
    /// <summary>缩略图文件名或 Resources 路径（无扩展名）；空=ActionImage/{actionId}；-=无图。</summary>
    public string thumbImage;
    public string costItemId;
    public int costAmount;
    /// <summary>独立掷骰：命中后随机 goldMin～goldMax 枚系统金币（与 loot 无关）。</summary>
    public float goldChance;
    public int goldMin;
    public int goldMax;
    public LootRow[] loot;
  }

  [Serializable]
  public class LootRow
  {
    /// <summary>道具 id；<c>_empty</c> 表示一无所获占位。</summary>
    public string itemId;
    /// <summary>相对权重（不必加和为 1）；每次完成动作按权重随机 1 种。</summary>
    public float chance;
    public int min;
    public int max;
  }
}
