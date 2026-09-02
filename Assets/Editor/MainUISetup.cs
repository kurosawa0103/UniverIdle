#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UniverIdle.Game;
using UniverIdle.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace UniverIdle.Editor
{
    public static partial class MainUISetup
    {
        private const string RootName = "UniverIdle_MainUI";

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
            SetActiveLayout(ResolveLayoutForBuild());
            RemoveExistingRoot();

            var font = GetChineseFontAsset();
            var root = BuildUI(font);

            Selection.activeGameObject = root;
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[UniverIdle] 主界面已重建（布局来自 MainUILayoutParams.asset，未从场景覆盖）。");
        }

        private static void RemoveExistingRoot()
        {
            var existing = GameObject.Find(RootName);
            if (existing != null)
                Undo.DestroyObjectImmediate(existing);
        }

        private static TMP_FontAsset GetChineseFontAsset()
        {
            const string projectFontPath = "Assets/Res/fonts/unifont-15.asset";

            var asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(projectFontPath);
            if (asset != null) return asset;

            asset = FindFallbackTmpFont();
            if (asset != null)
            {
                Debug.LogWarning(
                    $"[UniverIdle] 未找到 {projectFontPath}，暂用备用 TMP 字体；请确认 unifont-15 资源存在。");
                return asset;
            }

            throw new System.InvalidOperationException(
                "[UniverIdle] 未找到任何 TMP_FontAsset。请先导入 TextMeshPro Essential Resources（Window → TextMeshPro → Import TMP Essential Resources）。");
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
            canvasGo.AddComponent<MainUIInputBootstrap>();
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = L.referenceResolution;
            scaler.matchWidthOrHeight = L.matchWidthOrHeight;
            canvasGo.AddComponent<GraphicRaycaster>();

            var app = CreateRect("App", canvasGo.transform);
            Stretch(app);
            var appImg = app.gameObject.AddComponent<Image>();
            appImg.color = UITheme.Background;
            appImg.raycastTarget = false;

            var vlg = app.gameObject.AddComponent<VerticalLayoutGroup>();
            ConfigureLayoutGroup(vlg, expandWidth: true, expandHeight: false);
            vlg.childForceExpandHeight = false;
            vlg.spacing = 0;
            vlg.padding = new RectOffset(0, 0, 0, 0);

            var controller = app.gameObject.AddComponent<MainUIController>();
            app.gameObject.AddComponent<UniverIdle.Game.GameSession>();
            var skills = new List<SkillNavItemView>();

            var topBar = CreatePanel(app, "TopBar", UITheme.TopBarBottom, ConceptLayout.TopBarHeight);
            LockLayoutHeight(topBar, ConceptLayout.TopBarHeight);
            AttachTopGradient(topBar);
            AddTopBar(topBar, font, out var inventoryButton);

            var body = CreateRect("Body", app);
            var bodyLE = body.gameObject.AddComponent<LayoutElement>();
            bodyLE.flexibleHeight = 1;
            var bodyHLG = body.gameObject.AddComponent<HorizontalLayoutGroup>();
            ConfigureLayoutGroup(bodyHLG, expandWidth: ConceptLayout.BodyChildForceExpandWidth, expandHeight: true);
            bodyHLG.childForceExpandWidth = ConceptLayout.BodyChildForceExpandWidth;
            bodyHLG.spacing = 0;

            var sidebar = CreatePanel(body, "Sidebar", UITheme.SidebarBg, -1, ConceptLayout.SidebarWidth);
            AddSkillNav(sidebar, font, skills);
            CreateDivider(body, vertical: true);

            var center = CreateRect("Center", body);
            var centerLE = center.gameObject.AddComponent<LayoutElement>();
            if (ConceptLayout.CenterPreferredWidth > 0f)
                centerLE.preferredWidth = ConceptLayout.CenterPreferredWidth;
            centerLE.flexibleWidth = ConceptLayout.CenterFlexibleWidth;
            var centerHost = center.gameObject.AddComponent<WorkCenterHost>();

            CreateStandardWorkCenter(center, font, GameContent.WorkScavengeId, centerHost);
            CreateStandardWorkCenter(center, font, GameContent.WorkWoodcuttingId, centerHost);
            CreateStandardWorkCenter(center, font, GameContent.WorkMiningId, centerHost);
            CreateStandardWorkCenter(center, font, GameContent.WorkMonsterExploreId, centerHost);

            if (ConceptLayout.UseBodyFlexSpacer)
            {
                var bodySpacer = CreateRect("BodyFlexSpacer", body);
                bodySpacer.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            }

            CreateDivider(body, vertical: true);
            var detail = CreatePanel(body, "Detail", UITheme.SidebarBg, -1,
                ConceptLayout.DetailPreferredWidth > 0f ? ConceptLayout.DetailPreferredWidth : -1f);
            var detailLE = detail.GetComponent<LayoutElement>();
            if (detailLE != null)
            {
                detailLE.flexibleWidth = ConceptLayout.DetailFlexibleWidth;
                if (ConceptLayout.DetailMinWidth > 0f)
                    detailLE.minWidth = ConceptLayout.DetailMinWidth;
            }
            AddDetailPanel(detail, font, out var detailTitle, out var detailBody);

            var inventoryPanel = CreateInventoryPanel(canvasGo.transform, font);

            controller.SetReferences(skills, centerHost, detailTitle, detailBody, inventoryPanel, inventoryButton);
            ApplyInitialWorkSelection(skills, centerHost);

            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(centerHost);
            EditorUtility.SetDirty(inventoryPanel);
            EditorUtility.SetDirty(canvasGo);

            return canvasGo;
        }

        private static void ApplyInitialWorkSelection(List<SkillNavItemView> skills, WorkCenterHost centerHost)
        {
            var workId = FindFirstAvailableWorkId(skills);
            if (string.IsNullOrEmpty(workId)) return;

            foreach (var skill in skills)
            {
                if (skill == null) continue;
                skill.SetSelected(skill.WorkId == workId);
            }

            centerHost.SetActiveWorkPreview(workId);
        }

        private static string FindFirstAvailableWorkId(List<SkillNavItemView> skills)
        {
            foreach (var skill in skills)
            {
                if (skill != null && skill.IsAvailable && !string.IsNullOrEmpty(skill.WorkId))
                    return skill.WorkId;
            }

            return GameContent.WorkScavengeId;
        }
    }
}
#endif
