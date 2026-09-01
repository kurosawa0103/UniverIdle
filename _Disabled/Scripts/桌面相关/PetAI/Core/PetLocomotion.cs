using UnityEngine;

namespace DesktopPet.AI
{
    /// <summary>
    /// 横板底部物理移动：Rigidbody2D 驱动 X 速度，Y 锁地面，边界内活动。
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PetLocomotion : MonoBehaviour
    {
        [Header("Ground")]
        [Tooltip("贴地后再加的偏移。精灵轴心在中心时，可填半身高度让脚更贴地；也可勾选下方自动估算")]
        public float feetOffset;

        [Tooltip("用 Collider2D / SpriteRenderer 高度自动估算 feetOffset（半高）")]
        public bool autoFeetOffsetFromBounds = false;

        [Header("Physics")]
        [Tooltip("速度平滑（越大越跟手，0=瞬时）")]
        public float acceleration = 12f;

        [Header("Facing")]
        public bool flipScaleX = true;

        private float _facingSign = 1f;
        public float GroundY { get; private set; }
        public float MinX { get; private set; }
        public float MaxX { get; private set; }
        public float MoveTargetX { get; private set; }
        public bool HasMoveTarget { get; private set; }
        public bool HitBoundaryThisStep { get; private set; }

        /// <summary>为 true 时允许走出 MinX/MaxX（探险离桌/回桌）。</summary>
        public bool AllowOutOfBounds { get; private set; }

        /// <summary>相对地面线的临时抬高（床/椅面）；0=贴地。FixedUpdate 仍锁 Y，不改重力。</summary>
        public float SurfaceLiftY => _surfaceLiftY;

        private Rigidbody2D _rb;
        private Vector3 _baseScale;
        private float _desiredVelocityX;
        private float _moveSpeed;
        private float _resolvedFeetOffset;
        private float _surfaceLiftY;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();

            _baseScale = transform.localScale;
            if (Mathf.Abs(_baseScale.x) > 0.0001f)
                _facingSign = Mathf.Sign(_baseScale.x);

            ResolveFeetOffset();
            RecalculateBoundsAndGround();
            SnapToLockedY();
            MoveTargetX = transform.position.x;
        }

        private void OnEnable()
        {
            DesktopPetPlayfieldBounds.Changed += OnPlayfieldChanged;
            if (DesktopPetPlayfieldBounds.Instance != null && DesktopPetPlayfieldBounds.Instance.IsValid)
                ApplyPlayfieldBounds();
        }

        private void OnDisable()
        {
            DesktopPetPlayfieldBounds.Changed -= OnPlayfieldChanged;
        }

        /// <summary>外部改过 localScale（如 Luby 模板缩放）后调用，避免朝向翻转用旧尺寸。</summary>
        public void RefreshBaseScale()
        {
            _baseScale = transform.localScale;
            if (Mathf.Abs(_baseScale.x) > 0.0001f)
                _facingSign = Mathf.Sign(_baseScale.x);
            else
                _facingSign = 1f;

            ResolveFeetOffset();
            ApplyPlayfieldBounds();
        }

        private void FixedUpdate()
        {
            HitBoundaryThisStep = false;

            if (HasMoveTarget)
                UpdateDesiredFromTarget();

            float currentVx = _rb.velocity.x;
            float nextVx = acceleration <= 0.01f
                ? _desiredVelocityX
                : Mathf.MoveTowards(currentVx, _desiredVelocityX, acceleration * Time.fixedDeltaTime);

            Vector2 pos = _rb.position;
            pos.y = LockedPivotY;

            if (AllowOutOfBounds)
            {
                pos.x += nextVx * Time.fixedDeltaTime;
                _rb.position = pos;
                _rb.velocity = new Vector2(nextVx, 0f);

                if (HasMoveTarget && HasReachedTarget())
                {
                    HasMoveTarget = false;
                    _desiredVelocityX = 0f;
                    _rb.velocity = Vector2.zero;
                }

                if (Mathf.Abs(nextVx) > 0.05f)
                    SetFacing(Mathf.Sign(nextVx));
                return;
            }

            // 先夹紧已越界位置；只有「仍朝界外推」才算撞边，避免站在边缘时每帧误报
            if (pos.x < MinX)
            {
                pos.x = MinX;
                if (nextVx < -0.01f)
                {
                    HitBoundaryThisStep = true;
                    nextVx = 0f;
                    _desiredVelocityX = 0f;
                    HasMoveTarget = false;
                }
            }
            else if (pos.x > MaxX)
            {
                pos.x = MaxX;
                if (nextVx > 0.01f)
                {
                    HitBoundaryThisStep = true;
                    nextVx = 0f;
                    _desiredVelocityX = 0f;
                    HasMoveTarget = false;
                }
            }

            float predictedX = pos.x + nextVx * Time.fixedDeltaTime;
            if (predictedX <= MinX && nextVx < -0.01f)
            {
                pos.x = MinX;
                nextVx = 0f;
                _desiredVelocityX = 0f;
                HasMoveTarget = false;
                HitBoundaryThisStep = true;
            }
            else if (predictedX >= MaxX && nextVx > 0.01f)
            {
                pos.x = MaxX;
                nextVx = 0f;
                _desiredVelocityX = 0f;
                HasMoveTarget = false;
                HitBoundaryThisStep = true;
            }
            else
            {
                pos.x = Mathf.Clamp(predictedX, MinX, MaxX);
            }

            // Dynamic 体不要用 MovePosition（易与速度求解打架导致看起来“AI 没在走”）
            _rb.position = pos;
            _rb.velocity = new Vector2(nextVx, 0f);

            if (Mathf.Abs(nextVx) > 0.05f)
                SetFacing(Mathf.Sign(nextVx));
        }

        public void SetMoveTarget(float worldX, float speed)
        {
            MoveTargetX = AllowOutOfBounds ? worldX : Mathf.Clamp(worldX, MinX, MaxX);
            _moveSpeed = Mathf.Abs(speed);
            HasMoveTarget = true;
            UpdateDesiredFromTarget();
        }

        public void SetAllowOutOfBounds(bool allow)
        {
            AllowOutOfBounds = allow;
            if (!allow && _rb != null)
            {
                Vector2 pos = _rb.position;
                pos.x = Mathf.Clamp(pos.x, MinX, MaxX);
                _rb.position = pos;
            }
        }

        public bool HasReachedTarget(float threshold = 0.2f)
        {
            // 没有目标时不能当成“已到达”，否则 Walk 会在第一帧被误判结束
            if (!HasMoveTarget || _rb == null)
                return false;
            return Mathf.Abs(_rb.position.x - MoveTargetX) <= threshold;
        }

        /// <summary>
        /// 随机水平目标。maxTravelDistance&gt;0 时优先在「朝左/右走一小段」里抽，更像闲逛。
        /// </summary>
        public float PickRandomTargetX(
            float minTravelDistance,
            float maxTravelDistance,
            System.Func<float, float, float> randomRange)
        {
            float span = MaxX - MinX;
            if (span <= 0.01f || _rb == null)
                return transform.position.x;

            float x = _rb.position.x;
            float minTravel = Mathf.Min(Mathf.Max(0.05f, minTravelDistance), span * 0.35f);

            if (maxTravelDistance > 0.01f)
            {
                float maxTravel = Mathf.Max(minTravel, Mathf.Min(maxTravelDistance, span * 0.85f));
                for (int i = 0; i < 8; i++)
                {
                    float dir = randomRange(0f, 1f) < 0.5f ? -1f : 1f;
                    float dist = randomRange(minTravel, maxTravel);
                    float candidate = Mathf.Clamp(x + dir * dist, MinX, MaxX);
                    if (Mathf.Abs(candidate - x) >= minTravel * 0.5f)
                        return candidate;
                }
            }

            for (int i = 0; i < 8; i++)
            {
                float candidate = randomRange(MinX, MaxX);
                if (Mathf.Abs(candidate - x) >= minTravel)
                    return candidate;
            }

            float toMin = Mathf.Abs(x - MinX);
            float toMax = Mathf.Abs(MaxX - x);
            return toMax >= toMin ? MaxX : MinX;
        }

        public void Stop()
        {
            HasMoveTarget = false;
            _desiredVelocityX = 0f;
            if (_rb != null)
                _rb.velocity = Vector2.zero;
        }

        /// <summary>
        /// 脚底对齐世界高度 worldFeetY（床面/椅面顶）。仍每帧锁 Y，不启用竖直物理。
        /// </summary>
        public void SetSurfaceLiftToFeetY(float worldFeetY)
        {
            float groundLine = ResolveGroundLineY();
            _surfaceLiftY = worldFeetY - groundLine;
            SnapToLockedY();
        }

        /// <summary>取消家具抬高，立刻贴回地面。</summary>
        public void ClearSurfaceLift()
        {
            if (Mathf.Approximately(_surfaceLiftY, 0f))
                return;
            _surfaceLiftY = 0f;
            SnapToLockedY();
        }

        /// <summary>强制水平朝向（+1 右 / -1 左）；小剧场等到点定朝向等场景用。</summary>
        public void SetFacingSign(float sign)
        {
            SetFacing(sign);
        }

        private void SetFacing(float sign)
        {
            if (Mathf.Abs(sign) < 0.01f)
                return;

            _facingSign = Mathf.Sign(sign);
            if (!flipScaleX)
                return;

            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(_baseScale.x) * _facingSign;
            scale.y = _baseScale.y;
            scale.z = _baseScale.z;
            transform.localScale = scale;
        }

        private void RecalculateBoundsAndGround()
        {
            ResolveFeetOffset();
            DesktopPetPlayfieldBounds playfield = DesktopPetPlayfieldBounds.EnsureExists();
            if (playfield != null && !playfield.IsValid)
                playfield.Refresh();
            ApplyPlayfieldBounds();
        }

        /// <summary>把脚底贴到地面碰撞顶边；Spawn / 仓库放置后调用。</summary>
        public void SnapFeetToGround(float worldX, bool clampX = true)
        {
            DesktopPetPlayfieldBounds playfield = DesktopPetPlayfieldBounds.EnsureExists();
            if (playfield != null && !playfield.IsValid)
                playfield.Refresh();
            else
                DesktopPetPlayfieldBounds.RefreshGlobal();

            ApplyPlayfieldBounds();

            float groundLine = playfield != null && playfield.IsValid
                ? playfield.GroundLineY
                : DesktopPetServices.ResolveGroundY();

            float x = clampX ? Mathf.Clamp(worldX, MinX, MaxX) : worldX;
            _surfaceLiftY = 0f;

            // 先把轴心放到地面线再量脚高；否则 collider.bounds 可能还是 Awake 时的旧值
            Vector2 probe = new Vector2(x, groundLine);
            if (_rb != null)
                _rb.position = probe;
            else
                transform.position = new Vector3(x, groundLine, transform.position.z);
            Physics2D.SyncTransforms();
            ResolveFeetOffset();

            float lift = MeasurePivotAboveFeet();
            float pivotY = groundLine + lift;
            GroundY = pivotY;

            Vector2 pos = new Vector2(x, pivotY);
            if (_rb != null)
            {
                _rb.position = pos;
                _rb.velocity = Vector2.zero;
            }
            else
                transform.position = new Vector3(x, pivotY, transform.position.z);

            MoveTargetX = x;
            HasMoveTarget = false;
            _desiredVelocityX = 0f;
        }

        private void OnPlayfieldChanged()
        {
            ApplyPlayfieldBounds();
        }

        private void ApplyPlayfieldBounds()
        {
            DesktopPetPlayfieldBounds playfield = DesktopPetPlayfieldBounds.Instance;
            if (playfield == null || !playfield.IsValid)
            {
                Debug.LogWarning("[PetLocomotion] DesktopPetPlayfieldBounds 未就绪。", this);
                MinX = transform.position.x - 10f;
                MaxX = transform.position.x + 10f;
                GroundY = transform.position.y;
                return;
            }

            MinX = playfield.MinX;
            MaxX = playfield.MaxX;
            GroundY = playfield.GroundLineY + MeasurePivotAboveFeet();
        }

        /// <summary>当前轴心相对碰撞体/精灵底边的高度（用于脚贴地面线）。</summary>
        private float MeasurePivotAboveFeet()
        {
            if (!Mathf.Approximately(feetOffset, 0f) || autoFeetOffsetFromBounds)
                return _resolvedFeetOffset;

            Physics2D.SyncTransforms();

            float pivotY = transform.position.y;
            float feetY = float.PositiveInfinity;

            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null && sr.enabled && sr.sprite != null)
                feetY = Mathf.Min(feetY, sr.bounds.min.y);

            Collider2D col = GetComponent<Collider2D>();
            if (col != null && col.enabled)
                feetY = Mathf.Min(feetY, col.bounds.min.y);

            if (feetY < float.PositiveInfinity)
                return Mathf.Max(0.01f, pivotY - feetY);

            return _resolvedFeetOffset;
        }

        private void ResolveFeetOffset()
        {
            if (!autoFeetOffsetFromBounds)
            {
                _resolvedFeetOffset = feetOffset;
                return;
            }

            Physics2D.SyncTransforms();

            float halfHeight = 0f;
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null && sr.enabled && sr.sprite != null)
                halfHeight = Mathf.Max(halfHeight, sr.bounds.extents.y);

            Collider2D col = GetComponent<Collider2D>();
            if (col != null && col.enabled)
                halfHeight = Mathf.Max(halfHeight, col.bounds.extents.y);

            // 轴心在几何中心时：地面线对准脚底 = 轴心 Y 比地面高半身
            _resolvedFeetOffset = feetOffset + halfHeight;
        }

        private void UpdateDesiredFromTarget()
        {
            float dx = MoveTargetX - _rb.position.x;
            if (Mathf.Abs(dx) <= 0.15f)
            {
                _desiredVelocityX = 0f;
                return;
            }

            _desiredVelocityX = Mathf.Sign(dx) * _moveSpeed;
        }

        private float LockedPivotY => GroundY + _surfaceLiftY;

        private float ResolveGroundLineY()
        {
            DesktopPetPlayfieldBounds playfield = DesktopPetPlayfieldBounds.Instance;
            if (playfield != null && playfield.IsValid)
                return playfield.GroundLineY;
            return DesktopPetServices.ResolveGroundY();
        }

        private void SnapToLockedY()
        {
            Vector2 pos = _rb != null ? _rb.position : (Vector2)transform.position;
            pos.y = LockedPivotY;
            if (!AllowOutOfBounds)
                pos.x = Mathf.Clamp(pos.x, MinX, MaxX);
            if (_rb != null)
            {
                _rb.position = pos;
                _rb.velocity = Vector2.zero;
            }
            else
            {
                transform.position = new Vector3(pos.x, pos.y, transform.position.z);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying)
                ResolveFeetOffset();
        }
#endif
    }
}
