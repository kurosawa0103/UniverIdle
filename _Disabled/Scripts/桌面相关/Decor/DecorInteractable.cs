using Sirenix.OdinInspector;
using UnityEngine;

namespace DesktopPet.Decor
{
    /// <summary>可玩装饰锚点：半径/槽位/冷却。表演与门闸在装饰商品 SO。</summary>
    [DisallowMultipleComponent]
    public sealed class DecorInteractable : MonoBehaviour
    {
        [Title("装饰交互（空间）")]
        [InfoBox("表演 / 谁能玩 → 配 ShopItemData/Decor_*.asset「Luby 交互」。", InfoMessageType.None)]
        [LabelText("锚点")]
        [Tooltip("留空则用本 Transform")]
        public Transform interactAnchor;

        [LabelText("半径")]
        [MinValue(0.1f)]
        public float radius = 2.2f;

        [LabelText("槽位")]
        [MinValue(1)]
        public int slots = 1;

        [LabelText("结束后冷却（秒）")]
        [MinValue(0f)]
        public float cooldownSeconds = 8f;

        public Vector2 AnchorWorld
        {
            get
            {
                Transform t = interactAnchor != null ? interactAnchor : transform;
                return t.position;
            }
        }

        public int OccupiedSlots { get; private set; }

        public bool HasFreeSlot => OccupiedSlots < Mathf.Max(1, slots);

        public bool TryOccupy()
        {
            if (!HasFreeSlot)
                return false;
            OccupiedSlots++;
            return true;
        }

        public void ReleaseOccupy()
        {
            OccupiedSlots = Mathf.Max(0, OccupiedSlots - 1);
        }

        public bool IsWithinRadius(Vector2 worldPoint)
        {
            Vector2 a = AnchorWorld;
            float dx = worldPoint.x - a.x;
            float dy = worldPoint.y - a.y;
            return dx * dx + dy * dy <= radius * radius;
        }

        /// <summary>冷却字典键：优先摆放实例 id，否则用组件 InstanceID。</summary>
        public string CooldownKey
        {
            get
            {
                PlacedDecor placed = GetComponent<PlacedDecor>();
                if (placed != null && !string.IsNullOrEmpty(placed.InstanceId))
                    return placed.InstanceId;
                return GetInstanceID().ToString();
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.35f);
            Vector3 c = interactAnchor != null ? interactAnchor.position : transform.position;
            Gizmos.DrawWireSphere(c, radius);
        }
#endif
    }
}
