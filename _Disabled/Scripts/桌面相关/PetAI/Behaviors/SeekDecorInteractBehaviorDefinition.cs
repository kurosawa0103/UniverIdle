using DesktopPet.Decor;
using DesktopPet.Luby;
using Sirenix.OdinInspector;
using UnityEngine;

namespace DesktopPet.AI
{
    /// <summary>
    /// 加权抽到后：随机 Claim 可玩装饰 → 走到锚点 → 请求商品表演（listen_radio / well_peek 等）。
    /// </summary>
    [CreateAssetMenu(menuName = "桌宠/AI/行为/寻找装饰互动", fileName = "SeekDecorInteractBehavior")]
    public sealed class SeekDecorInteractBehaviorDefinition : PetBehaviorDefinition
    {
        [BoxGroup("移动")]
        [LabelText("移动速度")]
        [MinValue(0.1f)]
        public float moveSpeed = 1.1f;

        [BoxGroup("移动")]
        [LabelText("到达停距")]
        [MinValue(0.05f)]
        public float stopDistance = 0.28f;

        private void Reset()
        {
            behaviorId = "seek_decor";
            weight = 0.2f;
            minDuration = 2f;
            // 走近掐断以系统 maxApproachSeconds 为准；此处只作兜底，勿短于系统超时
            maxDuration = 30f;
            cooldown = 8f;
            canBeInterrupted = true;
            interruptPriority = 4;
            animTrigger = "Walk";
            animSpeedParam = "Speed";
            animSpeedValue = 1f;
        }

        public override bool CanEnter(PetBehaviorContext context)
        {
            if (!base.CanEnter(context))
                return false;
            LubyInstanceComponent luby = ResolveLuby(context);
            LubyDecorInteractionSystem sys = DesktopPetServices.LubyDecorInteraction;
            return luby != null && sys != null && sys.HasAnyEligible(luby);
        }

        public override IPetBehaviorRuntime CreateRuntime() => new Runtime(this);

        private static LubyInstanceComponent ResolveLuby(PetBehaviorContext context)
        {
            return context.Agent != null
                ? context.Agent.GetComponent<LubyInstanceComponent>()
                : null;
        }

        private sealed class Runtime : TimedPetBehaviorRuntime
        {
            private bool _finishedEarly;
            private bool _handedOff;
            private LubyInstanceComponent _luby;
            private LubyDecorInteractionSystem _sys;
            private PetLocomotion _locomotion;
            private float _targetX;

            private SeekDecorInteractBehaviorDefinition Def =>
                (SeekDecorInteractBehaviorDefinition)Definition;

            public Runtime(SeekDecorInteractBehaviorDefinition definition) : base(definition) { }

            public override bool WantsExit => base.WantsExit || _finishedEarly;

            protected override void OnEnterInternal(PetBehaviorContext context)
            {
                _luby = ResolveLuby(context);
                _sys = DesktopPetServices.LubyDecorInteraction;
                _locomotion = context.Agent != null ? context.Agent.Locomotion : null;
                if (_luby == null || _sys == null || _locomotion == null)
                {
                    _finishedEarly = true;
                    return;
                }

                if (!_sys.TryClaimRandom(_luby, out DecorInteractable decor) || decor == null)
                {
                    _finishedEarly = true;
                    return;
                }

                _targetX = decor.AnchorWorld.x;
                _locomotion.SetMoveTarget(_targetX, Def.moveSpeed);
            }

            protected override void OnTickInternal(PetBehaviorContext context)
            {
                if (_finishedEarly || _handedOff)
                    return;
                if (_luby == null || _sys == null || _locomotion == null)
                {
                    _finishedEarly = true;
                    return;
                }

                DecorInteractable decor;
                if (!_sys.TryGetApproachingDecor(_luby, out decor))
                {
                    _finishedEarly = true;
                    return;
                }

                _targetX = decor.AnchorWorld.x;
                _locomotion.SetMoveTarget(_targetX, Def.moveSpeed);

                if (Mathf.Abs(_locomotion.transform.position.x - _targetX) > Def.stopDistance)
                    return;

                _locomotion.Stop();
                if (_sys.TryStartPerformance(_luby))
                {
                    _handedOff = true;
                    _finishedEarly = true;
                }
                else
                {
                    _finishedEarly = true;
                }
            }

            protected override void OnExitInternal(PetBehaviorContext context)
            {
                _locomotion?.Stop();
                if (!_handedOff && _luby != null)
                    _sys?.CancelApproach(_luby);
            }
        }
    }
}
