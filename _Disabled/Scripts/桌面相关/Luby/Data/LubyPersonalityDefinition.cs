using DesktopPet.AI;
using Sirenix.OdinInspector;
using UnityEngine;

namespace DesktopPet.Luby
{
    [CreateAssetMenu(menuName = "桌宠/Luby/性格", fileName = "LubyPersonality")]
    public sealed class LubyPersonalityDefinition : ScriptableObject
    {
        [BoxGroup("基础")]
        [LabelText("性格 ID")]
        public string personalityId = "personality";

        [BoxGroup("基础")]
        [LabelText("显示名")]
        public string displayName = "性格";

        [BoxGroup("基础")]
        [TextArea(2, 4)]
        public string description;

        [BoxGroup("AI")]
        [LabelText("专属 AI 组")]
        [Tooltip("Spawn 时注入 PetBrain；与 Normal/Lively/Shy 等并列，必填")]
        [Required]
        public PetAiGroup aiGroup;
    }
}
