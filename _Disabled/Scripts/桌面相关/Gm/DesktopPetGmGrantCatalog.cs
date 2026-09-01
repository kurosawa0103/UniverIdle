using System.Collections.Generic;
using DesktopPet.Luby;
using UnityEngine;

namespace DesktopPet.Gm
{
    /// <summary>从 Catalog/模板收集可指定的外形、性格、特质（GM 用）。</summary>
    public static class DesktopPetGmGrantCatalog
    {
        public static void CollectTemplates(
            LubyTemplateCatalog catalog,
            List<LubyTemplateDefinition> templates,
            List<string> labels)
        {
            templates.Clear();
            labels.Clear();
            if (catalog?.templates == null)
                return;

            for (int i = 0; i < catalog.templates.Count; i++)
            {
                LubyTemplateDefinition t = catalog.templates[i];
                if (t == null)
                    continue;
                templates.Add(t);
                labels.Add(string.IsNullOrEmpty(t.displayName) ? t.templateId : t.displayName);
            }
        }

        public static void CollectAppearances(
            LubyTemplateDefinition template,
            List<GameObject> appearances,
            List<string> labels)
        {
            appearances.Clear();
            labels.Clear();
            if (template == null)
                return;

            if (template.appearancePool == null)
                return;
            for (int i = 0; i < template.appearancePool.Length; i++)
                AddAppearance(appearances, labels, template.appearancePool[i]?.prefab);
        }

        public static void CollectPersonalities(
            LubyTemplateCatalog catalog,
            LubyTemplateDefinition template,
            List<LubyPersonalityDefinition> list,
            List<string> labels,
            bool includeNone)
        {
            list.Clear();
            labels.Clear();
            if (includeNone)
            {
                list.Add(null);
                labels.Add("（无）");
            }

            if (catalog != null)
                AddPersonalityPool(list, labels, catalog.defaultPersonalityPool);
            if (template == null)
                return;
            AddPersonalityPool(list, labels, template.personalityPool);
            if (template.appearancePool == null)
                return;
            for (int i = 0; i < template.appearancePool.Length; i++)
            {
                LubyWeightedAppearanceEntry app = template.appearancePool[i];
                if (app != null && app.bindPersonality)
                    AddPersonalityPool(list, labels, app.personalityPool);
            }
        }

        public static void CollectTraits(
            LubyTemplateCatalog catalog,
            LubyTemplateDefinition template,
            List<LubyTraitDefinition> list,
            List<string> labels,
            bool includeNone)
        {
            list.Clear();
            labels.Clear();
            if (includeNone)
            {
                list.Add(null);
                labels.Add("（无）");
            }

            if (catalog != null)
                AddTraitPool(list, labels, catalog.defaultTraitPool);
            if (template == null)
                return;
            AddTraitPool(list, labels, template.traitPool);
            if (template.appearancePool == null)
                return;
            for (int i = 0; i < template.appearancePool.Length; i++)
            {
                LubyWeightedAppearanceEntry app = template.appearancePool[i];
                if (app != null && app.bindTrait)
                    AddTraitPool(list, labels, app.traitPool);
            }
        }

        private static void AddAppearance(List<GameObject> apps, List<string> names, GameObject prefab)
        {
            if (prefab == null)
                return;
            for (int i = 0; i < apps.Count; i++)
            {
                if (apps[i] == prefab)
                    return;
            }

            apps.Add(prefab);
            names.Add(prefab.name);
        }

        private static void AddPersonalityPool(
            List<LubyPersonalityDefinition> items,
            List<string> names,
            IList<LubyWeightedPersonalityEntry> pool)
        {
            if (pool == null)
                return;
            for (int i = 0; i < pool.Count; i++)
            {
                LubyPersonalityDefinition p = pool[i]?.personality;
                if (p == null || Contains(items, p))
                    continue;
                items.Add(p);
                names.Add(string.IsNullOrEmpty(p.displayName) ? p.personalityId : p.displayName);
            }
        }

        private static void AddTraitPool(
            List<LubyTraitDefinition> items,
            List<string> names,
            IList<LubyWeightedTraitEntry> pool)
        {
            if (pool == null)
                return;
            for (int i = 0; i < pool.Count; i++)
            {
                LubyTraitDefinition t = pool[i]?.trait;
                if (t == null || Contains(items, t))
                    continue;
                items.Add(t);
                names.Add(string.IsNullOrEmpty(t.displayName) ? t.traitId : t.displayName);
            }
        }

        private static bool Contains(List<LubyPersonalityDefinition> items, LubyPersonalityDefinition p)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] == p)
                    return true;
            }

            return false;
        }

        private static bool Contains(List<LubyTraitDefinition> items, LubyTraitDefinition t)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] == t)
                    return true;
            }

            return false;
        }
    }
}
