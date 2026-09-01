using System;
using System.Collections.Generic;
using UnityEngine;

namespace DesktopPet.Luby
{
    /// <summary>外表图鉴：按 appearanceKey（外形 Prefab 名）永久收录。</summary>
    public sealed class LubyAppearanceCodex : MonoBehaviour
    {
        private readonly HashSet<string> _unlocked = new HashSet<string>(StringComparer.Ordinal);
        private readonly List<string> _ordered = new List<string>(32);

        public event Action Changed;

        public int UnlockedCount => _unlocked.Count;

        private void Awake()
        {
            DesktopPetServices.RegisterAppearanceCodex(this);
        }

        private void OnDestroy()
        {
            DesktopPetServices.UnregisterAppearanceCodex(this);
        }

        public bool IsUnlocked(string appearanceKey)
        {
            return !string.IsNullOrEmpty(appearanceKey) && _unlocked.Contains(appearanceKey);
        }

        /// <summary>首次收录返回 true。</summary>
        public bool TryUnlock(string appearanceKey)
        {
            if (string.IsNullOrEmpty(appearanceKey))
                return false;
            if (!_unlocked.Add(appearanceKey))
                return false;

            _ordered.Add(appearanceKey);
            Changed?.Invoke();
            return true;
        }

        public void Clear()
        {
            if (_unlocked.Count == 0)
                return;
            _unlocked.Clear();
            _ordered.Clear();
            Changed?.Invoke();
        }

        public void ReplaceFromSave(IReadOnlyList<string> keys)
        {
            _unlocked.Clear();
            _ordered.Clear();
            if (keys != null)
            {
                for (int i = 0; i < keys.Count; i++)
                {
                    string key = keys[i];
                    if (string.IsNullOrEmpty(key) || !_unlocked.Add(key))
                        continue;
                    _ordered.Add(key);
                }
            }

            Changed?.Invoke();
        }

        public List<string> CaptureForSave()
        {
            return new List<string>(_ordered);
        }

        /// <summary>用桌上 + 仓库已有外表补录（旧档兼容）。返回是否有新解锁。</summary>
        public bool BackfillFromOwned(LubyWorld world)
        {
            if (world == null)
                return false;

            bool any = false;
            IReadOnlyList<LubyInstanceComponent> desk = world.Instances;
            for (int i = 0; i < desk.Count; i++)
            {
                LubyInstanceComponent inst = desk[i];
                if (inst?.Data == null || string.IsNullOrEmpty(inst.Data.appearanceKey))
                    continue;
                if (_unlocked.Add(inst.Data.appearanceKey))
                {
                    _ordered.Add(inst.Data.appearanceKey);
                    any = true;
                }
            }

            IReadOnlyList<LubyInstanceData> warehouse = world.Warehouse;
            for (int i = 0; i < warehouse.Count; i++)
            {
                LubyInstanceData data = warehouse[i];
                if (data == null || string.IsNullOrEmpty(data.appearanceKey))
                    continue;
                if (_unlocked.Add(data.appearanceKey))
                {
                    _ordered.Add(data.appearanceKey);
                    any = true;
                }
            }

            if (any)
                Changed?.Invoke();
            return any;
        }
    }

    /// <summary>图鉴格子展示用的外表条目（Catalog 去重）。</summary>
    public readonly struct LubyAppearanceCodexEntry
    {
        public readonly string appearanceKey;
        public readonly GameObject prefab;
        public readonly string templateId;
        public readonly string templateDisplayName;

        public LubyAppearanceCodexEntry(
            string appearanceKey,
            GameObject prefab,
            string templateId,
            string templateDisplayName)
        {
            this.appearanceKey = appearanceKey;
            this.prefab = prefab;
            this.templateId = templateId;
            this.templateDisplayName = templateDisplayName;
        }

        public string DisplayName =>
            !string.IsNullOrEmpty(appearanceKey) ? appearanceKey : "—";
    }
}
