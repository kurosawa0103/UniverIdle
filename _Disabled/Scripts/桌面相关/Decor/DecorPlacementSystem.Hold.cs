using System.Collections.Generic;
using DesktopPet;
using DesktopPet.Shop;
using UnityEngine;

namespace DesktopPet.Decor
{
    public sealed partial class DecorPlacementSystem
    {
        /// <summary>仓库「放置」入口：扣 1、关面板、跟手；武装后左键点合法位置放下。</summary>
        public bool TryBeginFromInventory(ShopItemDefinition item)
        {
            if (_holding || item == null)
                return false;

            if (inventory == null)
                return false;

            if (DesktopPetServices.IsAnyPlacementHolding())
                return false;

            if (item.tab != ShopTabId.Decor)
                return false;

            if (item.placementPrefab == null)
            {
                Debug.LogError($"[Decor] 商品未配 placementPrefab，无法放置：{item.itemId}", item);
                return false;
            }

            if (inventory.GetCount(item.itemId) <= 0)
                return false;

            if (world == null || !world.CanPlaceOnDesk)
                return false;

            if (!inventory.TryRemove(item.itemId, 1))
                return false;

            _holdMode = HoldMode.FromInventory;
            _inventoryPlaceArmed = false;
            ClearHeldSubtreeState();
            if (!BeginHold(item, returnToInventoryOnCancel: true, closeInventory: true))
            {
                inventory.Add(item.itemId, 1);
                return false;
            }

            return true;
        }

        /// <summary>长按已摆装饰：整棵叠放子树一起拿起；松手放下或收回仓库。</summary>
        public bool TryBeginFromPlaced(PlacedDecor placed)
        {
            if (_holding || placed == null)
                return false;

            if (world == null)
                return false;

            ShopItemDefinition item = placed.Definition;
            if (item == null)
            {
                Debug.LogWarning($"[Decor] 无法拾取：找不到商品定义 {placed.ItemId}");
                return false;
            }

            _holdMode = HoldMode.FromPlaced;
            _inventoryPlaceArmed = true;
            _originPlacedPos = placed.transform.position;
            _originPlacedParentInstanceId = placed.ParentInstanceId;
            _originPlacedInstanceId = placed.InstanceId;

            _heldSubtree = world.DetachSubtree(placed);
            _heldRoot = placed;
            _heldLiveSubtree = true;
            SetSubtreeCollidersEnabled(_heldSubtree, false);

            // 收回按钮改由右键呼出，拿起时不自动显示
            if (!BeginHold(item, returnToInventoryOnCancel: false, closeInventory: false))
            {
                SetSubtreeCollidersEnabled(_heldSubtree, true);
                world.CommitSubtree(
                    placed,
                    _originPlacedPos,
                    _originPlacedParentInstanceId,
                    _heldSubtree);
                ClearHeldSubtreeState(destroyVisualRefsOnly: true);
                _holdMode = HoldMode.None;
                return false;
            }

            return true;
        }

        private bool BeginHold(ShopItemDefinition item, bool returnToInventoryOnCancel, bool closeInventory)
        {
            if (item == null || item.placementPrefab == null)
            {
                Debug.LogError(
                    $"[Decor] BeginHold 失败：缺少 placementPrefab（{item?.itemId ?? "null"}）。",
                    item);
                return false;
            }

            // 头顶「收回」若还开着，会挡住桌面左键确认摆放
            inventoryUi?.HideAllDeskOverlays();
            ClearPendingReturnTarget();

            _heldItem = item;
            _holding = true;
            _returnToInventoryOnCancel = returnToInventoryOnCancel;
            _footprint = DecorPrefabUtil.MeasureFootprint(item);
            CreateHeldAndGhost(item);

            if (closeInventory)
                DesktopPetServices.CloseHub();

            return true;
        }

        public void CancelHold()
        {
            if (!_holding)
                return;

            bool shouldOpenInventory = _returnToInventoryOnCancel;

            bool restoreOriginToPlaced = _holdMode == HoldMode.FromPlaced
                                           && world != null
                                           && _heldItem != null;

            if (_returnToInventoryOnCancel && inventory != null && _heldItem != null)
                inventory.Add(_heldItem.itemId, 1);
            else if (restoreOriginToPlaced)
            {
                if (_heldLiveSubtree && _heldRoot != null && _heldSubtree != null)
                {
                    world.CommitSubtree(
                        _heldRoot,
                        _originPlacedPos,
                        _originPlacedParentInstanceId,
                        _heldSubtree);
                    SetSubtreeCollidersEnabled(_heldSubtree, true);
                    ClearHeldSubtreeState(destroyVisualRefsOnly: true);
                }
                else
                {
                    world.Spawn(
                        _heldItem,
                        _originPlacedPos,
                        _originPlacedParentInstanceId,
                        _originPlacedInstanceId);
                }
            }

            CleanupHold();
            Persist();

            if (shouldOpenInventory)
                inventoryUi?.Open();
        }

        private void ReturnHeldToInventory()
        {
            if (!_holding || _heldItem == null)
                return;

            if (inventory != null)
            {
                if (_heldLiveSubtree && _heldSubtree != null)
                {
                    for (int i = 0; i < _heldSubtree.Count; i++)
                    {
                        PlacedDecor d = _heldSubtree[i];
                        if (d != null && !string.IsNullOrEmpty(d.ItemId))
                            inventory.Add(d.ItemId, 1);
                    }
                }
                else
                {
                    inventory.Add(_heldItem.itemId, 1);
                }
            }

            CleanupHold(destroyLiveSubtree: true);
            Persist();
        }

        /// <summary>未手持时：把桌上某装饰整棵子树收回仓库。</summary>
        private void ReturnPlacedToInventory(PlacedDecor placed)
        {
            if (placed == null || world == null)
                return;

            List<PlacedDecor> subtree = world.DetachSubtree(placed);
            if (subtree != null)
            {
                for (int i = 0; i < subtree.Count; i++)
                {
                    PlacedDecor d = subtree[i];
                    if (d == null)
                        continue;
                    DecorInteractable interactable = d.GetComponent<DecorInteractable>();
                    if (interactable != null)
                        DesktopPetServices.LubyDecorInteraction?.EndAllForDecor(interactable);
                    if (inventory != null && !string.IsNullOrEmpty(d.ItemId))
                        inventory.Add(d.ItemId, 1);
                }
            }

            if (placed != null)
                Object.Destroy(placed.gameObject);

            ClearPendingReturnTarget();
            inventoryUi?.HideAllDeskOverlays();
            Persist();
        }

        private void ShowReturnToInventoryButton()
        {
            if (inventoryUi == null)
                return;

            Transform anchor = null;
            if (_holding && _heldRoot != null)
                anchor = _heldRoot.transform;
            else if (_pendingReturnTarget != null)
                anchor = _pendingReturnTarget.transform;

            if (anchor == null)
                return;

            inventoryUi.ShowReturnDropZone(anchor, OnReturnDropZoneClicked);
        }

        private void OnReturnDropZoneClicked()
        {
            if (_holding && _holdMode == HoldMode.FromPlaced)
            {
                ReturnHeldToInventory();
                return;
            }

            if (_pendingReturnTarget != null)
                ReturnPlacedToInventory(_pendingReturnTarget);
        }

        private void ClearPendingReturnTarget()
        {
            _pendingReturnTarget = null;
        }

        private void ConfirmPlace()
        {
            if (!_holding || _heldItem == null || world == null || !_candidateValid)
                return;

            Vector3 placePos = _snapPos;
            bool onGround = string.IsNullOrEmpty(_parentId);

            PlacedDecor placed;
            if (_heldLiveSubtree && _heldRoot != null && _heldSubtree != null)
            {
                world.CommitSubtree(_heldRoot, placePos, _parentId, _heldSubtree);
                SetSubtreeCollidersEnabled(_heldSubtree, true);
                placed = _heldRoot;
                ClearHeldSubtreeState(destroyVisualRefsOnly: true);
            }
            else
            {
                placed = world.Spawn(_heldItem, placePos, _parentId);
            }

            _returnToInventoryOnCancel = false;
            CleanupHold();
            Persist();

            if (onGround && placed != null)
                placed.PlayPlaceDustOnce();
        }

        private void CleanupHold(bool destroyLiveSubtree = false)
        {
            inventoryUi?.HideAllDeskOverlays();

            if (_heldLiveSubtree)
            {
                if (destroyLiveSubtree && _heldRoot != null)
                {
                    // 子件挂在 root 下，销毁 root 即可
                    Object.Destroy(_heldRoot.gameObject);
                }

                ClearHeldSubtreeState(destroyVisualRefsOnly: true);
            }
            else
            {
                DecorHoldVisuals.Destroy(ref _heldVisual);
            }

            DecorHoldVisuals.Destroy(ref _ghost);

            _holding = false;
            _holdMode = HoldMode.None;
            _inventoryPlaceArmed = false;
            _heldItem = null;
            _returnToInventoryOnCancel = false;
            _originPlacedPos = Vector3.zero;
            _originPlacedParentInstanceId = null;
            _originPlacedInstanceId = null;
            _candidateValid = false;
            _placeSurface = null;
            _parentId = null;
            _ghostRenderers = null;
            ClearPendingReturnTarget();
        }

        private void ClearHeldSubtreeState(bool destroyVisualRefsOnly = false)
        {
            _heldSubtree = null;
            _heldRoot = null;
            _heldLiveSubtree = false;
            if (destroyVisualRefsOnly)
                _heldVisual = null;
        }

        private static void SetSubtreeCollidersEnabled(List<PlacedDecor> subtree, bool enabled)
        {
            if (subtree == null)
                return;

            for (int i = 0; i < subtree.Count; i++)
            {
                PlacedDecor d = subtree[i];
                if (d == null)
                    continue;

                Collider2D[] cols = d.GetComponentsInChildren<Collider2D>(true);
                for (int c = 0; c < cols.Length; c++)
                {
                    if (cols[c] != null)
                        cols[c].enabled = enabled;
                }
            }
        }

        private void EvaluateCandidate(Vector2 cursor)
        {
            float groundY = ResolveGroundY();
            _placeSurface = null;
            _parentId = null;

            PlacedDecor ignoreOwner = _heldLiveSubtree ? _heldRoot : null;
            if (world != null && _heldItem != null)
            {
                _placeSurface = world.FindPlaceSurface(
                    cursor,
                    groundY,
                    _heldItem,
                    groundPreferBand,
                    ignoreOwner);
            }

            if (world != null)
                _snapPos = world.ComputeSnapPosition(_footprint, groundY, cursor, _placeSurface, out _parentId);
            else
                _snapPos = new Vector3(cursor.x, groundY - _footprint.min.y, 0f);

            // 虚影始终在落点（地面/层板），不要求光标靠近
            Bounds candidate = new Bounds(_snapPos + _footprint.center, _footprint.size);
            _candidateValid = false;
            if (world != null)
            {
                if (_heldLiveSubtree && _heldRoot != null && _heldSubtree != null)
                {
                    _candidateValid = world.IsSubtreePlacementValid(
                        _heldRoot,
                        _heldSubtree,
                        _snapPos,
                        _placeSurface,
                        _heldItem);
                }
                else
                {
                    _candidateValid = world.IsPlacementValid(candidate, _placeSurface, _heldItem);
                }
            }

            float bottomToPivot = -_footprint.min.y;
            Vector3 holdPos = new Vector3(cursor.x, cursor.y + bottomToPivot, 0f);

            if (_heldLiveSubtree && _heldRoot != null)
                _heldRoot.transform.position = holdPos;
            else if (_heldVisual != null)
                _heldVisual.transform.position = holdPos;

            if (_ghost != null)
            {
                _ghost.SetActive(true);
                _ghost.transform.position = _snapPos;
            }
        }

        private void UpdateGhostVisual()
        {
            if (_ghost == null || !_ghost.activeSelf || _ghostRenderers == null)
                return;

            Color c = _candidateValid ? validGhostColor : invalidGhostColor;
            DecorHoldVisuals.Tint(_ghostRenderers, c);
        }

        private void CreateHeldAndGhost(ShopItemDefinition item)
        {
            DecorHoldVisuals.Destroy(ref _ghost);
            _ghostRenderers = null;

            if (_heldLiveSubtree && _heldRoot != null)
            {
                // 用手持实体本身，不再另造克隆（叠放子件已挂在 root 下）
                if (_heldVisual != null && _heldVisual != _heldRoot.gameObject)
                    DecorHoldVisuals.Destroy(ref _heldVisual);
                _heldVisual = _heldRoot.gameObject;
            }
            else
            {
                DecorHoldVisuals.Destroy(ref _heldVisual);
                _heldVisual = DecorHoldVisuals.Create(item, transform, "DecorHeld", holdColor, 51, true);
            }

            _ghost = DecorHoldVisuals.Create(item, transform, "DecorGhost", validGhostColor, 49, true);
            _ghostRenderers = _ghost != null
                ? _ghost.GetComponentsInChildren<SpriteRenderer>(true)
                : null;

            // 叠放子树：虚影上挂上子件相对偏移
            if (_heldLiveSubtree && _heldRoot != null && _heldSubtree != null && _ghost != null && _heldSubtree.Count > 1)
            {
                for (int i = 1; i < _heldSubtree.Count; i++)
                {
                    PlacedDecor child = _heldSubtree[i];
                    if (child == null)
                        continue;

                    ShopItemDefinition childDef = child.Definition;
                    if (childDef == null)
                        continue;

                    GameObject childGhost = DecorHoldVisuals.Create(
                        childDef,
                        _ghost.transform,
                        "DecorGhostChild",
                        validGhostColor,
                        49,
                        true);
                    if (childGhost == null)
                        continue;

                    childGhost.transform.localPosition = child.transform.localPosition;
                }

                _ghostRenderers = _ghost.GetComponentsInChildren<SpriteRenderer>(true);
            }

            Vector2 cursor = DeskPointer.WorldOnDeskPlane();
            float bottomToPivot = -_footprint.min.y;
            Vector3 holdPos = new Vector3(cursor.x, cursor.y + bottomToPivot, 0f);
            if (_heldLiveSubtree && _heldRoot != null)
                _heldRoot.transform.position = holdPos;
            else if (_heldVisual != null)
                _heldVisual.transform.position = holdPos;
        }

        private float ResolveGroundY()
        {
            if (ground != null)
                return ground.ResolveGroundY();

            return DesktopPetServices.ResolveGroundY();
        }
    }
}
