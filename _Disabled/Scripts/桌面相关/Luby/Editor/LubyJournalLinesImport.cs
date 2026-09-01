using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;

namespace DesktopPet.Luby.Editor
{
    /// <summary>从 知识库/Luby近况文案.xlsx 导入 → Resources CSV，供运行时读取。</summary>
    public static class LubyJournalLinesImport
    {
        private const string XlsxRelPath = "知识库/Luby近况文案.xlsx";
        private const string CsvRelPath = "Assets/Resources/GameData/Luby/DefaultLubyJournalLines.csv";

        private static readonly XNamespace Ss =
            "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

        [MenuItem("桌宠/导入 Luby 近况文案表", false, 520)]
        public static void ImportFromExcel()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string xlsxPath = Path.Combine(projectRoot, XlsxRelPath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(xlsxPath))
            {
                EditorUtility.DisplayDialog(
                    "导入失败",
                    "找不到表：\n" + xlsxPath + "\n请先在知识库放好 Luby近况文案.xlsx。",
                    "OK");
                return;
            }

            List<string[]> rows;
            try
            {
                rows = ReadFirstSheetRows(xlsxPath);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorUtility.DisplayDialog("导入失败", "解析 xlsx 出错：\n" + e.Message, "OK");
                return;
            }

            if (rows == null || rows.Count < 2)
            {
                EditorUtility.DisplayDialog("导入失败", "表为空或缺少「文案」数据行。", "OK");
                return;
            }

            var sb = new StringBuilder(4096);
            sb.Append("kind\tpersonalityId\ttraitId\tweight\tline\n");
            int written = 0;
            for (int i = 1; i < rows.Count; i++)
            {
                string[] r = rows[i];
                if (r == null || r.Length == 0)
                    continue;
                string kind = Cell(r, 0);
                if (string.IsNullOrEmpty(kind) || kind.StartsWith("#", StringComparison.Ordinal))
                    continue;
                if (string.Equals(kind, "kind", StringComparison.OrdinalIgnoreCase))
                    continue;

                string personality = Cell(r, 1);
                string trait = Cell(r, 2);
                string weight = Cell(r, 3);
                string line = Cell(r, 4);
                if (string.IsNullOrEmpty(line))
                    continue;
                if (string.IsNullOrEmpty(weight))
                    weight = "1";

                sb.Append(Escape(kind)).Append('\t')
                    .Append(Escape(personality)).Append('\t')
                    .Append(Escape(trait)).Append('\t')
                    .Append(Escape(weight)).Append('\t')
                    .Append(Escape(line)).Append('\n');
                written++;
            }

            string csvPath = Path.Combine(projectRoot, CsvRelPath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(csvPath) ?? csvPath);
            File.WriteAllText(csvPath, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            AssetDatabase.ImportAsset(CsvRelPath);
            LubyJournalLineTable.InvalidateCache();

            EditorUtility.DisplayDialog(
                "导入完成",
                $"已写入 {written} 行文案 →\n{CsvRelPath}\n运行时会自动读该 CSV。",
                "OK");
            Debug.Log($"[LubyJournal] 已导入 {written} 行 → {CsvRelPath}");
        }

        private static string Cell(string[] r, int i)
        {
            if (r == null || i < 0 || i >= r.Length || r[i] == null)
                return string.Empty;
            return r[i].Trim();
        }

        private static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s))
                return string.Empty;
            return s.Replace("\t", " ").Replace("\r", " ").Replace("\n", " ");
        }

        private static List<string[]> ReadFirstSheetRows(string xlsxPath)
        {
            using (ZipArchive zip = ZipFile.OpenRead(xlsxPath))
            {
                List<string> shared = ReadSharedStrings(zip);
                // Prefer sheet named 文案 via workbook; fallback sheet1
                string sheetPath = ResolveSheetPath(zip, "文案") ?? "xl/worksheets/sheet1.xml";
                ZipArchiveEntry entry = zip.GetEntry(sheetPath);
                if (entry == null)
                    throw new InvalidOperationException("xlsx 内找不到 worksheet：" + sheetPath);

                XDocument doc;
                using (Stream stream = entry.Open())
                    doc = XDocument.Load(stream);

                var result = new List<string[]>();
                XElement sheetData = doc.Root?.Element(Ss + "sheetData");
                if (sheetData == null)
                    return result;

                foreach (XElement row in sheetData.Elements(Ss + "row"))
                {
                    var cells = new Dictionary<int, string>();
                    int maxCol = -1;
                    foreach (XElement c in row.Elements(Ss + "c"))
                    {
                        string r = (string)c.Attribute("r");
                        int col = ColumnIndex(r);
                        if (col < 0)
                            continue;
                        string t = (string)c.Attribute("t");
                        XElement v = c.Element(Ss + "v");
                        XElement isElem = c.Element(Ss + "is");
                        string val = string.Empty;
                        if (t == "s" && v != null && int.TryParse(v.Value, out int si) &&
                            si >= 0 && si < shared.Count)
                            val = shared[si];
                        else if (t == "inlineStr" && isElem != null)
                        {
                            var sbInline = new StringBuilder();
                            foreach (XElement te in isElem.Descendants(Ss + "t"))
                                sbInline.Append(te.Value);
                            val = sbInline.ToString();
                        }
                        else if (v != null)
                            val = v.Value;
                        cells[col] = val ?? string.Empty;
                        if (col > maxCol)
                            maxCol = col;
                    }

                    if (maxCol < 0)
                        continue;
                    var arr = new string[Math.Max(5, maxCol + 1)];
                    for (int i = 0; i <= maxCol; i++)
                        arr[i] = cells.TryGetValue(i, out string s) ? s : string.Empty;
                    result.Add(arr);
                }

                return result;
            }
        }

        private static string ResolveSheetPath(ZipArchive zip, string preferredName)
        {
            ZipArchiveEntry wb = zip.GetEntry("xl/workbook.xml");
            ZipArchiveEntry rels = zip.GetEntry("xl/_rels/workbook.xml.rels");
            if (wb == null || rels == null)
                return null;

            XDocument wbDoc;
            XDocument relDoc;
            using (Stream s = wb.Open())
                wbDoc = XDocument.Load(s);
            using (Stream s = rels.Open())
                relDoc = XDocument.Load(s);

            XNamespace rNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
            var ridToTarget = new Dictionary<string, string>();
            foreach (XElement rel in relDoc.Root?.Elements() ?? Array.Empty<XElement>())
            {
                string id = (string)rel.Attribute("Id");
                string target = (string)rel.Attribute("Target");
                if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(target))
                    ridToTarget[id] = target.Replace('\\', '/');
            }

            foreach (XElement sh in wbDoc.Root?.Element(Ss + "sheets")?.Elements(Ss + "sheet")
                     ?? Array.Empty<XElement>())
            {
                string name = (string)sh.Attribute("name");
                string rid = (string)sh.Attribute(rNs + "id");
                if (!string.Equals(name, preferredName, StringComparison.Ordinal))
                    continue;
                if (rid == null || !ridToTarget.TryGetValue(rid, out string target))
                    return null;
                if (!target.StartsWith("xl/", StringComparison.Ordinal))
                    target = "xl/" + target.TrimStart('/');
                return target;
            }

            return null;
        }

        private static List<string> ReadSharedStrings(ZipArchive zip)
        {
            var list = new List<string>();
            ZipArchiveEntry entry = zip.GetEntry("xl/sharedStrings.xml");
            if (entry == null)
                return list;
            XDocument doc;
            using (Stream stream = entry.Open())
                doc = XDocument.Load(stream);
            foreach (XElement si in doc.Root?.Elements(Ss + "si") ?? Array.Empty<XElement>())
            {
                var sb = new StringBuilder();
                foreach (XElement t in si.Descendants(Ss + "t"))
                    sb.Append(t.Value);
                list.Add(sb.ToString());
            }

            return list;
        }

        private static int ColumnIndex(string cellRef)
        {
            if (string.IsNullOrEmpty(cellRef))
                return -1;
            int col = 0;
            for (int i = 0; i < cellRef.Length; i++)
            {
                char ch = cellRef[i];
                if (ch < 'A' || ch > 'Z')
                    break;
                col = col * 26 + (ch - 'A' + 1);
            }

            return col - 1;
        }
    }
}
