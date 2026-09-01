using System.Collections.Generic;
using DesktopPet.Inventory;
using DesktopPet.Luby;
using DesktopPet.Save;
using DesktopPet.Shop;
using Sirenix.OdinInspector;
using UnityEngine;

namespace DesktopPet.Decor
{
    /// <summary>
    /// 装饰摆放：仓库「放置」与桌上「长按移动」是两套确认逻辑。
    /// 仓库：跟手 → 左键点击合法落点放下；右键/Esc 取消退回。
    /// 桌上：长按拿起 → 松手放下；右键呼出「收回仓库」按钮。
    /// </summary>
    public sealed partial class DecorPlacementSystem : MonoBehaviour
    {
        private enum HoldMode
        {
            None,
            /// <summary>仓库取出：武装后左键按下放置。</summary>
            FromInventory,
            /// <summary>已摆移动：松手放置。</summary>
            FromPlaced
        }

        [Title("装饰摆放系统", "仓库放置 / 长按移动 / 贴地与叠放")]
        [InfoBox(
            "仓库：点「放置」→ 装饰跟鼠标，虚影始终在落点；左键放下（右键取消）。\n" +
            "桌上：长按移动；右键呼出「收回仓库」按钮点击收回。\n" +
            "上架吸附 DecorPlaceSurface；贴地用 DesktopPetGround。",
            InfoMessageType.None)]

        [FoldoutGroup("引用绑定", expanded: true)]
        [LabelText("装饰世界")]
        [SerializeField]
        private DecorWorld world;

        [FoldoutGroup("引用绑定")]
        [LabelText("商店管理器")]
        [SerializeField]
        private ShopManager shop;

        [FoldoutGroup("引用绑定")]
        [LabelText("仓库")]
        [SerializeField]
        private ItemInventory inventory;

        [FoldoutGroup("引用绑定")]
        [LabelText("仓库 UI")]
        [SerializeField]
        private InventoryUIController inventoryUi;

        [FoldoutGroup("引用绑定")]
        [LabelText("统一地面")]
        [Tooltip("贴地高度来源；为空则解析 Services 或同物体 DesktopPetGround。")]
        [SerializeField]
        private DesktopPetGround ground;

        [FoldoutGroup("吸附与上架", expanded: true)]
        [LabelText("贴地优先带")]
        [Tooltip("光标落在地面附近这一高度内时，优先贴地而不是抢吸附到层板。")]
        [MinValue(0.05f)]
        [SuffixLabel("世界单位", true)]
        [SerializeField]
        private float groundPreferBand = 0.18f;

        [FoldoutGroup("长按拾取")]
        [LabelText("长按时长")]
        [MinValue(0.05f)]
        [SuffixLabel("秒", true)]
        [SerializeField]
        private float longPressSeconds = 0.4f;

        [FoldoutGroup("长按拾取")]
        [LabelText("允许移动距离")]
        [MinValue(0f)]
        [SuffixLabel("世界单位", true)]
        [SerializeField]
        private float longPressMaxMove = 0.35f;

        [FoldoutGroup("虚影颜色")]
        [LabelText("手持本体颜色")]
        [SerializeField]
        private Color holdColor = Color.white;

        [FoldoutGroup("虚影颜色")]
        [LabelText("合法落点虚影")]
        [SerializeField]
        private Color validGhostColor = new Color(1f, 1f, 1f, 0.45f);

        [FoldoutGroup("虚影颜色")]
        [LabelText("非法落点虚影")]
        [SerializeField]
        private Color invalidGhostColor = new Color(1f, 0.2f, 0.2f, 0.7f);

        [FoldoutGroup("运行时状态", expanded: false)]
        [ShowInInspector, ReadOnly, LabelText("手持中")]
        private bool DebugIsHolding => _holding;

        [FoldoutGroup("运行时状态")]
        [ShowInInspector, ReadOnly, LabelText("手持商品")]
        private string DebugHeldItem => _heldItem != null ? $"{_heldItem.displayName} ({_heldItem.itemId})" : "—";

        private ShopItemDefinition _heldItem;
        private bool _holding;
        private HoldMode _holdMode;
        private bool _inventoryPlaceArmed;
        private bool _returnToInventoryOnCancel;
        private Vector3 _originPlacedPos;
        private string _originPlacedParentInstanceId;
        private string _originPlacedInstanceId;
        private GameObject _heldVisual;
        private GameObject _ghost;
        private SpriteRenderer[] _ghostRenderers;
        private Bounds _footprint;
        private bool _candidateValid;
        private Vector3 _snapPos;
        private DecorPlaceSurface _placeSurface;
        private string _parentId;

        /// <summary>桌上拿起时卸下的整棵叠放子树（含根）；仓库放置时为 null。</summary>
        private List<PlacedDecor> _heldSubtree;
        private PlacedDecor _heldRoot;
        private bool _heldLiveSubtree;

        /// <summary>右键点中、待点「收回仓库」的桌上装饰（未手持时）。</summary>
        private PlacedDecor _pendingReturnTarget;

        private bool _pressTracking;
        private float _pressStartTime;
        private Vector2 _pressStartWorld;
        private PlacedDecor _pressTarget;

        public bool IsHolding => _holding;

        private void Awake()
        {
            if (DesktopPetServices.Placement != null && DesktopPetServices.Placement != this)
            {
                Debug.LogWarning("[DecorPlacement] 场景中已有 DecorPlacementSystem，销毁重复实例。");
                Destroy(gameObject);
                return;
            }

            DesktopPetServices.RegisterPlacement(this);
            ResolveRefs();
            EnsureSaveBootstrap();
        }

        private void OnDestroy()
        {
            DesktopPetServices.UnregisterPlacement(this);
        }

        private void ResolveRefs()
        {
            if (world == null)
                world = GetComponent<DecorWorld>() ?? DesktopPetServices.DecorWorld;
            if (shop == null)
                shop = GetComponent<ShopManager>() ?? DesktopPetServices.Shop;
            if (inventory == null)
                inventory = GetComponent<ItemInventory>() ?? DesktopPetServices.Inventory;
            if (inventoryUi == null)
                inventoryUi = GetComponent<InventoryUIController>() ?? DesktopPetServices.InventoryUi;
            if (ground == null)
                ground = GetComponent<DesktopPetGround>() ?? DesktopPetServices.Ground;
            if (ground == null)
            {
                Debug.LogError(
                    "[DecorPlacement] 缺少 DesktopPetGround。请在 DecorSystem（或场景）上预挂后再运行。",
                    this);
            }
        }

        private void EnsureSaveBootstrap()
        {
            if (GetComponent<DesktopPetSaveBootstrap>() != null)
                return;
            if (DesktopPetSaveBootstrap.Exists)
                return;

            Debug.LogError("[DecorPlacement] 缺少 DesktopPetSaveBootstrap。请在 DecorSystem 上挂载后再运行。");
        }

        public void Persist()
        {
            if (shop == null || inventory == null || world == null)
                ResolveRefs();
            DesktopPetSaveMgr.SaveRuntime(
                shop,
                inventory,
                world,
                DesktopPetServices.LubyWorld);
        }
    }
}
