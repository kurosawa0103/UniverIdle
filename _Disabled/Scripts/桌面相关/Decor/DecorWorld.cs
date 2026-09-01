using System;
using System.Collections.Generic;
using DesktopPet;
using DesktopPet.Save;
using DesktopPet.Shop;
using Sirenix.OdinInspector;
using UnityEngine;

namespace DesktopPet.Decor
{
    /// <summary>管理已摆装饰：生成、重叠/叠放检测、从存档重建。</summary>
    public sealed class DecorWorld : MonoBehaviour
    {
        private sealed class PassiveGoldState
        {
            public float nextDropAt;
        }

        [Title("装饰世界", "已摆实例列表 / 重叠与叠放 / 存档重建")]
        [InfoBox("上架：手持 canStackOnOthers，落到 DecorPlaceSurface；贴地走 DesktopPetGround。水平重叠非法；上架时忽略与 Owner 本体重叠。", InfoMessageType.None)]

        [BoxGroup("场景")]
        [LabelText("装饰根节点")]
        [Tooltip("已摆装饰的父 Transform。场景须预挂；空则报错不自动创建。")]
        [SerializeField]
        private Transform decorRoot;

        [BoxGroup("场景")]
        [LabelText("默认排序层 Order")]
        [Tooltip("生成装饰时写到 SpriteRenderer.sortingOrder。")]
        [SerializeField]
        private int sortingOrder = 5;

        [BoxGroup("场景")]
        [LabelText("掉落金币根节点")]
        [Tooltip("场景须预挂；空则报错不自动创建。")]
        [SerializeField]
        private Transform droppedGoldRoot;

        [BoxGroup("运行时")]
        [ShowInInspector, ReadOnly, LabelText("已摆数量")]
        private int DebugPlacedCount => _placed.Count;

        [BoxGroup("运行时")]
        [ShowInInspector, ReadOnly, LabelText("已摆列表")]
        [ListDrawerSettings(IsReadOnly = true, ShowFoldout = true, DraggableItems = false)]
        private List<PlacedDecor> DebugPlacedList => _placed;

        [BoxGroup("运行时")]
        [ShowInInspector, ReadOnly, LabelText("桌上未捡金币")]
        private int DebugDroppedGoldAmount => DecorGoldCoin.SumUncollectedAmount();

        [Title("被动产币", "已摆且配了产币区间的装饰会随机掉可拾取金币")]

        [LabelText("轮询间隔")]
        [MinValue(0.1f)]
        [SerializeField]
        private float passiveGoldTickInterval = 0.35f;

        [LabelText("桌上未捡金币上限")]
        [Tooltip("地上未入账金币价值合计达到此值后暂停产币；捡走后恢复。")]
        [MinValue(1)]
        [SerializeField]
        private int droppedGoldCap = FallbackDroppedGoldCap;

        public const int MinDeskCapacity = DeskCapacityDefaults.MinDeskCapacity;
        private const int MinDroppedGoldCap = 1;
        private const int FallbackDroppedGoldCap = 20;

        private readonly List<PlacedDecor> _placed = new List<PlacedDecor>();
        private readonly List<DecorInteractable> _interactables = new List<DecorInteractable>(16);
        private readonly Dictionary<string, PassiveGoldState> _passiveGoldStates = new Dictionary<string, PassiveGoldState>();
        private readonly List<string> _passiveGoldRemoveKeys = new List<string>();
        private readonly HashSet<string> _passiveGoldAliveIds = new HashSet<string>();
        private int _deskCapacity = DeskCapacityDefaults.DecorInitial;
        private int _droppedGoldCap = FallbackDroppedGoldCap;
        private float _nextPassiveGoldTickAt;

        public IReadOnlyList<PlacedDecor> Placed => _placed;
        public IReadOnlyList<DecorInteractable> Interactables => _interactables;
        public int Count => _placed.Count;
        public int DeskCapacity => _deskCapacity;
        public int DroppedGoldCap => _droppedGoldCap;
        public bool CanPlaceOnDesk => Count < DeskCapacity;
        public event Action PlacedChanged;

        private void Awake()
        {
            if (DesktopPetServices.DecorWorld != null && DesktopPetServices.DecorWorld != this)
            {
                Debug.LogWarning("[DecorWorld] 场景中已有 DecorWorld，销毁重复实例。");
                Destroy(gameObject);
                return;
            }

            _deskCapacity = DeskCapacityDefaults.DecorInitial;
            _droppedGoldCap = Mathf.Max(MinDroppedGoldCap, droppedGoldCap);
            droppedGoldCap = _droppedGoldCap;
            DesktopPetServices.RegisterDecorWorld(this);
            DesktopPetLayers.EnsureGoldIgnoresActors();
            if (decorRoot == null)
            {
                Debug.LogError(
                    "[DecorWorld] 未指定 decorRoot。请在 DecorSystem 下预挂 PlacedDecors 并拖到字段。",
                    this);
            }

            if (droppedGoldRoot == null)
            {
                Debug.LogError(
                    "[DecorWorld] 未指定 droppedGoldRoot。请在 DecorSystem 下预挂 DroppedGold 并拖到字段。",
                    this);
            }
        }

        private void OnDestroy()
        {
            DesktopPetServices.UnregisterDecorWorld(this);
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextPassiveGoldTickAt)
                return;

            _nextPassiveGoldTickAt = Time.unscaledTime + passiveGoldTickInterval;
            TickPassiveGold();
        }

        public void SetDeskCapacity(int value)
        {
            _deskCapacity = ClampDeskCapacity(value);
            PlacedChanged?.Invoke();
        }

        public static int ResolveInitialDeskCapacity()
        {
            return DeskCapacityDefaults.DecorInitial;
        }

        public static int ResolveMaxDeskCapacity()
        {
            return DeskCapacityDefaults.DecorMax;
        }

        private static int ClampDeskCapacity(int value)
        {
            int max = Mathf.Max(MinDeskCapacity, ResolveMaxDeskCapacity());
            return Mathf.Clamp(value, MinDeskCapacity, max);
        }

        public PlacedDecor FindById(string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId))
                return null;

            for (int i = 0; i < _placed.Count; i++)
            {
                if (_placed[i] != null && _placed[i].InstanceId == instanceId)
                    return _placed[i];
            }

            return null;
        }

        /// <summary>是否有其它装饰叠在本件上（ParentInstanceId 指向本实例）。床/椅被占时不应再当落点。</summary>
        public bool HasStackedChildren(PlacedDecor parent)
        {
            if (parent == null || string.IsNullOrEmpty(parent.InstanceId))
                return false;

            string id = parent.InstanceId;
            for (int i = 0; i < _placed.Count; i++)
            {
                PlacedDecor d = _placed[i];
                if (d != null &&
                    d.isActiveAndEnabled &&
                    string.Equals(d.ParentInstanceId, id, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 点选拾取：点落在多件 Body 内时优先叠放层级更深的子件（可单独点上层；点底座空白才拖父节点）。
        /// </summary>
        public PlacedDecor FindPickupUnderPoint(Vector2 worldPoint, float edgePad = 0.02f)
        {
            PlacedDecor best = null;
            int bestDepth = -1;
            float bestY = float.MinValue;

            for (int i = 0; i < _placed.Count; i++)
            {
                PlacedDecor d = _placed[i];
                if (d == null)
                    continue;

                Bounds b = d.WorldBounds;
                float minX = b.min.x - edgePad;
                float maxX = b.max.x + edgePad;
                float minY = b.min.y - edgePad;
                float maxY = b.max.y + edgePad;
                if (worldPoint.x < minX || worldPoint.x > maxX || worldPoint.y < minY || worldPoint.y > maxY)
                    continue;

                int depth = GetStackDepth(d);
                float y = d.transform.position.y;
                if (depth > bestDepth || (depth == bestDepth && y > bestY))
                {
                    bestDepth = depth;
                    bestY = y;
                    best = d;
                }
            }

            return best;
        }

        /// <summary>沿 ParentInstanceId 向上数层数；贴地为 0。</summary>
        public int GetStackDepth(PlacedDecor decor)
        {
            if (decor == null)
                return 0;

            int depth = 0;
            string parentId = decor.ParentInstanceId;
            int guard = 0;
            while (!string.IsNullOrEmpty(parentId) && guard++ < 64)
            {
                depth++;
                PlacedDecor parent = FindById(parentId);
                if (parent == null)
                    break;
                parentId = parent.ParentInstanceId;
            }

            return depth;
        }

        /// <summary>
        /// 在光标水平位置找可摆放面（不要求靠近层板高度；取垂直最近一层）。
        /// ignoreOwner：手持子树所属装饰，避免吸到自己身上的面。
        /// groundPreferBand：光标靠近地面时优先贴地、不抢层板。
        /// </summary>
        public DecorPlaceSurface FindPlaceSurface(
            Vector2 cursor,
            float groundY,
            ShopItemDefinition held,
            float groundPreferBand,
            PlacedDecor ignoreOwner = null)
        {
            if (held == null || !held.canStackOnOthers)
                return null;

            DecorPlaceSurface best = null;
            float bestDistToTop = float.MaxValue;
            float groundBand = Mathf.Max(0.05f, groundPreferBand);

            for (int i = 0; i < _placed.Count; i++)
            {
                PlacedDecor d = _placed[i];
                if (d == null || d == ignoreOwner)
                    continue;

                DecorPlaceSurface[] surfaces = d.GetComponentsInChildren<DecorPlaceSurface>(true);
                for (int s = 0; s < surfaces.Length; s++)
                {
                    DecorPlaceSurface surface = surfaces[s];
                    if (surface == null || !surface.isActiveAndEnabled)
                        continue;

                    if (!surface.ContainsFootX(cursor.x))
                        continue;

                    float topY = surface.SurfaceY;
                    float distToTop = Mathf.Abs(cursor.y - topY);
                    float distToGround = Mathf.Abs(cursor.y - groundY);

                    // 贴地附近且更近地面时不抢吸附到层板
                    if (cursor.y <= groundY + groundBand && distToTop > distToGround)
                        continue;

                    if (distToTop < bestDistToTop)
                    {
                        bestDistToTop = distToTop;
                        best = surface;
                    }
                }
            }

            return best;
        }

        /// <summary>候选落点的世界坐标（物体 transform.position，脚底对齐）。有面则上架，否则贴地。</summary>
        public Vector3 ComputeSnapPosition(
            Bounds footprint,
            float groundY,
            Vector2 cursorWorld,
            DecorPlaceSurface placeSurface,
            out string parentInstanceId)
        {
            parentInstanceId = null;
            float bottomToPivot = -footprint.min.y;
            float x = cursorWorld.x;

            if (placeSurface != null)
            {
                PlacedDecor owner = placeSurface.Owner;
                if (owner != null)
                    parentInstanceId = owner.InstanceId;
                return new Vector3(x, placeSurface.SurfaceY + bottomToPivot, 0f);
            }

            return new Vector3(x, groundY + bottomToPivot, 0f);
        }

        public bool OverlapsOthers(Bounds candidateBounds, PlacedDecor ignoreParent)
        {
            for (int i = 0; i < _placed.Count; i++)
            {
                PlacedDecor d = _placed[i];
                if (d == null)
                    continue;
                if (ignoreParent != null && d == ignoreParent)
                    continue;

                Bounds other = d.WorldBounds;
                if (!BoundsOverlap2D(candidateBounds, other))
                    continue;

                return true;
            }

            return false;
        }

        /// <summary>
        /// 评估落点：上架时验层高（脚 X / canStack 已由 FindPlaceSurface 闸过）；
        /// 忽略与 Owner 本体重叠。
        /// </summary>
        public bool IsPlacementValid(
            Bounds candidateBounds,
            DecorPlaceSurface placeSurface,
            ShopItemDefinition item)
        {
            PlacedDecor ignoreParent = null;
            if (placeSurface != null)
            {
                float itemH = item != null
                    ? item.ResolvePlaceHeight(candidateBounds.size.y)
                    : candidateBounds.size.y;
                if (!placeSurface.AllowsItemHeight(itemH))
                    return false;

                ignoreParent = placeSurface.Owner;
            }

            return !OverlapsOthers(candidateBounds, ignoreParent);
        }

        public static bool BoundsOverlap2D(Bounds a, Bounds b)
        {
            // 略微内缩，避免贴边误判
            const float pad = 0.02f;
            float aMinX = a.min.x + pad;
            float aMaxX = a.max.x - pad;
            float aMinY = a.min.y + pad;
            float aMaxY = a.max.y - pad;
            float bMinX = b.min.x + pad;
            float bMaxX = b.max.x - pad;
            float bMinY = b.min.y + pad;
            float bMaxY = b.max.y - pad;

            if (aMaxX <= aMinX || aMaxY <= aMinY || bMaxX <= bMinX || bMaxY <= bMinY)
                return a.Intersects(b);

            return aMinX < bMaxX && aMaxX > bMinX && aMinY < bMaxY && aMaxY > bMinY;
        }

        public PlacedDecor Spawn(
            ShopItemDefinition item,
            Vector3 position,
            string parentInstanceId,
            string instanceId = null)
        {
            if (item == null)
                return null;

            if (!CanPlaceOnDesk && string.IsNullOrEmpty(instanceId))
            {
                Debug.LogWarning($"[DecorWorld] 桌上装饰已满（{Count}/{DeskCapacity}），无法放置。");
                return null;
            }

            GameObject go = DecorPrefabUtil.InstantiateDecor(item, decorRoot);
            if (go == null)
                return null;

            go.transform.position = position;
            ApplySorting(go);

            PlacedDecor placed = go.GetComponent<PlacedDecor>();
            if (placed == null)
            {
                Debug.LogError(
                    $"[DecorWorld] Prefab 缺少 PlacedDecor：{item.itemId}。请手改 placementPrefab。",
                    go);
                if (Application.isPlaying)
                    Destroy(go);
                else
                    DestroyImmediate(go);
                return null;
            }

            string id = string.IsNullOrEmpty(instanceId) ? Guid.NewGuid().ToString("N") : instanceId;
            placed.Initialize(id, item, parentInstanceId);
            _placed.Add(placed);
            TrackInteractables(go, add: true);
            PlacedChanged?.Invoke();
            return placed;
        }

        /// <summary>收集 root 及其全部叠放后代（root 在前，BFS）。</summary>
        public List<PlacedDecor> CollectSubtree(PlacedDecor root)
        {
            var result = new List<PlacedDecor>();
            if (root == null)
                return result;

            result.Add(root);
            for (int i = 0; i < result.Count; i++)
            {
                PlacedDecor node = result[i];
                if (node == null || string.IsNullOrEmpty(node.InstanceId))
                    continue;

                string id = node.InstanceId;
                for (int j = 0; j < _placed.Count; j++)
                {
                    PlacedDecor d = _placed[j];
                    if (d == null || d.ParentInstanceId != id)
                        continue;
                    if (!result.Contains(d))
                        result.Add(d);
                }
            }

            return result;
        }

        /// <summary>
        /// 从世界列表卸下整棵叠放子树（不销毁）；子物体 Unity 挂到 root 下便于一起拖动。
        /// </summary>
        public List<PlacedDecor> DetachSubtree(PlacedDecor root)
        {
            List<PlacedDecor> subtree = CollectSubtree(root);
            for (int i = 0; i < subtree.Count; i++)
            {
                PlacedDecor d = subtree[i];
                if (d != null)
                {
                    _placed.Remove(d);
                    TrackInteractables(d.gameObject, add: false);
                }
            }

            for (int i = 1; i < subtree.Count; i++)
            {
                PlacedDecor d = subtree[i];
                if (d != null && root != null)
                    d.transform.SetParent(root.transform, true);
            }

            PlacedChanged?.Invoke();
            return subtree;
        }

        /// <summary>把卸下的子树写回世界（位置 / 底座 parentId）；子物体恢复到 decorRoot。</summary>
        public void CommitSubtree(PlacedDecor root, Vector3 position, string parentInstanceId, List<PlacedDecor> subtree)
        {
            if (root == null || subtree == null || subtree.Count == 0)
                return;

            if (decorRoot != null)
                root.transform.SetParent(decorRoot, true);
            root.transform.position = position;
            root.SetParentInstanceId(parentInstanceId);

            for (int i = 1; i < subtree.Count; i++)
            {
                PlacedDecor d = subtree[i];
                if (d == null)
                    continue;
                if (decorRoot != null)
                    d.transform.SetParent(decorRoot, true);
            }

            for (int i = 0; i < subtree.Count; i++)
            {
                PlacedDecor d = subtree[i];
                if (d != null && !_placed.Contains(d))
                {
                    _placed.Add(d);
                    TrackInteractables(d.gameObject, add: true);
                }
            }

            PlacedChanged?.Invoke();
        }

        /// <summary>
        /// 整棵子树落点是否合法：临时挪 root 后复用 IsPlacementValid（层高只验根），
        /// 子件只查与世界其它装饰重叠。
        /// </summary>
        public bool IsSubtreePlacementValid(
            PlacedDecor root,
            List<PlacedDecor> subtree,
            Vector3 candidateRootPos,
            DecorPlaceSurface placeSurface,
            ShopItemDefinition rootItem)
        {
            if (root == null || subtree == null || subtree.Count == 0)
                return false;

            Vector3 saved = root.transform.position;
            root.transform.position = candidateRootPos;

            bool ok = IsPlacementValid(root.WorldBounds, placeSurface, rootItem);
            if (ok)
            {
                for (int i = 1; i < subtree.Count; i++)
                {
                    PlacedDecor d = subtree[i];
                    if (d == null)
                        continue;

                    if (OverlapsOthers(d.WorldBounds, null))
                    {
                        ok = false;
                        break;
                    }
                }
            }

            root.transform.position = saved;
            return ok;
        }

        public void ClearAll()
        {
            for (int i = _placed.Count - 1; i >= 0; i--)
            {
                if (_placed[i] != null)
                    Destroy(_placed[i].gameObject);
            }

            _placed.Clear();
            _interactables.Clear();
            _passiveGoldStates.Clear();
            PlacedChanged?.Invoke();
        }

        private void TrackInteractables(GameObject root, bool add)
        {
            if (root == null)
                return;

            DecorInteractable[] items = root.GetComponentsInChildren<DecorInteractable>(true);
            for (int i = 0; i < items.Length; i++)
            {
                DecorInteractable d = items[i];
                if (d == null)
                    continue;
                if (add)
                {
                    if (!_interactables.Contains(d))
                        _interactables.Add(d);
                }
                else
                {
                    _interactables.Remove(d);
                }
            }
        }

        public void RebuildFromSave(IReadOnlyList<DesktopPetPlacedEntry> entries, ShopCatalog catalog)
        {
            ClearAll();
            if (entries == null || catalog == null)
                return;

            // 先无父后有父，避免叠放顺序问题
            var pending = new List<DesktopPetPlacedEntry>();
            for (int i = 0; i < entries.Count; i++)
                pending.Add(entries[i]);

            int guard = 0;
            while (pending.Count > 0 && guard++ < 256)
            {
                bool progress = false;
                for (int i = pending.Count - 1; i >= 0; i--)
                {
                    DesktopPetPlacedEntry e = pending[i];
                    if (!string.IsNullOrEmpty(e.parentInstanceId) && FindById(e.parentInstanceId) == null)
                        continue;

                    ShopItemDefinition def = catalog.FindById(e.itemId);
                    if (def == null)
                    {
                        pending.RemoveAt(i);
                        continue;
                    }

                    Spawn(def, new Vector3(e.x, e.y, 0f), e.parentInstanceId, e.instanceId);
                    pending.RemoveAt(i);
                    progress = true;
                }

                if (!progress)
                    break;
            }
        }

        private void ApplySorting(GameObject go)
        {
            SpriteRenderer[] renderers = go.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                    renderers[i].sortingOrder = sortingOrder;
            }
        }

        private void TickPassiveGold()
        {
            SyncPassiveGoldStates();

            if (DesktopPetServices.Shop?.Wallet == null)
                return;

            int remaining = _droppedGoldCap - DecorGoldCoin.SumUncollectedAmount();
            if (remaining <= 0)
                return;

            for (int i = 0; i < _placed.Count; i++)
            {
                if (remaining <= 0)
                    break;

                PlacedDecor placed = _placed[i];
                if (placed == null || string.IsNullOrEmpty(placed.InstanceId))
                    continue;

                if (!_passiveGoldStates.TryGetValue(placed.InstanceId, out PassiveGoldState state))
                    continue;

                if (Time.time < state.nextDropAt || placed.Definition == null)
                    continue;

                int amount = placed.Definition.RollPassiveGoldAmount();
                if (amount > remaining)
                    amount = remaining;
                if (amount <= 0)
                    break;

                EmitPassiveGold(placed, amount);
                remaining -= amount;
                state.nextDropAt = Time.time + placed.Definition.RollPassiveGoldInterval();
            }
        }

        private void SyncPassiveGoldStates()
        {
            _passiveGoldAliveIds.Clear();
            for (int i = 0; i < _placed.Count; i++)
            {
                PlacedDecor placed = _placed[i];
                if (placed == null || string.IsNullOrEmpty(placed.InstanceId))
                    continue;

                if (placed.Definition == null || !placed.Definition.HasPassiveGold)
                    continue;

                _passiveGoldAliveIds.Add(placed.InstanceId);
                if (_passiveGoldStates.ContainsKey(placed.InstanceId))
                    continue;

                _passiveGoldStates.Add(
                    placed.InstanceId,
                    new PassiveGoldState
                    {
                        nextDropAt = Time.time + placed.Definition.RollPassiveGoldInterval()
                    });
            }

            if (_passiveGoldStates.Count == 0)
                return;

            _passiveGoldRemoveKeys.Clear();
            foreach (KeyValuePair<string, PassiveGoldState> pair in _passiveGoldStates)
            {
                if (!_passiveGoldAliveIds.Contains(pair.Key))
                    _passiveGoldRemoveKeys.Add(pair.Key);
            }

            for (int i = 0; i < _passiveGoldRemoveKeys.Count; i++)
                _passiveGoldStates.Remove(_passiveGoldRemoveKeys[i]);
        }

        private void EmitPassiveGold(PlacedDecor placed, int amount)
        {
            if (placed == null || placed.Definition == null || droppedGoldRoot == null)
                return;

            float groundY = DesktopPetServices.ResolveGroundY();
            Vector3 origin = placed.GetGoldSpawnOrigin();
            DecorGoldCoin.SpawnValue(
                droppedGoldRoot,
                origin,
                groundY,
                amount,
                DesktopPetServices.Shop.Wallet);
        }
    }

    internal static class DecorPrefabUtil
    {
        public static GameObject InstantiateDecor(ShopItemDefinition item, Transform parent)
        {
            if (item == null)
                return null;

            if (item.placementPrefab == null)
            {
                Debug.LogError($"[Decor] 商品未配 placementPrefab：{item.itemId}", item);
                return null;
            }

            GameObject go = UnityEngine.Object.Instantiate(item.placementPrefab, parent);
            go.name = "Decor_" + item.itemId;
            DesktopPetLayers.ApplyDecor(go);
            if (go.GetComponentInChildren<Collider2D>() == null)
            {
                Debug.LogError(
                    $"[Decor] 装饰 Prefab 缺少 Collider2D：{go.name}。请手改 placementPrefab。",
                    go);
            }

            return go;
        }

        public static Bounds MeasureFootprint(ShopItemDefinition item)
        {
            GameObject temp = InstantiateDecor(item, null);
            if (temp == null)
                return new Bounds(Vector3.zero, Vector3.one);

            temp.transform.position = Vector3.zero;

            Bounds b;
            SpriteRenderer sr = temp.GetComponentInChildren<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
                b = sr.bounds;
            else
                b = new Bounds(temp.transform.position, Vector3.one * 0.1f);

            Collider2D[] cols = temp.GetComponentsInChildren<Collider2D>(true);
            for (int i = 0; i < cols.Length; i++)
            {
                Collider2D col = cols[i];
                if (col == null || !col.enabled)
                    continue;
                if (col.GetComponent<DecorPlaceSurface>() != null)
                    continue;
                b.Encapsulate(col.bounds);
            }

            Bounds local = new Bounds(b.center - temp.transform.position, b.size);
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(temp);
            else
                UnityEngine.Object.DestroyImmediate(temp);
            return local;
        }
    }
}
