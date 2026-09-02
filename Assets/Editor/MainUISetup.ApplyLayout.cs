#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace UniverIdle.Editor
{
    public static partial class MainUISetup
    {
        public static MainUILayoutParams GetLayoutAsset() => LoadOrCreateLayoutAsset();

        public static bool HasMainUiInScene() => GameObject.Find(RootName) != null;

        /// <summary>把 asset 参数套到当前场景主界面（不销毁重建）。</summary>
        public static bool ApplyLayoutToScene(MainUILayoutParams p = null)
        {
            var root = GameObject.Find(RootName);
            if (root == null)
            {
                Debug.LogWarning("[UniverIdle] 当前场景没有 UniverIdle_MainUI，无法应用布局。");
                return false;
            }

            p ??= LoadOrCreateLayoutAsset();
            SetActiveLayout(p);
            ApplyLayoutFromRoot(root.transform);
            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
            return true;
        }

        /// <summary>从场景读取布局写入 asset（不应用）。</summary>
        public static bool CaptureLayoutFromScene(MainUILayoutParams p = null)
        {
            var root = GameObject.Find(RootName);
            if (root == null) return false;

            p ??= LoadOrCreateLayoutAsset();
            CaptureLayoutFromRoot(root.transform, p);
            EditorUtility.SetDirty(p);
            return true;
        }

        [MenuItem("UniverIdle/应用布局参数到场景")]
        public static void ApplyLayoutToSceneMenu()
        {
            if (!ApplyLayoutToScene())
            {
                EditorUtility.DisplayDialog("UniverIdle", "当前场景没有 UniverIdle_MainUI。", "确定");
                return;
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[UniverIdle] 已把 MainUILayoutParams 应用到场景。");
        }

        [MenuItem("UniverIdle/应用背包布局参数到场景")]
        public static void ApplyInventoryLayoutToScene()
        {
            if (!ApplyLayoutToScene())
                EditorUtility.DisplayDialog("UniverIdle", "当前场景没有 UniverIdle_MainUI。", "确定");
            else
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        private static void ApplyLayoutFromRoot(Transform root)
        {
            var scaler = root.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.referenceResolution = ConceptLayout.ReferenceResolution;
                scaler.matchWidthOrHeight = ConceptLayout.MatchWidthOrHeight;
            }

            var app = root.Find("App");
            if (app == null) return;

            ApplyTopBar(app.Find("TopBar"));
            ApplyBody(app.Find("Body"));
            ApplyDetailPanel(app.Find("Body/Detail"));
            ApplyInventoryPanelLayout(root.Find("InventoryOverlay/Panel"), GetLayoutAsset());

            var center = app.Find("Body/Center");
            if (center != null)
            {
                for (var i = 0; i < center.childCount; i++)
                {
                    var child = center.GetChild(i);
                    if (child.name.StartsWith("WorkView_"))
                        ApplyWorkCenter(child);
                }
            }

            if (root is RectTransform rootRt)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rootRt);
        }

        private static void ApplyTopBar(Transform topBar)
        {
            if (topBar is RectTransform rt)
                LockLayoutHeight(rt, ConceptLayout.TopBarHeight);

            var hlg = topBar.GetComponent<HorizontalLayoutGroup>();
            if (hlg != null)
            {
                var padH = Mathf.RoundToInt(ConceptLayout.TopBarPaddingH);
                hlg.padding = new RectOffset(padH, padH,
                    Mathf.RoundToInt(ConceptLayout.TopBarPadV),
                    Mathf.RoundToInt(ConceptLayout.TopBarPadV));
                hlg.spacing = ConceptLayout.TopBarGap;
                hlg.childForceExpandHeight = false;
            }

            var logoIcon = topBar.Find("Logo/LogoIcon") as RectTransform;
            if (logoIcon != null)
            {
                logoIcon.sizeDelta = new Vector2(ConceptLayout.LogoIconSize, ConceptLayout.LogoIconSize);
                AddLayout(logoIcon.gameObject, ConceptLayout.LogoIconSize, ConceptLayout.LogoIconSize);
            }

            var logo = topBar.Find("Logo");
            if (logo != null)
                AddLayout(logo.gameObject, 0, ConceptLayout.TopBarContentHeight);

            foreach (Transform child in topBar)
            {
                if (!child.name.StartsWith("Btn_")) continue;
                AddLayout(child.gameObject, 0, ConceptLayout.TopBarContentHeight);
            }
        }

        private static void ApplyBody(Transform body)
        {
            if (body == null) return;

            var hlg = body.GetComponent<HorizontalLayoutGroup>();
            if (hlg != null)
                hlg.childForceExpandWidth = ConceptLayout.BodyChildForceExpandWidth;

            ApplyLayoutWidth(body.Find("Sidebar"), ConceptLayout.SidebarWidth, flexibleWidth: 0);
            ApplyLayoutWidth(body.Find("Center"), ConceptLayout.CenterPreferredWidth, ConceptLayout.CenterFlexibleWidth);
            ApplyLayoutWidth(body.Find("Detail"), ConceptLayout.DetailPreferredWidth, ConceptLayout.DetailFlexibleWidth,
                minWidth: ConceptLayout.DetailMinWidth);

            var divider = body.Find("VDivider");
            if (divider != null)
            {
                var le = divider.GetComponent<LayoutElement>() ?? divider.gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = ConceptLayout.DividerThickness;
                le.minWidth = ConceptLayout.DividerThickness;
                le.flexibleWidth = 0;
            }
        }

        private static void ApplyLayoutWidth(Transform t, float preferredWidth, float flexibleWidth, float minWidth = -1f)
        {
            if (t == null) return;
            var le = t.GetComponent<LayoutElement>() ?? t.gameObject.AddComponent<LayoutElement>();
            if (preferredWidth > 0f) le.preferredWidth = preferredWidth;
            if (minWidth > 0f) le.minWidth = minWidth;
            if (flexibleWidth >= 0f) le.flexibleWidth = flexibleWidth;
        }

        private static void ApplyWorkCenter(Transform workView)
        {
            if (workView == null) return;

            var vlg = workView.GetComponent<VerticalLayoutGroup>();
            if (vlg != null)
            {
                var pad = Mathf.RoundToInt(ConceptLayout.CenterPadding);
                vlg.padding = new RectOffset(pad, pad, pad, pad);
                vlg.spacing = ConceptLayout.CenterGap;
                vlg.childForceExpandHeight = false;
            }

            var banner = workView.Find("LocationBanner");
            if (banner is RectTransform bannerRt)
            {
                var bannerVlg = banner.GetComponent<VerticalLayoutGroup>();
                if (bannerVlg != null)
                    bannerVlg.spacing = ConceptLayout.CenterGap;

                var bannerLe = banner.GetComponent<LayoutElement>() ?? banner.gameObject.AddComponent<LayoutElement>();
                bannerLe.flexibleHeight = 0;
            }

            var bannerArt = FindWorkViewBannerArt(workView);
            if (bannerArt is RectTransform artRt)
                LockLayoutHeight(artRt, ConceptLayout.BannerHeight);

            var bannerText = FindWorkViewBannerText(workView);
            var bannerTextVlg = bannerText != null ? bannerText.GetComponent<VerticalLayoutGroup>() : null;
            if (bannerTextVlg != null)
            {
                var padH = Mathf.RoundToInt(ConceptLayout.BannerOverlayPadH);
                var padV = Mathf.RoundToInt(ConceptLayout.BannerOverlayPadV);
                bannerTextVlg.padding = new RectOffset(padH, padH, padV, padV);
            }

            var tags = bannerText != null ? bannerText.Find("Tags") : null;
            if (tags is RectTransform tagsRt)
                LockLayoutHeight(tagsRt, ConceptLayout.TagHeight);

            ApplyActionCards(FindWorkViewActionCards(workView));

            var running = workView.Find("RunningBar");
            if (running is RectTransform runningRt)
                LockLayoutHeight(runningRt, ConceptLayout.RunningBarTotalHeight);

            var runningHlg = running != null ? running.GetComponent<HorizontalLayoutGroup>() : null;
            if (runningHlg != null)
            {
                runningHlg.padding = new RectOffset(
                    Mathf.RoundToInt(ConceptLayout.RunningPadH),
                    Mathf.RoundToInt(ConceptLayout.RunningPadH),
                    Mathf.RoundToInt(ConceptLayout.RunningPadV),
                    Mathf.RoundToInt(ConceptLayout.RunningPadV));
                runningHlg.spacing = ConceptLayout.RunningGap;
            }

            if (workView is RectTransform workRt)
                LayoutRebuilder.ForceRebuildLayoutImmediate(workRt);
        }

        private static void ApplyActionCards(Transform cardsRow)
        {
            if (cardsRow is not RectTransform rowRt) return;

            ConfigureActionCardsGrid(rowRt);

            for (var i = 0; i < cardsRow.childCount; i++)
            {
                var card = cardsRow.GetChild(i);
                if (card is not RectTransform cardRt) continue;

                var cardLe = card.GetComponent<LayoutElement>();
                if (cardLe != null)
                {
                    cardLe.preferredHeight = ConceptLayout.ActionCardHeight;
                    cardLe.flexibleHeight = 0;
                    cardLe.flexibleWidth = 0;
                    cardLe.minWidth = -1;
                }

                var cardVlg = card.GetComponent<VerticalLayoutGroup>();
                if (cardVlg != null)
                {
                    var pad = Mathf.RoundToInt(ConceptLayout.CardPadding);
                    cardVlg.padding = new RectOffset(pad, pad, pad, pad);
                    cardVlg.spacing = ConceptLayout.CardVlgSpacing;
                }

                var thumb = card.Find("Thumb") as RectTransform;
                if (thumb != null)
                    ApplyCardThumbLayout(thumb);

                var thumbInner = card.Find("Thumb/ThumbInner") as RectTransform;
                if (thumbInner != null)
                {
                    var innerW = ConceptLayout.CardThumbInnerWidth;
                    var innerH = ConceptLayout.CardThumbInnerHeight;
                    Center(thumbInner, innerW, innerH);
                }

                ApplyCardTextHeights(card);
            }
        }

        private static void ApplyCardTextHeights(Transform card)
        {
            var textIndex = 0;
            for (var i = 0; i < card.childCount; i++)
            {
                var child = card.GetChild(i);
                if (child.name == "Meta")
                {
                    if (child is RectTransform metaRt)
                        LockLayoutHeight(metaRt, ConceptLayout.CardMetaHeight);
                    continue;
                }

                if (child.name != "Text") continue;
                if (child is not RectTransform textRt) continue;

                var height = textIndex == 0 ? ConceptLayout.CardTitleHeight : ConceptLayout.CardMetaHeight;
                LockLayoutHeight(textRt, height);
                textIndex++;
            }
        }

        private static void ApplyDetailPanel(Transform detail)
        {
            if (detail == null) return;

            ApplyLayoutWidth(detail, ConceptLayout.DetailPreferredWidth, ConceptLayout.DetailFlexibleWidth,
                ConceptLayout.DetailMinWidth);

            var vlg = detail.GetComponent<VerticalLayoutGroup>();
            if (vlg != null)
            {
                var pad = Mathf.RoundToInt(ConceptLayout.DetailPadding);
                vlg.padding = new RectOffset(pad, pad, pad, pad);
                vlg.spacing = ConceptLayout.DetailGap;
                vlg.childForceExpandWidth = ConceptLayout.DetailChildForceExpandWidth;
            }

            var hero = detail.Find("Hero");
            if (hero is RectTransform heroRt)
            {
                var heroLe = hero.GetComponent<LayoutElement>() ?? hero.gameObject.AddComponent<LayoutElement>();
                heroLe.flexibleWidth = 1;
                LockLayoutHeight(heroRt, ConceptLayout.DetailHeroHeight);

                var thumb = hero.Find("HeroThumb") ?? hero.Find("Shrimp");
                if (thumb is RectTransform thumbRt)
                {
                    Center(thumbRt, ConceptLayout.DetailHeroThumbWidth, ConceptLayout.DetailHeroThumbHeight);
                    thumbRt.sizeDelta = new Vector2(ConceptLayout.DetailHeroThumbWidth, ConceptLayout.DetailHeroThumbHeight);
                }
            }

            var textIndex = 0;
            for (var i = 0; i < detail.childCount; i++)
            {
                var child = detail.GetChild(i);
                if (child.name != "Text") continue;

                var childLe = child.GetComponent<LayoutElement>();
                var tmp = child.GetComponent<TextMeshProUGUI>();
                if (childLe != null)
                {
                    switch (textIndex)
                    {
                        case 0:
                            LockLayoutHeight(child as RectTransform, ConceptLayout.DetailTitleHeight);
                            break;
                        case 1:
                            LockLayoutHeight(child as RectTransform, ConceptLayout.DetailBodyHeight);
                            childLe.flexibleHeight = ConceptLayout.DetailBodyFlexibleHeight;
                            break;
                        default:
                            LockLayoutHeight(child as RectTransform, ConceptLayout.DetailReqLineHeight);
                            break;
                    }
                }

                if (tmp != null)
                {
                    switch (textIndex)
                    {
                        case 0:
                            tmp.fontSize = ConceptLayout.DetailTitleFont;
                            break;
                        case 1:
                            tmp.fontSize = ConceptLayout.DetailBodyFont;
                            tmp.lineSpacing = ConceptLayout.DetailBodyLineSpacing;
                            break;
                        default:
                            tmp.fontSize = ConceptLayout.DetailReqFont;
                            break;
                    }
                }

                textIndex++;
            }

            if (detail is RectTransform detailRt)
                LayoutRebuilder.ForceRebuildLayoutImmediate(detailRt);
        }

        private static void ApplyInventoryPanelLayout(Transform panel, MainUILayoutParams p)
        {
            if (panel is not RectTransform panelRt) return;

            panelRt.sizeDelta = p.invPanelSize;

            var vlg = panel.GetComponent<VerticalLayoutGroup>();
            if (vlg != null)
            {
                var pad = Mathf.RoundToInt(p.invPanelPadding);
                vlg.padding = new RectOffset(pad, pad, pad, pad);
                vlg.spacing = p.invPanelGap;
                vlg.childForceExpandWidth = true;
                vlg.childForceExpandHeight = false;
            }

            var header = panel.Find("Header") as RectTransform;
            if (header != null)
            {
                LockLayoutHeight(header, p.invPanelHeaderHeight);
                var headerHlg = header.GetComponent<HorizontalLayoutGroup>();
                if (headerHlg != null)
                    headerHlg.childForceExpandHeight = false;
            }

            var titleTmp = header != null ? header.GetComponentInChildren<TextMeshProUGUI>() : null;
            if (titleTmp != null)
            {
                titleTmp.fontSize = p.invPanelTitleFont;
                var titleHeight = Mathf.RoundToInt(p.invPanelTitleFont * 1.35f);
                LockLayoutHeight(titleTmp.rectTransform, titleHeight);
            }

            var close = panel.Find("Header/Btn_Close") as RectTransform;
            if (close != null)
            {
                LockLayoutHeight(close, p.invPanelCloseSize);
                var closeLe = close.GetComponent<LayoutElement>() ?? close.gameObject.AddComponent<LayoutElement>();
                closeLe.preferredWidth = p.invPanelCloseSize;
                closeLe.minWidth = p.invPanelCloseSize;
                closeLe.flexibleWidth = 0;
            }

            var body = panel.Find("Body");
            if (body != null)
            {
                var bodyLe = body.GetComponent<LayoutElement>() ?? body.gameObject.AddComponent<LayoutElement>();
                bodyLe.flexibleHeight = 1;
                bodyLe.minHeight = 0;
            }

            var grid = panel.Find("Body/Scroll/Viewport/Content")?.GetComponent<GridLayoutGroup>();
            if (grid != null)
            {
                grid.cellSize = new Vector2(p.invPanelSlotWidth, p.invPanelSlotHeight);
                grid.spacing = new Vector2(p.invPanelSlotGap, p.invPanelSlotGap);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(panelRt);
        }
    }
}
#endif
