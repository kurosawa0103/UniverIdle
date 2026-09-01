using System.Collections;
using DesktopPet.Settings;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Audio;

namespace DesktopPet.Audio
{
    /// <summary>桌宠 BGM：场景默认曲 + 覆盖（如收音机），同 owner 才能清覆盖。</summary>
    [DisallowMultipleComponent]
    public sealed class DesktopPetBgmPlayer : MonoBehaviour
    {
        [Title("BGM")]
        [LabelText("默认曲")]
        [SerializeField]
        private AudioClip defaultClip;

        [LabelText("AudioSource")]
        [SerializeField]
        private AudioSource audioSource;

        [LabelText("输出组（可选，仅路由）")]
        [Tooltip("只决定进哪路 Mixer；音量以本 AudioSource.volume 为准（Settings 音乐滑条）。")]
        [SerializeField]
        private AudioMixerGroup outputGroup;

        [LabelText("淡入淡出")]
        [MinValue(0f)]
        [SerializeField]
        private float fadeSeconds = 0.6f;

        [LabelText("循环默认曲")]
        [SerializeField]
        private bool loopDefault = true;

        private object _overrideOwner;
        private Coroutine _fadeRoutine;
        private float _targetVolume = 1f;

        private void Awake()
        {
            EnsureAudioSource();
            DesktopPetServices.RegisterBgm(this);
        }

        private void Start()
        {
            ApplySettingsVolume();
            if (defaultClip != null)
                PlayDefault(fade: false);
        }

        private void OnDestroy()
        {
            DesktopPetServices.UnregisterBgm(this);
        }

        public void ApplySettingsVolume()
        {
            _targetVolume = Mathf.Clamp01(SettingsStore.BgmVolume);
            if (audioSource == null)
                return;
            if (_fadeRoutine == null)
                audioSource.volume = _targetVolume;
        }

        public void PlayDefault(bool fade = true)
        {
            _overrideOwner = null;
            if (defaultClip == null)
            {
                if (audioSource != null && audioSource.isPlaying)
                    audioSource.Stop();
                return;
            }

            SwitchTo(defaultClip, loopDefault, fade);
        }

        public void PlayOverride(AudioClip clip, object owner, bool fade = true)
        {
            if (clip == null || owner == null)
                return;

            _overrideOwner = owner;
            SwitchTo(clip, loop: true, fade);
        }

        public void ClearOverride(object owner, bool fade = true)
        {
            if (owner == null || !ReferenceEquals(_overrideOwner, owner))
                return;

            _overrideOwner = null;
            PlayDefault(fade);
        }

        private void SwitchTo(AudioClip clip, bool loop, bool fade)
        {
            EnsureAudioSource();
            if (audioSource == null || clip == null)
                return;

            if (_fadeRoutine != null)
            {
                StopCoroutine(_fadeRoutine);
                _fadeRoutine = null;
            }

            ApplySettingsVolume();

            if (!fade || fadeSeconds <= 0.01f)
            {
                audioSource.clip = clip;
                audioSource.loop = loop;
                audioSource.volume = _targetVolume;
                audioSource.Play();
                return;
            }

            _fadeRoutine = StartCoroutine(FadeSwitch(clip, loop));
        }

        private IEnumerator FadeSwitch(AudioClip clip, bool loop)
        {
            float duration = Mathf.Max(0.01f, fadeSeconds);
            if (audioSource.isPlaying)
            {
                float from = audioSource.volume;
                float t = 0f;
                while (t < duration)
                {
                    t += Time.unscaledDeltaTime;
                    audioSource.volume = Mathf.Lerp(from, 0f, t / duration);
                    yield return null;
                }

                audioSource.Stop();
            }

            audioSource.clip = clip;
            audioSource.loop = loop;
            audioSource.volume = 0f;
            audioSource.Play();

            float tIn = 0f;
            while (tIn < duration)
            {
                tIn += Time.unscaledDeltaTime;
                audioSource.volume = Mathf.Lerp(0f, _targetVolume, tIn / duration);
                yield return null;
            }

            audioSource.volume = _targetVolume;
            _fadeRoutine = null;
        }

        private void EnsureAudioSource()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                Debug.LogError("[DesktopPetBgmPlayer] 缺少 AudioSource。请在预制体上预挂后再运行。", this);
                return;
            }

            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            if (outputGroup != null)
                audioSource.outputAudioMixerGroup = outputGroup;
        }
    }
}
