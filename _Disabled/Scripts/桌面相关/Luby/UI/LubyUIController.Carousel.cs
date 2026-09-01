using UnityEngine;
using UnityEngine.UI;

namespace DesktopPet.Luby
{
    public sealed partial class LubyUIController
    {
        private void RebuildTemplateList()
        {
            _templates.Clear();
            if (catalog?.templates == null)
                return;

            for (int i = 0; i < catalog.templates.Count; i++)
            {
                LubyTemplateDefinition t = catalog.templates[i];
                if (t != null)
                    _templates.Add(t);
            }

            if (_selectedIndex >= _templates.Count)
                _selectedIndex = Mathf.Max(0, _templates.Count - 1);

            RebuildCarousel();
        }

        private void RebuildCarousel()
        {
            if (carouselRoot == null)
                return;

            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i] != null)
                    Destroy(_items[i].gameObject);
            }

            _items.Clear();

            if (carouselItemPrefab == null)
                return;

            for (int i = 0; i < _templates.Count; i++)
            {
                LubyCarouselItem item = Instantiate(carouselItemPrefab, carouselRoot);
                item.gameObject.SetActive(true);
                item.Bind(_templates[i], fallbackIcon, i == _selectedIndex);
                int index = i;
                item.WireClick(_ => SelectIndex(index));
                _items.Add(item);
            }
        }

        private void SelectIndex(int index)
        {
            if (_templates.Count == 0)
                return;

            _selectedIndex = Mathf.Clamp(index, 0, _templates.Count - 1);
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i] != null)
                    _items[i].SetSelected(i == _selectedIndex);
            }

            RefreshDetail();
            RefreshRollButton();
        }

        private void SelectPrev() => SelectIndex(_selectedIndex - 1);
        private void SelectNext() => SelectIndex(_selectedIndex + 1);

        private void RefreshAll()
        {
            RefreshDetail();
            RefreshRollButton();
        }
    }
}
