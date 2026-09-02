using UnityEngine;

namespace UniverIdle.UI
{
    /// <summary>运行时 UI 脚本引用的配色（场景里手调的颜色不必与此一致）。</summary>
    public static class UITheme
    {
        public static readonly Color Panel = Hex("#24302A");
        public static readonly Color PanelLight = Hex("#2F3C36");
        public static readonly Color CardHover = Hex("#3A4842");
        public static readonly Color Border = Hex("#4A5C54");
        public static readonly Color BorderSubtle = Hex("#35433C");
        /// <summary>几乎不可见但可接收射线，用于透明 Button 底图。</summary>
        public static readonly Color ClickableClear = new Color(1f, 1f, 1f, 0.004f);

        public static readonly Color Teal = Hex("#66B8A8");
        public static readonly Color TealBright = Hex("#84D4C4");
        public static readonly Color Accent = Hex("#E88860");
        public static readonly Color Gold = Hex("#E8C060");

        public static readonly Color Cream = Hex("#F5F0E6");
        public static readonly Color Text = Hex("#ECE9E2");
        public static readonly Color Muted = Hex("#90A098");

        public static readonly Color TagText = Hex("#C8EBE2");
        public static readonly Color TagBg = new Color(0.4f, 0.72f, 0.66f, 0.28f);

        public static Color Hex(string hex)
        {
            if (!hex.StartsWith("#")) hex = "#" + hex;
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }
    }
}
