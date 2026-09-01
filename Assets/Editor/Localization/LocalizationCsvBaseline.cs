#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 记录每个 Key 在「上次成功翻译」时的简体中文，用于检测原文是否被改过。
/// 与 CSV 内 Standard 列独立，避免导出/手改 CSV 时把快照一起改掉。
/// </summary>
public static class LocalizationCsvBaseline
{
    [Serializable]
    private class BaselineData
    {
        public string csvPath;
        public List<Entry> entries = new List<Entry>();
    }

    [Serializable]
    private class Entry
    {
        public string key;
        public string chinese;
    }

    private static string BaselinesDir =>
        Path.GetFullPath(Path.Combine(Application.dataPath, "Editor/Localization/Baselines"));

    public static string GetBaselineFilePath(string csvFullPath)
    {
        string safeName = Path.GetFileNameWithoutExtension(csvFullPath);
        foreach (char c in Path.GetInvalidFileNameChars())
            safeName = safeName.Replace(c, '_');
        return Path.Combine(BaselinesDir, safeName + ".baseline.json");
    }

    public static Dictionary<string, string> Load(string csvFullPath)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        string path = GetBaselineFilePath(csvFullPath);
        if (!File.Exists(path))
            return map;

        try
        {
            string json = File.ReadAllText(path, Encoding.UTF8);
            var data = JsonUtility.FromJson<BaselineData>(json);
            if (data?.entries == null)
                return map;

            foreach (var e in data.entries)
            {
                if (!string.IsNullOrEmpty(e.key))
                    map[e.key] = e.chinese ?? "";
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[本地化基线] 读取失败 {path}: {ex.Message}");
        }

        return map;
    }

    public static void Save(string csvFullPath, Dictionary<string, string> map)
    {
        if (map == null)
            return;

        string dir = BaselinesDir;
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var data = new BaselineData { csvPath = csvFullPath.Replace('\\', '/') };
        foreach (var kv in map)
        {
            if (string.IsNullOrEmpty(kv.Key))
                continue;
            data.entries.Add(new Entry { key = kv.Key, chinese = kv.Value ?? "" });
        }

        string path = GetBaselineFilePath(csvFullPath);
        File.WriteAllText(path, JsonUtility.ToJson(data, true), new UTF8Encoding(false));
    }

    /// <summary>
    /// 为已有完整译文的行建立基线（首次打开 CSV 时自动调用一次）。
    /// </summary>
    public static int BootstrapFromDocument(string csvFullPath, LocalizationCsvIO.Document doc, Dictionary<string, string> map)
    {
        int added = 0;
        foreach (var row in doc.Rows)
        {
            if (string.IsNullOrEmpty(row.Key) || string.IsNullOrWhiteSpace(row.Chinese))
                continue;
            if (!LocalizationCsvIO.HasCompleteTranslation(row))
                continue;
            if (map.ContainsKey(row.Key))
                continue;

            map[row.Key] = row.Chinese;
            added++;
        }

        if (added > 0)
            Save(csvFullPath, map);
        return added;
    }

    public static void UpdateAfterTranslation(
        string csvFullPath,
        Dictionary<string, string> map,
        IEnumerable<LocalizationCsvIO.Row> translatedRows)
    {
        foreach (var row in translatedRows)
        {
            if (string.IsNullOrEmpty(row.Key))
                continue;
            map[row.Key] = row.Chinese ?? "";
        }

        Save(csvFullPath, map);
    }

    /// <summary>用当前 CSV 中已有完整译文的行覆盖全部基线。</summary>
    public static Dictionary<string, string> RebuildAll(string csvFullPath, LocalizationCsvIO.Document doc)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var row in doc.Rows)
        {
            if (string.IsNullOrEmpty(row.Key) || !LocalizationCsvIO.HasCompleteTranslation(row))
                continue;
            map[row.Key] = row.Chinese ?? "";
        }

        Save(csvFullPath, map);
        return map;
    }

    public struct BatchRebuildResult
    {
        public int FilesProcessed;
        public int FilesFailed;
        public int TotalEntries;
        public List<string> Errors;
    }

    /// <summary>为目录下所有 CSV（可递归）重建翻译基线。</summary>
    public static BatchRebuildResult RebuildDirectory(string directoryPath, bool recursive = false)
    {
        var result = new BatchRebuildResult { Errors = new List<string>() };
        if (string.IsNullOrEmpty(directoryPath) || !Directory.Exists(directoryPath))
        {
            result.Errors.Add("目录不存在。");
            result.FilesFailed = 1;
            return result;
        }

        var search = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        foreach (string csvPath in Directory.GetFiles(directoryPath, "*.csv", search))
        {
            if (ShouldSkipCsv(csvPath))
                continue;

            try
            {
                var doc = LocalizationCsvIO.Read(csvPath);
                var map = RebuildAll(csvPath, doc);
                result.FilesProcessed++;
                result.TotalEntries += map.Count;
            }
            catch (Exception ex)
            {
                result.FilesFailed++;
                result.Errors.Add($"{Path.GetFileName(csvPath)}: {ex.Message}");
            }
        }

        return result;
    }

    private static bool ShouldSkipCsv(string csvPath)
    {
        string name = Path.GetFileName(csvPath);
        if (name.Equals("glossary.csv", StringComparison.OrdinalIgnoreCase))
            return true;
        return false;
    }

    [MenuItem("工具/批量建立 CSV 翻译基线")]
    public static void MenuRebuildDirectoryBaselines()
    {
        const string prefsKey = "MemeEcho_Localization_BaselineDir";
        string defaultDir = Path.GetFullPath(Path.Combine(Application.dataPath, "Resources/Language"));
        string lastDir = EditorPrefs.GetString(prefsKey, defaultDir);
        if (!Directory.Exists(lastDir))
            lastDir = defaultDir;

        string folder = EditorUtility.OpenFolderPanel("选择 CSV 目录（将为此目录下所有 CSV 写入基线）", lastDir, "");
        if (string.IsNullOrEmpty(folder))
            return;

        EditorPrefs.SetString(prefsKey, folder);

        bool recursive = EditorUtility.DisplayDialog(
            "批量建立翻译基线",
            "是否为子文件夹内的 CSV 也建立基线？\n\n" +
            "是 = 包含子目录\n否 = 仅当前目录",
            "包含子目录",
            "仅当前目录");

        if (!EditorUtility.DisplayDialog(
                "确认",
                $"将为以下目录写入/覆盖翻译基线：\n{folder}\n\n" +
                "规则：每个 CSV 中「已有完整译文（繁/英/日）」的行，以当前 Chinese 写入基线。\n\n继续？",
                "开始",
                "取消"))
            return;

        var result = RebuildDirectory(folder, recursive);

        string msg = $"处理完成\n\n" +
                     $"成功：{result.FilesProcessed} 个 CSV\n" +
                     $"共 {result.TotalEntries} 条基线\n" +
                     $"失败：{result.FilesFailed} 个";

        if (result.Errors != null && result.Errors.Count > 0)
            msg += "\n\n失败详情：\n" + string.Join("\n", result.Errors);

        EditorUtility.DisplayDialog(
            result.FilesFailed > 0 ? "完成（部分失败）" : "完成",
            msg,
            "确定");

        Debug.Log($"[本地化基线] 批量建立完成：{result.FilesProcessed} 文件，{result.TotalEntries} 条。" +
                  (result.FilesFailed > 0 ? $" 失败 {result.FilesFailed} 个。" : ""));
    }

    public static bool IsSourceChanged(LocalizationCsvIO.Row row, IReadOnlyDictionary<string, string> baselineByKey)
    {
        if (string.IsNullOrWhiteSpace(row.Chinese))
            return false;

        if (baselineByKey != null
            && !string.IsNullOrEmpty(row.Key)
            && baselineByKey.TryGetValue(row.Key, out string snapshot)
            && !string.IsNullOrWhiteSpace(snapshot))
        {
            return !string.Equals(row.Chinese.Trim(), snapshot.Trim(), StringComparison.Ordinal);
        }

        return LocalizationCsvIO.IsSourceChangedFromStandard(row);
    }
}

#endif
