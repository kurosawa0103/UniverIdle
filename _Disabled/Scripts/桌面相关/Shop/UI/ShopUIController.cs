using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace DesktopPet.Shop
{
    /// <summary>商店页：商品列表按 Catalog 动态生成；壳层由 DesktopHub 管理。</summary>
    public sealed class ShopUIController : MonoBehaviour
    {
        [Title("商店页", "主面板内的购买列表")]
        [SerializeField] private ShopManager shop;
        [SerializeField] private TextMeshProUGUI statusText;
        [Required]
        [SerializeField] private Transform shopContent;
        [Required]
        [SerializeField] private ShopItemSlot shopSlotPrefab;

        private readonly List<ShopItemSlot> _shopSlots = new List<ShopItemSlot>(16);
        private int _activeSlotCount;

        private void Awake()
        {
            if (shop == null)
                shop = DesktopPetServices.Shop;

            DesktopPetServices.RegisterShopUi(this);

            if (shopContent == null)
                Debug.LogError("[ShopUI] 未绑定 shopContent。请「应用主面板预制体」。");
            if (shopSlotPrefab == null)
                Debug.LogError("[ShopUI] 未绑定 shopSlotPrefab。");
        }

        private void OnDestroy()
        {
            DesktopPetServices.UnregisterShopUi(this);
        }

        private void OnEnable()
        {
            if (shop != null)
                shop.PurchaseFinished += OnPurchaseFinished;
        }

        private void OnDisable()
        {
            if (shop != null)
                shop.PurchaseFinished -= OnPurchaseFinished;
        }

        public void OnPageShown()
        {
            SetStatus(string.Empty);
            RebuildShopList();
            DesktopPetServices.HubUi?.RefreshChrome();
        }

        private void RebuildShopList()
        {
            int used = 0;
            if (shopSlotPrefab != null && shopContent != null && shop != null && shop.Catalog != null)
            {
                foreach (ShopItemDefinition item in shop.Catalog.GetItemsForTab(ShopTabId.Decor))
                {
                    if (item == null)
                        continue;

                    ShopItemSlot slot;
                    if (used < _shopSlots.Count && _shopSlots[used] != null)
                        slot = _shopSlots[used];
                    else
                    {
                        slot = Instantiate(shopSlotPrefab, shopContent);
                        if (used < _shopSlots.Count)
                            _shopSlots[used] = slot;
                        else
                            _shopSlots.Add(slot);
                    }

                    slot.gameObject.name = "ShopItem_" + item.itemId;
                    slot.gameObject.SetActive(true);
                    slot.Bind(item, shop);
                    used++;
                }
            }

            for (int i = used; i < _shopSlots.Count; i++)
            {
                if (_shopSlots[i] != null)
                    _shopSlots[i].gameObject.SetActive(false);
            }

            _activeSlotCount = used;
        }

        private void OnPurchaseFinished(ShopItemDefinition item, bool ok, string failReason)
        {
            if (ok)
            {
                SetStatus($"已购买：{item.displayName}");
                DesktopPetServices.InventoryUi?.RefreshIfOpen();
                DesktopPetServices.HubUi?.RefreshChrome();
            }
            else if (item != null && !string.IsNullOrEmpty(failReason))
                SetStatus($"{item.displayName}：{failReason}");
            else
                SetStatus(item != null ? $"无法购买：{item.displayName}" : "购买失败");

            for (int i = 0; i < _activeSlotCount; i++)
            {
                if (_shopSlots[i] != null)
                    _shopSlots[i].RefreshView();
            }
        }

        private void SetStatus(string msg)
        {
            if (statusText != null)
                statusText.text = msg;
        }
    }
}
