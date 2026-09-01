using UnityEngine;

namespace DesktopPet.Luby
{
    /// <summary>按 <see cref="LubyNameCatalog"/> 组合规则随机生成宠物名。</summary>
    public static class LubyNameGenerator
    {
        public static string Roll(
            LubyNameCatalog catalog,
            LubyPersonalityDefinition personality,
            string appearanceKey)
        {
            if (catalog == null || !catalog.HasUsablePools)
                return string.Empty;

            LubyNamePattern pattern = PickPattern(catalog.patterns);
            switch (pattern)
            {
                case LubyNamePattern.AdjBunny:
                    return PickAdjective(catalog, personality) + PickOne(catalog.bunnySuffixes);
                case LubyNamePattern.Reduplicate:
                {
                    string doubled = PickDoubled(catalog, catalog.reduplicateRoots);
                    if (string.IsNullOrEmpty(doubled))
                        return PickAdjective(catalog, personality) + PickOne(catalog.nouns);
                    if (Random.value < catalog.reduplicateBunnyChance)
                        return doubled + PickOne(catalog.bunnySuffixes);
                    return doubled + PickOne(catalog.nouns);
                }
                case LubyNamePattern.ColorNoun:
                    return PickColorWord(catalog, appearanceKey) + PickOne(catalog.nouns);
                case LubyNamePattern.Preset:
                    return PickOne(catalog.presets);
                default:
                    return PickAdjective(catalog, personality) + PickOne(catalog.nouns);
            }
        }

        private static LubyNamePattern PickPattern(LubyWeightedNamePattern[] patterns)
        {
            if (patterns == null || patterns.Length == 0)
                return LubyNamePattern.AdjNoun;

            int total = 0;
            for (int i = 0; i < patterns.Length; i++)
                total += Mathf.Max(1, patterns[i].weight);

            int roll = Random.Range(0, total);
            for (int i = 0; i < patterns.Length; i++)
            {
                roll -= Mathf.Max(1, patterns[i].weight);
                if (roll < 0)
                    return patterns[i].pattern;
            }

            return patterns[0].pattern;
        }

        private static string PickAdjective(LubyNameCatalog catalog, LubyPersonalityDefinition personality)
        {
            // 70% 性格偏置词，30% 通用形容词（有偏置时）
            if (personality != null
                && !string.IsNullOrEmpty(personality.personalityId)
                && Random.value < catalog.personalityBiasChance)
            {
                string biased = PickPersonalityWord(catalog, personality.personalityId);
                if (!string.IsNullOrEmpty(biased))
                    return biased;
            }

            return PickOne(catalog.adjectives);
        }

        private static string PickPersonalityWord(LubyNameCatalog catalog, string personalityId)
        {
            LubyPersonalityNameBias[] biases = catalog.personalityBiases;
            if (biases == null)
                return null;

            for (int i = 0; i < biases.Length; i++)
            {
                LubyPersonalityNameBias b = biases[i];
                if (b.personalityId == personalityId && b.adjectives != null && b.adjectives.Length > 0)
                    return PickOne(b.adjectives);
            }

            return null;
        }

        private static string PickColorWord(LubyNameCatalog catalog, string appearanceKey)
        {
            if (!string.IsNullOrEmpty(appearanceKey))
            {
                LubyAppearanceNameBias[] biases = catalog.appearanceBiases;
                if (biases != null)
                {
                    for (int i = 0; i < biases.Length; i++)
                    {
                        LubyAppearanceNameBias b = biases[i];
                        if (string.IsNullOrEmpty(b.appearanceKey))
                            continue;
                        if (appearanceKey.StartsWith(b.appearanceKey)
                            && b.colorWords != null
                            && b.colorWords.Length > 0)
                            return PickOne(b.colorWords);
                    }
                }
            }

            return PickOne(catalog.adjectives);
        }

        private static string PickDoubled(LubyNameCatalog catalog, string[] roots)
        {
            string root = PickOne(roots);
            if (string.IsNullOrEmpty(root))
                root = PickOne(catalog.reduplicateRoots);
            if (string.IsNullOrEmpty(root))
                return string.Empty;
            return root + root;
        }

        private static string PickOne(string[] pool)
        {
            if (pool == null || pool.Length == 0)
                return string.Empty;
            return pool[Random.Range(0, pool.Length)];
        }
    }
}
