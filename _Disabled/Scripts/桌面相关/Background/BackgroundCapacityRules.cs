using System.Collections.Generic;
using DesktopPet.Decor;
using DesktopPet.Hub;
using DesktopPet.Luby;
using UnityEngine;

namespace DesktopPet.Background
{
    /// <summary>背景容量：读 BackgroundDefinition 内嵌阶梯；无 Definition（透明桌）时用 DecorWorld/LubyWorld 硬编码默认。</summary>
    public static class BackgroundCapacityRules
    {
        public static int GetDecorInitial(BackgroundDefinition def)
        {
            if (def != null)
                return def.GetDecorInitialCapacity();
            return DecorWorld.ResolveInitialDeskCapacity();
        }

        public static int GetLubyInitial(BackgroundDefinition def)
        {
            if (def != null)
                return def.GetLubyInitialCapacity();
            return LubyWorld.ResolveInitialDeskCapacity();
        }

        public static int GetDecorMax(BackgroundDefinition def)
        {
            if (def != null)
                return def.GetDecorMaxCapacity();
            return DecorWorld.ResolveMaxDeskCapacity();
        }

        public static int GetLubyMax(BackgroundDefinition def)
        {
            if (def != null)
                return def.GetLubyMaxCapacity();
            return LubyWorld.ResolveMaxDeskCapacity();
        }

        public static bool TryGetDecorTier(BackgroundDefinition def, int level, out DeskCapacityUpgradeTier tier)
        {
            tier = default;
            return def != null && def.TryGetDecorTier(level, out tier);
        }

        public static bool TryGetLubyTier(BackgroundDefinition def, int level, out DeskCapacityUpgradeTier tier)
        {
            tier = default;
            return def != null && def.TryGetLubyTier(level, out tier);
        }

        public static int DecorTierCount(BackgroundDefinition def)
        {
            return def != null ? def.DecorTierCount : 0;
        }

        public static int LubyTierCount(BackgroundDefinition def)
        {
            return def != null ? def.LubyTierCount : 0;
        }

        public static int CountDecorUpgradeLevel(BackgroundDefinition def, int capacity)
        {
            return CountAppliedTiers(
                capacity,
                GetDecorInitial(def),
                GetDecorMax(def),
                def != null ? def.decorTiers : null);
        }

        public static int CountLubyUpgradeLevel(BackgroundDefinition def, int capacity)
        {
            return CountAppliedTiers(
                capacity,
                GetLubyInitial(def),
                GetLubyMax(def),
                def != null ? def.lubyTiers : null);
        }

        public static int ClampDecorCapacity(BackgroundDefinition def, int capacity)
        {
            return Mathf.Clamp(capacity, GetDecorInitial(def), GetDecorMax(def));
        }

        public static int ClampLubyCapacity(BackgroundDefinition def, int capacity)
        {
            return Mathf.Clamp(capacity, GetLubyInitial(def), GetLubyMax(def));
        }

        public static int DecorUpgradeGain(BackgroundDefinition def, int capacity, int level)
        {
            if (!TryGetDecorTier(def, level, out DeskCapacityUpgradeTier tier))
                return 0;

            return ClampGain(capacity, GetDecorMax(def), tier.slotGain);
        }

        public static int LubyUpgradeGain(BackgroundDefinition def, int capacity, int level)
        {
            if (!TryGetLubyTier(def, level, out DeskCapacityUpgradeTier tier))
                return 0;

            return ClampGain(capacity, GetLubyMax(def), tier.slotGain);
        }

        private static int CountAppliedTiers(
            int currentCapacity,
            int initialCapacity,
            int maxCapacity,
            List<DeskCapacityUpgradeTier> tiers)
        {
            if (tiers == null || tiers.Count == 0)
                return Mathf.Max(0, currentCapacity - initialCapacity);

            int simulated = initialCapacity;
            int level = 0;
            for (int i = 0; i < tiers.Count; i++)
            {
                if (simulated >= maxCapacity)
                    break;

                DeskCapacityUpgradeTier tier = tiers[i];
                int gain = Mathf.Max(1, tier.slotGain);
                int next = Mathf.Min(simulated + gain, maxCapacity);
                if (currentCapacity >= next && next > simulated)
                {
                    level++;
                    simulated = next;
                }
                else
                {
                    break;
                }
            }

            return level;
        }

        private static int ClampGain(int currentCapacity, int maxCapacity, int requestedGain)
        {
            if (currentCapacity >= maxCapacity)
                return 0;
            int gain = Mathf.Max(1, requestedGain);
            return Mathf.Min(gain, maxCapacity - currentCapacity);
        }
    }
}
