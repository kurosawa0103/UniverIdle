using UnityEngine;
using DG.Tweening;

/// <summary>
/// 对话结束提示箭头：沿 anchoredPosition Y 先上后下为「一下」，连续两下后停顿，再循环。
/// 挂在带 RectTransform 的箭头图标上即可。
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class DialogContinueArrowBounce : MonoBehaviour
{
    [SerializeField]
    [Tooltip("相对初始位置的向上偏移（像素）")]
    private float bounceAmplitude = 12f;

    [SerializeField]
    [Tooltip("单次上移或下移的时长（秒）")]
    private float halfCycleDuration = 0.22f;

    [SerializeField]
    [Tooltip("两轮上下后的静止时间（秒）")]
    private float pauseDuration = 0.85f;

    [SerializeField]
    [Tooltip("若为 true，使用 UnscaledTime（对话 Time.timeScale=0 时仍会动）")]
    private bool useUnscaledTime = true;

    [SerializeField]
    [Tooltip("位移缓动")]
    private Ease moveEase = Ease.InOutQuad;

    private RectTransform _rect;
    private Vector2 _baseAnchoredPos;
    private Sequence _sequence;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _baseAnchoredPos = _rect.anchoredPosition;
    }

    private void OnEnable()
    {
        if (_rect == null)
            _rect = GetComponent<RectTransform>();
        // OnDisable 已把位置拧回本轮静止点；此处再读一次可兼容父布局在禁用期间的变动。
        _baseAnchoredPos = _rect.anchoredPosition;
        BuildAndPlay();
    }

    private void OnDisable()
    {
        if (_sequence != null && _sequence.IsActive())
            _sequence.Kill();
        _sequence = null;
        if (_rect == null)
            return;
        _rect.DOKill();
        // 回到本轮开始记录过的静止位置，避免再次激活时基准被污染
        _rect.anchoredPosition = _baseAnchoredPos;
    }

    /// <summary>若父布局会改位置，可在对话打开时调用以刷新静止点。</summary>
    public void RefreshBasePosition()
    {
        _baseAnchoredPos = _rect.anchoredPosition;
        // 若父布局改变了箭头槽位，刷新后同步为新的静止点，关闭物体时再回到此处
        if (_sequence != null && _sequence.IsActive())
        {
            _sequence.Kill();
            BuildAndPlay();
        }
    }

    private void BuildAndPlay()
    {
        float upY = _baseAnchoredPos.y + bounceAmplitude;

        _sequence = DOTween.Sequence().SetTarget(this).SetLink(gameObject);
        if (useUnscaledTime)
            _sequence.SetUpdate(true);

        // 两下「上→下」：上、回、上、回
        AppendYMove(_sequence, upY);
        AppendYMove(_sequence, _baseAnchoredPos.y);
        AppendYMove(_sequence, upY);
        AppendYMove(_sequence, _baseAnchoredPos.y);

        _sequence.AppendInterval(pauseDuration);
        _sequence.SetLoops(-1, LoopType.Restart);
    }

    private void AppendYMove(Sequence seq, float targetY)
    {
        seq.Append(
            _rect.DOAnchorPosY(targetY, halfCycleDuration).SetEase(moveEase)
        );
    }
}
