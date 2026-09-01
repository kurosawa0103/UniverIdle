using System;
using System.IO;
using UnityEngine;

// 可序列化的游戏数据类
[Serializable]
public class GameData
{
    public string currentEventPath;
}


// 游戏数据处理接口
public interface IGameDataHandler
{
    void SaveGameData(GameData gameData); // 保存游戏数据
    GameData LoadGameData();              // 获取游戏数据

    // 新增：设置和获取当前事件路径
    void SetCurrentEventPath(string path);
    string GetCurrentEventPath();
    void ClearGameData(); // 清空游戏数据
}



// 实现游戏数据保存和加载的类
public class GameDataToJson : IGameDataHandler
{
    private const string jsonFileName = "game_data.json"; // JSON 文件名

    private string GetJsonFilePath()
    {
        string persistentDataPath = Application.persistentDataPath; // 获取持久化数据路径
        return Path.Combine(persistentDataPath, jsonFileName); // 返回完整的 JSON 文件路径
    }

    public void SaveGameData(GameData gameData)
    {
        string filePath = GetJsonFilePath(); // 获取 JSON 文件路径

        try
        {
            string json = JsonUtility.ToJson(gameData, true); // 转换为 JSON 字符串
            File.WriteAllText(filePath, json); // 将 JSON 写入文件
            Debug.Log("游戏数据保存至: " + filePath); // 日志记录成功消息
        }
        catch (Exception e)
        {
            Debug.LogError($"保存游戏数据出错: {e.Message}"); // 记录保存失败的错误消息
        }
    }

    public GameData LoadGameData()
    {
        string filePath = GetJsonFilePath(); // 获取 JSON 文件路径

        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath); // 读取 JSON 文件内容
            GameData gameData = JsonUtility.FromJson<GameData>(json); // 将 JSON 转换为 GameData 对象

            return gameData; // 返回游戏数据对象
        }
        else
        {
            Debug.LogWarning("未找到游戏数据文件，返回默认的 GameData 对象。"); // 日志记录文件未找到的警告消息
            return new GameData(); // 如果文件不存在，返回默认的 GameData 对象
        }
    }

    // 设置当前事件路径
    public void SetCurrentEventPath(string path)
    {
        GameData gameData = LoadGameData();
        gameData.currentEventPath = path;
        SaveGameData(gameData);
    }

    public string GetCurrentEventPath()
    {
        GameData gameData = LoadGameData();
        return gameData.currentEventPath;
    }
    public void ClearGameData()
    {
        string filePath = GetJsonFilePath();
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            Debug.Log("已清空游戏数据: " + filePath);
        }
        else
        {
            Debug.LogWarning("尝试清空游戏数据，但文件不存在: " + filePath);
        }
    }

}
