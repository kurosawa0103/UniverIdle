using System;
using DesktopPet.Save;
using DesktopPet.Shop;
using Sirenix.OdinInspector;
using UnityEngine;

namespace DesktopPet.Luby
{
    public enum LubyRollFailReason
    {
        None,
        MissingCatalog,
        MissingWallet,
        RollTemplateFailed,
        InsufficientFunds,
        SpawnFailed,
        ConflictingTraits
    }

    [Serializable]
    public struct LubyRollResult
    {
        public bool success;
        public LubyRollFailReason failReason;
        public int pricePaid;
        public LubyInstanceData instance;
        public LubyTemplateDefinition template;
        public LubyPersonalityDefinition personality;
        public LubyTraitDefinition trait;
        public LubyTraitDefinition trait2;
        /// <summary>true = 桌上已满，进了仓库；false = 已 Spawn 到场景。</summary>
        public bool sentToWarehouse;

        public string FailMessage
        {
            get
            {
                switch (failReason)
                {
                    case LubyRollFailReason.InsufficientFunds:
                        return "金币不足";
                    case LubyRollFailReason.MissingCatalog:
                        return "缺少 Luby 目录";
                    case LubyRollFailReason.MissingWallet:
                        return "缺少钱包";
                    case LubyRollFailReason.SpawnFailed:
                        return "生成失败";
                    case LubyRollFailReason.ConflictingTraits:
                        return "特质互斥，无法双发";
                    default:
                        return "抽取失败";
                }
            }
        }
    }

    /// <summary>扣款 + 盒内加权抽取 / 指定配置发放 + 进场或进仓 + 写档。Catalog 只认 LubyWorld。</summary>
    public sealed class LubyAcquisitionService : MonoBehaviour
    {
        [Title("Luby 抽取")]
        [LabelText("Luby 世界")]
        [SerializeField]
        private LubyWorld lubyWorld;

        [LabelText("商店（钱包）")]
        [SerializeField]
        private ShopManager shop;

        private LubyTemplateCatalog Catalog => lubyWorld != null ? lubyWorld.Catalog : null;

        private void Awake()
        {
            ResolveRefs();
        }

        private void OnDestroy()
        {
            DesktopPetServices.UnregisterLubyAcquisition(this);
        }

        private void ResolveRefs()
        {
            if (lubyWorld == null)
                lubyWorld = GetComponent<LubyWorld>() ?? DesktopPetServices.LubyWorld;
            if (shop == null)
                shop = DesktopPetServices.Shop;

            DesktopPetServices.RegisterLubyAcquisition(this);
        }

        /// <summary>盲盒：扣价 → 加权外形/性格/特质 → 桌上或仓库。</summary>
        public bool TryRollTemplate(LubyTemplateDefinition template, out LubyRollResult result)
        {
            if (!TryPrepare(template, out result))
                return false;

            if (shop == null || shop.Wallet == null)
            {
                result.failReason = LubyRollFailReason.MissingWallet;
                return false;
            }

            int price = Mathf.Max(0, template.rollPrice);
            if (!shop.Wallet.TrySpend(price))
            {
                result.failReason = LubyRollFailReason.InsufficientFunds;
                result.pricePaid = price;
                return false;
            }

            Catalog.RollBoxContents(
                template,
                out GameObject appearance,
                out string appearanceKey,
                out LubyPersonalityDefinition personality,
                out LubyTraitDefinition trait,
                out LubyTraitDefinition trait2);

            return TryDeliver(
                template,
                appearance,
                appearanceKey,
                personality,
                trait,
                trait2,
                price,
                refundOnFail: true,
                out result);
        }

        /// <summary>指定配置发放（不随机、不扣款），供 GM。trait2 可空；与 trait 互斥则失败。</summary>
        public bool TryGrantSpecified(
            LubyTemplateDefinition template,
            GameObject appearancePrefab,
            string appearanceKey,
            LubyPersonalityDefinition personality,
            LubyTraitDefinition trait,
            LubyTraitDefinition trait2,
            out LubyRollResult result)
        {
            if (!TryPrepare(template, out result))
                return false;

            if (trait != null && trait2 != null && trait.ConflictsForDual(trait2))
            {
                result.failReason = LubyRollFailReason.ConflictingTraits;
                return false;
            }

            GameObject appearance = appearancePrefab;
            if (appearance == null)
                appearance = template.ResolveSpawnPrefab(appearanceKey);

            string key = !string.IsNullOrEmpty(appearanceKey)
                ? appearanceKey
                : (appearance != null ? appearance.name : string.Empty);

            return TryDeliver(
                template,
                appearance,
                key,
                personality,
                trait,
                trait2,
                pricePaid: 0,
                refundOnFail: false,
                out result);
        }

        private bool TryPrepare(LubyTemplateDefinition template, out LubyRollResult result)
        {
            result = default;
            if (Catalog == null)
            {
                result.failReason = LubyRollFailReason.MissingCatalog;
                return false;
            }

            if (template == null)
            {
                result.failReason = LubyRollFailReason.RollTemplateFailed;
                return false;
            }

            result.template = template;
            if (lubyWorld == null)
            {
                result.failReason = LubyRollFailReason.SpawnFailed;
                return false;
            }

            return true;
        }

        private bool TryDeliver(
            LubyTemplateDefinition template,
            GameObject appearance,
            string appearanceKey,
            LubyPersonalityDefinition personality,
            LubyTraitDefinition trait,
            LubyTraitDefinition trait2,
            int pricePaid,
            bool refundOnFail,
            out LubyRollResult result)
        {
            result = default;
            result.template = template;
            result.pricePaid = pricePaid;

            if (appearance == null)
            {
                RefundIfNeeded(pricePaid, refundOnFail);
                result.failReason = LubyRollFailReason.RollTemplateFailed;
                Debug.LogError("[LubyAcquisition] 外形 Prefab 为空" + (refundOnFail ? "，已退款。" : "。"));
                return false;
            }

            float scale = template.ResolveScale(appearance);

            var data = new LubyInstanceData
            {
                instanceId = Guid.NewGuid().ToString("N"),
                templateId = template.templateId,
                personalityId = personality != null ? personality.personalityId : string.Empty,
                traitId = trait != null ? trait.traitId : string.Empty,
                traitId2 = trait2 != null ? trait2.traitId : string.Empty,
                petName = RollPetName(personality, appearanceKey, template),
                appearanceKey = string.IsNullOrEmpty(appearanceKey) && appearance != null
                    ? appearance.name
                    : appearanceKey,
                scale = scale
            };

            bool toWarehouse = !lubyWorld.CanSpawnOnDesk;
            if (toWarehouse)
            {
                lubyWorld.AddToWarehouse(data);
                result.sentToWarehouse = true;
            }
            else
            {
                lubyWorld.AssignRandomPosition(data);
                LubyInstanceComponent inst = lubyWorld.Spawn(data);
                if (inst == null)
                {
                    RefundIfNeeded(pricePaid, refundOnFail);
                    result.failReason = LubyRollFailReason.SpawnFailed;
                    Debug.LogError("[LubyAcquisition] Spawn 失败" + (refundOnFail ? "，已退款。" : "。"));
                    return false;
                }

                result.sentToWarehouse = false;
            }

            LubyAppearanceCodex codex = DesktopPetServices.AppearanceCodex;
            if (codex != null)
                codex.TryUnlock(data.appearanceKey);

            DesktopPetSaveMgr.PersistActive();

            result.success = true;
            result.instance = data;
            result.personality = personality;
            result.trait = trait;
            result.trait2 = trait2;
            return true;
        }

        private static string RollPetName(
            LubyPersonalityDefinition personality,
            string appearanceKey,
            LubyTemplateDefinition template)
        {
            string name = LubyNameGenerator.Roll(LubyNameCatalog.LoadDefault(), personality, appearanceKey);
            if (!string.IsNullOrEmpty(name))
                return name;
            if (template != null && !string.IsNullOrEmpty(template.displayName))
                return template.displayName;
            return "Luby";
        }

        private void RefundIfNeeded(int pricePaid, bool refundOnFail)
        {
            if (!refundOnFail || pricePaid <= 0 || shop?.Wallet == null)
                return;
            shop.Wallet.Add(pricePaid);
        }
    }
}
