using System;
using DesktopPet.Luby;
using DesktopPet.UI;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace DesktopPet.Inventory
{
    public sealed partial class InventoryUIController
    {
        [FoldoutGroup("Luby 桌上菜单")]
        [SerializeField] private RectTransform lubyDeskContextMenu;
        [SerializeField] private Button lubyDeskReturnButton;
        [SerializeField] private Button lubyDeskInfoButton;
        [SerializeField] private LubyInfoPanelController lubyInfoPanel;

        private Transform _lubyDeskFollowTarget;
        private LubyInstanceComponent _lubyDeskTarget;
        private UiPanelDragHandle _lubyDeskDragHandle;

        public void HideLubyInfoPanel()
        {
            lubyInfoPanel?.Hide();
        }

        /// <summary>在 Luby 头顶显示「收回仓库 / 信息面板」。</summary>
        public void ShowLubyDeskContextMenu(
            Transform worldTarget,
            LubyInstanceComponent luby,
            Action onReturnClicked)
        {
            if (worldTarget == null || luby == null)
                return;

            HideReturnDropZone();
            HideLubyInfoPanel();

            if (lubyDeskContextMenu == null)
            {
                Debug.LogError("[InventoryUI] 未绑定 lubyDeskContextMenu。请改 MainCanvas.prefab 后「应用主面板」。");
                return;
            }

            if (lubyDeskReturnButton == null || lubyDeskInfoButton == null)
            {
                Debug.LogError("[InventoryUI] LubyDeskContextMenu 缺少按钮。请改 MainCanvas.prefab。");
                return;
            }

            _lubyDeskFollowTarget = worldTarget;
            _lubyDeskTarget = luby;

            lubyDeskReturnButton.onClick.RemoveAllListeners();
            lubyDeskReturnButton.onClick.AddListener(() => onReturnClicked?.Invoke());

            lubyDeskInfoButton.onClick.RemoveAllListeners();
            lubyDeskInfoButton.onClick.AddListener(OnLubyDeskInfoClicked);

            if (_lubyDeskDragHandle == null && lubyDeskContextMenu != null)
                _lubyDeskDragHandle = lubyDeskContextMenu.GetComponentInChildren<UiPanelDragHandle>(true);
            _lubyDeskDragHandle?.ResetUserMoved();

            lubyDeskContextMenu.SetAsLastSibling();
            lubyDeskContextMenu.gameObject.SetActive(true);
            PlaceOverlayAbove(lubyDeskContextMenu, worldTarget);
        }

        public void HideLubyDeskContextMenu()
        {
            if (lubyDeskReturnButton != null)
                lubyDeskReturnButton.onClick.RemoveAllListeners();
            if (lubyDeskInfoButton != null)
                lubyDeskInfoButton.onClick.RemoveAllListeners();

            _lubyDeskFollowTarget = null;
            _lubyDeskTarget = null;

            if (lubyDeskContextMenu != null)
                lubyDeskContextMenu.gameObject.SetActive(false);
        }

        public bool IsCursorOverLubyDeskContextMenu() =>
            IsCursorOverActiveOverlay(lubyDeskContextMenu);

        public bool IsLubyDeskContextMenuVisible =>
            lubyDeskContextMenu != null && lubyDeskContextMenu.gameObject.activeSelf;

        public bool IsLubyDeskContextMenuAvailable =>
            lubyDeskContextMenu != null
            && lubyDeskReturnButton != null
            && lubyDeskInfoButton != null;

        public bool IsLubyInfoPanelVisible =>
            lubyInfoPanel != null && lubyInfoPanel.IsVisible;

        public bool IsCursorOverLubyInfoPanel() =>
            lubyInfoPanel != null
            && lubyInfoPanel.IsVisible
            && IsCursorOverActiveOverlay(lubyInfoPanel.PanelRect);

        private void OnLubyDeskInfoClicked()
        {
            LubyInstanceComponent luby = _lubyDeskTarget;
            HideLubyDeskContextMenu();
            if (lubyInfoPanel == null)
            {
                Debug.LogError("[InventoryUI] 未绑定 lubyInfoPanel。请改 MainCanvas.prefab 后「应用主面板」。");
                return;
            }

            lubyInfoPanel.Show(luby);
        }

        private void TickLubyDeskContextFollow()
        {
            if (lubyDeskContextMenu == null || !lubyDeskContextMenu.gameObject.activeSelf)
                return;
            if (_lubyDeskFollowTarget == null)
                return;

            if (_lubyDeskDragHandle != null && _lubyDeskDragHandle.UserMoved)
                return;

            PlaceOverlayAbove(lubyDeskContextMenu, _lubyDeskFollowTarget);
        }
    }
}
