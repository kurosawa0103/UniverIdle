using System.Collections.Generic;
using DesktopPet.Luby;
using DesktopPet.AI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DesktopPet.Inventory
{
    /// <summary>Luby 信息面板：展示桌上实例的性格 / 特质 / 外形等（MainCanvas 预制体）。</summary>
    public sealed class LubyInfoPanelController : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Image icon;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI bodyText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button dimButton;

        private LubyInstanceComponent _target;

        private static readonly Dictionary<string, string> BehaviorDisplayNames = new()
        {
            { "normal_stand", "发呆中" },
            { "stand", "站着" },
            { "walk", "散步中" },
            { "sleep", "睡觉中" },
            { "run", "奔跑中" },
            { "horizontal_move", "溜达中" },
            { "listen_radio", "听收音机" },
            { "well_peek", "看水井" },
            { "well_linger", "在水井旁逗留" },
            { "collect_coin", "捡金币" },
            { "want_social", "想找人玩" },
            { "seek_decor", "找装饰互动" },
            { "seek_adventure_board", "准备探险" },
            { "adventure_board_linger", "在看探险看板" },
            { "theater_greet", "打招呼" },
            { "theater_radio_chat", "收音机旁聊天" },
            { "hum_along", "哼小曲" },
            { "radio_far_hum", "远远地哼歌" },
        };

        public bool IsVisible => root != null && root.activeSelf;

        public RectTransform PanelRect =>
            root != null ? root.transform as RectTransform : null;

        private void Awake()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(Hide);
            if (dimButton != null)
                dimButton.onClick.AddListener(Hide);
            Hide();
        }

        private void OnDestroy()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(Hide);
            if (dimButton != null)
                dimButton.onClick.RemoveListener(Hide);
        }

        private void Update()
        {
            if (!IsVisible || _target == null) return;
            if (statusText != null)
                statusText.text = ResolveStatus(_target);
        }

        public void Show(LubyInstanceComponent luby)
        {
            if (root == null)
            {
                Debug.LogError("[LubyInfo] 未绑定 root。请改 MainCanvas.prefab 后「应用主面板」。");
                return;
            }

            if (luby == null || luby.Data == null)
                return;

            _target = luby;
            LubyInstanceData data = luby.Data;
            LubyTemplateDefinition template = luby.Template;
            LubyWorld world = DesktopPetServices.LubyWorld;

            Sprite spr = LubyPrefabIcon.Resolve(template, null);
            if (icon != null)
            {
                icon.sprite = spr;
                icon.enabled = spr != null;
                icon.preserveAspect = true;
            }

            string title = LubyDisplayNames.ResolvePetName(data, world?.Catalog);
            if (nameText != null)
                nameText.text = title;

            string pName = "—";
            string pDesc = string.Empty;
            string tName = "—";
            string tDesc = string.Empty;
            if (world?.Catalog != null)
            {
                LubyPersonalityDefinition p = world.Catalog.FindPersonalityById(data.personalityId);
                if (p != null)
                {
                    pName = p.displayName;
                    pDesc = p.description;
                }

                tName = LubyTraitDisplay.FormatNames(world.Catalog, data);
                tDesc = LubyTraitDisplay.FormatDescriptions(world.Catalog, data);
            }

            if (bodyText != null)
            {
                LubyJournalService.MaybeIdleThought(data);
                string journal = LubyJournalService.FormatSummary(data, world?.Catalog);
                bodyText.text =
                    $"<b>性格</b>　{pName}\n" +
                    $"{(string.IsNullOrEmpty(pDesc) ? "" : $"<color=#AAAAAA><size=85%>{pDesc}</size></color>\n")}" +
                    $"<b>特质</b>　{tName}\n" +
                    $"{(string.IsNullOrEmpty(tDesc) ? "" : $"<color=#AAAAAA><size=85%>{tDesc}</size></color>")}" +
                    $"{(string.IsNullOrEmpty(journal) ? "" : $"\n\n{journal}")}";
            }

            if (statusText != null)
                statusText.text = ResolveStatus(luby);

            root.SetActive(true);
            transform.SetAsLastSibling();
        }

        public void Hide()
        {
            _target = null;
            if (root != null)
                root.SetActive(false);
        }

        private static string ResolveStatus(LubyInstanceComponent luby)
        {
            if (luby == null || luby.Agent == null || luby.Agent.Brain == null)
                return "—";

            string id = luby.Agent.Brain.CurrentBehaviorId;
            if (string.IsNullOrEmpty(id))
                return "闲着";

            if (BehaviorDisplayNames.TryGetValue(id, out string display))
                return display;

            return id;
        }
    }
}
