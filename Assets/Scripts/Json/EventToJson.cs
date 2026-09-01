using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class EventState
{
    public string eventID;  // 事件的唯一标识
    public string eventState; // 事件的状态，例如 "completed" 或 "in_progress"
}

[Serializable]
public class EventDataList
{
    public List<EventState> eventStates = new List<EventState>(); // 存储所有事件的状态
    public string currentEventAddress;  // 当前进行中的事件路径
}

public interface IEventHandler
{
    void SaveEventState(List<EventState> eventStates);  // 保存事件状态
    List<EventState> LoadEventState();                   // 加载事件状态
    void SaveEventState(string eventName, string eventState);
    void SaveCurrentEventAddress(string currentEventAddress);  // 保存当前事件地址
    string GetCurrentEventAddress();                       // 获取当前事件地址
    void ClearEventState();                              // 清除事件状态
    bool IsEventCompleted(string eventName);
}

// 实现事件数据保存和加载的类
public class EventToJson : MonoBehaviour, IEventHandler
{
    private const string jsonFileName = "event_data.json"; // 事件状态 JSON 文件名

    // 获取事件数据文件路径
    private string GetJsonFilePath()
    {
        string persistentDataPath = Application.persistentDataPath;
        return Path.Combine(persistentDataPath, jsonFileName);
    }

    // 保存事件状态到 JSON 文件
    public void SaveEventState(List<EventState> eventStates)
    {
        string filePath = GetJsonFilePath();

        try
        {
            EventDataList eventDataList = LoadEventDataList();  // 获取现有的事件数据
            eventDataList.eventStates = eventStates;  // 更新事件状态

            string json = JsonUtility.ToJson(eventDataList, true);
            File.WriteAllText(filePath, json);
            Debug.Log("事件数据保存至: " + filePath);
        }
        catch (Exception e)
        {
            Debug.LogError($"保存事件数据出错: {e.Message}");
        }
    }

    // 保存单个事件状态
    public void SaveEventState(string eventName, string eventState)
    {
        List<EventState> eventStates = LoadEventState();  // 获取当前的事件状态

        // 查找事件状态
        EventState existingEventState = eventStates.Find(e => e.eventID == eventName);

        if (existingEventState == null)
        {
            // 如果找不到该事件，新增一个事件状态
            existingEventState = new EventState { eventID = eventName, eventState = eventState };
            eventStates.Add(existingEventState);
        }
        else
        {
            // 如果事件已存在，更新状态
            existingEventState.eventState = eventState;
        }

        // 保存更新后的事件状态
        SaveEventState(eventStates);
    }

    // 获取事件数据
    private EventDataList LoadEventDataList()
    {
        string filePath = GetJsonFilePath();

        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            EventDataList eventDataList = JsonUtility.FromJson<EventDataList>(json);
            return eventDataList;
        }
        else
        {
            // 如果文件不存在，返回一个新的 EventDataList
            return new EventDataList();
        }
    }

    // 加载 JSON 文件中的事件状态数据
    public List<EventState> LoadEventState()
    {
        return LoadEventDataList().eventStates;
    }

    // 保存当前事件路径
    public void SaveCurrentEventAddress(string currentEventAddress)
    {
        string filePath = GetJsonFilePath();

        try
        {
            EventDataList eventDataList = LoadEventDataList();  // 获取现有的事件数据
            eventDataList.currentEventAddress = currentEventAddress;  // 更新当前事件路径

            string json = JsonUtility.ToJson(eventDataList, true);
            File.WriteAllText(filePath, json);  // 保存更新后的数据
            Debug.Log("当前事件路径保存至: " + filePath);
        }
        catch (Exception e)
        {
            Debug.LogError($"保存当前事件路径时出错: {e.Message}");
        }
    }

    // 获取当前事件路径
    public string GetCurrentEventAddress()
    {
        return LoadEventDataList().currentEventAddress;
    }

    // 清除所有事件状态数据
    public void ClearEventState()
    {
        string filePath = GetJsonFilePath();

        if (File.Exists(filePath))
        {
            try
            {
                File.Delete(filePath);
                Debug.Log("事件数据已清除，文件已删除: " + filePath);
            }
            catch (Exception e)
            {
                Debug.LogError($"清除事件数据出错: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning("未找到事件数据文件，无法清除数据。");
        }
    }

    // 根据事件名称检查指定事件是否完成
    public bool IsEventCompleted(string eventName)
    {
        List<EventState> eventStates = LoadEventState();  // 获取所有事件的状态

        // 查找指定事件的状态
        EventState eventState = eventStates.Find(e => e.eventID == eventName);

        // 如果事件存在，返回其状态；如果不存在，返回 false
        return eventState != null && eventState.eventState == "completed";
    }
}
