#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

[System.Serializable]
public class LocalizationWriteTargets
{
    public bool chineseF = true;
    public bool english = true;
    public bool japanese = true;
    public bool korean = true;

    public bool AnySelected => chineseF || english || japanese || korean;

    public bool AllSelected => chineseF && english && japanese && korean;

    public void SelectAll()
    {
        chineseF = true;
        english = true;
        japanese = true;
        korean = true;
    }

    public void SelectOnlyChineseF()
    {
        chineseF = true;
        english = false;
        japanese = false;
        korean = false;
    }

    public void SelectOnlyEnglish()
    {
        chineseF = false;
        english = true;
        japanese = false;
        korean = false;
    }

    public void SelectOnlyJapanese()
    {
        chineseF = false;
        english = false;
        japanese = true;
        korean = false;
    }

    public void SelectOnlyKorean()
    {
        chineseF = false;
        english = false;
        japanese = false;
        korean = true;
    }

    public string GetSummaryLabel()
    {
        if (!AnySelected)
            return "（未选择）";

        var parts = new List<string>();
        if (chineseF) parts.Add("繁体");
        if (english) parts.Add("英文");
        if (japanese) parts.Add("日文");
        if (korean) parts.Add("韩文");
        return string.Join(" / ", parts);
    }

    public static void DrawEditorGui(LocalizationWriteTargets targets)
    {
        EditorGUILayout.LabelField("写入列（可多选，只写入勾选的列）", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        targets.chineseF = EditorGUILayout.ToggleLeft("ChineseF 繁体", targets.chineseF, GUILayout.Width(118));
        targets.english = EditorGUILayout.ToggleLeft("English 英文", targets.english, GUILayout.Width(118));
        targets.japanese = EditorGUILayout.ToggleLeft("Japanese 日文", targets.japanese, GUILayout.Width(118));
        targets.korean = EditorGUILayout.ToggleLeft("Korean 韩文", targets.korean, GUILayout.Width(118));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("全选", GUILayout.Width(48)))
            targets.SelectAll();
        if (GUILayout.Button("仅繁体", GUILayout.Width(56)))
            targets.SelectOnlyChineseF();
        if (GUILayout.Button("仅英文", GUILayout.Width(56)))
            targets.SelectOnlyEnglish();
        if (GUILayout.Button("仅日文", GUILayout.Width(56)))
            targets.SelectOnlyJapanese();
        if (GUILayout.Button("仅韩文", GUILayout.Width(56)))
            targets.SelectOnlyKorean();
        EditorGUILayout.EndHorizontal();

        if (!targets.AnySelected)
        {
            EditorGUILayout.HelpBox("请至少勾选一门要写入的语言。", MessageType.Warning);
        }
        else if (!targets.AllSelected)
        {
            EditorGUILayout.HelpBox(
                $"当前写入：{targets.GetSummaryLabel()}\n未勾选的语言列不会被修改。",
                MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox(
                "四门语言同翻时建议批次大小 3～5，过大容易导致模型输出截断或格式错乱。",
                MessageType.Info);
        }
    }

    public string BuildPromptTemplate(bool replaceCommaWithC, string extraContext = null)
    {
        var labels = new List<string>();
        if (chineseF) labels.Add("繁体");
        if (english) labels.Add("英文");
        if (japanese) labels.Add("日文");
        if (korean) labels.Add("韩文");

        var sb = new StringBuilder();
        sb.Append("游戏本地化：简中→").Append(string.Join("/", labels));
        sb.AppendLine("。直接输出译文，勿提问勿闲聊勿续写。");
        sb.AppendLine("每条必须带标签：");
        sb.AppendLine("[1]");
        foreach (var label in labels)
            sb.AppendLine(label + "：译文");
        sb.Append("保留{c}{0}。");
        if (english && replaceCommaWithC)
            sb.Append("英文可用半角逗号。");
        if (!string.IsNullOrWhiteSpace(extraContext))
            sb.Append("风格：").Append(extraContext.Trim()).Append('。');
        sb.Append("术语：{glossary}");
        return sb.ToString();
    }

    public string BuildFormatExampleLine()
    {
        var parts = new List<string>();
        if (chineseF) parts.Add("繁体：…");
        if (english) parts.Add("英文：…");
        if (japanese) parts.Add("日文：…");
        if (korean) parts.Add("韩文：…");
        return string.Join("\n", parts);
    }
}

#endif
