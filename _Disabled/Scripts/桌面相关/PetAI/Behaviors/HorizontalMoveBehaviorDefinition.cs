using Sirenix.OdinInspector;
using UnityEngine;

namespace DesktopPet.AI
{
    /// <summary>水平随机目标移动（走路/跑步共用逻辑，子类只配参数与动画）。</summary>
    public abstract class HorizontalMoveBehaviorDefinition : PetBehaviorDefinition
    {
        [BoxGroup("移动")]
        [LabelText("移动速度")]
        [MinValue(0f)]
        public float moveSpeed = 1.2f;

        [BoxGroup("移动")]
        [LabelText("最短路程")]
        [MinValue(0f)]
        [Tooltip("目标点至少离开当前位置这么远，避免原地抖")]
        public float minTravelDistance = 2.5f;

        [BoxGroup("移动")]
        [LabelText("最长路程")]
        [Tooltip("单次走动最远；≤0 表示不限制（可跨大半屏）")]
        public float maxTravelDistance = 0f;

        [BoxGroup("移动")]
        [LabelText("到达阈值")]
        [MinValue(0.01f)]
        [Tooltip("认为到达目标的距离")]
        public float arriveThreshold = 0.25f;

        [BoxGroup("移动")]
        [LabelText("到点后再停（秒）")]
        [MinValue(0f)]
        [Tooltip("到点或撞边停住后再发呆多久才结束本段")]
        public float arriveHoldSeconds = 0f;

        public override IPetBehaviorRuntime CreateRuntime() => new Runtime(this);
        private sealed class Runtime : TimedPetBehaviorRuntime
        {
            private bool _finishedEarly;
            private float _enterTime;
            private float _holdUntil = -1f;

            private HorizontalMoveBehaviorDefinition Def => (HorizontalMoveBehaviorDefinition)Definition;

            public Runtime(HorizontalMoveBehaviorDefinition definition) : base(definition) { }

            public override bool WantsExit => base.WantsExit || _finishedEarly;

            protected override void OnEnterInternal(PetBehaviorContext context)
            {
                _finishedEarly = false;
                _holdUntil = -1f;
                _enterTime = context.Time;
                PetLocomotion loco = context.Agent != null ? context.Agent.Locomotion : null;
                if (loco == null)
                {
                    Debug.LogWarning("[" + Def.GetType().Name + "] PetLocomotion 缺失，无法移动。", context.Agent);
                    _finishedEarly = true;
                    return;
                }

                float target = loco.PickRandomTargetX(
                    Def.minTravelDistance,
                    Def.maxTravelDistance,
                    context.RandomRange);
                loco.SetMoveTarget(target, Def.moveSpeed);
            }

            protected override void OnTickInternal(PetBehaviorContext context)
            {
                PetLocomotion loco = context.Agent != null ? context.Agent.Locomotion : null;
                if (loco == null)
                {
                    _finishedEarly = true;
                    return;
                }

                if (_holdUntil >= 0f)
                {
                    if (context.Time >= _holdUntil)
                        _finishedEarly = true;
                    return;
                }

                // 开局短暂忽略撞边，避免边界估算抖动导致立刻结束
                bool allowBoundaryExit = context.Time - _enterTime > 0.2f;
                if (loco.HasReachedTarget(Def.arriveThreshold) ||
                    (allowBoundaryExit && loco.HitBoundaryThisStep))
                {
                    loco.Stop();
                    if (Def.arriveHoldSeconds > 0.01f)
                        _holdUntil = context.Time + Def.arriveHoldSeconds;
                    else
                        _finishedEarly = true;
                }
            }

            protected override void OnExitInternal(PetBehaviorContext context)
            {
                context.Agent?.Locomotion?.Stop();
            }
        }
    }
}
