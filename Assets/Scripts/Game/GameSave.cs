using System;
using System.IO;
using UnityEngine;

namespace UniverIdle.Game
{
  [Serializable]
  public sealed class GameSaveFile
  {
    public int version = 1;
    public long gold;
    public int unlockedPageCount = 1;
    public int unlockedSlotCount = 10;
    public SaveItemRow[] items;
    public SaveWorkRow[] works;
    public SaveSceneRow[] scenes;
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
  public sealed class SaveSceneRow
  {
    public string workId;
    public string sceneId;
    public int level = 1;
    public int xp;
  }

  /// <summary>本地存档（save.dat，内容为 JSON）：金币、背包解锁、物品、工作/地区熟练度。不存进行中的挂机。</summary>
  public static class GameSave
  {
    public const int CurrentVersion = 1;
    public const string FileName = "save.dat";
    private const string LegacyFileName = "save.json";

    public static string FilePath =>
      Path.Combine(Application.persistentDataPath, FileName);

    private static string LegacyFilePath =>
      Path.Combine(Application.persistentDataPath, LegacyFileName);

    public static bool Exists => File.Exists(FilePath) || File.Exists(LegacyFilePath);

    public static bool TryLoad(out GameSaveFile data)
    {
      data = null;
      if (TryRead(FilePath, out data))
        return true;

      if (!TryRead(LegacyFilePath, out data))
        return false;

      Write(data);
      TryDeletePath(LegacyFilePath);
      return true;
    }

    public static void Write(GameSaveFile data)
    {
      if (data == null) return;
      data.version = CurrentVersion;
      try
      {
        File.WriteAllText(FilePath, JsonUtility.ToJson(data, true));
      }
      catch (Exception e)
      {
        Debug.LogWarning($"写入存档失败：{e.Message}");
      }
    }

    public static bool Delete()
    {
      var ok = TryDeletePath(FilePath);
      ok &= TryDeletePath(LegacyFilePath);
      return ok;
    }

    private static bool TryRead(string path, out GameSaveFile data)
    {
      data = null;
      if (!File.Exists(path)) return false;

      try
      {
        var json = File.ReadAllText(path);
        if (string.IsNullOrWhiteSpace(json)) return false;
        data = JsonUtility.FromJson<GameSaveFile>(json);
        return data != null;
      }
      catch (Exception e)
      {
        Debug.LogWarning($"读取存档失败：{e.Message}");
        return false;
      }
    }

    private static bool TryDeletePath(string path)
    {
      if (!File.Exists(path)) return true;
      try
      {
        File.Delete(path);
        return true;
      }
      catch (Exception e)
      {
        Debug.LogWarning($"删除存档失败：{e.Message}");
        return false;
      }
    }
  }
}
