using UnityEngine;

namespace UniverIdle.UI
{
    /// <summary>主界面配色：坠星谷 · 夜溪微光（暖金 + 萤光青 + 深林夜底）。</summary>
    public static class UITheme
    {
        // 底色层次
        public static readonly Color Background = Hex("#121816");
        public static readonly Color SidebarBg = Hex("#222A24");
        public static readonly Color InventoryBg = Hex("#1C221E");
        public static readonly Color Panel = Hex("#24302A");
        public static readonly Color PanelLight = Hex("#2F3C36");
        public static readonly Color PanelSelected = Hex("#36443E");
        public static readonly Color CardHover = Hex("#3A4842");
        public static readonly Color Border = Hex("#4A5C54");
        public static readonly Color BorderSubtle = Hex("#35433C");
        public static readonly Color Transparent = new Color(0, 0, 0, 0);
        /// <summary>几乎不可见但可接收射线，用于透明 Button 底图。</summary>
        public static readonly Color ClickableClear = new Color(1f, 1f, 1f, 0.004f);

        // 顶栏
        public static readonly Color TopBarTop = Hex("#2E3832");
        public static readonly Color TopBarBottom = Hex("#252D28");
        public static readonly Color LogoBg = Hex("#3D5A50");

        // 强调色
        public static readonly Color Gold = Hex("#F0C96E");
        public static readonly Color Teal = Hex("#66B8A8");
        public static readonly Color TealBright = Hex("#84D4C4");
        public static readonly Color Accent = Hex("#E88860");
        public static readonly Color AccentSoft = Hex("#D07050");

        // 文字
        public static readonly Color Cream = Hex("#F5F0E6");
        public static readonly Color Text = Hex("#ECE9E2");
        public static readonly Color Muted = Hex("#90A098");

        // 进度条
        public static readonly Color BarTrack = Hex("#0E1412");
        public static readonly Color ProgressFill = Hex("#E8A858");
        public static readonly Color XpFill = Hex("#66B8A8");

        // 地点横幅
        public static readonly Color BannerBg = Hex("#1A2838");
        public static readonly Color BannerMid = Hex("#2A4E58");
        public static readonly Color BannerAccent = Hex("#1A2824");
        public static readonly Color StarLight = Hex("#B8DCFF");
        public static readonly Color StarWarm = Hex("#F0DCA0");

        // 标签
        public static readonly Color TagText = Hex("#C8EBE2");
        public static readonly Color TagBg = new Color(0.4f, 0.72f, 0.66f, 0.28f);

        // 技能图标底色
        public static readonly Color SkillHunt = Hex("#4A3428");
        public static readonly Color SkillWood = Hex("#324030");
        public static readonly Color SkillFish = Hex("#2A4A58");
        public static readonly Color SkillForage = Hex("#2A4838");
        public static readonly Color SkillMine = Hex("#403830");
        public static readonly Color SkillAlchemy = Hex("#442E36");
        public static readonly Color SkillSmith = Hex("#383630");
        public static readonly Color SkillCombat = Hex("#442828");

        // 动作卡缩略图 / 物品
        public static readonly Color ThumbFish = Hex("#2E5850");
        public static readonly Color ThumbSand = Hex("#504838");
        public static readonly Color ThumbLocked = Hex("#3A5858");
        public static readonly Color RunningThumb = Hex("#2A3834");
        public static readonly Color ItemShrimp = Hex("#E88860");
        public static readonly Color ItemHerb = Hex("#66B8A8");
        public static readonly Color ItemPotion = Hex("#D85858");
        public static readonly Color ItemOre = Hex("#B89468");

        public static readonly Color ButtonPressed = Hex("#4A5852");

        public static Color Hex(string hex)
        {
            if (!hex.StartsWith("#")) hex = "#" + hex;
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }
    }
}
