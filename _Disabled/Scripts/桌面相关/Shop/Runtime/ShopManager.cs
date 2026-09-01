using System;
using DesktopPet.Decor;
using DesktopPet.Inventory;
using DesktopPet.Save;
using Sirenix.OdinInspector;
using UnityEngine;

namespace DesktopPet.Shop
{
    /// <summary>商店门面：购买校验并写入仓库；与桌宠存档联动。</summary>
    public sealed class ShopManager : MonoBehaviour
    {
        [Title("商店管理器", "购买校验 → 扣款 → 进仓库 → 写存档")]
        [InfoBox(
            "首次无 desktoppet.json 时，起始金币由 DesktopPetSaveBootstrap 按 Catalog.startingCurrency 写入钱包并首存。\n" +
            "有存档时由存档引导覆盖金币/仓库。",
            InfoMessageType.None)]

        [FoldoutGroup("引用绑定", expanded: true)]
        [LabelText("商品目录")]
        [Tooltip("可售商品列表与起始金币。")]
        [Required]
        [SerializeField]
        private ShopCatalog catalog;

        [FoldoutGroup("引用绑定")]
        [LabelText("钱包")]
        [Tooltip("玩家金币。留空则同物体上自动找 ShopWallet。")]
        [SerializeField]
        private ShopWallet wallet;

        [FoldoutGroup("引用绑定")]
        [LabelText("仓库")]
        [Tooltip("已购商品数量。通常在 InventorySystem 上；留空则用 DesktopPetServices.Inventory。")]
        [SerializeField]
        private ItemInventory inventory;

        [FoldoutGroup("运行时状态", expanded: false)]
        [ShowInInspector, ReadOnly, LabelText("当前金币")]
        private int DebugCurrency => wallet != null ? wallet.Currency : 0;

        [FoldoutGroup("运行时状态")]
        [ShowInInspector, ReadOnly, LabelText("仓库条目数")]
        private int DebugInventoryCount => inventory != null ? inventory.Entries.Count : 0;

        [FoldoutGroup("运行时状态")]
        [ShowInInspector, ReadOnly, LabelText("目录商品数")]
        private int DebugCatalogCount => catalog != null && catalog.items != null ? catalog.items.Count : 0;

        public ShopCatalog Catalog => catalog;
        public ShopWallet Wallet => wallet;
        public ItemInventory Inventory => inventory;

        public event Action<ShopItemDefinition, bool, string> PurchaseFinished;

        private void Awake()
        {
            if (DesktopPetServices.Shop != null && DesktopPetServices.Shop != this)
            {
                Debug.LogWarning("[Shop] 场景中已有 ShopManager，销毁重复实例。");
                Destroy(gameObject);
                return;
            }

            if (wallet == null)
                wallet = GetComponent<ShopWallet>();
            if (inventory == null)
                inventory = DesktopPetServices.Inventory;

            DesktopPetServices.RegisterShop(this);
        }

        private void OnDestroy()
        {
            if (DesktopPetServices.Shop == this)
                DesktopPetServices.UnregisterShop(this);
        }

        /// <summary>仓库数量 + 桌上已摆同 id 件数（与购买上限同口径）。</summary>
        public int CountOwned(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
                return 0;
            int owned = inventory != null ? inventory.GetCount(itemId) : 0;
            return owned + CountPlaced(itemId);
        }

        public bool CanBuy(ShopItemDefinition item, out string failReason)
        {
            failReason = null;
            if (item == null)
            {
                failReason = "商品无效";
                return false;
            }

            if (wallet == null || !wallet.CanAfford(item.price))
            {
                failReason = "金币不足";
                return false;
            }

            if (item.maxOwnCount > 0 && CountOwned(item.itemId) >= item.maxOwnCount)
            {
                failReason = "已达持有上限";
                return false;
            }

            return true;
        }

        private static int CountPlaced(string itemId)
        {
            DecorWorld world = DesktopPetServices.DecorWorld;
            if (world == null || string.IsNullOrEmpty(itemId))
                return 0;

            int n = 0;
            var list = world.Placed;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null && list[i].ItemId == itemId)
                    n++;
            }

            return n;
        }

        public bool TryBuy(ShopItemDefinition item)
        {
            if (!CanBuy(item, out string failReason))
            {
                PurchaseFinished?.Invoke(item, false, failReason);
                return false;
            }

            if (!wallet.TrySpend(item.price))
            {
                PurchaseFinished?.Invoke(item, false, "金币不足");
                return false;
            }

            if (inventory == null)
            {
                PurchaseFinished?.Invoke(item, false, "缺少仓库");
                Debug.LogWarning("[Shop] 无 ItemInventory，无法入库。");
                return false;
            }

            inventory.Add(item.itemId, 1);
            PurchaseFinished?.Invoke(item, true, null);
            Debug.Log($"[Shop] 购买成功：{item.displayName} x1，剩余金币 {wallet.Currency}");

            DesktopPetSaveMgr.PersistActive();

            return true;
        }
    }
}
