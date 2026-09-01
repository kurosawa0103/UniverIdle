using UnityEngine;

namespace DesktopPet.AI
{
    /// <summary>
    /// 行为进入条件扩展点。空列表 = 始终可进；需要限制时再挂具体条件 SO。
    /// </summary>
    public abstract class PetBehaviorCondition : ScriptableObject
    {
        public abstract bool Evaluate(PetBehaviorContext context);
    }
}
