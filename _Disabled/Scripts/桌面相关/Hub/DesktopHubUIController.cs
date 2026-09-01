using DesktopPet.Decor;
using DesktopPet.Inventory;
using DesktopPet.Luby;
using DesktopPet.Shop;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DesktopPet.Hub
{
    /// <summary>
    /// 主面板壳：单一菜单打开，顶栏页签切换商店/仓库/领养/图鉴/设置。
    /// </summary>
    [DefaultExecutionOrder(50)]
    public sealed class DesktopHubUIController : MonoBehaviour
    {
        [Title("主面板", "一个菜单按钮 · 顶栏分页")]
        [InfoBox("引用由「应用主面板预制体」接线；Capacity Text 须预写四占位模板。", InfoMessageType.None)]

        [BoxGroup("壳层")]
        [Required]
        [LabelText("DesktopHubPanel")]
        [SerializeField]
        private GameObject rootPanel;

        [BoxGroup("壳层")]
        [LabelText("打开菜单")]
        [SerializeField]
        private Button openMenuButton;

        [BoxGroup("壳层")]
        [LabelText("关闭")]
        [SerializeField]
        private Button closeButton;

        [BoxGroup("壳层")]
        [LabelText("容量文案")]
        [Tooltip("模板写在 Text 上，如：装饰 {0}/{1}   ·   Luby {2}/{3}")]
        [SerializeField]
        private TextMeshProUGUI capacityText;

        [BoxGroup("壳层")]
        [LabelText("金币")]
        [SerializeField]
        private TextMeshProUGUI currencyText;

        [FoldoutGroup("页签", expanded: false)]
        [LabelText("商店")]
        [SerializeField]
        private Button tabShopButton;

        [FoldoutGroup("页签")]
        [LabelText("仓库")]
        [SerializeField]
        private Button tabInventoryButton;

        [FoldoutGroup("页签")]
        [LabelText("领养")]
        [SerializeField]
        private Button tabLubyButton;

        [FoldoutGroup("页签")]
        [LabelText("图鉴")]
        [SerializeField]
        private Button tabCodexButton;

        [FoldoutGroup("页签")]
        [LabelText("场景")]
        [SerializeField]
        private Button tabSceneButton;

        [FoldoutGroup("页签")]
        [LabelText("设置")]
        [SerializeField]
        private Button tabSettingsButton;

        [FoldoutGroup("页面", expanded: false)]
        [LabelText("ShopPage")]
        [SerializeField]
        private GameObject shopPage;

        [FoldoutGroup("页面")]
        [LabelText("InventoryPage")]
        [SerializeField]
        private GameObject inventoryPage;

        [FoldoutGroup("页面")]
        [LabelText("LubyPage")]
        [SerializeField]
        private GameObject lubyPage;

        [FoldoutGroup("页面")]
        [LabelText("CodexPage")]
        [SerializeField]
        private GameObject codexPage;

        [FoldoutGroup("页面")]
        [LabelText("ScenePage")]
        [SerializeField]
        private GameObject scenePage;

        [FoldoutGroup("页面")]
        [LabelText("SettingsPage")]
        [SerializeField]
        private GameObject settingsPage;

        [FoldoutGroup("页控制器", expanded: false)]
        [LabelText("商店")]
        [SerializeField]
        private ShopUIController shopUi;

        [FoldoutGroup("页控制器")]
        [LabelText("仓库")]
        [SerializeField]
        private InventoryUIController inventoryUi;

        [FoldoutGroup("页控制器")]
        [LabelText("领养")]
        [SerializeField]
        private LubyUIController lubyUi;

        [FoldoutGroup("页控制器")]
        [LabelText("图鉴")]
        [SerializeField]
        private CodexUIController codexUi;

        [FoldoutGroup("启动", expanded: true)]
        [LabelText("默认页签")]
        [SerializeField]
        private HubTab startTab = HubTab.Shop;

        [FoldoutGroup("启动")]
        [LabelText("启动时打开")]
        [SerializeField]
        private bool startOpen;

        [Title("运行时", "Play 模式只读")]
        [ShowInInspector, ReadOnly, LabelText("当前页签")]
        private HubTab DebugCurrentTab => _current;

        [ShowInInspector, ReadOnly, LabelText("面板打开")]
        private bool DebugIsOpen => IsOpen;

        [ShowInInspector, ReadOnly, LabelText("容量模板")]
        private string DebugCapacityFormat => _capacityFormat;

        [ShowInInspector, ReadOnly, LabelText("装饰")]
        private string DebugDecorCapacity =>
            $"{(_decorWorld != null ? _decorWorld.Count : 0)}/{(_decorWorld != null ? _decorWorld.DeskCapacity : DecorWorld.ResolveInitialDeskCapacity())}";

        [ShowInInspector, ReadOnly, LabelText("Luby")]
        private string DebugLubyCapacity =>
            $"{(_lubyWorld != null ? _lubyWorld.OccupiedDeskSlots : 0)}/{(_lubyWorld != null ? _lubyWorld.DeskCapacity : LubyWorld.ResolveInitialDeskCapacity())}";

        private HubTab _current = HubTab.Shop;
        private ShopManager _shop;
        private ShopWallet _subscribedWallet;
        private DecorWorld _decorWorld;
        private LubyWorld _lubyWorld;
        private string _capacityFormat;

        public HubTab CurrentTab => _current;
        public bool IsOpen => rootPanel != null && rootPanel.activeSelf;

        private void Awake()
        {
            DesktopPetServices.RegisterHubUi(this);
            _shop = DesktopPetServices.Shop;
            _decorWorld = DesktopPetServices.DecorWorld;
            _lubyWorld = DesktopPetServices.LubyWorld;

            if (shopUi == null)
                shopUi = DesktopPetServices.ShopUi;
            if (inventoryUi == null)
                inventoryUi = DesktopPetServices.InventoryUi;
            if (lubyUi == null)
                lubyUi = DesktopPetServices.LubyUi;
            if (codexUi == null)
                codexUi = DesktopPetServices.CodexUi;

            if (rootPanel == null)
                Debug.LogError("[HubUI] 未绑定 DesktopHubPanel。请「应用主面板预制体」。");

            if (capacityText != null && !string.IsNullOrEmpty(capacityText.text))
                _capacityFormat = capacityText.text;
        }

        private void OnDestroy()
        {
            DesktopPetServices.UnregisterHubUi(this);
        }

        private void OnEnable()
        {
            EnsureWalletSubscription();

            if (openMenuButton != null)
                openMenuButton.onClick.AddListener(Toggle);
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);

            Wire(tabShopButton, () => ShowTab(HubTab.Shop));
            Wire(tabInventoryButton, () => ShowTab(HubTab.Inventory));
            Wire(tabLubyButton, () => ShowTab(HubTab.Luby));
            Wire(tabCodexButton, () => ShowTab(HubTab.Codex));
            Wire(tabSettingsButton, () => ShowTab(HubTab.Settings));
            Wire(tabSceneButton, () => ShowTab(HubTab.Scene));
            WireCapacityListeners();
        }

        private void OnDisable()
        {
            UnsubscribeWallet();
            UnwireCapacityListeners();

            if (openMenuButton != null)
                openMenuButton.onClick.RemoveAllListeners();
            if (closeButton != null)
                closeButton.onClick.RemoveAllListeners();

            Clear(tabShopButton);
            Clear(tabInventoryButton);
            Clear(tabLubyButton);
            Clear(tabCodexButton);
            Clear(tabSettingsButton);
            Clear(tabSceneButton);
        }

        private void Start()
        {
            EnsureWalletSubscription();
            UnwireCapacityListeners();
            WireCapacityListeners();

            if (startOpen)
                Open(startTab);
            else
                Close();
            RefreshChrome();
        }

        private void EnsureShop()
        {
            if (_shop == null)
                _shop = DesktopPetServices.Shop;
        }

        private void EnsureWalletSubscription()
        {
            EnsureShop();
            ShopWallet wallet = _shop != null ? _shop.Wallet : null;
            if (wallet == _subscribedWallet)
                return;

            UnsubscribeWallet();
            _subscribedWallet = wallet;
            if (_subscribedWallet != null)
                _subscribedWallet.CurrencyChanged += OnWalletCurrencyChanged;
        }

        private void UnsubscribeWallet()
        {
            if (_subscribedWallet == null)
                return;
            _subscribedWallet.CurrencyChanged -= OnWalletCurrencyChanged;
            _subscribedWallet = null;
        }

        public void Open(HubTab tab)
        {
            if (rootPanel != null)
            {
                rootPanel.SetActive(true);
                rootPanel.transform.SetAsLastSibling();
            }

            ShowTab(tab);
        }

        public void Toggle()
        {
            if (IsOpen)
                Close();
            else
                Open(startTab);
        }

        public void Close()
        {
            if (_current == HubTab.Settings)
                DesktopPetServices.SettingsUi?.PersistAndSave();
            if (_current == HubTab.Luby)
                lubyUi?.OnPageHidden();

            if (rootPanel != null)
                rootPanel.SetActive(false);
        }

        public void ShowTab(HubTab tab)
        {
            if (_current == HubTab.Settings && tab != HubTab.Settings)
                DesktopPetServices.SettingsUi?.PersistAndSave();
            if (_current == HubTab.Luby && tab != HubTab.Luby)
                lubyUi?.OnPageHidden();

            _current = tab;

            SetPageVisible(shopPage, tab == HubTab.Shop);
            SetPageVisible(inventoryPage, tab == HubTab.Inventory);
            SetPageVisible(lubyPage, tab == HubTab.Luby);
            SetPageVisible(codexPage, tab == HubTab.Codex);
            SetPageVisible(settingsPage, tab == HubTab.Settings);
            SetPageVisible(scenePage, tab == HubTab.Scene);

            SetTabVisual(tabShopButton, tab == HubTab.Shop);
            SetTabVisual(tabInventoryButton, tab == HubTab.Inventory);
            SetTabVisual(tabLubyButton, tab == HubTab.Luby);
            SetTabVisual(tabCodexButton, tab == HubTab.Codex);
            SetTabVisual(tabSettingsButton, tab == HubTab.Settings);
            SetTabVisual(tabSceneButton, tab == HubTab.Scene);

            switch (tab)
            {
                case HubTab.Shop:
                    shopUi?.OnPageShown();
                    break;
                case HubTab.Inventory:
                    inventoryUi?.OnPageShown();
                    break;
                case HubTab.Luby:
                    lubyUi?.OnPageShown();
                    break;
                case HubTab.Codex:
                    codexUi?.OnPageShown();
                    break;
                case HubTab.Settings:
                    DesktopPetServices.SettingsUi?.OnPageShown();
                    break;
                case HubTab.Scene:
                    DesktopPetServices.SceneUi?.OnPageShown();
                    break;
            }

            RefreshChrome();
        }

        public void RefreshChrome()
        {
            EnsureShop();
            int currency = _shop != null && _shop.Wallet != null ? _shop.Wallet.Currency : 0;
            if (currencyText != null)
                currencyText.text = currency.ToString();

            EnsureDecorWorld();
            int decorCount = _decorWorld != null ? _decorWorld.Count : 0;
            int decorCap = _decorWorld != null ? _decorWorld.DeskCapacity : DecorWorld.ResolveInitialDeskCapacity();

            if (_lubyWorld == null)
                _lubyWorld = DesktopPetServices.LubyWorld;
            int lubyCount = _lubyWorld != null ? _lubyWorld.OccupiedDeskSlots : 0;
            int lubyCap = _lubyWorld != null ? _lubyWorld.DeskCapacity : LubyWorld.ResolveInitialDeskCapacity();

            if (capacityText != null)
            {
                if (string.IsNullOrEmpty(_capacityFormat))
                {
                    Debug.LogError(
                        "[HubUI] Capacity Text 缺少模板（需含 {0}/{1}…{2}/{3}）。请改 MainCanvas 预制体后再「应用主面板」。",
                        capacityText);
                    return;
                }

                capacityText.text = string.Format(_capacityFormat, decorCount, decorCap, lubyCount, lubyCap);
            }
        }

        private void OnWalletCurrencyChanged(int _) => RefreshChrome();

        private void WireCapacityListeners()
        {
            EnsureDecorWorld();
            if (_decorWorld != null)
                _decorWorld.PlacedChanged += RefreshChrome;
            if (_lubyWorld == null)
                _lubyWorld = DesktopPetServices.LubyWorld;
            if (_lubyWorld != null)
                _lubyWorld.DeskChanged += RefreshChrome;
        }

        private void UnwireCapacityListeners()
        {
            if (_decorWorld != null)
                _decorWorld.PlacedChanged -= RefreshChrome;
            if (_lubyWorld != null)
                _lubyWorld.DeskChanged -= RefreshChrome;
        }

        private void EnsureDecorWorld()
        {
            if (_decorWorld == null)
                _decorWorld = DesktopPetServices.DecorWorld;
        }

        /// <summary>用 CanvasGroup 显隐，避免 SetActive 触发整树布局重算导致跳动。</summary>
        private static void SetPageVisible(GameObject page, bool on)
        {
            if (page == null)
                return;

            if (!page.activeSelf)
                page.SetActive(true);

            CanvasGroup cg = page.GetComponent<CanvasGroup>();
            if (cg == null)
            {
                Debug.LogError(
                    $"[HubUI] 页面「{page.name}」缺少 CanvasGroup。请在 MainCanvas.prefab 预挂后再「应用主面板」。",
                    page);
                return;
            }

            cg.alpha = on ? 1f : 0f;
            cg.interactable = on;
            cg.blocksRaycasts = on;
        }

        private static void SetTabVisual(Button btn, bool on) => DesktopPetTabVisual.Apply(btn, on);

        private static void Wire(Button btn, UnityEngine.Events.UnityAction action)
        {
            if (btn != null)
                btn.onClick.AddListener(action);
        }

        private static void Clear(Button btn)
        {
            if (btn != null)
                btn.onClick.RemoveAllListeners();
        }
    }
}
