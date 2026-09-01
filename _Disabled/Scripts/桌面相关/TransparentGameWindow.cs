using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

/// <summary>
/// Windows 真窗口 Alpha 透明 + 鼠标穿透。
/// 仅在 Windows Standalone 生效；Editor 内不会改真实窗体。
/// </summary>
[DefaultExecutionOrder(-100)]
public class TransparentGameWindow : MonoBehaviour
{
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("Dwmapi.dll")]
    private static extern uint DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS margins);

    [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
    private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
    private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
    private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
    private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_LAYERED = 0x00080000;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_SHOWWINDOW = 0x0040;
    private const string UnityWindowClass = "UnityWndClass";

    private IntPtr _hWnd;
    private bool _windowReady;
    private Coroutine _resolveHwndRoutine;
#endif

    [Header("窗口")]
    [Tooltip("是否置顶（可由设置界面开关；仅 Windows 包生效）")]
    [SerializeField]
    private bool alwaysOnTop = true;

    public bool AlwaysOnTop
    {
        get => alwaysOnTop;
        set
        {
            alwaysOnTop = value;
            ApplyTopMostState();
        }
    }

    [Header("UI 穿透例外")]
    [Tooltip("带这些 Tag 的 UI 不挡住穿透（默认 Tag：DesktopPetIgnore）")]
    public string[] ignoreClickTags = { "DesktopPetIgnore" };

    [Tooltip("这些 Layer 上的 UI 不挡住穿透")]
    public LayerMask ignoreUiLayers;

    [Tooltip("UI Graphic Alpha 低于此值时视为不可点，不挡住穿透")]
    [Range(0f, 1f)]
    public float minUiAlphaToBlock = 0.05f;

    [Header("世界交互")]
    [Tooltip("这些 Layer 上的 Collider2D 会挡住穿透（可点角色等）")]
    public LayerMask interactiveWorldLayers = ~0;

    [Tooltip("世界物体额外判定半径（世界单位）。鼠标靠近角色/装饰时提前取消穿透，减少「先点一下才生效」")]
    [SerializeField]
    private float worldHoverPad = 0.45f;

    [Header("其它")]
    [Tooltip("跨场景保留本组件所在对象")]
    public bool persistAcrossScenes = true;

    private List<GraphicRaycaster> _raycasters = new List<GraphicRaycaster>();

    private EventSystem _eventSystem;
    private bool _clickThrough;
    private readonly List<RaycastResult> _uiHits = new List<RaycastResult>(16);
    private readonly List<CanvasGroup> _canvasGroupBuffer = new List<CanvasGroup>(8);

    private void Awake()
    {
        if (DesktopPetServices.TransparentWindow != null && DesktopPetServices.TransparentWindow != this)
        {
            Destroy(gameObject);
            return;
        }

        DesktopPetServices.RegisterTransparentWindow(this);
        if (persistAcrossScenes)
            DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        RefreshRaycastTargets();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        if (_resolveHwndRoutine != null)
        {
            StopCoroutine(_resolveHwndRoutine);
            _resolveHwndRoutine = null;
        }
#endif
    }

    private void OnDestroy()
    {
        DesktopPetServices.UnregisterTransparentWindow(this);
    }

    private void Start()
    {
        Application.runInBackground = true;
        ConfigureCamerasForTransparency();
        RefreshRaycastTargets();

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        _resolveHwndRoutine = StartCoroutine(ResolveAndApplyWindow());
#else
        // Editor / 非 Windows：不跑 Win32 DWM，避免误以为编辑器里也会真透明
        Debug.Log("[TransparentGameWindow] True Alpha 仅在 Windows 独立构建生效；当前为 Editor 或非 Windows，已跳过窗体透明。");
#endif
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ConfigureCamerasForTransparency();
        RefreshRaycastTargets();

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        if (_windowReady && _hWnd != IntPtr.Zero)
        {
            ApplyDwmAndTopMost();
            // 切场景后强制刷新一次穿透状态
            _clickThrough = false;
            SetClickThrough(true);
        }
        else if (_resolveHwndRoutine == null)
        {
            _resolveHwndRoutine = StartCoroutine(ResolveAndApplyWindow());
        }
#endif
    }

    /// <summary>
    /// 需要游戏收鼠标：可点 UI / 世界物体 / 正在放置 / 面板打开。
    /// 空白不整带接住（否则桌面穿透失效）。穿透时滚轮给系统；缩放须已接住区域。
    /// </summary>
    private bool ShouldCaptureMouse()
    {
        if (DesktopPetServices.IsAnyPlacementHolding())
            return true;
        if (DesktopPetServices.IsHubOpen())
            return true;
        if (DesktopPetServices.InventoryUi != null && DesktopPetServices.InventoryUi.IsAnyDeskOverlayVisible)
            return true;

        return IsHoveringBlockingUI() || IsHoveringWorldCollider2D();
    }

    private void Update()
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        if (!_windowReady)
            return;

        bool capture = ShouldCaptureMouse();
        // 已接住鼠标时，按住不放期间保持接住，避免拖/放中途又穿透
        if (!capture && !_clickThrough && (Input.GetMouseButton(0) || Input.GetMouseButton(1)))
            capture = true;

        SetClickThrough(!capture);
#endif
    }

    private void ConfigureCamerasForTransparency()
    {
        Camera[] cameras = FindObjectsOfType<Camera>();
        for (int i = 0; i < cameras.Length; i++)
        {
            Camera cam = cameras[i];
            if (cam == null || !cam.enabled)
                continue;

            // 渲染到 RT 的相机不改清屏，避免破坏离屏缓冲
            if (cam.targetTexture != null)
                continue;

            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.clear;
            cam.allowHDR = false;
            cam.allowMSAA = false;
        }
    }

    /// <summary>
    /// 手动刷新透明/穿透相关状态（用于修复偶发黑屏/无穿透异常）。
    /// </summary>
    public void RefreshTransparencyCulling()
    {
        // 先做“通用的相机+射线目标”修复（不依赖 Win32）。
        ConfigureCamerasForTransparency();
        RefreshRaycastTargets();

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        // 真实窗体层面：重新 Apply DWM + 刷新 click-through。
        if (_hWnd == IntPtr.Zero)
        {
            // HWND 尚未就绪：尝试重新解析并等待。
            if (_resolveHwndRoutine == null)
                _resolveHwndRoutine = StartCoroutine(ResolveAndApplyWindow());
            return;
        }

        _windowReady = true;
        _clickThrough = false;
        ApplyDwmAndTopMost();
        SetClickThrough(true);
#endif
    }

    private void RefreshRaycastTargets()
    {
        _eventSystem = EventSystem.current != null
            ? EventSystem.current
            : FindObjectOfType<EventSystem>();

        _raycasters = new List<GraphicRaycaster>(FindObjectsOfType<GraphicRaycaster>());
    }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
    private IEnumerator ResolveAndApplyWindow()
    {
        const int maxAttempts = 30;
        for (int i = 0; i < maxAttempts; i++)
        {
            _hWnd = ResolveGameHwnd();
            if (_hWnd != IntPtr.Zero)
                break;
            yield return null;
        }

        _resolveHwndRoutine = null;

        if (_hWnd == IntPtr.Zero)
        {
            Debug.LogError("[TransparentGameWindow] 无法解析游戏窗口句柄，透明/穿透未启用。");
            yield break;
        }

        ApplyDwmAndTopMost();
        _windowReady = true;
        _clickThrough = false;
        SetClickThrough(true);
    }

    private static IntPtr ResolveGameHwnd()
    {
        IntPtr hwnd = GetActiveWindow();
        if (IsLikelyUnityGameWindow(hwnd))
            return hwnd;

        string productName = Application.productName;
        hwnd = FindWindow(UnityWindowClass, productName);
        if (IsLikelyUnityGameWindow(hwnd))
            return hwnd;

        hwnd = FindWindow(null, productName);
        if (IsLikelyUnityGameWindow(hwnd))
            return hwnd;

        return FindWindowByCurrentProcess();
    }

    private static IntPtr FindWindowByCurrentProcess()
    {
        uint pid = (uint)Process.GetCurrentProcess().Id;
        IntPtr found = IntPtr.Zero;

        EnumWindows((hWnd, lParam) =>
        {
            if (!IsWindowVisible(hWnd))
                return true;

            GetWindowThreadProcessId(hWnd, out uint windowPid);
            if (windowPid != pid)
                return true;

            if (!IsUnityWindowClass(hWnd))
                return true;

            found = hWnd;
            return false;
        }, IntPtr.Zero);

        return found;
    }

    private static bool IsLikelyUnityGameWindow(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero || !IsWindowVisible(hwnd))
            return false;

        GetWindowThreadProcessId(hwnd, out uint windowPid);
        if (windowPid != (uint)Process.GetCurrentProcess().Id)
            return false;

        return IsUnityWindowClass(hwnd);
    }

    private static bool IsUnityWindowClass(IntPtr hwnd)
    {
        var className = new StringBuilder(256);
        GetClassName(hwnd, className, className.Capacity);
        return className.ToString() == UnityWindowClass;
    }

    private void ApplyDwmAndTopMost()
    {
        MARGINS margins = new MARGINS { cxLeftWidth = -1 };
        DwmExtendFrameIntoClientArea(_hWnd, ref margins);

        long exStyle = GetWindowLongPtr(_hWnd, GWL_EXSTYLE).ToInt64();
        SetWindowLongPtr(_hWnd, GWL_EXSTYLE, new IntPtr(exStyle | WS_EX_LAYERED));

        ApplyTopMostState();
    }

    private void SetClickThrough(bool enable)
    {
        if (_hWnd == IntPtr.Zero || _clickThrough == enable)
            return;

        long exStyle = GetWindowLongPtr(_hWnd, GWL_EXSTYLE).ToInt64();
        if (enable)
            exStyle |= WS_EX_TRANSPARENT;
        else
            exStyle &= ~WS_EX_TRANSPARENT;

        exStyle |= WS_EX_LAYERED;
        SetWindowLongPtr(_hWnd, GWL_EXSTYLE, new IntPtr(exStyle));
        _clickThrough = enable;
    }

    private static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
    {
        return IntPtr.Size == 8
            ? GetWindowLongPtr64(hWnd, nIndex)
            : new IntPtr(GetWindowLong32(hWnd, nIndex));
    }

    private static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
    {
        return IntPtr.Size == 8
            ? SetWindowLongPtr64(hWnd, nIndex, dwNewLong)
            : new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
    }

    private struct MARGINS
    {
        public int cxLeftWidth;
        public int cxRightWidth;
        public int cyTopHeight;
        public int cyBottomHeight;
    }
#endif

    private void ApplyTopMostState()
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        if (_hWnd == IntPtr.Zero)
            return;

        IntPtr insertAfter = alwaysOnTop ? HWND_TOPMOST : HWND_NOTOPMOST;
        SetWindowPos(_hWnd, insertAfter, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
#endif
    }

    /// <summary>鼠标下是否有应挡住桌面操作的 UI（与穿透窗同规则）。</summary>
    public bool IsPointerOverBlockingUi() => IsHoveringBlockingUI();

    public static bool ShouldBlockWorldPointer()
    {
        TransparentGameWindow tw = DesktopPetServices.TransparentWindow;
        return tw != null && tw.IsPointerOverBlockingUi();
    }

    private bool IsHoveringBlockingUI()
    {
        if (_eventSystem == null || _raycasters == null || _raycasters.Count == 0)
            return false;

        PointerEventData pointer = new PointerEventData(_eventSystem)
        {
            position = Input.mousePosition
        };

        for (int i = 0; i < _raycasters.Count; i++)
        {
            GraphicRaycaster raycaster = _raycasters[i];
            if (raycaster == null || !raycaster.isActiveAndEnabled)
                continue;

            _uiHits.Clear();
            raycaster.Raycast(pointer, _uiHits);
            for (int h = 0; h < _uiHits.Count; h++)
            {
                GameObject go = _uiHits[h].gameObject;
                if (go != null && ShouldUiBlockClickThrough(go))
                    return true;
            }
        }

        return false;
    }

    private bool ShouldUiBlockClickThrough(GameObject go)
    {
        if (IsIgnoredTag(go))
            return false;

        if (((1 << go.layer) & ignoreUiLayers) != 0)
            return false;

        if (!IsUiVisiblyInteractive(go))
            return false;

        return true;
    }

    private bool IsIgnoredTag(GameObject go)
    {
        if (ignoreClickTags == null || ignoreClickTags.Length == 0)
            return false;

        Transform t = go.transform;
        while (t != null)
        {
            for (int i = 0; i < ignoreClickTags.Length; i++)
            {
                string tag = ignoreClickTags[i];
                if (string.IsNullOrEmpty(tag))
                    continue;

                try
                {
                    if (t.CompareTag(tag))
                        return true;
                }
                catch (UnityException)
                {
                    // Tag 未在 TagManager 里注册时忽略
                }
            }

            t = t.parent;
        }

        return false;
    }

    private bool IsUiVisiblyInteractive(GameObject go)
    {
        Graphic graphic = go.GetComponent<Graphic>();
        if (graphic == null)
            return false;
        if (!graphic.raycastTarget || !graphic.enabled)
            return false;
        if (graphic.color.a < minUiAlphaToBlock)
            return false;

        go.GetComponentsInParent(true, _canvasGroupBuffer);
        for (int i = 0; i < _canvasGroupBuffer.Count; i++)
        {
            CanvasGroup group = _canvasGroupBuffer[i];
            if (group == null)
                continue;
            if (!group.blocksRaycasts || group.alpha < minUiAlphaToBlock)
                return false;
        }

        return true;
    }

    private bool IsHoveringWorldCollider2D()
    {
        Camera cam = Camera.main;
        if (cam == null)
            return false;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit2D hit = Physics2D.GetRayIntersection(ray, Mathf.Infinity, interactiveWorldLayers);
        if (hit.collider != null)
            return true;

        // 靠近即可接住：避免「鼠标刚移到角色上，第一下仍穿透」
        float pad = Mathf.Max(0f, worldHoverPad);
        if (pad <= 0.001f)
            return false;

        Vector3 world3 = ray.origin;
        float dz = ray.direction.z;
        if (Mathf.Abs(dz) > 1e-5f)
        {
            float t = (0f - ray.origin.z) / dz;
            if (t >= 0f)
                world3 = ray.origin + ray.direction * t;
        }

        Vector2 world = new Vector2(world3.x, world3.y);
        Collider2D near = Physics2D.OverlapCircle(world, pad, interactiveWorldLayers);
        return near != null;
    }
}
