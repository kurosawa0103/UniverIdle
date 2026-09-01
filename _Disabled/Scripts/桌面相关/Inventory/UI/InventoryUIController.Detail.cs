using DesktopPet.Decor;
using DesktopPet.Luby;
using DesktopPet.Shop;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DesktopPet.Inventory
{
    public sealed partial class InventoryUIController
    {
        private void SelectSlot(InventorySlot slot)
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i] != null)
                    _slots[i].SetSelected(_slots[i] == slot);
            }

            if (slot != null && slot.IsLubySlot)
            {
                _selected = null;
                _selectedLuby = slot.LubyData;
            }
            else
            {
                _selectedLuby = null;
                _selected = slot != null ? slot.Item : null;
            }

            RefreshDetail();
        }

        private void RefreshDetail()
        {
            EnsureWorldRefs();
            if (_subTab == InvSubTab.Luby)
            {
                RefreshLubyDetail();
                return;
            }

            if (_selected == null)
            {
                ClearDetail();
                return;
            }

            if (detailIcon != null)
            {
                detailIcon.sprite = _selected.icon;
                detailIcon.enabled = _selected.icon != null;
                detailIcon.preserveAspect = true;
                detailIcon.type = Image.Type.Simple;
                detailIcon.color = Color.white;
            }

            if (detailNameText != null)
                detailNameText.text = _selected.displayName;

            if (detailDescText != null)
            {
                detailDescText.text = string.IsNullOrEmpty(_selected.description)
                    ? "暂无描述"
                    : _selected.description;
            }

            int count = inventory != null ? inventory.GetCount(_selected.itemId) : 0;
            bool canPlace = _selected.tab == ShopTabId.Decor && count > 0;
            bool deskFull = _decorWorld != null && !_decorWorld.CanPlaceOnDesk;

            if (actionButtonText != null)
            {
                if (_selected.tab != ShopTabId.Decor)
                    actionButtonText.text = "不可放置";
                else if (deskFull)
                    actionButtonText.text = "装饰已满";
                else if (_selected.placementPrefab == null)
                    actionButtonText.text = "放置（占位）";
                else
                    actionButtonText.text = "放置";
            }

            if (actionButton != null)
                actionButton.interactable = canPlace && !deskFull;
        }

        private void RefreshLubyDetail()
        {
            if (_selectedLuby == null)
            {
                ClearDetail();
                return;
            }

            LubyWorld world = _lubyWorld;
            LubyTemplateDefinition template = world != null
                ? world.Catalog?.FindTemplateById(_selectedLuby.templateId)
                : null;
            Sprite icon = LubyPrefabIcon.Resolve(template, null);

            if (detailIcon != null)
            {
                detailIcon.sprite = icon;
                detailIcon.enabled = icon != null;
                detailIcon.preserveAspect = true;
                detailIcon.type = Image.Type.Simple;
                detailIcon.color = Color.white;
            }

            string name = LubyDisplayNames.ResolvePetName(_selectedLuby, world?.Catalog);
            if (detailNameText != null)
                detailNameText.text = name;

            string pName = "—";
            string tName = "—";
            if (world?.Catalog != null)
            {
                LubyPersonalityDefinition p = world.Catalog.FindPersonalityById(_selectedLuby.personalityId);
                if (p != null) pName = p.displayName;
                tName = LubyTraitDisplay.FormatNames(world.Catalog, _selectedLuby);
            }

            if (detailDescText != null)
            {
                LubyJournalService.MaybeIdleThought(_selectedLuby);
                string journal = LubyJournalService.FormatSummary(_selectedLuby, world?.Catalog);
                string core = $"性格：{pName}\n特质：{tName}";
                detailDescText.text = string.IsNullOrEmpty(journal)
                    ? core
                    : core + "\n\n" + journal;
            }

            bool deskFull = world != null && !world.CanSpawnOnDesk;
            if (actionButtonText != null)
                actionButtonText.text = deskFull ? "桌上已满" : "放置";
            if (actionButton != null)
                actionButton.interactable = !deskFull;
        }

        private void ClearDetail()
        {
            _selected = null;
            _selectedLuby = null;
            if (detailIcon != null)
            {
                detailIcon.sprite = null;
                detailIcon.enabled = false;
            }

            if (detailNameText != null)
                detailNameText.text = string.Empty;
            if (detailDescText != null)
                detailDescText.text = "点选左侧物品预览";
            if (actionButtonText != null)
                actionButtonText.text = "放置";
            if (actionButton != null)
                actionButton.interactable = false;
        }

        private void OnActionClicked()
        {
            if (_subTab == InvSubTab.Luby)
            {
                OnLubyPlaceClicked();
                return;
            }

            if (_selected == null || _selected.tab != ShopTabId.Decor)
                return;

            DecorPlacementSystem placement = DesktopPetServices.Placement;
            if (placement == null)
            {
                SetStatus("无法放置：缺少 DecorPlacementSystem");
                return;
            }

            if (placement.IsHolding)
            {
                SetStatus("已在放置中，左键点桌面或右键取消");
                return;
            }

            if (DesktopPetServices.IsAnyPlacementHolding() && !placement.IsHolding)
            {
                SetStatus("正在放置 Luby");
                return;
            }

            DecorWorld decorWorld = _decorWorld;
            if (decorWorld != null && !decorWorld.CanPlaceOnDesk)
            {
                SetStatus($"装饰已满（{decorWorld.Count}/{decorWorld.DeskCapacity}）");
                return;
            }

            string displayName = _selected.displayName;
            if (placement.TryBeginFromInventory(_selected))
                SetStatus($"放置中：{displayName}（左键放下 / 右键取消）");
            else
                SetStatus("放置失败");
        }

        private void OnLubyPlaceClicked()
        {
            if (_selectedLuby == null)
                return;

            LubyPlacementSystem lubyPlace = DesktopPetServices.LubyPlacement;
            if (lubyPlace == null)
            {
                SetStatus("无法放置：缺少 LubyPlacementSystem");
                return;
            }

            if (DesktopPetServices.IsAnyPlacementHolding())
            {
                SetStatus("已在放置中");
                return;
            }

            LubyWorld world = _lubyWorld;
            if (world != null && !world.CanSpawnOnDesk)
            {
                SetStatus($"桌上已满（{world.OccupiedDeskSlots}/{world.DeskCapacity}）");
                return;
            }

            if (lubyPlace.TryBeginFromWarehouse(_selectedLuby))
                SetStatus("放置 Luby：左键贴地放下 / 右键取消");
            else
                SetStatus("放置失败");
        }
    }
}
