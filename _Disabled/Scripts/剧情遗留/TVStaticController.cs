using UnityEngine;
using System.Collections;
using UnityEngine.UI;

[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(Image))]
public class TVStaticController : MonoBehaviour
{
    [Header("电视雪花参数")]
    [Range(0, 1f)] public float maxStatic = 1f;
    [Range(0, 80f)] public float flickerSpeed = 28f;
    [Range(40, 1200f)] public float grainScale = 320f;
    [Range(100, 2400f)] public float fineGrainScale = 960f;
    [Range(0, 1f)] public float contrast = 0.58f;
    [Range(0, 1f)] public float snowMix = 0.94f;
    [Range(0, 0.05f)] public float rgbSplit = 0.014f;
    [Range(0, 0.05f)] public float rgbSplitSnow = 0.018f;
    [Range(0, 0.35f)] public float chromaBleed = 0.12f;
    public float staticDuration = 0.5f;

    [Header("播放时机")]
    [Tooltip("勾选后：开局不自动播放，需调用 PlayStatic() 或由 Fungus「播放电视雪花」命令触发。")]
    public bool skipPlayOnStart;

    [Header("显示")]
    [Tooltip("不播放时隐藏 Image；播放雪花时显示，结束后再次隐藏。")]
    public bool hideWhenIdle = true;

    [Header("雪花期间贴图")]
    [Tooltip("指定后：播放雪花瞬间把 Image 换成这张图，与雪花 shader 配套。")]
    public Sprite spriteDuringStatic;

    [Tooltip("指定后：雪花结束后用这张图；不指定则恢复为「开始播放前」原来的那张。")]
    public Sprite spriteAfterStatic;

    private Material _mat;
    private Image _image;
    private Coroutine _staticRoutine;
    private Sprite _spriteBackup;
    private bool _swappedStaticSprite;
    private Sprite _playDuringSprite;
    private Sprite _playAfterSprite;

    bool IsPlaying => _staticRoutine != null;

    /// <summary>未播放时是否应隐藏 Image（含「开局不播放」）。</summary>
    bool ShouldHideWhenIdle => hideWhenIdle || skipPlayOnStart;

    void Awake()
    {
        CacheImageAndMaterial();
        SetStaticStrength(0f);
        ApplyIdleOverlayState();
    }

    void Reset()
    {
        CacheImageAndMaterial();
        ApplyIdleOverlayState();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (Application.isPlaying)
            return;
        CacheImageAndMaterial();
        ApplyIdleOverlayState();
    }
#endif

    void CacheImageAndMaterial()
    {
        if (_image == null)
            _image = GetComponent<Image>();
        if (_image != null)
            _mat = _image.material;
    }

    void SetStaticStrength(float value)
    {
        if (_mat == null)
            return;
        _mat.SetFloat("_StaticStrength", value);
        if (_image != null)
            _image.SetMaterialDirty();
    }

    void Start()
    {
        if (!skipPlayOnStart)
            PlayStatic();
        else
            StartCoroutine(EnsureHiddenAfterFrame());
    }

    /// <summary>其它脚本的 Start 可能在本帧内打开 Image，延后一帧再强制收拢。</summary>
    IEnumerator EnsureHiddenAfterFrame()
    {
        yield return null;
        if (!IsPlaying)
            ApplyIdleOverlayState();
    }

    void OnEnable()
    {
        CacheImageAndMaterial();
        ApplyIdleOverlayState();
    }

    void OnDisable()
    {
        if (_staticRoutine != null)
        {
            StopCoroutine(_staticRoutine);
            _staticRoutine = null;
        }
        SetStaticStrength(0f);
        RestoreImageSprite();
        ApplyIdleOverlayState();
    }

    /// <summary>未播放时按配置隐藏 Image，避免开局闪一下。</summary>
    void ApplyIdleOverlayState()
    {
        if (_image == null)
            return;
        if (IsPlaying)
            return;
        if (ShouldHideWhenIdle)
            _image.enabled = false;
    }

    void SetOverlayVisible(bool visible)
    {
        if (_image == null)
            return;
        if (visible)
        {
            _image.enabled = true;
            return;
        }
        if (hideWhenIdle)
            _image.enabled = false;
    }

    /// <summary>播放一次电视雪花噪点；可重复调用，若正在播会先停止当前再重新开始。</summary>
    public void PlayStatic(Sprite duringStaticOverride = null, Sprite afterStaticOverride = null)
    {
        if (_mat == null)
        {
            if (_image == null)
                _image = GetComponent<Image>();
            if (_image != null)
                _mat = _image.material;
        }
        if (_mat == null)
            return;

        if (_image == null)
            _image = GetComponent<Image>();

        if (_staticRoutine != null)
        {
            StopCoroutine(_staticRoutine);
            _staticRoutine = null;
            SetStaticStrength(0f);
            RestoreImageSprite();
            ApplyIdleOverlayState();
        }

        _playDuringSprite = duringStaticOverride != null ? duringStaticOverride : spriteDuringStatic;
        _playAfterSprite = afterStaticOverride != null ? afterStaticOverride : spriteAfterStatic;

        _staticRoutine = StartCoroutine(StaticOnce());
    }

    void TrySwapToStaticSprite()
    {
        if (_playDuringSprite == null)
        {
            _swappedStaticSprite = false;
            return;
        }
        Image img = GetComponent<Image>();
        if (img == null)
        {
            _swappedStaticSprite = false;
            return;
        }
        _spriteBackup = img.sprite;
        img.sprite = _playDuringSprite;
        _swappedStaticSprite = true;
    }

    void RestoreImageSprite()
    {
        if (!_swappedStaticSprite)
            return;
        Image img = GetComponent<Image>();
        if (img == null)
        {
            _swappedStaticSprite = false;
            return;
        }
        img.sprite = _playAfterSprite != null ? _playAfterSprite : _spriteBackup;
        _swappedStaticSprite = false;
    }

    void ApplyShaderParams()
    {
        if (_mat == null)
            return;
        _mat.SetFloat("_FlickerSpeed", flickerSpeed);
        _mat.SetFloat("_GrainScale", grainScale);
        _mat.SetFloat("_FineGrainScale", fineGrainScale);
        _mat.SetFloat("_Contrast", contrast);
        _mat.SetFloat("_SnowMix", snowMix);
        _mat.SetFloat("_RgbSplit", rgbSplit);
        _mat.SetFloat("_RgbSplitSnow", rgbSplitSnow);
        _mat.SetFloat("_ChromaBleed", chromaBleed);
        if (_image != null)
            _image.SetMaterialDirty();
    }

    IEnumerator StaticOnce()
    {
        if (_image == null)
            _image = GetComponent<Image>();

        SetOverlayVisible(true);

        TrySwapToStaticSprite();

        if (_image != null)
            _mat = _image.material;
        if (_mat == null)
        {
            SetOverlayVisible(false);
            _staticRoutine = null;
            yield break;
        }

        ApplyShaderParams();

        float time = 0;
        while (time < staticDuration * 0.35f)
        {
            time += Time.unscaledDeltaTime;
            float s = Mathf.Lerp(0, maxStatic, time / (staticDuration * 0.35f));
            SetStaticStrength(s);
            yield return null;
        }

        time = 0;
        while (time < staticDuration * 0.65f)
        {
            time += Time.unscaledDeltaTime;
            float s = Mathf.Lerp(maxStatic, 0, time / (staticDuration * 0.65f));
            SetStaticStrength(s);
            yield return null;
        }

        SetStaticStrength(0f);
        RestoreImageSprite();
        if (_image != null)
            _mat = _image.material;
        SetOverlayVisible(false);
        _staticRoutine = null;
    }
}
