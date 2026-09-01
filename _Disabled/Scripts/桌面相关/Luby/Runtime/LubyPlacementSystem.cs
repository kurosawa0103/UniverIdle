using System.Collections.Generic;
using DesktopPet;
using DesktopPet.Decor;
using DesktopPet.Inventory;
using DesktopPet.Save;
using Sirenix.OdinInspector;
using UnityEngine;

namespace DesktopPet.Luby
{
    /// <summary>
    /// Luby 仅贴地放置：仓库放置 / 右键收回。
    /// 桌上已出场的 Luby <b>不可</b>长按或拖拽挪位。
    /// </summary>
    public sealed class LubyPlacementSystem : MonoBehaviour
    {
        [Title("Luby 放置")]
        [SerializeField] private LubyWorld lubyWorld;
        [SerializeField] private float ghostAlpha = 0.55f;
        [Tooltip("右键点选相对精灵包围盒的外扩")]
        [SerializeField] private float pickEdgePad = 0.08f;

        private DecorWorld _decorWorld;

        private bool _holding;
        private bool _placeArmed;
        private bool _candidateValid;
        private Vector3 _snapPos;
        private LubyInstanceData _heldData;
        private GameObject _ghost;
        private SpriteRenderer[] _ghostRenderers;
        private LubyInstanceComponent _pendingReturnTarget;

        public bool IsHolding => _holding;

        /// <summary>鼠标下是否有桌上 Luby（Bounds 点选，与右键菜单同源）。</summary>
        public LubyInstanceComponent TryPickUnderCursor() => FindLubyByBounds(DeskPointer.WorldOnDeskPlane());

        private void Awake()
        {
            ResolveRefs();
            DesktopPetServices.RegisterLubyPlacement(this);
        }

        private void OnDestroy()
        {
            DesktopPetServices.UnregisterLubyPlacement(this);
            CleanupHold(destroyGhost: true);
        }

        private void ResolveRefs()
        {
            if (lubyWorld == null)
                lubyWorld = GetComponent<LubyWorld>() ?? DesktopPetServices.LubyWorld;
            if (_decorWorld == null)
                _decorWorld = DesktopPetServices.DecorWorld;
        }

        private void Update()
        {
            if (_holding)
                TickWarehouseHold();
        }

        internal DeskIdlePointerInput.Faction BuildIdleFaction()
        {
            return new DeskIdlePointerInput.Faction
            {
                HasPending = _pendingReturnTarget != null,
                TrySelectOwn = TrySelectLubyPending,
                IsOtherUnderCursor = IsCursorOverPlacedDecor,
                OnSelectedOwn = ShowReturnToWarehouseButton,
                ClearPending = ClearPendingReturnTarget
            };
        }

        private bool TrySelectLubyPending()
        {
            LubyInstanceComponent hit = TryPickUnderCursor();
            if (hit == null)
                return false;
            _pendingReturnTarget = hit;
            return true;
        }

        /// <summary>仓库「放置」：从仓库取出一只，跟鼠标贴地放下。</summary>
        public bool TryBeginFromWarehouse(LubyInstanceData data)
        {
            if (_holding || data == null)
                return false;

            if (lubyWorld == null)
                return false;

            if (DesktopPetServices.IsAnyPlacementHolding())
                return false;

            if (!lubyWorld.CanSpawnOnDesk)
                return false;

            if (data.IsOnAdventureTrip)
                return false;

            if (!lubyWorld.TryTakeFromWarehouse(data.instanceId, out LubyInstanceData taken) || taken == null)
                return false;

            DesktopPetServices.InventoryUi?.HideAllDeskOverlays();
            ClearPendingReturnTarget();

            _heldData = taken;
            _placeArmed = false;
            _holding = true;
            CreateGhost(taken);
            DesktopPetServices.CloseHub();
            return true;
        }

        private void TickWarehouseHold()
        {
            Vector2 cursor = DeskPointer.WorldOnDeskPlane();
            EvaluateCandidate(cursor);
            UpdateGhostVisual();
            _candidateValid = TryMeasureHeldFootprint(out Bounds foot) && !OverlapsDesk(foot);
            SetGhostColor(_candidateValid);
            TickWarehouseConfirm();
        }

        private void ShowReturnToWarehouseButton()
        {
            Transform anchor = _pendingReturnTarget != null ? _pendingReturnTarget.transform : null;
            if (anchor == null || _pendingReturnTarget == null)
                return;

            InventoryUIController ui = DesktopPetServices.InventoryUi;
            if (ui == null)
                return;

            if (!ui.IsLubyDeskContextMenuAvailable)
            {
                Debug.LogError(
                    "[LubyPlacement] 未绑定 LubyDeskContextMenu。请改 MainCanvas.prefab 后「应用主面板」。");
                return;
            }

            ui.ShowLubyDeskContextMenu(anchor, _pendingReturnTarget, OnReturnDropZoneClicked);
        }

        private void OnReturnDropZoneClicked()
        {
            if (_pendingReturnTarget != null && lubyWorld != null)
            {
                LubyInstanceComponent target = _pendingReturnTarget;
                ClearPendingReturnTarget();
                DesktopPetServices.InventoryUi?.HideAllDeskOverlays();
                if (lubyWorld.TryReturnDeskToWarehouse(target))
                    DesktopPetSaveMgr.PersistActive();
                DesktopPetServices.HubUi?.RefreshChrome();
            }
        }

        private void ClearPendingReturnTarget()
        {
            _pendingReturnTarget = null;
        }

        private void TickWarehouseConfirm()
        {
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                CancelHold();
                return;
            }

            if (!_placeArmed)
            {
                if (!Input.GetMouseButton(0))
                    _placeArmed = true;
                return;
            }

            if (!Input.GetMouseButtonDown(0))
                return;

            if (TransparentGameWindow.ShouldBlockWorldPointer())
                return;

            if (_candidateValid)
                ConfirmWarehousePlace();
        }

        private void EvaluateCandidate(Vector2 cursor)
        {
            if (lubyWorld == null || _ghost == null)
                return;

            if (!TryEstimateHeldPivotLift(out float lift))
                return;

            float x = cursor.x;
            lubyWorld.ClampToGround(ref x, out float y);
            _snapPos = new Vector3(x, y + lift, 0f);
        }

        private bool OverlapsDesk(Bounds candidate)
        {
            if (_decorWorld != null && _decorWorld.OverlapsOthers(candidate, null))
                return true;
            return OverlapsOtherLubies(candidate);
        }

        private bool OverlapsOtherLubies(Bounds candidate)
        {
            if (lubyWorld == null)
                return false;

            IReadOnlyList<LubyInstanceComponent> list = lubyWorld.Instances;
            for (int i = 0; i < list.Count; i++)
            {
                LubyInstanceComponent other = list[i];
                if (other == null || !other.gameObject.activeInHierarchy)
                    continue;
                other.EnsureWorldPickBounds();
                if (DecorWorld.BoundsOverlap2D(candidate, other.PickBounds))
                    return true;
            }

            return false;
        }

        private void ConfirmWarehousePlace()
        {
            if (lubyWorld == null || _heldData == null)
            {
                CancelHold();
                return;
            }

            _heldData.x = _snapPos.x;
            _heldData.y = _snapPos.y;
            LubyInstanceData data = _heldData;
            LubyInstanceComponent spawned = lubyWorld.Spawn(data);
            CleanupHold(destroyGhost: true);

            if (spawned == null)
            {
                Debug.LogError("[LubyPlacement] 仓库放置 Spawn 失败，退回仓库");
                lubyWorld.AddToWarehouse(data);
                DesktopPetSaveMgr.PersistActive();
                DesktopPetServices.InventoryUi?.Open();
                return;
            }

            DesktopPetSaveMgr.PersistActive();
        }

        public void CancelHold()
        {
            if (!_holding)
                return;

            if (lubyWorld != null && _heldData != null)
                lubyWorld.AddToWarehouse(_heldData);

            CleanupHold(destroyGhost: true);
            DesktopPetSaveMgr.PersistActive();
            DesktopPetServices.InventoryUi?.Open();
        }

        private void CleanupHold(bool destroyGhost)
        {
            DesktopPetServices.InventoryUi?.HideAllDeskOverlays();
            ClearPendingReturnTarget();

            if (destroyGhost && _ghost != null)
            {
                Destroy(_ghost);
                _ghost = null;
            }

            _ghostRenderers = null;
            _holding = false;
            _placeArmed = false;
            _candidateValid = false;
            _heldData = null;
        }

        private void CreateGhost(LubyInstanceData data)
        {
            if (_ghost != null)
                Destroy(_ghost);

            if (lubyWorld == null || data == null)
                return;

            LubyTemplateDefinition template = lubyWorld.Catalog?.FindTemplateById(data.templateId);
            GameObject prefab = template != null
                ? template.ResolveSpawnPrefab(data.appearanceKey)
                : null;
            if (prefab == null)
                return;

            _ghost = Instantiate(prefab);
            _ghost.name = "LubyGhost";
            DecorHoldVisuals.StripForGhost(_ghost);

            _ghostRenderers = _ghost.GetComponentsInChildren<SpriteRenderer>(true);
            SetGhostColor(true);
        }

        private void UpdateGhostVisual()
        {
            if (_ghost == null)
                return;
            _ghost.transform.position = _snapPos;
            float s = _heldData != null && _heldData.scale > 0.01f ? _heldData.scale : 1f;
            _ghost.transform.localScale = Vector3.one * s;
        }

        private bool TryMeasureHeldFootprint(out Bounds footprint)
        {
            footprint = default;
            if (_ghost == null)
                return false;

            if (_ghostRenderers != null && _ghostRenderers.Length > 0)
            {
                Bounds b = _ghostRenderers[0].bounds;
                for (int i = 1; i < _ghostRenderers.Length; i++)
                {
                    if (_ghostRenderers[i] != null)
                        b.Encapsulate(_ghostRenderers[i].bounds);
                }

                footprint = b;
                return true;
            }

            Collider2D col = _ghost.GetComponentInChildren<Collider2D>();
            if (col != null && col.enabled)
            {
                footprint = col.bounds;
                return true;
            }

            return false;
        }

        private void SetGhostColor(bool valid)
        {
            if (_ghostRenderers == null)
                return;
            DecorHoldVisuals.ApplyPlacementTint(_ghostRenderers, valid, ghostAlpha);
        }

        private LubyInstanceComponent FindLubyByBounds(Vector2 worldPoint)
        {
            if (lubyWorld == null)
                return null;

            LubyInstanceComponent best = null;
            float bestY = float.MinValue;
            IReadOnlyList<LubyInstanceComponent> list = lubyWorld.Instances;
            float pad = pickEdgePad;
            for (int i = 0; i < list.Count; i++)
            {
                LubyInstanceComponent luby = list[i];
                if (luby == null || !luby.gameObject.activeInHierarchy)
                    continue;
                if (DesktopPetServices.LubyAdventure?.IsLubyInteractionLocked(luby) == true)
                    continue;

                luby.EnsureWorldPickBounds();
                Bounds b = luby.PickBounds;
                if (worldPoint.x < b.min.x - pad || worldPoint.x > b.max.x + pad
                    || worldPoint.y < b.min.y - pad || worldPoint.y > b.max.y + pad)
                    continue;

                float y = luby.transform.position.y;
                if (y >= bestY)
                {
                    bestY = y;
                    best = luby;
                }
            }

            return best;
        }

        private bool IsCursorOverPlacedDecor()
        {
            if (_decorWorld == null)
                return false;
            return _decorWorld.FindPickupUnderPoint(DeskPointer.WorldOnDeskPlane()) != null;
        }

        private bool TryEstimateHeldPivotLift(out float lift)
        {
            lift = 0f;
            if (_ghost == null)
                return false;

            Collider2D col = _ghost.GetComponentInChildren<Collider2D>();
            if (col != null && col.enabled)
            {
                lift = Mathf.Max(0.01f, _ghost.transform.position.y - col.bounds.min.y);
                return true;
            }

            SpriteRenderer sr = _ghost.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                lift = Mathf.Max(0.01f, _ghost.transform.position.y - sr.bounds.min.y);
                return true;
            }

            return false;
        }
    }
}
