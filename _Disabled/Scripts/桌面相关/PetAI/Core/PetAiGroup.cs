using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace DesktopPet.AI
{
    /// <summary>
    /// 一组行为模组打包成的 AI 配置，可按角色 / DLC 切换。
    /// </summary>
    [CreateAssetMenu(menuName = "桌宠/AI/行为组", fileName = "PetAiGroup")]
    public sealed class PetAiGroup : ScriptableObject
    {
        [Title("AI 行为组", "把一组行为打包给模板 / Prefab 使用")]
        [InfoBox("建议：组资产与本组行为放在同一夹（如 GameData/Luby/AI/Normal/）。", InfoMessageType.None)]
        [BoxGroup("基础")]
        [LabelText("显示名")]
        public string displayName = "默认 AI";

        [BoxGroup("行为列表")]
        [LabelText("行为")]
        [ListDrawerSettings(ShowFoldout = true, DraggableItems = true, ListElementLabelName = "behaviorId")]
        [AssetSelector(Paths = "Assets/Resources/GameData/Luby/AI")]
        public List<PetBehaviorDefinition> behaviors = new List<PetBehaviorDefinition>();

        [BoxGroup("行为列表")]
        [LabelText("仅 Request 行为")]
        [Tooltip("不进加权随机；RequestBehavior / FindById 仍可找到（如 listen_radio）。")]
        [ListDrawerSettings(ShowFoldout = true, DraggableItems = true, ListElementLabelName = "behaviorId")]
        [AssetSelector(Paths = "Assets/Resources/GameData/Luby/AI")]
        public List<PetBehaviorDefinition> requestOnlyBehaviors = new List<PetBehaviorDefinition>();

        [BoxGroup("行为列表")]
        [LabelText("回退行为")]
        [Tooltip("启动或无候选时使用")]
        [AssetSelector(Paths = "Assets/Resources/GameData/Luby/AI")]
        public PetBehaviorDefinition fallbackBehavior;

        [BoxGroup("选型规则")]
        [LabelText("禁止立刻重复当前行为")]
        [Tooltip("选下一段时排除与当前相同的 behaviorId；无其它候选时会放宽")]
        public bool avoidImmediateRepeat = true;

        public PetBehaviorDefinition FindById(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            PetBehaviorDefinition def = FindInList(behaviors, id);
            if (def != null)
                return def;

            def = FindInList(requestOnlyBehaviors, id);
            if (def != null)
                return def;

            if (fallbackBehavior != null &&
                string.Equals(fallbackBehavior.behaviorId, id, StringComparison.Ordinal))
            {
                return fallbackBehavior;
            }

            return null;
        }

        private static PetBehaviorDefinition FindInList(List<PetBehaviorDefinition> list, string id)
        {
            if (list == null)
                return null;

            for (int i = 0; i < list.Count; i++)
            {
                PetBehaviorDefinition def = list[i];
                if (def != null && string.Equals(def.behaviorId, id, StringComparison.Ordinal))
                    return def;
            }

            return null;
        }
    }
}
