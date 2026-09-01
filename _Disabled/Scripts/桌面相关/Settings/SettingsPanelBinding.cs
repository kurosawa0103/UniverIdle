using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DesktopPet.Settings
{
    /// <summary>
    /// 挂在 MainCanvas → SettingsPage 上，保存内容区控件引用（无模块顶栏 / 关闭）。
    /// SettingsUIController 只持有本组件一份引用；applicator 仍在 SettingsSystem 上。
    /// </summary>
    public sealed class SettingsPanelBinding : MonoBehaviour
    {
        public Button resetButton;

        public Button audioTabButton;
        public Button displayTabButton;
        public Button windowTabButton;
        public Button environmentTabButton;
        public GameObject audioPage;
        public GameObject displayPage;
        public GameObject windowPage;
        public GameObject environmentPage;
        public Slider masterVolumeSlider;
        public Slider bgmVolumeSlider;
        public TextMeshProUGUI masterVolumeValueText;
        public TextMeshProUGUI bgmVolumeValueText;
        public Slider zoomSpeedSlider;
        public TextMeshProUGUI zoomSpeedValueText;
        public Toggle ignoreZoomOverUiToggle;
        public Toggle alwaysOnTopToggle;
        public Button refreshTransparentCullingButton;
        public TMP_Dropdown dayNightPhaseDropdown;
        public Toggle dayNightAutoCycleToggle;
        public TMP_Dropdown weatherDropdown;
    }
}
