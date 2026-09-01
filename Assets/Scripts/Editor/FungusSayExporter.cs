using UnityEngine;
using UnityEditor;
using Fungus;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Linq;
using System.Globalization;

public class FungusTextIO : EditorWindow
{
    private Flowchart targetFlowchart;
    private TextAsset importFile;
    private string exportFolder = "Assets/游戏内剧情文案导出";

    [MenuItem("工具/Fungus 文本导入导出")]
    public static void ShowWindow()
    {
        GetWindow<FungusTextIO>("Fungus Text I/O");
    }

    void OnGUI()
    {
        GUILayout.Label("📘 Fungus 对话与菜单文本 导入 / 导出", EditorStyles.boldLabel);

        targetFlowchart = (Flowchart)EditorGUILayout.ObjectField("目标 Flowchart", targetFlowchart, typeof(Flowchart), true);
        importFile = (TextAsset)EditorGUILayout.ObjectField("导入文本文件 (.txt)", importFile, typeof(TextAsset), false);
        exportFolder = EditorGUILayout.TextField("导出文件夹", exportFolder);

        GUILayout.Space(10);

        if (GUILayout.Button("🔍 自动匹配场景对应资源", GUILayout.Height(30)))
        {
            AutoMatch();
        }

        GUILayout.Space(10);
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("⬆️ 导出文本", GUILayout.Height(35)))
        {
            if (targetFlowchart == null)
            {
                EditorUtility.DisplayDialog("错误", "请先指定 Flowchart！", "OK");
                return;
            }
            ExportTexts();
        }

        if (GUILayout.Button("⬇️ 导入文本", GUILayout.Height(35)))
        {
            if (targetFlowchart == null || importFile == null)
            {
                EditorUtility.DisplayDialog("错误", "请指定 Flowchart 和文本文件！", "OK");
                return;
            }
            ImportTexts();
        }

        if (GUILayout.Button("📤 导出 CSV（多语言）", GUILayout.Height(35)))
        {
            ExportToCSV();
        }

        GUILayout.EndHorizontal();
    }

    // ===================== 自动匹配逻辑 =====================
    void AutoMatch()
    {
        string sceneName = SceneManager.GetActiveScene().name;

        // 自动查找名为“事件”的Flowchart
        targetFlowchart = GameObject.Find("事件")?.GetComponent<Flowchart>();
        if (targetFlowchart != null)
        {
            Debug.Log($"✅ 找到 Flowchart: {targetFlowchart.name}");
        }
        else
        {
            Debug.LogWarning("⚠️ 未找到名为“事件”的 Flowchart！");
        }

        // 自动查找导入文件
        string importPath = Path.Combine(exportFolder, $"{sceneName}.txt");
        importPath = importPath.Replace("\\", "/");
        TextAsset foundText = AssetDatabase.LoadAssetAtPath<TextAsset>(importPath);

        if (foundText != null)
        {
            importFile = foundText;
            Debug.Log($"✅ 找到导入文本文件: {importPath}");
        }
        else
        {
            Debug.LogWarning($"⚠️ 未找到导入文本文件: {importPath}");
        }

        if (targetFlowchart != null && importFile != null)
        {
            EditorUtility.DisplayDialog("匹配成功", $"已找到 Flowchart “事件” 与文件：\n{importPath}", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("部分匹配失败", $"Flowchart 或文本文件未找到。\n\n查找路径：\n{importPath}", "OK");
        }
    }

    // ===================== 导出逻辑 =====================
    void ExportTexts()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        string exportPath = Path.Combine(exportFolder, $"{sceneName}.txt");

        StringBuilder sb = new StringBuilder();

        foreach (Block block in targetFlowchart.GetComponents<Block>())
        {
            sb.AppendLine($"=== Block: {block.BlockName} ===");

            foreach (Command cmd in block.CommandList)
            {
                if (cmd is Say sayCmd)
                {
                    SerializedObject so = new SerializedObject(sayCmd);
                    string text = GetStringProperty(so, "storyText");
                    if (string.IsNullOrEmpty(text))
                        text = GetStringProperty(so, "text");

                    string characterName = GetObjectNameProperty(so, "character");
                    if (string.IsNullOrEmpty(characterName))
                        characterName = GetObjectNameProperty(so, "setCharacter");
                    if (string.IsNullOrEmpty(characterName))
                        characterName = "Narrator";

                    sb.AppendLine($"{characterName}: {text}");
                }
                else if (cmd is Fungus.Menu menuCmd)
                {
                    SerializedObject so = new SerializedObject(menuCmd);
                    string text = GetStringProperty(so, "text");
                    if (string.IsNullOrEmpty(text))
                        text = GetStringProperty(so, "description");

                    string targetBlock = GetObjectNameProperty(so, "targetBlock");
                    if (!string.IsNullOrEmpty(targetBlock))
                        sb.AppendLine($"[分支选项 → {targetBlock}] {text}");
                    else
                        sb.AppendLine($"[分支选项] {text}");
                }
            }

            sb.AppendLine();
        }

        if (!Directory.Exists(exportFolder))
            Directory.CreateDirectory(exportFolder);

        File.WriteAllText(exportPath, sb.ToString(), Encoding.UTF8);
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("完成", $"文本已导出到:\n{exportPath}", "OK");
    }

    // ===================== 导入逻辑（含行数统计） =====================
    void ImportTexts()
    {
        string[] lines = importFile.text.Split('\n');
        Block currentBlock = null;
        int sayIndex = 0;
        int menuIndex = 0;

        int sayChanged = 0;
        int menuChanged = 0;

        foreach (string rawLine in lines)
        {
            string line = rawLine.Trim();
            if (string.IsNullOrEmpty(line)) continue;

            if (line.StartsWith("=== Block:"))
            {
                string blockName = line.Replace("=== Block:", "").Replace("===", "").Trim();
                currentBlock = FindBlock(blockName);
                sayIndex = 0;
                menuIndex = 0;
                continue;
            }

            if (currentBlock == null) continue;

            if (line.Contains(":") && !line.StartsWith("["))
            {
                int idx = line.IndexOf(':');
                string text = line.Substring(idx + 1).Trim();
                var sayCmds = currentBlock.CommandList.FindAll(c => c is Say);

                if (sayIndex < sayCmds.Count)
                {
                    Say say = (Say)sayCmds[sayIndex];
                    SerializedObject so = new SerializedObject(say);
                    var sp = so.FindProperty("storyText") ?? so.FindProperty("text");
                    if (sp != null && sp.stringValue != text)
                    {
                        sp.stringValue = text;
                        so.ApplyModifiedProperties();
                        sayChanged++;
                    }
                    sayIndex++;
                }
                else
                {
                    Debug.LogWarning($"⚠️ {currentBlock.BlockName} 的对白数量不匹配：多出文本 → {text}");
                }
            }
            else if (line.StartsWith("[分支选项"))
            {
                int idx = line.IndexOf("]");
                string text = line.Substring(idx + 1).Trim();
                var menuCmds = currentBlock.CommandList.FindAll(c => c is Fungus.Menu);

                if (menuIndex < menuCmds.Count)
                {
                    Fungus.Menu menu = (Fungus.Menu)menuCmds[menuIndex];
                    SerializedObject so = new SerializedObject(menu);
                    var sp = so.FindProperty("text") ?? so.FindProperty("description");
                    if (sp != null && sp.stringValue != text)
                    {
                        sp.stringValue = text;
                        so.ApplyModifiedProperties();
                        menuChanged++;
                    }
                    menuIndex++;
                }
                else
                {
                    Debug.LogWarning($"⚠️ {currentBlock.BlockName} 的分支数量不匹配：多出选项 → {text}");
                }
            }
        }

        EditorUtility.SetDirty(targetFlowchart);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        int total = sayChanged + menuChanged;
        string msg = $"对白修改: {sayChanged} 行\n分支修改: {menuChanged} 行\n总计修改: {total} 行";
        Debug.Log($"✅ 导入完成 - {msg}");
        EditorUtility.DisplayDialog("导入完成", msg, "OK");
    }

    // ===================== 工具函数 =====================
    Block FindBlock(string blockName)
    {
        foreach (var block in targetFlowchart.GetComponents<Block>())
            if (block.BlockName == blockName)
                return block;
        return null;
    }

    string GetStringProperty(SerializedObject so, string propertyName)
    {
        SerializedProperty sp = so.FindProperty(propertyName);
        return sp != null && sp.propertyType == SerializedPropertyType.String ? sp.stringValue : null;
    }

    string GetObjectNameProperty(SerializedObject so, string propertyName)
    {
        SerializedProperty sp = so.FindProperty(propertyName);
        if (sp != null && sp.propertyType == SerializedPropertyType.ObjectReference)
            if (sp.objectReferenceValue != null)
                return sp.objectReferenceValue.name;
        return null;
    }

    void ExportToCSV()
    {
        if (targetFlowchart == null)
        {
            EditorUtility.DisplayDialog("错误", "请先指定 Flowchart！", "OK");
            return;
        }

        string sceneName = SceneManager.GetActiveScene().name;
        string exportPath = Path.Combine(exportFolder, "Languages.csv");

        if (!Directory.Exists(exportFolder))
            Directory.CreateDirectory(exportFolder);

        // ✅ Step 1: 如果已存在旧CSV，先读入旧数据
        Dictionary<string, string[]> oldTranslations = new Dictionary<string, string[]>();
        if (File.Exists(exportPath))
        {
            var lines = File.ReadAllLines(exportPath, Encoding.UTF8);
            for (int i = 1; i < lines.Length; i++) // 跳过header
            {
                string[] cols = ParseCSVLine(lines[i]);
                if (cols.Length >= 7)
                {
                    string key = cols[0];
                    oldTranslations[key] = cols;
                }
            }
        }

        // ✅ Step 2: 遍历 Flowchart，导出文本
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Key,Type,Desc,Chinese,English,Japanese,null");

        Dictionary<string, bool> seen = new Dictionary<string, bool>();

        foreach (Block block in targetFlowchart.GetComponents<Block>())
        {
            foreach (Command cmd in block.CommandList)
            {
                string text = null;
                string type = "Text";
                string desc = "对白";

                if (cmd is Say sayCmd)
                {
                    SerializedObject so = new SerializedObject(sayCmd);
                    text = GetStringProperty(so, "storyText");
                    if (string.IsNullOrEmpty(text))
                        text = GetStringProperty(so, "text");
                    type = "Text";
                }
                else if (cmd is Fungus.Menu menuCmd)
                {
                    SerializedObject so = new SerializedObject(menuCmd);
                    text = GetStringProperty(so, "text");
                    if (string.IsNullOrEmpty(text))
                        text = GetStringProperty(so, "description");
                    type = "Text";
                    desc = "选项";
                }

                if (string.IsNullOrEmpty(text))
                    continue;

                string key = text; // ✅ Key = 中文原文
                if (seen.ContainsKey(key))
                    continue;
                seen[key] = true;

                string english = "";
                string japanese = "";
                string extra = "";

                // ✅ Step 3: 如果旧表有该key，保留旧翻译
                if (oldTranslations.TryGetValue(key, out var oldCols))
                {
                    if (oldCols.Length > 4) english = oldCols[4];
                    if (oldCols.Length > 5) japanese = oldCols[5];
                    if (oldCols.Length > 6) extra = oldCols[6];
                }

                sb.AppendLine($"{EscapeCSV(key)},{type},{desc},{EscapeCSV(text)},{EscapeCSV(english)},{EscapeCSV(japanese)},{EscapeCSV(extra)}");
            }
        }

        // ✅ Step 4: 追加旧表中存在但Flowchart里已不存在的行（防止丢失）
        foreach (var kv in oldTranslations)
        {
            if (seen.ContainsKey(kv.Key)) continue; // 已保留
            sb.AppendLine(string.Join(",", kv.Value));
        }

        File.WriteAllText(exportPath, sb.ToString(), Encoding.UTF8);
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("完成", $"CSV 导出成功！（已保留旧翻译）\n路径：\n{exportPath}", "OK");
        Debug.Log($"✅ CSV 导出完成（保留翻译）: {exportPath}");
    }

    string[] ParseCSVLine(string line)
    {
        List<string> result = new List<string>();
        bool inQuotes = false;
        StringBuilder current = new StringBuilder();

        foreach (char c in line)
        {
            if (c == '\"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        result.Add(current.ToString());
        return result.ToArray();
    }


    // ===================== 工具函数：转义 CSV 格式 =====================
    string EscapeCSV(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        value = value.Replace("\"", "\"\"");
        if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
            value = $"\"{value}\"";
        return value;
    }
}
