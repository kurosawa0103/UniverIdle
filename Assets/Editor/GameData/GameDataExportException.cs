#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;

namespace UniverIdle.Editor
{
  /// <summary>导表失败时携带涉及的 Sheet，供导出窗口标红与一键打开 Excel。</summary>
  public sealed class GameDataExportException : Exception
  {
    public readonly struct Entry
    {
      public readonly string SheetId;
      public readonly string Message;

      public Entry(string sheetId, string message)
      {
        SheetId = sheetId;
        Message = message;
      }
    }

    public IReadOnlyList<Entry> Entries { get; }

    public GameDataExportException(string sheetId, string message, Exception inner = null)
      : this(new[] { new Entry(sheetId, message) }, message, inner)
    {
    }

    public GameDataExportException(IEnumerable<Entry> entries, string summary, Exception inner = null)
      : base(summary, inner)
    {
      Entries = entries?.Where(e => !string.IsNullOrEmpty(e.SheetId)).ToArray()
                ?? Array.Empty<Entry>();
    }

    public IEnumerable<string> GetExcelKeys()
    {
      var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
      foreach (var entry in Entries)
      {
        var key = GameDataSheetRegistry.Get(entry.SheetId).ExcelKey;
        if (seen.Add(key))
          yield return key;
      }
    }
  }
}
#endif
