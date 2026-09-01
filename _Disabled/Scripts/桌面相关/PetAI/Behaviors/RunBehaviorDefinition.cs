using UnityEngine;

namespace DesktopPet.AI
{
    [CreateAssetMenu(menuName = "桌宠/AI/行为/跑步", fileName = "RunBehavior")]
    public sealed class RunBehaviorDefinition : HorizontalMoveBehaviorDefinition
    {
        private void Reset()
        {
            behaviorId = "run";
            weight = 0.5f;
            minDuration = 1f;
            maxDuration = 5f;
            cooldown = 4f;
            canBeInterrupted = true;
            interruptPriority = 0;
            animTrigger = "Run";
            animSpeedParam = "Speed";
            animSpeedValue = 2f;
            moveSpeed = 3.2f;
            minTravelDistance = 4f;
            maxTravelDistance = 0f;
            arriveThreshold = 0.35f;
            arriveHoldSeconds = 0f;
        }
    }
}
