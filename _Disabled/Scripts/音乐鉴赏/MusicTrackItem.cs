using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "Music_", menuName = "Game Data/Music Track", order = 2)]
public class MusicTrackItem : ScriptableObject
{
    [Header("基础信息")]
    public string id;
    public string displayName;
    [TextArea]
    public string description;

    [Header("素材")]
    [Tooltip("列表槽位小图标")]
    public Sprite coverSmall;
    [Tooltip("中央展示区大封面")]
    [FormerlySerializedAs("cover")]
    public Sprite coverLarge;
    [Tooltip("播放时切换的背景图；留空则保持当前背景")]
    public Sprite background;
    public AudioClip audioClip;

    [Header("排序")]
    [Tooltip("数值越小越靠前；相同时按 id 排序")]
    public int sortOrder;

    [HideInInspector]
    public int parsedId;
}
