using System.Collections.Generic;
using DesktopPet.Decor;
using DesktopPet.Hub;
using DesktopPet.Save;
using DesktopPet.Shop;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DesktopPet.Background
{
    /// <summary>
    /// 「场景」页签 UI 控制器。
    /// 左右轮播切换背景；底部购买/应用；中部双行容量升级（按背景独立存档）。
    /// </summary>
    public sealed class ScenePageUIController : MonoBehaviour
    {
        [Header("预览")]
        [SerializeField] private Image previewIcon;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI descText;

        [Header("轮播")]
        [SerializeField] private Button prevButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private TextMeshProUGUI pageIndicator;

        [Header("容量升级")]
        [SerializeField] private ScenePageCapacityBinding capacityBinding;

        [Header("操作")]
        [SerializeField] private Button actionButton;
        [SerializeField] private TextMeshProUGUI actionButtonText;

        private List<BackgroundDefinition> _backgrounds;
        private int _selectedIndex;

        private void Awake()
        {
            DesktopPetServices.RegisterSceneUi(this);
        }

        private void OnEnable()
        {
            if (prevButton != null) prevButton.onClick.AddListener(OnPrev);
            if (nextButton != null) nextButton.onClick.AddListener(OnNext);
            if (actionButton != null) actionButton.onClick.AddListener(OnAction);
            WireCapacityButtons(true);
        }

        private void OnDisable()
        {
            if (prevButton != null) prevButton.onClick.RemoveListener(OnPrev);
            if (nextButton != null) nextButton.onClick.RemoveListener(OnNext);
            if (actionButton != null) actionButton.onClick.RemoveListener(OnAction);
            WireCapacityButtons(false);
        }

        private void OnDestroy()
        {
            DesktopPetServices.UnregisterSceneUi(this);
        }

        public void OnPageShown()
        {
            RefreshList();
            SyncSelectionToCurrent();
            RefreshDisplay();
        }

        private void WireCapacityButtons(bool wire)
        {
            if (capacityBinding == null)
                return;

            if (capacityBinding.decorUpgradeButton != null)
            {
                capacityBinding.decorUpgradeButton.onClick.RemoveAllListeners();
                if (wire)
                    capacityBinding.decorUpgradeButton.onClick.AddListener(OnDecorUpgrade);
            }

            if (capacityBinding.lubyUpgradeButton != null)
            {
                capacityBinding.lubyUpgradeButton.onClick.RemoveAllListeners();
                if (wire)
                    capacityBinding.lubyUpgradeButton.onClick.AddListener(OnLubyUpgrade);
            }
        }

        private void RefreshList()
        {
            _backgrounds = new List<BackgroundDefinition>();
            BackgroundSystem bg = BackgroundSystem.Instance;
            if (bg?.Catalog == null) return;

            for (int i = 0; i < bg.Catalog.backgrounds.Count; i++)
            {
                BackgroundDefinition def = bg.Catalog.backgrounds[i];
                if (def != null)
                    _backgrounds.Add(def);
            }
        }

        private static string GetCurrentBackgroundId()
        {
            return BackgroundSystem.Instance?.CurrentBackgroundId
                ?? BackgroundDefinition.TransparentId;
        }

        private void SyncSelectionToCurrent()
        {
            if (_backgrounds == null || _backgrounds.Count == 0) return;

            string currentId = GetCurrentBackgroundId();
            for (int i = 0; i < _backgrounds.Count; i++)
            {
                if (_backgrounds[i].backgroundId == currentId)
                {
                    _selectedIndex = i;
                    return;
                }
            }

            _selectedIndex = 0;
        }

        private void RefreshDisplay()
        {
            if (_backgrounds == null || _backgrounds.Count == 0)
            {
                if (nameText != null) nameText.text = "无背景配置";
                if (descText != null) descText.text = "";
                if (actionButton != null) actionButton.interactable = false;
                if (actionButtonText != null) actionButtonText.text = "—";
                RefreshCapacity(null);
                return;
            }

            _selectedIndex = Mathf.Clamp(_selectedIndex, 0, _backgrounds.Count - 1);
            BackgroundDefinition def = _backgrounds[_selectedIndex];
            string currentId = GetCurrentBackgroundId();

            if (previewIcon != null)
            {
                previewIcon.sprite = def.icon;
                previewIcon.enabled = def.icon != null;
                previewIcon.preserveAspect = true;
            }

            if (nameText != null)
                nameText.text = def.displayName;

            if (descText != null)
                descText.text = def.description;

            if (pageIndicator != null)
                pageIndicator.text = $"{_selectedIndex + 1} / {_backgrounds.Count}";

            if (prevButton != null)
                prevButton.interactable = _selectedIndex > 0;
            if (nextButton != null)
                nextButton.interactable = _selectedIndex < _backgrounds.Count - 1;

            bool isCurrent = def.backgroundId == currentId;
            bool isUnlocked = BackgroundShopService.IsUnlocked(def.backgroundId);

            if (actionButton != null)
                actionButton.interactable = !isCurrent;

            if (actionButtonText != null)
            {
                if (isCurrent)
                    actionButtonText.text = "使用中";
                else if (isUnlocked)
                    actionButtonText.text = "应用";
                else
                    actionButtonText.text = $"购买（{def.price} 金币）";
            }

            RefreshCapacity(def);
        }

        private void RefreshCapacity(BackgroundDefinition def)
        {
            if (capacityBinding == null)
                return;

            if (def == null)
            {
                SetCapacityHint("无背景配置");
                return;
            }

            string bgId = def.backgroundId;
            bool isCurrent = bgId == GetCurrentBackgroundId();
            int decorCap = BackgroundSceneCapacity.GetDecorCapacity(bgId, def);
            int lubyCap = BackgroundSceneCapacity.GetLubyCapacity(bgId, def);
            int decorCount = GetDecorCount(bgId, isCurrent);
            int lubyCount = GetLubyCount(isCurrent);

            int decorLevel = BackgroundCapacityRules.CountDecorUpgradeLevel(def, decorCap);
            int lubyLevel = BackgroundCapacityRules.CountLubyUpgradeLevel(def, lubyCap);
            bool decorMax = decorLevel >= BackgroundCapacityRules.DecorTierCount(def);
            bool lubyMax = lubyLevel >= BackgroundCapacityRules.LubyTierCount(def);

            int decorGain = decorMax ? 0 : BackgroundCapacityRules.DecorUpgradeGain(def, decorCap, decorLevel);
            int lubyGain = lubyMax ? 0 : BackgroundCapacityRules.LubyUpgradeGain(def, lubyCap, lubyLevel);

            RefreshCapacityRow(
                "装饰摆放",
                decorCount,
                decorCap,
                decorGain,
                decorMax,
                decorLevel,
                def,
                true,
                capacityBinding.decorStatusText,
                capacityBinding.decorCostText,
                capacityBinding.decorUpgradeButton);

            RefreshCapacityRow(
                "Luby 栏位",
                lubyCount,
                lubyCap,
                lubyGain,
                lubyMax,
                lubyLevel,
                def,
                false,
                capacityBinding.lubyStatusText,
                capacityBinding.lubyCostText,
                capacityBinding.lubyUpgradeButton);

            SetCapacityHint(isCurrent
                ? "升级立即生效于此背景"
                : "可为未使用背景预先升级容量");
        }

        private static void RefreshCapacityRow(
            string label,
            int count,
            int cap,
            int gain,
            bool atMax,
            int level,
            BackgroundDefinition def,
            bool decor,
            TextMeshProUGUI statusText,
            TextMeshProUGUI costText,
            Button upgradeButton)
        {
            DeskCapacityUpgradeTier tier = default;
            bool hasTier = false;
            if (!atMax)
            {
                hasTier = decor
                    ? BackgroundCapacityRules.TryGetDecorTier(def, level, out tier)
                    : BackgroundCapacityRules.TryGetLubyTier(def, level, out tier);
            }

            if (statusText != null)
            {
                if (atMax)
                    statusText.text = $"{label}  {count}/{cap}（已满）";
                else if (gain > 1)
                    statusText.text = $"{label}  {count}/{cap} → {cap + gain}（+{gain}）";
                else
                    statusText.text = $"{label}  {count}/{cap} → {cap + gain}";
            }

            if (costText != null)
            {
                if (atMax)
                {
                    costText.text = "—";
                }
                else
                {
                    costText.text = hasTier ? $"{tier.goldCost} 金币" : "—";
                }
            }

            if (upgradeButton != null)
            {
                ShopWallet wallet = DesktopPetServices.Shop?.Wallet;
                bool canBuy = !atMax && wallet != null;
                if (canBuy)
                    canBuy = hasTier && wallet.CanAfford(tier.goldCost);

                upgradeButton.interactable = canBuy;
            }
        }

        private static int GetDecorCount(string backgroundId, bool isCurrent)
        {
            if (isCurrent)
            {
                DecorWorld world = DesktopPetServices.DecorWorld;
                return world != null ? world.Count : 0;
            }

            List<DesktopPetPlacedEntry> placed = DesktopPetSaveMgr.Current?.GetScenePlaced(backgroundId);
            return placed != null ? placed.Count : 0;
        }

        private static int GetLubyCount(bool isCurrent)
        {
            if (!isCurrent)
                return 0;

            return DesktopPetServices.LubyWorld != null
                ? DesktopPetServices.LubyWorld.OccupiedDeskSlots
                : 0;
        }

        private void SetCapacityHint(string message)
        {
            if (capacityBinding?.hintText != null)
                capacityBinding.hintText.text = message ?? string.Empty;
        }

        private void OnPrev()
        {
            if (_selectedIndex > 0)
            {
                _selectedIndex--;
                RefreshDisplay();
            }
        }

        private void OnNext()
        {
            if (_backgrounds != null && _selectedIndex < _backgrounds.Count - 1)
            {
                _selectedIndex++;
                RefreshDisplay();
            }
        }

        private void OnAction()
        {
            if (_backgrounds == null || _backgrounds.Count == 0) return;

            BackgroundDefinition def = _backgrounds[_selectedIndex];
            if (TrySwitchTo(def.backgroundId))
                RefreshDisplay();
        }

        private void OnDecorUpgrade()
        {
            if (_backgrounds == null || _backgrounds.Count == 0)
                return;

            BackgroundDefinition def = _backgrounds[_selectedIndex];
            ShopWallet wallet = DesktopPetServices.Shop?.Wallet;
            bool ok = BackgroundSceneCapacity.TryUpgradeDecor(def.backgroundId, def, wallet, out string error);
            SetCapacityHint(ok ? null : error);
            RefreshDisplay();
        }

        private void OnLubyUpgrade()
        {
            if (_backgrounds == null || _backgrounds.Count == 0)
                return;

            BackgroundDefinition def = _backgrounds[_selectedIndex];
            ShopWallet wallet = DesktopPetServices.Shop?.Wallet;
            bool ok = BackgroundSceneCapacity.TryUpgradeLuby(def.backgroundId, def, wallet, out string error);
            SetCapacityHint(ok ? null : error);
            RefreshDisplay();
        }

        private bool TrySwitchTo(string backgroundId)
        {
            BackgroundSystem bg = BackgroundSystem.Instance;
            if (bg == null)
            {
                Debug.LogError("[ScenePage] 缺少 BackgroundSystem。");
                return false;
            }

            if (backgroundId == bg.CurrentBackgroundId)
                return true;

            if (!BackgroundShopService.IsUnlocked(backgroundId))
            {
                BackgroundDefinition def = bg.Catalog?.FindById(backgroundId);
                ShopWallet wallet = DesktopPetServices.Shop?.Wallet;
                if (!BackgroundShopService.TryPurchase(def, wallet))
                    return false;
            }

            return DesktopPetSaveMgr.SwitchActiveBackground(backgroundId);
        }
    }
}
