#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UniverIdle.UI;
using UnityEngine;
using UnityEngine.UI;

namespace UniverIdle.Editor
{
    public static partial class MainUISetup
    {
        private static void AddTopBar(RectTransform top, TMP_FontAsset font, out TextMeshProUGUI goldText)
        {
            var hlg = top.gameObject.AddComponent<HorizontalLayoutGroup>();
            ConfigureLayoutGroup(hlg, expandWidth: false, expandHeight: true);
            hlg.padding = new RectOffset(20, 20, 0, 0);
            hlg.spacing = 16;
            hlg.childAlignment = TextAnchor.MiddleLeft;

            var logoIcon = CreateColorBlock("LogoIcon", top, UITheme.Hex("#3A5A4A"), new Vector2(36, 36));
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
                list.Add(CreateSkillItem(sidebar, font, d.name, d.loc, d.lv, d.xp, d.icon));
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
            ConfigureLayoutGroup(hlg, expandWidth: true, expandHeight: true);
            hlg.padding = new RectOffset(10, 10, 8, 8);
            hlg.spacing = 10;
            hlg.childAlignment = TextAnchor.MiddleLeft;

            var accent = CreateColorBlock("Accent", rt, UITheme.Teal, new Vector2(3, 36));
            AddLayout(accent.gameObject, 3, 36);

            var icon = CreateColorBlock("Icon", rt, iconColor, new Vector2(40, 40));
            AddLayout(icon.gameObject, 40, 40);

            var info = CreateRect("Info", rt);
            var infoLE = info.gameObject.AddComponent<LayoutElement>();
            infoLE.flexibleWidth = 1;
            var infoVLG = info.gameObject.AddComponent<VerticalLayoutGroup>();
            ConfigureLayoutGroup(infoVLG, expandWidth: true, expandHeight: false);
            infoVLG.spacing = 2;
            infoVLG.childAlignment = TextAnchor.UpperLeft;

            var nameT = CreateLayoutTMP(skillName, info, font, 14, UITheme.Text, TextAlignmentOptions.Left);
            var lvT = CreateLayoutTMP($"Lv. {level}", info, font, 11, UITheme.Muted, TextAlignmentOptions.Left);
            var barFill = CreateFilledBar(info, 3f, UITheme.BarTrack, UITheme.Teal, xp, "XpBg", "XpFill");

            var view = rt.gameObject.AddComponent<SkillNavItemView>();
            view.Setup(bg, accent, icon, nameT, lvT, barFill, skillName, location, level, xp, iconColor);
            return view;
        }

        private static RectTransform CreateBanner(RectTransform parent, TMP_FontAsset font, out TextMeshProUGUI title)
        {
            var banner = CreateRect("LocationBanner", parent);
            AddLayout(banner.gameObject, 0, 130);

            var bg = banner.gameObject.AddComponent<Image>();
            bg.color = UITheme.BannerBg;

            var accentBand = CreateColorBlock("AccentBand", banner, UITheme.BannerAccent, Vector2.zero);
            var bandRt = accentBand.rectTransform;
            bandRt.anchorMin = new Vector2(0, 0);
            bandRt.anchorMax = new Vector2(1, 0);
            bandRt.pivot = new Vector2(0.5f, 0);
            bandRt.sizeDelta = new Vector2(0, 48);
            bandRt.anchoredPosition = Vector2.zero;

            var textArea = CreateRect("BannerText", banner);
            Stretch(textArea);
            var pad = textArea.gameObject.AddComponent<VerticalLayoutGroup>();
            ConfigureLayoutGroup(pad, expandWidth: true, expandHeight: false);
            pad.padding = new RectOffset(20, 20, 16, 16);
            pad.childAlignment = TextAnchor.LowerLeft;

            title = CreateLayoutTMP("萤溪", textArea, font, 22, Color.white, TextAlignmentOptions.Left, 30);
            title.fontStyle = FontStyles.Bold;

            var tags = CreateRect("Tags", textArea);
            AddLayout(tags.gameObject, 0, 22);
            var tagHLG = tags.gameObject.AddComponent<HorizontalLayoutGroup>();
            ConfigureLayoutGroup(tagHLG, expandWidth: false, expandHeight: true);
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
            var hlg = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            ConfigureLayoutGroup(hlg, expandWidth: true, expandHeight: true);
            hlg.spacing = 10;
            hlg.childAlignment = TextAnchor.UpperLeft;

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

            var cardLE = rt.gameObject.AddComponent<LayoutElement>();
            cardLE.flexibleWidth = 1;
            cardLE.minHeight = 118;

            var vlg = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            ConfigureLayoutGroup(vlg, expandWidth: true, expandHeight: false);
            vlg.padding = new RectOffset(10, 10, 10, 10);
            vlg.spacing = 6;

            var thumb = CreateColorBlock("Thumb", rt, thumbColor, new Vector2(0, 56));
            AddLayout(thumb.gameObject, 0, 56);

            var titleT = CreateLayoutTMP(title, rt, font, 14, UITheme.Text, TextAlignmentOptions.Left);
            var metaRow = CreateRect("Meta", rt);
            AddLayout(metaRow.gameObject, 0, 18);
            var metaHLG = metaRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            ConfigureLayoutGroup(metaHLG, expandWidth: true, expandHeight: true);
            var metaL_T = CreateLayoutTMP(metaL, metaRow, font, 12, UITheme.Muted, TextAlignmentOptions.Left);
            var metaLE = metaL_T.gameObject.GetComponent<LayoutElement>();
            metaLE.flexibleWidth = 1;
            var metaR_T = CreateLayoutTMP(metaR, metaRow, font, 11, UITheme.Teal, TextAlignmentOptions.Right);

            var view = rt.gameObject.AddComponent<ActionCardView>();
            view.Setup(bg, thumb, titleT, metaL_T, metaR_T, cg, title, metaL, metaR, desc, locked, thumbColor);
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
            ConfigureLayoutGroup(hlg, expandWidth: true, expandHeight: true);
            hlg.padding = new RectOffset(14, 14, 14, 14);
            hlg.spacing = 14;
            hlg.childAlignment = TextAnchor.MiddleLeft;

            var thumb = CreateColorBlock("Thumb", rt, UITheme.Hex("#2A3830"), new Vector2(56, 56));
            AddLayout(thumb.gameObject, 56, 56);
            var thumbInner = CreateColorBlock("Inner", thumb.rectTransform, UITheme.Accent, new Vector2(24, 24));
            Center(thumbInner.rectTransform, 24, 24);

            var mid = CreateRect("Mid", rt);
            var midLE = mid.gameObject.AddComponent<LayoutElement>();
            midLE.flexibleWidth = 1;
            var midVLG = mid.gameObject.AddComponent<VerticalLayoutGroup>();
            ConfigureLayoutGroup(midVLG, expandWidth: true, expandHeight: false);
            midVLG.spacing = 8;

            label = CreateLayoutTMP("进行中 · 钓萤虾", mid, font, 15, UITheme.Text, TextAlignmentOptions.Left);
            fill = CreateFilledBar(mid, 10f, UITheme.BarTrack, UITheme.Accent, 0.62f);

            time = CreateTMP("00:06", rt, font, 13, UITheme.Gold, TextAlignmentOptions.Right);
            AddLayout(time.gameObject, 48, 36);

            return rt;
        }

        private static void AddDetailPanel(RectTransform detail, TMP_FontAsset font,
            out TextMeshProUGUI title, out TextMeshProUGUI body)
        {
            var vlg = detail.gameObject.AddComponent<VerticalLayoutGroup>();
            ConfigureLayoutGroup(vlg, expandWidth: true, expandHeight: false);
            vlg.padding = new RectOffset(14, 14, 14, 14);
            vlg.spacing = 12;

            var hero = CreateColorBlock("Hero", detail, UITheme.Panel, new Vector2(200, 200));
            AddLayout(hero.gameObject, 0, 200);
            var heroInner = CreateColorBlock("Shrimp", hero.rectTransform, UITheme.Accent, new Vector2(80, 80));
            Center(heroInner.rectTransform, 80, 80);

            title = CreateLayoutTMP("萤虾", detail, font, 16, UITheme.Text, TextAlignmentOptions.Left);
            body = CreateLayoutTMP("萤溪浅水的小虾，夜间会发光。炼药常用基材，也可直接出售。", detail, font, 13, UITheme.Muted, TextAlignmentOptions.TopLeft, 72);
            body.enableWordWrapping = true;
            var bodyLE = body.gameObject.GetComponent<LayoutElement>();
            bodyLE.flexibleHeight = 1;

            CreateLayoutTMP("✓ 溪钓 Lv.1", detail, font, 12, UITheme.Teal, TextAlignmentOptions.Left);
            CreateLayoutTMP("✓ 地点：萤溪", detail, font, 12, UITheme.Teal, TextAlignmentOptions.Left);
            CreateLayoutTMP("稀有：星沙 2%", detail, font, 12, UITheme.Muted, TextAlignmentOptions.Left);
        }

        private static void AddInventoryBar(RectTransform inv, TMP_FontAsset font)
        {
            var hlg = inv.gameObject.AddComponent<HorizontalLayoutGroup>();
            ConfigureLayoutGroup(hlg, expandWidth: false, expandHeight: true);
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
            var icon = CreateColorBlock("Icon", rt, color, new Vector2(28, 28));
            Center(icon.rectTransform, 28, 28);
            var cnt = CreateTMP(count, rt, font, 11, UITheme.Cream, TextAlignmentOptions.BottomRight);
            var cntRt = cnt.rectTransform;
            cntRt.anchorMin = new Vector2(1, 0);
            cntRt.anchorMax = new Vector2(1, 0);
            cntRt.pivot = new Vector2(1, 0);
            cntRt.anchoredPosition = new Vector2(-4, 2);
            cntRt.sizeDelta = new Vector2(30, 16);
        }
    }
}
#endif
