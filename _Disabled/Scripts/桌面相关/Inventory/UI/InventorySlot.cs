using System;
using DesktopPet.Luby;
using DesktopPet.Shop;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DesktopPet.Inventory
{
    /// <summary>仓库格子：装饰或 Luby；点击选中。</summary>
    public sealed class InventorySlot : MonoBehaviour, IPointerClickHandler
    {
        [Title("仓库槽", "图标 + 数量；点击选中")]
        [BoxGroup("数据")]
        [SerializeField]
        private ShopItemDefinition item;

        [BoxGroup("数据")]
        [SerializeField]
        private ItemInventory inventory;

        [BoxGroup("UI 绑定")]
        [LabelText("数量文本")]
        [SerializeField]
        private TextMeshProUGUI countText;

        [BoxGroup("UI 绑定")]
        [LabelText("图标")]
        [SerializeField]
        private Image iconImage;

        [BoxGroup("UI 绑定")]
        [LabelText("底图")]
        [SerializeField]
        private Image backgroundImage;

        [BoxGroup("UI 绑定")]
        [LabelText("选中高亮")]
        [SerializeField]
        private Image selectionHighlight;

        private LubyInstanceData _lubyData;

        public ShopItemDefinition Item => item;
        public LubyInstanceData LubyData => _lubyData;
        public bool IsLubySlot => _lubyData != null;

        public event Action<InventorySlot> Clicked;

        public void Bind(ShopItemDefinition definition, ItemInventory inv)
        {
            _lubyData = null;
            item = definition;
            inventory = inv;
            ApplyStaticLabels();
            RefreshView();
            SetSelected(false);
        }

        public void BindLuby(LubyInstanceData data, Sprite icon, string titleHint = null)
        {
            item = null;
            inventory = null;
            _lubyData = data;
            gameObject.SetActive(data != null);
            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
                iconImage.color = icon != null ? Color.white : new Color(0.28f, 0.32f, 0.28f, 1f);
                iconImage.preserveAspect = true;
            }

            if (countText != null)
                countText.text = string.IsNullOrEmpty(titleHint) ? string.Empty : titleHint;

            SetSelected(false);
        }

        private void ApplyStaticLabels()
        {
            if (item == null || iconImage == null)
                return;

            iconImage.sprite = item.icon;
            iconImage.enabled = item.icon != null;
            iconImage.color = item.icon != null ? Color.white : new Color(0.28f, 0.32f, 0.28f, 1f);
            iconImage.preserveAspect = true;
        }

        private void RefreshView()
        {
            if (_lubyData != null)
            {
                gameObject.SetActive(true);
                return;
            }

            if (item == null)
            {
                gameObject.SetActive(false);
                return;
            }

            int count = inventory != null ? inventory.GetCount(item.itemId) : 0;
            gameObject.SetActive(count > 0);
            if (countText != null)
                countText.text = count > 1 ? $"x{count}" : string.Empty;
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
