using System;
using System.Collections.Generic;
using DesktopPet;
using DesktopPet.AI;
using DesktopPet.Save;
using Sirenix.OdinInspector;
using UnityEngine;

namespace DesktopPet.Luby
{
    /// <summary>管理桌面 Luby 实例与未出场仓库。</summary>
    public sealed class LubyWorld : MonoBehaviour
    {
        public const int MinDeskCapacity = DeskCapacityDefaults.MinDeskCapacity;

        [Title("Luby 世界")]
        [LabelText("模板目录")]
        [SerializeField]
        private LubyTemplateCatalog catalog;

        [LabelText("实例根节点")]
        [Tooltip("场景须预挂；空则报错不自动创建。在 LubySystem 下挂 LubyRoot 并拖到此字段。")]
        [SerializeField]
        private Transform lubyRoot;

        [ShowInInspector, ReadOnly]
        private int DebugCount => _instances.Count;

        [ShowInInspector, ReadOnly]
        private int DebugWarehouse => _warehouse.Count;

        private readonly List<LubyInstanceComponent> _instances = new List<LubyInstanceComponent>(8);
        private readonly List<LubyInstanceData> _warehouse = new List<LubyInstanceData>(16);
        private int _deskCapacity = DeskCapacityDefaults.LubyInitial;

        public int Count => _instances.Count;
        public int WarehouseCount => _warehouse.Count;
        public int DeskCapacity => _deskCapacity;
        /// <summary>桌上可见 + 当前背景上未回桌的探险（其它场景出发的不占本场景栏位）。</summary>
        public int OccupiedDeskSlots => Count + AwayOnAdventureCount;
        public bool CanSpawnOnDesk => OccupiedDeskSlots < DeskCapacity;
        public IReadOnlyList<LubyInstanceComponent> Instances => _instances;
        public IReadOnlyList<LubyInstanceData> Warehouse => _warehouse;

        /// <summary>仓库里绑定当前背景、尚未回桌的探险只数（业务上全局最多 1 趟，但只在本场景计入容量）。</summary>
        public int AwayOnAdventureCount
        {
            get
            {
                string bgId = ResolveActiveBackgroundId();
                int n = 0;
                for (int i = 0; i < _warehouse.Count; i++)
                {
                    LubyInstanceData d = _warehouse[i];
                    if (d != null && d.IsOnAdventureTripForBackground(bgId))
                        n++;
                }

                return n;
            }
        }

        /// <summary>是否有任意场景的未回桌探险（同时只允许一趟出门）。</summary>
        public bool HasAnyAwayOnAdventure
        {
            get
            {
                for (int i = 0; i < _warehouse.Count; i++)
                {
                    if (_warehouse[i] != null && _warehouse[i].IsOnAdventureTrip)
                        return true;
                }

                return false;
            }
        }
        public event Action WarehouseChanged;
        public event Action DeskChanged;

        /// <summary>模板目录；空则从 Resources 加载默认。</summary>
        public LubyTemplateCatalog Catalog
        {
            get
            {
                if (catalog == null)
                    catalog = LubyTemplateCatalog.LoadDefault();
                return catalog;
            }
        }

        private void Awake()
        {
            if (DesktopPetServices.LubyWorld != null && DesktopPetServices.LubyWorld != this)
            {
                Debug.LogWarning("[LubyWorld] 场景中已有 LubyWorld，销毁重复实例。");
                Destroy(gameObject);
                return;
            }

            DesktopPetServices.RegisterLubyWorld(this);

            if (catalog == null)
                catalog = LubyTemplateCatalog.LoadDefault();

            if (lubyRoot == null)
            {
                Debug.LogError(
                    "[LubyWorld] 未指定 lubyRoot。请在 LubySystem 下预挂 LubyRoot 并拖到字段。",
                    this);
            }

            if (GetComponent<LubyCoinCollectSystem>() == null)
            {
                Debug.LogError(
                    "[LubyWorld] 缺少 LubyCoinCollectSystem。请在 LubySystem 上预挂该组件。",
                    this);
            }
        }

        private void OnDestroy()
        {
            DesktopPetServices.UnregisterLubyWorld(this);
        }

        public void SetDeskCapacity(int value)
        {
            int next = ClampDeskCapacity(value);
            if (_deskCapacity != next)
            {
                _deskCapacity = next;
                DeskChanged?.Invoke();
            }

            EvictDeskLubiesToFitCapacity();
        }

        /// <summary>
        /// 可见只数压到「容量 − 当前背景未回桌探险」。
        /// 其它场景出发的探险不占本场景栏位；仅本场景有人在外面时才会挤走桌上多余的。
        /// </summary>
        public void EvictDeskLubiesToFitCapacity()
        {
            int allowedVisible = Mathf.Max(0, DeskCapacity - AwayOnAdventureCount);
            for (int i = _instances.Count - 1; i >= 0 && Count > allowedVisible; i--)
            {
                LubyInstanceComponent inst = _instances[i];
                if (inst == null)
                    continue;
                if (DesktopPetServices.LubyAdventure?.IsLubyInteractionLocked(inst) == true)
                    continue;
                TryReturnDeskToWarehouse(inst);
            }
        }

        public static string ResolveActiveBackgroundId()
        {
            Background.BackgroundSystem bg = Background.BackgroundSystem.Instance;
            if (bg != null && !string.IsNullOrEmpty(bg.CurrentBackgroundId))
                return bg.CurrentBackgroundId;
            if (DesktopPetSaveMgr.Current != null && !string.IsNullOrEmpty(DesktopPetSaveMgr.Current.currentBackgroundId))
                return DesktopPetSaveMgr.Current.currentBackgroundId;
            return Background.BackgroundDefinition.TransparentId;
        }

        public static int ResolveInitialDeskCapacity()
        {
            return DeskCapacityDefaults.LubyInitial;
        }

        public static int ResolveMaxDeskCapacity()
        {
            return DeskCapacityDefaults.LubyMax;
        }

        private static int ClampDeskCapacity(int value)
        {
            int max = Mathf.Max(MinDeskCapacity, ResolveMaxDeskCapacity());
            return Mathf.Clamp(value, MinDeskCapacity, max);
        }

        public LubyInstanceComponent Spawn(LubyInstanceData data, float? overrideWorldX = null, bool clampSpawnX = true)
        {
            LubyTemplateCatalog cat = Catalog;
            if (data == null || cat == null)
                return null;

            LubyTemplateDefinition template = cat.FindTemplateById(data.templateId);
            if (template == null)
            {
                Debug.LogError($"[LubyWorld] 找不到模板: {data.templateId}");
                return null;
            }

            GameObject prefab = template.ResolveSpawnPrefab(data.appearanceKey);
            if (prefab == null)
            {
                Debug.LogError($"[LubyWorld] 模板无 Prefab: {data.templateId} / {data.appearanceKey}");
                return null;
            }

            if (string.IsNullOrEmpty(data.instanceId))
                data.instanceId = Guid.NewGuid().ToString("N");

            if (string.IsNullOrEmpty(data.appearanceKey))
                data.appearanceKey = prefab.name;

            if (Mathf.Approximately(data.x, 0f) && Mathf.Approximately(data.y, 0f))
                AssignRandomPosition(data);

            if (data.scale <= 0.01f)
                data.scale = template.ResolveScale(prefab);

            GameObject go = Instantiate(prefab, lubyRoot);
            DesktopPetLayers.ApplyLuby(go);
            string label = !string.IsNullOrEmpty(data.petName)
                ? data.petName
                : data.instanceId.Substring(0, 6);
            go.name = "Luby_" + label;

            LubyInstanceComponent inst = go.GetComponent<LubyInstanceComponent>();
            if (inst == null)
            {
                Debug.LogError(
                    $"[LubyWorld] Prefab 缺少 LubyInstanceComponent: {prefab.name}。请手改外形预制体。",
                    go);
                Destroy(go);
                return null;
            }

            if (go.GetComponent<LubyCoinCollector>() == null)
            {
                Debug.LogError(
                    $"[LubyWorld] Prefab 缺少 LubyCoinCollector: {prefab.name}。请手改外形预制体。",
                    go);
                Destroy(go);
                return null;
            }

            inst.Initialize(data, cat, template);
            _instances.Add(inst);

            DesktopPetPlayfieldBounds.RefreshGlobal();
            PetLocomotion loco = inst.GetComponent<PetLocomotion>();
            if (loco != null)
            {
                float spawnX = overrideWorldX ?? data.x;
                loco.SnapFeetToGround(spawnX, clampSpawnX);
                inst.SyncPositionToData();
            }
            else
            {
                ClampToGround(ref data.x, out float y);
                Vector3 p = inst.transform.position;
                p.x = data.x;
                p.y = y;
                inst.transform.position = p;
                inst.SyncPositionToData();
            }

            DeskChanged?.Invoke();
            return inst;
        }

        public void AddToWarehouse(LubyInstanceData data)
        {
            if (data == null)
                return;
            if (string.IsNullOrEmpty(data.instanceId))
                data.instanceId = Guid.NewGuid().ToString("N");
            _warehouse.Add(data.Clone());
            WarehouseChanged?.Invoke();
        }

        public bool TryTakeFromWarehouse(string instanceId, out LubyInstanceData data)
        {
            data = null;
            if (string.IsNullOrEmpty(instanceId))
                return false;

            for (int i = 0; i < _warehouse.Count; i++)
            {
                LubyInstanceData e = _warehouse[i];
                if (e == null || e.instanceId != instanceId)
                    continue;
                data = e.Clone();
                _warehouse.RemoveAt(i);
                WarehouseChanged?.Invoke();
                return true;
            }

            return false;
        }

        public LubyInstanceComponent FindDeskById(string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId))
                return null;
            for (int i = 0; i < _instances.Count; i++)
            {
                LubyInstanceComponent c = _instances[i];
                if (c != null && c.InstanceId == instanceId)
                    return c;
            }

            return null;
        }

        public LubyInstanceData FindWarehouseById(string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId))
                return null;
            for (int i = 0; i < _warehouse.Count; i++)
            {
                LubyInstanceData d = _warehouse[i];
                if (d != null && d.instanceId == instanceId)
                    return d;
            }

            return null;
        }

        /// <summary>桌上实例收回仓库（销毁场景物体）。</summary>
        public bool TryReturnDeskToWarehouse(LubyInstanceComponent inst)
        {
            if (inst == null || inst.Data == null)
                return false;

            if (!DetachDeskInstance(inst, destroyGameObject: false))
                return false;

            inst.SyncPositionToData();
            LubyInstanceData data = inst.Data.Clone();
            Destroy(inst.gameObject);
            AddToWarehouse(data);
            return true;
        }

        /// <summary>从桌上列表摘掉（可选销毁）。放置系统收回时用。</summary>
        public bool DetachDeskInstance(LubyInstanceComponent inst, bool destroyGameObject)
        {
            if (inst == null)
                return false;
            int idx = _instances.IndexOf(inst);
            if (idx < 0)
                return false;
            EndDeskInstance(inst);
            _instances.RemoveAt(idx);
            if (destroyGameObject)
                Destroy(inst.gameObject);
            DeskChanged?.Invoke();
            return true;
        }

        public void RebuildFromSave(IReadOnlyList<DesktopPetLubyEntry> entries)
        {
            ClearDeskOnly();

            if (entries == null || entries.Count == 0)
                return;

            for (int i = 0; i < entries.Count; i++)
            {
                DesktopPetLubyEntry e = entries[i];
                if (e == null || string.IsNullOrEmpty(e.templateId))
                    continue;
                Spawn(LubyInstanceData.FromSaveEntry(e));
            }

            EvictDeskLubiesToFitCapacity();
        }

        public void RebuildWarehouseFromSave(IReadOnlyList<DesktopPetLubyEntry> entries)
        {
            _warehouse.Clear();
            if (entries != null)
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    LubyInstanceData data = LubyInstanceData.FromSaveEntry(entries[i]);
                    if (data == null || string.IsNullOrEmpty(data.templateId))
                        continue;
                    _warehouse.Add(data);
                }
            }

            WarehouseChanged?.Invoke();
        }

        public List<DesktopPetLubyEntry> CaptureForSave()
        {
            var list = new List<DesktopPetLubyEntry>(_instances.Count);
            for (int i = 0; i < _instances.Count; i++)
            {
                LubyInstanceComponent inst = _instances[i];
                if (inst == null || inst.Data == null)
                    continue;

                inst.SyncPositionToData();
                list.Add(inst.Data.ToSaveEntry());
            }

            return list;
        }

        public List<DesktopPetLubyEntry> CaptureWarehouseForSave()
        {
            var list = new List<DesktopPetLubyEntry>(_warehouse.Count);
            for (int i = 0; i < _warehouse.Count; i++)
            {
                LubyInstanceData d = _warehouse[i];
                if (d == null)
                    continue;
                list.Add(d.ToSaveEntry());
            }

            return list;
        }

        public void ClearAll()
        {
            ClearDeskOnly();
            _warehouse.Clear();
            WarehouseChanged?.Invoke();
        }

        private void ClearDeskOnly()
        {
            for (int i = _instances.Count - 1; i >= 0; i--)
            {
                LubyInstanceComponent inst = _instances[i];
                if (inst != null)
                {
                    EndDeskInstance(inst);
                    Destroy(inst.gameObject);
                }
            }

            _instances.Clear();
            DeskChanged?.Invoke();
        }

        /// <summary>移除最近一只桌上 Luby。</summary>
        public bool RemoveLast()
        {
            if (_instances.Count == 0)
                return false;

            int idx = _instances.Count - 1;
            LubyInstanceComponent inst = _instances[idx];
            _instances.RemoveAt(idx);
            if (inst != null)
            {
                EndDeskInstance(inst);
                Destroy(inst.gameObject);
            }
            DeskChanged?.Invoke();
            return true;
        }

        private static void EndDeskInstance(LubyInstanceComponent inst)
        {
            if (inst == null)
                return;
            DesktopPetServices.EndAllLubyActivities(inst);
        }

        private void GetHorizontalBounds(out float minX, out float maxX)
        {
            DesktopPetPlayfieldBounds playfield = DesktopPetPlayfieldBounds.EnsureExists();
            if (playfield != null && playfield.IsValid)
            {
                minX = playfield.MinX;
                maxX = playfield.MaxX;
                return;
            }

            minX = -10f;
            maxX = 10f;
        }

        internal void AssignRandomPosition(LubyInstanceData data)
        {
            if (data == null)
                return;

            GetHorizontalBounds(out float minX, out float maxX);
            data.x = UnityEngine.Random.Range(minX, maxX);
            data.y = DesktopPetServices.ResolveGroundY();
        }

        internal void ClampToGround(ref float x, out float y)
        {
            GetHorizontalBounds(out float minX, out float maxX);
            x = Mathf.Clamp(x, minX, maxX);
            y = DesktopPetServices.ResolveGroundY();
        }
    }
}
