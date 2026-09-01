using DesktopPet.Luby;
using Sirenix.OdinInspector;
using UnityEngine;

namespace DesktopPet.AI
{
    /// <summary>
    /// 点击桌宠：打断当前行为，或切入指定行为（扩展喂食/摸摸）。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class PetInteraction : MonoBehaviour
    {
        public enum ClickAction
        {
            InterruptAndReselect = 0,
            RequestBehavior = 1
        }

        [SerializeField]
        private PetBrain brain;

        [SerializeField]
        private Camera rayCamera;

        [Header("Click")]
        public ClickAction clickAction = ClickAction.InterruptAndReselect;

        [Tooltip("ClickAction = RequestBehavior 时使用；打断强制切入时也可作兜底")]
        [AssetSelector(Paths = "Assets/Resources/GameData/Luby/AI")]
        public PetBehaviorDefinition clickBehavior;

        [Tooltip("打断 / 请求优先级")]
        public int clickPriority = 10;

        [Tooltip("强制切入（忽略 canBeInterrupted）")]
        public bool forceOnClick;

        private Collider2D _collider;
        private LubyInstanceComponent _luby;

        private void Awake()
        {
            if (brain == null)
                brain = GetComponent<PetBrain>();
            _collider = GetComponent<Collider2D>();
            _luby = GetComponent<LubyInstanceComponent>();
            if (rayCamera == null)
                rayCamera = Camera.main;
        }

        private void Update()
        {
            if (brain == null || _collider == null)
                return;

            if (!Input.GetMouseButtonDown(0))
                return;

            if (rayCamera == null)
                rayCamera = Camera.main;
            if (rayCamera == null)
                return;

            Ray ray = rayCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit2D hit = Physics2D.GetRayIntersection(ray, Mathf.Infinity);
            if (hit.collider == null || hit.collider != _collider)
                return;

            HandleClick();
        }

        private void HandleClick()
        {
            if (_luby == null)
                _luby = GetComponent<LubyInstanceComponent>();

            if (_luby != null && DesktopPetServices.LubyTheater != null &&
                DesktopPetServices.LubyTheater.TryHandlePlayerClick(_luby))
                return;

            string behaviorId = clickBehavior != null && !string.IsNullOrEmpty(clickBehavior.behaviorId)
                ? clickBehavior.behaviorId
                : null;
            switch (clickAction)
            {
                case ClickAction.RequestBehavior:
                    if (!string.IsNullOrEmpty(behaviorId))
                        brain.RequestBehavior(behaviorId, clickPriority, forceOnClick);
                    break;
                default:
                    if (forceOnClick)
                    {
                        if (!string.IsNullOrEmpty(behaviorId))
                            brain.RequestBehavior(behaviorId, clickPriority, true);
                    }
                    else
                        brain.InterruptAndReselect(clickPriority);
                    break;
            }
        }
    }
}
