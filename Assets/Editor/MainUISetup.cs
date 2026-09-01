#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using TMPro;
using UniverIdle.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UniverIdle.Editor
{
    public static partial class MainUISetup
    {
        private const string RootName = "UniverIdle_MainUI";
        private const float RefWidth = 1200f;
        private const float RefHeight = 680f;

        [MenuItem("UniverIdle/创建主界面（当前场景）")]
        public static void CreateMainUI()
        {
            Build();
        }

        /// <summary>供 batchmode 调用，生成并保存场景。</summary>
        public static void CreateMainUIBatch()
        {
            Build();
            EditorSceneManager.SaveOpenScenes();
        }

        private static void Build()
        {
            EnsureEventSystem();
            RemoveExistingRoot();

            var font = GetChineseFontAsset();
            var root = BuildUI(font);

            Selection.activeGameObject = root;
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[UniverIdle] 主界面已创建。运行场景即可预览；分辨率建议 1920×1080。");
        }

        private static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindObjectOfType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
            Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
        }

        private static void RemoveExistingRoot()
        {
            var existing = GameObject.Find(RootName);
            if (existing != null)
                Undo.DestroyObjectImmediate(existing);
        }

        private static TMP_FontAsset GetChineseFontAsset()
        {
            const string projectFontPath = "Assets/UI/Fonts/NotoSansSC-Regular SDF.asset";

            var asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(projectFontPath);
            if (asset != null) return asset;

            asset = TryCreateChineseFontAsset(projectFontPath);
            if (asset != null) return asset;

            asset = FindFallbackTmpFont();
            if (asset != null)
            {
                Debug.LogWarning(
                    "[UniverIdle] 未能自动生成中文 SDF 字体，暂用 TMP 内置字体；中文可能显示为方框。" +
                    "可在 Window → TextMeshPro → Font Asset Creator 生成 NotoSansSC-Regular SDF 到 Assets/UI/Fonts/。");
                return asset;
            }

            throw new System.InvalidOperationException(
                "[UniverIdle] 未找到任何 TMP_FontAsset。请先导入 TextMeshPro Essential Resources（Window → TextMeshPro → Import TMP Essential Resources）。");
        }

        private static TMP_FontAsset TryCreateChineseFontAsset(string savePath)
        {
            var osFont = Font.CreateDynamicFontFromOSFont(
                new[] { "Microsoft YaHei UI", "Microsoft YaHei", "SimHei", "Noto Sans CJK SC", "Arial Unicode MS", "Arial" },
                32);
            if (osFont == null) return null;

            var asset = TMP_FontAsset.CreateFontAsset(osFont);
            if (asset == null) return null;

            Directory.CreateDirectory("Assets/UI/Fonts");
            AssetDatabase.CreateAsset(asset, savePath);
            AssetDatabase.SaveAssets();
            return asset;
        }

        private static TMP_FontAsset FindFallbackTmpFont()
        {
            if (TMP_Settings.defaultFontAsset != null)
                return TMP_Settings.defaultFontAsset;

            var paths = new[]
            {
                "Packages/com.unity.textmeshpro/Resources/Fonts & Materials/LiberationSans SDF.asset",
                "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset",
            };
            foreach (var path in paths)
            {
                var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                if (font != null) return font;
            }

            foreach (var guid in AssetDatabase.FindAssets("t:TMP_FontAsset"))
            {
                var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guid));
                if (font != null) return font;
            }

            return null;
        }

        private static GameObject BuildUI(TMP_FontAsset font)
        {
            var canvasGo = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(canvasGo, "Create Main UI");

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var safe = CreateRect("SafeArea", canvasGo.transform);
            Stretch(safe);
            var safeImg = safe.gameObject.AddComponent<Image>();
            safeImg.color = new Color(0.07f, 0.09f, 0.08f, 1f);

            var app = CreateRect("App", safe);
            Center(app, RefWidth, RefHeight);
            var appImg = app.gameObject.AddComponent<Image>();
            appImg.color = UITheme.Background;

            var vlg = app.gameObject.AddComponent<VerticalLayoutGroup>();
            ConfigureLayoutGroup(vlg, expandWidth: true, expandHeight: false);
            vlg.spacing = 0;
            vlg.padding = new RectOffset(0, 0, 0, 0);

            var controller = app.gameObject.AddComponent<MainUIController>();
            var skills = new List<SkillNavItemView>();
            var actions = new List<ActionCardView>();

            var topBar = CreatePanel(app, "TopBar", UITheme.TopBarBottom, 52);
            AddTopBar(topBar, font, out var goldText);

            var body = CreateRect("Body", app);
            var bodyLE = body.gameObject.AddComponent<LayoutElement>();
            bodyLE.flexibleHeight = 1;
            bodyLE.minHeight = 400;
            var bodyHLG = body.gameObject.AddComponent<HorizontalLayoutGroup>();
            ConfigureLayoutGroup(bodyHLG, expandWidth: true, expandHeight: true);
            bodyHLG.spacing = 0;

            var sidebar = CreatePanel(body, "Sidebar", UITheme.SidebarBg, -1, 172);
            var sidebarVLG = sidebar.gameObject.AddComponent<VerticalLayoutGroup>();
            ConfigureLayoutGroup(sidebarVLG, expandWidth: true, expandHeight: false);
            sidebarVLG.padding = new RectOffset(8, 8, 10, 10);
            sidebarVLG.spacing = 6;
            AddSkillNav(sidebar, font, skills);

            var center = CreateRect("Center", body);
            var centerLE = center.gameObject.AddComponent<LayoutElement>();
            centerLE.flexibleWidth = 1;
            var centerVLG = center.gameObject.AddComponent<VerticalLayoutGroup>();
            ConfigureLayoutGroup(centerVLG, expandWidth: true, expandHeight: false);
            centerVLG.padding = new RectOffset(14, 14, 14, 14);
            centerVLG.spacing = 12;

            var banner = CreateBanner(center, font, out var locationTitle);
            var cardsRow = CreateActionCards(center, font, actions);
            CreateRunningBar(center, font, out var progressFill, out var progressLabel, out var progressTime);

            var cardsLE = cardsRow.gameObject.AddComponent<LayoutElement>();
            cardsLE.flexibleHeight = 1;
            cardsLE.minHeight = 120;

            var detail = CreatePanel(body, "Detail", UITheme.SidebarBg, -1, 228);
            AddDetailPanel(detail, font, out var detailTitle, out var detailBody);

            var invBar = CreatePanel(app, "InventoryBar", UITheme.InventoryBg, 76);
            AddInventoryBar(invBar, font);

            controller.SetReferences(skills, locationTitle, actions, progressFill, progressLabel, progressTime, detailTitle, detailBody, goldText);

            for (var i = 0; i < skills.Count; i++)
            {
                var btn = skills[i].GetComponent<Button>();
                if (btn != null) controller.BindSkillButton(i, btn);
            }
            for (var i = 0; i < actions.Count; i++)
            {
                var btn = actions[i].GetComponent<Button>();
                if (btn != null) controller.BindActionCard(i, btn);
            }

            return canvasGo;
        }
    }
}
#endif
