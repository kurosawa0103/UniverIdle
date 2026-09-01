using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DesktopPet.Luby
{
    /// <summary>图鉴格子：已解锁显示图标，未解锁灰影。</summary>
    public sealed class CodexAppearanceSlot : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image selectionHighlight;
        [SerializeField] private Image lockOverlay;

        private LubyAppearanceCodexEntry _entry;
        private bool _unlocked;

        public LubyAppearanceCodexEntry Entry => _entry;
        public bool IsUnlocked => _unlocked;
        public event Action<CodexAppearanceSlot> Clicked;

        public void Bind(LubyAppearanceCodexEntry entry, bool unlocked, Sprite icon, Sprite fallback)
        {
            _entry = entry;
            _unlocked = unlocked;

            Sprite sprite = icon != null ? icon : fallback;
            if (iconImage != null)
            {
                iconImage.sprite = sprite;
                iconImage.enabled = sprite != null;
                iconImage.color = unlocked
                    ? Color.white
                    : new Color(0.15f, 0.15f, 0.18f, 0.85f);
                iconImage.preserveAspect = true;
            }

            if (lockOverlay != null)
                lockOverlay.enabled = !unlocked;

            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            if (selectionHighlight != null)
                selectionHighlight.enabled = selected;
            else if (backgroundImage != null)
                backgroundImage.color = selected
                    ? new Color(0.40f, 0.55f, 0.72f, 1f)
                    : new Color(0.30f, 0.33f, 0.40f, 1f);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;
            Clicked?.Invoke(this);
        }
    }
}
