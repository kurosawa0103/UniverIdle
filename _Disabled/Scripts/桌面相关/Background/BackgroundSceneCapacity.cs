using DesktopPet.Decor;
using DesktopPet.Luby;
using DesktopPet.Save;
using DesktopPet.Shop;

namespace DesktopPet.Background
{
    /// <summary>读写各背景 scene 槽的桌上容量，并同步到运行时。</summary>
    public static class BackgroundSceneCapacity
    {
        public static int GetDecorCapacity(string backgroundId, BackgroundDefinition def)
        {
            DesktopPetScenePlaced scene = DesktopPetSaveMgr.Current.GetOrCreateScene(backgroundId);
            int value = scene.decorDeskCapacity > 0
                ? scene.decorDeskCapacity
                : BackgroundCapacityRules.GetDecorInitial(def);
            return BackgroundCapacityRules.ClampDecorCapacity(def, value);
        }

        public static int GetLubyCapacity(string backgroundId, BackgroundDefinition def)
        {
            DesktopPetScenePlaced scene = DesktopPetSaveMgr.Current.GetOrCreateScene(backgroundId);
            int value = scene.lubyDeskCapacity > 0
                ? scene.lubyDeskCapacity
                : BackgroundCapacityRules.GetLubyInitial(def);
            return BackgroundCapacityRules.ClampLubyCapacity(def, value);
        }

        public static void SetDecorCapacity(string backgroundId, BackgroundDefinition def, int capacity)
        {
            DesktopPetScenePlaced scene = DesktopPetSaveMgr.Current.GetOrCreateScene(backgroundId);
            scene.decorDeskCapacity = BackgroundCapacityRules.ClampDecorCapacity(def, capacity);
        }

        public static void SetLubyCapacity(string backgroundId, BackgroundDefinition def, int capacity)
        {
            DesktopPetScenePlaced scene = DesktopPetSaveMgr.Current.GetOrCreateScene(backgroundId);
            scene.lubyDeskCapacity = BackgroundCapacityRules.ClampLubyCapacity(def, capacity);
        }

        public static void CaptureFromRuntime(
            string backgroundId,
            DecorWorld decor = null,
            LubyWorld luby = null)
        {
            if (DesktopPetSaveMgr.Current == null)
                return;

            DesktopPetScenePlaced scene = DesktopPetSaveMgr.Current.GetOrCreateScene(backgroundId);
            decor ??= DesktopPetServices.DecorWorld;
            luby ??= DesktopPetServices.LubyWorld;
            if (decor != null)
                scene.decorDeskCapacity = decor.DeskCapacity;
            if (luby != null)
                scene.lubyDeskCapacity = luby.DeskCapacity;
        }

        public static void ApplyToRuntime(string backgroundId, BackgroundDefinition def)
        {
            int decorCap = GetDecorCapacity(backgroundId, def);
            int lubyCap = GetLubyCapacity(backgroundId, def);

            DecorWorld decor = DesktopPetServices.DecorWorld;
            if (decor != null)
                decor.SetDeskCapacity(decorCap);

            LubyWorld luby = DesktopPetServices.LubyWorld;
            if (luby != null)
                luby.SetDeskCapacity(lubyCap);

        }

        public static bool TryUpgradeDecor(
            string backgroundId,
            BackgroundDefinition def,
            ShopWallet wallet,
            out string error)
        {
            return TryUpgrade(backgroundId, def, wallet, decor: true, out error);
        }

        public static bool TryUpgradeLuby(
            string backgroundId,
            BackgroundDefinition def,
            ShopWallet wallet,
            out string error)
        {
            return TryUpgrade(backgroundId, def, wallet, decor: false, out error);
        }

        private static bool TryUpgrade(
            string backgroundId,
            BackgroundDefinition def,
            ShopWallet wallet,
            bool decor,
            out string error)
        {
            error = null;
            if (def == null || wallet == null)
            {
                error = "系统未就绪";
                return false;
            }

            int capacity = decor
                ? GetDecorCapacity(backgroundId, def)
                : GetLubyCapacity(backgroundId, def);
            int level = decor
                ? BackgroundCapacityRules.CountDecorUpgradeLevel(def, capacity)
                : BackgroundCapacityRules.CountLubyUpgradeLevel(def, capacity);
            int tierCount = decor
                ? BackgroundCapacityRules.DecorTierCount(def)
                : BackgroundCapacityRules.LubyTierCount(def);
            if (level >= tierCount)
            {
                error = decor ? "装饰容量已满" : "Luby 容量已满";
                return false;
            }

            bool hasTier = decor
                ? BackgroundCapacityRules.TryGetDecorTier(def, level, out Hub.DeskCapacityUpgradeTier tier)
                : BackgroundCapacityRules.TryGetLubyTier(def, level, out tier);
            if (!hasTier)
            {
                error = "无可用升级";
                return false;
            }

            int gain = decor
                ? BackgroundCapacityRules.DecorUpgradeGain(def, capacity, level)
                : BackgroundCapacityRules.LubyUpgradeGain(def, capacity, level);
            if (gain <= 0)
            {
                error = "已达上限";
                return false;
            }

            if (!wallet.TrySpend(tier.goldCost))
            {
                error = "金币不足";
                return false;
            }

            int next = capacity + gain;
            if (decor)
                SetDecorCapacity(backgroundId, def, next);
            else
                SetLubyCapacity(backgroundId, def, next);
            if (IsActiveBackground(backgroundId))
                ApplyToRuntime(backgroundId, def);

            DesktopPetSaveMgr.PersistActive();
            DesktopPetServices.HubUi?.RefreshChrome();
            return true;
        }

        private static bool IsActiveBackground(string backgroundId)
        {
            return BackgroundSystem.Instance != null
                && BackgroundSystem.Instance.CurrentBackgroundId == backgroundId;
        }
    }
}
