using System;
using UnityEngine;

/// <summary>
/// 桌面活动区：地面条 Collider 左右边 + 脚底线。全场景算一次，Luby 只叠加脚高偏移；不随 zoom 变。
/// 须场景预挂（建议挂 DecorSystem），勿运行时补建。
/// </summary>
[DefaultExecutionOrder(-60)]
public sealed class DesktopPetPlayfieldBounds : MonoBehaviour
{
    private static DesktopPetPlayfieldBounds _instance;
    private static event Action _changed;
    private static bool _missingLogged;

    [Tooltip("相对地面 Collider 左右内缩")]
    [SerializeField]
    private float horizontalPadding = 0.5f;

    [Tooltip("无地面 Collider 时的脚底线 Y 回退")]
    [SerializeField]
    private float fallbackManualGroundY = DesktopPetServices.DefaultManualGroundY;

    public static DesktopPetPlayfieldBounds Instance => _instance;

    public float MinX { get; private set; }
    public float MaxX { get; private set; }

    /// <summary>脚底线世界 Y（地面 Collider 顶边），不含 Luby 轴心偏移。</summary>
    public float GroundLineY { get; private set; }

    public bool IsValid { get; private set; }

    public static event Action Changed
    {
        add => _changed += value;
        remove => _changed -= value;
    }

    /// <summary>取场景实例；缺则报错并返回 null（不扫场）。</summary>
    public static DesktopPetPlayfieldBounds EnsureExists()
    {
        if (_instance != null)
            return _instance;

        if (!_missingLogged)
        {
            _missingLogged = true;
            Debug.LogError(
                "[DesktopPetPlayfieldBounds] 场景未挂 DesktopPetPlayfieldBounds。"
                + "请在 DecorSystem（或场景）上预挂后再运行。");
        }

        return null;
    }

    public static void RefreshGlobal()
    {
        DesktopPetPlayfieldBounds playfield = EnsureExists();
        playfield?.Refresh();
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning("[DesktopPetPlayfieldBounds] 场景中已有实例，忽略重复。");
            Destroy(this);
            return;
        }

        _instance = this;
        _missingLogged = false;
        Refresh();
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    public void Refresh()
    {
        DesktopPetGround ground = DesktopPetServices.Ground;
        Collider2D groundCol = ground != null ? ground.GroundCollider : null;
        if (groundCol != null)
        {
            ApplyFromGroundCollider(groundCol);
        }
        else if (ground != null)
        {
            GroundLineY = ground.ResolveGroundY();
            MinX = -10f;
            MaxX = 10f;
        }
        else
        {
            GroundLineY = fallbackManualGroundY;
            MinX = -10f;
            MaxX = 10f;
        }

        IsValid = true;
        RaiseChanged();
    }

    private void ApplyFromGroundCollider(Collider2D groundCol)
    {
        Bounds b = groundCol.bounds;
        MinX = b.min.x + horizontalPadding;
        MaxX = b.max.x - horizontalPadding;
        if (MaxX - MinX < 2f)
        {
            float mid = b.center.x;
            MinX = mid - 10f;
            MaxX = mid + 10f;
        }

        GroundLineY = b.max.y;
    }

    private static void RaiseChanged()
    {
        _changed?.Invoke();
    }
}
