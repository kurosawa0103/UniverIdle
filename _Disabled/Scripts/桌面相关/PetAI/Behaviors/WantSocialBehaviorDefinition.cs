using DesktopPet.Luby;
using UnityEngine;

namespace DesktopPet.AI
{
    /// <summary>
    /// 日常选型抽到时标记「想社交」；小剧场社交场需有人带此意图才开演。
    /// </summary>
    [CreateAssetMenu(menuName = "桌宠/AI/行为/想社交", fileName = "WantSocialBehavior")]
    public sealed class WantSocialBehaviorDefinition : PetBehaviorDefinition
    {
        [SerializeField]
        [Min(1f)]
        [Tooltip("意图在 Director 上保留的秒数（行为结束后仍可被剧场扫到）")]
        private float intentHoldSeconds = 14f;

        private void Reset()
        {
            behaviorId = "want_social";
            weight = 0.35f;
            minDuration = 1.5f;
            maxDuration = 3f;
            cooldown = 8f;
            canBeInterrupted = true;
            interruptPriority = 0;
            animTrigger = "Stand";
            animSpeedParam = "Speed";
            animSpeedValue = 0f;
        }

        public override IPetBehaviorRuntime CreateRuntime() => new Runtime(this);

        private sealed class Runtime : TimedPetBehaviorRuntime
        {
            private readonly WantSocialBehaviorDefinition _def;

            public Runtime(WantSocialBehaviorDefinition definition) : base(definition)
            {
                _def = definition;
            }

            protected override void OnEnterInternal(PetBehaviorContext context)
            {
                context.Agent?.Locomotion?.Stop();
                LubyInstanceComponent luby = context.Agent != null
                    ? context.Agent.GetComponent<LubyInstanceComponent>()
                    : null;
                DesktopPetServices.LubyTheater?.SignalSocialIntent(luby, _def.intentHoldSeconds);
            }

            protected override void OnTickInternal(PetBehaviorContext context)
            {
            }
        }
    }
}
