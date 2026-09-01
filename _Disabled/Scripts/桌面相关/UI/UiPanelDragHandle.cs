using UnityEngine;
using UnityEngine.EventSystems;

namespace DesktopPet.UI
{
    /// <summary>拖动手柄：在 dragHandle 区域按下拖动，移动 dragTarget（默认同节点）。</summary>
    [DisallowMultipleComponent]
    public sealed class UiPanelDragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        [SerializeField] private RectTransform dragTarget;

        /// <summary>用户是否手动拖过（用于取消跟随 Luby 等自动定位）。</summary>
        public bool UserMoved { get; private set; }

        public void Configure(RectTransform target)
        {
            dragTarget = target;
        }

        public void ResetUserMoved()
        {
            UserMoved = false;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            UserMoved = true;
        }

        public void OnDrag(PointerEventData eventData)
        {
            RectTransform rt = dragTarget != null ? dragTarget : transform as RectTransform;
            if (rt == null)
                return;

            Canvas canvas = rt.GetComponentInParent<Canvas>();
            float scale = canvas != null ? canvas.scaleFactor : 1f;
            rt.anchoredPosition += eventData.delta / scale;
        }
    }
}
