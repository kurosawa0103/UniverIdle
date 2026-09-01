using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DesktopPet.Shop
{
    /// <summary>商店商品格：图标 / 名称 / 价格，底部购买按钮；由 Catalog 动态生成。</summary>
    public sealed class ShopItemSlot : MonoBehaviour
    {
        [Title("商店商品格", "网格卡 · 购买在下方")]
        [BoxGroup("数据")]
        [LabelText("商品")]
        [SerializeField]
        private ShopItemDefinition item;

        [BoxGroup("数据")]
        [LabelText("商店管理器")]
        [Tooltip("由 ShopUI Bind 注入。")]
        [SerializeField]
        private ShopManager shop;

        [BoxGroup("UI 绑定")]
        [LabelText("名称文本")]
        [SerializeField]
        private TextMeshProUGUI nameText;

        [BoxGroup("UI 绑定")]
        [LabelText("价格文本")]
        [SerializeField]
        private TextMeshProUGUI priceText;

        [BoxGroup("UI 绑定")]
        [LabelText("已拥有文本")]
        [SerializeField]
        private TextMeshProUGUI ownedText;

        [BoxGroup("UI 绑定")]
        [LabelText("图标")]
        [SerializeField]
        private Image iconImage;

        [BoxGroup("UI 绑定")]
        [LabelText("购买按钮")]
        [SerializeField]
        private Button buyButton;

        /// <summary>绑定商品并刷新显示（动态列表用）。</summary>
        public void Bind(ShopItemDefinition definition, ShopManager manager)
        {
            item = definition;
            shop = manager;
            if (buyButton != null)
            {
                buyButton.onClick.RemoveListener(OnBuyClicked);
                buyButton.onClick.AddListener(OnBuyClicked);
            }
            ApplyStaticLabels();
            RefreshView();
        }

        private void ApplyStaticLabels()
        {
            if (item == null)
                return;

            if (nameText != null)
                nameText.text = item.displayName;
            if (priceText != null)
                priceText.text = item.price.ToString();
            if (iconImage != null)
            {
                iconImage.sprite = item.icon;
                iconImage.enabled = item.icon != null;
                iconImage.preserveAspect = true;
                iconImage.color = Color.white;
            }
        }

        public void RefreshView()
        {
            if (item == null)
                return;

            int owned = shop != null ? shop.CountOwned(item.itemId) : 0;

            if (ownedText != null)
                ownedText.text = $"持有 {owned}";

            if (buyButton != null)
                buyButton.interactable = shop != null && shop.CanBuy(item, out _);
        }

        private void OnBuyClicked()
        {
            if (shop == null || item == null)
                return;
            shop.TryBuy(item);
        }
    }
}
