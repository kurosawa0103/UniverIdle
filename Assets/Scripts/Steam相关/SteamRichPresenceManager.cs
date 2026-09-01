using UnityEngine;
using Steamworks;
using I2.Loc;

/// <summary>
/// Steam 游玩状态（Rich Presence）管理。
/// 写入 status=I2文案，steam_display=#StatusDisplay（Partner VDF 中为 %status%）。
/// 需上传并 Publish：Steam/richpresence_localization.vdf
/// </summary>
public class SteamRichPresenceManager : MonoBehaviour
{
    public const string FallbackDisplayToken = "#StatusDisplay";

    private static SteamRichPresenceManager s_instance;
    private static string s_currentStatusTerm;
    private static bool s_hasStatus;

#if UNITY_2019_3_OR_NEWER
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        s_instance = null;
        s_currentStatusTerm = null;
        s_hasStatus = false;
    }
#endif

    public static SteamRichPresenceManager Instance
    {
        get
        {
            if (s_instance == null)
            {
                s_instance = FindObjectOfType<SteamRichPresenceManager>();
                if (s_instance == null)
                {
                    var go = new GameObject("SteamRichPresenceManager");
                    s_instance = go.AddComponent<SteamRichPresenceManager>();
                }
            }
            return s_instance;
        }
    }

    public static string CurrentStatusTerm => s_hasStatus ? s_currentStatusTerm : null;

    private void Awake()
    {
        if (s_instance != null && s_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        s_instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        LocalizationManager.OnLocalizeEvent += OnLanguageChanged;
        if (s_hasStatus)
            ApplyCurrentStatus();
    }

    private void OnDisable()
    {
        LocalizationManager.OnLocalizeEvent -= OnLanguageChanged;
    }

    private void OnDestroy()
    {
        if (s_instance == this)
            s_instance = null;
    }

    private void OnLanguageChanged()
    {
        if (s_hasStatus)
            ApplyCurrentStatus();
    }

    public bool SetPlayStatus(string i2TermKey)
    {
        if (string.IsNullOrEmpty(i2TermKey))
        {
            Debug.LogWarning("[SteamRichPresence] Term Key 为空，无法设置游玩状态");
            return false;
        }

        s_currentStatusTerm = i2TermKey;
        s_hasStatus = true;
        return ApplyCurrentStatus();
    }

    public void ClearPlayStatus()
    {
        s_hasStatus = false;
        s_currentStatusTerm = null;

        if (!SteamManager.Initialized || !CallbackDispatcher.IsInitialized)
            return;

        SteamFriends.ClearRichPresence();
        Debug.Log("[SteamRichPresence] 已清除游玩状态");
    }

    private bool ApplyCurrentStatus()
    {
        if (!s_hasStatus || string.IsNullOrEmpty(s_currentStatusTerm))
            return false;

        if (!SteamManager.Initialized || !CallbackDispatcher.IsInitialized)
        {
            Debug.LogWarning("[SteamRichPresence] Steam 未初始化，无法设置游玩状态");
            return false;
        }

        string text = GetLocalizedString(s_currentStatusTerm);
        if (string.IsNullOrEmpty(text))
            text = s_currentStatusTerm;

        int maxLen = Constants.k_cchMaxRichPresenceValueLength - 1;
        if (text.Length > maxLen)
            text = text.Substring(0, maxLen);

        // 统一用 #StatusDisplay → %status%，由游戏写入的 I2 文案直接显示在好友列表。
        // 个别 #Status_Xxx token 仍保留在 VDF 中作备用；主路径不依赖它们是否上传成功。
        const string displayToken = FallbackDisplayToken;

        bool statusOk = SteamFriends.SetRichPresence("status", text);
        bool displayOk = SteamFriends.SetRichPresence("steam_display", displayToken);

        if (statusOk && displayOk)
        {
            // 注意：对本机 GetFriendRichPresence("steam_display") 经常读回空字符串，
            // 这不代表设置失败。是否显示只能看 Partner 测试页 / 好友视角。
            Debug.Log($"[SteamRichPresence] 已设置: term={s_currentStatusTerm}, text={text}, display={displayToken}");
            return true;
        }

        Debug.LogWarning($"[SteamRichPresence] SetRichPresence 失败: statusOk={statusOk}, displayOk={displayOk}, term={s_currentStatusTerm}");
        return false;
    }

    private static string GetLocalizedString(string termOrText)
    {
        if (string.IsNullOrEmpty(termOrText))
            return "";

        string translated = LocalizationManager.GetTranslation(termOrText);
        if (!string.IsNullOrEmpty(translated) && translated != termOrText)
            return translated;

        return termOrText;
    }
}
