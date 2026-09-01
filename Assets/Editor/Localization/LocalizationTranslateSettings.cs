#if UNITY_EDITOR
using UnityEngine;

[CreateAssetMenu(fileName = "LocalizationTranslateSettings", menuName = "MemeEcho/Localization Translate Settings")]
public class LocalizationTranslateSettings : ScriptableObject
{
    [Header("API 中转站（OpenAI 兼容）")]
    [Tooltip("中转平台控制台提供的 API Key")]
    public string apiKey = "";
    [Tooltip("中转给的根地址，通常以 /v1 结尾，例如 https://你的域名/v1")]
    public string baseUrl = "";
    [Tooltip("中转文档中的模型名，例如 gpt-4o-mini")]
    public string model = "gpt-4o-mini";
    public int batchSize = 5;

    [Header("翻译规则")]
    public bool replaceCommaWithC = true;
    public bool onlyUntranslatedRows = true;
    public LocalizationWriteTargets writeTargets = new LocalizationWriteTargets();

    [TextArea(2, 6)]
    [Tooltip("附加到自动 Prompt 的游戏/风格说明，例如「这是一款电波系梦核文字冒险AVG游戏」")]
    public string promptExtraContext = "";

    [TextArea(10, 24)]
    [Tooltip("已弃用：写入列会自动生成 Prompt。保留此字段仅作参考，实际翻译以写入列为准。")]
    public string promptTemplate =
        "你是专业游戏本地化翻译，将简体中文翻译为繁体中文、英文、日文、韩文。\n\n" +
        "## 输出格式（必须严格遵守）\n" +
        "不要使用 JSON，不要使用 markdown 代码块。\n" +
        "对输入的每一条，按编号输出：\n" +
        "[1]\n" +
        "繁体：繁体中文译文\n" +
        "英文：English translation\n" +
        "日文：日文訳\n" +
        "韩文：한국어 번역\n" +
        "[2]\n" +
        "繁体：...\n" +
        "英文：...\n" +
        "日文：...\n" +
        "韩文：...\n\n" +
        "## 翻译要求\n" +
        "1. 保持叙事/对话语气，适合文字冒险游戏\n" +
        "2. 保留 {c}、{0}、\\n 等占位符，不要增删\n" +
        "3. 英文可用半角逗号（程序会自动替换为 {c}）\n" +
        "4. 编号与输入一一对应，条数必须相同\n\n" +
        "## 术语库\n" +
        "{glossary}\n\n" +
        "## 待翻译（已编号）\n" +
        "见用户消息。";

    public void EnsureInitialized()
    {
        if (writeTargets == null)
            writeTargets = new LocalizationWriteTargets();
    }

    public string GetActivePromptTemplate()
    {
        EnsureInitialized();

        if (!writeTargets.AnySelected)
            return promptTemplate;

        return writeTargets.BuildPromptTemplate(replaceCommaWithC, promptExtraContext);
    }

    public string glossaryCsvPath = "Assets/Editor/Localization/glossary.csv";
}

#endif
