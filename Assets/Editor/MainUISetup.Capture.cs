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

            var asset = SaveCapturedLayout(CaptureLayoutFromRoot(root.transform));
            Selection.activeObject = asset;
            EditorUtility.DisplayDialog("UniverIdle",
                $"已同步到 {MainUILayoutParams.DefaultAssetPath}\n" +
                $"侧栏 {asset.sidebarWidth:0} · 中间 {asset.centerPreferredWidth:0} · 右侧 {asset.detailPreferredWidth:0} · 顶栏 {asset.topBarHeight:0}",
                "确定");
        }

        private static MainUILayoutParams ResolveLayoutForBuild()
        {
            var root = GameObject.Find(RootName);
            if (root != null)
            {
                var captured = CaptureLayoutFromRoot(root.transform);
                var asset = SaveCapturedLayout(captured);
                Debug.Log(
                    $"[UniverIdle] 已从场景捕获布局：{asset.referenceResolution.x:0}×{asset.referenceResolution.y:0}，" +
                    $"侧栏 {asset.sidebarWidth:0}，中间 {asset.centerPreferredWidth:0}，右侧 {asset.detailPreferredWidth:0}，顶栏 {asset.topBarHeight:0}");
                return asset;
            }

            var existing = AssetDatabase.LoadAssetAtPath<MainUILayoutParams>(MainUILayoutParams.DefaultAssetPath);
            if (existing != null) return existing;

            var defaults = ScriptableObject.CreateInstance<MainUILayoutParams>();
            SaveCapturedLayout(defaults);
            Debug.Log($"[UniverIdle] 场景无 UI，使用默认布局参数：{MainUILayoutParams.DefaultAssetPath}");
            return defaults;
        }

        private static MainUILayoutParams SaveCapturedLayout(MainUILayoutParams source)
        {
            var asset = AssetDatabase.LoadAssetAtPath<MainUILayoutParams>(MainUILayoutParams.DefaultAssetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<MainUILayoutParams>();
                AssetDatabase.CreateAsset(asset, MainUILayoutParams.DefaultAssetPath);
            }

            EditorUtility.CopySerialized(source, asset);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            Object.DestroyImmediate(source);
            return asset;
        }

        private static MainUILayoutParams CaptureLayoutFromRoot(Transform root)
        {
            var p = LoadOrCreateLayoutParamsWorkingCopy();

            var scaler = root.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                p.referenceResolution = scaler.referenceResolution;
                p.matchWidthOrHeight = scaler.matchWidthOrHeight;
            }

            var app = root.Find("App");
            if (app == null) return p;

            CaptureTopBar(app.Find("TopBar"), p);

            var body = app.Find("Body");
            if (body == null) return p;

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

            return p;
        }

        /// <summary>以已有 asset 为底，只覆盖能从场景读到的字段；避免重建把未捕获参数打回代码默认值。</summary>
        private static MainUILayoutParams LoadOrCreateLayoutParamsWorkingCopy()
        {
            var existing = AssetDatabase.LoadAssetAtPath<MainUILayoutParams>(MainUILayoutParams.DefaultAssetPath);
            return existing != null
                ? Object.Instantiate(existing)
                : ScriptableObject.CreateInstance<MainUILayoutParams>();
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
            CaptureLayoutElementHeight(workView.Find("ActionCards"), ref p.actionCardsRowHeight);
            CaptureLayoutElementHeight(workView.Find("RunningBar"), ref p.runningBarTotalHeight);

            CaptureLayoutElementHeight(workView.Find("LocationBanner/BannerText/Tags"), ref p.tagHeight);

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
    }
}
#endif
