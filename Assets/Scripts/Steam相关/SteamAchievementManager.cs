using UnityEngine;
using Steamworks;

public class SteamAchievementManager : MonoBehaviour
{
    private void Start()
    {
        if (SteamManager.Initialized)
        {
            Debug.Log("Steam 已初始化，准备检测成就系统。");
        }
        else
        {
            Debug.LogWarning("Steam 未初始化，成就功能将不可用。");
        }
    }

    public bool UnlockAchievement(string achievementID)
    {
        if (string.IsNullOrEmpty(achievementID))
            return false;

        if (!SteamManager.Initialized)
        {
            Debug.LogWarning("Steam 未初始化，无法解锁");
            return false;
        }

        if (IsAchievementUnlocked(achievementID))
            return true;

        Debug.Log($"尝试解锁成就：{achievementID}");
        bool success = SteamUserStats.SetAchievement(achievementID);
        Debug.Log("SetAchievement 返回：" + success);

        if (success)
            SteamUserStats.StoreStats();

        return success;
    }

    public void RevokeAchievement(string achievementID)
    {
        if (!SteamManager.Initialized)
        {
            Debug.LogWarning("Steam 未初始化，无法撤销成就");
            return;
        }

        SteamUserStats.ClearAchievement(achievementID);
        SteamUserStats.StoreStats(); // 提交更改
        Debug.Log($"已撤销成就：{achievementID}");
    }
    public bool IsAchievementUnlocked(string achievementID)
    {
        if (!SteamManager.Initialized) return false;

        bool achieved;
        SteamUserStats.GetAchievement(achievementID, out achieved);
        return achieved;
    }
}
