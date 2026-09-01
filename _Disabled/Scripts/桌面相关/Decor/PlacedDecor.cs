using DesktopPet.Shop;
using Sirenix.OdinInspector;
using UnityEngine;

namespace DesktopPet.Decor
{
    /// <summary>场景中已摆放的装饰实例。</summary>
    public sealed class PlacedDecor : MonoBehaviour
    {
        [Title("已摆装饰", "运行时由 DecorWorld 生成；以下为调试只读信息")]
        [ShowInInspector, ReadOnly, LabelText("实例 ID")]
        public string InstanceId { get; private set; }

        [ShowInInspector, ReadOnly, LabelText("商品 ID")]
        public string ItemId { get; private set; }

        [ShowInInspector, ReadOnly, LabelText("父实例 ID")]
        [Tooltip("叠放时指向底座的 InstanceId；贴地则为空。")]
        public string ParentInstanceId { get; private set; }

        [ShowInInspector, ReadOnly, LabelText("商品定义")]
        public ShopItemDefinition Definition { get; private set; }

        [Title("贴地烟尘")]
        [LabelText("烟尘粒子")]
        [SerializeField]
        private ParticleSystem placeDust;

        private Collider2D _collider;

        /// <summary>主体碰撞（拾取/重叠）；不含 DecorPlaceSurface。由 Initialize 解析。</summary>
        public Collider2D Collider2D => _collider;

        public Bounds WorldBounds
        {
            get
            {
                Collider2D col = Collider2D;
                if (col != null)
                    return col.bounds;

                SpriteRenderer sr = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();
                if (sr != null)
                    return sr.bounds;

                return new Bounds(transform.position, Vector3.one * 0.5f);
            }
        }

        public void Initialize(string instanceId, ShopItemDefinition definition, string parentInstanceId)
        {
            InstanceId = instanceId;
            Definition = definition;
            ItemId = definition != null ? definition.itemId : null;
            ParentInstanceId = parentInstanceId;
            _collider = ResolveBodyCollider();
        }

        public void SetParentInstanceId(string parentInstanceId)
        {
            ParentInstanceId = parentInstanceId;
        }

        private Collider2D ResolveBodyCollider()
        {
            Collider2D self = GetComponent<Collider2D>();
            if (self != null && self.GetComponent<DecorPlaceSurface>() == null)
                return self;

            Collider2D[] cols = GetComponentsInChildren<Collider2D>(true);
            for (int i = 0; i < cols.Length; i++)
            {
                Collider2D c = cols[i];
                if (c == null)
                    continue;
                if (c.GetComponent<DecorPlaceSurface>() != null)
                    continue;
                return c;
            }

            return self;
        }

        /// <summary>贴地放下时播一次烟尘（叠放不播；由调用方判断）。</summary>
        public void PlayPlaceDustOnce()
        {
            ParticleSystem ps = placeDust != null
                ? placeDust
                : GetComponentInChildren<ParticleSystem>(true);
            if (ps == null)
                return;

            ParticleSystemRenderer rend = ps.GetComponent<ParticleSystemRenderer>();
            if (rend != null)
            {
                // 与 Sprite 同走透明队列排序；必须高于背景(0)/装饰(5)/Luby(10)
                rend.sortingLayerID = 0;
                rend.sortingOrder = 50;
            }

            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Play(true);
        }

        /// <summary>掉金币的默认喷发点：装饰顶部偏上一点。</summary>
        public Vector3 GetGoldSpawnOrigin(float extraY = 0.2f)
        {
            Bounds bounds = WorldBounds;
            Vector3 origin = bounds.center;
            origin.y = bounds.max.y + extraY;
            return origin;
        }
    }
}
