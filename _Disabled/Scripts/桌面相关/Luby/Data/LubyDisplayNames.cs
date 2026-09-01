namespace DesktopPet.Luby
{
    /// <summary>实例显示名：优先存档 petName，否则模板名。</summary>
    public static class LubyDisplayNames
    {
        public static string ResolvePetName(LubyInstanceData data, LubyTemplateCatalog catalog = null)
        {
            if (data != null && !string.IsNullOrEmpty(data.petName))
                return data.petName;

            if (catalog != null && data != null && !string.IsNullOrEmpty(data.templateId))
            {
                LubyTemplateDefinition template = catalog.FindTemplateById(data.templateId);
                if (template != null && !string.IsNullOrEmpty(template.displayName))
                    return template.displayName;
            }

            return "Luby";
        }

        public static string ResolvePetName(LubyInstanceComponent inst)
        {
            if (inst == null)
                return "Luby";
            LubyWorld world = DesktopPetServices.LubyWorld;
            LubyTemplateCatalog catalog = world != null ? world.Catalog : null;
            return ResolvePetName(inst.Data, catalog);
        }
    }
}
