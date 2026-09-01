#if UNITY_EDITOR
using TMPro;
using UniverIdle.UI;
using UnityEngine;
using UnityEngine.UI;

namespace UniverIdle.Editor
{
    public static partial class MainUISetup
    {
        private static Image CreateFilledBar(RectTransform parent, float height, Color trackColor, Color fillColor,
            float fillAmount, string trackName = "BarBg", string fillName = "BarFill")
        {
            var barBg = CreateColorBlock(trackName, parent, trackColor, new Vector2(0, height));
            var barBgLE = barBg.gameObject.AddComponent<LayoutElement>();
            barBgLE.preferredHeight = height;
            barBgLE.flexibleWidth = 1;

            var barFill = CreateColorBlock(fillName, barBg.rectTransform, fillColor, new Vector2(0, height));
            barFill.type = Image.Type.Filled;
            barFill.fillMethod = Image.FillMethod.Horizontal;
            barFill.fillAmount = fillAmount;
            Stretch(barFill.rectTransform);
            return barFill;
        }

        private static void StyleOutline(Graphic graphic, Color color, Vector2 distance)
        {
            var outline = graphic.gameObject.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
            outline.useGraphicAlpha = true;
        }

        private static void ConfigureButton(Button button, Color normal, Color highlighted, Color pressed)
        {
            var colors = button.colors;
            colors.normalColor = normal;
            colors.highlightedColor = highlighted;
            colors.pressedColor = pressed;
            colors.selectedColor = highlighted;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
        }

        private static Image CreateDivider(RectTransform parent, bool vertical, float thickness = 1f)
        {
            var rt = CreateRect(vertical ? "VDivider" : "HDivider", parent);
            var le = rt.gameObject.AddComponent<LayoutElement>();
            if (vertical)
            {
                le.preferredWidth = thickness;
                le.flexibleHeight = 1;
            }
            else
            {
                le.preferredHeight = thickness;
                le.flexibleWidth = 1;
            }

            var img = rt.gameObject.AddComponent<Image>();
            img.color = UITheme.Border;
            img.raycastTarget = false;
            return img;
        }

        private static void AttachTopGradient(RectTransform panel)
        {
            var strip = CreateRect("TopGradient", panel);
            strip.anchorMin = new Vector2(0, 0.5f);
            strip.anchorMax = Vector2.one;
            strip.offsetMin = Vector2.zero;
            strip.offsetMax = Vector2.zero;
            var img = strip.gameObject.AddComponent<Image>();
            img.color = UITheme.TopBarTop;
            img.raycastTarget = false;
        }

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

        private static Image CreateColorBlock(string name, RectTransform parent, Color color, Vector2 size)
        {
            var rt = CreateRect(name, parent);
            if (size.x > 0) rt.sizeDelta = size;
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
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

        /// <summary>LayoutGroup 子项用：顶对齐 + 固定首选高度，避免 Stretch 锚点撑乱排版。</summary>
        private static TextMeshProUGUI CreateLayoutTMP(string text, RectTransform parent, TMP_FontAsset font,
            float size, Color color, TextAlignmentOptions align, float preferredHeight = 0)
        {
            if (preferredHeight <= 0f)
                preferredHeight = Mathf.Ceil(size * 1.35f);

            var rt = CreateRect("Text", parent);
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.sizeDelta = new Vector2(0, preferredHeight);

            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = preferredHeight;

            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.font = font;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Overflow;
            return tmp;
        }

        private static void ConfigureLayoutGroup(HorizontalOrVerticalLayoutGroup group, bool expandWidth, bool expandHeight)
        {
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = expandWidth;
            group.childForceExpandHeight = expandHeight;
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
    }
}
#endif
