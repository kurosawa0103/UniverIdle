using DesktopPet.Luby;
using Sirenix.OdinInspector;
using UnityEngine;

namespace DesktopPet.AI
{
    [CreateAssetMenu(menuName = "桌宠/AI/行为/拾取金币", fileName = "CollectCoinBehavior")]
    public sealed class CollectCoinBehaviorDefinition : PetBehaviorDefinition
    {
        [BoxGroup("移动")]
        [LabelText("移动速度")]
        [MinValue(0.1f)]
        public float moveSpeed = 1.15f;

        [BoxGroup("移动")]
        [LabelText("跟随阈值")]
        [MinValue(0.05f)]
        public float retargetThreshold = 0.05f;

        [BoxGroup("移动")]
        [LabelText("到达停距")]
        [MinValue(0.05f)]
        public float stopDistance = 0.22f;

        private void Reset()
        {
            behaviorId = "collect_coin";
            weight = 0f;
            minDuration = 2f;
            maxDuration = 8f;
            cooldown = 0.25f;
            canBeInterrupted = true;
            interruptPriority = 9;
            animTrigger = "Walk";
            animSpeedParam = "Speed";
            animSpeedValue = 1f;
        }

        public override IPetBehaviorRuntime CreateRuntime() => new Runtime(this);

        private sealed class Runtime : TimedPetBehaviorRuntime
        {
            private bool _finishedEarly;
            private float _lastTargetX;
            private LubyCoinCollector _collector;
            private PetLocomotion _locomotion;

            private CollectCoinBehaviorDefinition Def => (CollectCoinBehaviorDefinition)Definition;

            public Runtime(CollectCoinBehaviorDefinition definition) : base(definition) { }

            public override bool WantsExit => base.WantsExit || _finishedEarly;

            protected override void OnEnterInternal(PetBehaviorContext context)
            {
                LubyInstanceComponent inst = context.Agent != null
                    ? context.Agent.GetComponent<LubyInstanceComponent>()
                    : null;
                _collector = inst != null ? inst.CoinCollector : null;
                _locomotion = context.Agent != null ? context.Agent.Locomotion : null;
                if (_collector == null || !_collector.HasTarget || _locomotion == null)
                {
                    _finishedEarly = true;
                    return;
                }

                _lastTargetX = _collector.TargetCoin.PickupX;
                _locomotion.SetMoveTarget(_lastTargetX, Def.moveSpeed);
            }

            protected override void OnTickInternal(PetBehaviorContext context)
            {
                if (_collector == null || _locomotion == null || !_collector.HasTarget)
                {
                    _finishedEarly = true;
                    return;
                }

                float targetX = _collector.TargetCoin.PickupX;
                if (Mathf.Abs(targetX - _lastTargetX) >= Def.retargetThreshold)
                {
                    _lastTargetX = targetX;
                    _locomotion.SetMoveTarget(_lastTargetX, Def.moveSpeed);
                }

                if (Mathf.Abs(_locomotion.transform.position.x - targetX) <= Def.stopDistance)
                    _locomotion.Stop();
            }

            protected override void OnExitInternal(PetBehaviorContext context)
            {
                _locomotion?.Stop();
            }
        }
    }
}
