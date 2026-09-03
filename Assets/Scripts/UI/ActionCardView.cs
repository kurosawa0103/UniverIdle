using TMPro;
using UniverIdle.Game;
using UnityEngine;
using UnityEngine.UI;

namespace UniverIdle.UI
{
    public class ActionCardView : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private Outline border;
        [SerializeField] private Image thumb;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI metaLeftText;
        [SerializeField] private TextMeshProUGUI metaRightText;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image masteryIcon;
        [SerializeField] private TextMeshProUGUI masteryLevelText;

        private bool _masteryReady;
        private bool _layoutMasteryAtRuntime;

        public void Bind(string displayTitle, string metaLeft, string metaRight, bool locked,
            Sprite thumbSprite, int masteryLevel = 1, Sprite masterySprite = null)
        {
            if (titleText != null) titleText.text = displayTitle;
            if (metaLeftText != null) metaLeftText.text = metaLeft;
            if (metaRightText != null) metaRightText.text = metaRight;
            if (thumb != null)
            {
                if (thumbSprite != null)
                {
                    thumb.sprite = thumbSprite;
                    thumb.color = locked ? Color.black : Color.white;
                    thumb.preserveAspect = true;
                }
                else
                {
                    thumb.sprite = null;
                    thumb.color = locked ? Color.black : UITheme.PanelLight;
                }
            }

            if (canvasGroup != null)
                canvasGroup.interactable = !locked;

            var button = GetComponent<Button>();
            if (button != null)
                button.interactable = !locked;

            BindMastery(masteryLevel, masterySprite);
        }

        public void BindMastery(int level, Sprite icon)
        {
            EnsureMastery();
            if (masteryLevelText != null)
                masteryLevelText.text = $"Lv.{Mathf.Max(1, level)}";

            if (masteryIcon == null) return;
            if (icon != null)
            {
                masteryIcon.enabled = true;
                masteryIcon.sprite = icon;
                masteryIcon.color = Color.white;
                masteryIcon.preserveAspect = true;
            }
            else
            {
                masteryIcon.sprite = null;
                masteryIcon.color = UITheme.Muted;
            }
        }

        public static Sprite ResolveMasteryIcon(WorkActionDefinition action)
        {
            if (action?.LootTable != null)
            {
                for (var i = 0; i < action.LootTable.Count; i++)
                {
                    var itemId = action.LootTable[i].ItemId;
                    if (LootRules.IsEmpty(itemId)) continue;
                    var sprite = ItemIconLoader.Get(GameContent.GetItem(itemId));
                    if (sprite != null) return sprite;
                }
            }

            return ActionImageLoader.Get(action);
        }

        public void SetSelected(bool selected)
        {
            if (background != null)
                background.color = selected ? UITheme.CardHover : UITheme.Panel;
            if (border != null)
                border.effectColor = selected ? UITheme.Accent : UITheme.BorderSubtle;
        }

        private void EnsureMastery()
        {
            if (_masteryReady) return;
            _masteryReady = true;

            if (masteryLevelText != null && masteryLevelText.gameObject.name == "CD")
                masteryLevelText = null;

            if (masteryLevelText == null)
            {
                var existing = transform.Find("MasteryLevel");
                if (existing != null)
                    masteryLevelText = existing.GetComponent<TextMeshProUGUI>();
                else
                {
                    masteryLevelText = CreateMasteryLevelText();
                    _layoutMasteryAtRuntime = true;
                }
            }

            if (masteryIcon == null)
            {
                var existing = transform.Find("MasteryIcon");
                if (existing != null)
                    masteryIcon = existing.GetComponent<Image>();
                else
                {
                    masteryIcon = CreateMasteryIcon();
                    _layoutMasteryAtRuntime = true;
                }
            }

            if (_layoutMasteryAtRuntime)
                LayoutMasteryNextToCd();
        }

        private void LayoutMasteryNextToCd()
        {
            var cd = transform.Find("CD") as RectTransform;
            var iconRt = masteryIcon != null ? masteryIcon.rectTransform : null;
            var levelRt = masteryLevelText != null ? masteryLevelText.rectTransform : null;
            if (cd == null || iconRt == null || levelRt == null) return;

            var cdRight = cd.anchoredPosition.x + cd.sizeDelta.x * (1f - cd.pivot.x);

            iconRt.anchorMin = cd.anchorMin;
            iconRt.anchorMax = cd.anchorMax;
            iconRt.pivot = new Vector2(0f, 1f);
            iconRt.sizeDelta = new Vector2(16f, 16f);
            iconRt.anchoredPosition = new Vector2(cdRight + 6f, cd.anchoredPosition.y);

            levelRt.anchorMin = cd.anchorMin;
            levelRt.anchorMax = cd.anchorMax;
            levelRt.pivot = new Vector2(0f, 1f);
            levelRt.sizeDelta = new Vector2(44f, 18f);
            levelRt.anchoredPosition = new Vector2(cdRight + 24f, cd.anchoredPosition.y);
        }

        private Image CreateMasteryIcon()
        {
            var go = new GameObject("MasteryIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(transform, false);
            rt.localScale = Vector3.one;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(8f, -8f);
            rt.sizeDelta = new Vector2(16f, 16f);

            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            return image;
        }

        private TextMeshProUGUI CreateMasteryLevelText()
        {
            var go = new GameObject("MasteryLevel", typeof(RectTransform), typeof(CanvasRenderer));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(transform, false);
            rt.localScale = Vector3.one;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(28f, -8f);
            rt.sizeDelta = new Vector2(44f, 18f);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            var template = metaLeftText != null ? metaLeftText : transform.Find("CD")?.GetComponent<TextMeshProUGUI>();
            if (template != null)
            {
                tmp.font = template.font;
                tmp.fontSharedMaterial = template.fontSharedMaterial;
                tmp.fontSize = template.fontSize;
                tmp.color = template.color;
            }
            else
            {
                tmp.fontSize = 12f;
                tmp.color = UITheme.Muted;
            }

            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.enableWordWrapping = false;
            tmp.raycastTarget = false;
            return tmp;
        }
    }
}
