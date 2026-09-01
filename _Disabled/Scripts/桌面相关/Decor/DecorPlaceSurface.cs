using Sirenix.OdinInspector;
using UnityEngine;

namespace DesktopPet.Decor
{
    /// <summary>
    /// 可摆放面：薄水平 Trigger。摆放只认本组件；拾取/重叠用 Body。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class DecorPlaceSurface : MonoBehaviour
    {
        [Title("可摆放面", "挂在薄水平 Trigger 上；每层一块")]
        [InfoBox("物品「摆放高度」≤「本层最大摆放高度」才能放下。最大高度填 0 = 本层不限高。", InfoMessageType.None)]

        [BoxGroup("吸附", centerLabel: true)]
        [LabelText("边缘容差")]
        [Tooltip("脚底 X 超出面宽多少仍算压在面上。")]
        [MinValue(0f)]
        [SuffixLabel("世界单位", true)]
        [SerializeField]
        private float edgeSlop = 0.15f;

        [BoxGroup("层高", centerLabel: true)]
        [LabelText("本层最大摆放高度")]
        [Tooltip("与商品 SO「摆放高度」比较。0 = 不限高（如顶层）。")]
        [MinValue(0f)]
        [SuffixLabel("世界单位，0=不限", true)]
        [SerializeField]
        private float maxItemHeight;

        [BoxGroup("层高")]
        [ShowInInspector, ReadOnly, LabelText("当前生效上限")]
        private string DebugMaxHeight =>
            maxItemHeight > 0f ? maxItemHeight.ToString("0.##") : "不限高";

        private Collider2D _collider;
        private PlacedDecor _owner;

        public Collider2D Collider2D => _collider;

        public PlacedDecor Owner => _owner;

        public float SurfaceY
        {
            get
            {
                Collider2D col = Collider2D;
                return col != null ? col.bounds.max.y : transform.position.y;
            }
        }

        public Bounds WorldBounds
        {
            get
            {
                Collider2D col = Collider2D;
                if (col != null)
                    return col.bounds;
                return new Bounds(transform.position, new Vector3(1f, 0.05f, 0f));
            }
        }

        private void Awake()
        {
            _collider = GetComponent<Collider2D>();
            if (_collider != null)
                _collider.isTrigger = true;
            _owner = GetComponentInParent<PlacedDecor>();
        }

        public bool ContainsFootX(float footX)
        {
            Bounds b = WorldBounds;
            return footX >= b.min.x - edgeSlop && footX <= b.max.x + edgeSlop;
        }

        public bool AllowsItemHeight(float itemHeight)
        {
            if (maxItemHeight <= 0f)
                return true;
            return itemHeight <= maxItemHeight + 0.01f;
        }
    }
}
