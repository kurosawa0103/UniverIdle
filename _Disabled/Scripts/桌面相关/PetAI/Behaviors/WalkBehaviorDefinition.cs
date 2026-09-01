using UnityEngine;

namespace DesktopPet.AI
{
    [CreateAssetMenu(menuName = "桌宠/AI/行为/走路", fileName = "WalkBehavior")]
    public sealed class WalkBehaviorDefinition : HorizontalMoveBehaviorDefinition
    {
        private void Reset()
        {
            behaviorId = "walk";
            weight = 1.2f;
            minDuration = 2f;
            maxDuration = 10f;
            cooldown = 1.5f;
            canBeInterrupted = true;
            interruptPriority = 0;
            animTrigger = "Walk";
            animSpeedParam = "Speed";
            animSpeedValue = 1f;
            moveSpeed = 1.2f;
            minTravelDistance = 2.5f;
            maxTravelDistance = 0f;
            arriveThreshold = 0.25f;
            arriveHoldSeconds = 0f;
        }
    }
}
