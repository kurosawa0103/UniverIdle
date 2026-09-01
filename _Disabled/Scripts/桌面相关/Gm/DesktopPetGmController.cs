using System.Collections.Generic;
using DesktopPet.Background;
using DesktopPet.Decor;
using DesktopPet.Luby;
using DesktopPet.Save;
using DesktopPet.Shop;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DesktopPet.Gm
{
    /// <summary>
    /// 桌宠运行时 GM：加钱、重置存档、指定获得 Luby 等。
    /// OpenGmBtn 贴菜单左侧；GmPanel 居中大面板（打包后可用）。
    /// </summary>
    public sealed class DesktopPetGmController : MonoBehaviour
    {
        [SerializeField] private ShopManager shop;
        [SerializeField] private DecorWorld world;
        [SerializeField] private LubyWorld lubyWorld;
        [SerializeField] private LubyAcquisitionService lubyAcquisition;

        private enum GmSubTab
        {
            Money = 0,
            Decor = 1,
            Luby = 2,
            Save = 3
        }

        [Header("场景 UI 绑定（MainCanvas）")]
        [SerializeField] private GameObject rootPanel;
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button add100Button;
        [SerializeField] private Button add1000Button;
        [SerializeField] private Button resetSaveButton;
        [SerializeField] private Button clearPlacedButton;
        [SerializeField] private Button grantAllButton;
        [SerializeField] private Button clearLubiesButton;
        [SerializeField] private Button removeLastLubyButton;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI currencyText;
        [SerializeField] private TextMeshProUGUI lubyCountText;

        [Header("分页签")]
        [SerializeField] private Button tabMoneyButton;
        [SerializeField] private Button tabDecorButton;
        [SerializeField] private Button tabLubyButton;
        [SerializeField] private Button tabSaveButton;
        [SerializeField] private GameObject pageMoney;
        [SerializeField] private GameObject pageDecor;
        [SerializeField] private GameObject pageLuby;
        [SerializeField] private GameObject pageSave;

        [Header("指定获得")]
        [SerializeField] private GmCycleRow grantTemplateRow;
        [SerializeField] private GmCycleRow grantAppearanceRow;
        [SerializeField] private GmCycleRow grantPersonalityRow;
        [SerializeField] private GmCycleRow grantTraitRow;
        [SerializeField] private GmCycleRow grantTrait2Row;
        [SerializeField] private Button grantSpecifiedButton;

        [SerializeField] private bool startOpen;

        private GmSubTab _subTab = GmSubTab.Money;

        private readonly List<LubyTemplateDefinition> _templates = new List<LubyTemplateDefinition>();
        private readonly List<string> _templateLabels = new List<string>();
        private readonly List<GameObject> _appearances = new List<GameObject>();
        private readonly List<string> _appearanceLabels = new List<string>();
        private readonly List<LubyPersonalityDefinition> _personalities = new List<LubyPersonalityDefinition>();
        private readonly List<string> _personalityLabels = new List<string>();
        private readonly List<LubyTraitDefinition> _traits = new List<LubyTraitDefinition>();
        private readonly List<string> _traitLabels = new List<string>();

        private int _templateIndex;
        private int _appearanceIndex;
        private int _personalityIndex;
        private int _traitIndex;
        private int _trait2Index;

        private void Awake()
        {
            ResolveRefs();
            if (openButton == null || rootPanel == null)
                Debug.LogError("[DesktopPetGM] 未绑定 OpenGmBtn / GmPanel。请手改 MainCanvas.prefab 后「应用主面板预制体」。");
            WarnIfTabsMissing();
            WireButtons();
            SetSubTab(GmSubTab.Money);
        }

        private void OnEnable()
        {
            if (shop != null && shop.Wallet != null)
                shop.Wallet.CurrencyChanged += OnCurrencyChanged;
        }

        private void OnDisable()
        {
            if (shop != null && shop.Wallet != null)
                shop.Wallet.CurrencyChanged -= OnCurrencyChanged;
        }

        private void Start()
        {
            if (startOpen)
                Open();
            else
            {
                Close();
                RefreshChromeLabels();
            }
        }

        private void ResolveRefs()
        {
            if (shop == null)
                shop = DesktopPetServices.Shop;
            if (world == null)
                world = DesktopPetServices.DecorWorld;
            if (lubyWorld == null)
                lubyWorld = DesktopPetServices.LubyWorld;
            if (lubyAcquisition == null)
                lubyAcquisition = DesktopPetServices.LubyAcquisition
                                 ?? (lubyWorld != null ? lubyWorld.GetComponent<LubyAcquisitionService>() : null);
        }

        private static void CancelActiveHolds()
        {
            if (DesktopPetServices.IsAnyPlacementHolding())
            {
                if (DesktopPetServices.Placement != null && DesktopPetServices.Placement.IsHolding)
                    DesktopPetServices.Placement.CancelHold();
                if (DesktopPetServices.LubyPlacement != null && DesktopPetServices.LubyPlacement.IsHolding)
                    DesktopPetServices.LubyPlacement.CancelHold();
            }
        }

        public void Open()
        {
            RebuildGrantOptions();
            if (rootPanel != null)
            {
                rootPanel.SetActive(true);
                rootPanel.transform.SetAsLastSibling();
            }

            SetSubTab(GmSubTab.Money);
            RefreshChromeLabels();
        }

        public void Toggle()
        {
            if (IsOpen)
                Close();
            else
                Open();
        }

        public bool IsOpen => rootPanel != null && rootPanel.activeSelf;

        public void Close()
        {
            if (rootPanel != null)
                rootPanel.SetActive(false);
        }

        public void AddMoney(int amount)
        {
            if (shop == null || shop.Wallet == null)
            {
                SetStatus("无钱包");
                return;
            }

            shop.Wallet.Add(amount);
            DesktopPetSaveMgr.PersistActive();
            SetStatus($"+{amount} 金币");
            RefreshChromeLabels();
        }

        public void ResetSave()
        {
            CancelActiveHolds();

            DesktopPetSaveMgr.DeleteSaveFile();

            if (world != null)
                world.ClearAll();

            if (lubyWorld != null)
                lubyWorld.ClearAll();

            LubyAppearanceCodex codex = DesktopPetServices.AppearanceCodex;
            if (codex != null)
                codex.Clear();

            DesktopPetSaveMgr.ResetBackgroundProgress();

            if (shop != null)
            {
                if (shop.Inventory != null)
                    shop.Inventory.Clear();

                int start = 100;
                if (shop.Catalog != null)
                    start = shop.Catalog.startingCurrency;

                if (shop.Wallet != null)
                    shop.Wallet.SetCurrency(start);
            }

            DesktopPetSaveMgr.PersistActive();
            SetStatus("存档已重置");
            RefreshChromeLabels();
            Debug.Log("[DesktopPetGM] 存档已删除并重置运行时。");
        }

        public void ClearPlacedDecors()
        {
            CancelActiveHolds();

            if (world != null)
                world.ClearAll();

            DesktopPetSaveMgr.PersistActive();
            SetStatus("已清空桌上装饰");
            RefreshChromeLabels();
        }

        public void GrantAllDecorItems()
        {
            if (shop == null || shop.Catalog == null || shop.Inventory == null)
            {
                SetStatus("无商店目录");
                return;
            }

            int n = 0;
            foreach (ShopItemDefinition item in shop.Catalog.GetItemsForTab(ShopTabId.Decor))
            {
                if (item == null)
                    continue;
                shop.Inventory.Add(item.itemId, 1);
                n++;
            }

            DesktopPetSaveMgr.PersistActive();
            SetStatus($"已发放装饰 x{n}");
            RefreshChromeLabels();
        }

        public void ClearAllLubies()
        {
            if (lubyWorld == null)
            {
                SetStatus("无 LubyWorld");
                return;
            }

            int n = lubyWorld.Count;
            lubyWorld.ClearAll();
            DesktopPetSaveMgr.PersistActive();
            SetStatus(n > 0 ? $"已移除全部 Luby x{n}" : "桌面无 Luby");
            RefreshChromeLabels();
        }

        public void RemoveLastLuby()
        {
            if (lubyWorld == null)
            {
                SetStatus("无 LubyWorld");
                return;
            }

            if (!lubyWorld.RemoveLast())
            {
                SetStatus("桌面无 Luby");
                RefreshChromeLabels();
                return;
            }

            DesktopPetSaveMgr.PersistActive();
            SetStatus($"已移除最近一只，剩余 {lubyWorld.Count}");
            RefreshChromeLabels();
        }

        /// <summary>GM：按配置指定发放一只 Luby（免费；桌上满则进仓）。</summary>
        public bool GrantSpecifiedLuby(
            LubyTemplateDefinition template,
            GameObject appearancePrefab,
            string appearanceKey,
            LubyPersonalityDefinition personality,
            LubyTraitDefinition trait,
            LubyTraitDefinition trait2 = null)
        {
            if (lubyAcquisition == null)
            {
                SetStatus("无 LubyAcquisition");
                return false;
            }

            if (!lubyAcquisition.TryGrantSpecified(
                    template,
                    appearancePrefab,
                    appearanceKey,
                    personality,
                    trait,
                    trait2,
                    out LubyRollResult result))
            {
                SetStatus(result.FailMessage);
                RefreshChromeLabels();
                return false;
            }

            string name = LubyDisplayNames.ResolvePetName(result.instance, lubyWorld?.Catalog);
            string where = result.sentToWarehouse ? "进仓库" : "已上场";
            string traits = LubyTraitDisplay.FormatNames(result.trait, result.trait2);
            SetStatus($"指定获得 {name}（{where}｜{traits}）");
            RefreshChromeLabels();
            return true;
        }

        public void GrantSpecifiedFromPanel()
        {
            RebuildGrantOptions();
            if (_templates.Count == 0)
            {
                SetStatus("无可用模板");
                return;
            }

            LubyTemplateDefinition template = _templates[_templateIndex];
            GameObject appearance = _appearances.Count > 0 ? _appearances[_appearanceIndex] : null;
            string appearanceKey = appearance != null ? appearance.name : string.Empty;
            LubyPersonalityDefinition personality =
                _personalities.Count > 0 ? _personalities[_personalityIndex] : null;
            LubyTraitDefinition trait = _traits.Count > 0 ? _traits[_traitIndex] : null;
            LubyTraitDefinition trait2 = _traits.Count > 0 ? _traits[_trait2Index] : null;
            GrantSpecifiedLuby(template, appearance, appearanceKey, personality, trait, trait2);
        }

        private void OnCurrencyChanged(int _)
        {
            RefreshChromeLabels();
        }

        private void RefreshChromeLabels()
        {
            if (currencyText != null)
            {
                int c = shop != null && shop.Wallet != null ? shop.Wallet.Currency : 0;
                currencyText.text = $"金币：{c}";
            }

            if (lubyCountText != null)
            {
                int n = lubyWorld != null ? lubyWorld.Count : 0;
                lubyCountText.text = $"Luby：{n}";
            }
        }

        private void SetStatus(string msg)
        {
            if (statusText != null)
                statusText.text = msg;
        }

        private void RebuildGrantOptions()
        {
            LubyTemplateCatalog catalog = lubyWorld != null ? lubyWorld.Catalog : LubyTemplateCatalog.LoadDefault();
            DesktopPetGmGrantCatalog.CollectTemplates(catalog, _templates, _templateLabels);
            if (_templates.Count == 0)
            {
                _templateIndex = 0;
                _appearances.Clear();
                _appearanceLabels.Clear();
                _personalities.Clear();
                _personalityLabels.Clear();
                _traits.Clear();
                _traitLabels.Clear();
                RefreshGrantLabels();
                return;
            }

            _templateIndex = Mathf.Clamp(_templateIndex, 0, _templates.Count - 1);
            LubyTemplateDefinition template = _templates[_templateIndex];
            DesktopPetGmGrantCatalog.CollectAppearances(template, _appearances, _appearanceLabels);
            _appearanceIndex = _appearances.Count == 0 ? 0 : Mathf.Clamp(_appearanceIndex, 0, _appearances.Count - 1);

            DesktopPetGmGrantCatalog.CollectPersonalities(
                catalog, template, _personalities, _personalityLabels, includeNone: true);
            _personalityIndex = Mathf.Clamp(_personalityIndex, 0, Mathf.Max(0, _personalities.Count - 1));

            DesktopPetGmGrantCatalog.CollectTraits(
                catalog, template, _traits, _traitLabels, includeNone: true);
            _traitIndex = Mathf.Clamp(_traitIndex, 0, Mathf.Max(0, _traits.Count - 1));
            _trait2Index = Mathf.Clamp(_trait2Index, 0, Mathf.Max(0, _traits.Count - 1));

            RefreshGrantLabels();
        }

        private void RefreshGrantLabels()
        {
            SetRowLabel(grantTemplateRow, _templateLabels, _templateIndex, "无模板");
            SetRowLabel(grantAppearanceRow, _appearanceLabels, _appearanceIndex, "默认外形");
            SetRowLabel(grantPersonalityRow, _personalityLabels, _personalityIndex, "（无）");
            SetRowLabel(grantTraitRow, _traitLabels, _traitIndex, "（无）");
            SetRowLabel(grantTrait2Row, _traitLabels, _trait2Index, "（无）");
        }

        private static void SetRowLabel(GmCycleRow row, List<string> labels, int index, string empty)
        {
            if (row == null)
                return;
            if (labels == null || labels.Count == 0)
            {
                row.SetLabel(empty);
                return;
            }

            row.SetLabel(labels[Mathf.Clamp(index, 0, labels.Count - 1)]);
        }

        private void CycleTemplate(int delta)
        {
            if (_templates.Count == 0)
            {
                RebuildGrantOptions();
                return;
            }

            _templateIndex = Wrap(_templateIndex + delta, _templates.Count);
            _appearanceIndex = 0;
            _personalityIndex = 0;
            _traitIndex = 0;
            _trait2Index = 0;
            RebuildGrantOptions();
        }

        private void CycleIndex(ref int index, int count, int delta)
        {
            if (count <= 0)
                return;
            index = Wrap(index + delta, count);
            RefreshGrantLabels();
        }

        private static int Wrap(int index, int count)
        {
            if (count <= 0)
                return 0;
            int m = index % count;
            return m < 0 ? m + count : m;
        }

        private void WarnIfTabsMissing()
        {
            if (tabMoneyButton != null && tabDecorButton != null && tabLubyButton != null && tabSaveButton != null
                && pageMoney != null && pageDecor != null && pageLuby != null && pageSave != null)
                return;

            Debug.LogError(
                "[DesktopPetGM] 未绑定 GmPanel 分页签（SubTabs / Pages）。"
                + "请手改 MainCanvas.prefab 的 GmPanel 后「应用主面板预制体」。");
        }

        private void SetSubTab(GmSubTab tab)
        {
            _subTab = tab;
            if (pageMoney != null)
                pageMoney.SetActive(tab == GmSubTab.Money);
            if (pageDecor != null)
                pageDecor.SetActive(tab == GmSubTab.Decor);
            if (pageLuby != null)
                pageLuby.SetActive(tab == GmSubTab.Luby);
            if (pageSave != null)
                pageSave.SetActive(tab == GmSubTab.Save);

            DesktopPetTabVisual.Apply(tabMoneyButton, tab == GmSubTab.Money);
            DesktopPetTabVisual.Apply(tabDecorButton, tab == GmSubTab.Decor);
            DesktopPetTabVisual.Apply(tabLubyButton, tab == GmSubTab.Luby);
            DesktopPetTabVisual.Apply(tabSaveButton, tab == GmSubTab.Save);
        }

        private void WireButtons()
        {
            Wire(openButton, Toggle);
            Wire(closeButton, Close);
            Wire(add100Button, () => AddMoney(100));
            Wire(add1000Button, () => AddMoney(1000));
            Wire(resetSaveButton, ResetSave);
            Wire(clearPlacedButton, ClearPlacedDecors);
            Wire(grantAllButton, GrantAllDecorItems);
            Wire(clearLubiesButton, ClearAllLubies);
            Wire(removeLastLubyButton, RemoveLastLuby);

            Wire(tabMoneyButton, () => SetSubTab(GmSubTab.Money));
            Wire(tabDecorButton, () => SetSubTab(GmSubTab.Decor));
            Wire(tabLubyButton, () => SetSubTab(GmSubTab.Luby));
            Wire(tabSaveButton, () => SetSubTab(GmSubTab.Save));

            if (grantTemplateRow != null)
                grantTemplateRow.Wire(() => CycleTemplate(-1), () => CycleTemplate(1));
            if (grantAppearanceRow != null)
                grantAppearanceRow.Wire(() => CycleAppearance(-1), () => CycleAppearance(1));
            if (grantPersonalityRow != null)
                grantPersonalityRow.Wire(() => CyclePersonality(-1), () => CyclePersonality(1));
            if (grantTraitRow != null)
                grantTraitRow.Wire(() => CycleTrait(-1), () => CycleTrait(1));
            if (grantTrait2Row != null)
                grantTrait2Row.Wire(() => CycleTrait2(-1), () => CycleTrait2(1));
            Wire(grantSpecifiedButton, GrantSpecifiedFromPanel);
        }

        private void CycleAppearance(int delta) =>
            CycleIndex(ref _appearanceIndex, _appearances.Count, delta);

        private void CyclePersonality(int delta) =>
            CycleIndex(ref _personalityIndex, _personalities.Count, delta);

        private void CycleTrait(int delta) =>
            CycleIndex(ref _traitIndex, _traits.Count, delta);

        private void CycleTrait2(int delta) =>
            CycleIndex(ref _trait2Index, _traits.Count, delta);

        private static void Wire(Button btn, UnityEngine.Events.UnityAction action)
        {
            if (btn == null)
                return;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(action);
        }
    }
}
