#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UniverIdle.Game;
using UniverIdle.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace UniverIdle.Editor
{
    public static partial class MainUISetup
    {
        private const string SkillItemPrefabPath = "Assets/GameResources/Prefab/Skill_打猎.prefab";

        private static void AddTopBar(RectTransform top, TMP_FontAsset font, out Button inventoryButton)
        {
            var hlg = top.gameObject.AddComponent<HorizontalLayoutGroup>();
            ConfigureLayoutGroup(hlg, expandWidth: true, expandHeight: false);
            hlg.childForceExpandHeight = false;
            var padH = Mathf.RoundToInt(ConceptLayout.TopBarPaddingH);
            hlg.padding = new RectOffset(padH, padH,
                Mathf.RoundToInt(ConceptLayout.TopBarPadV),
                Mathf.RoundToInt(ConceptLayout.TopBarPadV));
            hlg.spacing = ConceptLayout.TopBarGap;
            hlg.childAlignment = TextAnchor.MiddleLeft;

            var logo = CreateRect("Logo", top);
            AddLayout(logo.gameObject, 0, ConceptLayout.TopBarContentHeight);
            var logoHLG = logo.gameObject.AddComponent<HorizontalLayoutGroup>();
            ConfigureLayoutGroup(logoHLG, expandWidth: false, expandHeight: false);
            logoHLG.spacing = ConceptLayout.LogoGap;
            logoHLG.childAlignment = TextAnchor.MiddleLeft;

            var logoIcon = CreateColorBlock("LogoIcon", logo, UITheme.LogoBg, new Vector2(ConceptLayout.LogoIconSize, ConceptLayout.LogoIconSize));
            AddLayout(logoIcon.gameObject, ConceptLayout.LogoIconSize, ConceptLayout.LogoIconSize);
            StyleOutline(logoIcon, UITheme.Border, new Vector2(1, -1));
            CreateTMP("✦", logoIcon.rectTransform, font, ConceptLayout.TopBarLogoGlyphFont, UITheme.Gold, TextAlignmentOptions.Center);

            var title = CreateLayoutTMP(
                $"坠星谷 <size={ConceptLayout.SubtitleFont}><color=#{ColorUtility.ToHtmlStringRGB(UITheme.Muted)}>萤溪村</color></size>",
                logo, font, ConceptLayout.TitleFont, UITheme.Cream, TextAlignmentOptions.Left, ConceptLayout.TopBarContentHeight);
            title.richText = true;
            title.fontStyle = FontStyles.Bold;
            var titleLE = title.gameObject.GetComponent<LayoutElement>();
            titleLE.flexibleWidth = 1;

            var spacer = CreateRect("Spacer", top);
            spacer.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

            var currency = CreateRect("Currency", top);
            var currencyHLG = currency.gameObject.AddComponent<HorizontalLayoutGroup>();
            ConfigureLayoutGroup(currencyHLG, expandWidth: false, expandHeight: false);
            currencyHLG.spacing = ConceptLayout.CurrencyGap;
            currencyHLG.childAlignment = TextAnchor.MiddleRight;

            CreateLayoutTMP("🪙 1,240 铜", currency, font, ConceptLayout.TopBarCurrencyFont, UITheme.Gold, TextAlignmentOptions.Right, ConceptLayout.TopBarContentHeight);
            CreateLayoutTMP("声望 ★★☆", currency, font, ConceptLayout.TopBarCurrencyFont, UITheme.Muted, TextAlignmentOptions.Right, ConceptLayout.TopBarContentHeight);

            CreateTopButton(top, font, "图鉴");
            inventoryButton = CreateTopButton(top, font, "背包");
            CreateTopButton(top, font, "设置");
        }

        private static Button CreateTopButton(RectTransform parent, TMP_FontAsset font, string label)
        {
            var rt = CreateRect($"Btn_{label}", parent);
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = ConceptLayout.TopBarContentHeight;
            le.flexibleHeight = 0;
            var img = rt.gameObject.AddComponent<Image>();
            img.color = UITheme.PanelLight;
            StyleOutline(img, UITheme.Border, new Vector2(1, -1));
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            ConfigureButton(btn, UITheme.PanelLight, UITheme.CardHover, UITheme.ButtonPressed);
            var tmp = CreateTMP(label, rt, font, ConceptLayout.TopBtnFont, UITheme.Cream, TextAlignmentOptions.Center);
            tmp.margin = new Vector4(ConceptLayout.TopBtnPadH, ConceptLayout.TopBtnPadV, ConceptLayout.TopBtnPadH, ConceptLayout.TopBtnPadV);
            return btn;
        }

        private static void AddSkillNav(RectTransform sidebar, TMP_FontAsset font, List<SkillNavItemView> list)
        {
            var scrollRt = CreateRect("Scroll", sidebar);
            Stretch(scrollRt);
            var scroll = scrollRt.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            var viewport = CreateRect("Viewport", scrollRt);
            Stretch(viewport);
            var viewportImg = viewport.gameObject.AddComponent<Image>();
            viewportImg.color = Color.white;
            viewportImg.raycastTarget = false;
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;

            var content = CreateRect("Content", viewport);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0, 0);

            var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
            ConfigureLayoutGroup(vlg, expandWidth: true, expandHeight: false);
            vlg.padding = new RectOffset(
                Mathf.RoundToInt(ConceptLayout.SidebarPadH),
                Mathf.RoundToInt(ConceptLayout.SidebarPadH),
                Mathf.RoundToInt(ConceptLayout.SidebarPadV),
                Mathf.RoundToInt(ConceptLayout.SidebarPadV));
            vlg.spacing = ConceptLayout.SidebarGap;

            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport;
            scroll.content = content;

            var data = new (string workId, string name, string loc, int lv, float xp, Color icon, bool available)[]
            {
                ("scavenge", "拾荒", "萤溪村", 1, 0f, UITheme.SkillForage, true),
                ("woodcutting", "砍伐", "黑松林", 1, 0f, UITheme.SkillWood, true),
                ("mining", "挖矿", "坠星矿洞", 1, 0f, UITheme.SkillMine, true),
                ("monster_explore", "魔物探索", "坠星野外", 1, 0f, UITheme.SkillCombat, true),
                ("", "打猎", "", 0, 0f, UITheme.SkillHunt, false),
                ("", "溪钓", "", 0, 0f, UITheme.SkillFish, false),
                ("", "炼药", "", 0, 0f, UITheme.SkillAlchemy, false),
                ("", "讨伐", "", 0, 0f, UITheme.SkillSmith, false),
            };

            foreach (var d in data)
                list.Add(InstantiateSkillItem(content, d.workId, d.name, d.loc, d.lv, d.xp, d.icon, d.available));
        }

        private static SkillNavItemView InstantiateSkillItem(RectTransform parent,
            string workId, string skillName, string location, int level, float xp, Color iconColor, bool available)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SkillItemPrefabPath);
            if (prefab == null)
                throw new System.IO.FileNotFoundException($"[UniverIdle] 找不到技能预制体：{SkillItemPrefabPath}");

            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = $"Skill_{skillName}";

            var le = go.GetComponent<LayoutElement>();
            if (le != null)
                le.flexibleWidth = 1;

            var view = go.GetComponent<SkillNavItemView>();
            if (view == null)
                throw new System.InvalidOperationException($"[UniverIdle] {SkillItemPrefabPath} 缺少 SkillNavItemView");

            view.Configure(workId, skillName, location, level, xp, iconColor, available);

            var btn = go.GetComponent<Button>();
            if (btn != null)
                btn.interactable = available && !string.IsNullOrEmpty(workId);

            return view;
        }

        private static StandardWorkCenterView CreateStandardWorkCenter(
            RectTransform host, TMP_FontAsset font, string workId, WorkCenterHost centerHost)
        {
            var root = CreateRect($"WorkView_{workId}", host);
            Stretch(root);

            var vlg = root.gameObject.AddComponent<VerticalLayoutGroup>();
            ConfigureLayoutGroup(vlg, expandWidth: true, expandHeight: false);
            var cp = Mathf.RoundToInt(ConceptLayout.CenterPadding);
            vlg.padding = new RectOffset(cp, cp, cp, cp);
            vlg.spacing = ConceptLayout.CenterGap;

            var banner = CreateBanner(root, font, out var locationTitle);
            var actions = new List<ActionCardView>();
            var cardsRow = CreateActionCards(banner, font, actions);

            if (ConceptLayout.UseCenterFlexSpacer)
            {
                var centerSpacer = CreateRect("FlexSpacer", root);
                centerSpacer.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
            }

            var runningBar = CreateRunningBar(root, font, out var progressFill, out var progressLabel, out var progressTime);
            var runningLE = runningBar.gameObject.AddComponent<LayoutElement>();
            runningLE.preferredHeight = ConceptLayout.RunningBarTotalHeight;
            runningLE.flexibleHeight = 0;

            var view = root.gameObject.AddComponent<StandardWorkCenterView>();
            view.Configure(workId, locationTitle, actions, progressFill, progressLabel, progressTime);
            centerHost.Register(view);
            return view;
        }

        private static RectTransform CreateBanner(RectTransform parent, TMP_FontAsset font, out TextMeshProUGUI title)
        {
            var banner = CreateRect("LocationBanner", parent);
            var bannerVlg = banner.gameObject.AddComponent<VerticalLayoutGroup>();
            ConfigureLayoutGroup(bannerVlg, expandWidth: true, expandHeight: false);
            bannerVlg.spacing = ConceptLayout.CenterGap;
            bannerVlg.childAlignment = TextAnchor.UpperLeft;

            var bannerFitter = banner.gameObject.AddComponent<ContentSizeFitter>();
            bannerFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            bannerFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var bannerLe = banner.gameObject.AddComponent<LayoutElement>();
            bannerLe.flexibleHeight = 0;

            var bg = banner.gameObject.AddComponent<Image>();
            bg.color = UITheme.BannerBg;
            bg.raycastTarget = false;
            StyleOutline(bg, UITheme.Border, new Vector2(1, -1));

            var bannerArt = CreateRect("BannerArt", banner);
            LockLayoutHeight(bannerArt, ConceptLayout.BannerHeight);
            var artLe = bannerArt.gameObject.GetComponent<LayoutElement>();
            if (artLe != null) artLe.flexibleWidth = 1;

            var gradMid = CreateColorBlock("GradMid", bannerArt, UITheme.BannerMid, Vector2.zero);
            var gradMidRt = gradMid.rectTransform;
            gradMidRt.anchorMin = Vector2.zero;
            gradMidRt.anchorMax = Vector2.one;
            gradMidRt.offsetMin = Vector2.zero;
            gradMidRt.offsetMax = Vector2.zero;
            gradMid.color = new Color(UITheme.BannerMid.r, UITheme.BannerMid.g, UITheme.BannerMid.b, 0.85f);
            IgnoreLayout(gradMidRt);

            CreateBannerStar(bannerArt, 0.78f, 0.72f, 4f, UITheme.StarLight);
            CreateBannerStar(bannerArt, 0.84f, 0.58f, 3f, UITheme.StarLight);
            CreateBannerStar(bannerArt, 0.9f, 0.78f, 3f, UITheme.StarWarm);

            var overlay = CreateColorBlock("Overlay", bannerArt, Color.black, Vector2.zero);
            var overlayRt = overlay.rectTransform;
            overlayRt.anchorMin = Vector2.zero;
            overlayRt.anchorMax = new Vector2(0.65f, 1f);
            overlayRt.offsetMin = Vector2.zero;
            overlayRt.offsetMax = Vector2.zero;
            overlay.color = new Color(0, 0, 0, 0.65f);
            IgnoreLayout(overlayRt);

            var textArea = CreateRect("BannerText", bannerArt);
            AnchorStretchWidthBottom(textArea);
            IgnoreLayout(textArea);
            var pad = textArea.gameObject.AddComponent<VerticalLayoutGroup>();
            ConfigureLayoutGroup(pad, expandWidth: true, expandHeight: false);
            pad.padding = new RectOffset(
                Mathf.RoundToInt(ConceptLayout.BannerOverlayPadH),
                Mathf.RoundToInt(ConceptLayout.BannerOverlayPadH),
                Mathf.RoundToInt(ConceptLayout.BannerOverlayPadV),
                Mathf.RoundToInt(ConceptLayout.BannerOverlayPadV));
            pad.childAlignment = TextAnchor.LowerLeft;
            pad.spacing = 6;

            var titleHeight = Mathf.RoundToInt(ConceptLayout.BannerTitleFont * 1.28f);
            title = CreateLayoutTMP("村口", textArea, font, ConceptLayout.BannerTitleFont, Color.white, TextAlignmentOptions.Left, titleHeight);
            title.fontStyle = FontStyles.Bold;
            LockLayoutHeight(title.rectTransform, titleHeight);

            var tags = CreateRect("Tags", textArea);
            LockLayoutHeight(tags, ConceptLayout.TagHeight);
            var tagsFitter = tags.gameObject.AddComponent<ContentSizeFitter>();
            tagsFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            tagsFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var tagHLG = tags.gameObject.AddComponent<HorizontalLayoutGroup>();
            ConfigureLayoutGroup(tagHLG, expandWidth: false, expandHeight: false);
            tagHLG.spacing = ConceptLayout.TagGap;
            CreateTag(tags, font, "微光");
            CreateTag(tags, font, "安全");
            CreateTag(tags, font, "★☆☆");

            var textFitter = textArea.gameObject.AddComponent<ContentSizeFitter>();
            textFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            textFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            return banner;
        }

        private static void CreateBannerStar(RectTransform bannerArt, float x, float y, float size, Color color)
        {
            var star = CreateColorBlock("Star", bannerArt, color, new Vector2(size, size));
            var rt = star.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(x, y);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = Vector2.zero;
            IgnoreLayout(rt);
        }

        private static void CreateTag(RectTransform parent, TMP_FontAsset font, string text)
        {
            var rt = CreateRect($"Tag_{text}", parent);
            LockLayoutHeight(rt, ConceptLayout.TagHeight);
            var le = rt.gameObject.GetComponent<LayoutElement>();
            if (le != null) le.minWidth = 48;
            var img = rt.gameObject.AddComponent<Image>();
            img.color = UITheme.TagBg;
            StyleOutline(img, new Color(UITheme.Teal.r, UITheme.Teal.g, UITheme.Teal.b, 0.5f), new Vector2(1, -1));
            var tmp = CreateTMP(text, rt, font, ConceptLayout.TagFont, UITheme.TagText, TextAlignmentOptions.Center);
            tmp.margin = new Vector4(8, 3, 8, 3);
        }

        private static RectTransform CreateActionCards(RectTransform parent, TMP_FontAsset font, List<ActionCardView> list)
        {
            var row = CreateRect("ActionCards", parent);
            ConfigureActionCardsGrid(row);

            const int slotCount = 3;
            var previewSpots = new[] { "老王家", "老李家", "张铁匠铺" };
            for (var i = 0; i < slotCount; i++)
            {
                list.Add(CreateActionCard(row, font,
                    previewSpots[i], "—", "", "同一场景下的子地点，运行后由配表填充", false, UITheme.SkillForage));
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

            var pad = Mathf.RoundToInt(ConceptLayout.CardPadding);
            var vlg = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            ConfigureLayoutGroup(vlg, expandWidth: true, expandHeight: false);
            vlg.padding = new RectOffset(pad, pad, pad, pad);
            vlg.spacing = ConceptLayout.CardVlgSpacing;

            var thumbFrame = CreateColorBlock("Thumb", rt, UITheme.PanelLight,
                new Vector2(ConceptLayout.CardThumbWidth > 0f ? ConceptLayout.CardThumbWidth : 0f, ConceptLayout.CardThumbHeight));
            ApplyCardThumbLayout(thumbFrame.rectTransform);
            var innerW = ConceptLayout.CardThumbInnerWidth;
            var innerH = ConceptLayout.CardThumbInnerHeight;
            var thumb = CreateColorBlock("ThumbInner", thumbFrame.rectTransform, thumbColor, new Vector2(innerW, innerH));
            Center(thumb.rectTransform, innerW, innerH);

            var titleT = CreateLayoutTMP(title, rt, font, ConceptLayout.CardTitleFont, UITheme.Text, TextAlignmentOptions.Left, ConceptLayout.CardTitleHeight);
            LockLayoutHeight(titleT.rectTransform, ConceptLayout.CardTitleHeight);
            var metaRow = CreateRect("Meta", rt);
            LockLayoutHeight(metaRow, ConceptLayout.CardMetaHeight);
            var metaHLG = metaRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            ConfigureLayoutGroup(metaHLG, expandWidth: true, expandHeight: false);
            var metaL_T = CreateLayoutTMP(metaL, metaRow, font, ConceptLayout.CardMetaFont, UITheme.Muted, TextAlignmentOptions.Left, ConceptLayout.CardMetaHeight);
            metaL_T.gameObject.GetComponent<LayoutElement>().flexibleWidth = 1;
            var metaR_T = CreateLayoutTMP(metaR, metaRow, font, ConceptLayout.CardYieldFont, UITheme.Teal, TextAlignmentOptions.Right, ConceptLayout.CardMetaHeight);
            LockLayoutHeight(metaL_T.rectTransform, ConceptLayout.CardMetaHeight);
            LockLayoutHeight(metaR_T.rectTransform, ConceptLayout.CardMetaHeight);

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
            ConfigureLayoutGroup(vlg, expandWidth: ConceptLayout.DetailChildForceExpandWidth, expandHeight: false);
            vlg.padding = new RectOffset(pad, pad, pad, pad);
            vlg.spacing = ConceptLayout.DetailGap;

            var hero = CreateColorBlock("Hero", detail, UITheme.Panel, Vector2.zero);
            var heroLE = hero.gameObject.AddComponent<LayoutElement>();
            heroLE.flexibleWidth = 1;
            heroLE.preferredHeight = ConceptLayout.DetailHeroHeight;
            StyleOutline(hero, UITheme.Border, new Vector2(1, -1));
            var heroInner = CreateColorBlock("HeroThumb", hero.rectTransform, UITheme.Accent,
                new Vector2(ConceptLayout.DetailHeroThumbWidth, ConceptLayout.DetailHeroThumbHeight));
            Center(heroInner.rectTransform, ConceptLayout.DetailHeroThumbWidth, ConceptLayout.DetailHeroThumbHeight);

            title = CreateLayoutTMP("村口 · 拾荒", detail, font, ConceptLayout.DetailTitleFont, UITheme.Text,
                TextAlignmentOptions.Left, ConceptLayout.DetailTitleHeight);
            body = CreateLayoutTMP("拾荒分多个场景，每个地点掉落不同。选动作后横幅显示当前场景。", detail, font,
                ConceptLayout.DetailBodyFont, UITheme.Muted, TextAlignmentOptions.TopLeft, ConceptLayout.DetailBodyHeight);
            body.enableWordWrapping = true;
            body.lineSpacing = ConceptLayout.DetailBodyLineSpacing;
            var bodyLE = body.gameObject.GetComponent<LayoutElement>();
            if (bodyLE != null && ConceptLayout.DetailBodyFlexibleHeight >= 0f)
                bodyLE.flexibleHeight = ConceptLayout.DetailBodyFlexibleHeight;

            CreateLayoutTMP("✓ 拾荒 Lv.1", detail, font, ConceptLayout.DetailReqFont, UITheme.Teal,
                TextAlignmentOptions.Left, ConceptLayout.DetailReqLineHeight);
            CreateLayoutTMP("场景：村口、街道、谷仓后…", detail, font, ConceptLayout.DetailReqFont, UITheme.Teal,
                TextAlignmentOptions.Left, ConceptLayout.DetailReqLineHeight);
            CreateLayoutTMP("掉落：按动作独立概率", detail, font, ConceptLayout.DetailReqFont, UITheme.Muted,
                TextAlignmentOptions.Left, ConceptLayout.DetailReqLineHeight);
        }

        private static InventoryPanelView CreateInventoryPanel(Transform canvas, TMP_FontAsset font)
        {
            var overlay = CreateRect("InventoryOverlay", canvas);
            Stretch(overlay);
            overlay.SetAsLastSibling();

            var backdrop = CreateRect("Backdrop", overlay);
            Stretch(backdrop);
            var backdropImg = backdrop.gameObject.AddComponent<Image>();
            backdropImg.color = new Color(0f, 0f, 0f, 0.55f);
            var backdropBtn = backdrop.gameObject.AddComponent<Button>();
            backdropBtn.transition = Selectable.Transition.None;

            var panel = CreateRect("Panel", overlay);
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = ConceptLayout.InvPanelSize;
            var panelImg = panel.gameObject.AddComponent<Image>();
            panelImg.color = UITheme.Panel;
            StyleOutline(panelImg, UITheme.Border, new Vector2(1, -1));

            var vlg = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            ConfigureLayoutGroup(vlg, expandWidth: true, expandHeight: false);
            var panelPad = Mathf.RoundToInt(ConceptLayout.InvPanelPadding);
            vlg.padding = new RectOffset(panelPad, panelPad, panelPad, panelPad);
            vlg.spacing = ConceptLayout.InvPanelGap;

            var header = CreateRect("Header", panel);
            LockLayoutHeight(header, ConceptLayout.InvPanelHeaderHeight);

            var headerHLG = header.gameObject.AddComponent<HorizontalLayoutGroup>();
            ConfigureLayoutGroup(headerHLG, expandWidth: true, expandHeight: false);
            headerHLG.childAlignment = TextAnchor.MiddleLeft;

            var titleLineHeight = Mathf.RoundToInt(ConceptLayout.InvPanelTitleFont * 1.35f);
            var title = CreateLayoutTMP("背包", header, font, ConceptLayout.InvPanelTitleFont, UITheme.Cream,
                TextAlignmentOptions.Left, titleLineHeight);
            title.fontStyle = FontStyles.Bold;
            title.gameObject.GetComponent<LayoutElement>().flexibleWidth = 1;
            LockLayoutHeight(title.rectTransform, titleLineHeight);

            var closeRt = CreateRect("Btn_Close", header);
            LockLayoutHeight(closeRt, ConceptLayout.InvPanelCloseSize);
            var closeLe = closeRt.gameObject.GetComponent<LayoutElement>();
            if (closeLe != null)
            {
                closeLe.preferredWidth = ConceptLayout.InvPanelCloseSize;
                closeLe.minWidth = ConceptLayout.InvPanelCloseSize;
                closeLe.flexibleWidth = 0;
            }
            var closeImg = closeRt.gameObject.AddComponent<Image>();
            closeImg.color = UITheme.PanelLight;
            StyleOutline(closeImg, UITheme.Border, new Vector2(1, -1));
            var closeBtn = closeRt.gameObject.AddComponent<Button>();
            closeBtn.targetGraphic = closeImg;
            ConfigureButton(closeBtn, UITheme.PanelLight, UITheme.CardHover, UITheme.ButtonPressed);
            CreateTMP("×", closeRt, font, 22, UITheme.Cream, TextAlignmentOptions.Center);

            var body = CreateRect("Body", panel);
            body.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
            var bodyBg = body.gameObject.AddComponent<Image>();
            bodyBg.color = UITheme.InventoryBg;
            StyleOutline(bodyBg, UITheme.BorderSubtle, new Vector2(1, -1));

            var scrollRt = CreateRect("Scroll", body);
            Stretch(scrollRt);
            var scroll = scrollRt.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            var viewport = CreateRect("Viewport", scrollRt);
            Stretch(viewport);
            var viewportImg = viewport.gameObject.AddComponent<Image>();
            viewportImg.color = Color.white;
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;

            var content = CreateRect("Content", viewport);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0, 0);

            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(ConceptLayout.InvPanelSlotWidth, ConceptLayout.InvPanelSlotHeight);
            grid.spacing = new Vector2(ConceptLayout.InvPanelSlotGap, ConceptLayout.InvPanelSlotGap);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.childAlignment = TextAnchor.UpperLeft;

            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport;
            scroll.content = content;

            var empty = CreateLayoutTMP("暂无物品", body, font, ConceptLayout.DetailBodyFont, UITheme.Muted,
                TextAlignmentOptions.Center, 24);
            Stretch(empty.rectTransform);
            empty.gameObject.SetActive(false);

            var gridView = body.gameObject.AddComponent<InventoryGridView>();
            gridView.Configure(content, font, empty);

            overlay.gameObject.SetActive(false);

            var view = overlay.gameObject.AddComponent<InventoryPanelView>();
            view.Configure(overlay.gameObject, gridView, closeBtn, backdropBtn);
            return view;
        }
    }
}
#endif
