using DesktopPet;
using DesktopPet.Inventory;
using DesktopPet.Luby;
using UnityEngine;

namespace DesktopPet.Decor
{
    public sealed partial class DecorPlacementSystem
    {
        private void Update()
        {
            if (_holding && _heldItem != null)
            {
                TickHold();
                return;
            }

            LubyPlacementSystem lubyPlacement = DesktopPetServices.LubyPlacement;
            if (lubyPlacement != null && lubyPlacement.IsHolding)
                return;

            DeskIdlePointerInput.Tick(
                BuildIdleFaction(),
                lubyPlacement != null ? lubyPlacement.BuildIdleFaction() : default);

            TickLongPressPickup();
        }

        private DeskIdlePointerInput.Faction BuildIdleFaction()
        {
            return new DeskIdlePointerInput.Faction
            {
                HasPending = _pendingReturnTarget != null,
                TrySelectOwn = TrySelectDecorPending,
                IsOtherUnderCursor = IsCursorOverLuby,
                OnSelectedOwn = ShowReturnToInventoryButton,
                ClearPending = ClearPendingReturnTarget
            };
        }

        private bool TrySelectDecorPending()
        {
            PlacedDecor hit = world != null
                ? world.FindPickupUnderPoint(DeskPointer.WorldOnDeskPlane())
                : null;
            if (hit == null)
                return false;
            _pendingReturnTarget = hit;
            return true;
        }

        private void TickHold()
        {
            Vector2 cursor = DeskPointer.WorldOnDeskPlane();
            EvaluateCandidate(cursor);
            UpdateGhostVisual();

            if (_holdMode == HoldMode.FromInventory)
            {
                TickInventoryPlaceConfirm();
                return;
            }

            // 桌上移动：右键呼出收回按钮；松手合法则放下
            if (Input.GetMouseButtonDown(1))
            {
                ShowReturnToInventoryButton();
                return;
            }

            if (Input.GetMouseButtonUp(0))
            {
                if (TransparentGameWindow.ShouldBlockWorldPointer())
                {
                    if (inventoryUi != null && inventoryUi.IsCursorOverReturnDropZone())
                        inventoryUi.HideReturnDropZone();
                    else
                        return;
                }

                if (_candidateValid)
                    ConfirmPlace();
                else
                    CancelHold();
            }
        }

        /// <summary>
        /// 仓库放置：等黄钮点击的那次按键松开后再武装；之后左键按下在合法点放置。
        /// 右键 / Esc 取消退回仓库。
        /// </summary>
        private void TickInventoryPlaceConfirm()
        {
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                CancelHold();
                return;
            }

            if (!_inventoryPlaceArmed)
            {
                if (!Input.GetMouseButton(0))
                    _inventoryPlaceArmed = true;
                return;
            }

            if (!Input.GetMouseButtonDown(0))
                return;

            if (TransparentGameWindow.ShouldBlockWorldPointer())
                return;

            if (_candidateValid)
                ConfirmPlace();
            // 非法点：继续跟手，不取消
        }

        private void TickLongPressPickup()
        {
            if (TransparentGameWindow.ShouldBlockWorldPointer())
            {
                ClearPressTracking();
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                PlacedDecor hit = world != null
                    ? world.FindPickupUnderPoint(DeskPointer.WorldOnDeskPlane())
                    : null;
                if (hit != null)
                {
                    _pressTracking = true;
                    _pressStartTime = Time.unscaledTime;
                    _pressStartWorld = DeskPointer.WorldOnDeskPlane();
                    _pressTarget = hit;
                }
                else
                {
                    ClearPressTracking();
                }

                return;
            }

            if (!_pressTracking || _pressTarget == null)
                return;

            if (!Input.GetMouseButton(0))
            {
                TryDispatchShortClick();
                ClearPressTracking();
                return;
            }

            Vector2 cursor = DeskPointer.WorldOnDeskPlane();
            if (Vector2.Distance(cursor, _pressStartWorld) > longPressMaxMove)
            {
                ClearPressTracking();
                return;
            }

            if (Time.unscaledTime - _pressStartTime < longPressSeconds)
                return;

            PlacedDecor target = _pressTarget;
            ClearPressTracking();
            ClearPendingReturnTarget();
            if (inventoryUi != null)
                inventoryUi.HideAllDeskOverlays();
            TryBeginFromPlaced(target);
        }

        private void TryDispatchShortClick()
        {
            if (_pressTarget == null)
                return;
            if (DesktopPetServices.HubUi != null && DesktopPetServices.HubUi.IsOpen)
                return;
            if (TransparentGameWindow.ShouldBlockWorldPointer())
                return;

            Vector2 cursor = DeskPointer.WorldOnDeskPlane();
            if (Vector2.Distance(cursor, _pressStartWorld) > longPressMaxMove)
                return;

            IDecorShortClickHandler[] handlers =
                _pressTarget.GetComponentsInChildren<IDecorShortClickHandler>(true);
            for (int i = 0; i < handlers.Length; i++)
                handlers[i]?.OnShortClick();
        }

        private void ClearPressTracking()
        {
            _pressTracking = false;
            _pressTarget = null;
        }

        private static bool IsCursorOverLuby()
        {
            LubyPlacementSystem lubyPlacement = DesktopPetServices.LubyPlacement;
            return lubyPlacement != null && lubyPlacement.TryPickUnderCursor() != null;
        }
    }
}

