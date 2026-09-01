using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace DesktopPet.Inventory
{
    /// <summary>仓库：已购未摆放的商品数量。</summary>
    public sealed class ItemInventory : MonoBehaviour
    {
        [Serializable]
        public struct Entry
        {
            [LabelText("商品 ID")]
            [Tooltip("对应 ShopItemDefinition.itemId。")]
            public string itemId;

            [LabelText("数量")]
            [MinValue(0)]
            public int count;
        }

        [Title("仓库", "已购未摆放的商品数量；放置时扣 1")]
        [InfoBox("列表通常由存档加载；也可在 Inspector 里手改调试。条目数=0 的会在 ReplaceAll 时被丢掉。", InfoMessageType.None)]

        [LabelText("仓库条目")]
        [ListDrawerSettings(ShowFoldout = true, DraggableItems = true, ShowIndexLabels = true)]
        [SerializeField]
        private List<Entry> entries = new List<Entry>();

        [ShowInInspector, ReadOnly, LabelText("条目种类数")]
        private int DebugEntryKinds => entries != null ? entries.Count : 0;

        public event Action Changed;

        public IReadOnlyList<Entry> Entries => entries;

        private void Awake()
        {
            DesktopPetServices.RegisterInventory(this);
        }

        private void OnDestroy()
        {
            DesktopPetServices.UnregisterInventory(this);
        }

        public int GetCount(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
                return 0;

            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].itemId == itemId)
                    return entries[i].count;
            }

            return 0;
        }

        public void Add(string itemId, int amount = 1)
        {
            if (string.IsNullOrEmpty(itemId) || amount <= 0)
                return;

            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].itemId != itemId)
                    continue;

                Entry e = entries[i];
                e.count += amount;
                entries[i] = e;
                Changed?.Invoke();
                return;
            }

            entries.Add(new Entry { itemId = itemId, count = amount });
            Changed?.Invoke();
        }

        public bool TryRemove(string itemId, int amount = 1)
        {
            if (string.IsNullOrEmpty(itemId) || amount <= 0)
                return false;

            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].itemId != itemId)
                    continue;

                if (entries[i].count < amount)
                    return false;

                Entry e = entries[i];
                e.count -= amount;
                if (e.count <= 0)
                    entries.RemoveAt(i);
                else
                    entries[i] = e;

                Changed?.Invoke();
                return true;
            }

            return false;
        }

        public void Clear()
        {
            entries.Clear();
            Changed?.Invoke();
        }

        /// <summary>用存档列表整体替换仓库内容。</summary>
        public void ReplaceAll(IReadOnlyList<Entry> source)
        {
            entries.Clear();
            if (source != null)
            {
                for (int i = 0; i < source.Count; i++)
                {
                    Entry e = source[i];
                    if (string.IsNullOrEmpty(e.itemId) || e.count <= 0)
                        continue;
                    entries.Add(new Entry { itemId = e.itemId, count = e.count });
                }
            }

            Changed?.Invoke();
        }
    }
}
