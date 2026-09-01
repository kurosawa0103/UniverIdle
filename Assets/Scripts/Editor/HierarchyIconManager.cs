using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[InitializeOnLoad]
public static class HierarchyIconManager
{
    private static HierarchyIconConfig config;
    private static Dictionary<string, Texture2D> map = new();

    static HierarchyIconManager()
    {
        LoadConfig();

        EditorApplication.hierarchyWindowItemOnGUI += OnGUI;
        EditorApplication.projectChanged += LoadConfig;
    }

    private static void LoadConfig()
    {
        map.Clear();

        var guids = AssetDatabase.FindAssets("t:HierarchyIconConfig");
        if (guids.Length == 0)
        {
            Debug.LogWarning("找不到 HierarchyIconConfig");
            return;
        }

        var path = AssetDatabase.GUIDToAssetPath(guids[0]);
        config = AssetDatabase.LoadAssetAtPath<HierarchyIconConfig>(path);

        if (config == null)
        {
            Debug.LogWarning("配置文件加载失败");
            return;
        }

        foreach (var entry in config.entries)
        {
            if (string.IsNullOrEmpty(entry.scriptName)) continue;
            if (entry.icon == null) continue;

            map[entry.scriptName] = entry.icon;
        }
    }

    private static void OnGUI(int instanceID, Rect rect)
    {
        var go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
        if (go == null) return;

        foreach (var comp in go.GetComponents<MonoBehaviour>())
        {
            if (comp == null) continue;

            var typeName = comp.GetType().Name;
            if (map.TryGetValue(typeName, out var icon))
            {
                var r = new Rect(rect.x - 28, rect.y + 1, 16, 16);
                GUI.DrawTexture(r, icon);
                return;
            }
        }
    }
}
