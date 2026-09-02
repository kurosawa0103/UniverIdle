#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace UniverIdle.Editor
{
    public static partial class MainUISetup
    {
        [MenuItem("UniverIdle/从场景同步布局参数")]
        public static void SyncLayoutParamsFromScene()
        {
            var root = GameObject.Find(RootName);
            if (root == null)
            {
                EditorUtility.DisplayDialog("UniverIdle", "当前场景没有 UniverIdle_MainUI，无法同步。", "确定");
                return;
            }

            var asset = LoadOrCreateLayoutAsset();
            CaptureLayoutFromRoot(root.transform, asset);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            Selection.activeObject = asset;
            EditorUtility.DisplayDialog("UniverIdle",
                $"已同步到 {MainUILayoutParams.DefaultAssetPath}\n" +
                $"侧栏 {asset.sidebarWidth:0} · 中间 {asset.centerPreferredWidth:0} · 右侧 {asset.detailPreferredWidth:0} · 顶栏 {asset.topBarHeight:0}",
                "确定");
        }

        private static MainUILayoutParams ResolveLayoutForBuild() => LoadOrCreateLayoutAsset();

        /// <summary>仅首次创建 asset；已有文件绝不整份覆盖。</summary>
        private static MainUILayoutParams LoadOrCreateLayoutAsset()
        {
            var asset = AssetDatabase.LoadAssetAtPath<MainUILayoutParams>(MainUILayoutParams.DefaultAssetPath);
            if (asset != null)
            {
                Debug.Log(
                    $"[UniverIdle] 按 MainUILayoutParams.asset 重建 · " +
                    $"Thumb {asset.cardThumbHeight:0}×{(asset.cardThumbWidth > 0f ? asset.cardThumbWidth.ToString("0") : "满宽")} · 卡 {asset.cardMinHeight:0}");
                return asset;
            }

            asset = ScriptableObject.CreateInstance<MainUILayoutParams>();
            AssetDatabase.CreateAsset(asset, MainUILayoutParams.DefaultAssetPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[UniverIdle] 首次创建 {MainUILayoutParams.DefaultAssetPath}（使用代码默认值，之后只由你改 Inspector）。");
            return asset;
        }

        /// <summary>把场景里读到的字段写入已有 asset 对象（就地修改，不用 CopySerialized）。</summary>
        private static void CaptureLayoutFromRoot(Transform root, MainUILayoutParams p)
        {
            var scaler = root.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                p.referenceResolution = scaler.referenceResolution;
                p.matchWidthOrHeight = scaler.matchWidthOrHeight;
            }

            var app = root.Find("App");
            if (app == null) return;

            CaptureTopBar(app.Find("TopBar"), p);

            var body = app.Find("Body");
            if (body == null) return;

            var bodyHlg = body.GetComponent<HorizontalLayoutGroup>();
            if (bodyHlg != null)
                p.bodyChildForceExpandWidth = bodyHlg.childForceExpandWidth;

            p.useBodyFlexSpacer = body.Find("BodyFlexSpacer") != null;
            CaptureLayoutElement(body.Find("Sidebar"), ref p.sidebarWidth);
            CaptureSidebar(body.Find("Sidebar"), p);
            CaptureLayoutElement(body.Find("Center"),
                ref p.centerPreferredWidth,
                ref p.centerFlexibleWidth);
            CaptureLayoutElement(body.Find("Detail"),
                ref p.detailPreferredWidth,
                ref p.detailFlexibleWidth);

            var divider = body.Find("VDivider");
            if (divider != null)
            {
                var le = divider.GetComponent<LayoutElement>();
                if (le != null && le.preferredWidth > 0f)
                    p.dividerThickness = le.preferredWidth;
            }

            CaptureWorkCenter(body.Find("Center/WorkView_scavenge"), p);
            CaptureDetailPanel(body.Find("Detail"), p);
            CaptureInventoryPanel(root.Find("InventoryOverlay/Panel"), p);
        }

        private static void CaptureSidebar(Transform sidebar, MainUILayoutParams p)
        {
            if (sidebar == null) return;

            var vlg = sidebar.GetComponent<VerticalLayoutGroup>();
            if (vlg == null) return;

            p.sidebarPadH = vlg.padding.left;
            p.sidebarPadV = vlg.padding.top;
            p.sidebarGap = vlg.spacing;
        }

        private static void CaptureActionCards(Transform workView, MainUILayoutParams p)
        {
            var cards = workView.Find("ActionCards");
            if (cards == null) return;

            CaptureLayoutElementHeight(cards, ref p.actionCardsRowHeight);

            if (cards.childCount == 0) return;

            var card = cards.GetChild(0);
            CaptureLayoutElementHeight(card, ref p.cardMinHeight);

            var cardVlg = card.GetComponent<VerticalLayoutGroup>();
            if (cardVlg != null)
            {
                p.cardPadding = cardVlg.padding.left;
                p.cardVlgSpacing = cardVlg.spacing;
            }

            var thumb = card.Find("Thumb");
            if (thumb != null)
            {
                CaptureLayoutElementHeight(thumb, ref p.cardThumbHeight);
                CaptureLayoutElementWidth(thumb, ref p.cardThumbWidth);
            }

            var textIndex = 0;
            for (var i = 0; i < card.childCount; i++)
            {
                var child = card.GetChild(i);
                if (child.name == "Thumb" || child.name == "Meta") continue;
                if (child.name != "Text") continue;

                var childLe = child.GetComponent<LayoutElement>();
                if (childLe == null || childLe.preferredHeight <= 0f) continue;

                if (textIndex == 0)
                    p.cardTitleHeight = childLe.preferredHeight;
                textIndex++;
            }

            var meta = card.Find("Meta");
            if (meta != null)
                CaptureLayoutElementHeight(meta, ref p.cardMetaHeight);

            p.actionCardsRowHeight = Mathf.Max(p.actionCardsRowHeight, p.cardMinHeight);
        }

        private static void CaptureBannerTagHeight(Transform workView, MainUILayoutParams p)
        {
            var tags = workView.Find("LocationBanner/BannerText/Tags");
            if (tags == null || tags.childCount == 0) return;

            for (var i = 0; i < tags.childCount; i++)
            {
                var tagLe = tags.GetChild(i).GetComponent<LayoutElement>();
                if (tagLe == null || tagLe.preferredHeight <= 0f) continue;
                if (tagLe.preferredHeight > 48f) continue;
                p.tagHeight = tagLe.preferredHeight;
                return;
            }
        }

        private static void CaptureTopBar(Transform topBar, MainUILayoutParams p)
        {
            if (topBar == null) return;

            var le = topBar.GetComponent<LayoutElement>();
            if (le != null)
            {
                if (le.preferredHeight > 0f) p.topBarHeight = le.preferredHeight;
                if (le.minHeight > 0f) p.topBarHeight = Mathf.Max(p.topBarHeight, le.minHeight);
            }

            var hlg = topBar.GetComponent<HorizontalLayoutGroup>();
            if (hlg != null)
            {
                p.topBarPaddingH = hlg.padding.left;
                p.topBarPadV = hlg.padding.top;
                p.topBarGap = hlg.spacing;
            }

            var logoIcon = topBar.Find("Logo/LogoIcon");
            if (logoIcon is RectTransform logoRt)
                p.logoIconSize = logoRt.sizeDelta.x;

            var logo = topBar.Find("Logo");
            if (logo is RectTransform logoRow)
                p.topBarContentHeight = logoRow.sizeDelta.y > 0f ? logoRow.sizeDelta.y : p.topBarContentHeight;
        }

        private static void CaptureWorkCenter(Transform workView, MainUILayoutParams p)
        {
            if (workView == null) return;

            p.useCenterFlexSpacer = workView.Find("FlexSpacer") != null;

            var vlg = workView.GetComponent<VerticalLayoutGroup>();
            if (vlg != null)
            {
                p.centerPadding = vlg.padding.left;
                p.centerGap = vlg.spacing;
            }

            CaptureLayoutElementHeight(workView.Find("LocationBanner"), ref p.bannerHeight);
            CaptureActionCards(workView, p);
            CaptureLayoutElementHeight(workView.Find("RunningBar"), ref p.runningBarTotalHeight);

            CaptureLayoutElementHeight(workView.Find("LocationBanner/BannerText/Tags"), ref p.tagHeight);
            CaptureBannerTagHeight(workView, p);

            var bannerText = workView.Find("LocationBanner/BannerText");
            var bannerVlg = bannerText != null ? bannerText.GetComponent<VerticalLayoutGroup>() : null;
            if (bannerVlg != null)
            {
                p.bannerOverlayPadH = bannerVlg.padding.left;
                p.bannerOverlayPadV = bannerVlg.padding.top;
            }

            var running = workView.Find("RunningBar");
            var runningHlg = running != null ? running.GetComponent<HorizontalLayoutGroup>() : null;
            if (runningHlg != null)
            {
                p.runningPadH = runningHlg.padding.left;
                p.runningPadV = runningHlg.padding.top;
                p.runningGap = runningHlg.spacing;
            }
        }

        private static void CaptureDetailPanel(Transform detail, MainUILayoutParams p)
        {
            if (detail is not RectTransform detailRt) return;

            var le = detail.GetComponent<LayoutElement>();
            if (le != null)
            {
                if (le.preferredWidth > 0f) p.detailPreferredWidth = le.preferredWidth;
                if (le.minWidth > 0f) p.detailMinWidth = le.minWidth;
                if (le.flexibleWidth >= 0f) p.detailFlexibleWidth = le.flexibleWidth;
            }

            var vlg = detail.GetComponent<VerticalLayoutGroup>();
            if (vlg != null)
            {
                p.detailPadding = vlg.padding.left;
                p.detailGap = vlg.spacing;
                p.detailChildForceExpandWidth = vlg.childForceExpandWidth;
            }

            Canvas.ForceUpdateCanvases();
            var measured = Mathf.Max(LayoutUtility.GetPreferredWidth(detailRt), detailRt.rect.width);
            if (measured > 0f)
                p.detailPreferredWidth = measured;

            var hero = detail.Find("Hero");
            if (hero != null)
                CaptureLayoutElementHeight(hero, ref p.detailHeroHeight);

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
                            if (childLe.preferredHeight > 0f) p.detailTitleHeight = childLe.preferredHeight;
                            break;
                        case 1:
                            if (childLe.preferredHeight > 0f) p.detailBodyHeight = childLe.preferredHeight;
                            if (childLe.flexibleHeight >= 0f) p.detailBodyFlexibleHeight = childLe.flexibleHeight;
                            break;
                        default:
                            if (childLe.preferredHeight > 0f) p.detailReqLineHeight = childLe.preferredHeight;
                            break;
                    }
                }

                if (tmp != null)
                {
                    switch (textIndex)
                    {
                        case 0:
                            p.detailTitleFont = tmp.fontSize;
                            break;
                        case 1:
                            p.detailBodyFont = tmp.fontSize;
                            p.detailBodyLineSpacing = tmp.lineSpacing;
                            break;
                        default:
                            p.detailReqFont = tmp.fontSize;
                            break;
                    }
                }

                textIndex++;
            }
        }

        private static void CaptureInventoryPanel(Transform panel, MainUILayoutParams p)
        {
            if (panel is not RectTransform rt) return;
            p.invPanelSize = rt.sizeDelta;

            var vlg = panel.GetComponent<VerticalLayoutGroup>();
            if (vlg != null)
            {
                p.invPanelPadding = vlg.padding.left;
                p.invPanelGap = vlg.spacing;
            }
        }

        private static void CaptureLayoutElement(Transform t,
            ref float width, ref float flexW)
        {
            if (t == null) return;
            var le = t.GetComponent<LayoutElement>();
            if (le == null) return;
            if (le.preferredWidth > 0f) width = le.preferredWidth;
            if (le.flexibleWidth >= 0f) flexW = le.flexibleWidth;
        }

        private static void CaptureLayoutElement(Transform t, ref float width)
        {
            if (t == null) return;
            var le = t.GetComponent<LayoutElement>();
            if (le == null) return;
            if (le.preferredWidth > 0f) width = le.preferredWidth;
        }

        private static void CaptureLayoutElementHeight(Transform t, ref float height)
        {
            if (t == null) return;
            var le = t.GetComponent<LayoutElement>();
            if (le != null && le.preferredHeight > 0f)
                height = le.preferredHeight;
        }

        private static void CaptureLayoutElementWidth(Transform t, ref float width)
        {
            if (t == null) return;
            var le = t.GetComponent<LayoutElement>();
            if (le != null && le.preferredWidth > 0f)
                width = le.preferredWidth;
        }
    }
}
#endif
