using UnityEngine;
using Steamworks;

public class SteamTest : MonoBehaviour
{
    void Start()
    {
        if (SteamManager.Initialized)
        {
            Debug.Log("✅ Steam 初始化成功，玩家：" + SteamFriends.GetPersonaName());
        }
        else
        {
            Debug.LogError("❌ Steam 初始化失败！");
        }
    }
}
