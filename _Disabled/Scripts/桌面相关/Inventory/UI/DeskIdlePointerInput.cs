using System;
using UnityEngine;

namespace DesktopPet.Inventory
{
    /// <summary>Decor / Luby 未手持时的右键收回输入：每帧只 Tick 一次，UI 遮挡只 Raycast 一次。</summary>
    public static class DeskIdlePointerInput
    {
        public struct Faction
        {
            public bool HasPending;
            public Func<bool> TrySelectOwn;
            public Func<bool> IsOtherUnderCursor;
            public Action OnSelectedOwn;
            public Action ClearPending;
        }

        public static void Tick(Faction decor, Faction luby)
        {
            bool anyPending = decor.HasPending || luby.HasPending;
            if (!anyPending
                && !Input.GetMouseButtonDown(0)
                && !Input.GetMouseButtonDown(1))
            {
                return;
            }

            if (TransparentGameWindow.ShouldBlockWorldPointer())
                return;

            InventoryUIController ui = DesktopPetServices.InventoryUi;

            if (Input.GetMouseButtonDown(1))
            {
                if (decor.TrySelectOwn != null && decor.TrySelectOwn())
                {
                    decor.OnSelectedOwn?.Invoke();
                    return;
                }

                if (luby.TrySelectOwn != null && luby.TrySelectOwn())
                {
                    luby.OnSelectedOwn?.Invoke();
                    return;
                }

                decor.ClearPending?.Invoke();
                luby.ClearPending?.Invoke();

                bool otherOwns = (decor.IsOtherUnderCursor != null && decor.IsOtherUnderCursor())
                    || (luby.IsOtherUnderCursor != null && luby.IsOtherUnderCursor());
                if (!otherOwns)
                    ui?.HideAllDeskOverlays();
                return;
            }

            if (anyPending
                && Input.GetMouseButtonDown(0)
                && ui != null
                && !ui.IsCursorOverAnyDeskOverlay())
            {
                decor.ClearPending?.Invoke();
                luby.ClearPending?.Invoke();
                ui.HideAllDeskOverlays();
            }
        }
    }
}
