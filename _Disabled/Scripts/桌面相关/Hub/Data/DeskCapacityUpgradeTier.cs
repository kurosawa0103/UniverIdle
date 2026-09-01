using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace DesktopPet.Hub
{
    [Serializable]
    public struct DeskCapacityUpgradeTier
    {
        [LabelText("金币")]
        [MinValue(1)]
        public int goldCost;

        [LabelText("解锁栏位")]
        [MinValue(1)]
        public int slotGain;
    }
}

