#if UNITY_EDITOR
using DesktopPet.Luby;
using UnityEditor;
using UnityEngine;

namespace DesktopPet.Hub.Editor
{
    /// <summary>
    /// MainCanvas：仅缺时补 TabCodex + CodexPage + 槽预制体。已有则不写。
    /// </summary>
    public static class DesktopHubCodexPageEnsure
    {
        private const string SlotPath = "Assets/Resources/Prefabs/LubyUI/CodexAppearanceSlot.prefab";

        /// <summary>校验图鉴页签、页面与槽预制体；不写预制体。</summary>
        public static bool ValidateTabsAndPage(out string error)
        {
            error = null;

            if (AssetDatabase.LoadAssetAtPath<CodexAppearanceSlot>(SlotPath) == null)
            {
                error = $"缺少图鉴槽预制体：{SlotPath}。请手建后再应用。";
                return false;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(DesktopHubApply.MainCanvasPrefabPath);
            try
            {
                Transform hub = root.transform.Find("DesktopHubPanel");
                if (hub == null)
                {
                    error = "MainCanvas.prefab 无 DesktopHubPanel。";
                    return false;
                }

                Transform topBar = hub.Find("TopBar");
                Transform pages = hub.Find("Pages");
                if (topBar == null || pages == null)
                {
                    error = "DesktopHubPanel 缺少 TopBar 或 Pages。请手改 MainCanvas.prefab。";
                    return false;
                }

                if (topBar.Find("TabCodex") == null)
                {
                    error = "TopBar 缺少 TabCodex。请手改 MainCanvas.prefab。";
                    return false;
                }

                Transform codexPage = pages.Find("CodexPage");
                if (codexPage == null)
                {
                    error = "Pages 缺少 CodexPage。请手改 MainCanvas.prefab。";
                    return false;
                }

                if (codexPage.Find("Body/GridScroll/Viewport/Content") == null)
                {
                    error = "CodexPage 缺少 Body/GridScroll/Viewport/Content。请手改 MainCanvas.prefab。";
                    return false;
                }

                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        public static CodexAppearanceSlot LoadSlotPrefab() =>
            AssetDatabase.LoadAssetAtPath<CodexAppearanceSlot>(SlotPath);
    }
}
#endif
