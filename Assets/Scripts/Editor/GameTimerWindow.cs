using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;

public class GameTimerWindow : EditorWindow
{
    private static DateTime startTime;
    private static bool isRunning;
    private static double elapsedSeconds;

    private static bool isPaused;
    private static DateTime pauseStartTime;
    private static double pausedTotalSeconds;

    private static string currentSceneName;
    private static DateTime sceneEnterTime;
    private static Dictionary<string, double> sceneTimes = new Dictionary<string, double>();

    private static DateTime pauseTime;

    // 自定义大字样式
    private GUIStyle titleStyle;
    private GUIStyle timerStyle;
    private GUIStyle sceneStyle;

    [MenuItem("工具/游戏计时器")]
    public static void ShowWindow()
    {
        GetWindow<GameTimerWindow>("Game Timer");
    }

    private void OnEnable()
    {
        EditorApplication.update += UpdateTimer;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        SceneManager.activeSceneChanged += OnSceneChanged;
        EditorApplication.pauseStateChanged += OnEditorPauseChanged;

        // 初始化自定义样式
        titleStyle = new GUIStyle()
        {
            fontSize = 24,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };
        timerStyle = new GUIStyle()
        {
            fontSize = 28,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.yellow }
        };
        sceneStyle = new GUIStyle()
        {
            fontSize = 18,
            normal = { textColor = Color.cyan }
        };
    }

    private void OnDisable()
    {
        EditorApplication.update -= UpdateTimer;
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        SceneManager.activeSceneChanged -= OnSceneChanged;
        EditorApplication.pauseStateChanged -= OnEditorPauseChanged;
    }

    private void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            startTime = DateTime.Now;
            elapsedSeconds = 0;
            isRunning = true;
            isPaused = false;
            pausedTotalSeconds = 0;

            currentSceneName = SceneManager.GetActiveScene().name;
            sceneEnterTime = DateTime.Now;
            sceneTimes.Clear();
        }
        else if (state == PlayModeStateChange.ExitingPlayMode)
        {
            SaveCurrentSceneTime();
            isRunning = false;
        }
        else if (state == PlayModeStateChange.ExitingEditMode)
        {
            ShowWindow();
        }
    }

    private void OnSceneChanged(Scene oldScene, Scene newScene)
    {
        if (isRunning)
        {
            SaveCurrentSceneTime();
            currentSceneName = newScene.name;
            sceneEnterTime = DateTime.Now;
        }
    }

    private void SaveCurrentSceneTime()
    {
        if (!string.IsNullOrEmpty(currentSceneName))
        {
            double duration = (DateTime.Now - sceneEnterTime).TotalSeconds;

            if (sceneTimes.ContainsKey(currentSceneName))
                sceneTimes[currentSceneName] += duration;
            else
                sceneTimes[currentSceneName] = duration;

            sceneEnterTime = DateTime.Now;
            Debug.Log($"[GameTimer] 场景 {currentSceneName} 本次耗时: {duration:F2} 秒，总计: {sceneTimes[currentSceneName]:F2} 秒");
        }
    }

    private void OnEditorPauseChanged(PauseState state)
    {
        if (state == PauseState.Paused)
        {
            isPaused = true;
            pauseStartTime = DateTime.Now;
        }
        else if (state == PauseState.Unpaused)
        {
            if (isPaused)
            {
                pausedTotalSeconds += (DateTime.Now - pauseStartTime).TotalSeconds;
                isPaused = false;
            }
        }
    }

    private void UpdateTimer()
    {
        // 防护：只要不在 Play 模式，就不再计时
        if (!EditorApplication.isPlaying)
        {
            isRunning = false;
            return;
        }

        if (!isRunning) return;

        if (EditorApplication.isPaused)
        {
            if (pauseTime == DateTime.MinValue)
                pauseTime = DateTime.Now;
            return;
        }
        else
        {
            if (pauseTime != DateTime.MinValue)
            {
                TimeSpan pausedDuration = DateTime.Now - pauseTime;
                startTime = startTime.Add(pausedDuration);
                sceneEnterTime = sceneEnterTime.Add(pausedDuration);
                pauseTime = DateTime.MinValue;
            }
        }

        elapsedSeconds = (DateTime.Now - startTime).TotalSeconds;
        Repaint();
    }

    private void OnGUI()
    {
        GUILayout.Label("游戏运行计时器", titleStyle);

        if (isRunning)
        {
            TimeSpan time = TimeSpan.FromSeconds(elapsedSeconds);
            GUILayout.Label($"运行时间: {time:hh\\:mm\\:ss}", timerStyle);

            if (isPaused)
            {
                GUILayout.Label(" 已暂停", sceneStyle);
            }

            GUILayout.Space(10);
            GUILayout.Label("场景耗时统计（从高到低）：", titleStyle);

            // 按耗时从高到低排序显示
            foreach (var kvp in SortedSceneTimes())
            {
                TimeSpan t = TimeSpan.FromSeconds(kvp.Value);
                GUILayout.Label($"- {kvp.Key}: {t:hh\\:mm\\:ss}", sceneStyle);
            }

            // 当前场景单独显示（含本次进入的时间）
            if (!string.IsNullOrEmpty(currentSceneName))
            {
                double currentSceneElapsed = (DateTime.Now - sceneEnterTime).TotalSeconds;
                double total = currentSceneElapsed + (sceneTimes.ContainsKey(currentSceneName) ? sceneTimes[currentSceneName] : 0);
                TimeSpan t = TimeSpan.FromSeconds(total);
                GUILayout.Label($"* 当前场景 {currentSceneName}: {t:hh\\:mm\\:ss}", sceneStyle);
            }
        }
        else
        {
            GUILayout.Label("未在运行", timerStyle);
        }
    }

    // 返回按耗时从高到低排序的场景列表
    private IEnumerable<KeyValuePair<string, double>> SortedSceneTimes()
    {
        var sorted = new List<KeyValuePair<string, double>>(sceneTimes);
        sorted.Sort((a, b) => b.Value.CompareTo(a.Value)); // 按值从高到低
        return sorted;
    }
}
