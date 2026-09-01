using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

[Serializable]
public struct PlayerPrefPair
{
    public string Key { get; set; }

    public object Value { get; set; }
}

public class PlayerPrefsWindow : EditorWindow
{
    private Vector2 scrollViewPos;


    private string searchKey = string.Empty;
    private List<PlayerPrefPair> dataList = new List<PlayerPrefPair>();
    private float searchBarHeight = 30;

    private GUILayoutOption btnWidthOpt = GUILayout.Width(125);

    [MenuItem("工具/PlayerPrefs窗口 %q")]
    public static void OpenWindow()
    {
        PlayerPrefsWindow window = UnityEditor.EditorWindow.GetWindow<PlayerPrefsWindow>();
        window.name = "PlayerPrefs窗口";
    }

    public void OnGUI()
    {
        GUILayout.BeginVertical();
        Rect rect = new Rect(0, 0, position.width, searchBarHeight);
        GUILayout.BeginArea(rect);
        {
            GUI.Box(rect, "");
            //搜索
            GUILayout.BeginHorizontal();
            DrawSearchBar();
            GUILayout.EndHorizontal();
        }
        GUILayout.EndArea();

        //数据
        FillDataList(ref dataList);

        //主框体
        DrawMainWindow(dataList);

        GUILayout.EndVertical();
    }


    private void DrawSearchBar()
    {
        //绘制label:
        GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel);
        labelStyle.normal.textColor = Color.cyan;
        EditorGUILayout.LabelField("输入要搜索的key:", labelStyle, GUILayout.Width(120));

        //绘制输入框:
        var style = new GUIStyle(EditorStyles.textField);
        style.fontStyle = FontStyle.Bold;
        searchKey = GUILayout.TextField(searchKey, style, GUILayout.ExpandWidth(true));

        //绘制 清空输入 按钮
        if (GUILayout.Button("x", btnWidthOpt))
        {
            searchKey = string.Empty;
        }

        //绘制 删除所有Key 按钮
        if (GUILayout.Button("删除所有", btnWidthOpt))
        {
            if (EditorUtility.DisplayDialog("删除提示", "确认删除所有key吗？", "确定", "取消"))
            {
                PlayerPrefs.DeleteAll();
            };
        }
    }

    private void FillDataList(ref List<PlayerPrefPair> list)
    {
        list.Clear();
        list = PlayerPrefsExtension.GetAll().ToList();
        if (!string.IsNullOrEmpty(searchKey))
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (!list[i].Key.ToLower().Contains(searchKey.ToLower()))
                {
                    list.RemoveAt(i);
                    i--;
                }
            }
        }
    }

    private void DrawMainWindow(List<PlayerPrefPair> dataList)
    {
        Rect rect = new Rect(0, searchBarHeight + 10, position.width, position.height);
        GUILayout.BeginArea(rect);
        scrollViewPos = GUILayout.BeginScrollView(scrollViewPos, false, true, GUILayout.Height(1000));
        GUILayout.BeginVertical();
        foreach (PlayerPrefPair pair in dataList)
        {
            if (!PlayerPrefs.HasKey(pair.Key))
            {
                continue;
            }

            GUILayout.BeginHorizontal();
            EditorGUILayout.TextField(pair.Key + ":", pair.Value.ToString());
            GUILayout.ExpandWidth(true);

            if (GUILayout.Button("删除Key", btnWidthOpt))
            {
                if (PlayerPrefs.HasKey(pair.Key))
                {
                    PlayerPrefs.DeleteKey(pair.Key);
                    Debug.Log($"{GetType()} delete key success,key:{pair.Key}");
                }
            }
            GUILayout.EndHorizontal();
        }
        GUILayout.EndVertical();
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }
}
