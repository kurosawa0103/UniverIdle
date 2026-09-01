using UnityEngine;

namespace DesktopPet.Luby
{
    /// <summary>从模板 / 外形 Prefab 取预览 Sprite（领养、仓库、图鉴、信息面板共用）。</summary>
    public static class LubyPrefabIcon
    {
        public static Sprite Resolve(GameObject prefab, Sprite fallback = null)
        {
            if (prefab == null)
                return fallback;
            SpriteRenderer sr = prefab.GetComponentInChildren<SpriteRenderer>(true);
            return sr != null && sr.sprite != null ? sr.sprite : fallback;
        }

        public static Sprite Resolve(LubyTemplateDefinition template, Sprite fallback = null)
        {
            if (template == null)
                return fallback;
            if (template.previewIcon != null)
                return template.previewIcon;
            return Resolve(template.ResolveSpawnPrefab(null), fallback);
        }
    }
}
