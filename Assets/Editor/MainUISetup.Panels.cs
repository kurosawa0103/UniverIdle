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
            hlg.padding = new RectOffset(20, 20, 8, 8);
            hlg.spacing = 14;
            hlg.childAlignment = TextAnchor.MiddleLeft;

            var logoIcon = CreateColorBlock("LogoIcon", top, UITheme.Hex("#3A5A4A"), new Vector2(40, 40));
            AddLayout(logoIcon.gameObject, 40, 40);
            StyleOutline(logoIcon, UITheme.Border, new Vector2(1, -1));
            CreateTMP("✦", logoIcon.rectTransform, font, 18, UITheme.Gold, TextAlignmentOptions.Center);

            var titleBlock = CreateRect("TitleBlock", top);
            AddLayout(titleBlock.gameObject, 120, 40);
            var titleVLG = titleBlock.gameObject.AddComponent<VerticalLayoutGroup>();
            ConfigureLayoutGroup(titleVLG, expandWidth: true, expandHeight: false);
            titleVLG.spacing = 0;
            titleVLG.childAlignment = TextAnchor.MiddleLeft;

            var title = CreateLayoutTMP("坠星谷", titleBlock, font, 17, UITheme.Cream, TextAlignmentOptions.Left, 22);
            title.fontStyle = FontStyles.Bold;
            CreateLayoutTMP("萤溪村", titleBlock, font, 12, UITheme.Muted, TextAlignmentOptions.Left, 16);

            var spacer = CreateRect("Spacer", top);
            var spLE = spacer.gameObject.AddComponent<LayoutElement>();
            spLE.flexibleWidth = 1;

            goldText = CreateLayoutTMP("🪙 1,240", top, font, 14, UITheme.Gold, TextAlignmentOptions.Right, 36);
            AddLayout(goldText.gameObject, 96, 36);

            CreateLayoutTMP("声望 ★★☆", top, font, 14, UITheme.Muted, TextAlignmentOptions.Right, 36)
                .gameObject.AddComponent<LayoutElement>().preferredWidth = 108;

            CreateTopButton(top, font, "图鉴");
            CreateTopButton(top, font, "背包");
            CreateTopButton(top, font, "设置");
        }

        private static void CreateTopButton(RectTransform parent, TMP_FontAsset font, string label)
        {
            var rt = CreateRect($"Btn_{label}", parent);
            AddLayout(rt.gameObject, 60, 34);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = UITheme.PanelLight;
            StyleOutline(img, UITheme.Border, new Vector2(1, -1));
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            ConfigureButton(btn, UITheme.PanelLight, UITheme.CardHover, UITheme.Hex("#4A5A50"));
            CreateTMP(label, rt, font, 13, UITheme.Cream, TextAlignmentOptions.Center);
        }

        private static void AddSkillNav(RectTransform sidebar, TMP_FontAsset font, List<SkillNavItemView> list)
        {
            var vlg = sidebar.gameObject.AddComponent<VerticalLayoutGroup>();
            ConfigureLayoutGroup(vlg, expandWidth: true, expandHeight: false);
            vlg.padding = new RectOffset(10, 10, 12, 12);
            vlg.spacing = 6;

            CreateSectionLabel(sidebar, font, "工作");

            var data = new (string name, string loc, int lv, float xp, Color icon)[]
            {
                ("打猎", "谷仓", 8, 0.4f, UITheme.Hex("#3A2820")),
                ("伐木", "村外", 5, 0.32f, UITheme.Hex("#2A3820")),
                ("溪钓", "萤溪", 12, 0.65f, UITheme.Hex("#2A4858")),
                ("野拾", "林缘", 6, 0.28f, UITheme.Hex("#2A4838")),
                ("掘矿", "矮洞", 10, 0.52f, UITheme.Hex("#383028")),
                ("炼药", "工坊", 9, 0.48f, UITheme.Hex("#3A2830")),
                ("锻造", "铁砧", 7, 0.35f, UITheme.Hex("#303028")),
                ("讨伐", "林缘", 11, 0.58f, UITheme.Hex("#382828")),
            };

            foreach (var d in data)
                list.Add(CreateSkillItem(sidebar, font, d.name, d.loc, d.lv, d.xp, d.icon));
        }

        private static SkillNavItemView CreateSkillItem(RectTransform parent, TMP_FontAsset font,
            string skillName, string location, int level, float xp, Color iconColor)
        {
            var rt = CreateRect($"Skill_{skillName}", parent);
            AddLayout(rt.gameObject, 0, 56);
            var le = rt.gameObject.GetComponent<LayoutElement>();
            le.flexibleWidth = 1;

            var bg = rt.gameObject.AddComponent<Image>();
            bg.color = UITheme.Transparent;
            var border = bg.gameObject.AddComponent<Outline>();
            border.effectColor = UITheme.Border;
            border.effectDistance = new Vector2(1, -1);
            border.useGraphicAlpha = true;
            border.enabled = false;

            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = bg;
            ConfigureButton(btn, UITheme.Transparent, UITheme.Panel, UITheme.PanelLight);

            var hlg = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            ConfigureLayoutGroup(hlg, expandWidth: true, expandHeight: true);
            hlg.padding = new RectOffset(8, 10, 8, 8);
            hlg.spacing = 10;
            hlg.childAlignment = TextAnchor.MiddleLeft;

            var accent = CreateColorBlock("Accent", rt, UITheme.Teal, new Vector2(3, 40));
            AddLayout(accent.gameObject, 3, 40);
            accent.enabled = false;

            var iconFrame = CreateColorBlock("IconFrame", rt, iconColor, new Vector2(40, 40));
            AddLayout(iconFrame.gameObject, 40, 40);
            StyleOutline(iconFrame, UITheme.Border, new Vector2(1, -1));
            var iconInner = CreateColorBlock("IconInner", iconFrame.rectTransform, UITheme.PanelLight, new Vector2(18, 18));
            Center(iconInner.rectTransform, 18, 18);
            iconInner.color = new Color(iconColor.r, iconColor.g, iconColor.b, 0.55f);

            var info = CreateRect("Info", rt);
            var infoLE = info.gameObject.AddComponent<LayoutElement>();
            infoLE.flexibleWidth = 1;
            var infoVLG = info.gameObject.AddComponent<VerticalLayoutGroup>();
            ConfigureLayoutGroup(infoVLG, expandWidth: true, expandHeight: false);
            infoVLG.spacing = 3;
            infoVLG.childAlignment = TextAnchor.UpperLeft;

            var nameT = CreateLayoutTMP(skillName, info, font, 14, UITheme.Text, TextAlignmentOptions.Left, 20);
            nameT.fontStyle = FontStyles.Bold;
            var lvT = CreateLayoutTMP($"Lv. {level}", info, font, 11, UITheme.Muted, TextAlignmentOptions.Left, 16);
            var barFill = CreateFilledBar(info, 4f, UITheme.BarTrack, UITheme.Teal, xp, "XpBg", "XpFill");

            var view = rt.gameObject.AddComponent<SkillNavItemView>();
            view.Setup(bg, border, accent, iconFrame, nameT, lvT, barFill, skillName, location, level, xp, iconColor);
            return view;
        }

        private static RectTransform CreateBanner(RectTransform parent, TMP_FontAsset font, out TextMeshProUGUI title)
        {
            var banner = CreateRect("LocationBanner", parent);
            AddLayout(banner.gameObject, 0, 136);

            var bg = banner.gameObject.AddComponent<Image>();
            bg.color = UITheme.BannerBg;
            StyleOutline(bg, UITheme.Border, new Vector2(1, -1));

            var gradMid = CreateColorBlock("GradMid", banner, UITheme.BannerMid, Vector2.zero);
            var gradMidRt = gradMid.rectTransform;
            gradMidRt.anchorMin = Vector2.zero;
            gradMidRt.anchorMax = Vector2.one;
            gradMidRt.offsetMin = new Vector2(0, 24);
            gradMidRt.offsetMax = Vector2.zero;
            gradMid.color = new Color(UITheme.BannerMid.r, UITheme.BannerMid.g, UITheme.BannerMid.b, 0.75f);

            var gradBottom = CreateColorBlock("GradBottom", banner, UITheme.BannerAccent, Vector2.zero);
            var gradBottomRt = gradBottom.rectTransform;
            gradBottomRt.anchorMin = new Vector2(0, 0);
            gradBottomRt.anchorMax = new Vector2(1, 0);
            gradBottomRt.pivot = new Vector2(0.5f, 0);
            gradBottomRt.sizeDelta = new Vector2(0, 52);
            gradBottomRt.anchoredPosition = Vector2.zero;

            var overlay = CreateColorBlock("Overlay", banner, Color.black, Vector2.zero);
            Stretch(overlay.rectTransform);
            overlay.color = new Color(0, 0, 0, 0.35f);

            CreateBannerStar(banner, 0.78f, 0.72f, 4f, UITheme.Hex("#C8E8FF"));
            CreateBannerStar(banner, 0.84f, 0.58f, 3f, UITheme.Hex("#C8E8FF"));
            CreateBannerStar(banner, 0.9f, 0.78f, 3f, UITheme.Hex("#E8D8A8"));

            var textArea = CreateRect("BannerText", banner);
            Stretch(textArea);
            var pad = textArea.gameObject.AddComponent<VerticalLayoutGroup>();
            ConfigureLayoutGroup(pad, expandWidth: true, expandHeight: false);
            pad.padding = new RectOffset(20, 20, 18, 16);
            pad.childAlignment = TextAnchor.LowerLeft;
            pad.spacing = 6;

            title = CreateLayoutTMP("萤溪", textArea, font, 24, Color.white, TextAlignmentOptions.Left, 32);
            title.fontStyle = FontStyles.Bold;

            var tags = CreateRect("Tags", textArea);
            AddLayout(tags.gameObject, 0, 24);
            var tagHLG = tags.gameObject.AddComponent<HorizontalLayoutGroup>();
            ConfigureLayoutGroup(tagHLG, expandWidth: false, expandHeight: true);
            tagHLG.spacing = 8;
            CreateTag(tags, font, "微光");
            CreateTag(tags, font, "安全");
            CreateTag(tags, font, "★☆☆");

            return banner;
        }

        private static void CreateBannerStar(RectTransform banner, float x, float y, float size, Color color)
        {
            var star = CreateColorBlock("Star", banner, color, new Vector2(size, size));
            var rt = star.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(x, y);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = Vector2.zero;
        }

        private static void CreateTag(RectTransform parent, TMP_FontAsset font, string text)
        {
            var rt = CreateRect($"Tag_{text}", parent);
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 24;
            le.minWidth = 52;
            var img = rt.gameObject.AddComponent<Image>();
            img.color = UITheme.TagBg;
            StyleOutline(img, UITheme.Teal, new Vector2(1, -1));
            var tmp = CreateTMP(text, rt, font, 11, UITheme.TagText, TextAlignmentOptions.Center);
            tmp.margin = new Vector4(10, 2, 10, 2);
        }

        private static RectTransform CreateActionCards(RectTransform parent, TMP_FontAsset font, List<ActionCardView> list)
        {
            var row = CreateRect("ActionCards", parent);
            var hlg = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            ConfigureLayoutGroup(hlg, expandWidth: true, expandHeight: true);
            hlg.spacing = 12;
            hlg.childAlignment = TextAnchor.UpperLeft;

            var cards = new[]
            {
                ("钓萤虾", "8.0 秒", "+1 萤虾", "萤溪浅水的小虾，夜间会发光。炼药常用基材，也可直接出售。", false, UITheme.Hex("#2A5850")),
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
            var border = bg.gameObject.AddComponent<Outline>();
            border.effectColor = UITheme.Border;
            border.effectDistance = new Vector2(1, -1);
            border.useGraphicAlpha = true;

            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = bg;
            ConfigureButton(btn, UITheme.Panel, UITheme.CardHover, UITheme.PanelLight);
            var cg = rt.gameObject.AddComponent<CanvasGroup>();

            var cardLE = rt.gameObject.AddComponent<LayoutElement>();
            cardLE.flexibleWidth = 1;
            cardLE.minHeight = 124;

            var vlg = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            ConfigureLayoutGroup(vlg, expandWidth: true, expandHeight: false);
            vlg.padding = new RectOffset(12, 12, 12, 12);
            vlg.spacing = 8;

            var thumbFrame = CreateColorBlock("Thumb", rt, UITheme.PanelLight, new Vector2(0, 60));
            AddLayout(thumbFrame.gameObject, 0, 60);
            StyleOutline(thumbFrame, UITheme.Border, new Vector2(1, -1));
            var thumb = CreateColorBlock("ThumbInner", thumbFrame.rectTransform, thumbColor, new Vector2(36, 36));
            Center(thumb.rectTransform, 36, 36);
            var thumbAccent = CreateColorBlock("ThumbAccent", thumb.rectTransform, UITheme.Accent, new Vector2(14, 14));
            Center(thumbAccent.rectTransform, 14, 14);

            var titleT = CreateLayoutTMP(title, rt, font, 14, UITheme.Text, TextAlignmentOptions.Left, 20);
            titleT.fontStyle = FontStyles.Bold;
            var metaRow = CreateRect("Meta", rt);
            AddLayout(metaRow.gameObject, 0, 18);
            var metaHLG = metaRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            ConfigureLayoutGroup(metaHLG, expandWidth: true, expandHeight: true);
            var metaL_T = CreateLayoutTMP(metaL, metaRow, font, 12, UITheme.Muted, TextAlignmentOptions.Left, 18);
            metaL_T.gameObject.GetComponent<LayoutElement>().flexibleWidth = 1;
            var metaR_T = CreateLayoutTMP(metaR, metaRow, font, 11, UITheme.Teal, TextAlignmentOptions.Right, 18);

            var view = rt.gameObject.AddComponent<ActionCardView>();
            view.Setup(bg, border, thumb, titleT, metaL_T, metaR_T, cg, title, metaL, metaR, desc, locked, thumbColor);
            return view;
        }

        private static RectTransform CreateRunningBar(RectTransform parent, TMP_FontAsset font,
            out Image fill, out TextMeshProUGUI label, out TextMeshProUGUI time)
        {
            var rt = CreateRect("RunningBar", parent);
            AddLayout(rt.gameObject, 0, 84);
            var bg = rt.gameObject.AddComponent<Image>();
            bg.color = UITheme.Panel;
            StyleOutline(bg, UITheme.Border, new Vector2(1, -1));

            var hlg = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            ConfigureLayoutGroup(hlg, expandWidth: true, expandHeight: true);
            hlg.padding = new RectOffset(16, 16, 14, 14);
            hlg.spacing = 14;
            hlg.childAlignment = TextAnchor.MiddleLeft;

            var thumb = CreateColorBlock("Thumb", rt, UITheme.Hex("#2A3830"), new Vector2(56, 56));
            AddLayout(thumb.gameObject, 56, 56);
            StyleOutline(thumb, UITheme.Border, new Vector2(1, -1));
            var thumbInner = CreateColorBlock("Inner", thumb.rectTransform, UITheme.Accent, new Vector2(26, 26));
            Center(thumbInner.rectTransform, 26, 26);

            var mid = CreateRect("Mid", rt);
            var midLE = mid.gameObject.AddComponent<LayoutElement>();
            midLE.flexibleWidth = 1;
            var midVLG = mid.gameObject.AddComponent<VerticalLayoutGroup>();
            ConfigureLayoutGroup(midVLG, expandWidth: true, expandHeight: false);
            midVLG.spacing = 10;

            label = CreateLayoutTMP("进行中 · 钓萤虾", mid, font, 15, UITheme.Text, TextAlignmentOptions.Left, 22);
            label.fontStyle = FontStyles.Bold;
            fill = CreateFilledBar(mid, 10f, UITheme.BarTrack, UITheme.Accent, 0.62f);

            time = CreateLayoutTMP("00:06", rt, font, 13, UITheme.Gold, TextAlignmentOptions.Right, 36);
            AddLayout(time.gameObject, 52, 36);

            return rt;
        }

        private static void AddDetailPanel(RectTransform detail, TMP_FontAsset font,
            out TextMeshProUGUI title, out TextMeshProUGUI body)
        {
            var vlg = detail.gameObject.AddComponent<VerticalLayoutGroup>();
            ConfigureLayoutGroup(vlg, expandWidth: true, expandHeight: false);
            vlg.padding = new RectOffset(16, 16, 16, 16);
            vlg.spacing = 12;

            CreateSectionLabel(detail, font, "详情");

            var hero = CreateColorBlock("Hero", detail, UITheme.Panel, new Vector2(200, 200));
            AddLayout(hero.gameObject, 0, 188);
            StyleOutline(hero, UITheme.Border, new Vector2(1, -1));
            var heroInner = CreateColorBlock("Shrimp", hero.rectTransform, UITheme.Accent, new Vector2(88, 88));
            Center(heroInner.rectTransform, 88, 88);
            var heroGlow = CreateColorBlock("Glow", hero.rectTransform, UITheme.Teal, new Vector2(120, 120));
            Center(heroGlow.rectTransform, 120, 120);
            heroGlow.transform.SetAsFirstSibling();
            heroGlow.color = new Color(UITheme.Teal.r, UITheme.Teal.g, UITheme.Teal.b, 0.12f);

            title = CreateLayoutTMP("萤虾", detail, font, 17, UITheme.Text, TextAlignmentOptions.Left, 24);
            title.fontStyle = FontStyles.Bold;
            body = CreateLayoutTMP("萤溪浅水的小虾，夜间会发光。炼药常用基材，也可直接出售。", detail, font, 13, UITheme.Muted, TextAlignmentOptions.TopLeft, 72);
            body.enableWordWrapping = true;
            body.lineSpacing = 4f;
            body.gameObject.GetComponent<LayoutElement>().flexibleHeight = 1;

            CreateDivider(detail, vertical: false);
            CreateLayoutTMP("✓ 溪钓 Lv.1", detail, font, 12, UITheme.Teal, TextAlignmentOptions.Left, 20);
            CreateLayoutTMP("✓ 地点：萤溪", detail, font, 12, UITheme.Teal, TextAlignmentOptions.Left, 20);
            CreateLayoutTMP("稀有：星沙 2%", detail, font, 12, UITheme.Muted, TextAlignmentOptions.Left, 20);
        }

        private static void AddInventoryBar(RectTransform inv, TMP_FontAsset font)
        {
            var hlg = inv.gameObject.AddComponent<HorizontalLayoutGroup>();
            ConfigureLayoutGroup(hlg, expandWidth: false, expandHeight: true);
            hlg.padding = new RectOffset(18, 18, 12, 12);
            hlg.spacing = 12;
            hlg.childAlignment = TextAnchor.MiddleLeft;

            var label = CreateTMP("物品", inv, font, 12, UITheme.Muted, TextAlignmentOptions.Center);
            AddLayout(label.gameObject, 28, 56);

            CreateInvSlot(inv, font, UITheme.Accent, "48");
            CreateInvSlot(inv, font, UITheme.Teal, "22");
            CreateInvSlot(inv, font, UITheme.Hex("#C84848"), "3");
            CreateInvSlot(inv, font, UITheme.Hex("#A88858"), "6");
            CreateInvSlot(inv, font, UITheme.Gold, "31");
        }

        private static void CreateInvSlot(RectTransform parent, TMP_FontAsset font, Color color, string count)
        {
            var rt = CreateRect("Slot", parent);
            AddLayout(rt.gameObject, 56, 56);
            var bg = rt.gameObject.AddComponent<Image>();
            bg.color = UITheme.Panel;
            StyleOutline(bg, UITheme.Border, new Vector2(1, -1));
            var icon = CreateColorBlock("Icon", rt, color, new Vector2(30, 30));
            Center(icon.rectTransform, 30, 30);
            var cnt = CreateTMP(count, rt, font, 11, UITheme.Cream, TextAlignmentOptions.BottomRight);
            var cntRt = cnt.rectTransform;
            cntRt.anchorMin = new Vector2(1, 0);
            cntRt.anchorMax = new Vector2(1, 0);
            cntRt.pivot = new Vector2(1, 0);
            cntRt.anchoredPosition = new Vector2(-5, 3);
            cntRt.sizeDelta = new Vector2(30, 16);
            cnt.fontStyle = FontStyles.Bold;
        }
    }
}
#endif
