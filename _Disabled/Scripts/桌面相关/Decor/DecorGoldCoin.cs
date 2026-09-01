using System.Collections.Generic;
using DesktopPet.Luby;
using DesktopPet.Save;
using DesktopPet.Shop;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace DesktopPet.Decor
{
    /// <summary>桌上装饰掉出的可拾取金币：先喷起再落地，落地弹跳；鼠标碰到或 Luby 走近后收取。</summary>
    [DisallowMultipleComponent]
    public sealed class DecorGoldCoin : MonoBehaviour
    {
        private static readonly List<DecorGoldCoin> ActiveCoins = new List<DecorGoldCoin>(32);

        private const string GoldPrefabResourcePath = "Prefabs/杂项预制体/DecorGoldCoinPickup";
        private const string SilverPrefabResourcePath = "Prefabs/杂项预制体/DecorSilverCoinPickup";
        private const string CopperPrefabResourcePath = "Prefabs/杂项预制体/DecorCopperCoinPickup";

        private static GameObject _goldPrefab;
        private static GameObject _silverPrefab;
        private static GameObject _copperPrefab;

        // 物理喷射
        private const float LaunchSpeedMin = 3.6f;
        private const float LaunchSpeedMax = 5.2f;
        private const float LaunchAngleDegMin = 55f;
        private const float LaunchAngleDegMax = 125f;

        // 收取飘字 + 淡出
        private const float FloatFadeDuration = 0.55f;
        private const float FloatRiseDistance = 0.9f;

        // 落地弹跳
        private const float BounceCount = 2.5f;
        private const float BounceDuration = 0.55f;

        private const float MousePickupRadius = 0.4f;

        public static void CollectActive(List<DecorGoldCoin> results)
        {
            if (results == null) return;
            results.Clear();
            for (int i = 0; i < ActiveCoins.Count; i++)
            {
                DecorGoldCoin coin = ActiveCoins[i];
                if (IsLiveUncollected(coin))
                    results.Add(coin);
            }
        }

        /// <summary>桌上未入账金币价值合计（含尚未落地；已开始收取动画的不计）。</summary>
        public static int SumUncollectedAmount()
        {
            int sum = 0;
            for (int i = 0; i < ActiveCoins.Count; i++)
            {
                DecorGoldCoin coin = ActiveCoins[i];
                if (!IsLiveUncollected(coin))
                    continue;
                sum += coin.Amount;
            }

            return sum;
        }

        private static bool IsLiveUncollected(DecorGoldCoin coin) =>
            coin != null && coin.isActiveAndEnabled && !coin._collected;

        private static readonly List<int> SplitFaces = new List<int>(16);

        /// <summary>按金50 / 银10 / 铜1 拆成多枚掉落；钱包入账仍是各枚面额之和。</summary>
        public static void SpawnValue(Transform parent, Vector3 origin, float groundY, int amount, ShopWallet wallet)
        {
            DeskCoinChange.Split(amount, SplitFaces);
            float spread = SplitFaces.Count > 1 ? 0.08f : 0f;
            for (int i = 0; i < SplitFaces.Count; i++)
            {
                float t = SplitFaces.Count == 1
                    ? 0f
                    : (i / (float)(SplitFaces.Count - 1)) * 2f - 1f;
                Vector3 pos = origin;
                pos.x += t * spread;
                SpawnOne(parent, pos, groundY, SplitFaces[i], wallet);
            }
        }

        private static DecorGoldCoin SpawnOne(
            Transform parent, Vector3 origin, float groundY, int face, ShopWallet wallet)
        {
            GameObject prefab = LoadCoinPrefab(face, out string path);
            if (prefab == null)
            {
                Debug.LogError(
                    "[DecorGoldCoin] 缺少预制体 Resources/" + path
                    + "。请恢复对应硬币预制体。");
                return null;
            }

            GameObject go = Instantiate(prefab, parent);

            DecorGoldCoin coin = go.GetComponent<DecorGoldCoin>();
            if (coin == null)
            {
                Debug.LogError(
                    "[DecorGoldCoin] 预制体缺少 DecorGoldCoin 组件：" + path,
                    go);
                Destroy(go);
                return null;
            }

            if (!coin.Initialize(origin, groundY, face, wallet))
            {
                Destroy(go);
                return null;
            }

            return coin;
        }

        public int Amount { get; private set; }
        public bool IsGrounded => _grounded;
        public bool IsCollected => _collected;
        public float PickupX => transform.position.x;
        public LubyInstanceComponent ClaimedByLuby => _claimedByLuby;

        private ShopWallet _wallet;
        private SpriteRenderer _renderer;
        private CircleCollider2D _collider;
        private TMP_Text _popupText; // 预制体 PopupText（TextMeshProUGUI）
        private Color _popupBaseColor;
        private Rigidbody2D _rb;

        private float _groundPivotY;

        private bool _collected;
        private bool _grounded;
        private bool _bouncing;

        private float _bounceElapsed;
        private Vector3 _baseScale;

        private Sequence _collectSeq;

        private LubyInstanceComponent _claimedByLuby;

        // ─── 给 LubyCoinCollectSystem 的接口 ─────────────────────────────
        public bool TryClaim(LubyInstanceComponent luby)
        {
            if (!CanBePicked) return false;
            if (_claimedByLuby != null && _claimedByLuby != luby) return false;
            _claimedByLuby = luby;
            return true;
        }

        public void ReleaseClaim(LubyInstanceComponent luby)
        {
            if (_claimedByLuby == luby)
                _claimedByLuby = null;
        }

        public bool CanReach(LubyInstanceComponent luby, float collectRadius)
        {
            if (!CanBePicked || luby == null) return false;
            return Mathf.Abs(luby.transform.position.x - PickupX) <= collectRadius;
        }

        private bool CanBePicked => _grounded && !_collected;

        private void OnEnable()
        {
            if (!ActiveCoins.Contains(this))
                ActiveCoins.Add(this);
        }

        private void OnDisable() => ActiveCoins.Remove(this);

        private void Update()
        {
            if (_collected) return;

            if (!_grounded && transform.position.y <= _groundPivotY + 0.01f)
                Land();

            if (_bouncing)
                TickBounce();

            TryPickupByMouseHover();
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (_collected || _grounded) return;
            Land();
        }

        private bool Initialize(Vector3 origin, float groundY, int amount, ShopWallet wallet)
        {
            // reset
            _collected = false;
            _grounded = false;
            _bouncing = false;
            _bounceElapsed = 0f;
            _collectSeq?.Kill();
            _collectSeq = null;
            _claimedByLuby = null;

            Amount = Mathf.Max(1, amount);
            _wallet = wallet;

            _renderer = GetComponent<SpriteRenderer>();
            if (_renderer == null)
            {
                Debug.LogError("[DecorGoldCoin] 预制体缺少 SpriteRenderer。", this);
                return false;
            }

            if (_renderer.sprite == null)
            {
                Debug.LogError("[DecorGoldCoin] SpriteRenderer 未指定 Sprite。", this);
                return false;
            }

            _baseScale = transform.localScale;

            // 拾取 trigger（子节点优先）
            CircleCollider2D trigger = null;
            CircleCollider2D[] cols = GetComponentsInChildren<CircleCollider2D>(true);
            for (int i = 0; i < cols.Length; i++)
            {
                if (cols[i] != null && cols[i].isTrigger) { trigger = cols[i]; break; }
            }

            _collider = trigger != null ? trigger : GetComponent<CircleCollider2D>();
            if (_collider == null)
            {
                Debug.LogError("[DecorGoldCoin] 预制体缺少 CircleCollider2D（拾取 Trigger）。", this);
                return false;
            }

            // 飘字：字体/字号以预制体为准；出生时关掉，收取时再开
            _popupText = GetComponentInChildren<TMP_Text>(true);
            if (_popupText == null)
            {
                Debug.LogError("[DecorGoldCoin] 金币预制体缺少 TMP 飘字子节点 PopupText。", this);
            }
            else
            {
                Transform canvas = _popupText.transform.parent;
                if (canvas != null)
                    canvas.gameObject.SetActive(false);
                _popupText.gameObject.SetActive(false);
                _popupBaseColor = _popupText.color;
            }

            transform.position = origin;

            // 落地对齐：让渲染体/碰撞体底边贴地（视觉不穿地）
            float bottomOffset = _renderer.bounds.min.y - transform.position.y;
            _groundPivotY = groundY - bottomOffset;

            float speed = UnityEngine.Random.Range(LaunchSpeedMin, LaunchSpeedMax);
            float angleDeg = UnityEngine.Random.Range(LaunchAngleDegMin, LaunchAngleDegMax);
            float rad = angleDeg * Mathf.Deg2Rad;
            Vector2 initialVelocity = new Vector2(Mathf.Cos(rad) * speed, Mathf.Sin(rad) * speed);

            _rb = GetComponent<Rigidbody2D>();
            if (_rb == null)
            {
                Debug.LogError("[DecorGoldCoin] 预制体缺少 Rigidbody2D。请手改 DecorGoldCoinPickup.prefab。", this);
                return false;
            }

            _rb.velocity = initialVelocity;

            return true;
        }

        private void Land()
        {
            _grounded = true;

            if (_rb != null)
            {
                _rb.velocity = Vector2.zero;
                _rb.angularVelocity = 0f;
                _rb.isKinematic = true;
            }

            Vector3 p = transform.position;
            p.y = _groundPivotY;
            transform.position = p;

            _bouncing = true;
            _bounceElapsed = 0f;
        }

        private void TickBounce()
        {
            _bounceElapsed += Time.deltaTime;
            float t = _bounceElapsed / BounceDuration;

            if (t >= 1f)
            {
                _bouncing = false;
                transform.localScale = _baseScale;
                return;
            }

            float decay = 1f - t;
            float sine = Mathf.Sin(t * BounceCount * Mathf.PI * 2f);
            float squash = sine * decay * 0.28f;

            Vector3 s = _baseScale;
            s.y = _baseScale.y * (1f - squash);
            s.x = _baseScale.x * (1f + squash * 0.5f);
            transform.localScale = s;
        }

        private void TryPickupByMouseHover()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 mouse = Input.mousePosition;
            mouse.z = Mathf.Abs(cam.transform.position.z - transform.position.z);
            Vector3 world = cam.ScreenToWorldPoint(mouse);
            Vector2 point = new Vector2(world.x, world.y);

            Vector2 coinPos = new Vector2(transform.position.x, transform.position.y);
            if (Vector2.Distance(point, coinPos) <= MousePickupRadius && Collect())
                DesktopPetSaveMgr.PersistActive();
        }

        /// <summary>收币入账并播动画；不写盘。鼠标路径自行 Persist；Luby 路径与近况合并 Persist。</summary>
        internal bool Collect()
        {
            if (_collected) return false;
            _collected = true;
            _claimedByLuby = null;
            _bouncing = false;

            if (_rb != null)
            {
                _rb.velocity = Vector2.zero;
                _rb.isKinematic = true;
            }

            if (_collider != null)
                _collider.enabled = false;

            if (_popupText != null)
            {
                // PopupCanvas 默认 inactive，须同时打开父节点才能渲染
                _popupText.transform.parent?.gameObject.SetActive(true);
                _popupText.text = "+" + Amount;
                _popupText.color = _popupBaseColor; // _popupBaseColor.a == 1f
                _popupText.gameObject.SetActive(true);
            }

            _wallet?.Add(Amount);
            StartCollectTween();
            return true;
        }

        private void StartCollectTween()
        {
            _collectSeq = DOTween.Sequence();

            Vector3 end = transform.position + Vector3.up * FloatRiseDistance;

            _collectSeq.Join(transform.DOMove(end, FloatFadeDuration).SetEase(Ease.OutQuad));

            if (_renderer != null)
            {
                Color rc = _renderer.color;
                rc.a = 0f;
                _collectSeq.Join(_renderer.DOColor(rc, FloatFadeDuration));
            }

            if (_popupText != null)
            {
                Color pc = _popupBaseColor;
                pc.a = 0f;
                _collectSeq.Join(DOTween.To(() => _popupText.color, x => _popupText.color = x, pc, FloatFadeDuration));
            }

            _collectSeq.OnComplete(() =>
            {
                if (this != null)
                    Destroy(gameObject);
            });
        }

        private static GameObject LoadCoinPrefab(int face, out string path)
        {
            switch (face)
            {
                case DeskCoinChange.GoldFace:
                    path = GoldPrefabResourcePath;
                    if (_goldPrefab == null)
                        _goldPrefab = Resources.Load<GameObject>(path);
                    return _goldPrefab;
                case DeskCoinChange.SilverFace:
                    path = SilverPrefabResourcePath;
                    if (_silverPrefab == null)
                        _silverPrefab = Resources.Load<GameObject>(path);
                    return _silverPrefab;
                default:
                    path = CopperPrefabResourcePath;
                    if (_copperPrefab == null)
                        _copperPrefab = Resources.Load<GameObject>(path);
                    return _copperPrefab;
            }
        }
    }
}

