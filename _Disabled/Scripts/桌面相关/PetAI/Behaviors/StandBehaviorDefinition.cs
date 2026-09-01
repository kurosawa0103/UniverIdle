using UnityEngine;

namespace DesktopPet.AI
{
    [CreateAssetMenu(menuName = "桌宠/AI/行为/站立", fileName = "StandBehavior")]
    public sealed class StandBehaviorDefinition : PetBehaviorDefinition
    {
        private void Reset()
        {
            behaviorId = "stand";
            weight = 1.5f;
            minDuration = 2f;
            maxDuration = 5f;
            cooldown = 0f;
            canBeInterrupted = true;
            interruptPriority = 0;
            animTrigger = "Stand";
            animSpeedParam = "Speed";
            animSpeedValue = 0f;
        }

        public override IPetBehaviorRuntime CreateRuntime() => new Runtime(this);

        private sealed class Runtime : TimedPetBehaviorRuntime
        {
            public Runtime(StandBehaviorDefinition definition) : base(definition)
            {
            }

            protected override void OnEnterInternal(PetBehaviorContext context)
            {
                context.Agent?.Locomotion?.Stop();
            }

            protected override void OnTickInternal(PetBehaviorContext context)
            {
            }
        }
    }
}
