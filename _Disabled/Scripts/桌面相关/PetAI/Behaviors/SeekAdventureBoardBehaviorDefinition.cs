using DesktopPet.AI;
using DesktopPet.Decor;
using DesktopPet.Luby;
using Sirenix.OdinInspector;
using UnityEngine;

namespace DesktopPet.Adventure
{
    /// <summary>
    /// 加权抽到后：Claim 探险看板 → 走近 → 看板前互动 → 离桌探险（由 LubyAdventureSystem 收尾）。
    /// </summary>
    [CreateAssetMenu(menuName = "桌宠/AI/行为/寻找探险看板", fileName = "SeekAdventureBoardBehavior")]
    public sealed class SeekAdventureBoardBehaviorDefinition : PetBehaviorDefinition
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
            behaviorId = "seek_adventure_board";
            weight = 0.1f;
            minDuration = 2f;
            maxDuration = 30f;
            cooldown = 20f;
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
            LubyAdventureSystem sys = DesktopPetServices.LubyAdventure;
            return luby != null && sys != null && sys.HasAnyEligibleBoard(luby);
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
            private LubyAdventureSystem _sys;
            private PetLocomotion _locomotion;
            private float _targetX;

            private SeekAdventureBoardBehaviorDefinition Def =>
                (SeekAdventureBoardBehaviorDefinition)Definition;

            public Runtime(SeekAdventureBoardBehaviorDefinition definition) : base(definition) { }

            public override bool WantsExit => base.WantsExit || _finishedEarly;

            protected override void OnEnterInternal(PetBehaviorContext context)
            {
                _luby = ResolveLuby(context);
                _sys = DesktopPetServices.LubyAdventure;
                _locomotion = context.Agent != null ? context.Agent.Locomotion : null;
                if (_luby == null || _sys == null || _locomotion == null)
                {
                    _finishedEarly = true;
                    return;
                }

                if (!_sys.TryClaimBoard(_luby, out DecorInteractable board) || board == null)
                {
                    _finishedEarly = true;
                    return;
                }

                _targetX = board.AnchorWorld.x;
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

                if (!_sys.TryGetApproachingBoard(_luby, out DecorInteractable board))
                {
                    _finishedEarly = true;
                    return;
                }

                _targetX = board.AnchorWorld.x;
                _locomotion.SetMoveTarget(_targetX, Def.moveSpeed);

                if (Mathf.Abs(_locomotion.transform.position.x - _targetX) > Def.stopDistance)
                    return;

                _locomotion.Stop();
                if (_sys.TryStartBoardInteract(_luby))
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
