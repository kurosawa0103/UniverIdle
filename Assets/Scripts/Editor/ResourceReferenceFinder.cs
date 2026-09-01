using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class ResourceReferenceFinder : EditorWindow
{
    private Object targetObject; // 要检索的目标对象
    private Vector2 scrollPosition; // 滚动视图的位置
    private bool hasFoundReferences = false; // 是否找到引用的标志

    private List<string> referencedScenes = new List<string>(); // 存储引用资源的场景路径
    private List<string> referencedPrefabs = new List<string>(); // 存储引用资源的预制体路径
    private List<string> referencedScriptableObjects = new List<string>(); // 存储引用资源的 ScriptableObject 路径

    [MenuItem("工具/资源路径检索引用工具")]
    public static void ShowWindow()
    {
        GetWindow<ResourceReferenceFinder>("资源路径检索引用工具");
    }

    private void OnGUI()
    {
        GUILayout.Label("查找场景、预制体和 ScriptableObject 引用的资源", EditorStyles.boldLabel);

        targetObject = EditorGUILayout.ObjectField("目标对象", targetObject, typeof(Object), false);

        if (GUILayout.Button("查找引用"))
        {
            FindReferences();
        }

        if (referencedScenes.Count > 0 || referencedPrefabs.Count > 0 || referencedScriptableObjects.Count > 0)
        {
            hasFoundReferences = true; // 找到引用
            GUILayout.Label("找到的引用:", EditorStyles.boldLabel);
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, true);

            GUILayout.Label("在场景中:");
            foreach (string sceneInfo in referencedScenes)
            {
                GUILayout.Label(sceneInfo);
            }
            GUILayout.Space(5); // 增加一些间隔
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider); // 添加分割线
            GUILayout.Space(5); // 增加一些间隔

            GUILayout.Label("在预制体中:");
            foreach (string prefabInfo in referencedPrefabs)
            {
                GUILayout.Label(prefabInfo);
            }
            GUILayout.Space(5); // 增加一些间隔
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider); // 添加分割线
            GUILayout.Space(5); // 增加一些间隔

            GUILayout.Label("在 ScriptableObjects 中:");
            foreach (string soInfo in referencedScriptableObjects)
            {
                GUILayout.Label(soInfo);
            }

            GUILayout.EndScrollView();
        }
        else if (hasFoundReferences) // 如果已经查找过，但没有找到引用
        {
            GUILayout.Label("未找到引用。", EditorStyles.boldLabel);
        }
    }

    private void FindReferences()
    {
        referencedScenes.Clear();
        referencedPrefabs.Clear();
        referencedScriptableObjects.Clear();
        hasFoundReferences = false; // 重置标志

        if (targetObject == null)
        {
            Debug.LogWarning("请选择一个目标对象进行引用检索。");
            return;
        }

        string targetGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(targetObject));

        // 搜索场景
        string[] allScenes = AssetDatabase.FindAssets("t:Scene");
        foreach (string sceneGUID in allScenes)
        {
            string scenePath = AssetDatabase.GUIDToAssetPath(sceneGUID);
            if (IsObjectReferencedInAsset(scenePath, targetGUID))
            {
                referencedScenes.Add($"场景: {scenePath}");
            }
        }

        // 搜索预制体
        string[] allPrefabs = AssetDatabase.FindAssets("t:Prefab");
        foreach (string prefabGUID in allPrefabs)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGUID);
            if (IsObjectReferencedInAsset(prefabPath, targetGUID))
            {
                referencedPrefabs.Add($"预制体: {prefabPath}");
            }
        }

        // 搜索 ScriptableObject
        string[] allScriptableObjects = AssetDatabase.FindAssets("t:ScriptableObject");
        foreach (string soGUID in allScriptableObjects)
        {
            string soPath = AssetDatabase.GUIDToAssetPath(soGUID);
            if (IsObjectReferencedInAsset(soPath, targetGUID))
            {
                referencedScriptableObjects.Add($"ScriptableObject: {soPath}");
            }
        }

        // 检查是否没有找到任何引用
        if (referencedScenes.Count == 0 && referencedPrefabs.Count == 0 && referencedScriptableObjects.Count == 0)
        {
            hasFoundReferences = true; // 设置找到引用的标志为 true
        }
    }

    // 检查资源在给定的文件（场景、预制体或 ScriptableObject）中是否被引用
    private bool IsObjectReferencedInAsset(string assetPath, string targetGUID)
    {
        string fileContent = File.ReadAllText(assetPath);
        return fileContent.Contains(targetGUID);
    }

    // 绘制边界框的辅助线（在编辑器模式下可见）
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(20, 10, 20)); // 绘制边界框
    }
}
