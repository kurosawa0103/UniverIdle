#if UNITY_EDITOR
using Sirenix.OdinInspector;
using UnityEngine;

namespace UniverIdle.Editor
{
    /// <summary>主界面布局参数。Inspector 由 Odin 绘制。</summary>
    [Title("主界面布局参数", "UniverIdle_MainUI", TitleAlignments.Centered)]
    [InfoBox("推荐用菜单「UniverIdle → 布局调参窗口」：改参数可实时预览，满意后保存到场景 / Asset。整页重建用「创建主界面」。", InfoMessageType.Info)]
    public class MainUILayoutParams : ScriptableObject
    {
        public const string DefaultAssetPath = "Assets/Editor/MainUILayoutParams.asset";

        [PropertyOrder(-11)]
        [Button("打开布局调参窗口", ButtonSizes.Large)]
        [GUIColor(0.55f, 0.78f, 0.95f)]
        private void OpenLayoutTuneWindow() => MainUILayoutTuneWindow.ShowWindow();

        [PropertyOrder(-10)]
        [Button("从场景同步布局参数", ButtonSizes.Medium)]
        [Tooltip("把当前场景 UniverIdle_MainUI 的尺寸写回本 asset（仅在场景里手调过后用）")]
        private void SyncFromScene() => MainUISetup.SyncLayoutParamsFromScene();

        [PropertyOrder(-9)]
        [Button("创建主界面（当前场景）", ButtonSizes.Large)]
        [GUIColor(0.45f, 0.85f, 0.65f)]
        [Tooltip("按本 asset 数值重建主界面，不会先读场景覆盖配置")]
        private void RebuildMainUI() => MainUISetup.CreateMainUI();

        [FoldoutGroup("Canvas · 根画布", expanded: true)]
        [LabelText("参考分辨率")]
        [Tooltip("Canvas Scaler → Reference Resolution")]
        public Vector2 referenceResolution = new Vector2(1920f, 1080f);

        [FoldoutGroup("Canvas · 根画布")]
        [LabelText("宽高等比匹配")]
        [Tooltip("0=偏宽，1=偏高，0.5=折中")]
        [PropertyRange(0f, 1f)]
        public float matchWidthOrHeight = 0.5f;

        [FoldoutGroup("TopBar · 顶栏")]
        [LabelText("顶栏高度")]
        public float topBarHeight = 100f;

        [FoldoutGroup("TopBar · 顶栏")]
        [HorizontalGroup("TopBar · 顶栏/内边距")]
        [LabelText("左右 Padding")]
        public float topBarPaddingH = 20f;

        [HorizontalGroup("TopBar · 顶栏/内边距")]
        [LabelText("上下 Padding")]
        public float topBarPadV = 14f;

        [FoldoutGroup("TopBar · 顶栏")]
        [LabelText("元素横向间距")]
        public float topBarGap = 14f;

        [FoldoutGroup("TopBar · 顶栏")]
        [LabelText("Logo 行高度")]
        public float topBarContentHeight = 72f;

        [FoldoutGroup("TopBar · 顶栏")]
        [HorizontalGroup("TopBar · 顶栏/Logo")]
        [LabelText("图标边长")]
        public float logoIconSize = 44f;

        [HorizontalGroup("TopBar · 顶栏/Logo")]
        [LabelText("图标与标题间距")]
        public float logoGap = 10f;

        [FoldoutGroup("TopBar · 顶栏")]
        [HorizontalGroup("TopBar · 顶栏/字号")]
        [LabelText("主标题")]
        public float titleFont = 18f;

        [HorizontalGroup("TopBar · 顶栏/字号")]
        [LabelText("副标题")]
        public float subtitleFont = 13f;

        [FoldoutGroup("TopBar · 顶栏")]
        [LabelText("货币间距")]
        public float currencyGap = 16f;

        [FoldoutGroup("TopBar · 顶栏")]
        [HorizontalGroup("TopBar · 顶栏/按钮")]
        [LabelText("按钮左右 Padding")]
        public float topBtnPadH = 12f;

        [HorizontalGroup("TopBar · 顶栏/按钮")]
        [LabelText("按钮上下 Padding")]
        public float topBtnPadV = 8f;

        [FoldoutGroup("TopBar · 顶栏")]
        [HorizontalGroup("TopBar · 顶栏/字号2")]
        [LabelText("按钮字号")]
        public float topBtnFont = 14f;

        [HorizontalGroup("TopBar · 顶栏/字号2")]
        [LabelText("货币字号")]
        public float topBarCurrencyFont = 14f;

        [FoldoutGroup("TopBar · 顶栏")]
        [LabelText("Logo 字形字号")]
        public float topBarLogoGlyphFont = 20f;

        [FoldoutGroup("Body · 主体三栏")]
        [LabelText("竖分割线宽度")]
        public float dividerThickness = 1f;

        [FoldoutGroup("Body · 主体三栏")]
        [LabelText("插入 BodyFlexSpacer")]
        [Tooltip("Center 与 Detail 之间的弹性占位")]
        public bool useBodyFlexSpacer = true;

        [FoldoutGroup("Body · 主体三栏")]
        [LabelText("子项强制拉满宽度")]
        public bool bodyChildForceExpandWidth = false;

        [FoldoutGroup("Body · 主体三栏")]
        [HorizontalGroup("Body · 主体三栏/宽度")]
        [LabelText("侧栏 Sidebar")]
        public float sidebarWidth = 380f;

        [HorizontalGroup("Body · 主体三栏/宽度")]
        [LabelText("中间 Center 首选")]
        public float centerPreferredWidth = 900f;

        [FoldoutGroup("Body · 主体三栏")]
        [LabelText("中间 Center 弹性宽")]
        public float centerFlexibleWidth = 1f;

        [FoldoutGroup("Body · 主体三栏")]
        [HorizontalGroup("Body · 主体三栏/详情")]
        [LabelText("右侧 Detail 首选")]
        public float detailPreferredWidth = 160f;

        [HorizontalGroup("Body · 主体三栏/详情")]
        [LabelText("Detail 最小宽")]
        [Tooltip("≤0 表示不限制")]
        public float detailMinWidth = -1f;

        [FoldoutGroup("Body · 主体三栏")]
        [LabelText("Detail 弹性宽")]
        public float detailFlexibleWidth = 0f;

        [FoldoutGroup("Sidebar · 技能导航")]
        [HorizontalGroup("Sidebar · 技能导航/内边距")]
        [LabelText("左右 Padding")]
        public float sidebarPadH = 8f;

        [HorizontalGroup("Sidebar · 技能导航/内边距")]
        [LabelText("上下 Padding")]
        public float sidebarPadV = 10f;

        [FoldoutGroup("Sidebar · 技能导航")]
        [LabelText("技能项间距")]
        public float sidebarGap = 6f;

        [FoldoutGroup("Center · 工作区外框")]
        [LabelText("WorkView 内边距")]
        public float centerPadding = 14f;

        [FoldoutGroup("Center · 工作区外框")]
        [LabelText("区块纵向间距")]
        [Tooltip("横幅 / 动作卡 / 进度条")]
        public float centerGap = 12f;

        [FoldoutGroup("Center · 工作区外框")]
        [LabelText("插入 Center FlexSpacer")]
        public bool useCenterFlexSpacer = true;

        [FoldoutGroup("Scene · 场景（Banner + ActionCards）")]
        [LabelText("横幅区高度")]
        [Tooltip("LocationBanner/BannerArt：场景名与 Tags 区域")]
        public float bannerHeight = 130f;

        [FoldoutGroup("Scene · 场景（Banner + ActionCards）")]
        [FoldoutGroup("Scene · 场景（Banner + ActionCards）")]
        [HorizontalGroup("Scene · 场景（Banner + ActionCards）/文案边距")]
        [LabelText("BannerText 左右")]
        public float bannerOverlayPadH = 20f;

        [HorizontalGroup("Scene · 场景（Banner + ActionCards）/文案边距")]
        [LabelText("BannerText 上下")]
        public float bannerOverlayPadV = 16f;

        [FoldoutGroup("Scene · 场景（Banner + ActionCards）")]
        [LabelText("场景标题字号")]
        public float bannerTitleFont = 22f;

        [FoldoutGroup("Scene · 场景（Banner + ActionCards）")]
        [HorizontalGroup("Scene · 场景（Banner + ActionCards）/Tags")]
        [LabelText("标签间距")]
        public float tagGap = 8f;

        [HorizontalGroup("Scene · 场景（Banner + ActionCards）/Tags")]
        [LabelText("标签字号")]
        public float tagFont = 11f;

        [FoldoutGroup("Scene · 场景（Banner + ActionCards）")]
        [LabelText("Tags 行高度")]
        [Tooltip("微光 / 安全 / 星级 标签高度")]
        public float tagHeight = 22f;

        [FoldoutGroup("Scene · 场景（Banner + ActionCards）")]
        [HorizontalGroup("Scene · 场景（Banner + ActionCards）/卡片")]
        [LabelText("整卡高度")]
        [Tooltip("LocationBanner/ActionCards 下单卡高度")]
        public float cardMinHeight = 250f;

        [HorizontalGroup("Scene · 场景（Banner + ActionCards）/卡片")]
        [LabelText("行高度")]
        [Tooltip("LocationBanner/ActionCards 整行")]
        public float actionCardsRowHeight = 250f;

        [FoldoutGroup("Scene · 场景（Banner + ActionCards）")]
        [HorizontalGroup("Scene · 场景（Banner + ActionCards）/Thumb")]
        [LabelText("Thumb 高度")]
        public float cardThumbHeight = 160f;

        [HorizontalGroup("Scene · 场景（Banner + ActionCards）/Thumb")]
        [LabelText("Thumb 宽度")]
        [Tooltip("0 = 横向拉满卡片")]
        public float cardThumbWidth = 0f;

        [FoldoutGroup("Scene · 场景（Banner + ActionCards）")]
        [HorizontalGroup("Scene · 场景（Banner + ActionCards）/卡内边距")]
        [LabelText("卡片 Padding")]
        public float cardPadding = 10f;

        [HorizontalGroup("Scene · 场景（Banner + ActionCards）/卡内边距")]
        [LabelText("卡内行间距")]
        public float cardVlgSpacing = 8f;

        [FoldoutGroup("Scene · 场景（Banner + ActionCards）")]
        [LabelText("卡间距")]
        public float cardGap = 10f;

        [FoldoutGroup("Scene · 场景（Banner + ActionCards）")]
        [LabelText("Grid 列数")]
        [Tooltip("ActionCards 的 GridLayoutGroup 固定列数")]
        public int cardGridColumns = 3;

        [FoldoutGroup("Scene · 场景（Banner + ActionCards）")]
        [HorizontalGroup("Scene · 场景（Banner + ActionCards）/Grid Cell")]
        [LabelText("Cell 宽")]
        [Tooltip("GridLayoutGroup.cellSize.x；0 = 按容器宽度均分")]
        public float cardGridCellWidth = 350f;

        [HorizontalGroup("Scene · 场景（Banner + ActionCards）/Grid Cell")]
        [LabelText("Cell 高")]
        [Tooltip("GridLayoutGroup.cellSize.y")]
        public float cardGridCellHeight = 250f;

        [FoldoutGroup("Scene · 场景（Banner + ActionCards）")]
        [HorizontalGroup("Scene · 场景（Banner + ActionCards）/卡行高")]
        [LabelText("标题行高")]
        public float cardTitleHeight = 18f;

        [HorizontalGroup("Scene · 场景（Banner + ActionCards）/卡行高")]
        [LabelText("Meta 行高")]
        public float cardMetaHeight = 16f;

        [FoldoutGroup("Scene · 场景（Banner + ActionCards）")]
        [HorizontalGroup("Scene · 场景（Banner + ActionCards）/卡字号")]
        [LabelText("标题字号")]
        public float cardTitleFont = 14f;

        [HorizontalGroup("Scene · 场景（Banner + ActionCards）/卡字号")]
        [LabelText("元信息字号")]
        public float cardMetaFont = 12f;

        [FoldoutGroup("Scene · 场景（Banner + ActionCards）")]
        [LabelText("产出字号")]
        public float cardYieldFont = 11f;

        [FoldoutGroup("RunningBar · 进行条")]
        [LabelText("整行高度")]
        public float runningBarTotalHeight = 84f;

        [FoldoutGroup("RunningBar · 进行条")]
        [HorizontalGroup("RunningBar · 进行条/内边距")]
        [LabelText("左右 Padding")]
        public float runningPadH = 16f;

        [HorizontalGroup("RunningBar · 进行条/内边距")]
        [LabelText("上下 Padding")]
        public float runningPadV = 14f;

        [FoldoutGroup("RunningBar · 进行条")]
        [LabelText("横向间距")]
        public float runningGap = 14f;

        [FoldoutGroup("RunningBar · 进行条")]
        [LabelText("Thumb 边长")]
        public float runningThumb = 56f;

        [FoldoutGroup("RunningBar · 进行条")]
        [LabelText("标签字号")]
        public float runningLabelFont = 15f;

        [FoldoutGroup("RunningBar · 进行条")]
        [LabelText("标签与进度条间距")]
        public float runningLabelToBar = 8f;

        [FoldoutGroup("RunningBar · 进行条")]
        [HorizontalGroup("RunningBar · 进行条/进度")]
        [LabelText("进度条高度")]
        public float runningBarHeight = 10f;

        [HorizontalGroup("RunningBar · 进行条/进度")]
        [LabelText("倒计时区宽度")]
        public float runningTimeWidth = 48f;

        [FoldoutGroup("RunningBar · 进行条")]
        [LabelText("倒计时字号")]
        public float runningTimeFont = 13f;

        [FoldoutGroup("Detail · 右侧详情")]
        [LabelText("内边距")]
        public float detailPadding = 14f;

        [FoldoutGroup("Detail · 右侧详情")]
        [LabelText("区块间距")]
        public float detailGap = 12f;

        [FoldoutGroup("Detail · 右侧详情")]
        [LabelText("配图区高度")]
        public float detailHeroHeight = 132f;

        [FoldoutGroup("Detail · 右侧详情")]
        [HorizontalGroup("Detail · 右侧详情/配图缩略图")]
        [LabelText("缩略图宽")]
        public float detailHeroThumbWidth = 80f;

        [HorizontalGroup("Detail · 右侧详情/配图缩略图")]
        [LabelText("缩略图高")]
        public float detailHeroThumbHeight = 80f;

        [FoldoutGroup("Detail · 右侧详情")]
        [HorizontalGroup("Detail · 右侧详情/文本行")]
        [LabelText("标题行高")]
        public float detailTitleHeight = 22f;

        [HorizontalGroup("Detail · 右侧详情/文本行")]
        [LabelText("正文行高")]
        public float detailBodyHeight = 64f;

        [FoldoutGroup("Detail · 右侧详情")]
        [LabelText("正文弹性高度")]
        public float detailBodyFlexibleHeight = 1f;

        [FoldoutGroup("Detail · 右侧详情")]
        [LabelText("条件行行高")]
        public float detailReqLineHeight = 18f;

        [FoldoutGroup("Detail · 右侧详情")]
        [LabelText("正文行距")]
        public float detailBodyLineSpacing = 6f;

        [FoldoutGroup("Detail · 右侧详情")]
        [HorizontalGroup("Detail · 右侧详情/字号")]
        [LabelText("标题字号")]
        public float detailTitleFont = 16f;

        [HorizontalGroup("Detail · 右侧详情/字号")]
        [LabelText("正文字号")]
        public float detailBodyFont = 13f;

        [FoldoutGroup("Detail · 右侧详情")]
        [LabelText("条件行字号")]
        public float detailReqFont = 12f;

        [FoldoutGroup("Detail · 右侧详情")]
        [LabelText("子项拉满宽度")]
        public bool detailChildForceExpandWidth = true;

        [FoldoutGroup("Inventory · 背包浮层")]
        [LabelText("面板尺寸")]
        public Vector2 invPanelSize = new Vector2(520f, 560f);

        [FoldoutGroup("Inventory · 背包浮层")]
        [HorizontalGroup("Inventory · 背包浮层/内边距")]
        [LabelText("Padding")]
        public float invPanelPadding = 16f;

        [HorizontalGroup("Inventory · 背包浮层/内边距")]
        [LabelText("区块间距")]
        public float invPanelGap = 12f;

        [FoldoutGroup("Inventory · 背包浮层")]
        [LabelText("标题栏高度")]
        [Tooltip("Panel/Header 行高；Panel 的 VerticalLayoutGroup 不得 Force Expand Height")]
        public float invPanelHeaderHeight = 40f;

        [FoldoutGroup("Inventory · 背包浮层")]
        [LabelText("标题字号")]
        public float invPanelTitleFont = 18f;

        [FoldoutGroup("Inventory · 背包浮层")]
        [LabelText("关闭按钮边长")]
        public float invPanelCloseSize = 32f;

        [FoldoutGroup("Inventory · 背包浮层")]
        [HorizontalGroup("Inventory · 背包浮层/格子")]
        [LabelText("格宽")]
        public float invPanelSlotWidth = 88f;

        [HorizontalGroup("Inventory · 背包浮层/格子")]
        [LabelText("格高")]
        public float invPanelSlotHeight = 96f;

        [FoldoutGroup("Inventory · 背包浮层")]
        [LabelText("格子间距")]
        public float invPanelSlotGap = 8f;
    }
}
#endif
