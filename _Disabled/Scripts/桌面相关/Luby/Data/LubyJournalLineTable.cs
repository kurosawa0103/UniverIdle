using System;
using System.Collections.Generic;
using UnityEngine;

namespace DesktopPet.Luby
{
    /// <summary>
    /// 近况文案表：运行时读 Resources CSV（由 Excel 导入生成）。
    /// 人读/改：知识库/Luby近况文案.xlsx → 菜单「桌宠 → 导入 Luby 近况文案表」。
    /// </summary>
    public static class LubyJournalLineTable
    {
        public const string ResourcePath = "GameData/Luby/DefaultLubyJournalLines";
        public const string FallbackLine = "做了点什么，自己也说不清。";

        [Serializable]
        public sealed class Row
        {
            public string kind;
            public string personalityId;
            public string traitId;
            public int weight = 1;
            public string line;
        }

        private static List<Row> _rows;
        private static bool _loadAttempted;

        public static void InvalidateCache()
        {
            _rows = null;
            _loadAttempted = false;
        }

        public static string Pick(string kind, LubyInstanceData data, string peerDisplayName)
        {
            EnsureLoaded();
            if (_rows == null || _rows.Count == 0)
                return FallbackLine;

            string personalityId = data != null ? data.personalityId : null;
            string traitId = data != null ? data.traitId : null;
            string traitId2 = data != null ? data.traitId2 : null;

            List<Row> traitHits = null;
            List<Row> personalityHits = null;
            List<Row> genericHits = null;

            for (int i = 0; i < _rows.Count; i++)
            {
                Row row = _rows[i];
                if (row == null || row.kind != kind || string.IsNullOrEmpty(row.line))
                    continue;

                bool hasP = !string.IsNullOrEmpty(row.personalityId);
                bool hasT = !string.IsNullOrEmpty(row.traitId);

                if (hasT)
                {
                    if (!TraitMatches(row.traitId, traitId, traitId2))
                        continue;
                    if (hasP && !string.Equals(row.personalityId, personalityId, StringComparison.Ordinal))
                        continue;
                    traitHits ??= new List<Row>(4);
                    traitHits.Add(row);
                    continue;
                }

                if (hasP)
                {
                    if (!string.Equals(row.personalityId, personalityId, StringComparison.Ordinal))
                        continue;
                    personalityHits ??= new List<Row>(4);
                    personalityHits.Add(row);
                    continue;
                }

                genericHits ??= new List<Row>(8);
                genericHits.Add(row);
            }

            List<Row> pool = traitHits != null && traitHits.Count > 0
                ? traitHits
                : personalityHits != null && personalityHits.Count > 0
                    ? personalityHits
                    : genericHits;

            string line = PickWeighted(pool);
            if (string.IsNullOrEmpty(line))
                line = FallbackLine;

            if (kind == LubyJournalKinds.Greet || line.IndexOf("{0}", StringComparison.Ordinal) >= 0)
            {
                string name = string.IsNullOrEmpty(peerDisplayName) ? "一位伙伴" : peerDisplayName;
                try
                {
                    line = string.Format(line, name);
                }
                catch (FormatException)
                {
                    // 表里写了坏占位符时保底原文
                }
            }

            return line;
        }

        /// <summary>喜好句式：kind = like_decor / like_luby / like_adventure / like_coin / like_header</summary>
        public static string PickLikeFormat(string kind, string label)
        {
            EnsureLoaded();
            string line = PickWeighted(CollectKind(kind));
            if (string.IsNullOrEmpty(line))
            {
                switch (kind)
                {
                    case "like_decor": return "好像特别喜欢" + label;
                    case "like_luby": return "有点黏着" + label;
                    case "like_adventure": return "好像爱上探险了";
                    case "like_coin": return "对金币很上心";
                    case "like_header": return "好像喜欢：";
                    default: return label;
                }
            }

            if (line.IndexOf("{0}", StringComparison.Ordinal) >= 0)
            {
                try
                {
                    return string.Format(line, label ?? string.Empty);
                }
                catch (FormatException)
                {
                    return line;
                }
            }

            return line;
        }

        private static List<Row> CollectKind(string kind)
        {
            var list = new List<Row>(4);
            if (_rows == null)
                return list;
            for (int i = 0; i < _rows.Count; i++)
            {
                Row row = _rows[i];
                if (row != null && row.kind == kind && !string.IsNullOrEmpty(row.line))
                    list.Add(row);
            }

            return list;
        }

        private static bool TraitMatches(string want, string t1, string t2)
        {
            if (string.IsNullOrEmpty(want))
                return true;
            return want == t1 || want == t2;
        }

        private static string PickWeighted(List<Row> pool)
        {
            if (pool == null || pool.Count == 0)
                return null;

            int total = 0;
            for (int i = 0; i < pool.Count; i++)
                total += Mathf.Max(1, pool[i].weight);
            int roll = UnityEngine.Random.Range(0, total);
            for (int i = 0; i < pool.Count; i++)
            {
                roll -= Mathf.Max(1, pool[i].weight);
                if (roll < 0)
                    return pool[i].line;
            }

            return pool[pool.Count - 1].line;
        }

        private static void EnsureLoaded()
        {
            if (_loadAttempted)
                return;
            _loadAttempted = true;
            _rows = new List<Row>(64);

            TextAsset asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null || string.IsNullOrEmpty(asset.text))
            {
                Debug.LogError(
                    $"[LubyJournal] 缺少文案表 Resources/{ResourcePath}.csv。请改 知识库/Luby近况文案.xlsx 后执行「桌宠 → 导入 Luby 近况文案表」。");
                return;
            }

            ParseTsv(asset.text, _rows);
        }

        public static void ParseTsv(string text, List<Row> dst)
        {
            if (dst == null || string.IsNullOrEmpty(text))
                return;

            string[] lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string raw = lines[i];
                if (string.IsNullOrWhiteSpace(raw))
                    continue;
                if (i == 0 && raw.StartsWith("kind", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (raw.StartsWith("#", StringComparison.Ordinal))
                    continue;

                string[] cols = raw.Split('\t');
                if (cols.Length < 5)
                    continue;

                int weight = 1;
                int.TryParse(cols[3].Trim(), out weight);
                if (weight < 1)
                    weight = 1;

                dst.Add(new Row
                {
                    kind = cols[0].Trim(),
                    personalityId = cols[1].Trim(),
                    traitId = cols[2].Trim(),
                    weight = weight,
                    line = cols[4].Trim()
                });
            }
        }
    }
}
