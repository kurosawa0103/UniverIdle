#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

/// <summary>
/// 编辑器性能监视器：打开窗口后进入 Play 模式，实时查看 FPS、内存、渲染与场景对象等数据。
/// </summary>
public class PerformanceMonitorWindow : EditorWindow
{
    const int FpsHistoryLength = 120;
    const float ObjectCountRefreshInterval = 2f;

    enum MonitorTab
    {
        Overview,
        Rendering,
        Memory,
        SceneObjects,
        Recording
    }

    [MenuItem("工具/性能监视器")]
    public static void ShowWindow()
    {
        var window = GetWindow<PerformanceMonitorWindow>("性能监视器");
        window.minSize = new Vector2(360, 420);
        window.Show();
    }

    MonitorTab _tab = MonitorTab.Overview;
    bool _autoRepaint = true;
    float _fpsSampleInterval = 0.25f;
    float _fpsSampleTimer;

    float _fpsCurrent;
    float _fpsAvg;
    float _fpsMin = float.MaxValue;
    float _fpsMax;
    float _frameMs;
    readonly float[] _fpsHistory = new float[FpsHistoryLength];
    int _fpsHistoryIndex;

    double _playStartTime;
    bool _wasPlaying;

    int _goCount;
    int _activeGoCount;
    int _meshRendererCount;
    int _spriteRendererCount;
    int _canvasCount;
    int _audioSourceCount;
    int _particleSystemCount;
    float _objectCountTimer;

    bool _isRecording;
    readonly List<string> _recordLines = new List<string>();
    string _lastExportPath;

    GUIStyle _titleStyle;
    GUIStyle _valueStyle;
    GUIStyle _warnStyle;
    GUIStyle _sectionStyle;
    Vector2 _scroll;
    Texture2D _graphTexture;

    void OnEnable()
    {
        EditorApplication.update += OnEditorUpdate;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        SceneManager.activeSceneChanged += OnSceneChanged;

        _titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 16,
            normal = { textColor = new Color(0.9f, 0.95f, 1f) }
        };
        _valueStyle = new GUIStyle(EditorStyles.label)
        {
            fontSize = 13,
            richText = true
        };
        _warnStyle = new GUIStyle(EditorStyles.label)
        {
            fontSize = 12,
            normal = { textColor = new Color(1f, 0.75f, 0.4f) }
        };
        _sectionStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 12,
            normal = { textColor = new Color(0.65f, 0.85f, 1f) }
        };

        ResetSessionStats();
    }

    void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        SceneManager.activeSceneChanged -= OnSceneChanged;

        if (_graphTexture != null)
            DestroyImmediate(_graphTexture);
    }

    void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            ResetSessionStats();
            _playStartTime = EditorApplication.timeSinceStartup;
        }
        else if (state == PlayModeStateChange.ExitingPlayMode)
        {
            _wasPlaying = false;
        }
    }

    void OnSceneChanged(Scene oldScene, Scene newScene)
    {
        _objectCountTimer = 0f;
        RefreshObjectCounts();
    }

    void ResetSessionStats()
    {
        _fpsMin = float.MaxValue;
        _fpsMax = 0f;
        _fpsAvg = 0f;
        Array.Clear(_fpsHistory, 0, _fpsHistory.Length);
        _fpsHistoryIndex = 0;
        _objectCountTimer = 0f;
    }

    void OnEditorUpdate()
    {
        if (!EditorApplication.isPlaying)
        {
            _wasPlaying = false;
            return;
        }

        if (!_wasPlaying)
        {
            _wasPlaying = true;
            ResetSessionStats();
            _playStartTime = EditorApplication.timeSinceStartup;
        }

        if (EditorApplication.isPaused)
            return;

        float dt = Time.unscaledDeltaTime;
        if (dt <= 0f)
            return;

        _fpsCurrent = 1f / dt;
        _frameMs = dt * 1000f;

        _fpsSampleTimer += dt;
        if (_fpsSampleTimer >= _fpsSampleInterval)
        {
            _fpsSampleTimer = 0f;
            PushFpsSample(_fpsCurrent);

            if (_isRecording)
                AppendRecordLine();
        }

        _objectCountTimer += dt;
        if (_objectCountTimer >= ObjectCountRefreshInterval)
        {
            _objectCountTimer = 0f;
            RefreshObjectCounts();
        }

        if (_autoRepaint)
            Repaint();
    }

    void PushFpsSample(float fps)
    {
        _fpsHistory[_fpsHistoryIndex] = fps;
        _fpsHistoryIndex = (_fpsHistoryIndex + 1) % FpsHistoryLength;

        if (fps < _fpsMin) _fpsMin = fps;
        if (fps > _fpsMax) _fpsMax = fps;

        float sum = 0f;
        int count = 0;
        for (int i = 0; i < FpsHistoryLength; i++)
        {
            if (_fpsHistory[i] <= 0f) continue;
            sum += _fpsHistory[i];
            count++;
        }
        _fpsAvg = count > 0 ? sum / count : fps;
    }

    void RefreshObjectCounts()
    {
        if (!EditorApplication.isPlaying)
            return;

        var allGos = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        _goCount = allGos.Length;
        int active = 0;
        foreach (var go in allGos)
        {
            if (go.activeInHierarchy)
                active++;
        }
        _activeGoCount = active;

        _meshRendererCount = FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
        _spriteRendererCount = FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
        _canvasCount = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
        _audioSourceCount = FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
        _particleSystemCount = FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
    }

    static string FormatBytes(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        double kb = bytes / 1024.0;
        if (kb < 1024) return $"{kb:F1} KB";
        double mb = kb / 1024.0;
        if (mb < 1024) return $"{mb:F2} MB";
        return $"{mb / 1024.0:F2} GB";
    }

    static long GetGfxMemory() =>
        Profiler.GetAllocatedMemoryForGraphicsDriver();

    void OnGUI()
    {
        DrawToolbar();

        if (!EditorApplication.isPlaying)
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.HelpBox(
                "请先点击 Play 进入运行模式，本窗口会实时刷新性能数据。\n" +
                "建议：Play 前打开本窗口，并勾选「运行时自动刷新」。",
                MessageType.Info);
            return;
        }

        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        switch (_tab)
        {
            case MonitorTab.Overview:
                DrawOverviewTab();
                break;
            case MonitorTab.Rendering:
                DrawRenderingTab();
                break;
            case MonitorTab.Memory:
                DrawMemoryTab();
                break;
            case MonitorTab.SceneObjects:
                DrawSceneObjectsTab();
                break;
            case MonitorTab.Recording:
                DrawRecordingTab();
                break;
        }

        EditorGUILayout.EndScrollView();
    }

    void DrawToolbar()
    {
        EditorGUILayout.Space(4);
        _tab = (MonitorTab)GUILayout.Toolbar((int)_tab, new[]
        {
            "概览", "渲染", "内存", "对象", "记录"
        });
        EditorGUILayout.Space(4);

        EditorGUILayout.BeginHorizontal();
        _autoRepaint = EditorGUILayout.ToggleLeft("运行时自动刷新", _autoRepaint, GUILayout.Width(130));
        if (GUILayout.Button("立即刷新", GUILayout.Width(72)))
        {
            if (EditorApplication.isPlaying)
                RefreshObjectCounts();
            Repaint();
        }
        if (GUILayout.Button("重置统计", GUILayout.Width(72)))
            ResetSessionStats();
        EditorGUILayout.EndHorizontal();

        _fpsSampleInterval = EditorGUILayout.Slider("采样间隔(秒)", _fpsSampleInterval, 0.1f, 1f);
    }

    void DrawOverviewTab()
    {
        GUILayout.Label("帧率", _titleStyle);
        DrawFpsGraph(140f);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("当前 FPS", $"<b>{_fpsCurrent:F1}</b>  ({_frameMs:F2} ms)", _valueStyle);
        EditorGUILayout.LabelField("平均 / 最低 / 最高",
            $"{_fpsAvg:F1} / {(_fpsMin == float.MaxValue ? 0 : _fpsMin):F1} / {_fpsMax:F1}", _valueStyle);

        EditorGUILayout.Space(8);
        GUILayout.Label("运行环境", _titleStyle);

        var scene = SceneManager.GetActiveScene();
        double runSec = EditorApplication.timeSinceStartup - _playStartTime;
        EditorGUILayout.LabelField("当前场景", scene.name, _valueStyle);
        EditorGUILayout.LabelField("运行时长", TimeSpan.FromSeconds(runSec).ToString(@"hh\:mm\:ss"), _valueStyle);
        EditorGUILayout.LabelField("目标帧率", Application.targetFrameRate <= 0 ? "不限制" : Application.targetFrameRate.ToString(), _valueStyle);
        EditorGUILayout.LabelField("垂直同步", QualitySettings.vSyncCount == 0 ? "关" : $"每 {QualitySettings.vSyncCount} 帧", _valueStyle);
        EditorGUILayout.LabelField("画质等级", QualitySettings.names[QualitySettings.GetQualityLevel()], _valueStyle);
        EditorGUILayout.LabelField("平台", Application.platform.ToString(), _valueStyle);

        EditorGUILayout.Space(8);
        GUILayout.Label("快速摘要", _titleStyle);
        EditorGUILayout.LabelField("内存(已分配)", FormatBytes(Profiler.GetTotalAllocatedMemoryLong()), _valueStyle);
        EditorGUILayout.LabelField("Draw Calls / Batches", $"{UnityStats.drawCalls} / {UnityStats.batches}", _valueStyle);
        EditorGUILayout.LabelField("三角形", UnityStats.triangles.ToString("N0"), _valueStyle);
        EditorGUILayout.LabelField("场景对象(激活/总计)", $"{_activeGoCount} / {_goCount}", _valueStyle);

        if (EditorApplication.isPaused)
            EditorGUILayout.LabelField("编辑器已暂停 — 数据不会更新", _warnStyle);
    }

    void DrawRenderingTab()
    {
        GUILayout.Label("渲染统计 (Game 视图 Stats)", _titleStyle);
        EditorGUILayout.Space(4);

        DrawStatRow("Draw Calls", UnityStats.drawCalls.ToString());
        DrawStatRow("Batches", UnityStats.batches.ToString());
        DrawStatRow("SetPass Calls", UnityStats.setPassCalls.ToString());
        DrawStatRow("Triangles", UnityStats.triangles.ToString("N0"));
        DrawStatRow("Vertices", UnityStats.vertices.ToString("N0"));
        DrawStatRow("Shadow Casters", UnityStats.shadowCasters.ToString());
        DrawStatRow("Visible Skinned Meshes", UnityStats.visibleSkinnedMeshes.ToString());
        DrawStatRow("Animation Components Playing", UnityStats.animationComponentsPlaying.ToString());
        DrawStatRow("Animator Components Playing", UnityStats.animatorComponentsPlaying.ToString());

        EditorGUILayout.Space(8);
        GUILayout.Label("纹理 / RenderTexture", _sectionStyle);
        DrawStatRow("Used Textures", UnityStats.usedTextureCount.ToString());
        DrawStatRow("Texture Memory", FormatBytes(UnityStats.usedTextureMemorySize));
        DrawStatRow("Render Textures", UnityStats.renderTextureCount.ToString());
        DrawStatRow("RT Memory", FormatBytes(UnityStats.renderTextureBytes));

        EditorGUILayout.Space(8);
        GUILayout.Label("屏幕", _sectionStyle);
        int sw = Screen.width;
        int sh = Screen.height;
        DrawStatRow("Screen Size", $"{sw} × {sh}");
        long screenBytes = (long)sw * sh * 4;
        DrawStatRow("Screen Buffer (约)", FormatBytes(screenBytes));
    }

    void DrawMemoryTab()
    {
        GUILayout.Label("Profiler 内存", _titleStyle);
        EditorGUILayout.Space(4);

        long allocated = Profiler.GetTotalAllocatedMemoryLong();
        long reserved = Profiler.GetTotalReservedMemoryLong();
        long unused = reserved - allocated;
        long monoUsed = Profiler.GetMonoUsedSizeLong();
        long monoHeap = Profiler.GetMonoHeapSizeLong();
        long gfx = GetGfxMemory();

        DrawStatRow("Total Allocated", FormatBytes(allocated));
        DrawStatRow("Total Reserved", FormatBytes(reserved));
        DrawStatRow("Unused Reserved", FormatBytes(unused));
        DrawStatRow("Mono Used", FormatBytes(monoUsed));
        DrawStatRow("Mono Heap", FormatBytes(monoHeap));
        DrawStatRow("Graphics Driver", FormatBytes(gfx));

        EditorGUILayout.Space(8);
        GUILayout.Label("操作", _sectionStyle);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("GC.Collect()"))
        {
            GC.Collect();
            Debug.Log("[性能监视器] 已执行 GC.Collect()");
        }
        if (GUILayout.Button("卸载未使用资源"))
        {
            Resources.UnloadUnusedAssets();
            Debug.Log("[性能监视器] 已调用 Resources.UnloadUnusedAssets()");
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);
        EditorGUILayout.HelpBox(
            "内存数值来自 Unity Profiler API，与 Profiler 窗口一致方向的数据。\n" +
            "若在场景中频繁 Instantiate/Destroy，可观察 Mono Used 与 GC 按钮效果。",
            MessageType.None);
    }

    void DrawSceneObjectsTab()
    {
        GUILayout.Label("场景对象计数", _titleStyle);
        EditorGUILayout.LabelField($"每 {ObjectCountRefreshInterval:F0} 秒自动刷新（或点「立即刷新」）", _warnStyle);
        EditorGUILayout.Space(4);

        DrawStatRow("GameObject 总计", _goCount.ToString("N0"));
        DrawStatRow("GameObject 激活中", _activeGoCount.ToString("N0"));
        DrawStatRow("MeshRenderer", _meshRendererCount.ToString("N0"));
        DrawStatRow("SpriteRenderer", _spriteRendererCount.ToString("N0"));
        DrawStatRow("Canvas", _canvasCount.ToString("N0"));
        DrawStatRow("AudioSource", _audioSourceCount.ToString("N0"));
        DrawStatRow("ParticleSystem", _particleSystemCount.ToString("N0"));

        EditorGUILayout.Space(8);
        if (GUILayout.Button("立即重新统计对象"))
            RefreshObjectCounts();

        EditorGUILayout.HelpBox(
            "对象统计会遍历场景，仅用于调试。正式包体请勿常驻开启高频刷新。",
            MessageType.Warning);
    }

    void DrawRecordingTab()
    {
        GUILayout.Label("数据记录", _titleStyle);
        EditorGUILayout.Space(4);

        EditorGUILayout.BeginHorizontal();
        if (_isRecording)
        {
            GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button("停止记录", GUILayout.Height(28)))
                _isRecording = false;
            GUI.backgroundColor = Color.white;
        }
        else
        {
            GUI.enabled = EditorApplication.isPlaying;
            GUI.backgroundColor = new Color(0.5f, 1f, 0.6f);
            if (GUILayout.Button("开始记录", GUILayout.Height(28)))
                StartRecording();
            GUI.backgroundColor = Color.white;
            GUI.enabled = true;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField("状态", _isRecording ? "记录中…" : "未记录", _valueStyle);
        EditorGUILayout.LabelField("已采样行数", _recordLines.Count.ToString("N0"), _valueStyle);

        if (!string.IsNullOrEmpty(_lastExportPath))
            EditorGUILayout.LabelField("上次导出", _lastExportPath, EditorStyles.miniLabel);

        EditorGUILayout.Space(8);
        EditorGUILayout.BeginHorizontal();
        GUI.enabled = _recordLines.Count > 0;
        if (GUILayout.Button("导出 CSV"))
            ExportCsv();
        if (GUILayout.Button("复制到剪贴板"))
            CopyToClipboard();
        if (GUILayout.Button("清空记录"))
        {
            _recordLines.Clear();
            _lastExportPath = null;
        }
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);
        EditorGUILayout.HelpBox(
            "按「采样间隔」写入一行：时间、FPS、帧时、DrawCalls、内存等。\n" +
            "适合对比优化前后、或长时间跑图时的性能曲线。",
            MessageType.Info);
    }

    void DrawStatRow(string label, string value)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(200));
        EditorGUILayout.LabelField(value, _valueStyle);
        EditorGUILayout.EndHorizontal();
    }

    void DrawFpsGraph(float height)
    {
        Rect rect = GUILayoutUtility.GetRect(10, height, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(rect, new Color(0.12f, 0.12f, 0.14f));

        float maxFps = 30f;
        for (int i = 0; i < FpsHistoryLength; i++)
            if (_fpsHistory[i] > maxFps) maxFps = _fpsHistory[i];
        maxFps = Mathf.Max(maxFps, 60f);
        maxFps = Mathf.Ceil(maxFps / 10f) * 10f;

        Handles.color = new Color(1f, 1f, 1f, 0.08f);
        for (float y = 0; y <= maxFps; y += maxFps / 4f)
        {
            float ny = rect.yMax - (y / maxFps) * rect.height;
            Handles.DrawLine(new Vector3(rect.xMin, ny), new Vector3(rect.xMax, ny));
        }

        Handles.color = new Color(0.3f, 0.9f, 1f, 0.95f);
        Vector3? prev = null;
        for (int i = 0; i < FpsHistoryLength; i++)
        {
            int idx = (_fpsHistoryIndex + i) % FpsHistoryLength;
            float fps = _fpsHistory[idx];
            if (fps <= 0f) continue;

            float x = rect.xMin + (i / (float)(FpsHistoryLength - 1)) * rect.width;
            float y = rect.yMax - (fps / maxFps) * rect.height;
            var pt = new Vector3(x, y);
            if (prev.HasValue)
                Handles.DrawLine(prev.Value, pt);
            prev = pt;
        }

        GUI.Label(new Rect(rect.x + 4, rect.y + 2, 80, 18), $"0–{maxFps:F0} FPS", EditorStyles.miniLabel);
    }

    void StartRecording()
    {
        _recordLines.Clear();
        _recordLines.Add(GetCsvHeader());
        _isRecording = true;
    }

    static string GetCsvHeader() =>
        "TimeSec,Scene,FPS,AvgFPS,FrameMs,DrawCalls,Batches,SetPass,Triangles,Vertices," +
        "MemAllocated,MemReserved,MonoUsed,GfxMemory,GameObjects,ActiveGOs";

    void AppendRecordLine()
    {
        double t = EditorApplication.timeSinceStartup - _playStartTime;
        var scene = SceneManager.GetActiveScene().name;
        var sb = new StringBuilder();
        sb.Append(t.ToString("F2")).Append(',');
        sb.Append(EscapeCsv(scene)).Append(',');
        sb.Append(_fpsCurrent.ToString("F2")).Append(',');
        sb.Append(_fpsAvg.ToString("F2")).Append(',');
        sb.Append(_frameMs.ToString("F3")).Append(',');
        sb.Append(UnityStats.drawCalls).Append(',');
        sb.Append(UnityStats.batches).Append(',');
        sb.Append(UnityStats.setPassCalls).Append(',');
        sb.Append(UnityStats.triangles).Append(',');
        sb.Append(UnityStats.vertices).Append(',');
        sb.Append(Profiler.GetTotalAllocatedMemoryLong()).Append(',');
        sb.Append(Profiler.GetTotalReservedMemoryLong()).Append(',');
        sb.Append(Profiler.GetMonoUsedSizeLong()).Append(',');
        sb.Append(GetGfxMemory()).Append(',');
        sb.Append(_goCount).Append(',');
        sb.Append(_activeGoCount);
        _recordLines.Add(sb.ToString());
    }

    static string EscapeCsv(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        if (s.IndexOfAny(new[] { ',', '"', '\n' }) < 0) return s;
        return "\"" + s.Replace("\"", "\"\"") + "\"";
    }

    void ExportCsv()
    {
        string dir = Path.Combine(Application.dataPath, "..", "PerformanceLogs");
        Directory.CreateDirectory(dir);
        string fileName = $"perf_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        string path = Path.Combine(dir, fileName);
        File.WriteAllLines(path, _recordLines, Encoding.UTF8);
        _lastExportPath = path;
        Debug.Log($"[性能监视器] 已导出 { _recordLines.Count } 行 → {path}");
        EditorUtility.RevealInFinder(path);
    }

    void CopyToClipboard()
    {
        EditorGUIUtility.systemCopyBuffer = string.Join("\n", _recordLines);
        Debug.Log($"[性能监视器] 已复制 {_recordLines.Count} 行到剪贴板");
    }
}
#endif
