using UnityEngine;

namespace DesktopPet.Settings
{
    /// <summary>
    /// 把 SettingsStore 应用到音量 / 相机缩放 / 窗口置顶。
    /// </summary>
    public sealed class SettingsApplicator : MonoBehaviour
    {
        [SerializeField]
        private DesktopCameraZoom cameraZoom;

        [SerializeField]
        private TransparentGameWindow transparentWindow;

        [SerializeField]
        private EnvironmentApplicator environmentApplicator;

        public EnvironmentApplicator Environment => environmentApplicator;

        private void Awake()
        {
            if (transparentWindow == null)
                transparentWindow = DesktopPetServices.TransparentWindow;
            if (cameraZoom == null)
                cameraZoom = DesktopPetServices.CameraZoom;
            if (environmentApplicator == null)
                environmentApplicator = GetComponent<EnvironmentApplicator>();
            if (cameraZoom == null)
                Debug.LogWarning("[Settings] 未绑定 DesktopCameraZoom，滚轮灵敏度等显示项不会生效。");
        }

        private void Start()
        {
            ApplyAll();
        }

        public void ApplyAll()
        {
            ApplyMasterVolume(SettingsStore.MasterVolume);
            ApplyBgmVolume(SettingsStore.BgmVolume);
            ApplyZoomSpeed(SettingsStore.ZoomSpeed);
            ApplyIgnoreZoomOverUi(SettingsStore.IgnoreZoomOverUi);
            ApplyAlwaysOnTop(SettingsStore.AlwaysOnTop);
        }

        public void ApplyMasterVolume(float value)
        {
            SettingsStore.MasterVolume = value;
            AudioListener.volume = Mathf.Clamp01(value);
        }

        public void ApplyBgmVolume(float value)
        {
            SettingsStore.BgmVolume = value;
            DesktopPetServices.Bgm?.ApplySettingsVolume();
        }

        public void ApplyZoomSpeed(float value)
        {
            SettingsStore.ZoomSpeed = value;
            if (cameraZoom != null)
                cameraZoom.ZoomSpeed = value;
        }

        public void ApplyIgnoreZoomOverUi(bool value)
        {
            SettingsStore.IgnoreZoomOverUi = value;
            if (cameraZoom != null)
                cameraZoom.IgnoreWhenPointerOverUi = value;
        }

        public void ApplyAlwaysOnTop(bool value)
        {
            SettingsStore.AlwaysOnTop = value;
            if (transparentWindow != null)
                transparentWindow.AlwaysOnTop = value;
        }

        /// <summary>
        /// 手动刷新透明/穿透状态（用于修复偶发黑屏/无穿透异常）。
        /// </summary>
        public void RefreshTransparentCulling()
        {
            if (transparentWindow == null)
                transparentWindow = DesktopPetServices.TransparentWindow;

            transparentWindow?.RefreshTransparencyCulling();
        }

        public void ResetToDefaultsAndApply()
        {
            SettingsStore.ResetToDefaults();
            ApplyAll();
            environmentApplicator?.ApplyAllEnvironment();
        }

        private void OnApplicationQuit()
        {
            environmentApplicator?.PersistDayNightState();
            SettingsStore.Save();
        }
    }
}
