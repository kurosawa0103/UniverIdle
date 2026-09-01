using System.Text;

namespace DesktopPet.Luby
{
    /// <summary>双特质显示文案（详情 / 抽取结果 / 调试）。</summary>
    public static class LubyTraitDisplay
    {
        public static string FormatNames(
            LubyTraitDefinition first,
            LubyTraitDefinition second,
            string empty = "—")
        {
            string a = NameOf(first);
            string b = NameOf(second);
            if (string.IsNullOrEmpty(a) && string.IsNullOrEmpty(b))
                return empty;
            if (string.IsNullOrEmpty(a))
                return b;
            if (string.IsNullOrEmpty(b))
                return a;
            return a + " · " + b;
        }

        public static string FormatNames(
            LubyTemplateCatalog catalog,
            LubyInstanceData data,
            string empty = "—")
        {
            if (catalog == null || data == null)
                return empty;
            return FormatNames(
                catalog.FindTraitById(data.traitId),
                catalog.FindTraitById(data.traitId2),
                empty);
        }

        public static string FormatDescriptions(
            LubyTraitDefinition first,
            LubyTraitDefinition second)
        {
            var sb = new StringBuilder();
            AppendDesc(sb, first);
            AppendDesc(sb, second);
            return sb.ToString();
        }

        public static string FormatDescriptions(
            LubyTemplateCatalog catalog,
            LubyInstanceData data)
        {
            if (catalog == null || data == null)
                return string.Empty;
            return FormatDescriptions(
                catalog.FindTraitById(data.traitId),
                catalog.FindTraitById(data.traitId2));
        }

        public static string FormatIds(string traitId, string traitId2, string empty = "—")
        {
            bool a = !string.IsNullOrEmpty(traitId);
            bool b = !string.IsNullOrEmpty(traitId2);
            if (!a && !b)
                return empty;
            if (!a)
                return traitId2;
            if (!b)
                return traitId;
            return traitId + " · " + traitId2;
        }

        public static string FormatIds(LubyInstanceData data, string empty = "—")
        {
            if (data == null)
                return empty;
            return FormatIds(data.traitId, data.traitId2, empty);
        }

        private static string NameOf(LubyTraitDefinition t)
        {
            if (t == null)
                return null;
            return string.IsNullOrEmpty(t.displayName) ? t.traitId : t.displayName;
        }

        private static void AppendDesc(StringBuilder sb, LubyTraitDefinition t)
        {
            if (t == null || string.IsNullOrEmpty(t.description))
                return;
            if (sb.Length > 0)
                sb.Append('\n');
            sb.Append(t.description);
        }
    }
}
