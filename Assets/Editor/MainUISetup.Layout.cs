#if UNITY_EDITOR

namespace UniverIdle.Editor

{

    public static partial class MainUISetup

    {

        private static MainUILayoutParams _layout;



        private static void SetActiveLayout(MainUILayoutParams layout) => _layout = layout;



        private static MainUILayoutParams L => _layout;



        private static class ConceptLayout

        {

            public static float DividerThickness => L.dividerThickness;

            public static bool UseBodyFlexSpacer => L.useBodyFlexSpacer;

            public static bool BodyChildForceExpandWidth => L.bodyChildForceExpandWidth;



            public static float TopBarHeight => L.topBarHeight;

            public static float TopBarPaddingH => L.topBarPaddingH;

            public static float TopBarPadV => L.topBarPadV;

            public static float TopBarGap => L.topBarGap;

            public static float TopBarContentHeight => L.topBarContentHeight;

            public static float LogoIconSize => L.logoIconSize;

            public static float LogoGap => L.logoGap;

            public static float TitleFont => L.titleFont;

            public static float SubtitleFont => L.subtitleFont;

            public static float CurrencyGap => L.currencyGap;

            public static float TopBtnPadH => L.topBtnPadH;

            public static float TopBtnPadV => L.topBtnPadV;

            public static float TopBtnFont => L.topBtnFont;

            public static float TopBarCurrencyFont => L.topBarCurrencyFont;

            public static float TopBarLogoGlyphFont => L.topBarLogoGlyphFont;



            public static float SidebarWidth => L.sidebarWidth;

            public static float CenterPreferredWidth => L.centerPreferredWidth;

            public static float CenterFlexibleWidth => L.centerFlexibleWidth;

            public static float DetailPreferredWidth => L.detailPreferredWidth;

            public static float DetailMinWidth => L.detailMinWidth;

            public static float DetailFlexibleWidth => L.detailFlexibleWidth;



            public static float SidebarPadH => L.sidebarPadH;

            public static float SidebarPadV => L.sidebarPadV;

            public static float SidebarGap => L.sidebarGap;

            public const float SkillMinHeight = 52f;

            public const float SkillPadH = 10f;

            public const float SkillPadV = 8f;

            public const float SkillGap = 10f;

            public const float SkillIconSize = 40f;

            public const float SkillAccentWidth = 3f;

            public const float SkillBarHeight = 3f;

            public const float SkillNameFont = 14f;

            public const float SkillLvFont = 11f;



            public static float CenterPadding => L.centerPadding;

            public static float CenterGap => L.centerGap;

            public static float BannerHeight => L.bannerHeight;

            public static float BannerOverlayPadH => L.bannerOverlayPadH;

            public static float BannerOverlayPadV => L.bannerOverlayPadV;

            public static float BannerTitleFont => L.bannerTitleFont;

            public static float TagGap => L.tagGap;

            public static float TagFont => L.tagFont;

            public static bool UseCenterFlexSpacer => L.useCenterFlexSpacer;



            public static float CardGap => L.cardGap;

            public static float CardPadding => L.cardPadding;

            public static float CardMinHeight => L.cardMinHeight;

            public static float ActionCardsRowHeight => L.actionCardsRowHeight;

            public static float CardThumbHeight => L.cardThumbHeight;

            public static float CardTitleFont => L.cardTitleFont;

            public static float CardMetaFont => L.cardMetaFont;

            public static float CardYieldFont => L.cardYieldFont;



            public static float RunningPadH => L.runningPadH;

            public static float RunningPadV => L.runningPadV;

            public static float RunningGap => L.runningGap;

            public static float RunningThumb => L.runningThumb;

            public static float RunningLabelFont => L.runningLabelFont;

            public static float RunningLabelToBar => L.runningLabelToBar;

            public static float RunningBarHeight => L.runningBarHeight;

            public static float RunningBarTotalHeight => L.runningBarTotalHeight;

            public static float RunningTimeWidth => L.runningTimeWidth;

            public static float RunningTimeFont => L.runningTimeFont;



            public static float DetailWidth => L.detailPreferredWidth;

            public static float DetailPadding => L.detailPadding;

            public static float DetailGap => L.detailGap;

            public static float DetailHeroHeight => L.detailHeroHeight;

            public static float DetailTitleHeight => L.detailTitleHeight;

            public static float DetailBodyHeight => L.detailBodyHeight;

            public static float DetailBodyFlexibleHeight => L.detailBodyFlexibleHeight;

            public static float DetailReqLineHeight => L.detailReqLineHeight;

            public static float DetailBodyLineSpacing => L.detailBodyLineSpacing;

            public static float DetailTitleFont => L.detailTitleFont;

            public static float DetailBodyFont => L.detailBodyFont;

            public static float DetailReqFont => L.detailReqFont;

            public static bool DetailChildForceExpandWidth => L.detailChildForceExpandWidth;



            public static float InvPanelPadding => L.invPanelPadding;

            public static float InvPanelGap => L.invPanelGap;

            public static float InvPanelHeaderHeight => L.invPanelHeaderHeight;

            public static float InvPanelTitleFont => L.invPanelTitleFont;

            public static float InvPanelCloseSize => L.invPanelCloseSize;

            public static float InvPanelSlotWidth => L.invPanelSlotWidth;

            public static float InvPanelSlotHeight => L.invPanelSlotHeight;

            public static float InvPanelSlotGap => L.invPanelSlotGap;

            public static UnityEngine.Vector2 InvPanelSize => L.invPanelSize;

        }

    }

}

#endif

