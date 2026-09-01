using UnityEngine;
using UnityEngine.UI;

namespace DesktopPet.Luby
{
    public sealed partial class LubyUIController
    {
        private void RefreshDetail()
        {
            LubyTemplateDefinition selected = GetSelectedTemplate();
            if (detailRoot != null)
                detailRoot.SetActive(true);

            if (selected == null)
            {
                if (detailNameText != null)
                    detailNameText.text = "—";
                if (detailDescText != null)
                    detailDescText.text = "暂无模板";
                if (detailIcon != null)
                    detailIcon.enabled = false;
                return;
            }

            if (detailNameText != null)
                detailNameText.text = selected.displayName;
            if (detailDescText != null)
            {
                string desc = string.IsNullOrEmpty(selected.description)
                    ? "长按：权重抽外形 → 权重抽性格/特质"
                    : selected.description;
                detailDescText.text = desc;
            }

            Sprite icon = LubyPrefabIcon.Resolve(selected, fallbackIcon);
            if (detailIcon != null)
            {
                detailIcon.sprite = icon;
                detailIcon.enabled = icon != null;
                detailIcon.preserveAspect = true;
                detailIcon.type = Image.Type.Simple;
            }
        }

        private void RefreshRollButton()
        {
            LubyTemplateDefinition selected = GetSelectedTemplate();
            int price = selected != null ? Mathf.Max(0, selected.rollPrice) : 0;
            if (rollPriceText != null)
                rollPriceText.text = selected != null ? price.ToString() : "—";

            if (rollButton != null)
                rollButton.interactable = _templates.Count > 0;
        }

        private LubyTemplateDefinition GetSelectedTemplate()
        {
            if (_templates.Count == 0)
                return null;
            return _templates[Mathf.Clamp(_selectedIndex, 0, _templates.Count - 1)];
        }
    }
}
