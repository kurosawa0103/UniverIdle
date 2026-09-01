using UnityEngine;
using System.Collections;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class GlitchController : MonoBehaviour
{
    [Header("显像管故障参数")]
    [Range(0, 0.2f)] public float maxGlitch = 0.15f;
    [Range(0, 50)] public float glitchSpeed = 30;
    [Range(1, 10)] public float glitchLines = 5;
    public float glitchDuration = 0.35f;

    [Header("播放时机")]
    [Tooltip("勾选后：开局不自动播放，需调用 PlayGlitch() 或由 Fungus「播放 CRT 故障」命令触发。")]
    public bool skipPlayOnStart;

    [Header("故障期间贴图")]
    [Tooltip("指定后：播放故障瞬间把 Image 换成这张图（适合雪花/坏图），与抖动 shader 配套。")]
    public Sprite spriteDuringGlitch;

    [Tooltip("指定后：故障结束后用这张图；不指定则恢复为「开始播放前」原来的那张。")]
    public Sprite spriteAfterGlitch;

    private Material _mat;
    private Image _image;
    private Coroutine _glitchRoutine;
    private Sprite _spriteBackup;
    private bool _swappedGlitchSprite;
    /// <summary>单次播放解析后的贴图（来自 PlayGlitch 参数或 Inspector 默认值）。</summary>
    private Sprite _playDuringSprite;
    private Sprite _playAfterSprite;

    void Awake()
    {
        _image = GetComponent<Image>();
        if (_image == null)
            return;
        _mat = _image.material;
        SetGlitchStrength(0f);
    }

    /// <summary>
    /// 写入原始 UI 材质上的故障强度，并通知 Graphic 重建材质链。
    /// 使用 Soft Mask 时实际绘制的是克隆材质，仅在 GetModifiedMaterial 时从原始材质 CopyProperties；
    /// 若只 SetFloat 不标脏，克隆会卡在旧强度，表现为故障一直不关。
    /// </summary>
    void SetGlitchStrength(float value)
    {
        if (_mat == null)
            return;
        _mat.SetFloat("_GlitchStrength", value);
        if (_image != null)
            _image.SetMaterialDirty();
    }

    void Start()
    {
        if (!skipPlayOnStart)
            PlayGlitch();
    }

    void OnEnable()
    {
        if (_image == null)
            _image = GetComponent<Image>();
        if (_image != null)
            _mat = _image.material;
    }

    void OnDisable()
    {
        if (_glitchRoutine != null)
        {
            StopCoroutine(_glitchRoutine);
            _glitchRoutine = null;
        }
        SetGlitchStrength(0f);
        RestoreImageSprite();
    }

    /// <summary>播放一次故障动效；可重复调用，若正在播会先停止当前再重新开始。</summary>
    /// <param name="duringGlitchOverride">非空则本条播放用该图作为故障中贴图，否则用组件上的 spriteDuringGlitch。</param>
    /// <param name="afterGlitchOverride">非空则本条播放用该图作为故障结束贴图，否则用组件上的 spriteAfterGlitch；两者皆空则恢复播放前原图。</param>
    public void PlayGlitch(Sprite duringGlitchOverride = null, Sprite afterGlitchOverride = null)
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

        if (_glitchRoutine != null)
        {
            StopCoroutine(_glitchRoutine);
            _glitchRoutine = null;
            SetGlitchStrength(0f);
            RestoreImageSprite();
        }

        _playDuringSprite = duringGlitchOverride != null ? duringGlitchOverride : spriteDuringGlitch;
        _playAfterSprite = afterGlitchOverride != null ? afterGlitchOverride : spriteAfterGlitch;

        _glitchRoutine = StartCoroutine(GlitchOnce());
    }

    /// <summary>与 PlayGlitch 相同，保留旧场景或外部调用名。</summary>
    public void PlayOneGlitchOnce() => PlayGlitch();

    void TrySwapToGlitchSprite()
    {
        if (_playDuringSprite == null)
        {
            _swappedGlitchSprite = false;
            return;
        }
        Image img = GetComponent<Image>();
        if (img == null)
        {
            _swappedGlitchSprite = false;
            return;
        }
        _spriteBackup = img.sprite;
        img.sprite = _playDuringSprite;
        _swappedGlitchSprite = true;
    }

    void RestoreImageSprite()
    {
        if (!_swappedGlitchSprite)
            return;
        Image img = GetComponent<Image>();
        if (img == null)
        {
            _swappedGlitchSprite = false;
            return;
        }
        img.sprite = _playAfterSprite != null ? _playAfterSprite : _spriteBackup;
        _swappedGlitchSprite = false;
    }

    IEnumerator GlitchOnce()
    {
        TrySwapToGlitchSprite();

        // 换 Sprite 后 Graphic 可能绑定新的材质实例，必须重新取 material，否则 SetFloat 写在旧实例上画面无扰动。
        if (_image == null)
            _image = GetComponent<Image>();
        if (_image != null)
            _mat = _image.material;
        if (_mat == null)
        {
            _glitchRoutine = null;
            yield break;
        }

        _mat.SetFloat("_GlitchSpeed", glitchSpeed);
        _mat.SetFloat("_GlitchAmount", glitchLines);
        if (_image != null)
            _image.SetMaterialDirty();

        float time = 0;
        while (time < glitchDuration * 0.4f)
        {
            time += Time.unscaledDeltaTime;
            float s = Mathf.Lerp(0, maxGlitch, time / (glitchDuration * 0.4f));
            SetGlitchStrength(s);
            yield return null;
        }

        time = 0;
        while (time < glitchDuration * 0.6f)
        {
            time += Time.unscaledDeltaTime;
            float s = Mathf.Lerp(maxGlitch, 0, time / (glitchDuration * 0.6f));
            SetGlitchStrength(s);
            yield return null;
        }

        SetGlitchStrength(0f);
        RestoreImageSprite();
        if (_image != null)
            _mat = _image.material;
        _glitchRoutine = null;
    }
}
