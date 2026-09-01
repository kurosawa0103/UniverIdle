using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 通用按钮点击音效。用法与 <see cref="CollectibleSlot"/> 的音效一致：
/// 必须与 <see cref="Button"/> 挂在<strong>同一 GameObject</strong> 上，在 Inspector 拖入 Sfx Clip 即可。
/// </summary>
[RequireComponent(typeof(Button))]
public class ButtonClickSound : MonoBehaviour
{
    [Header("音效")]
    [Tooltip("点击时播放，直接拖入 AudioClip；留空则不播")]
    public AudioClip sfxClip;
    [Range(0f, 1f)]
    [Tooltip("PlayOneShot 音量系数，会乘以该物体上 AudioSource 的 Volume")]
    public float volume = 1f;

    private Button btn;
    private AudioSource audioSource;

    private void Awake()
    {
        btn = GetComponent<Button>();
        if (btn == null) return;

        btn.onClick.AddListener(PlaySfx);
    }

    private void OnDestroy()
    {
        if (btn != null)
            btn.onClick.RemoveListener(PlaySfx);
    }

    /// <summary>与 CollectibleSlot.PlayClickSfx 相同逻辑。</summary>
    private void PlaySfx()
    {
        if (sfxClip == null || volume <= 0f) return;

        GameObject host = btn != null ? btn.gameObject : gameObject;
        if (audioSource != null && audioSource.gameObject != host)
            audioSource = null;

        if (audioSource == null)
        {
            audioSource = host.GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = host.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            audioSource.mute = false;
            audioSource.enabled = true;
        }

        audioSource.PlayOneShot(sfxClip, volume);
    }
}
