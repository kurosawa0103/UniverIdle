using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[InitializeOnLoad]
public static class HierarchyIconManager
{
    private const string DefaultAssetPath = "Assets/Editor/HierarchyIconConfig.asset";

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
            config = EnsureDefaultAsset();
            return;
        }

        var path = AssetDatabase.GUIDToAssetPath(guids[0]);
        config = AssetDatabase.LoadAssetAtPath<HierarchyIconConfig>(path);
        if (config == null)
            config = EnsureDefaultAsset();

        RebuildMap();
    }

    private static HierarchyIconConfig EnsureDefaultAsset()
    {
        var existing = AssetDatabase.LoadAssetAtPath<HierarchyIconConfig>(DefaultAssetPath);
        if (existing != null)
        {
            RebuildMapFrom(existing);
            return existing;
        }

        if (!AssetDatabase.IsValidFolder("Assets/Editor"))
            AssetDatabase.CreateFolder("Assets", "Editor");

        var created = ScriptableObject.CreateInstance<HierarchyIconConfig>();
        AssetDatabase.CreateAsset(created, DefaultAssetPath);
        AssetDatabase.SaveAssets();
        RebuildMapFrom(created);
        return created;
    }

    private static void RebuildMap()
    {
        if (config == null) return;
        RebuildMapFrom(config);
    }

    private static void RebuildMapFrom(HierarchyIconConfig source)
    {
        map.Clear();
        if (source?.entries == null) return;

        foreach (var entry in source.entries)
        {
            if (string.IsNullOrEmpty(entry.scriptName) || entry.icon == null) continue;
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
