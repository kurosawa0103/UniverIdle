using UnityEngine;

namespace DesktopPet.AI
{
    /// <summary>
    /// Animator 占位驱动：无 Animator 时静默 no-op，逻辑仍可运行。
    /// </summary>
    public sealed class PetAnimatorDriver : MonoBehaviour
    {
        [SerializeField]
        private Animator animator;

        private bool _loggedMissing;

        private void Awake()
        {
            if (animator == null)
                animator = GetComponentInChildren<Animator>();

            if (animator == null && !_loggedMissing)
            {
                _loggedMissing = true;
                Debug.Log("[PetAnimatorDriver] 未找到 Animator，动画调用将静默跳过。", this);
            }
        }

        public void SetTrigger(string name)
        {
            if (animator == null || string.IsNullOrEmpty(name))
                return;
            animator.SetTrigger(name);
        }

        public void SetBool(string name, bool value)
        {
            if (animator == null || string.IsNullOrEmpty(name))
                return;
            animator.SetBool(name, value);
        }

        public void SetFloat(string name, float value)
        {
            if (animator == null || string.IsNullOrEmpty(name))
                return;
            animator.SetFloat(name, value);
        }
    }
}
