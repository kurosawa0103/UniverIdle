using DesktopPet.AI;
using DesktopPet.Decor;
using UnityEngine;

namespace DesktopPet.Luby
{
    /// <summary>挂在单只 Luby 上，给 collect_coin 行为读取当前目标金币。</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PetAgent))]
    public sealed class LubyCoinCollector : MonoBehaviour
    {
        public DecorGoldCoin TargetCoin { get; private set; }

        public bool HasTarget => TargetCoin != null && TargetCoin.isActiveAndEnabled && !TargetCoin.IsCollected;

        public void BeginCollect(DecorGoldCoin coin)
        {
            TargetCoin = coin;
        }

        public void Clear()
        {
            TargetCoin = null;
        }
    }
}
