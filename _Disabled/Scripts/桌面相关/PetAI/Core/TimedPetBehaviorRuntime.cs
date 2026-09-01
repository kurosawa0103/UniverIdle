using UnityEngine;

namespace DesktopPet.AI
{
    /// <summary>
    /// 带时长的行为运行时基类。
    /// </summary>
    public abstract class TimedPetBehaviorRuntime : IPetBehaviorRuntime
    {
        protected float Duration;
        protected float Elapsed;

        public PetBehaviorDefinition Definition { get; }
        float IPetBehaviorRuntime.Elapsed => Elapsed;
        float IPetBehaviorRuntime.Duration => Duration;

        protected TimedPetBehaviorRuntime(PetBehaviorDefinition definition)
        {
            Definition = definition;
        }

        public void OnEnter(PetBehaviorContext context)
        {
            Elapsed = 0f;
            Duration = RollDuration(context, Definition);
            ApplyAnim(context);
            OnEnterInternal(context);
        }

        public void OnTick(PetBehaviorContext context)
        {
            Elapsed += context.DeltaTime;
            OnTickInternal(context);
        }

        public void OnExit(PetBehaviorContext context)
        {
            OnExitInternal(context);
        }

        public virtual bool WantsExit => Elapsed >= Duration;

        protected abstract void OnEnterInternal(PetBehaviorContext context);
        protected abstract void OnTickInternal(PetBehaviorContext context);

        protected virtual void OnExitInternal(PetBehaviorContext context)
        {
        }

        private void ApplyAnim(PetBehaviorContext context)
        {
            if (Definition == null)
                return;

            PetAgent agent = context.Agent;
            if (agent?.AnimatorDriver == null)
                return;

            if (!string.IsNullOrEmpty(Definition.animTrigger))
                agent.AnimatorDriver.SetTrigger(Definition.animTrigger);
            if (!string.IsNullOrEmpty(Definition.animBool))
                agent.AnimatorDriver.SetBool(Definition.animBool, Definition.animBoolValue);
            if (!string.IsNullOrEmpty(Definition.animSpeedParam))
                agent.AnimatorDriver.SetFloat(Definition.animSpeedParam, Definition.animSpeedValue);
        }

        private static float RollDuration(PetBehaviorContext context, PetBehaviorDefinition def)
        {
            if (def == null)
                return 1f;
            float min = Mathf.Min(def.minDuration, def.maxDuration);
            float max = Mathf.Max(def.minDuration, def.maxDuration);
            return Mathf.Max(0.05f, context.RandomRange(min, max));
        }
    }
}
