using System.Collections.Generic;
using DesktopPet.Luby;
using DesktopPet.Shop;
using UnityEngine;

namespace DesktopPet.Decor
{
    /// <summary>
    /// 家具落点共用：按 <see cref="DecorFurnitureKind"/> 找最近空闲件、认领、站位 XY。
    /// 床/椅/地板同一套；有叠放子件或已被占则不可用。
    /// </summary>
    public static class DecorFurnitureSpot
    {
        /// <summary>装饰 InstanceId → 占用中的 Luby InstanceId。</summary>
        private static readonly Dictionary<string, string> Claims = new Dictionary<string, string>(8);

        public static bool IsUsable(DecorWorld world, PlacedDecor decor, DecorFurnitureKind kind)
        {
            if (world == null || decor == null || !decor.isActiveAndEnabled)
                return false;
            if (kind == DecorFurnitureKind.None)
                return false;
            ShopItemDefinition def = decor.Definition;
            if (def == null || def.furnitureKind != kind)
                return false;
            if (string.IsNullOrEmpty(decor.InstanceId))
                return false;
            if (Claims.ContainsKey(decor.InstanceId))
                return false;
            if (world.HasStackedChildren(decor))
                return false;
            return true;
        }

        public static float ResolveStandX(PlacedDecor decor)
        {
            float x = decor.transform.position.x;
            ShopItemDefinition def = decor.Definition;
            if (def != null)
                x += def.furnitureStandOffsetX;
            return x;
        }

        public static float ResolveFeetY(PlacedDecor decor)
        {
            float y = decor.transform.position.y;
            ShopItemDefinition def = decor.Definition;
            if (def != null)
                y += def.furnitureStandOffsetY;
            return y;
        }

        /// <summary>认领最近可用家具；失败则 outs 为 null/0。</summary>
        public static bool TryClaimNearest(
            LubyInstanceComponent luby,
            DecorFurnitureKind kind,
            out PlacedDecor best,
            out float standX)
        {
            best = null;
            standX = 0f;
            DecorWorld world = DesktopPetServices.DecorWorld;
            if (world?.Placed == null ||
                luby == null ||
                string.IsNullOrEmpty(luby.InstanceId) ||
                kind == DecorFurnitureKind.None)
                return false;

            float x = luby.transform.position.x;
            float bestDist = float.MaxValue;
            IReadOnlyList<PlacedDecor> placed = world.Placed;
            for (int i = 0; i < placed.Count; i++)
            {
                PlacedDecor d = placed[i];
                if (!IsUsable(world, d, kind))
                    continue;

                float tx = ResolveStandX(d);
                float dist = Mathf.Abs(tx - x);
                if (dist >= bestDist)
                    continue;
                bestDist = dist;
                best = d;
                standX = tx;
            }

            if (best == null)
                return false;

            Claims[best.InstanceId] = luby.InstanceId;
            return true;
        }

        public static void Release(string decorInstanceId, LubyInstanceComponent luby)
        {
            if (string.IsNullOrEmpty(decorInstanceId))
                return;
            string lubyId = luby != null ? luby.InstanceId : null;
            if (Claims.TryGetValue(decorInstanceId, out string holder) &&
                (holder == lubyId || string.IsNullOrEmpty(lubyId)))
            {
                Claims.Remove(decorInstanceId);
            }
        }
    }
}
