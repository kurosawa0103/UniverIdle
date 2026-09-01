using System.Collections.Generic;
using DesktopPet.Environment;
using DesktopPet.Hub;
using Sirenix.OdinInspector;
using UnityEngine;

namespace DesktopPet.Background
{
    /// <summary>
    /// 一个可切换背景的配置资产。
    /// 背景解锁：transparent 始终可用；<see cref="defaultUnlocked"/> 或商店购买写入 unlockedBackgrounds。
    /// 桌上容量与升级阶梯直接配在本资产上（装饰 / Luby 各一套）。
    /// </summary>
    [CreateAssetMenu(fileName = "Background_New", menuName = "桌宠/背景/背景定义")]
    public sealed class BackgroundDefinition : ScriptableObject
    {
        [Title("背景定义")]

        [BoxGroup("基础")]
        [LabelText("背景 ID")]
        [Tooltip("稳定唯一 ID，存档用。内置透明背景 ID = transparent。")]
        public string backgroundId = "background";

        [BoxGroup("基础")]
        [LabelText("显示名")]
        public string displayName = "新背景";

        [BoxGroup("基础")]
        [LabelText("描述")]
        [TextArea(2, 3)]
        public string description;

        [BoxGroup("基础")]
        [LabelText("预览图标")]
        [PreviewField(64, ObjectFieldAlignment.Center)]
        public Sprite icon;

        [BoxGroup("场景")]
        [LabelText("背景预制体")]
        [Tooltip("整个背景的预制体（底图 SpriteRenderer + 可选 PlaceSurface 等）。空 = 透明桌面。")]
        [AssetSelector]
        public GameObject backgroundPrefab;

        [BoxGroup("购买")]
        [LabelText("默认解锁")]
        [Tooltip("勾选则新存档即拥有，无需购买。")]
        public bool defaultUnlocked;

        [BoxGroup("购买")]
        [LabelText("价格（金币）")]
        [Tooltip("0 = 免费解锁（点击即得）。默认解锁时此项无效。")]
        [MinValue(0)]
        public int price;

        [Title("桌上容量")]
        [InfoBox("0 级 = 该背景初始栏位；下面每行 = 下一次升级。装饰与 Luby 独立计数。", InfoMessageType.None)]

        [BoxGroup("装饰")]
        [LabelText("0 级栏位")]
        [MinValue(1)]
        public int decorInitialCapacity = 35;

        [BoxGroup("装饰")]
        [LabelText("升级阶梯")]
        [ListDrawerSettings(ShowFoldout = true, DraggableItems = true, ShowIndexLabels = true)]
        public List<DeskCapacityUpgradeTier> decorTiers = new();

        [BoxGroup("装饰")]
        [ShowInInspector, ReadOnly, LabelText("满级栏位")]
        private int DebugDecorMax => GetDecorMaxCapacity();

        [BoxGroup("Luby")]
        [LabelText("0 级栏位")]
        [MinValue(1)]
        public int lubyInitialCapacity = 3;

        [BoxGroup("Luby")]
        [LabelText("升级阶梯")]
        [ListDrawerSettings(ShowFoldout = true, DraggableItems = true, ShowIndexLabels = true)]
        public List<DeskCapacityUpgradeTier> lubyTiers = new();

        [BoxGroup("Luby")]
        [ShowInInspector, ReadOnly, LabelText("满级栏位")]
        private int DebugLubyMax => GetLubyMaxCapacity();

        [Title("可用天气")]
        [InfoBox("仅在本背景激活时，设置页/Gm 可切换或随机到下列天气。透明桌面不受此限制。", InfoMessageType.None)]
        [BoxGroup("天气")]
        [LabelText("可触发天气")]
        [Tooltip("拖入 WeatherDefinition 资产（GameData/天气系统配置/）。")]
        [ListDrawerSettings(ShowFoldout = true, DraggableItems = true)]
        public List<WeatherDefinition> allowedWeathers = new();

        /// <summary>透明桌面默认背景 ID。始终解锁，无需购买。</summary>
        public const string TransparentId = "transparent";

        public int DecorTierCount => decorTiers != null ? decorTiers.Count : 0;
        public int LubyTierCount => lubyTiers != null ? lubyTiers.Count : 0;

        public int GetDecorInitialCapacity() => Mathf.Max(1, decorInitialCapacity);
        public int GetLubyInitialCapacity() => Mathf.Max(1, lubyInitialCapacity);
        public int GetDecorMaxCapacity() => ComputeMax(decorInitialCapacity, decorTiers);
        public int GetLubyMaxCapacity() => ComputeMax(lubyInitialCapacity, lubyTiers);

        public bool TryGetDecorTier(int level, out DeskCapacityUpgradeTier tier)
            => TryGetTier(decorTiers, level, out tier);

        public bool TryGetLubyTier(int level, out DeskCapacityUpgradeTier tier)
            => TryGetTier(lubyTiers, level, out tier);

        private static bool TryGetTier(List<DeskCapacityUpgradeTier> tiers, int level, out DeskCapacityUpgradeTier tier)
        {
            tier = default;
            if (tiers == null || level < 0 || level >= tiers.Count)
                return false;
            tier = tiers[level];
            return tier.goldCost > 0 && tier.slotGain > 0;
        }

        private static int ComputeMax(int initial, List<DeskCapacityUpgradeTier> tiers)
        {
            int cap = Mathf.Max(1, initial);
            if (tiers == null)
                return cap;

            for (int i = 0; i < tiers.Count; i++)
                cap += Mathf.Max(0, tiers[i].slotGain);

            return cap;
        }
    }
}
