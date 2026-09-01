#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace UniverIdle.Editor
{
  /// <summary>无第三方依赖的 .xlsx 读写（表格式，首行为表头）。</summary>
  public static class SimpleXlsx
  {
    private static readonly XNamespace MainNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace RelNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace PkgRelNs = "http://schemas.openxmlformats.org/package/2006/relationships";

    public static Dictionary<string, List<string[]>> Read(string filePath)
    {
      if (!File.Exists(filePath))
        throw new FileNotFoundException("找不到 Excel 文件：" + filePath);

      using var stream = File.OpenRead(filePath);
      using var zip = new ZipArchive(stream, ZipArchiveMode.Read);
      var sharedStrings = ReadSharedStrings(zip);
      var sheetEntries = ReadSheetEntries(zip);

      var result = new Dictionary<string, List<string[]>>(StringComparer.OrdinalIgnoreCase);
      foreach (var entry in sheetEntries)
        result[entry.Name] = ReadSheet(zip, entry.Path, sharedStrings);
      return result;
    }

    public static void Write(string filePath, IReadOnlyDictionary<string, IList<string[]>> sheets)
    {
      if (sheets == null || sheets.Count == 0)
        throw new ArgumentException("没有可写入的工作表。");

      var dir = Path.GetDirectoryName(filePath);
      if (!string.IsNullOrEmpty(dir))
        Directory.CreateDirectory(dir);

      if (File.Exists(filePath))
        File.Delete(filePath);

      using var stream = File.Create(filePath);
      using var zip = new ZipArchive(stream, ZipArchiveMode.Create);

      var shared = new List<string>();
      var sharedIndex = new Dictionary<string, int>();
      int GetStringIndex(string s)
      {
        s ??= string.Empty;
        if (sharedIndex.TryGetValue(s, out var idx)) return idx;
        idx = shared.Count;
        shared.Add(s);
        sharedIndex[s] = idx;
        return idx;
      }

      var sheetIndex = 1;
      var sheetOverrides = new List<string>();
      var sheetNames = new List<string>();

      foreach (var kv in sheets)
      {
        var sheetName = kv.Key;
        sheetNames.Add(sheetName);
        var rows = kv.Value ?? Array.Empty<string[]>();
        var sheetPath = $"xl/worksheets/sheet{sheetIndex}.xml";

        WriteSheet(zip, sheetPath, rows, GetStringIndex);
        sheetOverrides.Add(
          $"<Override PartName=\"/{sheetPath}\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>");

        sheetIndex++;
      }

      WriteSharedStrings(zip, shared);
      WriteWorkbook(zip, sheetNames);
      WriteWorkbookRels(zip, sheets.Count);
      WriteRootRels(zip);
      WriteContentTypes(zip, sheetOverrides);
    }

    private static List<string> ReadSharedStrings(ZipArchive zip)
    {
      var entry = zip.GetEntry("xl/sharedStrings.xml");
      if (entry == null) return new List<string>();

      using var stream = entry.Open();
      var doc = XDocument.Load(stream);
      var list = new List<string>();
      foreach (var si in doc.Descendants(MainNs + "si"))
      {
        var text = string.Concat(si.Descendants(MainNs + "t").Select(t => t.Value));
        list.Add(text);
      }
      return list;
    }

    private sealed class SheetEntry
    {
      public string Name;
      public string Path;
    }

    private static List<SheetEntry> ReadSheetEntries(ZipArchive zip)
    {
      var rels = new Dictionary<string, string>(StringComparer.Ordinal);
      var relEntry = zip.GetEntry("xl/_rels/workbook.xml.rels");
      if (relEntry != null)
      {
        using var stream = relEntry.Open();
        var doc = XDocument.Load(stream);
        foreach (var rel in doc.Descendants(PkgRelNs + "Relationship"))
        {
          var id = (string)rel.Attribute("Id");
          var target = (string)rel.Attribute("Target");
          if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(target))
            rels[id] = target.Replace('\\', '/');
        }
      }

      var wbEntry = zip.GetEntry("xl/workbook.xml");
      if (wbEntry == null)
        throw new InvalidDataException("无效的 xlsx：缺少 xl/workbook.xml");

      using (var stream = wbEntry.Open())
      {
        var doc = XDocument.Load(stream);
        var list = new List<SheetEntry>();
        foreach (var sheet in doc.Descendants(MainNs + "sheet"))
        {
          var name = (string)sheet.Attribute("name");
          var relId = (string)sheet.Attribute(RelNs + "id");
          if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(relId)) continue;
          if (!rels.TryGetValue(relId, out var target)) continue;
          var path = target.StartsWith("worksheets/", StringComparison.Ordinal)
            ? "xl/" + target
            : "xl/worksheets/" + target.TrimStart('/');
          list.Add(new SheetEntry { Name = name, Path = path });
        }
        return list;
      }
    }

    private static List<string[]> ReadSheet(ZipArchive zip, string path, List<string> sharedStrings)
    {
      var entry = zip.GetEntry(path) ?? zip.GetEntry(path.TrimStart('/'));
      if (entry == null)
        return new List<string[]>();

      using var stream = entry.Open();
      var doc = XDocument.Load(stream);
      var cells = new Dictionary<(int row, int col), string>();
      var maxRow = 0;
      var maxCol = 0;

      foreach (var rowEl in doc.Descendants(MainNs + "row"))
      {
        foreach (var c in rowEl.Elements(MainNs + "c"))
        {
          var cellRef = (string)c.Attribute("r");
          if (string.IsNullOrEmpty(cellRef)) continue;
          var (col, row) = ParseCellRef(cellRef);
          var value = ReadCellValue(c, sharedStrings);
          cells[(row, col)] = value;
          maxRow = Math.Max(maxRow, row);
          maxCol = Math.Max(maxCol, col);
        }
      }

      var rows = new List<string[]>();
      for (var r = 0; r <= maxRow; r++)
      {
        var row = new string[maxCol + 1];
        for (var c = 0; c <= maxCol; c++)
          cells.TryGetValue((r, c), out row[c]);
        rows.Add(row);
      }
      return rows;
    }

    private static string ReadCellValue(XElement cell, List<string> sharedStrings)
    {
      var type = (string)cell.Attribute("t");
      var v = cell.Element(MainNs + "v");
      if (v == null) return string.Empty;
      var raw = v.Value;
      if (type == "s")
      {
        if (int.TryParse(raw, out var idx) && idx >= 0 && idx < sharedStrings.Count)
          return sharedStrings[idx];
        return string.Empty;
      }
      return raw;
    }

    private static (int col, int row) ParseCellRef(string cellRef)
    {
      var i = 0;
      while (i < cellRef.Length && char.IsLetter(cellRef[i])) i++;
      var colLetters = cellRef.Substring(0, i);
      var rowPart = cellRef.Substring(i);
      var col = 0;
      foreach (var ch in colLetters)
        col = col * 26 + (char.ToUpperInvariant(ch) - 'A' + 1);
      col--;
      var row = int.Parse(rowPart) - 1;
      return (col, row);
    }

    private static void WriteSharedStrings(ZipArchive zip, List<string> shared)
    {
      var sb = new StringBuilder();
      sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
      sb.Append("<sst xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" count=\"")
        .Append(shared.Count).Append("\" uniqueCount=\"").Append(shared.Count).Append("\">");
      foreach (var s in shared)
        sb.Append("<si><t>").Append(EscapeXml(s)).Append("</t></si>");
      sb.Append("</sst>");
      WriteTextEntry(zip, "xl/sharedStrings.xml", sb.ToString());
    }

    private static void WriteSheet(ZipArchive zip, string path, IList<string[]> rows, Func<string, int> getStringIndex)
    {
      var sb = new StringBuilder();
      sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
      sb.Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">");
      sb.Append("<sheetData>");

      for (var r = 0; r < rows.Count; r++)
      {
        var row = rows[r] ?? Array.Empty<string>();
        sb.Append("<row r=\"").Append(r + 1).Append("\">");
        for (var c = 0; c < row.Length; c++)
        {
          var value = row[c] ?? string.Empty;
          var cellRef = ColumnName(c) + (r + 1);
          if (IsNumber(value))
          {
            sb.Append("<c r=\"").Append(cellRef).Append("\"><v>").Append(value).Append("</v></c>");
          }
          else
          {
            var idx = getStringIndex(value);
            sb.Append("<c r=\"").Append(cellRef).Append("\" t=\"s\"><v>").Append(idx).Append("</v></c>");
          }
        }
        sb.Append("</row>");
      }

      sb.Append("</sheetData></worksheet>");
      WriteTextEntry(zip, path, sb.ToString());
    }

    private static void WriteWorkbook(ZipArchive zip, List<string> sheetNames)
    {
      var sb = new StringBuilder();
      sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
      sb.Append("<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">");
      sb.Append("<sheets>");
      for (var i = 0; i < sheetNames.Count; i++)
      {
        sb.Append("<sheet name=\"").Append(EscapeXml(sheetNames[i]))
          .Append("\" sheetId=\"").Append(i + 1)
          .Append("\" r:id=\"rId").Append(i + 2).Append("\"/>");
      }
      sb.Append("</sheets></workbook>");
      WriteTextEntry(zip, "xl/workbook.xml", sb.ToString());
    }

    private static void WriteWorkbookRels(ZipArchive zip, int sheetCount)
    {
      var sb = new StringBuilder();
      sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
      sb.Append("<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">");
      sb.Append("<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/sharedStrings\" Target=\"sharedStrings.xml\"/>");
      for (var i = 0; i < sheetCount; i++)
      {
        sb.Append("<Relationship Id=\"rId").Append(i + 2)
          .Append("\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet")
          .Append(i + 1).Append(".xml\"/>");
      }
      sb.Append("</Relationships>");
      WriteTextEntry(zip, "xl/_rels/workbook.xml.rels", sb.ToString());
    }

    private static void WriteRootRels(ZipArchive zip)
    {
      const string xml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
        "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
        "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>" +
        "</Relationships>";
      WriteTextEntry(zip, "_rels/.rels", xml);
    }

    private static void WriteContentTypes(ZipArchive zip, List<string> sheetOverrides)
    {
      var sb = new StringBuilder();
      sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
      sb.Append("<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">");
      sb.Append("<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>");
      sb.Append("<Default Extension=\"xml\" ContentType=\"application/xml\"/>");
      sb.Append("<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>");
      sb.Append("<Override PartName=\"/xl/sharedStrings.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sharedStrings+xml\"/>");
      foreach (var line in sheetOverrides)
        sb.Append(line);
      sb.Append("</Types>");
      WriteTextEntry(zip, "[Content_Types].xml", sb.ToString());
    }

    private static void WriteTextEntry(ZipArchive zip, string path, string content)
    {
      var entry = zip.CreateEntry(path, CompressionLevel.Optimal);
      using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
      writer.Write(content);
    }

    private static string ColumnName(int col)
    {
      var sb = new StringBuilder();
      col++;
      while (col > 0)
      {
        col--;
        sb.Insert(0, (char)('A' + col % 26));
        col /= 26;
      }
      return sb.ToString();
    }

    private static bool IsNumber(string value)
    {
      if (string.IsNullOrWhiteSpace(value)) return false;
      return double.TryParse(value, System.Globalization.NumberStyles.Float,
        System.Globalization.CultureInfo.InvariantCulture, out _);
    }

    private static string EscapeXml(string text)
    {
      if (string.IsNullOrEmpty(text)) return string.Empty;
      return text
        .Replace("&", "&amp;")
        .Replace("<", "&lt;")
        .Replace(">", "&gt;")
        .Replace("\"", "&quot;")
        .Replace("'", "&apos;");
    }
  }
}
#endif
