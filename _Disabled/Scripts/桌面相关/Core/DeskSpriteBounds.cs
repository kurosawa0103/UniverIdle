using System.Collections.Generic;
using UnityEngine;

namespace DesktopPet
{
    /// <summary>桌面实体精灵盒：量本地包围、头顶跟随缓存。不另开 HeadAnchor 类型。</summary>
    public static class DeskSpriteBounds
    {
        private struct HeadEntry
        {
            public Transform Target;
            public Vector3 LocalTop;
            public Vector3 CachedScale;
        }

        private static readonly Dictionary<int, HeadEntry> HeadCache = new Dictionary<int, HeadEntry>(16);

        public static bool TryMeasureLocalBox(
            Transform root,
            out Vector3 localCenter,
            out Vector3 localSize)
        {
            localCenter = Vector3.zero;
            localSize = Vector3.one;

            if (root == null)
                return false;

            SpriteRenderer[] srs = root.GetComponentsInChildren<SpriteRenderer>(true);
            if (srs != null && srs.Length > 0)
            {
                Bounds world = srs[0].bounds;
                for (int i = 1; i < srs.Length; i++)
                {
                    if (srs[i] != null)
                        world.Encapsulate(srs[i].bounds);
                }

                return WorldBoxToLocal(root, world, out localCenter, out localSize);
            }

            Collider2D col = root.GetComponentInChildren<Collider2D>();
            if (col != null)
                return WorldBoxToLocal(root, col.bounds, out localCenter, out localSize);

            return false;
        }

        public static void InvalidateHead(Transform target)
        {
            if (target == null)
                return;
            HeadCache.Remove(target.GetInstanceID());
        }

        public static Vector3 ResolveHeadWorld(Transform target, float padding)
        {
            if (target == null)
                return Vector3.zero;

            int id = target.GetInstanceID();
            Vector3 scale = target.localScale;
            if (!HeadCache.TryGetValue(id, out HeadEntry e) || e.Target != target || e.CachedScale != scale)
            {
                e.Target = target;
                e.CachedScale = scale;
                if (TryMeasureLocalBox(target, out Vector3 localCenter, out Vector3 localSize))
                    e.LocalTop = localCenter + Vector3.up * (localSize.y * 0.5f);
                else
                    e.LocalTop = Vector3.up * 1.2f;
                HeadCache[id] = e;
            }

            Vector3 worldTop = target.TransformPoint(e.LocalTop);
            return new Vector3(worldTop.x, worldTop.y + padding, target.position.z);
        }

        private static bool WorldBoxToLocal(
            Transform root,
            Bounds world,
            out Vector3 localCenter,
            out Vector3 localSize)
        {
            Vector3 lossy = root.lossyScale;
            localCenter = root.InverseTransformPoint(world.center);
            localSize = new Vector3(
                world.size.x / Mathf.Max(Mathf.Abs(lossy.x), 1e-4f),
                world.size.y / Mathf.Max(Mathf.Abs(lossy.y), 1e-4f),
                world.size.z / Mathf.Max(Mathf.Abs(lossy.z), 1e-4f));
            return true;
        }
    }
}
