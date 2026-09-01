using UnityEngine;

namespace UniverIdle.UI
{
    /// <summary>主界面配色，与 docs/设计/概念图/主界面-概念.html 一致。</summary>
    public static class UITheme
    {
        public static readonly Color Background = Hex("#1E2420");
        public static readonly Color Panel = Hex("#2A322C");
        public static readonly Color PanelLight = Hex("#354038");
        public static readonly Color CardHover = Hex("#3D4A42");
        public static readonly Color Border = Hex("#4A5A50");
        public static readonly Color Gold = Hex("#E8B84A");
        public static readonly Color Teal = Hex("#5A9A8A");
        public static readonly Color Cream = Hex("#F0EBE0");
        public static readonly Color Text = Hex("#E8E4DC");
        public static readonly Color Muted = Hex("#9AA89E");
        public static readonly Color Accent = Hex("#C87840");
        public static readonly Color SidebarBg = Hex("#222A24");
        public static readonly Color TopBarTop = Hex("#2E3832");
        public static readonly Color TopBarBottom = Hex("#252D28");
        public static readonly Color InventoryBg = Hex("#1C221E");
        public static readonly Color BarTrack = Hex("#1A201C");
        public static readonly Color BannerBg = Hex("#1A3040");
        public static readonly Color BannerMid = Hex("#2A5048");
        public static readonly Color BannerAccent = Hex("#1A2820");
        public static readonly Color TagText = Hex("#B8E0D4");
        public static readonly Color TagBg = new Color(0.353f, 0.604f, 0.541f, 0.35f);
        public static readonly Color Transparent = new Color(0, 0, 0, 0);

        public static Color Hex(string hex)
        {
            if (!hex.StartsWith("#")) hex = "#" + hex;
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }
    }
}
