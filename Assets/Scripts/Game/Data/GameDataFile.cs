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
  public class ScavengeDataFile
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
    public string color;
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
    public string displayName;
    public float durationSeconds;
    public int xpReward;
    public int requiredWorkLevel;
    public string description;
    public string thumbColor;
    public LootRow[] loot;
  }

  [Serializable]
  public class LootRow
  {
    public string itemId;
    public float chance;
    public int min;
    public int max;
  }
}
