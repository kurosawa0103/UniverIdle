#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public static class LocalizationCsvIO
{
    public const string ColKey = "Key";
    public const string ColChinese = "Chinese";
    public const string ColChineseF = "ChineseF";
    public const string ColEnglish = "English";
    public const string ColJapanese = "Japanese";
    public const string ColKorean = "Korean";
    public const string ColStandard = "Standard";
    public const string ColDescription = "Description";

    public class Row
    {
        public string Key;
        public string Description;
        public string Standard;
        public string Chinese;
        public string ChineseF;
        public string English;
        public string Japanese;
        public string Korean;
        public string[] RawCells;
    }

    public class Document
    {
        public string[] Headers;
        public List<Row> Rows = new List<Row>();
        public Dictionary<string, int> ColumnIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    }

    public static Document Read(string filePath)
    {
        var text = File.ReadAllText(filePath, Encoding.UTF8);
        var table = ParseCsv(text);
        if (table.Count == 0)
            throw new Exception("CSV 为空");

        var doc = new Document();
        doc.Headers = table[0];
        for (int i = 0; i < doc.Headers.Length; i++)
            doc.ColumnIndex[NormalizeHeader(doc.Headers[i])] = i;

        for (int r = 1; r < table.Count; r++)
        {
            var cells = table[r];
            if (IsEmptyRow(cells))
                continue;

            var row = new Row { RawCells = PadCells(cells, doc.Headers.Length) };
            row.Key = GetCell(doc, row.RawCells, ColKey);
            row.Description = GetCell(doc, row.RawCells, ColDescription);
            row.Standard = GetCell(doc, row.RawCells, ColStandard);
            row.Chinese = GetCell(doc, row.RawCells, ColChinese);
            row.ChineseF = GetCell(doc, row.RawCells, ColChineseF);
            row.English = GetCell(doc, row.RawCells, ColEnglish);
            row.Japanese = GetCell(doc, row.RawCells, ColJapanese);
            row.Korean = GetCell(doc, row.RawCells, ColKorean);
            doc.Rows.Add(row);
        }

        if (!doc.ColumnIndex.ContainsKey(ColChinese))
            throw new Exception("CSV 缺少 Chinese 列，请使用项目标准格式。");

        return doc;
    }

    public static void Write(string filePath, Document doc)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", EscapeRow(doc.Headers)));

        foreach (var row in doc.Rows)
        {
            var cells = (string[])row.RawCells.Clone();
            SetCell(doc, cells, ColKey, row.Key);
            SetCell(doc, cells, ColDescription, row.Description);
            SetCell(doc, cells, ColStandard, string.IsNullOrEmpty(row.Standard) ? row.Chinese : row.Standard);
            SetCell(doc, cells, ColChinese, row.Chinese);
            SetCell(doc, cells, ColChineseF, row.ChineseF);
            SetCell(doc, cells, ColEnglish, row.English);
            SetCell(doc, cells, ColJapanese, row.Japanese);
            SetCell(doc, cells, ColKorean, row.Korean);
            sb.AppendLine(string.Join(",", EscapeRow(cells)));
        }

        File.WriteAllText(filePath, sb.ToString(), new UTF8Encoding(true));
    }

    public static bool NeedsTranslation(Row row)
    {
        if (string.IsNullOrWhiteSpace(row.Chinese))
            return false;
        return string.IsNullOrWhiteSpace(row.ChineseF)
               || string.IsNullOrWhiteSpace(row.English)
               || string.IsNullOrWhiteSpace(row.Japanese);
    }

    public static bool NeedsKoreanTranslation(Row row)
    {
        if (string.IsNullOrWhiteSpace(row.Chinese))
            return false;
        return string.IsNullOrWhiteSpace(row.Korean);
    }

    public static bool IsMissingChineseF(Row row)
        => !string.IsNullOrWhiteSpace(row.Chinese) && string.IsNullOrWhiteSpace(row.ChineseF);

    public static bool IsMissingEnglish(Row row)
        => !string.IsNullOrWhiteSpace(row.Chinese) && string.IsNullOrWhiteSpace(row.English);

    public static bool IsMissingJapanese(Row row)
        => !string.IsNullOrWhiteSpace(row.Chinese) && string.IsNullOrWhiteSpace(row.Japanese);

    public static bool IsMissingKorean(Row row) => NeedsKoreanTranslation(row);

    public static bool IsMissingForTargets(Row row, LocalizationWriteTargets targets)
    {
        if (string.IsNullOrWhiteSpace(row.Chinese) || targets == null || !targets.AnySelected)
            return false;

        if (targets.chineseF && IsMissingChineseF(row)) return true;
        if (targets.english && IsMissingEnglish(row)) return true;
        if (targets.japanese && IsMissingJapanese(row)) return true;
        if (targets.korean && IsMissingKorean(row)) return true;
        return false;
    }

    public static bool ShouldRetranslateForTargets(
        Row row,
        LocalizationWriteTargets targets,
        IReadOnlyDictionary<string, string> baselineByKey = null)
        => IsMissingForTargets(row, targets) || IsSourceChanged(row, baselineByKey);

    public static bool HasCompleteTranslation(Row row)
    {
        if (string.IsNullOrWhiteSpace(row.Chinese))
            return false;
        return !string.IsNullOrWhiteSpace(row.ChineseF)
               && !string.IsNullOrWhiteSpace(row.English)
               && !string.IsNullOrWhiteSpace(row.Japanese);
    }

    public static bool HasCompleteTranslationIncludingKorean(Row row)
        => HasCompleteTranslation(row) && !string.IsNullOrWhiteSpace(row.Korean);

    /// <summary>Chinese 与 Standard 不一致（备用检测，优先使用 LocalizationCsvBaseline）。</summary>
    public static bool IsSourceChangedFromStandard(Row row)
    {
        if (string.IsNullOrWhiteSpace(row.Chinese))
            return false;
        if (string.IsNullOrWhiteSpace(row.Standard))
            return false;
        return !string.Equals(row.Chinese.Trim(), row.Standard.Trim(), StringComparison.Ordinal);
    }

    public static bool IsSourceChanged(Row row, IReadOnlyDictionary<string, string> baselineByKey = null)
        => LocalizationCsvBaseline.IsSourceChanged(row, baselineByKey);

    public static bool ShouldRetranslate(Row row, IReadOnlyDictionary<string, string> baselineByKey = null)
        => NeedsTranslation(row) || IsSourceChanged(row, baselineByKey);

    public static string ReplaceEnglishCommas(string english)
    {
        return string.IsNullOrEmpty(english) ? english : english.Replace(",", "{c}");
    }

    private static string NormalizeHeader(string h) => h?.Trim() ?? "";

    private static string GetCell(Document doc, string[] cells, string col)
    {
        if (!doc.ColumnIndex.TryGetValue(col, out int idx) || idx >= cells.Length)
            return "";
        return cells[idx]?.Trim() ?? "";
    }

    private static void SetCell(Document doc, string[] cells, string col, string value)
    {
        if (!doc.ColumnIndex.TryGetValue(col, out int idx) || idx >= cells.Length)
            return;
        cells[idx] = value ?? "";
    }

    private static string[] PadCells(string[] cells, int len)
    {
        if (cells.Length >= len)
            return cells;
        var padded = new string[len];
        Array.Copy(cells, padded, cells.Length);
        return padded;
    }

    private static bool IsEmptyRow(string[] cells)
    {
        foreach (var c in cells)
        {
            if (!string.IsNullOrWhiteSpace(c))
                return false;
        }
        return true;
    }

    private static string[] EscapeRow(string[] cells)
    {
        var result = new string[cells.Length];
        for (int i = 0; i < cells.Length; i++)
            result[i] = EscapeCell(cells[i] ?? "");
        return result;
    }

    private static string EscapeCell(string cell)
    {
        if (cell.Contains(",") || cell.Contains("\"") || cell.Contains("\n") || cell.Contains("\r"))
            return "\"" + cell.Replace("\"", "\"\"") + "\"";
        return cell;
    }

    public static List<string[]> ParseCsv(string text)
    {
        var rows = new List<string[]>();
        var row = new List<string>();
        var cell = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"')
                    {
                        cell.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    cell.Append(c);
                }
            }
            else
            {
                if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    row.Add(cell.ToString());
                    cell.Clear();
                }
                else if (c == '\n' || (c == '\r' && (i + 1 >= text.Length || text[i + 1] != '\n')))
                {
                    row.Add(cell.ToString());
                    cell.Clear();
                    if (row.Count > 0 && !IsEmptyRow(row.ToArray()))
                        rows.Add(row.ToArray());
                    row = new List<string>();
                    if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                        i++;
                }
                else if (c != '\r')
                {
                    cell.Append(c);
                }
            }
        }

        row.Add(cell.ToString());
        if (row.Count > 0 && !IsEmptyRow(row.ToArray()))
            rows.Add(row.ToArray());

        return rows;
    }
}

#endif
