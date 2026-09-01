#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace UniverIdle.Editor
{
  public sealed class GameDataExportWindow : EditorWindow
  {
    private const string PrefsKey = "UniverIdle.GameDataExport.SelectedSheets";
    private const string SearchControlName = "GameDataExportSearch";

    private readonly Dictionary<string, bool> _selected = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _sheetStatus = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _sheetErrors = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _failedExcelKeys = new(StringComparer.OrdinalIgnoreCase);
    private Vector2 _scroll;
    private string _searchQuery = string.Empty;
    private string _statusMessage = string.Empty;
    private MessageType _statusType = MessageType.Info;

    private GUIStyle _headerTitleStyle;
    private GUIStyle _headerSubtitleStyle;
    private GUIStyle _workbookTitleStyle;
    private GUIStyle _pathStyle;
    private GUIStyle _sheetLabelStyle;
    private GUIStyle _sheetErrorLabelStyle;
    private GUIStyle _sheetErrorHintStyle;
    private GUIStyle _statusStyle;
    private GUIStyle _exportButtonStyle;
    private bool _stylesReady;

    [MenuItem("UniverIdle/导表工具...", false, 0)]
    public static void ShowWindow()
    {
      var window = GetWindow<GameDataExportWindow>("配表导出");
      window.minSize = new Vector2(540, 480);
      window.Show();
    }

    private void OnEnable()
    {
      _stylesReady = false;
      foreach (var sheet in GameDataSheetRegistry.All)
        _selected[sheet.Id] = true;
      LoadSelectionPrefs();
      RefreshSheetStatus();
    }

    private void OnGUI()
    {
      HandleSearchShortcut();
      EnsureStyles();

      DrawHeader();
      DrawToolbar();
      EditorGUILayout.Space(8);

      var anyVisible = false;
      _scroll = EditorGUILayout.BeginScrollView(_scroll);
      foreach (var workbook in GameDataSheetRegistry.Workbooks)
      {
        if (DrawWorkbook(workbook))
          anyVisible = true;
      }
      if (HasSearchFilter && !anyVisible)
        EditorGUILayout.HelpBox($"未找到与「{_searchQuery.Trim()}」匹配的 Sheet。", MessageType.Info);
      EditorGUILayout.EndScrollView();

      EditorGUILayout.Space(10);
      if (!string.IsNullOrEmpty(_statusMessage))
        EditorGUILayout.HelpBox(_statusMessage, _statusType);
      DrawFailedExcelActions();

      EditorGUILayout.Space(6);
      DrawExportButton();
    }

    private void EnsureStyles()
    {
      if (_stylesReady) return;

      var pro = EditorGUIUtility.isProSkin;
      var muted = pro ? new Color(0.72f, 0.76f, 0.8f) : new Color(0.35f, 0.38f, 0.42f);

      _headerTitleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 15 };
      _headerSubtitleStyle = new GUIStyle(EditorStyles.label) { fontSize = 12 };
      _workbookTitleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 };
      _pathStyle = new GUIStyle(EditorStyles.label)
      {
        fontSize = 12,
        normal = { textColor = muted },
      };
      _sheetLabelStyle = new GUIStyle(EditorStyles.label) { fontSize = 13 };
      var errorColor = pro ? new Color(1f, 0.45f, 0.45f) : new Color(0.78f, 0.12f, 0.12f);
      _sheetErrorLabelStyle = new GUIStyle(EditorStyles.label)
      {
        fontSize = 13,
        fontStyle = FontStyle.Bold,
        normal = { textColor = errorColor },
      };
      _sheetErrorHintStyle = new GUIStyle(EditorStyles.miniLabel)
      {
        fontSize = 11,
        wordWrap = true,
        normal = { textColor = errorColor },
      };
      _statusStyle = new GUIStyle(EditorStyles.label) { fontSize = 12, alignment = TextAnchor.MiddleRight };
      _exportButtonStyle = new GUIStyle(GUI.skin.button) { fontSize = 13, fontStyle = FontStyle.Bold };
      _stylesReady = true;
    }

    private void DrawHeader()
    {
      EditorGUILayout.LabelField("配表导出", _headerTitleStyle);
      EditorGUILayout.LabelField("Excel  →  JSON", _headerSubtitleStyle);
      EditorGUILayout.Space(6);
    }

    private void DrawToolbar()
    {
      SirenixEditorGUI.BeginHorizontalToolbar();
      if (SirenixEditorGUI.ToolbarButton(new GUIContent("全选"))) SetAllSelected(true);
      if (SirenixEditorGUI.ToolbarButton(new GUIContent("全不选"))) SetAllSelected(false);
      if (SirenixEditorGUI.ToolbarButton(new GUIContent("刷新"))) RefreshSheetStatus();

      GUILayout.FlexibleSpace();

      GUI.SetNextControlName(SearchControlName);
      var next = GUILayout.TextField(_searchQuery, EditorStyles.toolbarSearchField, GUILayout.MinWidth(140), GUILayout.MaxWidth(260));
      if (!string.Equals(next, _searchQuery, StringComparison.Ordinal))
      {
        _searchQuery = next;
        Repaint();
      }

      if (SirenixEditorGUI.ToolbarButton(new GUIContent("生成 Excel"))) CreateExcelFromJson();
      SirenixEditorGUI.EndHorizontalToolbar();
    }

    private static void HandleSearchShortcut()
    {
      var e = Event.current;
      if (e.type != EventType.KeyDown || e.keyCode != KeyCode.F || (!e.control && !e.command))
        return;
      EditorGUI.FocusTextInControl(SearchControlName);
      e.Use();
    }

    private bool HasSearchFilter => !string.IsNullOrWhiteSpace(_searchQuery);

    private bool DrawWorkbook(GameDataSheetRegistry.WorkbookInfo workbook)
    {
      var visibleSheets = GetVisibleSheets(workbook);
      if (visibleSheets.Count == 0) return false;

      var accent = GetWorkbookAccent(workbook.ExcelKey);

      EditorGUILayout.BeginVertical(EditorStyles.helpBox);
      DrawWorkbookHeader(workbook, accent);

      foreach (var sheet in visibleSheets)
        DrawSheetRow(sheet);

      EditorGUILayout.EndVertical();
      EditorGUILayout.Space(8);
      return true;
    }

    private void DrawWorkbookHeader(GameDataSheetRegistry.WorkbookInfo workbook, Color accent)
    {
      EditorGUILayout.BeginHorizontal();
      var prev = GUI.contentColor;
      GUI.contentColor = accent;
      EditorGUILayout.LabelField($"{workbook.Title} · {workbook.ExcelFileName}", _workbookTitleStyle);
      GUI.contentColor = prev;
      GUILayout.FlexibleSpace();

      if (GUILayout.Button("打开 Excel", GUILayout.Width(100), GUILayout.Height(22)))
        GameDataExcelExporter.OpenExcel(workbook.ExcelKey);
      EditorGUILayout.EndHorizontal();

      EditorGUILayout.LabelField($"Excel → {workbook.ExcelAssetPath}", _pathStyle);
      EditorGUILayout.LabelField($"JSON → {workbook.JsonAssetPath}", _pathStyle);
    }

    private void DrawSheetRow(GameDataSheetRegistry.SheetInfo sheet)
    {
      var hasError = _sheetErrors.TryGetValue(sheet.Id, out var errorMessage);
      if (hasError)
      {
        var prevBg = GUI.backgroundColor;
        GUI.backgroundColor = EditorGUIUtility.isProSkin
          ? new Color(0.55f, 0.18f, 0.18f, 1f)
          : new Color(0.95f, 0.72f, 0.72f, 1f);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUI.backgroundColor = prevBg;
        DrawSheetRowContent(sheet, hasError, errorMessage);
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(34);
        EditorGUILayout.LabelField(errorMessage, _sheetErrorHintStyle);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
        return;
      }

      DrawSheetRowContent(sheet, false, null);
    }

    private void DrawSheetRowContent(GameDataSheetRegistry.SheetInfo sheet, bool hasError, string _)
    {
      EditorGUILayout.BeginHorizontal();
      GUILayout.Space(16);

      var selected = _selected.TryGetValue(sheet.Id, out var on) && on;
      var next = EditorGUILayout.Toggle(selected, GUILayout.Width(18));
      if (next != selected)
      {
        _selected[sheet.Id] = next;
        SaveSelectionPrefs();
      }

      EditorGUILayout.LabelField(
        $"Sheet · {sheet.TabName}",
        hasError ? _sheetErrorLabelStyle : _sheetLabelStyle);
      GUILayout.FlexibleSpace();

      if (hasError)
        EditorGUILayout.LabelField("失败", _sheetErrorLabelStyle, GUILayout.Width(40));
      else if (_sheetStatus.TryGetValue(sheet.Id, out var status))
        EditorGUILayout.LabelField(status, _statusStyle, GUILayout.Width(64));

      EditorGUILayout.EndHorizontal();
    }

    private void DrawFailedExcelActions()
    {
      if (_failedExcelKeys.Count == 0) return;

      EditorGUILayout.Space(4);
      EditorGUILayout.BeginHorizontal();
      EditorGUILayout.LabelField("打开表格：", GUILayout.Width(64));
      foreach (var excelKey in _failedExcelKeys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
      {
        var fileName = GameDataSheetRegistry.GetExcelFileName(excelKey);
        if (GUILayout.Button($"打开 {fileName}", GUILayout.Height(22)))
        {
          try
          {
            GameDataExcelExporter.OpenExcel(excelKey);
          }
          catch (Exception ex)
          {
            SetStatus("打开 Excel 失败：" + ex.Message, MessageType.Error);
          }
        }
      }
      EditorGUILayout.EndHorizontal();
    }

    private void DrawExportButton()
    {
      using (new EditorGUI.DisabledScope(!HasAnySelected()))
      {
        if (GUILayout.Button("导出选中 Sheet", _exportButtonStyle, GUILayout.Height(36)))
          ExportSelected();
      }
    }

    private static Color GetWorkbookAccent(string excelKey)
    {
      if (excelKey == GameDataSheetRegistry.ItemsExcelKey)
        return EditorGUIUtility.isProSkin ? new Color(1f, 0.78f, 0.28f) : new Color(0.88f, 0.52f, 0.08f);
      if (excelKey == GameDataSheetRegistry.WoodcuttingExcelKey)
        return EditorGUIUtility.isProSkin ? new Color(0.72f, 0.58f, 0.38f) : new Color(0.52f, 0.38f, 0.2f);
      if (excelKey == GameDataSheetRegistry.MonsterExploreExcelKey)
        return EditorGUIUtility.isProSkin ? new Color(0.95f, 0.45f, 0.45f) : new Color(0.72f, 0.28f, 0.28f);
      return EditorGUIUtility.isProSkin ? new Color(0.35f, 0.88f, 0.68f) : new Color(0.1f, 0.62f, 0.42f);
    }

    private void SetAllSelected(bool value)
    {
      foreach (var sheet in EnumerateTargetSheets())
        _selected[sheet.Id] = value;
      SaveSelectionPrefs();
    }

    private IEnumerable<GameDataSheetRegistry.SheetInfo> EnumerateTargetSheets()
    {
      if (!HasSearchFilter)
      {
        foreach (var sheet in GameDataSheetRegistry.All)
          yield return sheet;
        yield break;
      }

      foreach (var workbook in GameDataSheetRegistry.Workbooks)
      {
        foreach (var sheet in GetVisibleSheets(workbook))
          yield return sheet;
      }
    }

    private List<GameDataSheetRegistry.SheetInfo> GetVisibleSheets(GameDataSheetRegistry.WorkbookInfo workbook)
    {
      var list = new List<GameDataSheetRegistry.SheetInfo>();
      foreach (var sheet in workbook.Sheets)
      {
        if (MatchesSearch(sheet, workbook, _searchQuery))
          list.Add(sheet);
      }
      return list;
    }

    private static bool MatchesSearch(
      GameDataSheetRegistry.SheetInfo sheet,
      GameDataSheetRegistry.WorkbookInfo workbook,
      string query)
    {
      if (string.IsNullOrWhiteSpace(query)) return true;

      var haystack = string.Join(" ",
        sheet.Id,
        sheet.TabName,
        sheet.Title,
        sheet.Description,
        workbook.Title,
        workbook.ExcelFileName,
        workbook.ExcelKey);

      foreach (var token in query.Split((char[])null, StringSplitOptions.RemoveEmptyEntries))
      {
        if (haystack.IndexOf(token, StringComparison.OrdinalIgnoreCase) < 0)
          return false;
      }
      return true;
    }

    private bool HasAnySelected()
    {
      foreach (var sheet in GameDataSheetRegistry.All)
      {
        if (_selected.TryGetValue(sheet.Id, out var on) && on)
          return true;
      }
      return false;
    }

    private string[] GetSelectedIds()
    {
      return GameDataSheetRegistry.All
        .Where(s => _selected.TryGetValue(s.Id, out var on) && on)
        .Select(s => s.Id)
        .ToArray();
    }

    private void ExportSelected()
    {
      try
      {
        var ids = GetSelectedIds();
        if (ids.Length == 0)
        {
          SetStatus("请至少勾选一个 Sheet。", MessageType.Warning);
          return;
        }

        ClearExportErrors();
        GameDataExcelExporter.ExportExcelToJson(ids);
        RefreshSheetStatus();
        SetStatus($"导出成功：{string.Join("、", ids)}", MessageType.Info);
      }
      catch (GameDataExportException ex)
      {
        ApplyExportErrors(ex);
        SetStatus(ex.Message, MessageType.Error);
        Debug.LogError("[UniverIdle] 导表失败：" + ex);
      }
      catch (Exception ex)
      {
        ApplyExportErrorsForSelection(GetSelectedIds(), ex.Message);
        SetStatus("导出失败：" + ex.Message, MessageType.Error);
        Debug.LogError("[UniverIdle] 导表失败：" + ex);
      }
    }

    private void CreateExcelFromJson()
    {
      try
      {
        GameDataExcelExporter.CreateExcelFromJson();
        AssetDatabase.Refresh();
        RefreshSheetStatus();
        SetStatus("已从 JSON 生成 items、scavenge、woodcutting、monster_explore 四套 Excel。", MessageType.Info);
      }
      catch (Exception ex)
      {
        SetStatus("生成 Excel 失败：" + ex.Message, MessageType.Error);
      }
    }

    private void RefreshSheetStatus()
    {
      _sheetStatus.Clear();
      foreach (var workbook in GameDataSheetRegistry.Workbooks)
      {
        var path = GameDataExcelExporter.GetExcelFullPath(workbook.ExcelKey);
        Dictionary<string, List<string[]>> sheets = null;
        var readFailed = false;

        if (!File.Exists(path))
        {
          foreach (var sheet in workbook.Sheets)
            _sheetStatus[sheet.Id] = "缺失";
          continue;
        }

        try
        {
          sheets = SimpleXlsx.Read(path);
        }
        catch
        {
          readFailed = true;
        }

        foreach (var sheet in workbook.Sheets)
        {
          if (readFailed)
          {
            _sheetStatus[sheet.Id] = "读取失败";
            continue;
          }

          var count = GameDataExcelExporter.CountDataRows(sheets, sheet);
          _sheetStatus[sheet.Id] = count < 0 ? "无此表" : $"{count} 行";
        }
      }
    }

    private void ClearExportErrors()
    {
      _sheetErrors.Clear();
      _failedExcelKeys.Clear();
    }

    private void ApplyExportErrors(GameDataExportException ex)
    {
      ClearExportErrors();
      foreach (var entry in ex.Entries)
        _sheetErrors[entry.SheetId] = entry.Message;
      foreach (var key in ex.GetExcelKeys())
        _failedExcelKeys.Add(key);
      Repaint();
    }

    private void ApplyExportErrorsForSelection(string[] sheetIds, string message)
    {
      ClearExportErrors();
      foreach (var id in sheetIds)
      {
        _sheetErrors[id] = message;
        _failedExcelKeys.Add(GameDataSheetRegistry.Get(id).ExcelKey);
      }
      Repaint();
    }

    private void SetStatus(string message, MessageType type)
    {
      _statusMessage = message;
      _statusType = type;
      Repaint();
    }

    private void SaveSelectionPrefs()
    {
      var value = string.Join(",", GetSelectedIds());
      EditorPrefs.SetString(PrefsKey, value);
    }

    private void LoadSelectionPrefs()
    {
      if (!EditorPrefs.HasKey(PrefsKey)) return;
      var raw = EditorPrefs.GetString(PrefsKey);
      if (string.IsNullOrEmpty(raw)) return;
      foreach (var sheet in GameDataSheetRegistry.All)
        _selected[sheet.Id] = false;
      foreach (var id in raw.Split(','))
      {
        var key = MapLegacySheetId(id.Trim());
        if (key.Length > 0 && _selected.ContainsKey(key))
          _selected[key] = true;
      }
    }

    private static string MapLegacySheetId(string id) =>
      id switch
      {
        "works" => "scavenge_works",
        "actions" => "scavenge_actions",
        "loot" => "scavenge_loot",
        _ => id,
      };
  }
}
#endif
