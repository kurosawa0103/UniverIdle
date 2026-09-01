#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace DesktopPet.Gm.Editor
{
    /// <summary>
    /// Play Mode 全局时间加速（Time.timeScale）。
    /// 菜单 / Ctrl+Shift+T 切换；退出 Play 自动回到 1。
    /// </summary>
    public static class DesktopPetTimeScaleEditor
    {
        private static readonly float[] Steps = { 1f, 2f, 5f, 10f };

        [InitializeOnLoadMethod]
        private static void HookPlayMode()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode
                || state == PlayModeStateChange.EnteredEditMode)
            {
                Time.timeScale = 1f;
            }
        }

        [MenuItem("桌宠/时间加速 · 切换 %#t", false, 200)]
        public static void Cycle()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("[桌宠时间] 请先进入 Play Mode。快捷键 Ctrl+Shift+T。");
                return;
            }

            float current = Time.timeScale;
            int next = 0;
            for (int i = 0; i < Steps.Length; i++)
            {
                if (Mathf.Approximately(current, Steps[i]))
                {
                    next = (i + 1) % Steps.Length;
                    Apply(Steps[next]);
                    return;
                }
            }

            Apply(Steps[0]);
        }

        [MenuItem("桌宠/时间加速/x1", false, 210)]
        public static void SetX1() => Apply(1f);

        [MenuItem("桌宠/时间加速/x2", false, 211)]
        public static void SetX2() => Apply(2f);

        [MenuItem("桌宠/时间加速/x5", false, 212)]
        public static void SetX5() => Apply(5f);

        [MenuItem("桌宠/时间加速/x10", false, 213)]
        public static void SetX10() => Apply(10f);

        [MenuItem("桌宠/时间加速/x1", true)]
        [MenuItem("桌宠/时间加速/x2", true)]
        [MenuItem("桌宠/时间加速/x5", true)]
        [MenuItem("桌宠/时间加速/x10", true)]
        private static bool ValidateScaleMenus() => EditorApplication.isPlaying;

        public static void Apply(float scale)
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("[桌宠时间] 请先进入 Play Mode。");
                return;
            }

            float clamped = Mathf.Clamp(scale, 0.01f, 100f);
            Time.timeScale = clamped;
            Debug.Log($"[桌宠时间] Time.timeScale = {clamped:0.##}x（探险离桌 UTC 倒计时不受影响）");
        }

        public static float CurrentScale =>
            EditorApplication.isPlaying ? Time.timeScale : 1f;
    }
}
#endif
