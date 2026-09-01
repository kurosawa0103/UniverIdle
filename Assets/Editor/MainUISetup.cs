#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UniverIdle.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UniverIdle.Editor
{
    public static class MainUISetup
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
            if (Object.FindObjectOfType<EventSystem>() != null) return;
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
            var path = "Assets/UI/Fonts/NotoSansSC-Regular SDF.asset";
            var asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (asset != null) return asset;

            var osFont = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "SimHei", "Noto Sans CJK SC", "Arial" }, 32);
            asset = TMP_FontAsset.CreateFontAsset(osFont);
            System.IO.Directory.CreateDirectory("Assets/UI/Fonts");
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            return asset;
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
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 0;
            vlg.padding = new RectOffset(0, 0, 0, 0);

            var controller = app.gameObject.AddComponent<MainUIController>();
            var skills = new List<SkillNavItemView>();
            var actions = new List<ActionCardView>();

            // Top bar
            var topBar = CreatePanel(app, "TopBar", UITheme.TopBarBottom, 52);
            AddTopBar(topBar, font, out var goldText);

            // Body
            var body = CreateRect("Body", app);
            var bodyLE = body.gameObject.AddComponent<LayoutElement>();
            bodyLE.flexibleHeight = 1;
            bodyLE.minHeight = 400;
            var bodyHLG = body.gameObject.AddComponent<HorizontalLayoutGroup>();
            bodyHLG.childControlWidth = true;
            bodyHLG.childControlHeight = true;
            bodyHLG.childForceExpandHeight = true;
            bodyHLG.spacing = 0;

            // Sidebar
            var sidebar = CreatePanel(body, "Sidebar", UITheme.SidebarBg, -1, 172);
            var sidebarVLG = sidebar.gameObject.AddComponent<VerticalLayoutGroup>();
            sidebarVLG.padding = new RectOffset(8, 8, 10, 10);
            sidebarVLG.spacing = 6;
            sidebarVLG.childControlHeight = true;
            sidebarVLG.childForceExpandHeight = false;
            AddSkillNav(sidebar, font, skills);

            // Center
            var center = CreateRect("Center", body);
            var centerLE = center.gameObject.AddComponent<LayoutElement>();
            centerLE.flexibleWidth = 1;
            var centerVLG = center.gameObject.AddComponent<VerticalLayoutGroup>();
            centerVLG.padding = new RectOffset(14, 14, 14, 14);
            centerVLG.spacing = 12;
            centerVLG.childForceExpandWidth = true;
            centerVLG.childForceExpandHeight = false;

            var banner = CreateBanner(center, font, out var locationTitle);
            var cardsRow = CreateActionCards(center, font, actions);
            var running = CreateRunningBar(center, font, out var progressFill, out var progressLabel, out var progressTime);

            var cardsLE = cardsRow.gameObject.AddComponent<LayoutElement>();
            cardsLE.flexibleHeight = 1;
            cardsLE.minHeight = 120;

            // Detail
            var detail = CreatePanel(body, "Detail", UITheme.SidebarBg, -1, 228);
            AddDetailPanel(detail, font, out var detailTitle, out var detailBody);

            // Inventory
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

        private static void AddTopBar(RectTransform top, TMP_FontAsset font, out TextMeshProUGUI goldText)
        {
            var hlg = top.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(20, 20, 0, 0);
            hlg.spacing = 16;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childForceExpandWidth = false;

            var logoIcon = CreateImage("LogoIcon", top, UITheme.Hex("#3A5A4A"), new Vector2(36, 36));
            AddLayout(logoIcon.gameObject, 36, 36);
            CreateTMP("✦", logoIcon.rectTransform, font, 18, UITheme.Gold, TextAlignmentOptions.Center);

            var title = CreateTMP("坠星谷", top, font, 17, UITheme.Cream, TextAlignmentOptions.Left);
            title.fontStyle = FontStyles.Bold;
            AddLayout(title.gameObject, 80, 36);

            var sub = CreateTMP("萤溪村", top, font, 12, UITheme.Muted, TextAlignmentOptions.Left);
            AddLayout(sub.gameObject, 60, 36);

            var spacer = CreateRect("Spacer", top);
            var spLE = spacer.gameObject.AddComponent<LayoutElement>();
            spLE.flexibleWidth = 1;

            goldText = CreateTMP("🪙 1,240", top, font, 14, UITheme.Gold, TextAlignmentOptions.Right);
            AddLayout(goldText.gameObject, 90, 36);

            var rep = CreateTMP("声望 ★★☆", top, font, 14, UITheme.Muted, TextAlignmentOptions.Right);
            AddLayout(rep.gameObject, 100, 36);

            CreateTopButton(top, font, "图鉴");
            CreateTopButton(top, font, "背包");
            CreateTopButton(top, font, "设置");
        }

        private static void CreateTopButton(RectTransform parent, TMP_FontAsset font, string label)
        {
            var rt = CreateRect($"Btn_{label}", parent);
            AddLayout(rt.gameObject, 56, 32);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = UITheme.PanelLight;
            var btn = rt.gameObject.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = UITheme.Hex("#3D4A42");
            colors.pressedColor = UITheme.Hex("#4A5A50");
            btn.colors = colors;
            CreateTMP(label, rt, font, 13, UITheme.Cream, TextAlignmentOptions.Center);
        }

        private static void AddSkillNav(RectTransform sidebar, TMP_FontAsset font, List<SkillNavItemView> list)
        {
            var data = new (string name, string loc, int lv, float xp, Color icon, bool selected)[]
            {
                ("打猎", "谷仓", 8, 0.4f, UITheme.Hex("#3A2820"), false),
                ("伐木", "村外", 5, 0.32f, UITheme.Hex("#2A3820"), false),
                ("溪钓", "萤溪", 12, 0.65f, UITheme.Hex("#2A4858"), true),
                ("野拾", "林缘", 6, 0.28f, UITheme.Hex("#2A4838"), false),
                ("掘矿", "矮洞", 10, 0.52f, UITheme.Hex("#383028"), false),
                ("炼药", "工坊", 9, 0.48f, UITheme.Hex("#3A2830"), false),
                ("锻造", "铁砧", 7, 0.35f, UITheme.Hex("#303028"), false),
                ("讨伐", "林缘", 11, 0.58f, UITheme.Hex("#382828"), false),
            };

            foreach (var d in data)
            {
                var item = CreateSkillItem(sidebar, font, d.name, d.loc, d.lv, d.xp, d.icon);
                list.Add(item);
            }
        }

        private static SkillNavItemView CreateSkillItem(RectTransform parent, TMP_FontAsset font,
            string skillName, string location, int level, float xp, Color iconColor)
        {
            var rt = CreateRect($"Skill_{skillName}", parent);
            AddLayout(rt.gameObject, 0, 52);
            var le = rt.gameObject.GetComponent<LayoutElement>();
            le.flexibleWidth = 1;

            var bg = rt.gameObject.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0);
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = bg;

            var hlg = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(10, 10, 8, 8);
            hlg.spacing = 10;
            hlg.childAlignment = TextAnchor.MiddleLeft;

            var accent = CreateImage("Accent", rt, UITheme.Teal, new Vector2(3, 36));
            AddLayout(accent.gameObject, 3, 36);
            accent.type = Image.Type.Simple;

            var icon = CreateImage("Icon", rt, iconColor, new Vector2(40, 40));
            AddLayout(icon.gameObject, 40, 40);

            var info = CreateRect("Info", rt);
            var infoLE = info.gameObject.AddComponent<LayoutElement>();
            infoLE.flexibleWidth = 1;
            var infoVLG = info.gameObject.AddComponent<VerticalLayoutGroup>();
            infoVLG.spacing = 2;
            infoVLG.childAlignment = TextAnchor.UpperLeft;

            var nameT = CreateTMP(skillName, info, font, 14, UITheme.Text, TextAlignmentOptions.Left);
            var lvT = CreateTMP($"Lv. {level}", info, font, 11, UITheme.Muted, TextAlignmentOptions.Left);

            var barBg = CreateImage("XpBg", info, UITheme.BarTrack, new Vector2(80, 3));
            AddLayout(barBg.gameObject, 0, 3);
            var barFill = CreateImage("XpFill", barBg, UITheme.Teal, new Vector2(80, 3));
            barFill.type = Image.Type.Filled;
            barFill.fillMethod = Image.FillMethod.Horizontal;
            barFill.fillAmount = xp;
            Stretch(barFill);

            var view = rt.gameObject.AddComponent<SkillNavItemView>();
            view.Bind(bg, accent, icon, nameT, lvT, barFill);
            view.Setup(skillName, location, level, xp, iconColor);
            return view;
        }

        private static RectTransform CreateBanner(RectTransform parent, TMP_FontAsset font, out TextMeshProUGUI title)
        {
            var banner = CreateRect("LocationBanner", parent);
            AddLayout(banner.gameObject, 0, 130);

            var bg = banner.gameObject.AddComponent<Image>();
            bg.color = UITheme.Hex("#1A3040");

            var artPath = "Assets/UI/Art/主界面-概念图.png";
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(artPath);
            if (tex != null)
            {
                var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                bg.sprite = sprite;
                bg.type = Image.Type.Simple;
                bg.color = Color.white;
                bg.preserveAspect = false;
            }

            var overlay = CreateImage("Overlay", banner, UITheme.BannerOverlay, Vector2.zero);
            Stretch(overlay);

            var textArea = CreateRect("BannerText", banner);
            Stretch(textArea);
            var pad = textArea.gameObject.AddComponent<VerticalLayoutGroup>();
            pad.padding = new RectOffset(20, 20, 16, 16);
            pad.childAlignment = TextAnchor.LowerLeft;

            title = CreateTMP("萤溪", textArea, font, 22, Color.white, TextAlignmentOptions.Left);
            title.fontStyle = FontStyles.Bold;

            var tags = CreateRect("Tags", textArea);
            var tagHLG = tags.gameObject.AddComponent<HorizontalLayoutGroup>();
            tagHLG.spacing = 8;
            CreateTag(tags, font, "微光");
            CreateTag(tags, font, "安全");
            CreateTag(tags, font, "★☆☆");

            return banner;
        }

        private static void CreateTag(RectTransform parent, TMP_FontAsset font, string text)
        {
            var rt = CreateRect($"Tag_{text}", parent);
            AddLayout(rt.gameObject, 48, 22);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = new Color(UITheme.Teal.r, UITheme.Teal.g, UITheme.Teal.b, 0.35f);
            CreateTMP(text, rt, font, 11, UITheme.Hex("#B8E0D4"), TextAlignmentOptions.Center);
        }

        private static RectTransform CreateActionCards(RectTransform parent, TMP_FontAsset font, List<ActionCardView> list)
        {
            var row = CreateRect("ActionCards", parent);
            var grid = row.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(200, 100);
            grid.spacing = new Vector2(10, 10);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.childAlignment = TextAnchor.UpperLeft;

            var cards = new[]
            {
                ("钓萤虾", "8.0 秒", "+1 萤虾", "萤溪浅水的小虾，夜间会发光。炼药常用基材。", false, UITheme.Hex("#2A5850")),
                ("淘星沙", "12 秒", "+1 星沙", "溪底沉积的星尘碎屑，附魔与炼金都需要。", false, UITheme.Hex("#4A4030")),
                ("钓鳟鱼", "需 Lv.10", "🔒", "更深处才有鳟鱼，需要更高的溪钓等级。", true, UITheme.Hex("#3A5858")),
            };

            foreach (var c in cards)
                list.Add(CreateActionCard(row, font, c.Item1, c.Item2, c.Item3, c.Item4, c.Item5, c.Item6));

            return row;
        }

        private static ActionCardView CreateActionCard(RectTransform parent, TMP_FontAsset font,
            string title, string metaL, string metaR, string desc, bool locked, Color thumbColor)
        {
            var rt = CreateRect($"Card_{title}", parent);
            var bg = rt.gameObject.AddComponent<Image>();
            bg.color = UITheme.Panel;
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = bg;
            var cg = rt.gameObject.AddComponent<CanvasGroup>();

            var vlg = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(10, 10, 10, 10);
            vlg.spacing = 6;

            var thumb = CreateImage("Thumb", rt, thumbColor, new Vector2(0, 56));
            AddLayout(thumb.gameObject, 0, 56);

            var titleT = CreateTMP(title, rt, font, 14, UITheme.Text, TextAlignmentOptions.Left);
            var metaRow = CreateRect("Meta", rt);
            var metaHLG = metaRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            metaHLG.childForceExpandWidth = true;
            var metaL_T = CreateTMP(metaL, metaRow, font, 12, UITheme.Muted, TextAlignmentOptions.Left);
            var metaLE = metaL_T.gameObject.AddComponent<LayoutElement>();
            metaLE.flexibleWidth = 1;
            var metaR_T = CreateTMP(metaR, metaRow, font, 11, UITheme.Teal, TextAlignmentOptions.Right);

            var view = rt.gameObject.AddComponent<ActionCardView>();
            view.Bind(bg, thumb, titleT, metaL_T, metaR_T, cg);
            view.Setup(title, metaL, metaR, desc, locked, thumbColor);
            return view;
        }

        private static RectTransform CreateRunningBar(RectTransform parent, TMP_FontAsset font,
            out Image fill, out TextMeshProUGUI label, out TextMeshProUGUI time)
        {
            var rt = CreateRect("RunningBar", parent);
            AddLayout(rt.gameObject, 0, 72);
            var bg = rt.gameObject.AddComponent<Image>();
            bg.color = UITheme.Panel;

            var hlg = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(14, 14, 14, 14);
            hlg.spacing = 14;
            hlg.childAlignment = TextAnchor.MiddleLeft;

            var thumb = CreateImage("Thumb", rt, UITheme.Hex("#2A3830"), new Vector2(56, 56));
            AddLayout(thumb.gameObject, 56, 56);
            var thumbInner = CreateImage("Inner", thumb, UITheme.Accent, new Vector2(24, 24));
            Center(thumbInner, 24, 24);

            var mid = CreateRect("Mid", rt);
            var midLE = mid.gameObject.AddComponent<LayoutElement>();
            midLE.flexibleWidth = 1;
            var midVLG = mid.gameObject.AddComponent<VerticalLayoutGroup>();
            midVLG.spacing = 8;

            label = CreateTMP("进行中 · 钓萤虾", mid, font, 15, UITheme.Text, TextAlignmentOptions.Left);

            var barBg = CreateImage("BarBg", mid, UITheme.BarTrack, new Vector2(0, 10));
            AddLayout(barBg.gameObject, 0, 10);
            fill = CreateImage("BarFill", barBg, UITheme.Accent, new Vector2(0, 10));
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillAmount = 0.62f;
            Stretch(fill);

            time = CreateTMP("00:06", rt, font, 13, UITheme.Gold, TextAlignmentOptions.Right);
            AddLayout(time.gameObject, 48, 36);

            return rt;
        }

        private static void AddDetailPanel(RectTransform detail, TMP_FontAsset font,
            out TextMeshProUGUI title, out TextMeshProUGUI body)
        {
            var vlg = detail.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(14, 14, 14, 14);
            vlg.spacing = 12;

            var hero = CreateImage("Hero", detail, UITheme.Panel, new Vector2(200, 200));
            AddLayout(hero.gameObject, 0, 200);
            var heroInner = CreateImage("Shrimp", hero, UITheme.Accent, new Vector2(80, 80));
            Center(heroInner, 80, 80);

            title = CreateTMP("萤虾", detail, font, 16, UITheme.Text, TextAlignmentOptions.Left);
            body = CreateTMP("萤溪浅水的小虾，夜间会发光。炼药常用基材，也可直接出售。", detail, font, 13, UITheme.Muted, TextAlignmentOptions.TopLeft);
            body.enableWordWrapping = true;
            var bodyLE = body.gameObject.AddComponent<LayoutElement>();
            bodyLE.flexibleHeight = 1;

            CreateTMP("✓ 溪钓 Lv.1", detail, font, 12, UITheme.Teal, TextAlignmentOptions.Left);
            CreateTMP("✓ 地点：萤溪", detail, font, 12, UITheme.Teal, TextAlignmentOptions.Left);
            CreateTMP("稀有：星沙 2%", detail, font, 12, UITheme.Muted, TextAlignmentOptions.Left);
        }

        private static void AddInventoryBar(RectTransform inv, TMP_FontAsset font)
        {
            var hlg = inv.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(16, 16, 10, 10);
            hlg.spacing = 10;
            hlg.childAlignment = TextAnchor.MiddleLeft;

            var label = CreateTMP("物品", inv, font, 12, UITheme.Muted, TextAlignmentOptions.Center);
            AddLayout(label.gameObject, 24, 52);

            CreateInvSlot(inv, font, UITheme.Accent, "48");
            CreateInvSlot(inv, font, UITheme.Teal, "22");
            CreateInvSlot(inv, font, UITheme.Hex("#C84848"), "3");
            CreateInvSlot(inv, font, UITheme.Hex("#A88858"), "6");
            CreateInvSlot(inv, font, UITheme.Gold, "31");
        }

        private static void CreateInvSlot(RectTransform parent, TMP_FontAsset font, Color color, string count)
        {
            var rt = CreateRect("Slot", parent);
            AddLayout(rt.gameObject, 52, 52);
            var bg = rt.gameObject.AddComponent<Image>();
            bg.color = UITheme.Panel;
            var icon = CreateImage("Icon", rt, color, new Vector2(28, 28));
            Center(icon, 28, 28);
            var cnt = CreateTMP(count, rt, font, 11, UITheme.Cream, TextAlignmentOptions.BottomRight);
            var cntRt = cnt.rectTransform;
            cntRt.anchorMin = new Vector2(1, 0);
            cntRt.anchorMax = new Vector2(1, 0);
            cntRt.pivot = new Vector2(1, 0);
            cntRt.anchoredPosition = new Vector2(-4, 2);
            cntRt.sizeDelta = new Vector2(30, 16);
        }

        #region UI Helpers

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private static RectTransform CreatePanel(Transform parent, string name, Color color, float height, float width = -1)
        {
            var rt = CreateRect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            var le = rt.gameObject.AddComponent<LayoutElement>();
            if (height > 0) le.preferredHeight = height;
            if (width > 0)
            {
                le.preferredWidth = width;
                le.flexibleWidth = 0;
            }
            return rt;
        }

        private static Image CreateImage(string name, RectTransform parent, Color color, Vector2 size)
        {
            var rt = CreateRect(name, parent);
            if (size.x > 0) rt.sizeDelta = size;
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return img;
        }

        private static TextMeshProUGUI CreateTMP(string text, RectTransform parent, TMP_FontAsset font,
            float size, Color color, TextAlignmentOptions align)
        {
            var rt = CreateRect("Text", parent);
            Stretch(rt);
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.font = font;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void Center(RectTransform rt, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = Vector2.zero;
        }

        private static void AddLayout(GameObject go, float w, float h)
        {
            var le = go.GetComponent<LayoutElement>();
            if (le == null) le = go.AddComponent<LayoutElement>();
            if (w > 0) le.preferredWidth = w;
            if (h > 0) le.preferredHeight = h;
        }

        #endregion
    }
}
#endif
