#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

    [MenuItem("UniverIdle/导表工具...", false, 0)]
    public static void ShowWindow()
    {
      var window = GetWindow<GameDataExportWindow>("配表导出");
      window.minSize = new Vector2(460, 380);
      window.Show();
    }

    private void OnEnable()
    {
      foreach (var sheet in GameDataSheetRegistry.All)
        _selected[sheet.Id] = true;
      LoadSelectionPrefs();
      RefreshSheetStatus();
    }

    private void OnGUI()
    {
      EditorGUILayout.Space(6);
      EditorGUILayout.LabelField("Excel → JSON 导表", EditorStyles.boldLabel);
      EditorGUILayout.LabelField("道具表", GameDataPaths.ItemsExcelAssetPath, EditorStyles.wordWrappedLabel);
      EditorGUILayout.LabelField("拾荒表", GameDataPaths.ScavengeExcelAssetPath, EditorStyles.wordWrappedLabel);
      EditorGUILayout.LabelField("道具 JSON", GameDataPaths.ItemsJsonAssetPath, EditorStyles.wordWrappedLabel);
      EditorGUILayout.LabelField("拾荒 JSON", GameDataPaths.ScavengeJsonAssetPath, EditorStyles.wordWrappedLabel);

      EditorGUILayout.Space(8);
      DrawToolbar();
      EditorGUILayout.Space(4);

      _scroll = EditorGUILayout.BeginScrollView(_scroll);
      foreach (var sheet in GameDataSheetRegistry.All)
        DrawSheetRow(sheet);
      EditorGUILayout.EndScrollView();

      EditorGUILayout.Space(8);
      if (!string.IsNullOrEmpty(_statusMessage))
        EditorGUILayout.HelpBox(_statusMessage, _statusType);

      EditorGUILayout.Space(4);
      using (new EditorGUI.DisabledScope(!HasAnySelected()))
      {
        if (GUILayout.Button("导出选中表格", GUILayout.Height(32)))
          ExportSelected();
      }
    }

    private void DrawToolbar()
    {
      EditorGUILayout.BeginHorizontal();
      if (GUILayout.Button("全选", GUILayout.Width(72)))
        SetAllSelected(true);
      if (GUILayout.Button("全不选", GUILayout.Width(72)))
        SetAllSelected(false);
      if (GUILayout.Button("刷新", GUILayout.Width(72)))
        RefreshSheetStatus();
      GUILayout.FlexibleSpace();
      if (GUILayout.Button("生成 Excel", GUILayout.Width(88)))
        CreateExcelFromJson();
      if (GUILayout.Button("items", GUILayout.Width(56)))
        GameDataExcelExporter.OpenExcel(GameDataSheetRegistry.ItemsExcelKey);
      if (GUILayout.Button("scavenge", GUILayout.Width(72)))
        GameDataExcelExporter.OpenExcel(GameDataSheetRegistry.ScavengeExcelKey);
      EditorGUILayout.EndHorizontal();
    }

    private void DrawSheetRow(GameDataSheetRegistry.SheetInfo sheet)
    {
      EditorGUILayout.BeginVertical("box");
      EditorGUILayout.BeginHorizontal();
      var selected = _selected.TryGetValue(sheet.Id, out var on) && on;
      var next = EditorGUILayout.ToggleLeft(sheet.Title, selected, GUILayout.Width(180));
      if (next != selected)
      {
        _selected[sheet.Id] = next;
        SaveSelectionPrefs();
      }
      GUILayout.FlexibleSpace();
      EditorGUILayout.LabelField(GameDataSheetRegistry.GetExcelFileName(sheet.ExcelKey), EditorStyles.miniLabel, GUILayout.Width(96));
      if (_sheetStatus.TryGetValue(sheet.Id, out var status))
        EditorGUILayout.LabelField(status, EditorStyles.miniLabel, GUILayout.Width(72));
      EditorGUILayout.EndHorizontal();
      EditorGUILayout.LabelField(sheet.Description, EditorStyles.wordWrappedMiniLabel);
      EditorGUILayout.EndVertical();
      EditorGUILayout.Space(2);
    }

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
          SetStatus("请至少勾选一个表格。", MessageType.Warning);
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
      foreach (var sheet in GameDataSheetRegistry.All)
      {
        var path = GameDataExcelExporter.GetExcelFullPath(sheet.ExcelKey);
        if (!File.Exists(path))
        {
          _sheetStatus[sheet.Id] = "缺失";
          continue;
        }

        try
        {
          var count = GameDataExcelExporter.CountDataRows(sheet.ExcelKey, sheet.Id);
          _sheetStatus[sheet.Id] = count < 0 ? "无此表" : $"{count} 行";
        }
        catch
        {
          _sheetStatus[sheet.Id] = "读取失败";
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
