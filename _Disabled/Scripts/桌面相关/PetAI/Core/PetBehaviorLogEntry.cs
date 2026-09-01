using UnityEngine;

namespace DesktopPet.AI
{
    /// <summary>单段行为活动记录（供调试日志 / Editor 查看）。</summary>
    public readonly struct PetBehaviorLogEntry
    {
        public readonly string BehaviorId;
        public readonly string AssetName;
        public readonly float EnteredAt;
        public readonly float ExitedAt;
        public readonly float PlannedDuration;
        public readonly string Reason;

        public bool IsOpen => ExitedAt < 0f;
        public float LivedSeconds => IsOpen ? -1f : Mathf.Max(0f, ExitedAt - EnteredAt);

        public PetBehaviorLogEntry(
            string behaviorId,
            string assetName,
            float enteredAt,
            float exitedAt,
            float plannedDuration,
            string reason)
        {
            BehaviorId = behaviorId ?? string.Empty;
            AssetName = assetName ?? string.Empty;
            EnteredAt = enteredAt;
            ExitedAt = exitedAt;
            PlannedDuration = plannedDuration;
            Reason = reason ?? string.Empty;
        }

        public PetBehaviorLogEntry WithExit(float exitedAt)
        {
            return new PetBehaviorLogEntry(
                BehaviorId,
                AssetName,
                EnteredAt,
                exitedAt,
                PlannedDuration,
                Reason);
        }
    }
}
