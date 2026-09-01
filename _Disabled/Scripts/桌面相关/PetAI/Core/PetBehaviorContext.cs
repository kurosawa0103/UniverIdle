using UnityEngine;

namespace DesktopPet.AI
{
    /// <summary>
    /// Brain 每帧传给行为的上下文。
    /// </summary>
    public sealed class PetBehaviorContext
    {
        public PetAgent Agent { get; }
        public float DeltaTime { get; private set; }
        public float Time { get; private set; }

        public PetBehaviorContext(PetAgent agent)
        {
            Agent = agent;
        }

        public void BeginFrame(float deltaTime, float time)
        {
            DeltaTime = deltaTime;
            Time = time;
        }

        public float RandomRange(float min, float max)
        {
            return Random.Range(min, max);
        }
    }
}
