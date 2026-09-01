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
        private static void AddTopBar(RectTransform top, TMP_FontAsset font)
        {
            var hlg = top.gameObject.AddComponent<HorizontalLayoutGroup>();
            ConfigureLayoutGroup(hlg, expandWidth: false, expandHeight: true);
            var padH = Mathf.RoundToInt(ConceptLayout.TopBarPaddingH);
            hlg.padding = new RectOffset(padH, padH, 0, 0);
            hlg.spacing = ConceptLayout.TopBarGap;
            hlg.childAlignment = TextAnchor.MiddleLeft;

            var logo = CreateRect("Logo", top);
            AddLayout(logo.gameObject, 0, ConceptLayout.LogoIconSize);
            var logoHLG = logo.gameObject.AddComponent<HorizontalLayoutGroup>();
            ConfigureLayoutGroup(logoHLG, expandWidth: false, expandHeight: true);
            logoHLG.spacing = ConceptLayout.LogoGap;
            logoHLG.childAlignment = TextAnchor.MiddleLeft;

            var logoIcon = CreateColorBlock("LogoIcon", logo, UITheme.LogoBg, new Vector2(ConceptLayout.LogoIconSize, ConceptLayout.LogoIconSize));
            AddLayout(logoIcon.gameObject, ConceptLayout.LogoIconSize, ConceptLayout.LogoIconSize);
            StyleOutline(logoIcon, UITheme.Border, new Vector2(1, -1));
            CreateTMP("✦", logoIcon.rectTransform, font, 18, UITheme.Gold, TextAlignmentOptions.Center);

            var title = CreateLayoutTMP(
                $"坠星谷 <size={ConceptLayout.SubtitleFont}><color=#{ColorUtility.ToHtmlStringRGB(UITheme.Muted)}>萤溪村</color></size>",
                logo, font, ConceptLayout.TitleFont, UITheme.Cream, TextAlignmentOptions.Left, ConceptLayout.LogoIconSize);
            title.richText = true;
            title.fontStyle = FontStyles.Bold;
            var titleLE = title.gameObject.GetComponent<LayoutElement>();
            titleLE.flexibleWidth = 1;

            var spacer = CreateRect("Spacer", top);
            spacer.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

            var currency = CreateRect("Currency", top);
            var currencyHLG = currency.gameObject.AddComponent<HorizontalLayoutGroup>();
            ConfigureLayoutGroup(currencyHLG, expandWidth: false, expandHeight: true);
            currencyHLG.spacing = ConceptLayout.CurrencyGap;
            currencyHLG.childAlignment = TextAnchor.MiddleRight;

            CreateLayoutTMP("🪙 0", currency, font, 14, UITheme.Gold, TextAlignmentOptions.Right, ConceptLayout.LogoIconSize);
            CreateLayoutTMP("声望 ★★☆", currency, font, 14, UITheme.Muted, TextAlignmentOptions.Right, ConceptLayout.LogoIconSize);

            CreateTopButton(top, font, "图鉴");
            CreateTopButton(top, font, "背包");
            CreateTopButton(top, font, "设置");
        }

        private static void CreateTopButton(RectTransform parent, TMP_FontAsset font, string label)
        {
            var rt = CreateRect($"Btn_{label}", parent);
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.minHeight = ConceptLayout.TopBtnFont + ConceptLayout.TopBtnPadV * 2f + 8f;
            var img = rt.gameObject.AddComponent<Image>();
            img.color = UITheme.PanelLight;
            StyleOutline(img, UITheme.Border, new Vector2(1, -1));
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            ConfigureButton(btn, UITheme.PanelLight, UITheme.CardHover, UITheme.ButtonPressed);
            var tmp = CreateTMP(label, rt, font, ConceptLayout.TopBtnFont, UITheme.Cream, TextAlignmentOptions.Center);
            tmp.margin = new Vector4(ConceptLayout.TopBtnPadH, ConceptLayout.TopBtnPadV, ConceptLayout.TopBtnPadH, ConceptLayout.TopBtnPadV);
        }

        private static void AddSkillNav(RectTransform sidebar, TMP_FontAsset font, List<SkillNavItemView> list)
        {
            var vlg = sidebar.gameObject.AddComponent<VerticalLayoutGroup>();
            ConfigureLayoutGroup(vlg, expandWidth: true, expandHeight: false);
            vlg.padding = new RectOffset(
                Mathf.RoundToInt(ConceptLayout.SidebarPadH),
                Mathf.RoundToInt(ConceptLayout.SidebarPadH),
                Mathf.RoundToInt(ConceptLayout.SidebarPadV),
                Mathf.RoundToInt(ConceptLayout.SidebarPadV));
            vlg.spacing = ConceptLayout.SidebarGap;

            var data = new (string workId, string name, string loc, int lv, float xp, Color icon)[]
            {
                ("scavenge", "拾荒", "萤溪村", 1, 0f, UITheme.SkillForage),
            };

            foreach (var d in data)
                list.Add(CreateSkillItem(sidebar, font, d.workId, d.name, d.loc, d.lv, d.xp, d.icon));
        }

        private static SkillNavItemView CreateSkillItem(RectTransform parent, TMP_FontAsset font,
            string workId, string skillName, string location, int level, float xp, Color iconColor)
        {
            var rt = CreateRect($"Skill_{skillName}", parent);
            AddLayout(rt.gameObject, 0, ConceptLayout.SkillMinHeight);
            rt.gameObject.GetComponent<LayoutElement>().flexibleWidth = 1;

            var bg = rt.gameObject.AddComponent<Image>();
            bg.color = UITheme.Transparent;
            var border = bg.gameObject.AddComponent<Outline>();
            border.effectColor = UITheme.Teal;
            border.effectDistance = new Vector2(1, -1);
            border.useGraphicAlpha = true;
            border.enabled = false;

            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = bg;
            ConfigureButton(btn, UITheme.Transparent, UITheme.Panel, UITheme.PanelLight);

            var hlg = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            ConfigureLayoutGroup(hlg, expandWidth: true, expandHeight: true);
            hlg.padding = new RectOffset(
                Mathf.RoundToInt(ConceptLayout.SkillPadH),
                Mathf.RoundToInt(ConceptLayout.SkillPadH),
                Mathf.RoundToInt(ConceptLayout.SkillPadV),
                Mathf.RoundToInt(ConceptLayout.SkillPadV));
            hlg.spacing = ConceptLayout.SkillGap;
            hlg.childAlignment = TextAnchor.MiddleLeft;

            var accent = CreateColorBlock("Accent", rt, UITheme.Teal, new Vector2(ConceptLayout.SkillAccentWidth, ConceptLayout.SkillIconSize));
            AddLayout(accent.gameObject, ConceptLayout.SkillAccentWidth, ConceptLayout.SkillIconSize);
            accent.enabled = false;

            var iconFrame = CreateColorBlock("IconFrame", rt, iconColor, new Vector2(ConceptLayout.SkillIconSize, ConceptLayout.SkillIconSize));
            AddLayout(iconFrame.gameObject, ConceptLayout.SkillIconSize, ConceptLayout.SkillIconSize);
            StyleOutline(iconFrame, UITheme.Border, new Vector2(1, -1));

            var info = CreateRect("Info", rt);
            info.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            var infoVLG = info.gameObject.AddComponent<VerticalLayoutGroup>();
            ConfigureLayoutGroup(infoVLG, expandWidth: true, expandHeight: false);
            infoVLG.spacing = 2;
            infoVLG.childAlignment = TextAnchor.UpperLeft;

            var nameT = CreateLayoutTMP(skillName, info, font, ConceptLayout.SkillNameFont, UITheme.Text, TextAlignmentOptions.Left, 18);
            var lvT = CreateLayoutTMP($"Lv. {level}", info, font, ConceptLayout.SkillLvFont, UITheme.Muted, TextAlignmentOptions.Left, 14);
            var barFill = CreateFilledBar(info, ConceptLayout.SkillBarHeight, UITheme.BarTrack, UITheme.Teal, xp, "XpBg", "XpFill");

            var view = rt.gameObject.AddComponent<SkillNavItemView>();
            view.Setup(bg, border, accent, iconFrame, nameT, lvT, barFill, workId, skillName, location, level, xp, iconColor);
            return view;
        }

        private static RectTransform CreateBanner(RectTransform parent, TMP_FontAsset font, out TextMeshProUGUI title)
        {
            var banner = CreateRect("LocationBanner", parent);
            AddLayout(banner.gameObject, 0, ConceptLayout.BannerHeight);

            var bg = banner.gameObject.AddComponent<Image>();
            bg.color = UITheme.BannerBg;
            StyleOutline(bg, UITheme.Border, new Vector2(1, -1));

            var gradMid = CreateColorBlock("GradMid", banner, UITheme.BannerMid, Vector2.zero);
            var gradMidRt = gradMid.rectTransform;
            gradMidRt.anchorMin = Vector2.zero;
            gradMidRt.anchorMax = Vector2.one;
            gradMidRt.offsetMin = Vector2.zero;
            gradMidRt.offsetMax = Vector2.zero;
            gradMid.color = new Color(UITheme.BannerMid.r, UITheme.BannerMid.g, UITheme.BannerMid.b, 0.85f);

            CreateBannerStar(banner, 0.78f, 0.72f, 4f, UITheme.StarLight);
            CreateBannerStar(banner, 0.84f, 0.58f, 3f, UITheme.StarLight);
            CreateBannerStar(banner, 0.9f, 0.78f, 3f, UITheme.StarWarm);

            var overlay = CreateColorBlock("Overlay", banner, Color.black, Vector2.zero);
            var overlayRt = overlay.rectTransform;
            overlayRt.anchorMin = Vector2.zero;
            overlayRt.anchorMax = new Vector2(0.65f, 1f);
            overlayRt.offsetMin = Vector2.zero;
            overlayRt.offsetMax = Vector2.zero;
            overlay.color = new Color(0, 0, 0, 0.65f);

            var textArea = CreateRect("BannerText", banner);
            Stretch(textArea);
            var pad = textArea.gameObject.AddComponent<VerticalLayoutGroup>();
            ConfigureLayoutGroup(pad, expandWidth: true, expandHeight: false);
            pad.padding = new RectOffset(
                Mathf.RoundToInt(ConceptLayout.BannerOverlayPadH),
                Mathf.RoundToInt(ConceptLayout.BannerOverlayPadH),
                Mathf.RoundToInt(ConceptLayout.BannerOverlayPadV),
                Mathf.RoundToInt(ConceptLayout.BannerOverlayPadV));
            pad.childAlignment = TextAnchor.LowerLeft;
            pad.spacing = 6;

            title = CreateLayoutTMP("萤溪", textArea, font, ConceptLayout.BannerTitleFont, Color.white, TextAlignmentOptions.Left, 28);
            title.fontStyle = FontStyles.Bold;

            var tags = CreateRect("Tags", textArea);
            AddLayout(tags.gameObject, 0, 22);
            var tagHLG = tags.gameObject.AddComponent<HorizontalLayoutGroup>();
            ConfigureLayoutGroup(tagHLG, expandWidth: false, expandHeight: true);
            tagHLG.spacing = ConceptLayout.TagGap;
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
            le.preferredHeight = 22;
            le.minWidth = 48;
            var img = rt.gameObject.AddComponent<Image>();
            img.color = UITheme.TagBg;
            StyleOutline(img, new Color(UITheme.Teal.r, UITheme.Teal.g, UITheme.Teal.b, 0.5f), new Vector2(1, -1));
            var tmp = CreateTMP(text, rt, font, ConceptLayout.TagFont, UITheme.TagText, TextAlignmentOptions.Center);
            tmp.margin = new Vector4(8, 3, 8, 3);
        }

        private static RectTransform CreateActionCards(RectTransform parent, TMP_FontAsset font, List<ActionCardView> list)
        {
            var row = CreateRect("ActionCards", parent);
            var hlg = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            ConfigureLayoutGroup(hlg, expandWidth: true, expandHeight: true);
            hlg.spacing = ConceptLayout.CardGap;
            hlg.childAlignment = TextAnchor.UpperLeft;

            const int slotCount = 6;
            for (var i = 0; i < slotCount; i++)
            {
                list.Add(CreateActionCard(row, font,
                    "占位", "—", "—", "运行后由数据填充", false, UITheme.SkillForage));
            }

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
            cardLE.minHeight = ConceptLayout.CardMinHeight;

            var pad = Mathf.RoundToInt(ConceptLayout.CardPadding);
            var vlg = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            ConfigureLayoutGroup(vlg, expandWidth: true, expandHeight: false);
            vlg.padding = new RectOffset(pad, pad, pad, pad);
            vlg.spacing = 8;

            var thumbFrame = CreateColorBlock("Thumb", rt, UITheme.PanelLight, new Vector2(0, ConceptLayout.CardThumbHeight));
            AddLayout(thumbFrame.gameObject, 0, ConceptLayout.CardThumbHeight);
            var thumb = CreateColorBlock("ThumbInner", thumbFrame.rectTransform, thumbColor, new Vector2(36, 36));
            Center(thumb.rectTransform, 36, 36);

            var titleT = CreateLayoutTMP(title, rt, font, ConceptLayout.CardTitleFont, UITheme.Text, TextAlignmentOptions.Left, 18);
            var metaRow = CreateRect("Meta", rt);
            AddLayout(metaRow.gameObject, 0, 16);
            var metaHLG = metaRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            ConfigureLayoutGroup(metaHLG, expandWidth: true, expandHeight: true);
            var metaL_T = CreateLayoutTMP(metaL, metaRow, font, ConceptLayout.CardMetaFont, UITheme.Muted, TextAlignmentOptions.Left, 16);
            metaL_T.gameObject.GetComponent<LayoutElement>().flexibleWidth = 1;
            var metaR_T = CreateLayoutTMP(metaR, metaRow, font, ConceptLayout.CardYieldFont, UITheme.Teal, TextAlignmentOptions.Right, 16);

            var view = rt.gameObject.AddComponent<ActionCardView>();
            view.Setup(bg, border, thumb, titleT, metaL_T, metaR_T, cg, title, metaL, metaR, desc, locked, thumbColor);
            return view;
        }

        private static RectTransform CreateRunningBar(RectTransform parent, TMP_FontAsset font,
            out Image fill, out TextMeshProUGUI label, out TextMeshProUGUI time)
        {
            var rt = CreateRect("RunningBar", parent);
            var bg = rt.gameObject.AddComponent<Image>();
            bg.color = UITheme.Panel;
            StyleOutline(bg, UITheme.Border, new Vector2(1, -1));

            var hlg = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            ConfigureLayoutGroup(hlg, expandWidth: true, expandHeight: true);
            hlg.padding = new RectOffset(
                Mathf.RoundToInt(ConceptLayout.RunningPadH),
                Mathf.RoundToInt(ConceptLayout.RunningPadH),
                Mathf.RoundToInt(ConceptLayout.RunningPadV),
                Mathf.RoundToInt(ConceptLayout.RunningPadV));
            hlg.spacing = ConceptLayout.RunningGap;
            hlg.childAlignment = TextAnchor.MiddleLeft;

            var thumb = CreateColorBlock("Thumb", rt, UITheme.RunningThumb, new Vector2(ConceptLayout.RunningThumb, ConceptLayout.RunningThumb));
            AddLayout(thumb.gameObject, ConceptLayout.RunningThumb, ConceptLayout.RunningThumb);
            var thumbInner = CreateColorBlock("Inner", thumb.rectTransform, UITheme.Accent, new Vector2(24, 24));
            Center(thumbInner.rectTransform, 24, 24);

            var mid = CreateRect("Mid", rt);
            mid.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            var midVLG = mid.gameObject.AddComponent<VerticalLayoutGroup>();
            ConfigureLayoutGroup(midVLG, expandWidth: true, expandHeight: false);
            midVLG.spacing = ConceptLayout.RunningLabelToBar;

            label = CreateLayoutTMP("进行中 · 钓萤虾", mid, font, ConceptLayout.RunningLabelFont, UITheme.Text, TextAlignmentOptions.Left, 20);
            fill = CreateFilledBar(mid, ConceptLayout.RunningBarHeight, UITheme.BarTrack, UITheme.Accent, 0.62f);

            time = CreateLayoutTMP("00:06", rt, font, ConceptLayout.RunningTimeFont, UITheme.Gold, TextAlignmentOptions.Right, ConceptLayout.RunningThumb);
            AddLayout(time.gameObject, ConceptLayout.RunningTimeWidth, ConceptLayout.RunningThumb);

            return rt;
        }

        private static void AddDetailPanel(RectTransform detail, TMP_FontAsset font,
            out TextMeshProUGUI title, out TextMeshProUGUI body)
        {
            var pad = Mathf.RoundToInt(ConceptLayout.DetailPadding);
            var vlg = detail.gameObject.AddComponent<VerticalLayoutGroup>();
            ConfigureLayoutGroup(vlg, expandWidth: true, expandHeight: false);
            vlg.padding = new RectOffset(pad, pad, pad, pad);
            vlg.spacing = ConceptLayout.DetailGap;

            var hero = CreateColorBlock("Hero", detail, UITheme.Panel, Vector2.zero);
            var heroLE = hero.gameObject.AddComponent<LayoutElement>();
            heroLE.flexibleWidth = 1;
            heroLE.preferredHeight = ConceptLayout.DetailWidth - ConceptLayout.DetailPadding * 2f;
            StyleOutline(hero, UITheme.Border, new Vector2(1, -1));
            var heroInner = CreateColorBlock("Shrimp", hero.rectTransform, UITheme.Accent, new Vector2(80, 80));
            Center(heroInner.rectTransform, 80, 80);

            title = CreateLayoutTMP("村口 · 拾荒", detail, font, ConceptLayout.DetailTitleFont, UITheme.Text, TextAlignmentOptions.Left, 22);
            body = CreateLayoutTMP("拾荒分多个场景，每个地点掉落不同。选动作后横幅显示当前场景。", detail, font, ConceptLayout.DetailBodyFont, UITheme.Muted, TextAlignmentOptions.TopLeft, 64);
            body.enableWordWrapping = true;
            body.lineSpacing = 6f;
            body.gameObject.GetComponent<LayoutElement>().flexibleHeight = 1;

            CreateLayoutTMP("✓ 拾荒 Lv.1", detail, font, ConceptLayout.DetailReqFont, UITheme.Teal, TextAlignmentOptions.Left, 18);
            CreateLayoutTMP("场景：村口、街道、谷仓后…", detail, font, ConceptLayout.DetailReqFont, UITheme.Teal, TextAlignmentOptions.Left, 18);
            CreateLayoutTMP("掉落：按动作独立概率", detail, font, ConceptLayout.DetailReqFont, UITheme.Muted, TextAlignmentOptions.Left, 18);
        }

        private static InventoryBarView AddInventoryBar(RectTransform inv, TMP_FontAsset font)
        {
            var hlg = inv.gameObject.AddComponent<HorizontalLayoutGroup>();
            ConfigureLayoutGroup(hlg, expandWidth: false, expandHeight: true);
            hlg.padding = new RectOffset(
                Mathf.RoundToInt(ConceptLayout.InvPadH),
                Mathf.RoundToInt(ConceptLayout.InvPadH),
                Mathf.RoundToInt(ConceptLayout.InvPadV),
                Mathf.RoundToInt(ConceptLayout.InvPadV));
            hlg.spacing = ConceptLayout.InvGap;
            hlg.childAlignment = TextAnchor.MiddleLeft;

            var label = CreateTMP("物品", inv, font, 12, UITheme.Muted, TextAlignmentOptions.Center);
            AddLayout(label.gameObject, ConceptLayout.InvLabelWidth, ConceptLayout.SlotSize);
            label.characterSpacing = 2f;

            var slots = CreateRect("Slots", inv);
            var slotsHLG = slots.gameObject.AddComponent<HorizontalLayoutGroup>();
            ConfigureLayoutGroup(slotsHLG, expandWidth: false, expandHeight: true);
            slotsHLG.spacing = ConceptLayout.InvGap;
            slotsHLG.childAlignment = TextAnchor.MiddleLeft;
            var slotsLE = slots.gameObject.AddComponent<LayoutElement>();
            slotsLE.flexibleWidth = 1;

            var view = inv.gameObject.AddComponent<InventoryBarView>();
            view.Configure(slots, font, 10);
            return view;
        }
    }
}
#endif
