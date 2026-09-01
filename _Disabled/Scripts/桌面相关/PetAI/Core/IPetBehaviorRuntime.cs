using UnityEngine;

namespace DesktopPet.AI
{
    /// <summary>
    /// 行为运行时实例（非 MonoBehaviour），由 <see cref="PetBehaviorDefinition.CreateRuntime"/> 创建。
    /// </summary>
    public interface IPetBehaviorRuntime
    {
        PetBehaviorDefinition Definition { get; }
        float Elapsed { get; }
        float Duration { get; }
        void OnEnter(PetBehaviorContext context);
        void OnTick(PetBehaviorContext context);
        void OnExit(PetBehaviorContext context);
        bool WantsExit { get; }
    }
}
