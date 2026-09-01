#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public static class LocalizationTranslateRunner
{
    private const string DefaultGlossaryPath = "Assets/Editor/Localization/glossary.csv";

    public static async Task RunAsync(
        LocalizationTranslateSettings settings,
        string csvFilePath,
        List<int> docRowIndices,
        Action<float, string> onProgress = null)
    {
        if (settings == null)
            throw new ArgumentNullException(nameof(settings));
        if (string.IsNullOrEmpty(csvFilePath) || !File.Exists(csvFilePath))
            throw new Exception("请先选择有效的 CSV 文件。");
        if (docRowIndices == null || docRowIndices.Count == 0)
            throw new Exception("没有选中任何行。");

        onProgress?.Invoke(0f, "读取 CSV…");

        var doc = LocalizationCsvIO.Read(csvFilePath);
        var toTranslate = new List<LocalizationCsvIO.Row>();
        foreach (int idx in docRowIndices)
        {
            if (idx >= 0 && idx < doc.Rows.Count)
                toTranslate.Add(doc.Rows[idx]);
        }

        if (toTranslate.Count == 0)
            throw new Exception("选中的行在 CSV 中未找到，请刷新后重试。");

        string glossaryPath = string.IsNullOrEmpty(settings.glossaryCsvPath)
            ? Path.GetFullPath(DefaultGlossaryPath)
            : Path.GetFullPath(settings.glossaryCsvPath);
        string glossaryText = OpenAiTranslateClient.BuildGlossarySection(glossaryPath);
        var writeTargets = settings.writeTargets ?? new LocalizationWriteTargets();
        settings.EnsureInitialized();
        writeTargets = settings.writeTargets;
        if (!writeTargets.AnySelected)
            throw new Exception("请至少勾选一门要写入的语言。");

        string systemPrompt = settings.GetActivePromptTemplate().Replace("{glossary}", glossaryText);
        Debug.Log($"[本地化翻译] 写入列：{writeTargets.GetSummaryLabel()}");

        string baseUrl = OpenAiTranslateClient.NormalizeBaseUrl(settings.baseUrl);
        if (baseUrl != settings.baseUrl.Trim().TrimEnd('/'))
        {
            settings.baseUrl = baseUrl;
            Debug.Log($"[本地化翻译] Base URL 已自动修正为: {baseUrl}");
        }

        int batchSize = Mathf.Max(1, settings.batchSize);
        int totalBatches = (toTranslate.Count + batchSize - 1) / batchSize;

        for (int b = 0; b < totalBatches; b++)
        {
            int start = b * batchSize;
            int count = Mathf.Min(batchSize, toTranslate.Count - start);
            var batchRows = toTranslate.GetRange(start, count);
            var texts = new List<string>();
            foreach (var r in batchRows)
                texts.Add(r.Chinese);

            onProgress?.Invoke(
                (float)(b + 1) / totalBatches,
                $"翻译中 {b + 1}/{totalBatches} 批（{start + 1}-{start + count} / {toTranslate.Count}）");

            var results = await OpenAiTranslateClient.TranslateBatchAsync(
                settings.apiKey,
                baseUrl,
                settings.model,
                systemPrompt,
                texts,
                writeTargets);

            for (int i = 0; i < batchRows.Count; i++)
            {
                var before = CaptureWrittenFields(batchRows[i], writeTargets);
                ApplyTranslationResult(batchRows[i], results[i], writeTargets, settings.replaceCommaWithC);
                WarnIfNothingWritten(batchRows[i], results[i], writeTargets, before);

                if (writeTargets.AllSelected)
                    batchRows[i].Standard = batchRows[i].Chinese;
            }
        }

        LocalizationCsvIO.Write(csvFilePath, doc);

        var baseline = LocalizationCsvBaseline.Load(csvFilePath);
        LocalizationCsvBaseline.UpdateAfterTranslation(csvFilePath, baseline, toTranslate);

        if (csvFilePath.Replace('\\', '/').Contains("/Assets/"))
            AssetDatabase.Refresh();

        EditorUtility.SetDirty(settings);
    }

    private struct WrittenSnapshot
    {
        public string ChineseF;
        public string English;
        public string Japanese;
        public string Korean;
    }

    private static WrittenSnapshot CaptureWrittenFields(LocalizationCsvIO.Row row, LocalizationWriteTargets targets)
        => new WrittenSnapshot
        {
            ChineseF = targets.chineseF ? row.ChineseF : null,
            English = targets.english ? row.English : null,
            Japanese = targets.japanese ? row.Japanese : null,
            Korean = targets.korean ? row.Korean : null
        };

    private static void WarnIfNothingWritten(
        LocalizationCsvIO.Row row,
        TranslationItem result,
        LocalizationWriteTargets targets,
        WrittenSnapshot before)
    {
        bool wrote = false;
        if (targets.chineseF && row.ChineseF != before.ChineseF && !string.IsNullOrWhiteSpace(row.ChineseF)) wrote = true;
        if (targets.english && row.English != before.English && !string.IsNullOrWhiteSpace(row.English)) wrote = true;
        if (targets.japanese && row.Japanese != before.Japanese && !string.IsNullOrWhiteSpace(row.Japanese)) wrote = true;
        if (targets.korean && row.Korean != before.Korean && !string.IsNullOrWhiteSpace(row.Korean)) wrote = true;

        if (wrote)
            return;

        Debug.LogWarning(
            $"[本地化翻译] 行「{row.Key}」未写入任何译文。解析结果：" +
            $"繁体={NullOrPreview(result.traditional)} " +
            $"英文={NullOrPreview(result.english)} " +
            $"日文={NullOrPreview(result.japanese)} " +
            $"韩文={NullOrPreview(result.korean)}");
    }

    private static string NullOrPreview(string s)
        => string.IsNullOrWhiteSpace(s) ? "(空)" : TruncatePreview(s);

    private static string TruncatePreview(string s)
        => s.Length <= 24 ? s : s.Substring(0, 24) + "…";

    private static void ApplyTranslationResult(
        LocalizationCsvIO.Row row,
        TranslationItem result,
        LocalizationWriteTargets targets,
        bool replaceCommaWithC)
    {
        if (targets.chineseF && !string.IsNullOrWhiteSpace(result.traditional))
            row.ChineseF = result.traditional.Trim();

        if (targets.english && !string.IsNullOrWhiteSpace(result.english))
        {
            string en = result.english.Trim();
            if (replaceCommaWithC)
                en = LocalizationCsvIO.ReplaceEnglishCommas(en);
            row.English = en;
        }

        if (targets.japanese && !string.IsNullOrWhiteSpace(result.japanese))
            row.Japanese = result.japanese.Trim();

        if (targets.korean && !string.IsNullOrWhiteSpace(result.korean))
            row.Korean = result.korean.Trim();
    }
}

#endif
