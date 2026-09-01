using System.Collections.Generic;
using DesktopPet.Save;
using DesktopPet.Shop;
using UnityEngine;

namespace DesktopPet.Background
{
    /// <summary>
    /// 背景购买/解锁服务。
    /// 背景不进仓库，购买后直接记录 unlockedBackgrounds 并可立即切换。
    /// </summary>
    public static class BackgroundShopService
    {
        /// <summary>返回背景是否已解锁（透明背景始终解锁）。</summary>
        public static bool IsUnlocked(string backgroundId)
        {
            if (string.IsNullOrEmpty(backgroundId))
                return true;

            BackgroundCatalog catalog = BackgroundSystem.Instance?.Catalog;
            if (catalog != null)
            {
                BackgroundDefinition def = catalog.FindById(backgroundId);
                if (def != null && def.defaultUnlocked)
                    return true;
            }

            List<string> list = DesktopPetSaveMgr.Current?.unlockedBackgrounds;
            if (list == null)
                return false;

            return list.Contains(backgroundId);
        }

        /// <summary>
        /// 尝试购买并解锁背景。
        /// 已解锁则直接返回 true；价格 0 免费解锁。
        /// </summary>
        public static bool TryPurchase(BackgroundDefinition def, ShopWallet wallet)
        {
            if (def == null)
                return false;

            if (IsUnlocked(def.backgroundId))
                return true;

            if (def.price > 0)
            {
                if (wallet == null || !wallet.TrySpend(def.price))
                {
                    Debug.Log($"[BackgroundShop] 余额不足，无法购买背景「{def.displayName}」（需 {def.price} 金币）。");
                    return false;
                }
            }

            Unlock(def.backgroundId);
            Debug.Log($"[BackgroundShop] 已解锁背景「{def.displayName}」。");
            return true;
        }

        /// <summary>直接解锁（GM / 赠送用）。</summary>
        public static void Unlock(string backgroundId)
        {
            if (string.IsNullOrEmpty(backgroundId))
                return;

            List<string> list = DesktopPetSaveMgr.Current?.unlockedBackgrounds;
            if (list == null)
                return;

            if (!list.Contains(backgroundId))
                list.Add(backgroundId);
        }
    }
}
