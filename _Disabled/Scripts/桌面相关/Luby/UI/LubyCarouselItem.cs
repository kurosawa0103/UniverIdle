using UnityEngine;
using UnityEngine.UI;

namespace DesktopPet.Luby
{
    /// <summary>左侧轮播中的单个模板槽。</summary>
    public sealed class LubyCarouselItem : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private Image selectionHighlight;
        [SerializeField] private Button button;

        public void Bind(LubyTemplateDefinition template, Sprite fallbackIcon, bool selected)
        {
            Sprite icon = LubyPrefabIcon.Resolve(template, fallbackIcon);
            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
                iconImage.preserveAspect = true;
            }

            SetSelected(selected);
        }

        public void SetSelected(bool selected)
        {
            if (selectionHighlight != null)
                selectionHighlight.enabled = selected;
        }

        public void WireClick(System.Action<LubyCarouselItem> onClick)
        {
            if (button == null)
                button = GetComponent<Button>();
            if (button == null)
                return;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke(this));
        }
    }
}
