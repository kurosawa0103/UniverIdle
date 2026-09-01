using Sirenix.OdinInspector;
using UnityEngine;

namespace DesktopPet.Luby
{
    [CreateAssetMenu(menuName = "桌宠/Luby/宠物模板", fileName = "LubyTemplate")]
    public sealed class LubyTemplateDefinition : ScriptableObject
    {
        [BoxGroup("基础")]
        [LabelText("模板 ID")]
        public string templateId = "luby_basic";

        [BoxGroup("基础")]
        [LabelText("显示名")]
        public string displayName = "Luby";

        [BoxGroup("基础")]
        [TextArea(2, 4)]
        public string description;

        [BoxGroup("基础")]
        [LabelText("面板预览图（空则用 Prefab 精灵）")]
        public Sprite previewIcon;

        [BoxGroup("抽取")]
        [LabelText("抽取价格（金币）")]
        [Min(0)]
        public int rollPrice = 30;

        [BoxGroup("生成")]
        [LabelText("外形池（按权重）")]
        [Tooltip("盲盒从此池按权重抽外形；条目可勾选绑定专属性格/特质池。必填。")]
        [ListDrawerSettings(ShowFoldout = true, ListElementLabelName = "prefab")]
        [Required]
        public LubyWeightedAppearanceEntry[] appearancePool;

        [BoxGroup("生成")]
        [LabelText("缩放回退")]
        [Tooltip("外形 Prefab 缩放无效时用。")]
        [Min(0.01f)]
        public float scale = 1f;

        [BoxGroup("默认随机池")]
        [LabelText("性格池（按权重；空=Catalog 全局）")]
        [Tooltip("外形未勾选「绑定性格」时用此池。")]
        public LubyWeightedPersonalityEntry[] personalityPool;

        [BoxGroup("默认随机池")]
        [LabelText("特质池（按权重；空=Catalog 全局）")]
        [Tooltip("外形未勾选「绑定特质」时用此池。")]
        public LubyWeightedTraitEntry[] traitPool;

        [BoxGroup("默认随机池")]
        [LabelText("双特质概率")]
        [Tooltip("抽到第一特质后，再以该概率抽第二个不同特质（同池排除）。0=永不双特质。")]
        [PropertyRange(0f, 1f)]
        public float dualTraitChance;

        public GameObject ResolveSpawnPrefab(string appearanceKey)
        {
            if (appearancePool == null || appearancePool.Length == 0)
                return null;

            if (!string.IsNullOrEmpty(appearanceKey))
            {
                for (int i = 0; i < appearancePool.Length; i++)
                {
                    LubyWeightedAppearanceEntry e = appearancePool[i];
                    if (e?.prefab != null && e.prefab.name == appearanceKey)
                        return e.prefab;
                }

                return null;
            }

            for (int i = 0; i < appearancePool.Length; i++)
            {
                if (appearancePool[i]?.prefab != null)
                    return appearancePool[i].prefab;
            }

            return null;
        }

        /// <summary>Prefab 缩放优先，否则模板 scale，再否则 1。</summary>
        public float ResolveScale(GameObject appearancePrefab)
        {
            if (appearancePrefab != null)
            {
                float ps = Mathf.Abs(appearancePrefab.transform.localScale.x);
                if (ps > 0.01f)
                    return ps;
            }

            return scale > 0.01f ? scale : 1f;
        }
    }
}
