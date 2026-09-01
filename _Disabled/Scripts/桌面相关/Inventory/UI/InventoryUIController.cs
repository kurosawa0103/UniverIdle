using System.Collections.Generic;
using DesktopPet;
using DesktopPet.Decor;
using DesktopPet.Hub;
using DesktopPet.Luby;
using DesktopPet.Shop;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DesktopPet.Inventory
{
    /// <summary>仓库页：装饰 / Luby 子页签 · 预览放置；壳层由 DesktopHub 管理。</summary>
    public sealed partial class InventoryUIController : MonoBehaviour
    {
        private enum InvSubTab
        {
            Decor = 0,
            Luby = 1
        }

        [Title("仓库页", "滚动列表 · 选中预览 · 放置")]
        [InfoBox("装饰与 Luby 分页；Luby 仅可贴地放置。", InfoMessageType.None)]

        [FoldoutGroup("引用", expanded: true)]
        [SerializeField] private ItemInventory inventory;

        [FoldoutGroup("页面", expanded: true)]
        [SerializeField] private TextMeshProUGUI statusText;

        [FoldoutGroup("子页签")]
        [SerializeField] private Button subDecorButton;
        [SerializeField] private Button subLubyButton;

        [FoldoutGroup("动态列表", expanded: true)]
        [Required]
        [SerializeField] private Transform inventoryContent;
        [Required]
        [SerializeField] private InventorySlot inventorySlotPrefab;
        [SerializeField] private GameObject inventoryEmptyHint;

        [FoldoutGroup("详情")]
        [SerializeField] private Image detailIcon;
        [SerializeField] private TextMeshProUGUI detailNameText;
        [SerializeField] private TextMeshProUGUI detailDescText;
        [SerializeField] private Button actionButton;
        [SerializeField] private TextMeshProUGUI actionButtonText;

        [FoldoutGroup("收回仓库热区")]
        [SerializeField] private RectTransform returnDropZone;
        [Tooltip("按钮底边相对目标头顶再往上抬一点（世界单位）")]
        [SerializeField] private float returnZoneHeadPadding = 0.2f;

        private readonly List<InventorySlot> _slots = new List<InventorySlot>(32);
        private ShopItemDefinition _selected;
        private LubyInstanceData _selectedLuby;
        private InvSubTab _subTab = InvSubTab.Decor;
        private Transform _returnFollowTarget;
        private LubyWorld _lubyWorld;
        private DecorWorld _decorWorld;

        private void Awake()
        {
            if (inventory == null)
                inventory = GetComponent<ItemInventory>() ?? DesktopPetServices.Inventory;

            EnsureWorldRefs();
            DesktopPetServices.RegisterInventoryUi(this);

            if (inventoryContent == null)
                Debug.LogError("[InventoryUI] 未绑定 inventoryContent。请「应用主面板预制体」。");
            if (inventorySlotPrefab == null)
                Debug.LogError("[InventoryUI] 未绑定 inventorySlotPrefab。");

            EnsureSubTabsBound();
            DesktopPetDetailLayout.Stabilize(
                detailIcon, detailNameText, detailDescText,
                previewHeight: 168f, descMinHeight: 64f, detailFlexibleWidth: 0.85f);
            ClearDetail();
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i] != null)
                    _slots[i].Clicked -= SelectSlot;
            }

            if (_lubyWorld != null)
            {
                _lubyWorld.WarehouseChanged -= OnLubyWarehouseChanged;
                _lubyWorld.DeskChanged -= OnDeskOrDecorCapacityChanged;
            }

            if (_decorWorld != null)
                _decorWorld.PlacedChanged -= OnDeskOrDecorCapacityChanged;
            DesktopPetServices.UnregisterInventoryUi(this);
        }

        private void OnEnable()
        {
            if (inventory != null)
                inventory.Changed += OnInventoryChanged;
            if (actionButton != null)
                actionButton.onClick.AddListener(OnActionClicked);
            WireSubTabs();
            EnsureWorldRefs();
            if (_lubyWorld != null)
            {
                _lubyWorld.WarehouseChanged += OnLubyWarehouseChanged;
                _lubyWorld.DeskChanged += OnDeskOrDecorCapacityChanged;
            }

            if (_decorWorld != null)
                _decorWorld.PlacedChanged += OnDeskOrDecorCapacityChanged;
        }

        private void OnDisable()
        {
            if (inventory != null)
                inventory.Changed -= OnInventoryChanged;
            if (actionButton != null)
                actionButton.onClick.RemoveAllListeners();
            UnwireSubTabs();
            if (_lubyWorld != null)
            {
                _lubyWorld.WarehouseChanged -= OnLubyWarehouseChanged;
                _lubyWorld.DeskChanged -= OnDeskOrDecorCapacityChanged;
            }

            if (_decorWorld != null)
                _decorWorld.PlacedChanged -= OnDeskOrDecorCapacityChanged;
        }

        public void Open() => DesktopPetServices.HubUi?.Open(HubTab.Inventory);

        public bool IsOpen =>
            DesktopPetServices.HubUi != null
            && DesktopPetServices.HubUi.IsOpen
            && DesktopPetServices.HubUi.CurrentTab == HubTab.Inventory;

        public void OnPageShown()
        {
            EnsureWorldRefs();
            DesktopPetDetailLayout.Stabilize(
                detailIcon, detailNameText, detailDescText,
                previewHeight: 168f, descMinHeight: 64f, detailFlexibleWidth: 0.85f);
            RefreshSubTabVisual();
            SetStatus(_subTab == InvSubTab.Luby
                ? "Luby：点选后「放置」到地面"
                : "装饰：点选后「放置」");
            RebuildInventoryList();
        }

        private void LateUpdate()
        {
            TickDeskOverlayFollow();
        }

        /// <summary>收回钮 / Luby 右键菜单 / 信息面板一并关掉。</summary>
        public void HideAllDeskOverlays()
        {
            HideReturnDropZone();
            HideLubyDeskContextMenu();
            HideLubyInfoPanel();
        }

        private void TickDeskOverlayFollow()
        {
            if (returnDropZone != null && returnDropZone.gameObject.activeSelf && _returnFollowTarget != null)
                PlaceOverlayAbove(returnDropZone, _returnFollowTarget);
            TickLubyDeskContextFollow();
        }

        /// <summary>在目标头顶显示「收回仓库」；跟随目标移动。无目标则不显示。</summary>
        public void ShowReturnDropZone(Transform worldTarget, System.Action onClicked = null)
        {
            if (worldTarget == null)
                return;

            if (returnDropZone == null)
            {
                Debug.LogError("[InventoryUI] 未绑定 returnDropZone。请在 MainCanvas.prefab 手改后再「应用主面板」。");
                return;
            }

            Button btn = returnDropZone.GetComponent<Button>();
            if (btn == null)
            {
                Debug.LogError("[InventoryUI] ReturnDropZone 缺少 Button。请在 MainCanvas.prefab 预挂后再「应用主面板」。");
                return;
            }

            _returnFollowTarget = worldTarget;

            HideLubyDeskContextMenu();
            HideLubyInfoPanel();

            btn.onClick.RemoveAllListeners();
            if (onClicked != null)
                btn.onClick.AddListener(() => onClicked());

            // 布局（锚点/尺寸/Transition）以 MainCanvas ReturnDropZone 预制体为准
            returnDropZone.SetAsLastSibling();
            returnDropZone.gameObject.SetActive(true);
            PlaceOverlayAbove(returnDropZone, worldTarget);
        }

        public void HideReturnDropZone()
        {
            if (returnDropZone == null)
                return;

            Button btn = returnDropZone.GetComponent<Button>();
            if (btn != null)
                btn.onClick.RemoveAllListeners();

            _returnFollowTarget = null;
            returnDropZone.gameObject.SetActive(false);
        }

        public bool IsCursorOverReturnDropZone() =>
            IsCursorOverActiveOverlay(returnDropZone);

        public bool IsCursorOverAnyDeskOverlay() =>
            IsCursorOverReturnDropZone()
            || IsCursorOverLubyDeskContextMenu()
            || IsCursorOverLubyInfoPanel();

        public bool IsAnyDeskOverlayVisible =>
            IsReturnDropZoneVisible
            || IsLubyDeskContextMenuVisible
            || IsLubyInfoPanelVisible;

        public bool IsReturnDropZoneVisible =>
            returnDropZone != null && returnDropZone.gameObject.activeSelf;

        private void PlaceOverlayAbove(RectTransform overlay, Transform worldTarget)
        {
            if (overlay == null || worldTarget == null)
                return;

            Vector3 world = DeskSpriteBounds.ResolveHeadWorld(worldTarget, returnZoneHeadPadding);
            Camera worldCam = Camera.main;
            if (worldCam == null)
                return;

            Canvas canvas = overlay.GetComponentInParent<Canvas>();
            if (canvas == null)
                return;

            RectTransform parent = overlay.parent as RectTransform;
            if (parent == null)
                return;

            Camera uiCam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(worldCam, world);
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, uiCam, out Vector2 local))
                overlay.anchoredPosition = local;
        }

        private bool IsCursorOverActiveOverlay(RectTransform rect)
        {
            if (rect == null || !rect.gameObject.activeSelf)
                return false;
            return IsScreenPointInsideRect(rect);
        }

        private bool IsScreenPointInsideRect(RectTransform rect)
        {
            if (rect == null)
                return false;
            Canvas canvas = rect.GetComponentInParent<Canvas>();
            Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;
            return RectTransformUtility.RectangleContainsScreenPoint(rect, Input.mousePosition, cam);
        }

        public void RefreshIfOpen()
        {
            if (IsOpen)
                RebuildInventoryList();
        }

        private void SetStatus(string msg)
        {
            if (statusText != null)
                statusText.text = msg;
        }

        private void OnLubyWarehouseChanged()
        {
            if (IsOpen && _subTab == InvSubTab.Luby)
                RebuildInventoryList();
        }

        private void OnDeskOrDecorCapacityChanged()
        {
            if (IsOpen)
                RefreshDetail();
        }

        private void EnsureWorldRefs()
        {
            if (_lubyWorld == null)
                _lubyWorld = DesktopPetServices.LubyWorld;
            if (_decorWorld == null)
                _decorWorld = DesktopPetServices.DecorWorld;
        }
    }
}
