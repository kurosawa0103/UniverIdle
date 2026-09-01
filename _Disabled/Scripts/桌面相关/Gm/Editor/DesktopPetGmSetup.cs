#if UNITY_EDITOR
using DesktopPet.Decor;
using DesktopPet.Luby;
using DesktopPet.Shop;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DesktopPet.Gm.Editor
{
    /// <summary>将 MainCanvas GM UI 接到 DesktopPetGmController。不生成 UI；缺节点则报错。</summary>
    public static class DesktopPetGmSetup
    {
        /// <summary>接线场景里已有的 MainCanvas GM UI（OpenGmBtn / GmPanel）。</summary>
        public static void WireExistingGmUiInScene(bool save = false)
        {
            Scene scene = SceneManager.GetActiveScene();
            GameObject host = GameObject.Find("GmSystem");
            if (host == null)
            {
                Debug.LogError("[DesktopPetGM] 场景缺少 GmSystem。请在 Demo 场景补齐后再应用。");
                return;
            }

            DesktopPetGmController gm = host.GetComponent<DesktopPetGmController>();
            if (gm == null)
            {
                Debug.LogError("[DesktopPetGM] GmSystem 缺少 DesktopPetGmController。");
                return;
            }

            if (!TryCollectGmUiFromMainCanvas(out GmUiRefs refs))
            {
                Transform canvasTf = FindMainCanvasTransform();
                Transform panelTf = canvasTf != null ? canvasTf.Find("GmPanel") : null;
                string gmError = null;
                bool panelOk = panelTf != null
                                 && DesktopPetGmPanelTabsEnsure.ValidateTransform(panelTf, out gmError);
                if (!panelOk)
                {
                    Debug.LogError(
                        "[DesktopPetGM] MainCanvas 下缺少 OpenGmBtn / GmPanel（或分页签未齐）。"
                        + (string.IsNullOrEmpty(gmError) ? "" : " " + gmError)
                        + " 请手改 MainCanvas.prefab 补齐后「应用主面板」。");
                    return;
                }

                if (!TryCollectGmUiFromMainCanvas(out refs))
                {
                    Debug.LogError(
                        "[DesktopPetGM] GmPanel 接线字段未齐。请手改 MainCanvas.prefab 补齐后「应用主面板」。");
                    return;
                }
            }

            if (refs.rootPanel.transform.Find("PageScroll/Viewport/Pages/LubyPage/GrantLuby") == null
                && refs.rootPanel.transform.Find("Pages/LubyPage/GrantLuby") == null
                && refs.rootPanel.transform.Find("GrantLuby") == null)
            {
                Debug.LogError(
                    "[DesktopPetGM] GmPanel 缺少 GrantLuby。请在 MainCanvas.prefab 的 LubyPage 下补齐后再「应用主面板」。",
                    refs.rootPanel);
            }

            BindGmController(gm, refs);
            EditorUtility.SetDirty(host);
            EditorUtility.SetDirty(refs.rootPanel);
            EditorSceneManager.MarkSceneDirty(scene);
            if (save)
                EditorSceneManager.SaveScene(scene);
        }

        private static void BindGmController(DesktopPetGmController gm, GmUiRefs refs)
        {
            ShopManager shop = GetHostComponent<ShopManager>("ShopSystem");
            SerializedObject so = new SerializedObject(gm);
            so.FindProperty("shop").objectReferenceValue = shop;
            so.FindProperty("world").objectReferenceValue = GetHostComponent<DecorWorld>("DecorSystem");
            so.FindProperty("lubyWorld").objectReferenceValue = GetHostComponent<LubyWorld>("LubySystem");
            so.FindProperty("lubyAcquisition").objectReferenceValue =
                GetHostComponent<LubyAcquisitionService>("LubySystem");
            so.FindProperty("rootPanel").objectReferenceValue = refs.rootPanel;
            so.FindProperty("openButton").objectReferenceValue = refs.openButton;
            so.FindProperty("closeButton").objectReferenceValue = refs.closeButton;
            so.FindProperty("add100Button").objectReferenceValue = refs.add100Button;
            so.FindProperty("add1000Button").objectReferenceValue = refs.add1000Button;
            so.FindProperty("resetSaveButton").objectReferenceValue = refs.resetSaveButton;
            so.FindProperty("clearPlacedButton").objectReferenceValue = refs.clearPlacedButton;
            so.FindProperty("grantAllButton").objectReferenceValue = refs.grantAllButton;
            so.FindProperty("clearLubiesButton").objectReferenceValue = refs.clearLubiesButton;
            so.FindProperty("removeLastLubyButton").objectReferenceValue = refs.removeLastLubyButton;
            so.FindProperty("statusText").objectReferenceValue = refs.statusText;
            so.FindProperty("currencyText").objectReferenceValue = refs.currencyText;
            so.FindProperty("lubyCountText").objectReferenceValue = refs.lubyCountText;

            so.FindProperty("tabMoneyButton").objectReferenceValue = refs.tabMoneyButton;
            so.FindProperty("tabDecorButton").objectReferenceValue = refs.tabDecorButton;
            so.FindProperty("tabLubyButton").objectReferenceValue = refs.tabLubyButton;
            so.FindProperty("tabSaveButton").objectReferenceValue = refs.tabSaveButton;
            so.FindProperty("pageMoney").objectReferenceValue = refs.pageMoney;
            so.FindProperty("pageDecor").objectReferenceValue = refs.pageDecor;
            so.FindProperty("pageLuby").objectReferenceValue = refs.pageLuby;
            so.FindProperty("pageSave").objectReferenceValue = refs.pageSave;

            so.FindProperty("grantTemplateRow").objectReferenceValue = refs.grantTemplateRow;
            so.FindProperty("grantAppearanceRow").objectReferenceValue = refs.grantAppearanceRow;
            so.FindProperty("grantPersonalityRow").objectReferenceValue = refs.grantPersonalityRow;
            so.FindProperty("grantTraitRow").objectReferenceValue = refs.grantTraitRow;
            so.FindProperty("grantTrait2Row").objectReferenceValue = refs.grantTrait2Row;
            so.FindProperty("grantSpecifiedButton").objectReferenceValue = refs.grantSpecifiedButton;

            so.FindProperty("startOpen").boolValue = false;
            so.ApplyModifiedProperties();
        }

        private static T GetHostComponent<T>(string hostName) where T : Component
        {
            GameObject host = GameObject.Find(hostName);
            return host != null ? host.GetComponent<T>() : null;
        }

        private struct GmUiRefs
        {
            public GameObject rootPanel;
            public Button openButton;
            public Button closeButton;
            public Button add100Button;
            public Button add1000Button;
            public Button resetSaveButton;
            public Button clearPlacedButton;
            public Button grantAllButton;
            public Button clearLubiesButton;
            public Button removeLastLubyButton;
            public TextMeshProUGUI statusText;
            public TextMeshProUGUI currencyText;
            public TextMeshProUGUI lubyCountText;
            public Button tabMoneyButton;
            public Button tabDecorButton;
            public Button tabLubyButton;
            public Button tabSaveButton;
            public GameObject pageMoney;
            public GameObject pageDecor;
            public GameObject pageLuby;
            public GameObject pageSave;
            public GmCycleRow grantTemplateRow;
            public GmCycleRow grantAppearanceRow;
            public GmCycleRow grantPersonalityRow;
            public GmCycleRow grantTraitRow;
            public GmCycleRow grantTrait2Row;
            public Button grantSpecifiedButton;
        }

        private static bool TryCollectGmUiFromMainCanvas(out GmUiRefs refs)
        {
            refs = new GmUiRefs();
            Transform canvasTf = FindMainCanvasTransform();
            if (canvasTf == null)
                return false;

            Transform openTf = canvasTf.Find("OpenGmBtn");
            Transform panelTf = canvasTf.Find("GmPanel");
            if (openTf == null || panelTf == null)
                return false;

            refs.openButton = openTf.GetComponent<Button>();
            refs.rootPanel = panelTf.gameObject;
            if (refs.openButton == null)
                return false;

            Transform subTabs = panelTf.Find("SubTabs");
            Transform pages = panelTf.Find("Pages")
                              ?? panelTf.Find("PageScroll/Viewport/Pages");
            if (subTabs == null || pages == null)
                return false;

            refs.tabMoneyButton = subTabs.Find("MoneyTab")?.GetComponent<Button>();
            refs.tabDecorButton = subTabs.Find("DecorTab")?.GetComponent<Button>();
            refs.tabLubyButton = subTabs.Find("LubyTab")?.GetComponent<Button>();
            refs.tabSaveButton = subTabs.Find("SaveTab")?.GetComponent<Button>();
            refs.pageMoney = pages.Find("MoneyPage")?.gameObject;
            refs.pageDecor = pages.Find("DecorPage")?.gameObject;
            refs.pageLuby = pages.Find("LubyPage")?.gameObject;
            refs.pageSave = pages.Find("SavePage")?.gameObject;

            refs.closeButton = FindButton(panelTf, "CloseBtn") ?? FindButton(panelTf, "×Btn");
            refs.add100Button = FindButton(panelTf, "+100Btn");
            refs.add1000Button = FindButton(panelTf, "+1000Btn");
            refs.grantAllButton = FindButton(panelTf, "发放全部Btn");
            refs.clearPlacedButton = FindButton(panelTf, "清空桌上Btn");
            refs.removeLastLubyButton = FindButton(panelTf, "移除最近Btn");
            refs.clearLubiesButton = FindButton(panelTf, "清空全部Btn");
            refs.resetSaveButton = FindButton(panelTf, "重置存档Btn");

            Transform stats = panelTf.Find("Stats");
            if (stats != null)
            {
                TextMeshProUGUI[] statsTexts = stats.GetComponentsInChildren<TextMeshProUGUI>(true);
                if (statsTexts.Length > 0)
                    refs.currencyText = statsTexts[0];
                if (statsTexts.Length > 1)
                    refs.lubyCountText = statsTexts[1];
            }

            Transform status = panelTf.Find("Status");
            if (status != null)
            {
                refs.statusText = status.GetComponent<TextMeshProUGUI>()
                                  ?? status.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            CollectGrantRefs(panelTf, ref refs);

            return refs.add100Button != null
                   && refs.resetSaveButton != null
                   && refs.tabMoneyButton != null
                   && refs.pageLuby != null;
        }

        private static void CollectGrantRefs(Transform panelTf, ref GmUiRefs refs)
        {
            Transform grant = panelTf.Find("PageScroll/Viewport/Pages/LubyPage/GrantLuby")
                              ?? panelTf.Find("Pages/LubyPage/GrantLuby")
                              ?? FindDeep(panelTf, "GrantLuby");
            if (grant == null)
                return;

            refs.grantTemplateRow = GetCycleRow(grant, "TemplateRow");
            refs.grantAppearanceRow = GetCycleRow(grant, "AppearanceRow");
            refs.grantPersonalityRow = GetCycleRow(grant, "PersonalityRow");
            refs.grantTraitRow = GetCycleRow(grant, "TraitRow");
            refs.grantTrait2Row = GetCycleRow(grant, "Trait2Row");
            refs.grantSpecifiedButton = FindButton(grant, "指定获得Btn");
        }

        private static GmCycleRow GetCycleRow(Transform grant, string rowName)
        {
            Transform row = grant.Find(rowName);
            if (row == null)
                return null;
            GmCycleRow cycle = row.GetComponent<GmCycleRow>();
            if (cycle != null)
                cycle.Resolve();
            return cycle;
        }

        private static Button FindButton(Transform root, string name)
        {
            Transform t = FindDeep(root, name);
            return t != null ? t.GetComponent<Button>() : null;
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

        private static Transform FindMainCanvasTransform()
        {
            GameObject main = GameObject.Find("MainCanvas");
            return main != null ? main.transform : null;
        }
    }
}
#endif
