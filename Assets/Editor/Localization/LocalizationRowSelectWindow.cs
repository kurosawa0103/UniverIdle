#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 选择 CSV 中需要 AI 翻译的行；仅勾选的行会参与翻译。
/// </summary>
public class LocalizationRowSelectWindow : EditorWindow
{
    private class RowEntry
    {
        public int DocRowIndex;
        public LocalizationCsvIO.Row Row;
        public bool Selected;
        public bool NeedsTranslation;
        public bool MissingKorean;
        public bool SourceChanged;
    }

    private string csvFilePath;
    private LocalizationTranslateSettings settings;
    private Dictionary<string, string> baselineByKey = new Dictionary<string, string>(StringComparer.Ordinal);
    private enum RowListFilter
    {
        All,
        Pending,
        MissingOnly,
        MissingKoreanOnly,
        ChangedOnly
    }

    private const string PrefHideCompleted = "MemeEcho.Localization.HideCompletedRows";

    private List<RowEntry> entries = new List<RowEntry>();
    private Vector2 scroll;
    private string filter = "";
    private RowListFilter listFilter = RowListFilter.All;
    private bool hideCompletedRows = true;
    private bool isTranslating;
    private float progress;
    private string status = "";

    public static void Show(string csvPath, LocalizationTranslateSettings translateSettings)
    {
        if (string.IsNullOrEmpty(csvPath) || translateSettings == null)
            return;

        var win = GetWindow<LocalizationRowSelectWindow>(true, "选择要翻译的行", true);
        win.minSize = new Vector2(640, 420);
        win.csvFilePath = csvPath;
        win.settings = translateSettings;
        translateSettings.EnsureInitialized();
        win.hideCompletedRows = EditorPrefs.GetBool(PrefHideCompleted, true);
        win.LoadRows();
        if (translateSettings.onlyUntranslatedRows)
            win.ApplyPendingFilterAndSelect(selectRows: true);
        win.Show();
    }

    private void LoadRows()
    {
        entries.Clear();
        status = "";

        if (string.IsNullOrEmpty(csvFilePath) || !File.Exists(csvFilePath))
        {
            status = "CSV 文件不存在。";
            return;
        }

        try
        {
            var doc = LocalizationCsvIO.Read(csvFilePath);
            baselineByKey = LocalizationCsvBaseline.Load(csvFilePath);

            bool hadBaseline = File.Exists(LocalizationCsvBaseline.GetBaselineFilePath(csvFilePath));
            int bootstrapped = LocalizationCsvBaseline.BootstrapFromDocument(csvFilePath, doc, baselineByKey);

            for (int i = 0; i < doc.Rows.Count; i++)
            {
                var row = doc.Rows[i];
                if (string.IsNullOrWhiteSpace(row.Chinese))
                    continue;

                bool needs = LocalizationCsvIO.NeedsTranslation(row);
                bool missingKorean = LocalizationCsvIO.NeedsKoreanTranslation(row);
                bool changed = LocalizationCsvIO.IsSourceChanged(row, baselineByKey);
                bool selected = settings.onlyUntranslatedRows
                    ? LocalizationCsvIO.ShouldRetranslateForTargets(row, settings.writeTargets, baselineByKey)
                    : true;

                entries.Add(new RowEntry
                {
                    DocRowIndex = i,
                    Row = row,
                    Selected = selected,
                    NeedsTranslation = needs,
                    MissingKorean = missingKorean,
                    SourceChanged = changed
                });
            }

            string baselineNote = !hadBaseline && bootstrapped > 0
                ? $"（已自动为 {bootstrapped} 行建立翻译基线，之后改中文即可标为「原文已改」）"
                : "";
            status = entries.Count == 0
                ? "没有含简体中文内容的行。"
                : $"共 {entries.Count} 行可翻译：{CountNeedsTranslation()} 行缺失译文，{CountMissingKorean()} 行缺韩语，{CountSourceChanged()} 行原文已改。{baselineNote}";

            if (hideCompletedRows)
                DeselectCompletedRows();
        }
        catch (Exception ex)
        {
            status = "读取失败: " + ex.Message;
            Debug.LogException(ex);
        }
    }

    private int CountNeedsTranslation()
    {
        int n = 0;
        foreach (var e in entries)
            if (e.NeedsTranslation) n++;
        return n;
    }

    private int CountSourceChanged()
    {
        int n = 0;
        foreach (var e in entries)
            if (e.SourceChanged) n++;
        return n;
    }

    private int CountMissingKorean()
    {
        int n = 0;
        foreach (var e in entries)
            if (e.MissingKorean) n++;
        return n;
    }

    private int CountSelected()
    {
        int n = 0;
        foreach (var e in entries)
            if (e.Selected) n++;
        return n;
    }

    private int CountPending()
    {
        int n = 0;
        foreach (var e in entries)
            if (e.NeedsTranslation || e.SourceChanged) n++;
        return n;
    }

    private int CountVisible()
    {
        string f = filter?.Trim() ?? "";
        int n = 0;
        foreach (var e in entries)
        {
            if (PassesFilter(e, f))
                n++;
        }
        return n;
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(4);
        settings.EnsureInitialized();
        if (settings.writeTargets == null)
            settings.writeTargets = new LocalizationWriteTargets();
        LocalizationWriteTargets.DrawEditorGui(settings.writeTargets);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("CSV", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(Path.GetFileName(csvFilePath ?? ""), EditorStyles.wordWrappedLabel);
        EditorGUILayout.Space(4);

        EditorGUILayout.BeginHorizontal();
        filter = EditorGUILayout.TextField("筛选 Key / 中文", filter);
        if (GUILayout.Button("刷新", GUILayout.Width(48)))
            LoadRows();
        EditorGUILayout.EndHorizontal();

        EditorGUI.BeginChangeCheck();
        hideCompletedRows = EditorGUILayout.ToggleLeft(
            "隐藏已本地化 OK（按当前写入列：译文齐且原文未改）",
            hideCompletedRows);
        if (EditorGUI.EndChangeCheck())
        {
            EditorPrefs.SetBool(PrefHideCompleted, hideCompletedRows);
            if (hideCompletedRows)
                DeselectCompletedRows();
            scroll = Vector2.zero;
        }

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("列表显示");
        var newFilter = (RowListFilter)GUILayout.Toolbar(
            (int)listFilter,
            new[] { "全部", "待处理", "缺失译文", "缺韩语", "原文已改" },
            GUILayout.Height(22));
        if (newFilter != listFilter)
        {
            listFilter = newFilter;
            scroll = Vector2.zero;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        var pendingStyle = listFilter == RowListFilter.Pending ? EditorStyles.toolbarButton : GUI.skin.button;
        if (GUILayout.Button("一键：待处理（筛选+全选）", pendingStyle, GUILayout.Height(24)))
            ApplyPendingFilterAndSelect(selectRows: true);
        if (GUILayout.Button("一键：缺韩语", GUILayout.Height(24)))
            ApplyMissingKoreanFilterAndSelect(selectRows: true);
        if (GUILayout.Button("显示全部", GUILayout.Width(72), GUILayout.Height(24)))
        {
            hideCompletedRows = false;
            EditorPrefs.SetBool(PrefHideCompleted, false);
            listFilter = RowListFilter.All;
            scroll = Vector2.zero;
        }
        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField(
            $"显示 {CountVisible()}/{entries.Count} · 待处理 {CountPending()} · 已选 {CountSelected()}",
            GUILayout.Width(280));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("全选可见", GUILayout.Width(72)))
            SetAllSelected(true);
        if (GUILayout.Button("全不选", GUILayout.Width(72)))
            SetAllSelected(false);
        if (GUILayout.Button("仅选可见待处理", GUILayout.Width(108)))
            SelectOnlyPending();
        if (GUILayout.Button("仅选可见缺韩语", GUILayout.Width(108)))
            SelectOnlyMissingKorean();
        if (GUILayout.Button("仅选可见原文已改", GUILayout.Width(120)))
            SelectOnlySourceChanged();
        if (GUILayout.Button("重建基线", GUILayout.Width(72)))
            RebuildBaseline();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);

        DrawHeaderRow();

        scroll = EditorGUILayout.BeginScrollView(scroll);
        string f = filter?.Trim() ?? "";
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            if (!PassesFilter(e, f))
                continue;
            DrawRowEntry(e, i, baselineByKey);
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(8);

        if (isTranslating)
            EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(false, 20), progress, status);
        else if (!string.IsNullOrEmpty(status))
            EditorGUILayout.HelpBox(status, MessageType.Info);

        EditorGUI.BeginDisabledGroup(
            isTranslating || CountSelected() == 0 || settings.writeTargets == null || !settings.writeTargets.AnySelected);
        if (GUILayout.Button($"开始翻译已选行（{CountSelected()}）", GUILayout.Height(32)))
            _ = RunTranslateSelectedAsync();
        EditorGUI.EndDisabledGroup();

        if (GUI.changed && settings != null)
            EditorUtility.SetDirty(settings);
    }

    private static void DrawHeaderRow()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("", GUILayout.Width(20));
        GUILayout.Label("Key", EditorStyles.boldLabel, GUILayout.Width(160));
        GUILayout.Label("简体中文", EditorStyles.boldLabel, GUILayout.MinWidth(120));
        GUILayout.Label("状态", EditorStyles.boldLabel, GUILayout.MinWidth(200));
        EditorGUILayout.EndHorizontal();
    }

    private static void DrawRowEntry(RowEntry e, int index, Dictionary<string, string> baseline)
    {
        EditorGUILayout.BeginHorizontal(index % 2 == 0 ? EditorStyles.helpBox : GUIStyle.none);

        e.Selected = EditorGUILayout.Toggle(e.Selected, GUILayout.Width(20));

        string key = string.IsNullOrEmpty(e.Row.Key) ? "(无 Key)" : e.Row.Key;
        EditorGUILayout.LabelField(key, GUILayout.Width(160));

        string preview = Truncate(e.Row.Chinese, 48);
        var previewRect = GUILayoutUtility.GetRect(new GUIContent(preview), EditorStyles.label, GUILayout.MinWidth(120));
        if (e.SourceChanged && baseline != null && !string.IsNullOrEmpty(e.Row.Key)
            && baseline.TryGetValue(e.Row.Key, out string oldCn))
            GUI.Label(previewRect, new GUIContent(preview, $"基线：{oldCn}\n当前：{e.Row.Chinese}"));
        else
            GUI.Label(previewRect, preview);

        BuildRowStatus(e, out string statusText, out Color color);
        var prev = GUI.color;
        GUI.color = color;
        EditorGUILayout.LabelField(statusText, GUILayout.MinWidth(200));
        GUI.color = prev;

        EditorGUILayout.EndHorizontal();
    }

    private static void BuildRowStatus(RowEntry e, out string statusText, out Color color)
    {
        var parts = new List<string>();
        if (e.SourceChanged)
            parts.Add("原文已改");
        if (LocalizationCsvIO.IsMissingChineseF(e.Row))
            parts.Add("缺繁体");
        if (LocalizationCsvIO.IsMissingEnglish(e.Row))
            parts.Add("缺英文");
        if (LocalizationCsvIO.IsMissingJapanese(e.Row))
            parts.Add("缺日文");
        if (LocalizationCsvIO.IsMissingKorean(e.Row))
            parts.Add("缺韩语");

        if (parts.Count == 0)
        {
            statusText = "已有译文";
            color = new Color(0.6f, 0.85f, 0.6f);
            return;
        }

        statusText = string.Join("+", parts);

        bool missingLegacy = e.NeedsTranslation || e.MissingKorean;
        if (e.SourceChanged)
            color = missingLegacy
                ? new Color(1f, 0.45f, 0.35f)
                : new Color(1f, 0.55f, 0.2f);
        else if (e.NeedsTranslation)
            color = new Color(1f, 0.75f, 0.3f);
        else
            color = new Color(0.55f, 0.75f, 1f);
    }

    private static bool PassesListFilter(RowEntry e, RowListFilter mode)
    {
        switch (mode)
        {
            case RowListFilter.Pending:
                return e.NeedsTranslation || e.SourceChanged;
            case RowListFilter.MissingOnly:
                return e.NeedsTranslation;
            case RowListFilter.MissingKoreanOnly:
                return e.MissingKorean;
            case RowListFilter.ChangedOnly:
                return e.SourceChanged;
            default:
                return true;
        }
    }

    private bool IsLocalizedOk(RowEntry e)
    {
        if (e.SourceChanged)
            return false;

        var targets = settings?.writeTargets;
        if (targets != null && targets.AnySelected)
            return !LocalizationCsvIO.IsMissingForTargets(e.Row, targets);

        return LocalizationCsvIO.HasCompleteTranslationIncludingKorean(e.Row);
    }

    private void DeselectCompletedRows()
    {
        foreach (var e in entries)
        {
            if (IsLocalizedOk(e))
                e.Selected = false;
        }
    }

    private bool PassesFilter(RowEntry e, string textFilter)
    {
        if (hideCompletedRows && IsLocalizedOk(e))
            return false;
        if (!PassesListFilter(e, listFilter))
            return false;
        if (string.IsNullOrEmpty(textFilter))
            return true;
        return (e.Row.Key?.IndexOf(textFilter, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0
               || (e.Row.Chinese?.IndexOf(textFilter, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0;
    }

    private void ApplyMissingKoreanFilterAndSelect(bool selectRows)
    {
        listFilter = RowListFilter.MissingKoreanOnly;
        scroll = Vector2.zero;
        if (!selectRows)
        {
            Repaint();
            return;
        }

        foreach (var e in entries)
            e.Selected = e.MissingKorean;
        Repaint();
    }

    private void ApplyPendingFilterAndSelect(bool selectRows)
    {
        listFilter = RowListFilter.Pending;
        scroll = Vector2.zero;
        if (!selectRows)
        {
            Repaint();
            return;
        }

        foreach (var e in entries)
            e.Selected = e.NeedsTranslation || e.SourceChanged;
        Repaint();
    }

    private static string Truncate(string s, int max)
    {
        if (string.IsNullOrEmpty(s)) return "";
        s = s.Replace("\r", " ").Replace("\n", " ");
        return s.Length <= max ? s : s.Substring(0, max) + "…";
    }

    private void SetAllSelected(bool selected)
    {
        string f = filter?.Trim() ?? "";
        foreach (var e in entries)
        {
            if (!PassesFilter(e, f))
                continue;
            e.Selected = selected;
        }
        Repaint();
    }

    private void SelectOnlyPending()
    {
        string f = filter?.Trim() ?? "";
        foreach (var e in entries)
        {
            if (!PassesFilter(e, f))
                continue;
            e.Selected = e.NeedsTranslation || e.SourceChanged;
        }
        Repaint();
    }

    private void SelectOnlyMissingKorean()
    {
        string f = filter?.Trim() ?? "";
        foreach (var e in entries)
        {
            if (!PassesFilter(e, f))
                continue;
            e.Selected = e.MissingKorean;
        }
        Repaint();
    }

    private void SelectOnlySourceChanged()
    {
        string f = filter?.Trim() ?? "";
        foreach (var e in entries)
        {
            if (!PassesFilter(e, f))
                continue;
            e.Selected = e.SourceChanged;
        }
        Repaint();
    }

    private void RebuildBaseline()
    {
        if (string.IsNullOrEmpty(csvFilePath) || !File.Exists(csvFilePath))
            return;

        if (!EditorUtility.DisplayDialog(
                "重建翻译基线",
                "将把当前 CSV 里「已有完整译文」的行的简体中文写入基线。\n" +
                "之后只有修改了 Chinese 列的行才会显示「原文已改」。\n\n继续？",
                "重建",
                "取消"))
            return;

        try
        {
            var doc = LocalizationCsvIO.Read(csvFilePath);
            baselineByKey = LocalizationCsvBaseline.RebuildAll(csvFilePath, doc);
            LoadRows();
            EditorUtility.DisplayDialog("完成", $"已更新 {baselineByKey.Count} 条基线记录。", "确定");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorUtility.DisplayDialog("失败", ex.Message, "确定");
        }
    }

    private async Task RunTranslateSelectedAsync()
    {
        var selectedIndices = new List<int>();
        foreach (var e in entries)
        {
            if (e.Selected)
                selectedIndices.Add(e.DocRowIndex);
        }

        if (selectedIndices.Count == 0)
        {
            EditorUtility.DisplayDialog("提示", "请至少勾选一行。", "确定");
            return;
        }

        if (settings.writeTargets == null || !settings.writeTargets.AnySelected)
        {
            EditorUtility.DisplayDialog("提示", "请至少勾选一门要写入的语言。", "确定");
            return;
        }

        string targetSummary = settings.writeTargets.GetSummaryLabel();
        if (!EditorUtility.DisplayDialog(
                "确认翻译",
                $"将翻译 {selectedIndices.Count} 行，仅写入：{targetSummary}\n" +
                "未勾选的语言列不会被修改。\n\n继续？",
                "开始",
                "取消"))
            return;

        isTranslating = true;
        progress = 0f;
        status = "准备翻译…";
        Repaint();

        try
        {
            await LocalizationTranslateRunner.RunAsync(
                settings,
                csvFilePath,
                selectedIndices,
                (p, msg) =>
                {
                    progress = p;
                    status = msg;
                    Repaint();
                });

            status = "完成！已保存: " + Path.GetFileName(csvFilePath);
            progress = 1f;
            EditorUtility.DisplayDialog(
                "翻译完成",
                $"共翻译 {selectedIndices.Count} 行\n已保存至:\n{csvFilePath}",
                "确定");
            LoadRows();
        }
        catch (Exception ex)
        {
            status = "失败: " + ex.Message;
            Debug.LogException(ex);
            EditorUtility.DisplayDialog("翻译失败", ex.Message, "确定");
        }
        finally
        {
            isTranslating = false;
            Repaint();
        }
    }
}

#endif
