using UnityEngine;

namespace DesktopPet
{
    /// <summary>
    /// 滚轮 zoom：拉近跟鼠标；拉远按进度插值回到居中机位。
    /// </summary>
    public sealed class DesktopCameraZoom : MonoBehaviour
    {
        [Header("目标")]
        [SerializeField]
        private Camera targetCamera;

        [Header("缩放")]
        [Tooltip("滚轮灵敏度")]
        [SerializeField]
        private float zoomSpeed = 2f;

        [Tooltip("Ortho=orthographicSize；透视=相机到缩放平面的距离")]
        [SerializeField]
        private float minZoom = 6f;

        [SerializeField]
        private float maxZoom = 22f;

        [Tooltip("求交平面的世界 Z（精灵一般在 Z=0）")]
        [SerializeField]
        private float zoomPlaneZ;

        [Tooltip("0=瞬时；越大越慢")]
        [SerializeField]
        private float zoomSmoothing;

        [Header("视野边界（世界 XY，在缩放平面上）")]
        [SerializeField]
        private Vector2 boundsMin = new Vector2(-22.7f, -13.5f);

        [SerializeField]
        private Vector2 boundsMax = new Vector2(24.1f, -8.3f);

        [Tooltip("视野底边锁在 boundsMin.y")]
        [SerializeField]
        private bool pinBottom = true;

        [Tooltip("指针在 UI 上时不缩放")]
        [SerializeField]
        private bool ignoreWhenPointerOverUi = true;

        private float _currentZoom;
        private float _targetZoom;
        private Vector3 _lockedAnchorScreen;
        private bool _hasLockedAnchor;
        private Vector3 _centeredCameraPosition;
        private bool _hasCenteredCameraPosition;
        private bool _zoomingOutGesture;
        private float _zoomOutStartZoom;
        private Vector3 _zoomOutStartPos;

        public float ZoomSpeed
        {
            get => zoomSpeed;
            set => zoomSpeed = Mathf.Max(0.05f, value);
        }

        public bool IgnoreWhenPointerOverUi
        {
            get => ignoreWhenPointerOverUi;
            set => ignoreWhenPointerOverUi = value;
        }

        private void Awake()
        {
            DesktopPetServices.RegisterCameraZoom(this);

            if (targetCamera == null)
                targetCamera = Camera.main;

            if (targetCamera == null)
            {
                Debug.LogError("[DesktopCameraZoom] 未找到 Camera。");
                enabled = false;
                return;
            }

            _currentZoom = ReadZoom();
            _targetZoom = Mathf.Clamp(_currentZoom, minZoom, maxZoom);
            _currentZoom = _targetZoom;
            ApplyZoomImmediate(_currentZoom);
            ClampToBounds();
            CaptureCenteredCameraPosition();
        }

        private void OnDestroy()
        {
            DesktopPetServices.UnregisterCameraZoom(this);
        }

        private void Update()
        {
            if (targetCamera == null)
                return;

            float scroll = NormalizeScroll(Input.mouseScrollDelta.y);
            if (Mathf.Abs(scroll) > 0.01f && !ShouldIgnoreScroll())
                HandleScroll(scroll);

            if (zoomSmoothing > 0.0001f && !Mathf.Approximately(_currentZoom, _targetZoom))
            {
                float t = 1f - Mathf.Exp(-Time.deltaTime * (12f / zoomSmoothing));
                float next = Mathf.Lerp(_currentZoom, _targetZoom, t);
                ApplyZoomAroundMouse(next);
            }
            else if (_hasLockedAnchor || _zoomingOutGesture)
            {
                _hasLockedAnchor = false;
                _zoomingOutGesture = false;
            }
        }

        private bool ShouldIgnoreScroll()
        {
            if (!ignoreWhenPointerOverUi)
                return false;
            return TransparentGameWindow.ShouldBlockWorldPointer();
        }

        private void HandleScroll(float scroll)
        {
            float desired = Mathf.Clamp(_targetZoom - scroll * zoomSpeed, minZoom, maxZoom);
            bool zoomingOut = desired > _currentZoom + 1e-4f;

            if (zoomingOut)
            {
                if (!_zoomingOutGesture)
                {
                    _zoomingOutGesture = true;
                    _zoomOutStartZoom = _currentZoom;
                    _zoomOutStartPos = targetCamera.transform.position;
                }

                _hasLockedAnchor = false;
            }
            else
            {
                _zoomingOutGesture = false;
                _lockedAnchorScreen = Input.mousePosition;
                _hasLockedAnchor = true;
            }

            if (zoomSmoothing <= 0.0001f)
            {
                ApplyZoomAroundMouse(desired);
                _targetZoom = _currentZoom;
                _hasLockedAnchor = false;
                _zoomingOutGesture = false;
            }
            else
            {
                _targetZoom = desired;
            }
        }

        /// <summary>
        /// Unity 通常每格 ±1；部分驱动/输入会给 ±120。再夹到 ±1，避免一格从 max 跳到 min。
        /// </summary>
        private static float NormalizeScroll(float raw)
        {
            float abs = Mathf.Abs(raw);
            if (abs < 0.01f)
                return 0f;
            if (abs >= 2f)
                raw /= 120f;
            return Mathf.Clamp(raw, -1f, 1f);
        }

        private void ApplyZoomAroundMouse(float newZoom)
        {
            if (_zoomingOutGesture && _hasCenteredCameraPosition)
            {
                ApplyZoomImmediate(newZoom);

                float raw = Mathf.Clamp01(Mathf.InverseLerp(_zoomOutStartZoom, maxZoom, newZoom));
                float t = raw * raw * (3f - 2f * raw);

                Vector3 pos = targetCamera.transform.position;
                pos.x = Mathf.Lerp(_zoomOutStartPos.x, _centeredCameraPosition.x, t);
                pos.y = Mathf.Lerp(_zoomOutStartPos.y, _centeredCameraPosition.y, t);
                targetCamera.transform.position = pos;
            }
            else
            {
                Vector3 anchorScreen = _hasLockedAnchor ? _lockedAnchorScreen : Input.mousePosition;
                bool hasAnchor = TryScreenToPlane(anchorScreen, out Vector3 before);

                ApplyZoomImmediate(newZoom);

                if (hasAnchor && TryScreenToPlane(anchorScreen, out Vector3 after))
                {
                    Vector3 delta = before - after;
                    delta.z = 0f;
                    targetCamera.transform.position += delta;
                }
            }

            ClampToBounds();
        }

        /// <summary>maxZoom + Clamp 下的居中机位，拉远时插值目标。</summary>
        private void CaptureCenteredCameraPosition()
        {
            Vector3 savedPos = targetCamera.transform.position;
            float savedZoom = _currentZoom;

            ApplyZoomImmediate(maxZoom);
            ClampToBounds();
            _centeredCameraPosition = targetCamera.transform.position;
            _hasCenteredCameraPosition = true;

            ApplyZoomImmediate(savedZoom);
            targetCamera.transform.position = savedPos;
            _currentZoom = savedZoom;
        }

        private void ApplyZoomImmediate(float zoom)
        {
            zoom = Mathf.Clamp(zoom, minZoom, maxZoom);
            _currentZoom = zoom;

            if (targetCamera.orthographic)
            {
                targetCamera.orthographicSize = zoom;
                return;
            }

            // 透视：沿视线退到距缩放平面 zoom 处（相机必须在平面「前方」，即朝平面看）
            Transform t = targetCamera.transform;
            Vector3 pos = t.position;
            Vector3 forward = t.forward;
            if (Mathf.Abs(forward.z) < 0.0001f)
                return;

            float tHit = (zoomPlaneZ - pos.z) / forward.z;
            Vector3 onPlane = pos + forward * tHit;
            t.position = onPlane - forward.normalized * zoom;
        }

        private float ReadZoom()
        {
            if (targetCamera.orthographic)
                return targetCamera.orthographicSize;

            Transform t = targetCamera.transform;
            Vector3 forward = t.forward;
            if (Mathf.Abs(forward.z) < 0.0001f)
                return Mathf.Abs(t.position.z - zoomPlaneZ);

            float s = (zoomPlaneZ - t.position.z) / forward.z;
            return Mathf.Abs(s);
        }

        /// <summary>
        /// 与 z=zoomPlaneZ 平面求交。不用 Plane.Raycast：相机在平面背面时会永远失败，
        /// 导致视野矩形变成 (0,0) 后 pinBottom 每帧累加偏移。
        /// </summary>
        private bool TryRayToPlane(Ray ray, out Vector3 world)
        {
            world = default;
            float dz = ray.direction.z;
            if (Mathf.Abs(dz) < 1e-6f)
                return false;

            float t = (zoomPlaneZ - ray.origin.z) / dz;
            if (t < 0f)
                return false;

            world = ray.origin + ray.direction * t;
            return IsFinite(world);
        }

        private bool TryScreenToPlane(Vector3 screen, out Vector3 world)
        {
            return TryRayToPlane(targetCamera.ScreenPointToRay(screen), out world);
        }

        private bool TryViewportToPlane(Vector2 viewport, out Vector3 world)
        {
            return TryRayToPlane(targetCamera.ViewportPointToRay(viewport), out world);
        }

        private bool TryGetViewRectOnPlane(out Rect view)
        {
            view = default;
            if (!TryViewportToPlane(new Vector2(0f, 0f), out Vector3 bl))
                return false;
            if (!TryViewportToPlane(new Vector2(1f, 0f), out Vector3 br))
                return false;
            if (!TryViewportToPlane(new Vector2(0f, 1f), out Vector3 tl))
                return false;
            if (!TryViewportToPlane(new Vector2(1f, 1f), out Vector3 tr))
                return false;

            float xMin = Min4(bl.x, br.x, tl.x, tr.x);
            float xMax = Max4(bl.x, br.x, tl.x, tr.x);
            float yMin = Min4(bl.y, br.y, tl.y, tr.y);
            float yMax = Max4(bl.y, br.y, tl.y, tr.y);

            if (xMax - xMin < 1e-4f || yMax - yMin < 1e-4f)
                return false;

            view = Rect.MinMaxRect(xMin, yMin, xMax, yMax);
            return true;
        }

        private void ClampToBounds()
        {
            if (boundsMax.x <= boundsMin.x || boundsMax.y <= boundsMin.y)
                return;

            if (!TryGetViewRectOnPlane(out Rect view))
                return;

            float viewW = view.width;
            float viewH = view.height;
            float boundsW = boundsMax.x - boundsMin.x;
            float boundsH = boundsMax.y - boundsMin.y;

            float shiftX = 0f;
            float shiftY = 0f;

            if (viewW >= boundsW)
            {
                float centerX = (boundsMin.x + boundsMax.x) * 0.5f;
                shiftX = centerX - view.center.x;
            }
            else
            {
                if (view.xMin < boundsMin.x)
                    shiftX = boundsMin.x - view.xMin;
                else if (view.xMax > boundsMax.x)
                    shiftX = boundsMax.x - view.xMax;
            }

            if (pinBottom)
            {
                shiftY = boundsMin.y - view.yMin;
                float newYMax = view.yMax + shiftY;
                if (viewH < boundsH && newYMax > boundsMax.y)
                    shiftY += boundsMax.y - newYMax;
            }
            else if (viewH >= boundsH)
            {
                float centerY = (boundsMin.y + boundsMax.y) * 0.5f;
                shiftY = centerY - view.center.y;
            }
            else
            {
                if (view.yMin < boundsMin.y)
                    shiftY = boundsMin.y - view.yMin;
                else if (view.yMax > boundsMax.y)
                    shiftY = boundsMax.y - view.yMax;
            }

            Vector3 pos = targetCamera.transform.position;
            pos.x += shiftX;
            pos.y += shiftY;
            targetCamera.transform.position = pos;
        }

        private static bool IsFinite(Vector3 v)
        {
            return !(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z)
                     || float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z));
        }

        private static float Min4(float a, float b, float c, float d)
        {
            return Mathf.Min(Mathf.Min(a, b), Mathf.Min(c, d));
        }

        private static float Max4(float a, float b, float c, float d)
        {
            return Mathf.Max(Mathf.Max(a, b), Mathf.Max(c, d));
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.85f);
            Vector3 a = new Vector3(boundsMin.x, boundsMin.y, zoomPlaneZ);
            Vector3 b = new Vector3(boundsMax.x, boundsMin.y, zoomPlaneZ);
            Vector3 c = new Vector3(boundsMax.x, boundsMax.y, zoomPlaneZ);
            Vector3 d = new Vector3(boundsMin.x, boundsMax.y, zoomPlaneZ);
            Gizmos.DrawLine(a, b);
            Gizmos.DrawLine(b, c);
            Gizmos.DrawLine(c, d);
            Gizmos.DrawLine(d, a);
        }
#endif
    }
}
