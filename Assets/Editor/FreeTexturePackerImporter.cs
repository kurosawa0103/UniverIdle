#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

/// <summary>
/// 从 Free Texture Packer 导出的 atlas.png + atlas.json 一键生成 Multiple Sprite。
/// 右键选中 png / json（或两者）→ Import Free Texture Packer
/// </summary>
public static class FreeTexturePackerImporter
{
    const string MenuPath = "Assets/Import Free Texture Packer";

    [MenuItem(MenuPath, false, 2000)]
    static void ImportFromSelection()
    {
        if (!TryResolveAtlasPaths(Selection.assetGUIDs, out string texturePath, out string jsonPath))
        {
            EditorUtility.DisplayDialog(
                "Import Free Texture Packer",
                "请选中 atlas 的 .png 和/或同名的 .json 文件。\n例如：地图ui.png + 地图ui.json",
                "确定");
            return;
        }

        try
        {
            int count = ImportAtlas(texturePath, jsonPath);
            EditorUtility.DisplayDialog(
                "Import Free Texture Packer",
                $"已导入 {count} 个 Sprite：\n{Path.GetFileName(texturePath)}",
                "确定");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorUtility.DisplayDialog("Import Free Texture Packer", "导入失败：\n" + ex.Message, "确定");
        }
    }

    [MenuItem(MenuPath, true)]
    static bool ValidateImportFromSelection()
    {
        return TryResolveAtlasPaths(Selection.assetGUIDs, out _, out _);
    }

    public static int ImportAtlas(string textureAssetPath, string jsonAssetPath)
    {
        string jsonFullPath = ToFullPath(jsonAssetPath);
        if (!File.Exists(jsonFullPath))
            throw new FileNotFoundException("找不到 JSON 文件", jsonAssetPath);

        var atlas = FtpAtlasData.Load(jsonFullPath);

        string jsonImage = atlas.Meta?.Image;
        string pngFileName = Path.GetFileName(textureAssetPath);
        if (!string.IsNullOrEmpty(jsonImage) &&
            !string.Equals(jsonImage, pngFileName, StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogWarning(
                $"[FTP Import] JSON meta.image=\"{jsonImage}\" 与 PNG 文件名 \"{pngFileName}\" 不一致，仍继续导入。");
        }

        int texHeight = atlas.Meta?.Size?.H ?? 0;
        if (texHeight <= 0)
            throw new InvalidDataException("JSON meta.size.h 无效，无法转换坐标。");

        var importer = AssetImporter.GetAtPath(textureAssetPath) as TextureImporter;
        if (importer == null)
            throw new InvalidOperationException("无法获取 TextureImporter：" + textureAssetPath);

        ConfigureTextureImporter(importer);

        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
        dataProvider.InitSpriteEditorDataProvider();

        var existingByName = dataProvider.GetSpriteRects()
            .GroupBy(r => GetSpriteName(r.name))
            .ToDictionary(g => g.Key, g => g.First().spriteID);

        var spriteRects = new List<SpriteRect>(atlas.Frames.Count);
        int rotatedCount = 0;

        foreach (var frame in atlas.Frames)
        {
            if (frame.Rotated)
                rotatedCount++;

            string spriteName = GetSpriteName(frame.Key);
            var rect = frame.Frame;

            var spriteRect = new SpriteRect
            {
                name = spriteName,
                rect = ToUnityRect(rect.X, rect.Y, rect.W, rect.H, texHeight),
                pivot = CalcPivot(frame),
                alignment = SpriteAlignment.Custom,
                border = Vector4.zero,
                spriteID = existingByName.TryGetValue(spriteName, out GUID existingId)
                    ? existingId
                    : GUID.Generate()
            };

            spriteRects.Add(spriteRect);
        }

        if (rotatedCount > 0)
        {
            Debug.LogWarning(
                $"[FTP Import] {rotatedCount} 个 Sprite 在 JSON 中 marked rotated=true，" +
                "Unity 不支持旋转图块，这些 Sprite 的显示可能不正确。建议在 Free Texture Packer 中关闭 Allow Rotation。");
        }

        dataProvider.SetSpriteRects(spriteRects.ToArray());
        dataProvider.Apply();

        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();
        AssetDatabase.Refresh();

        Debug.Log($"[FTP Import] {textureAssetPath} ← {jsonAssetPath}，共 {spriteRects.Count} 个 Sprite。");
        return spriteRects.Count;
    }

    static void ConfigureTextureImporter(TextureImporter importer)
    {
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.npotScale = TextureImporterNPOTScale.None;
    }

    static Rect ToUnityRect(int x, int y, int w, int h, int textureHeight)
    {
        // Free Texture Packer：原点在左上；Unity Sprite rect：原点在左下
        return new Rect(x, textureHeight - y - h, w, h);
    }

    static Vector2 CalcPivot(FtpFrameEntry entry)
    {
        int fw = entry.Frame.W;
        int fh = entry.Frame.H;
        if (fw <= 0 || fh <= 0)
            return new Vector2(0.5f, 0.5f);

        float srcW = entry.SourceSize.W;
        float srcH = entry.SourceSize.H;
        float ssx = entry.SpriteSourceSize.X;
        float ssy = entry.SpriteSourceSize.Y;
        float px = entry.Pivot.X;
        float py = entry.Pivot.Y;

        float pivotX = (px * srcW - ssx) / fw;
        float pivotY = (py * srcH - (srcH - ssy - fh)) / fh;

        return new Vector2(
            Mathf.Clamp01(pivotX),
            Mathf.Clamp01(pivotY));
    }

    /// <summary>只取文件名，去掉路径前缀与 .png 后缀。</summary>
    static string GetSpriteName(string frameKey)
    {
        if (string.IsNullOrEmpty(frameKey))
            return frameKey;

        string name = frameKey.Replace('\\', '/');
        int slash = name.LastIndexOf('/');
        if (slash >= 0)
            name = name.Substring(slash + 1);

        return StripPngExtension(name);
    }

    static string StripPngExtension(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return fileName;

        if (fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            return fileName.Substring(0, fileName.Length - 4);

        return fileName;
    }

    static bool TryResolveAtlasPaths(string[] guids, out string texturePath, out string jsonPath)
    {
        texturePath = null;
        jsonPath = null;

        if (guids == null || guids.Length == 0)
            return false;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path))
                continue;

            if (path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                texturePath = path;
            else if (path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                jsonPath = path;
        }

        if (!string.IsNullOrEmpty(texturePath) && string.IsNullOrEmpty(jsonPath))
            jsonPath = Path.ChangeExtension(texturePath, ".json");
        else if (!string.IsNullOrEmpty(jsonPath) && string.IsNullOrEmpty(texturePath))
            texturePath = Path.ChangeExtension(jsonPath, ".png");

        if (string.IsNullOrEmpty(texturePath) || string.IsNullOrEmpty(jsonPath))
            return false;

        return File.Exists(ToFullPath(texturePath)) && File.Exists(ToFullPath(jsonPath));
    }

    static string ToFullPath(string assetPath)
    {
        return Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
    }

    // ── JSON 数据结构 ──────────────────────────────────────────

    sealed class FtpAtlasData
    {
        public List<FtpFrameEntry> Frames = new List<FtpFrameEntry>();
        public FtpMeta Meta;

        public static FtpAtlasData Load(string jsonFullPath)
        {
            string json = File.ReadAllText(jsonFullPath);
            var root = SimpleJson.Deserialize(json) as Dictionary<string, object>;
            if (root == null)
                throw new InvalidDataException("JSON 根节点不是 object。");

            var data = new FtpAtlasData();

            if (root.TryGetValue("frames", out object framesObj) && framesObj is Dictionary<string, object> frames)
            {
                foreach (var kv in frames)
                {
                    if (kv.Value is Dictionary<string, object> frameDict)
                        data.Frames.Add(FtpFrameEntry.Parse(kv.Key, frameDict));
                }
            }
            else
            {
                throw new InvalidDataException("JSON 缺少 frames 对象。");
            }

            if (root.TryGetValue("meta", out object metaObj) && metaObj is Dictionary<string, object> metaDict)
                data.Meta = FtpMeta.Parse(metaDict);

            data.Frames.Sort((a, b) => string.Compare(a.Key, b.Key, StringComparison.Ordinal));
            return data;
        }
    }

    sealed class FtpFrameEntry
    {
        public string Key;
        public FtpRect Frame;
        public FtpRect SpriteSourceSize;
        public FtpSize SourceSize;
        public FtpPivot Pivot;
        public bool Rotated;
        public bool Trimmed;

        public static FtpFrameEntry Parse(string key, Dictionary<string, object> dict)
        {
            var entry = new FtpFrameEntry { Key = key };

            if (dict.TryGetValue("frame", out object frameObj))
                entry.Frame = FtpRect.Parse(frameObj as Dictionary<string, object>);

            if (dict.TryGetValue("spriteSourceSize", out object ssObj))
                entry.SpriteSourceSize = FtpRect.Parse(ssObj as Dictionary<string, object>);

            if (dict.TryGetValue("sourceSize", out object srcObj))
                entry.SourceSize = FtpSize.Parse(srcObj as Dictionary<string, object>);

            if (dict.TryGetValue("pivot", out object pivotObj))
                entry.Pivot = FtpPivot.Parse(pivotObj as Dictionary<string, object>);

            entry.Rotated = ReadBool(dict, "rotated");
            entry.Trimmed = ReadBool(dict, "trimmed");

            entry.Frame ??= new FtpRect();
            entry.SpriteSourceSize ??= new FtpRect { W = entry.Frame.W, H = entry.Frame.H };
            entry.SourceSize ??= new FtpSize { W = entry.Frame.W, H = entry.Frame.H };
            entry.Pivot ??= new FtpPivot { X = 0.5f, Y = 0.5f };

            return entry;
        }
    }

    sealed class FtpRect
    {
        public int X;
        public int Y;
        public int W;
        public int H;

        public static FtpRect Parse(Dictionary<string, object> dict)
        {
            if (dict == null)
                return null;

            return new FtpRect
            {
                X = ReadInt(dict, "x"),
                Y = ReadInt(dict, "y"),
                W = ReadInt(dict, "w"),
                H = ReadInt(dict, "h")
            };
        }
    }

    sealed class FtpSize
    {
        public int W;
        public int H;

        public static FtpSize Parse(Dictionary<string, object> dict)
        {
            if (dict == null)
                return null;

            return new FtpSize
            {
                W = ReadInt(dict, "w"),
                H = ReadInt(dict, "h")
            };
        }
    }

    sealed class FtpPivot
    {
        public float X = 0.5f;
        public float Y = 0.5f;

        public static FtpPivot Parse(Dictionary<string, object> dict)
        {
            if (dict == null)
                return null;

            return new FtpPivot
            {
                X = ReadFloat(dict, "x", 0.5f),
                Y = ReadFloat(dict, "y", 0.5f)
            };
        }
    }

    sealed class FtpMeta
    {
        public string Image;
        public FtpSize Size;

        public static FtpMeta Parse(Dictionary<string, object> dict)
        {
            var meta = new FtpMeta
            {
                Image = dict.TryGetValue("image", out object image) ? image as string : null
            };

            if (dict.TryGetValue("size", out object sizeObj))
                meta.Size = FtpSize.Parse(sizeObj as Dictionary<string, object>);

            return meta;
        }
    }

    static int ReadInt(Dictionary<string, object> dict, string key)
    {
        if (!dict.TryGetValue(key, out object value) || value == null)
            return 0;

        switch (value)
        {
            case long l: return (int)l;
            case int i: return i;
            case double d: return (int)d;
            case float f: return (int)f;
            case string s when int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed):
                return parsed;
            default:
                return Convert.ToInt32(value, CultureInfo.InvariantCulture);
        }
    }

    static float ReadFloat(Dictionary<string, object> dict, string key, float fallback)
    {
        if (!dict.TryGetValue(key, out object value) || value == null)
            return fallback;

        switch (value)
        {
            case double d: return (float)d;
            case float f: return f;
            case long l: return l;
            case int i: return i;
            case string s when float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed):
                return parsed;
            default:
                return Convert.ToSingle(value, CultureInfo.InvariantCulture);
        }
    }

    static bool ReadBool(Dictionary<string, object> dict, string key)
    {
        if (!dict.TryGetValue(key, out object value) || value == null)
            return false;

        if (value is bool b)
            return b;

        if (value is string s && bool.TryParse(s, out bool parsed))
            return parsed;

        return Convert.ToBoolean(value, CultureInfo.InvariantCulture);
    }

    /// <summary>轻量 JSON 解析（仅 Deserialize，供 Editor 导入使用）。</summary>
    static class SimpleJson
    {
        public static object Deserialize(string json)
        {
            if (string.IsNullOrEmpty(json))
                return null;

            return Parser.Parse(json);
        }

        sealed class Parser
        {
            readonly string _json;
            int _index;

            Parser(string json)
            {
                _json = json;
                _index = 0;
            }

            public static object Parse(string json)
            {
                var parser = new Parser(json);
                object result = parser.ParseValue();
                parser.SkipWhitespace();
                return result;
            }

            object ParseValue()
            {
                SkipWhitespace();
                if (_index >= _json.Length)
                    return null;

                char c = _json[_index];
                switch (c)
                {
                    case '{': return ParseObject();
                    case '[': return ParseArray();
                    case '"': return ParseString();
                    case 't': return ParseLiteral("true", true);
                    case 'f': return ParseLiteral("false", false);
                    case 'n': return ParseLiteral("null", null);
                    default: return ParseNumber();
                }
            }

            Dictionary<string, object> ParseObject()
            {
                var dict = new Dictionary<string, object>();
                _index++; // skip {

                while (true)
                {
                    SkipWhitespace();
                    if (TryConsume('}'))
                        break;

                    string key = ParseString();
                    SkipWhitespace();
                    Expect(':');
                    dict[key] = ParseValue();

                    SkipWhitespace();
                    if (TryConsume('}'))
                        break;
                    Expect(',');
                }

                return dict;
            }

            List<object> ParseArray()
            {
                var list = new List<object>();
                _index++; // skip [

                while (true)
                {
                    SkipWhitespace();
                    if (TryConsume(']'))
                        break;

                    list.Add(ParseValue());

                    SkipWhitespace();
                    if (TryConsume(']'))
                        break;
                    Expect(',');
                }

                return list;
            }

            string ParseString()
            {
                Expect('"');
                var sb = new System.Text.StringBuilder();

                while (_index < _json.Length)
                {
                    char c = _json[_index++];
                    if (c == '"')
                        return sb.ToString();

                    if (c == '\\' && _index < _json.Length)
                    {
                        char esc = _json[_index++];
                        switch (esc)
                        {
                            case '"': sb.Append('"'); break;
                            case '\\': sb.Append('\\'); break;
                            case '/': sb.Append('/'); break;
                            case 'b': sb.Append('\b'); break;
                            case 'f': sb.Append('\f'); break;
                            case 'n': sb.Append('\n'); break;
                            case 'r': sb.Append('\r'); break;
                            case 't': sb.Append('\t'); break;
                            case 'u':
                                string hex = _json.Substring(_index, 4);
                                _index += 4;
                                sb.Append((char)Convert.ToInt32(hex, 16));
                                break;
                            default: sb.Append(esc); break;
                        }
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }

                throw new InvalidDataException("JSON 字符串未闭合。");
            }

            object ParseNumber()
            {
                int start = _index;
                if (_json[_index] == '-')
                    _index++;

                while (_index < _json.Length && char.IsDigit(_json[_index]))
                    _index++;

                bool isFloat = false;
                if (_index < _json.Length && _json[_index] == '.')
                {
                    isFloat = true;
                    _index++;
                    while (_index < _json.Length && char.IsDigit(_json[_index]))
                        _index++;
                }

                if (_index < _json.Length && (_json[_index] == 'e' || _json[_index] == 'E'))
                {
                    isFloat = true;
                    _index++;
                    if (_index < _json.Length && (_json[_index] == '+' || _json[_index] == '-'))
                        _index++;
                    while (_index < _json.Length && char.IsDigit(_json[_index]))
                        _index++;
                }

                string number = _json.Substring(start, _index - start);
                if (isFloat)
                    return double.Parse(number, CultureInfo.InvariantCulture);

                return long.Parse(number, CultureInfo.InvariantCulture);
            }

            object ParseLiteral(string literal, object value)
            {
                if (!_json.Substring(_index, literal.Length).Equals(literal, StringComparison.Ordinal))
                    throw new InvalidDataException("JSON 字面量解析失败：" + literal);

                _index += literal.Length;
                return value;
            }

            void SkipWhitespace()
            {
                while (_index < _json.Length && char.IsWhiteSpace(_json[_index]))
                    _index++;
            }

            bool TryConsume(char expected)
            {
                if (_index < _json.Length && _json[_index] == expected)
                {
                    _index++;
                    return true;
                }

                return false;
            }

            void Expect(char expected)
            {
                SkipWhitespace();
                if (_index >= _json.Length || _json[_index] != expected)
                    throw new InvalidDataException($"JSON 期望 '{expected}'，位置 {_index}。");

                _index++;
            }
        }
    }
}
#endif
