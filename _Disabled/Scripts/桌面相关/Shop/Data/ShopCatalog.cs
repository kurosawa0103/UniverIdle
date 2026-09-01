using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace DesktopPet.Shop
{
    [CreateAssetMenu(menuName = "桌宠/商店/目录", fileName = "ShopCatalog")]
    public sealed class ShopCatalog : ScriptableObject
    {
        [Title("商店目录", "可售商品列表 + 首次起始金币")]
        [InfoBox("起始金币仅在无存档时由 ShopManager 写入钱包。", InfoMessageType.None)]

        [BoxGroup("基础")]
        [LabelText("起始金币")]
        [Tooltip("首次无 desktoppet.json 时写入钱包。")]
        [MinValue(0)]
        public int startingCurrency = 100;

        [BoxGroup("商品")]
        [LabelText("商品列表")]
        [ListDrawerSettings(ShowFoldout = true, DraggableItems = true, ShowIndexLabels = true)]
        [AssetSelector(Paths = "Assets/Resources/GameData/ShopItemData")]
        public List<ShopItemDefinition> items = new List<ShopItemDefinition>();

        [BoxGroup("商品")]
        [ShowInInspector, ReadOnly, LabelText("商品数量")]
        private int DebugItemCount => items != null ? items.Count : 0;

        public IEnumerable<ShopItemDefinition> GetItemsForTab(ShopTabId tab)
        {
            if (items == null)
                yield break;

            for (int i = 0; i < items.Count; i++)
            {
                ShopItemDefinition item = items[i];
                if (item != null && item.tab == tab)
                    yield return item;
            }
        }

        public ShopItemDefinition FindById(string itemId)
        {
            if (items == null || string.IsNullOrEmpty(itemId))
                return null;

            for (int i = 0; i < items.Count; i++)
            {
                ShopItemDefinition item = items[i];
                if (item != null && item.itemId == itemId)
                    return item;
            }

            return null;
        }
    }
}
