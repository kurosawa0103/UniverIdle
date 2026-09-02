#if UNITY_EDITOR
using UnityEngine;

namespace UniverIdle.Editor
{
    /// <summary>主界面布局参数。重建前从场景捕获，或作为无场景 UI 时的默认值。</summary>
    public class MainUILayoutParams : ScriptableObject
    {
        public const string DefaultAssetPath = "Assets/Editor/MainUILayoutParams.asset";

        public Vector2 referenceResolution = new Vector2(1920f, 1080f);
        public float matchWidthOrHeight = 0.5f;

        public float topBarHeight = 100f;
        public float topBarPaddingH = 20f;
        public float topBarPadV = 14f;
        public float topBarGap = 14f;
        public float topBarContentHeight = 72f;
        public float logoIconSize = 44f;
        public float logoGap = 10f;
        public float titleFont = 18f;
        public float subtitleFont = 13f;
        public float currencyGap = 16f;
        public float topBtnPadH = 12f;
        public float topBtnPadV = 8f;
        public float topBtnFont = 14f;
        public float topBarCurrencyFont = 14f;
        public float topBarLogoGlyphFont = 20f;

        public float dividerThickness = 1f;
        public bool useBodyFlexSpacer = true;
        public bool bodyChildForceExpandWidth = false;

        public float sidebarWidth = 380f;
        public float centerPreferredWidth = 900f;
        public float centerFlexibleWidth = 1f;
        public float detailPreferredWidth = 160f;
        public float detailMinWidth = -1f;
        public float detailFlexibleWidth = 0f;

        public float sidebarPadH = 8f;
        public float sidebarPadV = 10f;
        public float sidebarGap = 6f;

        public float centerPadding = 14f;
        public float centerGap = 12f;
        public float bannerHeight = 130f;
        public float bannerOverlayPadH = 20f;
        public float bannerOverlayPadV = 16f;
        public float bannerTitleFont = 22f;
        public float tagGap = 8f;
        public float tagFont = 11f;
        public float tagHeight = 22f;

        public float cardGap = 10f;
        public float cardPadding = 10f;
        public float cardMinHeight = 100f;
        public float actionCardsRowHeight = 100f;
        public float cardThumbHeight = 56f;
        public float cardTitleFont = 14f;
        public float cardMetaFont = 12f;
        public float cardYieldFont = 11f;
        public bool useCenterFlexSpacer = true;

        public float runningPadH = 16f;
        public float runningPadV = 14f;
        public float runningGap = 14f;
        public float runningThumb = 56f;
        public float runningLabelFont = 15f;
        public float runningLabelToBar = 8f;
        public float runningBarHeight = 10f;
        public float runningBarTotalHeight = 84f;
        public float runningTimeWidth = 48f;
        public float runningTimeFont = 13f;

        public float detailPadding = 14f;
        public float detailGap = 12f;
        public float detailHeroHeight = 132f;
        public float detailTitleHeight = 22f;
        public float detailBodyHeight = 64f;
        public float detailBodyFlexibleHeight = 1f;
        public float detailReqLineHeight = 18f;
        public float detailBodyLineSpacing = 6f;
        public float detailTitleFont = 16f;
        public float detailBodyFont = 13f;
        public float detailReqFont = 12f;
        public bool detailChildForceExpandWidth = true;

        public float invPanelPadding = 16f;
        public float invPanelGap = 12f;
        public float invPanelHeaderHeight = 36f;
        public float invPanelTitleFont = 18f;
        public float invPanelCloseSize = 32f;
        public float invPanelSlotWidth = 88f;
        public float invPanelSlotHeight = 96f;
        public float invPanelSlotGap = 8f;
        public Vector2 invPanelSize = new Vector2(520f, 560f);
    }
}
#endif
