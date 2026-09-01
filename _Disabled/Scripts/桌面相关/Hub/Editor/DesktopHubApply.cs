#if UNITY_EDITOR
using DesktopPet.Environment;
using DesktopPet.Gm.Editor;
using DesktopPet.Hub;
using DesktopPet.Inventory;
using DesktopPet.Luby;
using DesktopPet.Settings;
using DesktopPet.Shop;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DesktopPet.Hub.Editor
{
    /// <summary>
    /// 单一主面板：菜单 + 页签。UI 源为 MainCanvas.prefab（内含 DesktopHubPanel 子节点）。
    /// 「应用主面板」：用 MainCanvas.prefab 替换 Demo 中的 MainCanvas 并接线。
    /// </summary>
    public static class DesktopHubApply
    {
        public const string MainCanvasPrefabPath = "Assets/Resources/Prefabs/杂项预制体/MainCanvas.prefab";
        private const string DemoPath = "Assets/Scenes/Demo.unity";
        private const string InvSlotPath = "Assets/Resources/Prefabs/ShopUI/ShopInventorySlot.prefab";
        private const string ShopSlotPath = "Assets/Resources/Prefabs/ShopUI/ShopItemSlot.prefab";
        private const string CarouselPath = "Assets/Resources/Prefabs/LubyUI/LubyCarouselItem.prefab";

        [MenuItem("桌宠重建/应用主面板预制体", false, 5)]
        public static void ApplyMenu()
        {
            // 0=应用 1=取消 2=应用并修 GM 布局
            int choice = EditorUtility.DisplayDialogComplex(
                "应用 MainCanvas 预制体",
                "用现有 MainCanvas.prefab 替换 Demo 中的 MainCanvas，并接线商店/仓库/领养/图鉴/场景/设置。\n\n"
                + "「应用」：校验 GM / 图鉴结构，缺则中止（不写预制体）。\n"
                + "「应用并修 GM 布局」：校验通过后仅重写 GmPanel 固定高度/滚动/锚点（不补签、不 new 节点；缺结构请手改预制体）。\n"
                + "缺页签或节点时请手改 MainCanvas.prefab。",
                "应用",
                "取消",
                "应用并修 GM 布局");

            if (choice == 1)
                return;

            if (AssetDatabase.LoadAssetAtPath<GameObject>(MainCanvasPrefabPath) == null)
            {
                EditorUtility.DisplayDialog(
                    "缺少 MainCanvas 预制体",
                    $"找不到 {MainCanvasPrefabPath}。请手改或从版本库恢复后再应用。",
                    "确定");
                return;
            }

            if (choice == 2)
            {
                if (!DesktopHubCodexPageEnsure.ValidateTabsAndPage(out string codexError))
                {
                    EditorUtility.DisplayDialog("图鉴结构不完整", codexError, "确定");
                    return;
                }

                if (!DesktopPetGmPanelTabsEnsure.FixLayoutOnPrefab())
                {
                    EditorUtility.DisplayDialog(
                        "GM 布局未改",
                        "GmPanel 结构不完整或修布局失败。请手改 MainCanvas.prefab 后重试（不再自动补签）。",
                        "确定");
                    return;
                }
            }
            else
            {
                string gmError = null;
                string codexError = null;
                bool gmOk = DesktopPetGmPanelTabsEnsure.ValidatePrefabStructure(out gmError);
                bool codexOk = DesktopHubCodexPageEnsure.ValidateTabsAndPage(out codexError);
                if (!gmOk || !codexOk)
                {
                    string msg = string.Empty;
                    if (!string.IsNullOrEmpty(gmError))
                        msg += gmError + "\n";
                    if (!string.IsNullOrEmpty(codexError))
                        msg += codexError;
                    EditorUtility.DisplayDialog(
                        "预制体结构不完整",
                        msg.Trim()
                        + "\n\n请手改 MainCanvas.prefab 后重试。",
                        "确定");
                    return;
                }
            }

            Scene scene = EditorSceneManager.OpenScene(DemoPath, OpenSceneMode.Single);
            ApplyMainCanvasPrefabToLoadedScene();
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[HubUI] 已应用 MainCanvas 到 Demo。");
        }

        /// <summary>批处理：打开 Demo 并应用主面板（无对话框）。</summary>
        public static void BatchApplyMainCanvasToDemo()
        {
            Scene scene = EditorSceneManager.OpenScene(DemoPath, OpenSceneMode.Single);
            ApplyMainCanvasPrefabToLoadedScene();
            EditorSceneManager.SaveScene(scene);
        }

        private static void ApplyMainCanvasPrefabToLoadedScene()
        {
            GameObject canvasPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MainCanvasPrefabPath);
            if (canvasPrefab == null)
            {
                Debug.LogError($"[HubUI] 缺少 {MainCanvasPrefabPath}。");
                return;
            }

            Canvas oldCanvas = FindMainCanvas();
            Transform parent = oldCanvas != null ? oldCanvas.transform.parent : null;
            int siblingIndex = oldCanvas != null ? oldCanvas.transform.GetSiblingIndex() : -1;

            if (oldCanvas != null)
                Object.DestroyImmediate(oldCanvas.gameObject);

            GameObject canvasGo = parent != null
                ? (GameObject)PrefabUtility.InstantiatePrefab(canvasPrefab, parent)
                : (GameObject)PrefabUtility.InstantiatePrefab(canvasPrefab);
            canvasGo.name = "MainCanvas";
            if (siblingIndex >= 0)
                canvasGo.transform.SetSiblingIndex(siblingIndex);

            Transform canvas = canvasGo.transform;

            Button menuBtn = canvas.Find("OpenMenuBtn")?.GetComponent<Button>();
            if (menuBtn == null)
            {
                Debug.LogError("[HubUI] MainCanvas 下缺少 OpenMenuBtn。请在 MainCanvas.prefab 内补齐后再应用。");
                return;
            }

            Transform hubTf = canvas.Find("DesktopHubPanel");
            if (hubTf == null)
            {
                Debug.LogError("[HubUI] MainCanvas 下缺少 DesktopHubPanel 子节点。");
                return;
            }

            GameObject panel = hubTf.gameObject;
            panel.SetActive(false);

            RectTransform returnZone = canvas.Find("ReturnDropZone") as RectTransform;
            if (returnZone == null)
            {
                Debug.LogError("[HubUI] MainCanvas 下缺少 ReturnDropZone。请在 MainCanvas.prefab 内补齐后再应用。");
                return;
            }

            returnZone.gameObject.SetActive(false);

            DesktopHubUIController hub = EnsureHubController();
            if (hub == null)
                return;
            WireAll(hub, panel, menuBtn, returnZone, canvas);
            DesktopPetGmSetup.WireExistingGmUiInScene(save: false);
            EditorUtility.SetDirty(hub);
            EditorUtility.SetDirty(canvasGo);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        private static DesktopHubUIController EnsureHubController()
        {
            GameObject go = GameObject.Find("HubSystem");
            if (go == null)
            {
                Debug.LogError("[HubUI] 场景缺少 HubSystem。请在 Demo 场景补齐后再应用。");
                return null;
            }

            DesktopHubUIController hub = go.GetComponent<DesktopHubUIController>();
            if (hub == null)
                Debug.LogError("[HubUI] HubSystem 缺少 DesktopHubUIController。");
            return hub;
        }

        private static T GetHostComponent<T>(string hostName) where T : Component
        {
            GameObject host = GameObject.Find(hostName);
            return host != null ? host.GetComponent<T>() : null;
        }

        private static void WireAll(
            DesktopHubUIController hub,
            GameObject panel,
            Button menuBtn,
            RectTransform returnDropZone,
            Transform mainCanvas)
        {
            DesktopHubPanelBinding binding = panel.GetComponent<DesktopHubPanelBinding>();
            if (binding == null)
            {
                Debug.LogError("[HubUI] DesktopHubPanel 缺少 DesktopHubPanelBinding。");
                return;
            }

            ShopUIController shopUi = GetHostComponent<ShopUIController>("ShopSystem");
            InventoryUIController invUi = GetHostComponent<InventoryUIController>("InventorySystem");
            LubyUIController lubyUi = GetHostComponent<LubyUIController>("LubySystem");
            CodexUIController codexUi = GetHostComponent<CodexUIController>("LubySystem");
            SettingsUIController settingsUi = GetHostComponent<SettingsUIController>("SettingsSystem");

            if (shopUi == null)
                Debug.LogWarning("[HubUI] ShopSystem 缺少 ShopUIController。");
            if (invUi == null)
                Debug.LogWarning("[HubUI] InventorySystem 缺少 InventoryUIController。");
            if (lubyUi == null)
                Debug.LogWarning("[HubUI] LubySystem 缺少 LubyUIController。");
            if (codexUi == null)
                Debug.LogWarning("[HubUI] LubySystem 缺少 CodexUIController。");
            if (settingsUi == null)
                Debug.LogWarning("[HubUI] SettingsSystem 缺少 SettingsUIController。");

            SerializedObject so = new SerializedObject(hub);
            so.FindProperty("rootPanel").objectReferenceValue = panel;
            so.FindProperty("openMenuButton").objectReferenceValue = menuBtn;
            so.FindProperty("closeButton").objectReferenceValue = binding.closeButton;
            so.FindProperty("capacityText").objectReferenceValue = binding.capacityText;
            so.FindProperty("currencyText").objectReferenceValue = binding.currencyText;
            so.FindProperty("tabShopButton").objectReferenceValue = binding.tabShop;
            so.FindProperty("tabInventoryButton").objectReferenceValue = binding.tabInventory;
            so.FindProperty("tabLubyButton").objectReferenceValue = binding.tabLuby;
            so.FindProperty("tabCodexButton").objectReferenceValue = binding.tabCodex;
            so.FindProperty("tabSettingsButton").objectReferenceValue = binding.tabSettings;
            if (binding.tabScene != null)
                so.FindProperty("tabSceneButton").objectReferenceValue = binding.tabScene;
            so.FindProperty("shopPage").objectReferenceValue = binding.shopPage;
            so.FindProperty("inventoryPage").objectReferenceValue = binding.inventoryPage;
            so.FindProperty("lubyPage").objectReferenceValue = binding.lubyPage;
            so.FindProperty("codexPage").objectReferenceValue = binding.codexPage;
            so.FindProperty("settingsPage").objectReferenceValue = binding.settingsPage;
            if (binding.scenePage != null)
                so.FindProperty("scenePage").objectReferenceValue = binding.scenePage;
            so.FindProperty("shopUi").objectReferenceValue = shopUi;
            so.FindProperty("inventoryUi").objectReferenceValue = invUi;
            so.FindProperty("lubyUi").objectReferenceValue = lubyUi;
            so.FindProperty("codexUi").objectReferenceValue = codexUi;
            so.FindProperty("startOpen").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();

            WireShopPage(shopUi, binding.shopPage);
            WireInventoryPage(invUi, binding.inventoryPage, returnDropZone, mainCanvas);
            WireLubyPage(lubyUi, binding.lubyPage);
            WireCodexPage(codexUi, binding.codexPage);
            WireSettingsPage(settingsUi, binding.settingsPage);
        }

        private static void WireCodexPage(CodexUIController ui, GameObject page)
        {
            if (ui == null || page == null)
            {
                if (page == null)
                    Debug.LogWarning(
                        "[HubUI] 缺少 CodexPage。请在 MainCanvas.prefab 内补齐图鉴页后再应用。");
                    return;
            }

            Transform content = RequireChild(page.transform, "Body/GridScroll/Viewport/Content", "Codex");
            Transform detail = RequireChild(page.transform, "Body/Detail", "Codex");
            CodexAppearanceSlot slot = DesktopHubCodexPageEnsure.LoadSlotPrefab();

            SerializedObject so = new SerializedObject(ui);
            so.FindProperty("statusText").objectReferenceValue =
                RequireChild(page.transform, "Status", "Codex")?.GetComponent<TextMeshProUGUI>();
            so.FindProperty("gridContent").objectReferenceValue = content;
            if (slot != null)
                so.FindProperty("slotPrefab").objectReferenceValue = slot;
            if (detail != null)
            {
                so.FindProperty("detailIcon").objectReferenceValue =
                    RequireChild(detail, "Preview/Icon", "Codex")?.GetComponent<Image>();
                so.FindProperty("detailNameText").objectReferenceValue =
                    RequireChild(detail, "Name", "Codex")?.GetComponent<TextMeshProUGUI>();
                so.FindProperty("detailDescText").objectReferenceValue =
                    RequireChild(detail, "Desc", "Codex")?.GetComponent<TextMeshProUGUI>();
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireShopPage(ShopUIController ui, GameObject page)
        {
            if (ui == null || page == null)
                return;
            Transform content = RequireChild(page.transform, "ListArea/Viewport/Content", "Shop");
            TextMeshProUGUI status = RequireChild(page.transform, "Status", "Shop")?.GetComponent<TextMeshProUGUI>();
            ShopItemSlot slot = AssetDatabase.LoadAssetAtPath<ShopItemSlot>(ShopSlotPath);
            ShopManager shop = GetHostComponent<ShopManager>("ShopSystem");

            SerializedObject so = new SerializedObject(ui);
            so.FindProperty("statusText").objectReferenceValue = status;
            so.FindProperty("shopContent").objectReferenceValue = content;
            if (slot != null)
                so.FindProperty("shopSlotPrefab").objectReferenceValue = slot;
            if (shop != null)
                so.FindProperty("shop").objectReferenceValue = shop;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireInventoryPage(
            InventoryUIController ui,
            GameObject page,
            RectTransform returnDropZone,
            Transform mainCanvas)
        {
            if (ui == null || page == null)
                return;
            Transform content = RequireChild(page.transform, "Body/GridScroll/Viewport/Content", "Inventory");
            GameObject empty = RequireChild(page.transform, "Body/GridScroll/EmptyHint", "Inventory")?.gameObject;
            Transform detail = RequireChild(page.transform, "Body/Detail", "Inventory");
            InventorySlot slot = AssetDatabase.LoadAssetAtPath<InventorySlot>(InvSlotPath);
            ItemInventory inv = GetHostComponent<ItemInventory>("ShopSystem");

            SerializedObject so = new SerializedObject(ui);
            so.FindProperty("statusText").objectReferenceValue =
                RequireChild(page.transform, "Status", "Inventory")?.GetComponent<TextMeshProUGUI>();
            so.FindProperty("inventoryContent").objectReferenceValue = content;
            so.FindProperty("inventoryEmptyHint").objectReferenceValue = empty;
            so.FindProperty("returnDropZone").objectReferenceValue = returnDropZone;
            if (mainCanvas != null)
            {
                Transform menu = mainCanvas.Find("LubyDeskContextMenu");
                so.FindProperty("lubyDeskContextMenu").objectReferenceValue = menu as RectTransform;
                so.FindProperty("lubyDeskReturnButton").objectReferenceValue =
                    menu != null ? menu.Find("ReturnBtn")?.GetComponent<Button>() : null;
                so.FindProperty("lubyDeskInfoButton").objectReferenceValue =
                    menu != null ? menu.Find("InfoBtn")?.GetComponent<Button>() : null;
                so.FindProperty("lubyInfoPanel").objectReferenceValue =
                    mainCanvas.Find("LubyInfoPanel")?.GetComponent<LubyInfoPanelController>();
            }
            so.FindProperty("subDecorButton").objectReferenceValue =
                RequireChild(page.transform, "SubTabs/DecorTab", "Inventory")?.GetComponent<Button>();
            so.FindProperty("subLubyButton").objectReferenceValue =
                RequireChild(page.transform, "SubTabs/LubyTab", "Inventory")?.GetComponent<Button>();
            if (slot != null)
                so.FindProperty("inventorySlotPrefab").objectReferenceValue = slot;
            if (detail != null)
            {
                so.FindProperty("detailIcon").objectReferenceValue =
                    RequireChild(detail, "Preview/Icon", "Inventory")?.GetComponent<Image>();
                so.FindProperty("detailNameText").objectReferenceValue =
                    RequireChild(detail, "Name", "Inventory")?.GetComponent<TextMeshProUGUI>();
                so.FindProperty("detailDescText").objectReferenceValue =
                    RequireChild(detail, "Desc", "Inventory")?.GetComponent<TextMeshProUGUI>();
                so.FindProperty("actionButton").objectReferenceValue =
                    RequireChild(detail, "ActionBtn", "Inventory")?.GetComponent<Button>();
                so.FindProperty("actionButtonText").objectReferenceValue =
                    RequireChild(detail, "ActionBtn/Label", "Inventory")?.GetComponent<TextMeshProUGUI>();
            }

            if (inv != null)
                so.FindProperty("inventory").objectReferenceValue = inv;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireLubyPage(LubyUIController ui, GameObject page)
        {
            if (ui == null || page == null)
                return;
            LubyCarouselItem item = AssetDatabase.LoadAssetAtPath<LubyCarouselItem>(CarouselPath);
            Transform body = RequireChild(page.transform, "Body", "Luby");
            Transform detail = body != null ? RequireChild(body, "Detail", "Luby") : null;
            Transform carousel = body != null ? RequireChild(body, "Carousel", "Luby") : null;

            SerializedObject so = new SerializedObject(ui);
            so.FindProperty("statusText").objectReferenceValue =
                RequireChild(page.transform, "Status", "Luby")?.GetComponent<TextMeshProUGUI>();
            if (carousel != null)
            {
                so.FindProperty("carouselRoot").objectReferenceValue =
                    RequireChild(carousel, "Items", "Luby");
                so.FindProperty("prevButton").objectReferenceValue =
                    RequireChild(carousel, "Nav/Prev", "Luby")?.GetComponent<Button>();
                so.FindProperty("nextButton").objectReferenceValue =
                    RequireChild(carousel, "Nav/Next", "Luby")?.GetComponent<Button>();
            }

            if (item != null)
                so.FindProperty("carouselItemPrefab").objectReferenceValue = item;
            if (detail != null)
            {
                so.FindProperty("detailRoot").objectReferenceValue = detail.gameObject;
                so.FindProperty("detailIcon").objectReferenceValue =
                    RequireChild(detail, "Preview/Icon", "Luby")?.GetComponent<Image>();
                so.FindProperty("detailNameText").objectReferenceValue =
                    RequireChild(detail, "Name", "Luby")?.GetComponent<TextMeshProUGUI>();
                so.FindProperty("detailDescText").objectReferenceValue =
                    RequireChild(detail, "Desc", "Luby")?.GetComponent<TextMeshProUGUI>();
                so.FindProperty("rollButton").objectReferenceValue =
                    RequireChild(detail, "RollBtn", "Luby")?.GetComponent<Button>();
                so.FindProperty("rollPriceText").objectReferenceValue =
                    RequireChild(detail, "RollBtn/Price", "Luby")?.GetComponent<TextMeshProUGUI>();
                so.FindProperty("rollFillImage").objectReferenceValue =
                    RequireChild(detail, "RollBtn/Fill", "Luby")?.GetComponent<Image>();
                so.FindProperty("longPressHintText").objectReferenceValue =
                    RequireChild(detail, "LongPressHint", "Luby")?.GetComponent<TextMeshProUGUI>();
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireSettingsPage(SettingsUIController ui, GameObject page)
        {
            if (ui == null || page == null)
                return;

            SettingsPanelBinding nested = page.GetComponentInChildren<SettingsPanelBinding>(true);
            if (nested == null)
            {
                Debug.LogWarning(
                    "[HubUI] Settings 页缺少 SettingsPanelBinding，请在 MainCanvas.prefab 的 SettingsPage 内补齐后再应用。");
                return;
            }

            SettingsApplicator applicator = ui.GetComponent<SettingsApplicator>()
                                           ?? GetHostComponent<SettingsApplicator>("SettingsSystem");

            SerializedObject so = new SerializedObject(ui);
            if (applicator != null)
                so.FindProperty("applicator").objectReferenceValue = applicator;
            // 控件只认 SettingsPage 上的 SettingsPanelBinding，不再逐字段复制到 Controller
            so.FindProperty("panel").objectReferenceValue = nested;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Canvas FindMainCanvas()
        {
            GameObject go = GameObject.Find("MainCanvas");
            return go != null ? go.GetComponent<Canvas>() : null;
        }

        private static Transform RequireChild(Transform root, string path, string pageLabel)
        {
            if (root == null)
            {
                Debug.LogWarning($"[HubUI] {pageLabel} 接线失败：根节点为空（期望路径 {path}）。");
                return null;
            }

            Transform found = root.Find(path);
            if (found == null)
                Debug.LogWarning(
                    $"[HubUI] {pageLabel} 接线失败：找不到 `{path}`（在 `{root.name}` 下）。请在 MainCanvas.prefab 内补齐节点后再应用。");
            return found;
        }
    }
}
#endif
