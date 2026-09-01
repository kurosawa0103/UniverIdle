using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace DesktopPet.Background
{
    /// <summary>所有可用背景的配置目录。放 Resources/GameData/Background/DefaultBackgroundCatalog。</summary>
    [CreateAssetMenu(fileName = "DefaultBackgroundCatalog", menuName = "桌宠/背景/背景目录")]
    public sealed class BackgroundCatalog : ScriptableObject
    {
        private const string DefaultPath = "GameData/Background/DefaultBackgroundCatalog";

        [Title("背景目录")]
        [LabelText("背景列表")]
        [ListDrawerSettings(ShowFoldout = true, DraggableItems = true)]
        public List<BackgroundDefinition> backgrounds = new();

        public BackgroundDefinition FindById(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            for (int i = 0; i < backgrounds.Count; i++)
            {
                BackgroundDefinition b = backgrounds[i];
                if (b != null && b.backgroundId == id)
                    return b;
            }

            return null;
        }

        public static BackgroundCatalog LoadDefault()
        {
            if (_cached != null)
                return _cached;

            _cached = Resources.Load<BackgroundCatalog>(DefaultPath);
            return _cached;
        }

        private static BackgroundCatalog _cached;

        /// <summary>catalog 中第一个 defaultUnlocked 的背景 ID；无则 transparent。</summary>
        public string ResolveDefaultUnlockedId()
        {
            for (int i = 0; i < backgrounds.Count; i++)
            {
                BackgroundDefinition d = backgrounds[i];
                if (d != null && d.defaultUnlocked)
                    return d.backgroundId;
            }

            return BackgroundDefinition.TransparentId;
        }

        /// <summary>存档 ID 为空时回退到 defaultUnlocked；catalog 缺失则 transparent。</summary>
        public static string ResolveActiveId(string savedId, BackgroundCatalog catalog)
        {
            if (!string.IsNullOrEmpty(savedId))
                return savedId;

            return catalog != null
                ? catalog.ResolveDefaultUnlockedId()
                : BackgroundDefinition.TransparentId;
        }
    }
}
