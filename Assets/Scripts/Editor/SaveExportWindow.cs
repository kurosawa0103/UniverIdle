using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class SaveExportWindow : EditorWindow
{
    private string exportRootDir = "";
    private string exportFolderName = "";
    private bool includeSaveMgrDat = true;
    private bool includePlayerPrefs = true;
    private string importSourceDir = "";
    private bool backupBeforeImport = true;
    private bool importPlayerPrefs = true;
    private bool clearAllPlayerPrefsBeforeImport = false;
    private Vector2 scroll;
    private const string ProjectSaveHubRelative = "存档";
    private const string PlayerPrefsFileName = "playerprefs.json";

    [Serializable]
    private class PlayerPrefsExport
    {
        public string companyName;
        public string productName;
        public List<PlayerPrefsEntry> entries = new List<PlayerPrefsEntry>();
    }

    [Serializable]
    private class PlayerPrefsEntry
    {
        public string key;
        public string type;  // int / float / string
        public string value; // 始终用字符串存
    }

    [MenuItem("工具/存档导出")]
    public static void ShowWindow()
    {
        GetWindow<SaveExportWindow>("存档导出");
    }

    private void OnEnable()
    {
        if (string.IsNullOrWhiteSpace(exportFolderName))
        {
            exportFolderName = $"SaveExport_{DateTime.Now:yyyyMMdd_HHmmss}";
        }

        // 默认把“导出/导入目录”指向项目下的“存档”文件夹（如果存在）
        var hub = GetProjectSaveHubPath();
        if (Directory.Exists(hub))
        {
            if (string.IsNullOrWhiteSpace(exportRootDir))
            {
                exportRootDir = hub;
            }
            if (string.IsNullOrWhiteSpace(importSourceDir))
            {
                importSourceDir = hub;
            }
        }
    }

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        EditorGUILayout.LabelField("一键导出存档到指定目录（并重命名文件夹）", EditorStyles.boldLabel);
        EditorGUILayout.Space(8);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("使用项目/存档目录", GUILayout.Width(140)))
            {
                var hub = GetProjectSaveHubPath();
                Directory.CreateDirectory(hub);
                exportRootDir = hub;
                importSourceDir = hub;
                GUI.FocusControl(null);
            }

            if (GUILayout.Button("打开项目/存档目录", GUILayout.Width(140)))
            {
                var hub = GetProjectSaveHubPath();
                Directory.CreateDirectory(hub);
                EditorUtility.RevealInFinder(hub);
            }
        }

        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("导出目标目录（会在其下创建导出文件夹）", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.TextField(exportRootDir);
                if (GUILayout.Button("选择...", GUILayout.Width(80)))
                {
                    var picked = EditorUtility.OpenFolderPanel("选择导出目标目录", exportRootDir, "");
                    if (!string.IsNullOrEmpty(picked))
                    {
                        exportRootDir = picked;
                        GUI.FocusControl(null);
                    }
                }
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("导出文件夹名（即“重命名”的名字）", EditorStyles.boldLabel);
            exportFolderName = EditorGUILayout.TextField(exportFolderName);
            EditorGUILayout.HelpBox("会自动清理非法字符（Windows 文件夹名不能包含 \\ / : * ? \" < > |）。", MessageType.Info);
        }

        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("导出内容", EditorStyles.boldLabel);
            includeSaveMgrDat = EditorGUILayout.ToggleLeft("包含自定义存档 savedata.dat（persistentDataPath）", includeSaveMgrDat);
            includePlayerPrefs = EditorGUILayout.ToggleLeft("包含 PlayerPrefs（导出为 playerprefs.json）", includePlayerPrefs);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("当前存档路径（只读）", EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(GetSaveMgrDatPath(), EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
        }

        EditorGUILayout.Space(8);
        using (new EditorGUI.DisabledScope(!CanExport()))
        {
            if (GUILayout.Button("导出", GUILayout.Height(32)))
            {
                DoExport();
            }
        }

        if (!CanExport())
        {
            EditorGUILayout.HelpBox("请选择导出目标目录，并至少勾选一项导出内容。", MessageType.Warning);
        }

        EditorGUILayout.Space(16);
        EditorGUILayout.LabelField("导入（覆盖当前存档）", EditorStyles.boldLabel);

        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("存档目录（目录下应包含 savedata.dat 或 *.sav）", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.TextField(importSourceDir);
                if (GUILayout.Button("选择...", GUILayout.Width(80)))
                {
                    var picked = EditorUtility.OpenFolderPanel("选择要导入的存档目录", importSourceDir, "");
                    if (!string.IsNullOrEmpty(picked))
                    {
                        importSourceDir = picked;
                        GUI.FocusControl(null);
                    }
                }
            }

            backupBeforeImport = EditorGUILayout.ToggleLeft("导入前自动备份当前存档", backupBeforeImport);
            importPlayerPrefs = EditorGUILayout.ToggleLeft("同时导入 PlayerPrefs（如果目录中有 playerprefs.json）", importPlayerPrefs);
            using (new EditorGUI.DisabledScope(!importPlayerPrefs))
            {
                clearAllPlayerPrefsBeforeImport = EditorGUILayout.ToggleLeft("导入 PlayerPrefs 前先清空全部 PlayerPrefs", clearAllPlayerPrefsBeforeImport);
            }
            EditorGUILayout.HelpBox("导入会把选中目录下的存档文件复制到 persistentDataPath 并覆盖现有文件。", MessageType.Warning);

            using (new EditorGUI.DisabledScope(!CanImport()))
            {
                if (GUILayout.Button("导入并覆盖", GUILayout.Height(32)))
                {
                    DoImport();
                }
            }

            if (!CanImport())
            {
                EditorGUILayout.HelpBox("请选择一个有效的存档目录（包含 savedata.dat 或 *.sav）。", MessageType.Info);
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private bool CanExport()
    {
        if (string.IsNullOrWhiteSpace(exportRootDir))
        {
            return false;
        }

        if (!includeSaveMgrDat && !includePlayerPrefs)
        {
            return false;
        }

        return true;
    }

    private static string GetSaveMgrDatPath()
    {
        return Path.Combine(Application.persistentDataPath, "savedata.dat");
    }

    private static string GetProjectSaveHubPath()
    {
        // 例如：E:/MemeEcho/存档
        return Path.Combine(Directory.GetParent(Application.dataPath).FullName, ProjectSaveHubRelative);
    }

    private bool CanImport()
    {
        if (string.IsNullOrWhiteSpace(importSourceDir) || !Directory.Exists(importSourceDir))
        {
            return false;
        }

        if (TryFindImportSource(importSourceDir, out _, out _))
        {
            return true;
        }

        if (importPlayerPrefs && TryFindPlayerPrefsFile(importSourceDir, out _))
        {
            return true;
        }

        return false;
    }

    private void DoExport()
    {
        try
        {
            var safeFolderName = SanitizeFolderName(exportFolderName);
            if (string.IsNullOrWhiteSpace(safeFolderName))
            {
                safeFolderName = $"SaveExport_{DateTime.Now:yyyyMMdd_HHmmss}";
            }

            var destDir = Path.Combine(exportRootDir, safeFolderName);
            Directory.CreateDirectory(destDir);

            var copiedAny = false;

            if (includeSaveMgrDat)
            {
                var src = GetSaveMgrDatPath();
                if (File.Exists(src))
                {
                    File.Copy(src, Path.Combine(destDir, "savedata.dat"), true);
                    copiedAny = true;
                }
            }

            if (includePlayerPrefs)
            {
                var exportPath = Path.Combine(destDir, PlayerPrefsFileName);
                ExportPlayerPrefs(exportPath);
                copiedAny = true;
            }

            if (!copiedAny)
            {
                EditorUtility.DisplayDialog("存档导出", "没有找到可导出的存档文件/文件夹（源路径不存在或为空）。", "确定");
                return;
            }

            EditorUtility.RevealInFinder(destDir);
            EditorUtility.DisplayDialog("存档导出", $"导出完成：\n{destDir}", "确定");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorUtility.DisplayDialog("存档导出失败", ex.Message, "确定");
        }
    }

    private static string SanitizeFolderName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "";
        }

        var invalid = Path.GetInvalidFileNameChars();
        foreach (var c in invalid)
        {
            name = name.Replace(c.ToString(), "");
        }

        // 额外清理一些在 Windows 上常见的问题字符/空白
        name = name.Trim();
        name = name.Replace(".", "_");
        while (name.Contains("  "))
        {
            name = name.Replace("  ", " ");
        }

        return name;
    }

    private void DoImport()
    {
        try
        {
            if (!CanImport())
            {
                EditorUtility.DisplayDialog("存档导入", "导入目录无效，或未找到 savedata.dat / *.sav / playerprefs.json。", "确定");
                return;
            }

            var dest = GetSaveMgrDatPath();

            var hasSaveFile = TryFindImportSource(importSourceDir, out var sourceToCopy, out var sourceName);
            string prefsPath = null;
            var hasPrefsFile = importPlayerPrefs && TryFindPlayerPrefsFile(importSourceDir, out prefsPath);

            if (!hasSaveFile && !hasPrefsFile)
            {
                EditorUtility.DisplayDialog("存档导入", "没有找到可导入的内容（存档文件或 PlayerPrefs）。", "确定");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    "确认导入",
                    $"将执行导入覆盖：\n- 存档文件：{(hasSaveFile ? sourceName : "（跳过）")}\n- PlayerPrefs：{(hasPrefsFile ? Path.GetFileName(prefsPath) : "（跳过）")}\n\n此操作会覆盖当前数据。",
                    "继续",
                    "取消"))
            {
                return;
            }

            if (hasSaveFile)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(dest));

                if (backupBeforeImport && File.Exists(dest))
                {
                    var backupDir = Path.Combine(Application.persistentDataPath, "SaveBackups");
                    Directory.CreateDirectory(backupDir);
                    var backupPath = Path.Combine(backupDir, $"savedata_{DateTime.Now:yyyyMMdd_HHmmss}.bak");
                    File.Copy(dest, backupPath, true);
                }

                File.Copy(sourceToCopy, dest, true);
            }

            if (hasPrefsFile)
            {
                ImportPlayerPrefs(prefsPath, clearAllPlayerPrefsBeforeImport);
            }

            EditorUtility.DisplayDialog("存档导入", "导入完成。建议重新进入游戏/重启 Play 模式以确保数据重新加载。", "确定");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorUtility.DisplayDialog("存档导入失败", ex.Message, "确定");
        }
    }

    private static bool TryFindImportSource(string dir, out string sourcePath, out string sourceName)
    {
        sourcePath = null;
        sourceName = null;

        // 1) 目录内直接存在 savedata.dat
        var dat = Path.Combine(dir, "savedata.dat");
        if (File.Exists(dat))
        {
            sourcePath = dat;
            sourceName = "savedata.dat";
            return true;
        }

        // 2) 目录内有 .sav，则取最新修改的一个
        var savFiles = Directory.GetFiles(dir, "*.sav");
        if (savFiles != null && savFiles.Length > 0)
        {
            Array.Sort(savFiles, (a, b) => File.GetLastWriteTimeUtc(b).CompareTo(File.GetLastWriteTimeUtc(a)));
            sourcePath = savFiles[0];
            sourceName = Path.GetFileName(sourcePath);
            return true;
        }

        // 3) 如果用户选的是“存档中转站”根目录，则在其一级子目录中找最新的 savedata.dat
        var subDirs = Directory.GetDirectories(dir);
        if (subDirs == null || subDirs.Length == 0)
        {
            return false;
        }

        string best = null;
        DateTime bestTime = DateTime.MinValue;
        for (int i = 0; i < subDirs.Length; i++)
        {
            var candidate = Path.Combine(subDirs[i], "savedata.dat");
            if (!File.Exists(candidate))
            {
                continue;
            }

            var t = File.GetLastWriteTimeUtc(candidate);
            if (t > bestTime)
            {
                bestTime = t;
                best = candidate;
            }
        }

        if (best != null)
        {
            sourcePath = best;
            sourceName = $"savedata.dat（来自 {Path.GetFileName(Path.GetDirectoryName(best))}）";
            return true;
        }

        return false;
    }

    private static bool TryFindPlayerPrefsFile(string dir, out string prefsPath)
    {
        prefsPath = null;

        var direct = Path.Combine(dir, PlayerPrefsFileName);
        if (File.Exists(direct))
        {
            prefsPath = direct;
            return true;
        }

        var subDirs = Directory.GetDirectories(dir);
        if (subDirs == null || subDirs.Length == 0)
        {
            return false;
        }

        string best = null;
        DateTime bestTime = DateTime.MinValue;
        for (int i = 0; i < subDirs.Length; i++)
        {
            var candidate = Path.Combine(subDirs[i], PlayerPrefsFileName);
            if (!File.Exists(candidate))
            {
                continue;
            }

            var t = File.GetLastWriteTimeUtc(candidate);
            if (t > bestTime)
            {
                bestTime = t;
                best = candidate;
            }
        }

        if (best != null)
        {
            prefsPath = best;
            return true;
        }

        return false;
    }

    private static void ExportPlayerPrefs(string exportPath)
    {
        var prefs = new PlayerPrefsExport
        {
            companyName = PlayerSettings.companyName,
            productName = PlayerSettings.productName,
            entries = new List<PlayerPrefsEntry>()
        };

        var all = PlayerPrefsExtension.GetAll();
        for (int i = 0; i < all.Length; i++)
        {
            var pair = all[i];
            if (string.IsNullOrEmpty(pair.Key))
            {
                continue;
            }

            var entry = new PlayerPrefsEntry { key = pair.Key };
            var v = pair.Value;

            if (v is int vi)
            {
                entry.type = "int";
                entry.value = vi.ToString();
            }
            else if (v is float vf)
            {
                entry.type = "float";
                entry.value = vf.ToString("R");
            }
            else
            {
                entry.type = "string";
                entry.value = v != null ? v.ToString() : "";
            }

            prefs.entries.Add(entry);
        }

        var json = JsonUtility.ToJson(prefs, true);
        File.WriteAllText(exportPath, json);
    }

    private static void ImportPlayerPrefs(string prefsJsonPath, bool clearAllFirst)
    {
        if (!File.Exists(prefsJsonPath))
        {
            return;
        }

        var json = File.ReadAllText(prefsJsonPath);
        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        var data = JsonUtility.FromJson<PlayerPrefsExport>(json);
        if (data == null || data.entries == null)
        {
            return;
        }

        if (clearAllFirst)
        {
            PlayerPrefs.DeleteAll();
        }

        for (int i = 0; i < data.entries.Count; i++)
        {
            var e = data.entries[i];
            if (e == null || string.IsNullOrEmpty(e.key))
            {
                continue;
            }

            switch (e.type)
            {
                case "int":
                    if (int.TryParse(e.value, out var iv))
                    {
                        PlayerPrefs.SetInt(e.key, iv);
                    }
                    break;
                case "float":
                    if (float.TryParse(e.value, out var fv))
                    {
                        PlayerPrefs.SetFloat(e.key, fv);
                    }
                    break;
                default:
                    PlayerPrefs.SetString(e.key, e.value ?? "");
                    break;
            }
        }

        PlayerPrefs.Save();
    }

}

