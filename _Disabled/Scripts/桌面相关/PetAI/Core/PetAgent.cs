using UnityEngine;

namespace DesktopPet.AI
{
    /// <summary>
    /// 桌宠门面：聚合 Brain / Locomotion / Animator。
    /// AI 组只挂在 <see cref="PetBrain"/>；本类只读转发。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(PetLocomotion))]
    [RequireComponent(typeof(PetBrain))]
    [RequireComponent(typeof(PetAnimatorDriver))]
    public sealed class PetAgent : MonoBehaviour
    {
        [SerializeField]
        private PetBrain brain;

        [SerializeField]
        private PetLocomotion locomotion;

        [SerializeField]
        private PetAnimatorDriver animatorDriver;

        public PetBrain Brain => brain;
        public PetLocomotion Locomotion => locomotion;
        public PetAnimatorDriver AnimatorDriver => animatorDriver;

        private void Awake()
        {
            if (brain == null)
                brain = GetComponent<PetBrain>();
            if (locomotion == null)
                locomotion = GetComponent<PetLocomotion>();
            if (animatorDriver == null)
                animatorDriver = GetComponent<PetAnimatorDriver>();

            if (brain != null && brain.AiGroup == null)
                Debug.LogError("[PetAgent] PetBrain 未指定 PetAiGroup，行为 AI 不会运行。", this);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (brain == null)
                brain = GetComponent<PetBrain>();
            if (locomotion == null)
                locomotion = GetComponent<PetLocomotion>();
            if (animatorDriver == null)
                animatorDriver = GetComponent<PetAnimatorDriver>();
        }
#endif
    }
}
