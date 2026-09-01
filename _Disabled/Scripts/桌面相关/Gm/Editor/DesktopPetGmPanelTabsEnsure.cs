#if UNITY_EDITOR
using DesktopPet.Hub.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DesktopPet.Gm.Editor
{
    /// <summary>
    /// GmPanel：校验分页结构；可选只修已有节点布局（不 new 补签）。
    /// </summary>
    public static class DesktopPetGmPanelTabsEnsure
    {
        private const float PanelWidth = 560f;
        private const float PanelHeight = 520f;

        /// <summary>校验 GmPanel 分页签结构；不写预制体。</summary>
        public static bool ValidatePrefabStructure(out string error)
        {
            error = null;
            GameObject root = PrefabUtility.LoadPrefabContents(DesktopHubApply.MainCanvasPrefabPath);
            try
            {
                Transform gm = root.transform.Find("GmPanel");
                if (gm == null)
                {
                    error = "MainCanvas.prefab 无 GmPanel。";
                    return false;
                }

                return ValidateTransform(gm, out error);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>校验场景/预制体实例上的 GmPanel；不写节点。</summary>
        public static bool ValidateTransform(Transform gmPanel, out string error)
        {
            error = null;
            if (gmPanel == null)
            {
                error = "GmPanel 为空。";
                return false;
            }

            if (gmPanel.Find("SubTabs") == null)
            {
                error = "GmPanel 缺少 SubTabs。请手改 MainCanvas.prefab。";
                return false;
            }

            if (FindPages(gmPanel) == null)
            {
                error = "GmPanel 缺少 Pages（或 PageScroll/Viewport/Pages）。请手改 MainCanvas.prefab。";
                return false;
            }

            if (gmPanel.Find("PageScroll") == null)
            {
                error = "GmPanel 缺少 PageScroll。请手改 MainCanvas.prefab。";
                return false;
            }

            Transform pages = FindPages(gmPanel);
            if (pages.Find("MoneyPage") == null || pages.Find("DecorPage") == null
                || pages.Find("LubyPage") == null || pages.Find("SavePage") == null)
            {
                error = "GmPanel 分页不完整（Money/Decor/Luby/Save）。请手改 MainCanvas.prefab。";
                return false;
            }

            return true;
        }

        /// <summary>
        /// 仅在结构已齐时重写固定高度 / 滚动 / 子项锚点；缺结构则报错不补节点。
        /// </summary>
        public static bool FixLayoutOnPrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(DesktopHubApply.MainCanvasPrefabPath);
            try
            {
                Transform gm = root.transform.Find("GmPanel");
                if (gm == null)
                {
                    Debug.LogError("[DesktopPetGM] MainCanvas.prefab 无 GmPanel。");
                    return false;
                }

                if (!ValidateTransform(gm, out string error))
                {
                    Debug.LogError("[DesktopPetGM] " + error + " 请手改预制体后再修布局。");
                    return false;
                }

                FixLayout(gm);
                PrefabUtility.SaveAsPrefabAsset(root, DesktopHubApply.MainCanvasPrefabPath);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>兼容旧菜单名；等同 <see cref="FixLayoutOnPrefab"/>。</summary>
        public static bool ForceFixLayoutOnPrefab() => FixLayoutOnPrefab();

        /// <summary>场景实例：只校验，不写节点。</summary>
        public static bool ValidateOnTransform(Transform gmPanel) =>
            ValidateTransform(gmPanel, out _);

        private static Transform FindPages(Transform gm)
        {
            Transform direct = gm.Find("Pages");
            if (direct != null)
                return direct;
            return gm.Find("PageScroll/Viewport/Pages");
        }

        /// <summary>固定面板高度；Pages 放进已有滚动视口；修 Luby 子项锚点叠压。不创建节点。</summary>
        private static void FixLayout(Transform gm)
        {
            Undo.RegisterFullObjectHierarchyUndo(gm.gameObject, "GM Panel Fixed Scroll Layout");

            ContentSizeFitter panelCsf = gm.GetComponent<ContentSizeFitter>();
            if (panelCsf != null)
                Object.DestroyImmediate(panelCsf, true);

            RectTransform gmRt = gm as RectTransform;
            if (gmRt != null)
            {
                gmRt.anchorMin = new Vector2(0.5f, 0.5f);
                gmRt.anchorMax = new Vector2(0.5f, 0.5f);
                gmRt.pivot = new Vector2(0.5f, 0.5f);
                gmRt.sizeDelta = new Vector2(PanelWidth, PanelHeight);
            }

            Transform pages = FindPages(gm);
            Transform pageScroll = gm.Find("PageScroll");
            if (pages == null || pageScroll == null)
                return;

            Transform viewport = pageScroll.Find("Viewport");
            if (viewport != null && pages.parent != viewport)
                pages.SetParent(viewport, false);
            ScrollRect scroll = pageScroll.GetComponent<ScrollRect>();
            if (scroll == null)
            {
                Debug.LogError("[DesktopPetGM] PageScroll 缺少 ScrollRect。请手改 MainCanvas.prefab。", pageScroll);
                return;
            }

            if (viewport != null)
                scroll.viewport = viewport as RectTransform;
            scroll.content = pages as RectTransform;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;

            LayoutElement scrollLe = pageScroll.GetComponent<LayoutElement>();
            if (scrollLe != null)
            {
                scrollLe.minHeight = 220f;
                scrollLe.preferredHeight = 260f;
                scrollLe.flexibleHeight = 1f;
            }

            LayoutElement pagesLe = pages.GetComponent<LayoutElement>();
            if (pagesLe != null)
                Object.DestroyImmediate(pagesLe, true);

            ContentSizeFitter pagesCsf = pages.GetComponent<ContentSizeFitter>();
            if (pagesCsf != null)
            {
                pagesCsf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                pagesCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            RectTransform pagesRt = pages as RectTransform;
            if (pagesRt != null)
            {
                pagesRt.anchorMin = new Vector2(0f, 1f);
                pagesRt.anchorMax = new Vector2(1f, 1f);
                pagesRt.pivot = new Vector2(0.5f, 1f);
                pagesRt.anchoredPosition = Vector2.zero;
                pagesRt.sizeDelta = new Vector2(0f, pagesRt.sizeDelta.y);
            }

            VerticalLayoutGroup pagesVlg = pages.GetComponent<VerticalLayoutGroup>();
            if (pagesVlg != null)
            {
                pagesVlg.childControlWidth = true;
                pagesVlg.childControlHeight = true;
                pagesVlg.childForceExpandWidth = true;
                pagesVlg.childForceExpandHeight = false;
            }

            for (int i = 0; i < pages.childCount; i++)
            {
                Transform page = pages.GetChild(i);
                PreparePageRoot(page);
                for (int c = 0; c < page.childCount; c++)
                    PrepareLayoutChild(page.GetChild(c));
            }

            Transform grant = FindDeep(pages, "GrantLuby");
            if (grant != null)
            {
                for (int c = 0; c < grant.childCount; c++)
                    PrepareLayoutChild(grant.GetChild(c));

                VerticalLayoutGroup grantVlg = grant.GetComponent<VerticalLayoutGroup>();
                if (grantVlg != null)
                {
                    grantVlg.childControlWidth = true;
                    grantVlg.childControlHeight = true;
                    grantVlg.childForceExpandWidth = true;
                    grantVlg.childForceExpandHeight = false;
                    grantVlg.spacing = 6f;
                }

                ContentSizeFitter grantCsf = grant.GetComponent<ContentSizeFitter>();
                if (grantCsf != null)
                {
                    grantCsf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                    grantCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                }

                LayoutElement gle = grant.GetComponent<LayoutElement>();
                if (gle != null)
                {
                    gle.minHeight = 36f;
                    gle.preferredHeight = -1f;
                    gle.flexibleHeight = 0f;
                }

                RectTransform grantRt = grant as RectTransform;
                if (grantRt != null)
                {
                    grantRt.anchorMin = new Vector2(0f, 1f);
                    grantRt.anchorMax = new Vector2(1f, 1f);
                    grantRt.pivot = new Vector2(0.5f, 1f);
                    grantRt.anchoredPosition = Vector2.zero;
                    grantRt.sizeDelta = new Vector2(0f, grantRt.sizeDelta.y);
                }
            }

            int order = 0;
            SetSiblingIf(gm.Find("Header"), ref order);
            SetSiblingIf(gm.Find("Stats"), ref order);
            SetSiblingIf(gm.Find("Status"), ref order);
            SetSiblingIf(gm.Find("SubTabs"), ref order);
            SetSiblingIf(gm.Find("PageScroll"), ref order);
        }

        private static void PreparePageRoot(Transform page)
        {
            if (page == null)
                return;
            RectTransform rt = page as RectTransform;
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = Vector2.zero;
            }

            LayoutElement le = page.GetComponent<LayoutElement>();
            if (le != null)
            {
                le.minHeight = 36f;
                le.preferredHeight = -1f;
                le.flexibleHeight = 0f;
            }

            VerticalLayoutGroup vlg = page.GetComponent<VerticalLayoutGroup>();
            if (vlg != null)
            {
                vlg.childControlWidth = true;
                vlg.childControlHeight = true;
                vlg.childForceExpandWidth = true;
                vlg.childForceExpandHeight = false;
                vlg.spacing = 8f;
            }
        }

        private static void PrepareLayoutChild(Transform child)
        {
            if (child == null)
                return;
            RectTransform rt = child as RectTransform;
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(0f, rt.sizeDelta.y);
            }

            LayoutElement le = child.GetComponent<LayoutElement>();
            if (le != null)
            {
                if (le.minHeight < 1f)
                    le.minHeight = 36f;
                if (le.preferredHeight < 1f)
                    le.preferredHeight = 36f;
            }
        }

        private static void SetSiblingIf(Transform t, ref int order)
        {
            if (t == null)
                return;
            t.SetSiblingIndex(order++);
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root == null)
                return null;
            if (root.name == name)
                return root;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDeep(root.GetChild(i), name);
                if (found != null)
                    return found;
            }

            return null;
        }
    }
}
#endif
