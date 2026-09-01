using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DesktopPet.Background
{
    /// <summary>ScenePage 容量区控件引用（MainCanvas 预制体）。</summary>
    public sealed class ScenePageCapacityBinding : MonoBehaviour
    {
        public TextMeshProUGUI decorStatusText;
        public TextMeshProUGUI decorCostText;
        public Button decorUpgradeButton;

        public TextMeshProUGUI lubyStatusText;
        public TextMeshProUGUI lubyCostText;
        public Button lubyUpgradeButton;

        public TextMeshProUGUI hintText;
    }
}
