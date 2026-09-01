using System.Collections.Generic;
using DesktopPet.Decor;
using UnityEngine;

namespace DesktopPet.Luby
{
    /// <summary>小剧场站位：道具锚点或无道具时的伙伴中点（编辑器预览与运行时共用）。</summary>
    public static class LubyTheaterStaging
    {
        /// <summary>同一角色槽多人时的水平间距。</summary>
        public const float SameRoleSpacingX = 0.35f;

        public static float ResolveRoleOffsetX(float baseOffsetX, int slotIndex, int slotCount)
        {
            if (slotCount > 1)
                return baseOffsetX + slotIndex * SameRoleSpacingX;
            return baseOffsetX;
        }

        public static bool TryFindStageProp(string itemId, DecorWorld decor, out PlacedDecor placed)
        {
            placed = null;
            if (string.IsNullOrEmpty(itemId) || decor == null)
                return false;

            var list = decor.Placed;
            for (int i = 0; i < list.Count; i++)
            {
                PlacedDecor d = list[i];
                if (d != null && d.ItemId == itemId)
                {
                    placed = d;
                    return true;
                }
            }

            return false;
        }

        public static Vector3 GetPropAnchorWorld(PlacedDecor placed)
        {
            if (placed == null)
                return Vector3.zero;

            DecorInteractable interactable = placed.GetComponent<DecorInteractable>();
            if (interactable != null)
            {
                Vector2 a = interactable.AnchorWorld;
                return new Vector3(a.x, a.y, placed.transform.position.z);
            }

            return placed.transform.position;
        }

        public static float GetStageWorldX(PlacedDecor placed, float offsetX)
        {
            return GetPropAnchorWorld(placed).x + offsetX;
        }

        public static float GetPeerStageWorldX(float midpointX, float offsetX) => midpointX + offsetX;

        /// <summary>社交寻访：有意图者站到目标旁边的世界 X。</summary>
        public static float ComputeSeekStandBesideX(float seekerX, float targetX, float seekArriveDistance)
        {
            float seekArrive = Mathf.Max(0.1f, seekArriveDistance);
            float side = seekerX <= targetX ? -1f : 1f;
            return targetX + side * seekArrive * 0.85f;
        }

        /// <summary>调试/预览用朝向缩写。</summary>
        public static string FormatStageFacingShort(LubyTheaterStageFacing facing)
        {
            switch (facing)
            {
                case LubyTheaterStageFacing.Left:
                    return "←";
                case LubyTheaterStageFacing.Right:
                    return "→";
                case LubyTheaterStageFacing.FaceCenter:
                    return "◎";
                default:
                    return "Auto";
            }
        }

        /// <summary>解析角色槽配置的最终朝向符号（+1 右 / -1 左）；Auto 返回 0 表示不覆盖。</summary>
        public static float ResolveStageFacingSign(
            LubyTheaterStageFacing facing,
            float lubyWorldX,
            float referenceCenterX)
        {
            switch (facing)
            {
                case LubyTheaterStageFacing.Left:
                    return -1f;
                case LubyTheaterStageFacing.Right:
                    return 1f;
                case LubyTheaterStageFacing.FaceCenter:
                    if (Mathf.Abs(lubyWorldX - referenceCenterX) < 0.02f)
                        return 0f;
                    return lubyWorldX < referenceCenterX ? 1f : -1f;
                default:
                    return 0f;
            }
        }

        /// <summary>调试/场次摘要用短名：去掉 Luby_ 前缀，最长 16。</summary>
        public static string ShortLubyName(LubyInstanceComponent luby)
        {
            return ShortLubyName(luby != null ? luby.gameObject.name : null);
        }

        public static string ShortLubyName(string goName)
        {
            if (string.IsNullOrEmpty(goName))
                return "?";
            if (goName.StartsWith("Luby_"))
                goName = goName.Substring(5);
            return goName.Length > 16 ? goName.Substring(0, 16) : goName;
        }

        /// <summary>演员水平跨度是否 ≤ maxSpan（≤0 表示不限制）。</summary>
        public static bool IsCastSpanOk(IReadOnlyList<LubyInstanceComponent> castLubies, float maxSpan)
        {
            if (maxSpan <= 0f || castLubies == null || castLubies.Count <= 1)
                return true;

            float min = float.MaxValue;
            float max = float.MinValue;
            int n = 0;
            for (int i = 0; i < castLubies.Count; i++)
            {
                LubyInstanceComponent l = castLubies[i];
                if (l == null)
                    continue;
                float x = l.transform.position.x;
                if (x < min)
                    min = x;
                if (x > max)
                    max = x;
                n++;
            }

            return n < 2 || (max - min) <= maxSpan;
        }

        public static bool IsStagePropAlive(PlacedDecor prop)
        {
            return prop != null && prop.isActiveAndEnabled;
        }
    }
}
