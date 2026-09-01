#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class TranslationItem
{
    public string traditional;
    public string english;
    public string japanese;
    public string korean;
}

public static class OpenAiTranslateClient
{
    private const int MaxFormatAttempts = 2;

    private static readonly string[] KoreanLabels =
    {
        "韩文", "韩语", "韩문", "韓文", "韓語", "korean", "한국어"
    };

    private static readonly string[] ChattyMarkers =
    {
        "让我先看看", "项目结构", "使用场景", "您希望我", "你希望我",
        "需要我做什么", "能告诉我", "请问您", "我可以帮",
        "what would you like", "how can i help", "could you tell me",
        "look at the project", "project structure"
    };

    [Serializable]
    private class ChatRequestPlain
    {
        public string model;
        public OpenAiReqMessage[] messages;
        public float temperature = 0.3f;
    }

    [Serializable]
    private class OpenAiReqMessage
    {
        public string role;
        public string content;
    }

    public static async Task<TranslationItem[]> TranslateBatchAsync(
        string apiKey,
        string baseUrl,
        string model,
        string systemPrompt,
        List<string> chineseTexts,
        LocalizationWriteTargets writeTargets = null)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new Exception("请先在设置中填写 API Key");

        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new Exception(
                "请填写中转站的 Base URL。\n" +
                "一般为控制台提供的 API 根地址，以 /v1 结尾，例如：https://你的中转.com/v1");

        baseUrl = NormalizeBaseUrl(baseUrl);
        string url = BuildChatCompletionsUrl(baseUrl);
        Exception lastError = null;

        for (int attempt = 1; attempt <= MaxFormatAttempts; attempt++)
        {
            try
            {
                if (attempt > 1)
                    Debug.LogWarning($"[本地化翻译] 格式不对，第 {attempt} 次重试（仅此时多耗一次）…");

                // 首次：system=完整 Prompt，user=仅编号文本（与改前接近，省 token）
                // 重试：user 前加一行格式提醒（不重复整段 Prompt/术语库）
                string userPayload = BuildNumberedUserPayload(chineseTexts, writeTargets, attempt);
                string json = JsonUtility.ToJson(new ChatRequestPlain
                {
                    model = model,
                    temperature = attempt > 1 ? 0.1f : 0.3f,
                    messages = new[]
                    {
                        new OpenAiReqMessage { role = "system", content = systemPrompt },
                        new OpenAiReqMessage { role = "user", content = userPayload }
                    }
                });

                string raw = await SendChatRequestAsync(url, apiKey, json, baseUrl);
                return ParseResponse(raw, url, chineseTexts.Count, writeTargets);
            }
            catch (Exception ex)
            {
                lastError = ex;
                if (attempt >= MaxFormatAttempts || !IsFormatError(ex.Message))
                    throw;
                await Task.Delay(600 * attempt);
            }
        }

        throw lastError ?? new Exception("翻译失败");
    }

    private static async Task<string> SendChatRequestAsync(string url, string apiKey, string json, string baseUrl)
    {
        Debug.Log($"[本地化翻译] POST {url}");

        using var request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.timeout = 120;
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);
        request.SetRequestHeader("HTTP-Referer", "https://memeecho.local");
        request.SetRequestHeader("X-Title", "MemeEcho Localization");

        var op = request.SendWebRequest();
        while (!op.isDone)
            await Task.Yield();

#if UNITY_2020_1_OR_NEWER
        if (request.result != UnityWebRequest.Result.Success)
#else
        if (request.isNetworkError || request.isHttpError)
#endif
        {
            long code = request.responseCode;
            string body = request.downloadHandler?.text ?? "";
            throw new Exception(FormatApiError(code, request.error, body, baseUrl));
        }

        return request.downloadHandler.text;
    }

    private static TranslationItem[] ParseResponse(string raw, string url, int expectedCount, LocalizationWriteTargets writeTargets = null)
    {
        if (IsHtmlResponse(raw))
        {
            throw new Exception(
                "收到网页 HTML，不是 API 数据。Base URL 填错了。\n\n" +
                $"实际请求：\n{url}\n\n" +
                "请填写中转控制台里的 API 根地址（通常以 /v1 结尾），\n" +
                "不要填网站首页或用户中心页面。");
        }

        string content = ExtractAssistantContent(raw);
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new Exception(
                "无法从 API 响应中读取翻译内容。\n" +
                $"请求：{url}\n" +
                "响应预览：\n" + raw.Substring(0, Math.Min(500, raw.Length)));
        }

        Debug.Log($"[本地化翻译] 模型返回预览：\n{content.Substring(0, Math.Min(600, content.Length))}");

        if (LooksLikeChattyAssistant(content))
        {
            throw new Exception(
                "模型没有在翻译，而是进入了助手聊天模式（例如分析项目、提问、闲聊）。\n" +
                "常见原因：中转把请求路由到编程/Agent 模型，或忽略了翻译指令。\n" +
                "建议：换纯聊天补全模型（如 gpt-4o-mini），批次改为 1，确认中转支持 system/user 消息。\n\n" +
                "模型返回预览：\n" + content.Substring(0, Math.Min(800, content.Length)));
        }

        var results = ParsePlainTextTranslations(content, expectedCount, writeTargets);
        if (results.Length != expectedCount)
        {
            throw new Exception(
                $"翻译条数不匹配：输入 {expectedCount} 条，解析到 {results.Length} 条。\n" +
                "请检查 Prompt 是否要求纯文本格式，或减小批次大小。\n\n" +
                "模型返回预览：\n" + content.Substring(0, Math.Min(800, content.Length)));
        }

        ValidateNonEmptyResults(results, content, writeTargets);

        return results;
    }

    public static void ValidateNonEmptyResults(TranslationItem[] results, string rawContent, LocalizationWriteTargets targets = null)
    {
        for (int i = 0; i < results.Length; i++)
        {
            var item = results[i];
            var invalidScript = SanitizeInvalidScriptFields(item, targets);

            if (targets != null)
            {
                var missing = new List<string>();
                if (targets.chineseF && string.IsNullOrWhiteSpace(item.traditional)) missing.Add("繁体");
                if (targets.english && string.IsNullOrWhiteSpace(item.english)) missing.Add("英文");
                if (targets.japanese && string.IsNullOrWhiteSpace(item.japanese)) missing.Add("日文");
                if (targets.korean && string.IsNullOrWhiteSpace(item.korean)) missing.Add("韩文");

                if (missing.Count == 0)
                    continue;

                string hint;
                if (LooksLikeChattyAssistant(rawContent))
                    hint = "模型进入了助手聊天模式，没有输出译文。\n";
                else if (invalidScript.Count > 0)
                    hint = $"语种错误：{string.Join("、", invalidScript)} 列不是对应语言（可能用中文代替了翻译）。\n";
                else if (!string.IsNullOrWhiteSpace(item.traditional) && LooksLikeChineseNarrative(rawContent))
                    hint = "模型似乎用中文续写/润色代替了英日韩翻译。\n";
                else
                    hint = "模型可能未输出全部语言，或格式无法识别。\n";

                throw new Exception(
                    $"第 {i + 1} 条缺少：{string.Join("、", missing)}。\n" +
                    hint +
                    "必须格式：[1] 下每行「繁体：…」「英文：…」「日文：…」「韩文：…」。\n" +
                    "建议：换模型 / 批次改 1 / 检查中转是否注入了 Agent 行为。\n\n" +
                    "模型返回预览：\n" + rawContent.Substring(0, Math.Min(800, rawContent.Length)));
            }

            if (HasAnyTranslation(item))
                continue;

            throw new Exception(
                $"第 {i + 1} 条翻译结果为空，未能从模型返回中解析出任何语言。\n" +
                "常见原因：模型输出格式与 Prompt 不一致（标签应为「繁体：」「英文：」「日文：」「韩文：」）。\n" +
                "建议：减小批次大小，或检查 Console 中的「模型返回预览」。\n\n" +
                "模型返回预览：\n" + rawContent.Substring(0, Math.Min(800, rawContent.Length)));
        }
    }

    private static List<string> SanitizeInvalidScriptFields(TranslationItem item, LocalizationWriteTargets targets)
    {
        var invalid = new List<string>();
        if (targets == null)
            return invalid;

        if (targets.english && !string.IsNullOrWhiteSpace(item.english)
            && !IsPlausibleScript(item.english, DetectedScript.English))
        {
            invalid.Add("英文");
            item.english = null;
        }
        if (targets.japanese && !string.IsNullOrWhiteSpace(item.japanese)
            && !IsPlausibleScript(item.japanese, DetectedScript.Japanese))
        {
            invalid.Add("日文");
            item.japanese = null;
        }
        if (targets.korean && !string.IsNullOrWhiteSpace(item.korean)
            && !IsPlausibleScript(item.korean, DetectedScript.Korean))
        {
            invalid.Add("韩文");
            item.korean = null;
        }
        return invalid;
    }

    /// <summary>
    /// 日文可全汉字（如「誰？」无假名），不能仅因无平假名/片假名就判错。
    /// </summary>
    private static bool IsPlausibleScript(string text, DetectedScript expected)
    {
        var detected = DetectScriptLanguage(text);
        switch (expected)
        {
            case DetectedScript.English:
                return detected == DetectedScript.English;
            case DetectedScript.Japanese:
                // 假名 → 日文；纯汉字/标点 → 常见短句日文，也接受
                return detected == DetectedScript.Japanese
                       || detected == DetectedScript.Traditional
                       || detected == DetectedScript.Unknown;
            case DetectedScript.Korean:
                return detected == DetectedScript.Korean;
            default:
                return true;
        }
    }

    private static bool HasAnyTranslation(TranslationItem item)
        => !string.IsNullOrWhiteSpace(item.traditional)
           || !string.IsNullOrWhiteSpace(item.english)
           || !string.IsNullOrWhiteSpace(item.japanese)
           || !string.IsNullOrWhiteSpace(item.korean);

    private static bool IsFormatError(string msg)
        => !string.IsNullOrEmpty(msg)
           && (msg.Contains("缺少：")
               || msg.Contains("翻译条数不匹配")
               || msg.Contains("未能从模型返回中解析")
               || msg.Contains("助手聊天模式")
               || msg.Contains("中文续写")
               || msg.Contains("语种错误"));

    private static bool LooksLikeChattyAssistant(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return false;

        // 已有正规标签则不算聊天
        if (content.Contains("繁体：") || content.Contains("英文：") || content.Contains("日文：") || content.Contains("韩文：")
            || content.Contains("繁體：") || content.Contains("English:") || content.Contains("Japanese:"))
            return false;

        string lower = content.ToLowerInvariant();
        foreach (var marker in ChattyMarkers)
        {
            if (lower.Contains(marker.ToLowerInvariant()))
                return true;
        }

        return false;
    }

    private static bool LooksLikeChineseNarrative(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return false;
        if (content.Contains("英文：") || content.Contains("日文：") || content.Contains("韩文：")
            || content.Contains("English:") || content.Contains("Japanese:") || content.Contains("Korean:"))
            return false;

        int cjk = 0, latin = 0, hangul = 0, kana = 0;
        foreach (char c in content)
        {
            if (c >= '\uAC00' && c <= '\uD7A3') hangul++;
            else if ((c >= '\u3040' && c <= '\u309F') || (c >= '\u30A0' && c <= '\u30FF')) kana++;
            else if (c >= '\u4E00' && c <= '\u9FFF') cjk++;
            else if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z')) latin++;
        }

        return cjk > 20 && latin < cjk / 4 && hangul == 0 && kana == 0;
    }

    public static string BuildNumberedUserPayload(
        List<string> chineseTexts,
        LocalizationWriteTargets writeTargets = null,
        int attempt = 1)
    {
        var sb = new StringBuilder();
        if (attempt > 1 && writeTargets != null)
        {
            sb.AppendLine("只输出标签译文，勿聊天：");
            sb.AppendLine("[1]");
            sb.AppendLine(writeTargets.BuildFormatExampleLine());
        }

        for (int i = 0; i < chineseTexts.Count; i++)
            sb.AppendLine($"[{i + 1}] {chineseTexts[i]}");
        return sb.ToString().TrimEnd();
    }

    public static TranslationItem[] ParsePlainTextTranslations(
        string content,
        int expectedCount,
        LocalizationWriteTargets writeTargets = null)
    {
        content = content.Trim();
        content = Regex.Replace(content, @"^```[\w]*\n?", "", RegexOptions.Multiline);
        content = Regex.Replace(content, @"\n?```\s*$", "", RegexOptions.Multiline);

        var results = new List<TranslationItem>();
        var blockPattern = new Regex(
            @"\[(\d+)\]\s*\n?(.*?)(?=\[\d+\]|$)",
            RegexOptions.Singleline);
        var matches = blockPattern.Matches(content);

        if (matches.Count > 0)
        {
            var map = new Dictionary<int, TranslationItem>();
            foreach (Match m in matches)
            {
                int index = int.Parse(m.Groups[1].Value);
                string body = m.Groups[2].Value.Trim();
                map[index] = ParseBlockBody(body, writeTargets);
            }

            for (int i = 1; i <= expectedCount; i++)
            {
                if (map.TryGetValue(i, out var item))
                    results.Add(item);
                else
                    results.Add(new TranslationItem());
            }
            return results.ToArray();
        }

        return ParseLineBasedFallback(content, expectedCount, writeTargets);
    }

    private static TranslationItem ParseBlockBody(string body, LocalizationWriteTargets targets = null)
    {
        var item = new TranslationItem();
        var unlabeledLines = new List<string>();
        foreach (var line in body.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
        {
            string trimmed = NormalizeLabelLine(line);
            string value = ExtractLabelValue(trimmed);
            if (value != null && TryAssignLabel(trimmed, value, item))
                continue;
            if (!string.IsNullOrWhiteSpace(trimmed))
                unlabeledLines.Add(trimmed);
        }

        if (unlabeledLines.Count > 0)
            FillMissingFromUnlabeled(item, unlabeledLines, targets);

        return item;
    }

    private static void FillMissingFromUnlabeled(
        TranslationItem item,
        List<string> lines,
        LocalizationWriteTargets targets)
    {
        if (targets == null || lines.Count == 0)
            return;

        var cleaned = new List<string>();
        foreach (var line in lines)
        {
            string stripped = StripListPrefix(line);
            if (!string.IsNullOrWhiteSpace(stripped))
                cleaned.Add(stripped);
        }
        if (cleaned.Count == 0)
            return;

        var fields = GetSelectedFieldsInOrder(targets);
        if (fields.Count == 0)
            return;

        if (cleaned.Count == fields.Count && CountEmptySelectedFields(item, targets) == fields.Count)
        {
            if (TryOrderAssignByScript(cleaned, item, targets))
                return;
        }

        foreach (var line in cleaned)
            TryAssignByScript(item, targets, DetectScriptLanguage(line), line);
    }

    private static bool TryOrderAssignByScript(
        List<string> lines,
        TranslationItem item,
        LocalizationWriteTargets targets)
    {
        var expected = GetExpectedScriptsInOrder(targets);
        var fields = GetSelectedFieldsInOrder(targets);
        if (lines.Count != expected.Count)
            return false;

        for (int i = 0; i < lines.Count; i++)
        {
            if (DetectScriptLanguage(lines[i]) != expected[i])
                return false;
        }

        for (int i = 0; i < fields.Count; i++)
            fields[i](item, lines[i]);
        return true;
    }

    private static List<DetectedScript> GetExpectedScriptsInOrder(LocalizationWriteTargets targets)
    {
        var scripts = new List<DetectedScript>();
        if (targets.chineseF) scripts.Add(DetectedScript.Traditional);
        if (targets.english) scripts.Add(DetectedScript.English);
        if (targets.japanese) scripts.Add(DetectedScript.Japanese);
        if (targets.korean) scripts.Add(DetectedScript.Korean);
        return scripts;
    }

    private static int CountEmptySelectedFields(TranslationItem item, LocalizationWriteTargets targets)
    {
        int count = 0;
        if (targets.chineseF && string.IsNullOrWhiteSpace(item.traditional)) count++;
        if (targets.english && string.IsNullOrWhiteSpace(item.english)) count++;
        if (targets.japanese && string.IsNullOrWhiteSpace(item.japanese)) count++;
        if (targets.korean && string.IsNullOrWhiteSpace(item.korean)) count++;
        return count;
    }

    private static string StripListPrefix(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return "";

        line = line.Trim();
        line = Regex.Replace(line, @"^\[\d+\]\s*", "");
        line = Regex.Replace(line, @"^\d+[\.\)、．]\s*", "");
        return line.Trim();
    }

    private enum DetectedScript { Traditional, English, Japanese, Korean, Unknown }

    private static DetectedScript DetectScriptLanguage(string text)
    {
        int hangul = 0, kana = 0, latin = 0, cjk = 0;
        foreach (char c in text)
        {
            if (c >= '\uAC00' && c <= '\uD7A3')
                hangul++;
            else if (c >= '\u1100' && c <= '\u11FF')
                hangul++;
            else if (c >= '\u3040' && c <= '\u309F')
                kana++;
            else if (c >= '\u30A0' && c <= '\u30FF')
                kana++;
            else if (c >= '\u4E00' && c <= '\u9FFF')
                cjk++;
            else if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))
                latin++;
        }

        if (hangul > 0)
            return DetectedScript.Korean;
        if (kana > 0)
            return DetectedScript.Japanese;
        if (latin > 0 && latin >= cjk)
            return DetectedScript.English;
        if (cjk > 0)
            return DetectedScript.Traditional;
        if (latin > 0)
            return DetectedScript.English;
        return DetectedScript.Unknown;
    }

    private static void TryAssignByScript(
        TranslationItem item,
        LocalizationWriteTargets targets,
        DetectedScript script,
        string text)
    {
        switch (script)
        {
            case DetectedScript.Traditional:
                if (targets.chineseF && string.IsNullOrWhiteSpace(item.traditional))
                    item.traditional = text;
                break;
            case DetectedScript.English:
                if (targets.english && string.IsNullOrWhiteSpace(item.english))
                    item.english = text;
                break;
            case DetectedScript.Japanese:
                if (targets.japanese && string.IsNullOrWhiteSpace(item.japanese))
                    item.japanese = text;
                break;
            case DetectedScript.Korean:
                if (targets.korean && string.IsNullOrWhiteSpace(item.korean))
                    item.korean = text;
                break;
        }
    }

    private static List<Action<TranslationItem, string>> GetSelectedFieldsInOrder(LocalizationWriteTargets targets)
    {
        var fields = new List<Action<TranslationItem, string>>();
        if (targets.chineseF) fields.Add((item, v) => item.traditional = v);
        if (targets.english) fields.Add((item, v) => item.english = v);
        if (targets.japanese) fields.Add((item, v) => item.japanese = v);
        if (targets.korean) fields.Add((item, v) => item.korean = v);
        return fields;
    }

    private static string NormalizeLabelLine(string line)
    {
        if (string.IsNullOrEmpty(line))
            return "";

        line = line.Trim();
        line = Regex.Replace(line, @"^[\*#_\->\s]+", "");
        line = Regex.Replace(line, @"[\*#_\s]+$", "");
        // 模型偶发把「韩文」写成「韩문」等变体，统一后再匹配
        line = Regex.Replace(line, @"^韩문\s*：", "韩文：");
        line = Regex.Replace(line, @"^한국어\s*：", "韩文：");
        return line.Trim();
    }

    private static string ExtractLabelValue(string line)
    {
        int colon = line.IndexOf(':');
        int fullColon = line.IndexOf('：');
        int sep = colon >= 0 && (fullColon < 0 || colon < fullColon) ? colon : fullColon;
        if (sep < 0)
            return null;
        return line.Substring(sep + 1).Trim();
    }

    private static bool IsLabel(string line, params string[] labels)
    {
        string lower = line.ToLowerInvariant();
        foreach (var label in labels)
        {
            string l = label.ToLowerInvariant();
            if (lower.StartsWith(l + ":") || lower.StartsWith(l + "："))
                return true;
        }
        return false;
    }

    private static bool TryAssignLabel(string trimmed, string value, TranslationItem current)
    {
        if (IsLabel(trimmed, "繁体", "繁體", "繁体中文", "繁體中文", "traditional", "chinesef"))
        {
            current.traditional = value;
            return true;
        }
        if (IsLabel(trimmed, "英文", "英语", "english"))
        {
            current.english = value;
            return true;
        }
        if (IsLabel(trimmed, "日文", "日语", "日語", "japanese"))
        {
            current.japanese = value;
            return true;
        }
        if (IsLabel(trimmed, KoreanLabels))
        {
            current.korean = value;
            return true;
        }
        return false;
    }

    private static TranslationItem[] ParseLineBasedFallback(
        string content,
        int expectedCount,
        LocalizationWriteTargets writeTargets = null)
    {
        var results = new List<TranslationItem>();
        var current = new TranslationItem();
        var unlabeledLines = new List<string>();
        foreach (var line in content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
        {
            string trimmed = NormalizeLabelLine(line);
            if (Regex.IsMatch(trimmed, @"^\[\d+\]$"))
                continue;

            string value = ExtractLabelValue(trimmed);
            if (value != null && TryAssignLabel(trimmed, value, current))
                continue;

            string stripped = StripListPrefix(trimmed);
            if (!string.IsNullOrWhiteSpace(stripped))
                unlabeledLines.Add(stripped);
        }

        FillMissingFromUnlabeled(current, unlabeledLines, writeTargets);

        if (HasAnyTranslation(current))
            results.Add(current);

        while (results.Count < expectedCount)
            results.Add(new TranslationItem());
        if (results.Count > expectedCount)
            results.RemoveRange(expectedCount, results.Count - expectedCount);
        return results.ToArray();
    }

    public static string NormalizeBaseUrl(string baseUrl)
    {
        baseUrl = baseUrl.Trim().TrimEnd('/');
        if (string.IsNullOrEmpty(baseUrl))
            return baseUrl;

        if (!baseUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !baseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            baseUrl = "https://" + baseUrl;

        const string chatSuffix = "/chat/completions";
        if (baseUrl.EndsWith(chatSuffix, StringComparison.OrdinalIgnoreCase))
            baseUrl = baseUrl.Substring(0, baseUrl.Length - chatSuffix.Length).TrimEnd('/');

        if (!baseUrl.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
        {
            if (baseUrl.EndsWith("/api", StringComparison.OrdinalIgnoreCase))
                return baseUrl + "/v1";
        }

        return baseUrl;
    }

    public static string BuildChatCompletionsUrl(string baseUrl)
    {
        baseUrl = NormalizeBaseUrl(baseUrl);
        const string suffix = "/chat/completions";
        if (baseUrl.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            return baseUrl;
        return baseUrl.TrimEnd('/') + suffix;
    }

    private static bool IsHtmlResponse(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return false;
        string trimmed = body.TrimStart();
        return trimmed.StartsWith("<!", StringComparison.OrdinalIgnoreCase)
               || trimmed.StartsWith("<html", StringComparison.OrdinalIgnoreCase)
               || trimmed.StartsWith("<!doctype", StringComparison.OrdinalIgnoreCase);
    }

    private static string ExtractAssistantContent(string responseJson)
    {
        var matches = Regex.Matches(
            responseJson,
            @"""content""\s*:\s*""((?:\\.|[^""\\])*)""",
            RegexOptions.Singleline);
        if (matches.Count > 0)
            return UnescapeJsonString(matches[matches.Count - 1].Groups[1].Value);

        var nullContent = Regex.Match(
            responseJson,
            @"""message""\s*:\s*\{[^}]*""content""\s*:\s*""((?:\\.|[^""\\])*)""",
            RegexOptions.Singleline);
        if (nullContent.Success)
            return UnescapeJsonString(nullContent.Groups[1].Value);

        return "";
    }

    private static string UnescapeJsonString(string s)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < s.Length; i++)
        {
            if (s[i] == '\\' && i + 1 < s.Length)
            {
                switch (s[i + 1])
                {
                    case 'n': sb.Append('\n'); i++; continue;
                    case 'r': sb.Append('\r'); i++; continue;
                    case 't': sb.Append('\t'); i++; continue;
                    case '"': sb.Append('"'); i++; continue;
                    case '\\': sb.Append('\\'); i++; continue;
                }
            }
            sb.Append(s[i]);
        }
        return sb.ToString();
    }

    public static string BuildGlossarySection(string glossaryPath)
    {
        if (string.IsNullOrEmpty(glossaryPath) || !System.IO.File.Exists(glossaryPath))
            return "（暂无术语，按常规翻译即可）";

        var doc = LocalizationCsvIO.ParseCsv(System.IO.File.ReadAllText(glossaryPath, Encoding.UTF8));
        if (doc.Count < 2)
            return "（暂无术语，按常规翻译即可）";

        var headers = doc[0];
        int idxCn = Array.IndexOf(headers, "简体中文");
        int idxTf = Array.IndexOf(headers, "繁体中文");
        int idxEn = Array.IndexOf(headers, "英文");
        int idxJp = Array.IndexOf(headers, "日文");
        if (idxCn < 0) idxCn = Array.IndexOf(headers, "Chinese");

        var lines = new List<string>();
        for (int r = 1; r < doc.Count; r++)
        {
            var row = doc[r];
            if (row.Length <= idxCn || string.IsNullOrWhiteSpace(row[idxCn]))
                continue;
            string cn = row[idxCn];
            string tf = idxTf >= 0 && idxTf < row.Length ? row[idxTf] : "";
            string en = idxEn >= 0 && idxEn < row.Length ? row[idxEn] : "";
            string jp = idxJp >= 0 && idxJp < row.Length ? row[idxJp] : "";
            lines.Add($"- {cn} → 繁体:{tf} | EN:{en} | JP:{jp}");
        }

        return lines.Count == 0 ? "（暂无术语，按常规翻译即可）" : string.Join("\n", lines);
    }

    private static string FormatApiError(long code, string error, string body, string baseUrl)
    {
        if (code == 403 && body.Contains("not available in your region"))
        {
            return
                "HTTP 403：当前地址无法访问该模型。\n" +
                "请检查中转平台是否支持该模型，或更换模型名。\n" +
                $"当前 Base URL：{baseUrl}\n{body}";
        }

        if (code == 401)
            return $"HTTP 401：API Key 无效，或与 Base URL 不是同一中转平台。\n{body}";

        if (code == 429)
            return $"HTTP 429：请求过于频繁或额度不足，请稍后重试或减小批次大小。\n{body}";

        if (code >= 500 || (body != null && body.Contains("upstream")))
        {
            return
                $"HTTP {code}：中转站上游模型服务异常（upstream error）。\n\n" +
                "常见原因：\n" +
                "• 当前模型过载、宕机或暂时不可用\n" +
                "• 单次发送文本过多（请把批次大小改为 3～5）\n" +
                "• 中转渠道不稳定\n\n" +
                "建议：换模型、减小批次、隔几分钟重试；持续失败请联系中转客服。\n\n" +
                $"Base URL：{baseUrl}\n{body}";
        }

        if (code == 404)
        {
            return
                $"HTTP 404：API 地址不正确。\n\n" +
                $"Base URL 只填中转文档里的根地址（通常到 /v1），不要含 chat/completions。\n" +
                $"程序会自动请求：{{Base URL}}/chat/completions\n\n" +
                $"当前 Base URL：{baseUrl}\n{body}";
        }

        return $"请求失败 (HTTP {code})\n{error}\n{body}";
    }
}

#endif
