using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace DesktopPet.Shop
{
    /// <summary>玩家货币。</summary>
    public sealed class ShopWallet : MonoBehaviour
    {
        [Title("商店钱包", "玩家金币；购买扣款，GM/存档可改")]
        [InfoBox(
            "无档开局金币以 ShopCatalog.startingCurrency 为准（DesktopPetSaveBootstrap 写入）。\n" +
            "有存档时以 desktoppet.json 为准。Inspector「当前金币」勿当起始配置。",
            InfoMessageType.None)]

        [LabelText("当前金币")]
        [MinValue(0)]
        [SerializeField]
        private int currency;

        public int Currency => currency;

        public event Action<int> CurrencyChanged;

        public void SetCurrency(int value)
        {
            currency = Mathf.Max(0, value);
            CurrencyChanged?.Invoke(currency);
        }

        public bool CanAfford(int price) => currency >= price;

        public bool TrySpend(int price)
        {
            if (price < 0 || currency < price)
                return false;

            currency -= price;
            CurrencyChanged?.Invoke(currency);
            return true;
        }

        public void Add(int amount)
        {
            if (amount <= 0)
                return;
            currency += amount;
            CurrencyChanged?.Invoke(currency);
        }
    }
}
