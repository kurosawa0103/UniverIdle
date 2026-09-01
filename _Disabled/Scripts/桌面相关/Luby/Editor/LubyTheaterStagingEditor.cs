#if UNITY_EDITOR
using DesktopPet.Decor;
using UnityEngine;

namespace DesktopPet.Luby.Editor
{
    /// <summary>小剧场站位编辑器预览：场景扫场仅在此，不进运行时 Staging。</summary>
    internal static class LubyTheaterStagingEditor
    {
        public static PlacedDecor FindPropInOpenScenes(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
                return null;

            DecorWorld world = DesktopPetServices.DecorWorld;
            if (world == null)
                world = Object.FindObjectOfType<DecorWorld>();

            if (world != null && LubyTheaterStaging.TryFindStageProp(itemId, world, out PlacedDecor placed))
                return placed;

            PlacedDecor[] all = Object.FindObjectsOfType<PlacedDecor>();
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].ItemId == itemId)
                    return all[i];
            }

            return null;
        }
    }
}
#endif
