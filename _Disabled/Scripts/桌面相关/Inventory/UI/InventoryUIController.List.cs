using System.Collections.Generic;
using DesktopPet.Luby;
using DesktopPet.Shop;
using UnityEngine;

namespace DesktopPet.Inventory
{
    public sealed partial class InventoryUIController
    {
        private ShopCatalog Catalog =>
            DesktopPetServices.Shop != null ? DesktopPetServices.Shop.Catalog : null;

        private void RebuildInventoryList()
        {
            _selected = null;
            _selectedLuby = null;

            if (_subTab == InvSubTab.Luby)
            {
                RebuildLubyList();
                return;
            }

            RebuildDecorList();
        }

        private void RebuildDecorList()
        {
            int used = 0;
            InventorySlot first = null;
            ShopCatalog catalog = Catalog;

            if (inventorySlotPrefab != null && inventoryContent != null
                && inventory != null && catalog != null)
            {
                IReadOnlyList<ItemInventory.Entry> entries = inventory.Entries;
                for (int i = 0; i < entries.Count; i++)
                {
                    ItemInventory.Entry e = entries[i];
                    if (e.count <= 0 || string.IsNullOrEmpty(e.itemId))
                        continue;

                    ShopItemDefinition item = catalog.FindById(e.itemId);
                    if (item == null || item.tab != ShopTabId.Decor)
                        continue;

                    InventorySlot slot = GetOrCreateSlot(used);
                    slot.gameObject.name = "Inv_" + item.itemId;
                    slot.gameObject.SetActive(true);
                    slot.Bind(item, inventory);

                    if (slot.gameObject.activeSelf)
                    {
                        if (first == null)
                            first = slot;
                        used++;
                    }
                }
            }
            else if (inventorySlotPrefab == null)
            {
                Debug.LogError("[InventoryUI] 未绑定 inventorySlotPrefab。");
            }

            HideUnusedSlots(used);
            FinishListRebuild(first != null, first);
        }

        private void RebuildLubyList()
        {
            EnsureWorldRefs();
            int used = 0;
            InventorySlot first = null;
            LubyWorld world = _lubyWorld;

            if (inventorySlotPrefab != null && inventoryContent != null && world != null)
            {
                IReadOnlyList<LubyInstanceData> list = world.Warehouse;
                for (int i = 0; i < list.Count; i++)
                {
                    LubyInstanceData data = list[i];
                    if (data == null)
                        continue;
                    if (data.IsOnAdventureTrip)
                        continue;

                    Sprite icon = ResolveLubyIcon(world, data);
                    string title = ResolveLubyTitle(world, data);
                    InventorySlot slot = GetOrCreateSlot(used);
                    slot.gameObject.name = "LubyInv_" + data.instanceId;
                    slot.gameObject.SetActive(true);
                    slot.BindLuby(data, icon, title);
                    if (first == null)
                        first = slot;
                    used++;
                }
            }

            HideUnusedSlots(used);
            FinishListRebuild(first != null, first);
        }

        private InventorySlot GetOrCreateSlot(int index)
        {
            InventorySlot slot;
            if (index < _slots.Count && _slots[index] != null)
            {
                slot = _slots[index];
            }
            else
            {
                slot = Instantiate(inventorySlotPrefab, inventoryContent);
                slot.Clicked += SelectSlot;
                if (index < _slots.Count)
                    _slots[index] = slot;
                else
                    _slots.Add(slot);
            }

            return slot;
        }

        private void HideUnusedSlots(int used)
        {
            for (int i = used; i < _slots.Count; i++)
            {
                if (_slots[i] != null)
                    _slots[i].gameObject.SetActive(false);
            }
        }

        private void FinishListRebuild(bool anyItems, InventorySlot first)
        {
            if (_subTab == InvSubTab.Luby)
            {
                if (_selectedLuby == null && first != null)
                    SelectSlot(first);
                else if (_selectedLuby == null)
                    ClearDetail();
            }
            else
            {
                if (_selected == null && first != null)
                    SelectSlot(first);
                else if (_selected == null)
                    ClearDetail();
            }

            if (inventoryEmptyHint != null)
            {
                inventoryEmptyHint.transform.SetAsLastSibling();
                inventoryEmptyHint.SetActive(!anyItems);
            }
        }

        private static Sprite ResolveLubyIcon(LubyWorld world, LubyInstanceData data)
        {
            if (world == null || data == null)
                return null;
            LubyTemplateDefinition t = world.Catalog?.FindTemplateById(data.templateId);
            return LubyPrefabIcon.Resolve(t, null);
        }

        private static string ResolveLubyTitle(LubyWorld world, LubyInstanceData data)
        {
            if (world == null || data == null)
                return string.Empty;
            string name = LubyDisplayNames.ResolvePetName(data, world.Catalog);
            LubyPersonalityDefinition p = world.Catalog?.FindPersonalityById(data.personalityId);
            if (p != null && !string.IsNullOrEmpty(p.displayName))
                return $"{name} · {p.displayName}";
            return name;
        }

        private void OnInventoryChanged()
        {
            if (IsOpen && _subTab == InvSubTab.Decor)
                RebuildInventoryList();
        }

        private void EnsureSubTabsBound()
        {
            if (subDecorButton != null && subLubyButton != null)
                return;

            Debug.LogError(
                "[InventoryUI] 未绑定仓库子页签 SubTabs/DecorTab、LubyTab。"
                + "请在 MainCanvas.prefab 的 InventoryPage 内手改，再「应用主面板」。");
        }

        private void WireSubTabs()
        {
            if (subDecorButton != null)
            {
                subDecorButton.onClick.RemoveAllListeners();
                subDecorButton.onClick.AddListener(() => SetSubTab(InvSubTab.Decor));
            }

            if (subLubyButton != null)
            {
                subLubyButton.onClick.RemoveAllListeners();
                subLubyButton.onClick.AddListener(() => SetSubTab(InvSubTab.Luby));
            }

            RefreshSubTabVisual();
        }

        private void UnwireSubTabs()
        {
            if (subDecorButton != null)
                subDecorButton.onClick.RemoveAllListeners();
            if (subLubyButton != null)
                subLubyButton.onClick.RemoveAllListeners();
        }

        private void SetSubTab(InvSubTab tab)
        {
            _subTab = tab;
            RefreshSubTabVisual();
            RebuildInventoryList();
            SetStatus(tab == InvSubTab.Luby
                ? "Luby：点选后「放置」到地面"
                : "装饰：点选后「放置」");
        }

        private void RefreshSubTabVisual()
        {
            DesktopPetTabVisual.Apply(subDecorButton, _subTab == InvSubTab.Decor);
            DesktopPetTabVisual.Apply(subLubyButton, _subTab == InvSubTab.Luby);
        }
    }
}
