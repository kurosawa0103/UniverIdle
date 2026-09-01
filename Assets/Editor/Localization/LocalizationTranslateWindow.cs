#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public class LocalizationTranslateWindow : EditorWindow
{
    private const string SettingsPath = "Assets/Editor/Localization/LocalizationTranslateSettings.asset";
    private const string DefaultGlossaryPath = "Assets/Editor/Localization/glossary.csv";
    private const string PrefsApiKey = "MemeEcho_Localization_ApiKey";

    private LocalizationTranslateSettings settings;
    private string csvFilePath = "";
    private Vector2 scroll;
    private string status = "拖入 CSV 或点击选择文件，将打开行选择窗口";

    [MenuItem("工具/CSV 本地化翻译")]
    public static void Open()
    {
        var win = GetWindow<LocalizationTranslateWindow>("CSV 本地化翻译");
        win.minSize = new Vector2(480, 520);
        win.Show();
    }

    private void OnEnable()
    {
        settings = LoadOrCreateSettings();
        if (settings != null && string.IsNullOrEmpty(settings.apiKey))
            settings.apiKey = EditorPrefs.GetString(PrefsApiKey, "");
    }

    private void OnGUI()
    {
        DrawDropArea();
        EditorGUILayout.Space(8);

        scroll = EditorGUILayout.BeginScrollView(scroll);

        settings = (LocalizationTranslateSettings)EditorGUILayout.ObjectField(
            "设置", settings, typeof(LocalizationTranslateSettings), false);

        if (settings == null)
        {
            EditorGUILayout.HelpBox("未找到设置资源，请点击下方创建。", MessageType.Warning);
            if (GUILayout.Button("创建设置资源"))
                settings = LoadOrCreateSettings();
            EditorGUILayout.EndScrollView();
            return;
        }

        settings.EnsureInitialized();

        EditorGUILayout.LabelField("CSV 文件", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        csvFilePath = EditorGUILayout.TextField(csvFilePath);
        if (GUILayout.Button("浏览", GUILayout.Width(60)))
            BrowseCsv();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("API 中转站", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "三项必须来自同一中转平台控制台：\n" +
            "• Base URL — API 根地址，一般到 /v1（不要填网站首页，不要含 chat/completions）\n" +
            "• API Key — 中转提供的密钥\n" +
            "• 模型 — 中转文档里的模型名（如 gpt-4o-mini）\n\n" +
            "示例：Base URL = https://你的中转.com/v1\n" +
            "程序会自动请求：…/v1/chat/completions",
            MessageType.Info);

        settings.apiKey = EditorGUILayout.PasswordField("API Key", settings.apiKey);
        settings.baseUrl = EditorGUILayout.TextField("Base URL", settings.baseUrl);
        EditorGUILayout.LabelField(" ", "只填中转文档里的根地址，到 /v1 为止", EditorStyles.miniLabel);

        settings.model = EditorGUILayout.TextField("模型", settings.model);
        settings.batchSize = EditorGUILayout.IntSlider("批次大小", settings.batchSize, 1, 30);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("规则", EditorStyles.boldLabel);
        settings.replaceCommaWithC = EditorGUILayout.Toggle("英文逗号 → {c}", settings.replaceCommaWithC);
        settings.onlyUntranslatedRows = EditorGUILayout.Toggle(
            "打开行选择时默认勾选待处理行（缺失译文或原文已改）",
            settings.onlyUntranslatedRows);
        if (settings.writeTargets == null)
            settings.writeTargets = new LocalizationWriteTargets();
        LocalizationWriteTargets.DrawEditorGui(settings.writeTargets);

        var glossaryObj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(settings.glossaryCsvPath);
        var newGlossary = EditorGUILayout.ObjectField("术语库 CSV", glossaryObj, typeof(UnityEngine.Object), false);
        if (newGlossary != null)
            settings.glossaryCsvPath = AssetDatabase.GetAssetPath(newGlossary);
        else if (GUILayout.Button("使用默认术语库", GUILayout.Width(120)))
            settings.glossaryCsvPath = DefaultGlossaryPath;

        EditorGUILayout.LabelField("Prompt", EditorStyles.boldLabel);
        settings.promptExtraContext = EditorGUILayout.TextArea(
            settings.promptExtraContext,
            GUILayout.Height(48));
        EditorGUILayout.LabelField(" ", "可选：游戏风格说明，会附加到自动 Prompt", EditorStyles.miniLabel);

        EditorGUILayout.HelpBox("Prompt 会根据「写入列」自动生成，确保输出格式包含所选语言（含韩文）。", MessageType.Info);
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.TextArea(settings.GetActivePromptTemplate(), GUILayout.Height(120));
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space(8);

        EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(csvFilePath));
        if (GUILayout.Button("选择要翻译的行…", GUILayout.Height(36)))
            OpenRowSelectWindow();
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space(4);
        if (GUILayout.Button("批量建立目录下全部 CSV 翻译基线…"))
            LocalizationCsvBaseline.MenuRebuildDirectoryBaselines();

        EditorGUILayout.HelpBox(status, MessageType.Info);

        EditorGUILayout.EndScrollView();

        if (GUI.changed)
        {
            EditorPrefs.SetString(PrefsApiKey, settings.apiKey);
            EditorUtility.SetDirty(settings);
        }
    }

    private void DrawDropArea()
    {
        var rect = GUILayoutUtility.GetRect(0, 72, GUILayout.ExpandWidth(true));
        GUI.Box(rect, "拖拽 CSV 到此处\n（项目格式：Key, Chinese, ChineseF, English, Japanese, Korean...）");

        var evt = Event.current;
        if (!rect.Contains(evt.mousePosition))
            return;

        switch (evt.type)
        {
            case EventType.DragUpdated:
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                evt.Use();
                break;
            case EventType.DragPerform:
                DragAndDrop.AcceptDrag();
                foreach (var obj in DragAndDrop.objectReferences)
                {
                    string path = AssetDatabase.GetAssetPath(obj);
                    if (!string.IsNullOrEmpty(path) && path.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                    {
                        OnCsvSelected(Path.GetFullPath(path));
                        break;
                    }
                }
                if (string.IsNullOrEmpty(csvFilePath) && DragAndDrop.paths != null && DragAndDrop.paths.Length > 0)
                {
                    string p = DragAndDrop.paths[0];
                    if (p.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                        OnCsvSelected(Path.GetFullPath(p));
                }
                evt.Use();
                Repaint();
                break;
        }
    }

    private void BrowseCsv()
    {
        string projectDir = Path.GetFullPath(Path.Combine(Application.dataPath, "Resources/Language"));
        string path = EditorUtility.OpenFilePanel("选择 CSV", projectDir, "csv");
        if (!string.IsNullOrEmpty(path))
            OnCsvSelected(path);
    }

    private void OnCsvSelected(string path)
    {
        csvFilePath = path;
        status = "已选择: " + Path.GetFileName(path) + " — 请在行选择窗口中勾选要翻译的行";
        OpenRowSelectWindow();
    }

    private void OpenRowSelectWindow()
    {
        if (settings == null || string.IsNullOrEmpty(csvFilePath) || !File.Exists(csvFilePath))
        {
            EditorUtility.DisplayDialog("错误", "请先选择有效的 CSV 文件。", "确定");
            return;
        }

        LocalizationRowSelectWindow.Show(csvFilePath, settings);
    }

    private static LocalizationTranslateSettings LoadOrCreateSettings()
    {
        var s = AssetDatabase.LoadAssetAtPath<LocalizationTranslateSettings>(SettingsPath);
        if (s != null)
            return s;

        string dir = Path.GetDirectoryName(SettingsPath);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        s = CreateInstance<LocalizationTranslateSettings>();
        AssetDatabase.CreateAsset(s, SettingsPath);
        AssetDatabase.SaveAssets();

        if (!File.Exists(DefaultGlossaryPath))
        {
            File.WriteAllText(
                Path.GetFullPath(DefaultGlossaryPath),
                "简体中文,繁体中文,英文,日文\n赛博流,賽博流,cyber-stream,サイバー流\n",
                new System.Text.UTF8Encoding(true));
            AssetDatabase.Refresh();
        }

        return s;
    }
}

#endif
