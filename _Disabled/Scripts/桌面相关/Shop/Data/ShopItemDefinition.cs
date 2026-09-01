using System;
using System.Collections.Generic;
using DesktopPet.AI;
using DesktopPet.Decor;
using DesktopPet.Luby;
using Sirenix.OdinInspector;
using UnityEngine;

namespace DesktopPet.Shop
{
    /// <summary>装饰门闸一条：选性格资产；可选专属表演资产（空=用默认表演）。</summary>
    [Serializable]
    public sealed class LubyInteractPersonalityEntry
    {
        [LabelText("性格")]
        [Tooltip("拖 Personality_*.asset；门闸用其 personalityId")]
        [AssetSelector(Paths = "Assets/Resources/GameData/Luby/Personalities")]
        [Required]
        public LubyPersonalityDefinition personality;

        [LabelText("专属表演")]
        [Tooltip("空 = 用商品默认表演；须在各组 requestOnlyBehaviors 里能找到")]
        [AssetSelector(Paths = "Assets/Resources/GameData/Luby/AI")]
        public PetBehaviorDefinition performance;
    }

    [CreateAssetMenu(menuName = "桌宠/商店/商品", fileName = "ShopItem")]
    public sealed class ShopItemDefinition : ScriptableObject
    {
        [Title("商店商品", "基础信息 + 装饰摆放规则")]
        [BoxGroup("基础", centerLabel: true)]
        [LabelText("商品 ID")]
        [Tooltip("稳定 ID，仓库与存档用，不要随便改。")]
        [Required]
        public string itemId = "item";

        [BoxGroup("基础")]
        [LabelText("显示名")]
        public string displayName = "新商品";

        [BoxGroup("基础")]
        [LabelText("描述")]
        [TextArea(2, 4)]
        public string description;

        [BoxGroup("基础")]
        [HorizontalGroup("基础/图标行", Width = 72)]
        [PreviewField(64, ObjectFieldAlignment.Center)]
        [HideLabel]
        public Sprite icon;

        [BoxGroup("基础")]
        [VerticalGroup("基础/图标行/右")]
        [LabelText("价格")]
        [MinValue(0)]
        public int price = 10;

        [VerticalGroup("基础/图标行/右")]
        [LabelText("商店页签")]
        public ShopTabId tab = ShopTabId.Decor;

        [VerticalGroup("基础/图标行/右")]
        [LabelText("持有上限")]
        [Tooltip("仓库数量 + 已摆数量合计上限；0 = 不限。")]
        [MinValue(0)]
        public int maxOwnCount;

        [Title("装饰摆放", "仅装饰页签需要填")]
        [BoxGroup("摆放", centerLabel: true)]
        [LabelText("摆放预制体")]
        [Tooltip("世界摆放用（需 SpriteRenderer + Collider2D）。必填；空则无法放置。")]
        [AssetsOnly]
        public GameObject placementPrefab;

        [BoxGroup("摆放")]
        [LabelText("摆放高度")]
        [Tooltip("层高校验用。上架时与 PlaceSurface「本层最大摆放高度」比较；≤0 则用预制体脚印高度。")]
        [MinValue(0f)]
        [SuffixLabel("世界单位，0=自动量脚印", true)]
        public float placeHeight;

        [BoxGroup("摆放")]
        [LabelText("允许放到可摆放面上")]
        [Tooltip("勾选后可贴地，也可吸附到 DecorPlaceSurface（架子层/底座顶面）。")]
        public bool canStackOnOthers;

        [BoxGroup("摆放")]
        [ShowIf("@tab == ShopTabId.Decor")]
        [LabelText("家具用途")]
        [Tooltip("None=普通摆设；Bed=睡觉优先；Chair=坐下用；Floor=地毯等趴卧点。")]
        public DecorFurnitureKind furnitureKind = DecorFurnitureKind.None;

        [BoxGroup("摆放")]
        [ShowIf("@tab == ShopTabId.Decor && furnitureKind != DecorFurnitureKind.None")]
        [LabelText("站位 X 偏移")]
        [Tooltip("相对装饰 pivot 的水平偏移（睡觉/坐下等走近落点）。")]
        [SuffixLabel("世界单位", true)]
        public float furnitureStandOffsetX;

        [BoxGroup("摆放")]
        [ShowIf("@tab == ShopTabId.Decor && furnitureKind != DecorFurnitureKind.None")]
        [LabelText("站位 Y 偏移")]
        [Tooltip("相对装饰 pivot：脚底落在床面/椅面的高度。走近时仍贴地，到位再抬高。")]
        [SuffixLabel("世界单位", true)]
        public float furnitureStandOffsetY;

        [Title("被动产币", "只要配了这里的区间，并且摆在桌上，就会掉金币")]
        [BoxGroup("产币", centerLabel: true)]

        [BoxGroup("产币")]
        [LabelText("最短产币间隔")]
        [MinValue(1f)]
        [SuffixLabel("秒", true)]
        public float passiveGoldMinIntervalSeconds = 45f;

        [BoxGroup("产币")]
        [LabelText("最长产币间隔")]
        [MinValue(1f)]
        [SuffixLabel("秒", true)]
        public float passiveGoldMaxIntervalSeconds = 0f;

        [BoxGroup("产币")]
        [LabelText("最少金币价值")]
        [MinValue(1)]
        public int passiveGoldMinAmount = 1;

        [BoxGroup("产币")]
        [LabelText("最多金币价值")]
        [MinValue(1)]
        public int passiveGoldMaxAmount = 3;

        [Title("Luby 交互", "配在装饰商品 SO；Prefab 只留锚点/半径/槽位")]
        [BoxGroup("Luby交互", centerLabel: true)]
        [ShowIf("@tab == ShopTabId.Decor")]
        [LabelText("默认表演")]
        [Tooltip("允许性格未挂专属表演时用；Anyone 时全体用此表演")]
        [AssetSelector(Paths = "Assets/Resources/GameData/Luby/AI")]
        public PetBehaviorDefinition lubyPerformanceBehavior;

        [BoxGroup("Luby交互")]
        [ShowIf("@tab == ShopTabId.Decor")]
        [LabelText("任何人可玩")]
        [Tooltip("勾选后全体性格可玩（如收音机），都用默认表演。与白名单同时配时以「任何人」为准。")]
        public bool lubyInteractAllowAnyone;

        [BoxGroup("Luby交互")]
        [ShowIf("@tab == ShopTabId.Decor && !lubyInteractAllowAnyone")]
        [LabelText("允许性格")]
        [Tooltip("须命中其一（OR）；每条选性格，可选专属表演（空=默认）。与特质名单同时非空时两边都要过（AND）")]
        [ListDrawerSettings(ShowFoldout = true, DraggableItems = true, ListElementLabelName = "personality")]
        public List<LubyInteractPersonalityEntry> lubyInteractPersonalities =
            new List<LubyInteractPersonalityEntry>();

        [BoxGroup("Luby交互")]
        [ShowIf("@tab == ShopTabId.Decor && !lubyInteractAllowAnyone")]
        [LabelText("允许特质")]
        [Tooltip("非空则须命中其一；空=不限特质")]
        [ListDrawerSettings(ShowFoldout = true, DraggableItems = true)]
        [AssetSelector(Paths = "Assets/Resources/GameData/Luby/Traits")]
        public List<LubyTraitDefinition> lubyInteractTraits = new List<LubyTraitDefinition>();

        /// <summary>是否开启 Luby 装饰交互（配了门闸：Anyone 或性格/特质白名单）。</summary>
        public bool HasLubyInteractGate =>
            lubyInteractAllowAnyone ||
            (lubyInteractPersonalities != null && lubyInteractPersonalities.Count > 0) ||
            (lubyInteractTraits != null && lubyInteractTraits.Count > 0);

        /// <summary>按性格解析表演 id：该性格条挂了专属表演则用，否则默认；皆空返回 null。</summary>
        public string ResolveLubyPerformanceBehaviorId(string personalityId)
        {
            if (lubyInteractPersonalities != null && !string.IsNullOrEmpty(personalityId))
            {
                for (int i = 0; i < lubyInteractPersonalities.Count; i++)
                {
                    LubyInteractPersonalityEntry e = lubyInteractPersonalities[i];
                    if (e?.personality == null)
                        continue;
                    if (!string.Equals(e.personality.personalityId, personalityId, StringComparison.Ordinal))
                        continue;
                    if (e.performance != null && !string.IsNullOrEmpty(e.performance.behaviorId))
                        return e.performance.behaviorId;
                    break;
                }
            }

            if (lubyPerformanceBehavior != null &&
                !string.IsNullOrEmpty(lubyPerformanceBehavior.behaviorId))
                return lubyPerformanceBehavior.behaviorId;
            return null;
        }

        /// <summary>当前 Luby 是否通过本商品的性格/特质门闸。</summary>
        public bool MatchesLubyInteractGate(LubyInstanceComponent luby)
        {
            if (luby == null)
                return false;
            if (lubyInteractAllowAnyone)
                return true;

            bool hasPersonality = lubyInteractPersonalities != null &&
                                  lubyInteractPersonalities.Count > 0;
            bool hasTrait = lubyInteractTraits != null && lubyInteractTraits.Count > 0;
            if (!hasPersonality && !hasTrait)
                return false;

            if (hasPersonality)
            {
                string pid = luby.ResolvePersonalityId();
                if (string.IsNullOrEmpty(pid))
                    return false;
                bool hit = false;
                for (int i = 0; i < lubyInteractPersonalities.Count; i++)
                {
                    LubyInteractPersonalityEntry e = lubyInteractPersonalities[i];
                    if (e?.personality != null && e.personality.personalityId == pid)
                    {
                        hit = true;
                        break;
                    }
                }

                if (!hit)
                    return false;
            }

            if (hasTrait)
            {
                bool hit = false;
                for (int i = 0; i < lubyInteractTraits.Count; i++)
                {
                    LubyTraitDefinition t = lubyInteractTraits[i];
                    if (t != null && luby.HasTrait(t.traitId))
                    {
                        hit = true;
                        break;
                    }
                }

                if (!hit)
                    return false;
            }

            return true;
        }

        [BoxGroup("摆放")]
        [ShowInInspector, ReadOnly, LabelText("摆放说明")]
        private string StackHint
        {
            get
            {
                string height = placeHeight > 0f
                    ? $"高度 {placeHeight:0.##}"
                    : "高度自动（脚印）";
                if (canStackOnOthers)
                    return $"可贴地 / 上架；{height}。底座请在 Prefab 挂 DecorPlaceSurface。";
                return $"只能贴地；{height}。";
            }
        }

        /// <summary>层高用的物品高度：配置优先，否则回退 fallback。</summary>
        public float ResolvePlaceHeight(float fallback)
        {
            return placeHeight > 0f ? placeHeight : Mathf.Max(0f, fallback);
        }

        public bool HasPassiveGold =>
            passiveGoldMaxIntervalSeconds >= passiveGoldMinIntervalSeconds;

        // NOTE：这里的 Amount 是“这一枚掉落金币的价值”，不是生成多个金币的个数。
        public float RollPassiveGoldInterval() =>
            UnityEngine.Random.Range(passiveGoldMinIntervalSeconds, passiveGoldMaxIntervalSeconds);

        public int RollPassiveGoldAmount() =>
            UnityEngine.Random.Range(passiveGoldMinAmount, passiveGoldMaxAmount + 1);
    }
}
