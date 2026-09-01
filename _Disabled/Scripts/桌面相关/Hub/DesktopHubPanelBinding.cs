using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DesktopPet.Hub
{
    /// <summary>挂在 DesktopHubPanel 上，保存壳层与各页引用。</summary>
    public sealed class DesktopHubPanelBinding : MonoBehaviour
    {
        public Button closeButton;
        public TextMeshProUGUI capacityText;
        public TextMeshProUGUI currencyText;

        public Button tabShop;
        public Button tabInventory;
        public Button tabLuby;
        public Button tabCodex;
        public Button tabSettings;
        public Button tabScene;

        public GameObject shopPage;
        public GameObject inventoryPage;
        public GameObject lubyPage;
        public GameObject codexPage;
        public GameObject settingsPage;
        public GameObject scenePage;
    }
}
