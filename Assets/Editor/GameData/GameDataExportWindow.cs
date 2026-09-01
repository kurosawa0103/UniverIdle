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

    private readonly Dictionary<string, bool> _selected = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _sheetStatus = new(StringComparer.OrdinalIgnoreCase);
    private Vector2 _scroll;
    private string _statusMessage = string.Empty;
    private MessageType _statusType = MessageType.Info;

    private GUIStyle _headerTitleStyle;
    private GUIStyle _headerSubtitleStyle;
    private GUIStyle _workbookTitleStyle;
    private GUIStyle _pathKeyStyle;
    private GUIStyle _pathValueStyle;
    private GUIStyle _sheetIdStyle;
    private GUIStyle _sheetTitleStyle;
    private GUIStyle _sheetDescStyle;
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
      EnsureStyles();
      DrawHeader();
      DrawToolbar();
      EditorGUILayout.Space(8);

      _scroll = EditorGUILayout.BeginScrollView(_scroll);
      foreach (var workbook in GameDataSheetRegistry.Workbooks)
        DrawWorkbook(workbook);
      EditorGUILayout.EndScrollView();

      EditorGUILayout.Space(10);
      if (!string.IsNullOrEmpty(_statusMessage))
        EditorGUILayout.HelpBox(_statusMessage, _statusType);

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
      _pathKeyStyle = new GUIStyle(EditorStyles.label)
      {
        fontSize = 12,
        fontStyle = FontStyle.Bold,
        normal = { textColor = muted },
      };
      _pathValueStyle = new GUIStyle(EditorStyles.label)
      {
        fontSize = 12,
        normal = { textColor = pro ? new Color(0.82f, 0.86f, 0.9f) : new Color(0.2f, 0.24f, 0.28f) },
      };
      _sheetIdStyle = new GUIStyle(EditorStyles.label) { fontSize = 13, fontStyle = FontStyle.Bold };
      _sheetTitleStyle = new GUIStyle(EditorStyles.label) { fontSize = 13 };
      _sheetDescStyle = new GUIStyle(EditorStyles.wordWrappedLabel)
      {
        fontSize = 12,
        normal = { textColor = muted },
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
      if (SirenixEditorGUI.ToolbarButton(new GUIContent("生成 Excel"))) CreateExcelFromJson();
      SirenixEditorGUI.EndHorizontalToolbar();
    }

    private void DrawWorkbook(GameDataSheetRegistry.WorkbookInfo workbook)
    {
      var accent = GetWorkbookAccent(workbook.ExcelKey);

      EditorGUILayout.BeginVertical(EditorStyles.helpBox);
      DrawWorkbookHeader(workbook, accent);
      EditorGUILayout.Space(4);

      foreach (var sheet in workbook.Sheets)
        DrawSheetRow(sheet);

      EditorGUILayout.EndVertical();
      EditorGUILayout.Space(8);
    }

    private void DrawWorkbookHeader(GameDataSheetRegistry.WorkbookInfo workbook, Color accent)
    {
      EditorGUILayout.BeginHorizontal();
      var prev = GUI.contentColor;
      GUI.contentColor = accent;
      EditorGUILayout.LabelField($"{workbook.Title}   {workbook.ExcelFileName}", _workbookTitleStyle);
      GUI.contentColor = prev;
      GUILayout.FlexibleSpace();

      if (GUILayout.Button("打开 Excel", GUILayout.Width(100), GUILayout.Height(24)))
        GameDataExcelExporter.OpenExcel(workbook.ExcelKey);
      EditorGUILayout.EndHorizontal();

      EditorGUILayout.Space(2);
      DrawPathLine("Excel", workbook.ExcelAssetPath);
      DrawPathLine("JSON", workbook.JsonAssetPath);
    }

    private void DrawPathLine(string label, string path)
    {
      EditorGUILayout.BeginHorizontal();
      EditorGUILayout.LabelField(label, _pathKeyStyle, GUILayout.Width(44));
      EditorGUILayout.LabelField(path, _pathValueStyle);
      EditorGUILayout.EndHorizontal();
    }

    private void DrawSheetRow(GameDataSheetRegistry.SheetInfo sheet)
    {
      EditorGUILayout.BeginHorizontal();

      var selected = _selected.TryGetValue(sheet.Id, out var on) && on;
      var next = EditorGUILayout.Toggle(selected, GUILayout.Width(18));
      if (next != selected)
      {
        _selected[sheet.Id] = next;
        SaveSelectionPrefs();
      }

      EditorGUILayout.LabelField(sheet.Id, _sheetIdStyle, GUILayout.Width(80));
      EditorGUILayout.LabelField(sheet.Title, _sheetTitleStyle);
      GUILayout.FlexibleSpace();

      if (_sheetStatus.TryGetValue(sheet.Id, out var status))
        EditorGUILayout.LabelField(status, _statusStyle, GUILayout.Width(64));

      EditorGUILayout.EndHorizontal();
      EditorGUILayout.LabelField(sheet.Description, _sheetDescStyle);
      EditorGUILayout.Space(4);
    }

    private void DrawExportButton()
    {
      using (new EditorGUI.DisabledScope(!HasAnySelected()))
      {
        if (GUILayout.Button("导出选中 Sheet", _exportButtonStyle, GUILayout.Height(36)))
          ExportSelected();
      }
    }

    private static Color GetWorkbookAccent(string excelKey) =>
      excelKey == GameDataSheetRegistry.ItemsExcelKey
        ? EditorGUIUtility.isProSkin ? new Color(1f, 0.78f, 0.28f) : new Color(0.88f, 0.52f, 0.08f)
        : EditorGUIUtility.isProSkin ? new Color(0.35f, 0.88f, 0.68f) : new Color(0.1f, 0.62f, 0.42f);

    private void SetAllSelected(bool value)
    {
      foreach (var sheet in GameDataSheetRegistry.All)
        _selected[sheet.Id] = value;
      SaveSelectionPrefs();
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

        GameDataExcelExporter.ExportExcelToJson(ids);
        RefreshSheetStatus();
        SetStatus($"导出成功：{string.Join("、", ids)}", MessageType.Info);
      }
      catch (Exception ex)
      {
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
        SetStatus("已从 JSON 生成 items.xlsx 与 scavenge.xlsx。", MessageType.Info);
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

          var count = GameDataExcelExporter.CountDataRows(sheets, sheet.Id);
          _sheetStatus[sheet.Id] = count < 0 ? "无此表" : $"{count} 行";
        }
      }
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
        var key = id.Trim();
        if (key.Length > 0)
          _selected[key] = true;
      }
    }
  }
}
#endif
