using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DesktopPet.Luby
{
    /// <summary>图鉴页：网格列出全部外表；已解锁显示，未解锁灰影。</summary>
    public sealed class CodexUIController : MonoBehaviour
    {
        [Title("图鉴 UI")]
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private Transform gridContent;
        [SerializeField] private CodexAppearanceSlot slotPrefab;
        [SerializeField] private Sprite fallbackIcon;

        [FoldoutGroup("详情")]
        [SerializeField] private Image detailIcon;
        [SerializeField] private TextMeshProUGUI detailNameText;
        [SerializeField] private TextMeshProUGUI detailDescText;

        private readonly List<CodexAppearanceSlot> _slots = new List<CodexAppearanceSlot>(32);
        private readonly List<LubyAppearanceCodexEntry> _entries = new List<LubyAppearanceCodexEntry>(32);
        private LubyAppearanceCodex _codex;
        private LubyTemplateCatalog _catalog;
        private int _selected = -1;
        private bool _codexSubscribed;

        private void Awake()
        {
            DesktopPetServices.RegisterCodexUi(this);
            if (gridContent == null)
                Debug.LogError("[CodexUI] 未绑定 gridContent。请「应用主面板预制体」。");
            if (slotPrefab == null)
                Debug.LogError("[CodexUI] 未绑定 slotPrefab。");
            EnsureRefs(subscribe: true);
        }

        private void OnDestroy()
        {
            DesktopPetServices.UnregisterCodexUi(this);
            if (_codex != null && _codexSubscribed)
                _codex.Changed -= OnCodexChanged;
        }

        public void OnPageShown()
        {
            EnsureRefs(subscribe: true);
            Rebuild();
        }

        private void EnsureRefs(bool subscribe)
        {
            if (_codex == null)
                _codex = GetComponent<LubyAppearanceCodex>() ?? DesktopPetServices.AppearanceCodex;
            if (subscribe && _codex != null && !_codexSubscribed)
            {
                _codex.Changed += OnCodexChanged;
                _codexSubscribed = true;
            }

            if (_catalog == null)
            {
                LubyWorld world = GetComponent<LubyWorld>() ?? DesktopPetServices.LubyWorld;
                _catalog = world != null ? world.Catalog : LubyTemplateCatalog.LoadDefault();
            }
        }

        private void OnCodexChanged()
        {
            if (!isActiveAndEnabled)
                return;
            Rebuild();
        }

        private void Rebuild()
        {
            ClearSlots();
            _entries.Clear();
            _selected = -1;

            if (_catalog != null)
                _entries.AddRange(_catalog.CollectUniqueAppearances());

            int unlocked = 0;
            for (int i = 0; i < _entries.Count; i++)
            {
                LubyAppearanceCodexEntry e = _entries[i];
                bool on = _codex != null && _codex.IsUnlocked(e.appearanceKey);
                if (on)
                    unlocked++;

                if (slotPrefab == null || gridContent == null)
                    continue;

                CodexAppearanceSlot slot = Instantiate(slotPrefab, gridContent);
                Sprite icon = LubyPrefabIcon.Resolve(e.prefab, fallbackIcon);
                slot.Bind(e, on, icon, fallbackIcon);
                int index = i;
                slot.Clicked += _ => Select(index);
                _slots.Add(slot);
            }

            if (statusText != null)
                statusText.text = $"已解锁 {unlocked}/{_entries.Count}";

            if (_entries.Count > 0)
                Select(0);
            else
                ClearDetail();
        }

        private void Select(int index)
        {
            if (index < 0 || index >= _entries.Count)
            {
                ClearDetail();
                return;
            }

            _selected = index;
            for (int i = 0; i < _slots.Count; i++)
                _slots[i].SetSelected(i == index);

            LubyAppearanceCodexEntry e = _entries[index];
            bool on = _codex != null && _codex.IsUnlocked(e.appearanceKey);

            if (detailNameText != null)
                detailNameText.text = on ? e.DisplayName : "???";

            if (detailDescText != null)
            {
                detailDescText.text = on
                    ? (string.IsNullOrEmpty(e.templateDisplayName)
                        ? "已收录外表"
                        : $"来自盲盒：{e.templateDisplayName}")
                    : "尚未获得此外表";
            }

            if (detailIcon != null)
            {
                Sprite icon = LubyPrefabIcon.Resolve(e.prefab, fallbackIcon);
                if (on && icon != null)
                {
                    detailIcon.sprite = icon;
                    detailIcon.color = Color.white;
                    detailIcon.enabled = true;
                }
                else
                {
                    detailIcon.sprite = icon != null ? icon : fallbackIcon;
                    detailIcon.color = new Color(0.2f, 0.2f, 0.22f, 0.9f);
                    detailIcon.enabled = detailIcon.sprite != null;
                }

                detailIcon.preserveAspect = true;
            }
        }

        private void ClearDetail()
        {
            if (detailNameText != null)
                detailNameText.text = "—";
            if (detailDescText != null)
                detailDescText.text = "暂无外表条目";
            if (detailIcon != null)
                detailIcon.enabled = false;
        }

        private void ClearSlots()
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i] != null)
                    Destroy(_slots[i].gameObject);
            }

            _slots.Clear();
        }
    }
}
