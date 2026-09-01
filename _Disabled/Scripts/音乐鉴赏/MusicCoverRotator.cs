using UnityEngine;

/// <summary>
/// 音乐封面旋转：绑定 AudioSource 后，播放时持续旋转，停止时静止。
/// 挂在中央封面 Image 的 RectTransform 上即可。
/// </summary>
public class MusicCoverRotator : MonoBehaviour
{
    [Header("旋转")]
    [Tooltip("每秒旋转角度，正值为逆时针")]
    public float rotateSpeed = 36f;

    [Header("播放源")]
    [Tooltip("留空时不旋转；通常绑定 MusicPageView 的 bgmAudioSource")]
    public AudioSource audioSource;

    [Tooltip("使用 unscaledDeltaTime，不受 Time.timeScale 影响")]
    public bool useUnscaledTime = true;

    private void Update()
    {
        if (audioSource == null || !audioSource.isPlaying)
            return;

        float delta = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        transform.Rotate(0f, 0f, rotateSpeed * delta, Space.Self);
    }
}
