using UnityEngine;

namespace DesktopPet
{
    /// <summary>桌宠专用 Layer（TagManager：6=Luby，7=Decor，8=Gold）。</summary>
    public static class DesktopPetLayers
    {
        private const string LubyLayerName = "Luby";
        private const string DecorLayerName = "Decor";
        private const string GoldLayerName = "Gold";

        private const int LubyLayerIndex = 6;
        private const int DecorLayerIndex = 7;
        private const int GoldLayerIndex = 8;

        public static int Luby => Resolve(LubyLayerName, LubyLayerIndex);
        public static int Decor => Resolve(DecorLayerName, DecorLayerIndex);
        public static int Gold => Resolve(GoldLayerName, GoldLayerIndex);

        public static void ApplyLuby(GameObject root) => ApplyRecursively(root, Luby);
        public static void ApplyDecor(GameObject root) => ApplyRecursively(root, Decor);

        private static void ApplyRecursively(GameObject root, int layer)
        {
            if (root == null || layer < 0 || layer > 31)
                return;

            root.layer = layer;
            Transform t = root.transform;
            for (int i = 0; i < t.childCount; i++)
                ApplyRecursively(t.GetChild(i).gameObject, layer);
        }

        private static bool _goldFilterApplied;

        /// <summary>金币只碰 Default 地面，不与 Luby/装饰刚体互撞（一次 IgnoreLayerCollision，不逐物体扫）。</summary>
        public static void EnsureGoldIgnoresActors()
        {
            if (_goldFilterApplied)
                return;

            int gold = Gold;
            Physics2D.IgnoreLayerCollision(gold, Luby, true);
            Physics2D.IgnoreLayerCollision(gold, Decor, true);
            _goldFilterApplied = true;
        }

        private static int Resolve(string layerName, int fallbackIndex)
        {
            int id = LayerMask.NameToLayer(layerName);
            return id >= 0 ? id : fallbackIndex;
        }
    }
}
