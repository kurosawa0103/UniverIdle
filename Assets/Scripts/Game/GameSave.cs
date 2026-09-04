using System;
using System.IO;
using UnityEngine;

namespace UniverIdle.Game
{
  [Serializable]
  public sealed class GameSaveFile
  {
    public int version = 2;
    public long gold;
    public int unlockedPageCount = 1;
    public int unlockedSlotCount = 10;
    public SaveItemRow[] items;
    public SaveWorkRow[] works;
    /// <summary>各动作独立熟练度。</summary>
    public SaveActionMasteryRow[] actionMasteries;
  }

  [Serializable]
  public sealed class SaveItemRow
  {
    public string id;
    public long count;
  }

  [Serializable]
  public sealed class SaveWorkRow
  {
    public string id;
    public int level = 1;
    public int xp;
  }

  [Serializable]
  public sealed class SaveActionMasteryRow
  {
    public string actionId;
    public int level = 1;
    public int xp;
  }

  /// <summary>本地存档（save.dat，内容为 JSON）：金币、背包解锁、物品、工作总等级 / 动作熟练度。不存进行中的挂机。</summary>
  public static class GameSave
  {
    public const int CurrentVersion = 2;
    public const string FileName = "save.dat";

    public static string FilePath =>
      Path.Combine(Application.persistentDataPath, FileName);

    public static bool Exists => File.Exists(FilePath);

    public static bool TryLoad(out GameSaveFile data)
    {
      data = null;
      if (!Exists) return false;
      try
      {
        var json = File.ReadAllText(FilePath);
        data = JsonUtility.FromJson<GameSaveFile>(json);
        return data != null;
      }
      catch (Exception ex)
      {
        Debug.LogWarning("[UniverIdle] 读取存档失败：" + ex.Message);
        data = null;
        return false;
      }
    }

    public static void Write(GameSaveFile data)
    {
      if (data == null) return;
      data.version = CurrentVersion;
      try
      {
        File.WriteAllText(FilePath, JsonUtility.ToJson(data, true));
      }
      catch (Exception ex)
      {
        Debug.LogWarning("[UniverIdle] 写入存档失败：" + ex.Message);
      }
    }

    public static bool Delete()
    {
      try
      {
        if (!Exists) return false;
        File.Delete(FilePath);
        return true;
      }
      catch (Exception ex)
      {
        Debug.LogWarning("[UniverIdle] 删除存档失败：" + ex.Message);
        return false;
      }
    }
  }
}
