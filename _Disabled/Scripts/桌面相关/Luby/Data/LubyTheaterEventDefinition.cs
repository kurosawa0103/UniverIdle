using System;
using System.Collections.Generic;
using DesktopPet.AI;
using DesktopPet.Shop;
using Sirenix.OdinInspector;
using UnityEngine;

namespace DesktopPet.Luby
{
    /// <summary>走位到站位后的最终朝向；Auto 保留 Locomotion 走位朝向。</summary>
    public enum LubyTheaterStageFacing
    {
        Auto,
        Left,
        Right,
        FaceCenter
    }

    [Serializable]
    public sealed class LubyTheaterRoleSlot
    {
        [HorizontalGroup("头", Width = 0.35f)]
        [LabelText("角色键"), LabelWidth(48)]
        public string roleKey = "lead";

        [HorizontalGroup("头")]
        [LabelText("人数"), LabelWidth(36)]
        [MinValue(1)]
        public int count = 1;

        [HorizontalGroup("头")]
        [LabelText("站位 X"), LabelWidth(48)]
        [Tooltip("有舞台道具：相对道具锚点；无道具（社交场）：相对演员水平中点")]
        public float stageOffsetX;

        [BoxGroup("筛选（空=不限）")]
        [LabelText("性格")]
        [Tooltip("拖 Personality_*.asset；空=不限")]
        [AssetSelector(Paths = "Assets/Resources/GameData/Luby/Personalities")]
        public LubyPersonalityDefinition personality;

        [BoxGroup("筛选（空=不限）")]
        [LabelText("特质")]
        [Tooltip("拖 Trait_*.asset；空=不限")]
        [AssetSelector(Paths = "Assets/Resources/GameData/Luby/Traits")]
        public LubyTraitDefinition trait;

        [BoxGroup("表演")]
        [LabelText("表演行为")]
        [Tooltip("拖行为资产；须在各组 requestOnlyBehaviors 或加权池里能找到")]
        [AssetSelector(Paths = "Assets/Resources/GameData/Luby/AI")]
        [Required]
        public PetBehaviorDefinition performance;

        [BoxGroup("表演")]
        [LabelText("最终朝向")]
        [Tooltip("到站位并开始表演前应用。FaceCenter=朝舞台中心（道具锚点或伙伴中点）。")]
        public LubyTheaterStageFacing stageFacing = LubyTheaterStageFacing.FaceCenter;

        public string ResolvePerformanceBehaviorId() =>
            performance != null && !string.IsNullOrEmpty(performance.behaviorId)
                ? performance.behaviorId
                : null;

        public bool Matches(LubyInstanceComponent luby)
        {
            if (luby == null)
                return false;

            if (personality != null && !string.IsNullOrEmpty(personality.personalityId))
            {
                string pid = luby.Personality != null ? luby.Personality.personalityId : luby.Data?.personalityId;
                if (pid != personality.personalityId)
                    return false;
            }

            if (trait != null && !string.IsNullOrEmpty(trait.traitId))
            {
                if (!luby.HasTrait(trait.traitId))
                    return false;
            }

            return true;
        }
    }

    [Serializable]
    public sealed class LubyTheaterPropRequirement
    {
        [HorizontalGroup("道具")]
        [LabelText("商品"), LabelWidth(40)]
        [Tooltip("拖 ShopItemData/*.asset；桌上比对用其 itemId")]
        [AssetSelector(Paths = "Assets/Resources/GameData/ShopItemData")]
        [Required]
        public ShopItemDefinition item;

        [HorizontalGroup("道具", Width = 90)]
        [LabelText("最少"), LabelWidth(36)]
        [MinValue(1)]
        public int minCount = 1;

        public string ResolveItemId() =>
            item != null && !string.IsNullOrEmpty(item.itemId) ? item.itemId : null;
    }

    /// <summary>二期时间轴预留；一期运行时不读。</summary>
    [Serializable]
    public sealed class LubyTheaterStep
    {
        [HorizontalGroup("步")]
        [LabelText("角色"), LabelWidth(36)]
        public string roleKey;

        [HorizontalGroup("步")]
        [LabelText("行为"), LabelWidth(36)]
        [AssetSelector(Paths = "Assets/Resources/GameData/Luby/AI")]
        public PetBehaviorDefinition behavior;

        [HorizontalGroup("步", Width = 100)]
        [LabelText("等待"), LabelWidth(36)]
        [MinValue(0f)]
        public float waitSeconds;

        public string ResolveBehaviorId() =>
            behavior != null && !string.IsNullOrEmpty(behavior.behaviorId) ? behavior.behaviorId : null;
    }

    [CreateAssetMenu(menuName = "桌宠/Luby/小剧场事件", fileName = "TheaterEvent")]
    public sealed class LubyTheaterEventDefinition : ScriptableObject
    {
        [Title("小剧场事件", "走位 → 各角色 RequestBehavior；steps 一期不跑")]
        [BoxGroup("基础", centerLabel: true)]
        [LabelText("事件 ID")]
        [Required]
        public string eventId;

        [BoxGroup("基础")]
        [HorizontalGroup("基础/扫描")]
        [LabelText("权重"), LabelWidth(40)]
        [MinValue(0f)]
        public float weight = 1f;

        [HorizontalGroup("基础/扫描")]
        [LabelText("命中率"), LabelWidth(48)]
        [PropertyRange(0f, 1f)]
        public float scanChance = 0.35f;

        [BoxGroup("基础")]
        [HorizontalGroup("基础/时间")]
        [LabelText("冷却秒"), LabelWidth(48)]
        [MinValue(0f)]
        public float cooldownSeconds = 20f;

        [HorizontalGroup("基础/时间")]
        [LabelText("表演秒"), LabelWidth(48)]
        [Tooltip("全员到站位并开始表演后计时")]
        [MinValue(1f)]
        public float durationSeconds = 8f;

        [BoxGroup("打断")]
        [LabelText("允许点击打断")]
        [Tooltip("false：点击不拆场（表演 force）；true：点击强制结束剧场。放置/Hub/收回/道具消失等始终强制拆场。")]
        public bool allowPlayerInterrupt = true;

        [BoxGroup("社交意图")]
        [LabelText("需要社交意图")]
        [Tooltip("勾选后：桌上须有人近期抽到 want_social，且演员中至少一人带意图。调试强制开演可忽略。")]
        public bool requiresSocialIntent;

        [BoxGroup("社交意图")]
        [LabelText("先找人再对戏")]
        [Tooltip("无道具时：有意图者先走近对方；到位后对方可按概率走开，留下再进中点对戏。")]
        public bool seekPartnerFirst;

        [BoxGroup("社交意图")]
        [ShowIf(nameof(seekPartnerFirst))]
        [HorizontalGroup("社交意图/拒")]
        [LabelText("走开概率"), LabelWidth(64)]
        [PropertyRange(0f, 1f)]
        public float partnerRejectChance = 0.28f;

        [ShowIf(nameof(seekPartnerFirst))]
        [HorizontalGroup("社交意图/拒")]
        [LabelText("寻访到距"), LabelWidth(64)]
        [MinValue(0.1f)]
        public float seekArriveDistance = 0.5f;

        [ShowIf(nameof(seekPartnerFirst))]
        [HorizontalGroup("社交意图/逃")]
        [LabelText("走开距离"), LabelWidth(64)]
        [MinValue(0.3f)]
        public float rejectFleeDistance = 1.8f;

        [ShowIf(nameof(seekPartnerFirst))]
        [HorizontalGroup("社交意图/逃")]
        [LabelText("走开秒"), LabelWidth(48)]
        [MinValue(0.3f)]
        public float rejectFleeSeconds = 1.4f;

        [BoxGroup("站位", centerLabel: true)]
        [InfoBox("有道具：锚点+站位X。无道具（社交）：演员中点+站位X。", InfoMessageType.None)]
        [LabelText("舞台道具")]
        [Tooltip("空=用 requiredProps 第一项；再空=无道具中点站位")]
        [AssetSelector(Paths = "Assets/Resources/GameData/ShopItemData")]
        public ShopItemDefinition stagePropItem;

        [BoxGroup("站位")]
        [LabelText("最大配对跨度")]
        [Tooltip("演员当前水平跨度（maxX-minX）超过则凑不齐。0=不限制。社交场建议 3～5")]
        [MinValue(0f)]
        public float maxCastSpanX;

        [BoxGroup("站位")]
        [HorizontalGroup("站位/走")]
        [LabelText("速度"), LabelWidth(36)]
        [MinValue(0.1f)]
        public float stageMoveSpeed = 1.2f;

        [HorizontalGroup("站位/走")]
        [LabelText("到点距"), LabelWidth(48)]
        [MinValue(0.05f)]
        public float stageArriveDistance = 0.2f;

        [HorizontalGroup("站位/走")]
        [LabelText("超时秒"), LabelWidth(48)]
        [MinValue(1f)]
        public float stageApproachTimeout = 10f;

        [BoxGroup("演员槽", centerLabel: true)]
        [LabelText("角色列表")]
        [ListDrawerSettings(
            ShowFoldout = true,
            DraggableItems = true,
            ListElementLabelName = "roleKey",
            CustomAddFunction = nameof(CreateDefaultRole))]
        public List<LubyTheaterRoleSlot> roles = new List<LubyTheaterRoleSlot>();

        [BoxGroup("道具要求", centerLabel: true)]
        [LabelText("桌上道具")]
        [ListDrawerSettings(ShowFoldout = true, DraggableItems = true, ListElementLabelName = "item")]
        public List<LubyTheaterPropRequirement> requiredProps = new List<LubyTheaterPropRequirement>();

        [FoldoutGroup("步骤（一期不跑）")]
        [InfoBox("预留时间轴；运行时忽略。", InfoMessageType.Warning)]
        [ListDrawerSettings(ShowFoldout = true, DraggableItems = true, ListElementLabelName = "roleKey")]
        public List<LubyTheaterStep> steps = new List<LubyTheaterStep>();

        private static LubyTheaterRoleSlot CreateDefaultRole()
        {
            return new LubyTheaterRoleSlot
            {
                roleKey = "role",
                count = 1
            };
        }

        public string ResolveStagePropItemId()
        {
            if (stagePropItem != null && !string.IsNullOrEmpty(stagePropItem.itemId))
                return stagePropItem.itemId;
            if (requiredProps != null)
            {
                for (int i = 0; i < requiredProps.Count; i++)
                {
                    string id = requiredProps[i]?.ResolveItemId();
                    if (!string.IsNullOrEmpty(id))
                        return id;
                }
            }

            return null;
        }
    }
}
