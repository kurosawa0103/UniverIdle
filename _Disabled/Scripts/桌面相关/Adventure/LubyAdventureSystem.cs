using System;
using System.Collections.Generic;
using DesktopPet.AI;
using DesktopPet.Background;
using DesktopPet.Decor;
using DesktopPet.Luby;
using DesktopPet.Save;
using DesktopPet.Shop;
using Sirenix.OdinInspector;
using UnityEngine;

namespace DesktopPet.Adventure
{
    /// <summary>
    /// Luby 探险：seek_adventure_board → 看板前互动 → 标记离桌 → 计时结束后回桌。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LubyAdventureSystem : MonoBehaviour, ILubyActivity
    {
        public const string AdventureBoardItemId = "decor_adventure_board";

        private enum Phase
        {
            Approaching,
            Performing
        }

        private enum TransitKind
        {
            Exit,
            Enter
        }

        private sealed class Session
        {
            public LubyInstanceComponent Luby;
            public PetAgent Agent;
            public DecorInteractable Board;
            public Phase Phase;
            public float EndAt;
        }

        private sealed class TransitSession
        {
            public LubyInstanceComponent Luby;
            public TransitKind Kind;
            public float TargetX;
            public AdventureRegionDefinition Region;
            public AdventureEventDefinition Event;
            public string BackgroundId;
            public float DurationSeconds;
            public int SettleGold;
            public bool SoftCapped;
        }

        [Title("Luby 探险")]
        [LabelText("看板互动优先级")]
        [SerializeField]
        private int boardInteractPriority = 12;

        [LabelText("强制切入看板互动")]
        [SerializeField]
        private bool forceBoardInteract = true;

        [LabelText("走近超时（秒）")]
        [MinValue(1f)]
        [SerializeField]
        private float maxApproachSeconds = 10f;

        [LabelText("看板互动时长（秒）")]
        [MinValue(1f)]
        [SerializeField]
        private float boardInteractSeconds = 4f;

        private const float FallbackAdventureDurationSeconds = 30f;

        [LabelText("事件目录")]
        [SerializeField]
        private AdventureEventCatalog eventCatalog;

        [LabelText("出屏/回屏走路速度")]
        [MinValue(0.3f)]
        [SerializeField]
        private float transitWalkSpeed = 1.35f;

        [LabelText("屏外偏移（世界单位）")]
        [MinValue(0.5f)]
        [SerializeField]
        private float offScreenMargin = 1.8f;

        [LabelText("出屏/回屏到达阈值")]
        [MinValue(0.05f)]
        [SerializeField]
        private float transitArriveThreshold = 0.28f;

        private readonly Dictionary<string, float> _boardCooldownUntil = new Dictionary<string, float>();
        private readonly List<Session> _sessions = new List<Session>(4);
        private readonly List<TransitSession> _transitSessions = new List<TransitSession>(2);
        private readonly List<DecorInteractable> _eligibleScratch = new List<DecorInteractable>(4);
        private readonly HashSet<string> _pendingEnterIds = new HashSet<string>();
        private DecorWorld _decorWorld;

        private void Awake()
        {
            DesktopPetServices.RegisterLubyAdventure(this);
            DesktopPetServices.RegisterLubyActivity(this);
            EnsureDecorWorld();
            EnsureCatalog();
        }

        private void Start()
        {
            SubscribeBackgroundChanged();
        }

        private void OnDestroy()
        {
            UnsubscribeBackgroundChanged();
            EndAll();
            DesktopPetServices.UnregisterLubyAdventure(this);
            DesktopPetServices.UnregisterLubyActivity(this);
        }

        private void SubscribeBackgroundChanged()
        {
            BackgroundSystem bg = BackgroundSystem.Instance;
            if (bg != null)
                bg.BackgroundChanged += OnBackgroundChanged;
        }

        private void UnsubscribeBackgroundChanged()
        {
            BackgroundSystem bg = BackgroundSystem.Instance;
            if (bg != null)
                bg.BackgroundChanged -= OnBackgroundChanged;
        }

        /// <summary>切背景：取消看板走近；出/回屏途中立刻收尾（出屏按出发背景入库）。</summary>
        private void OnBackgroundChanged(string _)
        {
            for (int i = _sessions.Count - 1; i >= 0; i--)
            {
                FinishSession(_sessions[i], depart: false);
                _sessions.RemoveAt(i);
            }

            for (int i = _transitSessions.Count - 1; i >= 0; i--)
            {
                TransitSession t = _transitSessions[i];
                _transitSessions.RemoveAt(i);
                FinishTransit(t);
            }
        }

        private void Update()
        {
            bool holding = DesktopPetServices.IsAnyPlacementHolding();
            TickSessions(holding);
            TickTransitSessions();
            TickAdventureReturns();
        }

        /// <summary>出屏/回屏途中：不可点击、不可右键收回。</summary>
        public bool IsLubyInteractionLocked(LubyInstanceComponent luby)
        {
            return FindTransit(luby) != null;
        }

        public bool HasAnyEligibleBoard(LubyInstanceComponent luby)
        {
            if (!CanLubyAdventure(luby))
                return false;
            CollectEligibleBoards(luby, _eligibleScratch);
            return _eligibleScratch.Count > 0;
        }

        public bool TryClaimBoard(LubyInstanceComponent luby, out DecorInteractable board)
        {
            board = null;
            if (!CanLubyAdventure(luby))
                return false;

            CollectEligibleBoards(luby, _eligibleScratch);
            if (_eligibleScratch.Count == 0)
                return false;

            DecorInteractable pick = _eligibleScratch[UnityEngine.Random.Range(0, _eligibleScratch.Count)];
            if (pick == null || !pick.TryOccupy())
                return false;

            PetAgent agent = luby.Agent;
            _sessions.Add(new Session
            {
                Luby = luby,
                Agent = agent,
                Board = pick,
                Phase = Phase.Approaching,
                EndAt = Time.time + maxApproachSeconds
            });
            board = pick;
            return true;
        }

        public bool TryGetApproachingBoard(LubyInstanceComponent luby, out DecorInteractable board)
        {
            board = null;
            Session s = FindSession(luby);
            if (s == null || s.Phase != Phase.Approaching || s.Board == null)
                return false;
            board = s.Board;
            return true;
        }

        public bool TryStartBoardInteract(LubyInstanceComponent luby)
        {
            Session s = FindSession(luby);
            if (s == null || s.Phase != Phase.Approaching || s.Board == null)
                return false;

            PlacedDecor placed = s.Board.GetComponent<PlacedDecor>();
            ShopItemDefinition def = placed != null ? placed.Definition : null;
            string personalityId = luby.ResolvePersonalityId();
            string performanceId = def != null
                ? def.ResolveLubyPerformanceBehaviorId(personalityId)
                : null;
            if (string.IsNullOrEmpty(performanceId))
            {
                FinishSession(s, depart: false);
                _sessions.Remove(s);
                return false;
            }

            s.Phase = Phase.Performing;
            s.EndAt = Time.time + boardInteractSeconds;

            PetAgent agent = s.Agent != null ? s.Agent : luby.Agent;
            s.Agent = agent;
            agent?.Brain?.RequestBehavior(performanceId, boardInteractPriority, forceBoardInteract);
            return true;
        }

        public void CancelApproach(LubyInstanceComponent luby)
        {
            Session s = FindSession(luby);
            if (s == null || s.Phase != Phase.Approaching)
                return;
            FinishSession(s, depart: false);
            _sessions.Remove(s);
        }

        public void EndAllForLuby(LubyInstanceComponent luby)
        {
            LubyActivityEndUtility.EndAllForLuby(
                _sessions,
                luby,
                getSessionLuby: s => s.Luby,
                endAction: s =>
                {
                    FinishSession(s, depart: false);
                });
        }

        public bool IsLubyBusy(LubyInstanceComponent luby)
        {
            return FindSession(luby) != null || FindTransit(luby) != null;
        }

        /// <summary>看板面板：进行中 / 上回摘要。</summary>
        public string BuildBoardStatusText()
        {
            EnsureCatalog();
            LubyWorld world = DesktopPetServices.LubyWorld;
            var sb = new System.Text.StringBuilder(256);

            LubyInstanceData away = FindAwayData(world);
            TransitSession exitTransit = FindTransitKind(TransitKind.Exit);
            TransitSession enterTransit = FindTransitKind(TransitKind.Enter);
            if (exitTransit?.Luby?.Data != null)
            {
                string name = LubyDisplayNames.ResolvePetName(exitTransit.Luby.Data, world?.Catalog);
                sb.Append(name).Append(" 正在走出屏幕…\n");
            }
            else if (enterTransit?.Luby?.Data != null)
            {
                string name = LubyDisplayNames.ResolvePetName(enterTransit.Luby.Data, world?.Catalog);
                sb.Append(name).Append(" 正在从屏幕外走回来…\n");
            }
            else if (away != null && away.IsOnAdventureTrip)
            {
                string name = LubyDisplayNames.ResolvePetName(away, world != null ? world.Catalog : null);
                string activeBg = LubyWorld.ResolveActiveBackgroundId();
                if (!away.IsOnAdventureTripForBackground(activeBg))
                {
                    string sceneName = ResolveBackgroundDisplayName(away.adventureBackgroundId);
                    sb.Append(name).Append(" 在「").Append(sceneName).Append("」外面探险中。\n");
                    sb.Append("切回该场景才会走回来。\n");
                }
                else if (away.IsAwayOnAdventure)
                {
                    double left = Math.Max(0d, away.adventureEndsAtUtc - LubyInstanceData.UtcNowUnixSeconds());
                    int sec = Mathf.CeilToInt((float)left);
                    sb.Append(name).Append(" 出门了，大约还要 ").Append(sec).Append(" 秒。\n");
                }
                else
                {
                    sb.Append(name).Append(" 准备从外面走回来…\n");
                }

                if (away.IsOnAdventureTripForBackground(activeBg)
                    || !string.IsNullOrEmpty(away.adventureRegionId))
                {
                    AdventureEventDefinition ongoing = eventCatalog != null
                        ? eventCatalog.FindById(away.adventureEventId)
                        : null;
                    string regionName = AdventureEventResolver.ResolveRegionDisplayName(
                        eventCatalog, away.adventureRegionId);
                    if (!string.IsNullOrEmpty(regionName))
                        sb.Append("去了 ").Append(regionName);
                    if (ongoing != null && !string.IsNullOrEmpty(ongoing.title))
                    {
                        if (!string.IsNullOrEmpty(regionName))
                            sb.Append(" · ");
                        sb.Append("本趟：").Append(ongoing.title);
                    }

                    if (!string.IsNullOrEmpty(regionName)
                        || (ongoing != null && !string.IsNullOrEmpty(ongoing.title)))
                        sb.Append('\n');
                }
            }
            else
            {
                sb.Append("现在没人在外面。\n");
            }

            LubyInstanceData last = FindLatestSettled(world);
            sb.Append('\n');
            if (last != null && !string.IsNullOrEmpty(last.lastAdventureEventId))
            {
                string who = LubyDisplayNames.ResolvePetName(last, world != null ? world.Catalog : null);
                string title = string.IsNullOrEmpty(last.lastAdventureTitle) ? "探险" : last.lastAdventureTitle;
                string regionName = AdventureEventResolver.ResolveRegionDisplayName(
                    eventCatalog, last.lastAdventureRegionId);
                sb.Append("上回：").Append(who);
                if (!string.IsNullOrEmpty(regionName))
                    sb.Append(" · ").Append(regionName);
                sb.Append(" · ").Append(title);
                sb.Append("  +").Append(Mathf.Max(0, last.lastAdventureGold)).Append(" 金");
            }
            else
            {
                sb.Append("上回：还没有探险记录。");
            }

            sb.Append("\n\nLuby 自己会去看看板出门。同时只能一只。");
            sb.Append("\n探险绑定出发场景：切走后要切回来才会走回。");
            int cap = eventCatalog != null ? eventCatalog.dailySoftCapTrips : 6;
            sb.Append("\n每日每只约 ").Append(cap).Append(" 趟后收益变少。");
            return sb.ToString();
        }

        private static LubyInstanceData FindAwayData(LubyWorld world)
        {
            if (world == null)
                return null;
            IReadOnlyList<LubyInstanceData> warehouse = world.Warehouse;
            if (warehouse == null)
                return null;
            for (int i = 0; i < warehouse.Count; i++)
            {
                LubyInstanceData d = warehouse[i];
                if (d != null && d.IsOnAdventureTrip)
                    return d;
            }

            return null;
        }

        private static LubyInstanceData FindLatestSettled(LubyWorld world)
        {
            if (world == null)
                return null;

            LubyInstanceData best = null;
            double bestAt = 0d;
            ConsiderList(world.Warehouse, ref best, ref bestAt);
            IReadOnlyList<LubyInstanceComponent> desk = world.Instances;
            if (desk != null)
            {
                for (int i = 0; i < desk.Count; i++)
                {
                    LubyInstanceData d = desk[i] != null ? desk[i].Data : null;
                    if (d == null || d.lastAdventureEndedAtUtc <= bestAt)
                        continue;
                    best = d;
                    bestAt = d.lastAdventureEndedAtUtc;
                }
            }

            return best;
        }

        private static void ConsiderList(
            IReadOnlyList<LubyInstanceData> list,
            ref LubyInstanceData best,
            ref double bestAt)
        {
            if (list == null)
                return;
            for (int i = 0; i < list.Count; i++)
            {
                LubyInstanceData d = list[i];
                if (d == null || d.lastAdventureEndedAtUtc <= bestAt)
                    continue;
                best = d;
                bestAt = d.lastAdventureEndedAtUtc;
            }
        }

        private bool CanLubyAdventure(LubyInstanceComponent luby)
        {
            if (luby == null || !luby.isActiveAndEnabled || luby.Data == null)
                return false;
            if (luby.Data.IsOnAdventureTrip)
                return false;
            if (_transitSessions.Count > 0)
                return false;
            // 同时只允许一只：已有离桌探险，或已有走近/看板互动会话。
            LubyWorld world = DesktopPetServices.LubyWorld;
            if (world != null && world.HasAnyAwayOnAdventure)
                return false;
            if (_sessions.Count > 0 && FindSession(luby) == null)
                return false;
            if (DesktopPetServices.IsAnyPlacementHolding() || DesktopPetServices.IsHubOpen())
                return false;
            if (DesktopPetServices.IsLubyBlockedForWorldActivity(luby))
                return false;
            return true;
        }

        private void CollectEligibleBoards(LubyInstanceComponent luby, List<DecorInteractable> dst)
        {
            dst.Clear();
            EnsureDecorWorld();
            if (_decorWorld == null)
                return;

            IReadOnlyList<DecorInteractable> decors = _decorWorld.Interactables;
            for (int i = 0; i < decors.Count; i++)
            {
                DecorInteractable d = decors[i];
                if (d == null || !d.isActiveAndEnabled)
                    continue;
                if (!IsAdventureBoardEligible(luby, d))
                    continue;
                dst.Add(d);
            }
        }

        private bool IsAdventureBoardEligible(LubyInstanceComponent luby, DecorInteractable decor)
        {
            if (luby == null || decor == null)
                return false;
            PlacedDecor placed = decor.GetComponent<PlacedDecor>();
            ShopItemDefinition def = placed != null ? placed.Definition : null;
            if (def == null || def.itemId != AdventureBoardItemId)
                return false;
            if (!def.HasLubyInteractGate)
                return false;
            if (!def.MatchesLubyInteractGate(luby))
                return false;
            if (string.IsNullOrEmpty(def.ResolveLubyPerformanceBehaviorId(luby.ResolvePersonalityId())))
                return false;
            if (!decor.HasFreeSlot)
                return false;
            if (IsOnBoardCooldown(decor))
                return false;
            return true;
        }

        private void TickSessions(bool holding)
        {
            for (int i = _sessions.Count - 1; i >= 0; i--)
            {
                Session s = _sessions[i];
                if (s.Luby == null || s.Board == null)
                {
                    FinishSession(s, depart: false);
                    _sessions.RemoveAt(i);
                    continue;
                }

                if (!s.Luby.isActiveAndEnabled || !s.Board.isActiveAndEnabled)
                {
                    FinishSession(s, depart: false);
                    _sessions.RemoveAt(i);
                    continue;
                }

                if (holding || DesktopPetServices.IsLubyBusyWithOtherActivities(s.Luby, this))
                {
                    FinishSession(s, depart: false);
                    _sessions.RemoveAt(i);
                    continue;
                }

                if (Time.time < s.EndAt)
                {
                    if (s.Phase == Phase.Performing)
                    {
                        Vector2 foot = s.Luby.transform.position;
                        if (!s.Board.IsWithinRadius(foot))
                        {
                            FinishSession(s, depart: false);
                            _sessions.RemoveAt(i);
                        }
                    }

                    continue;
                }

                bool depart = s.Phase == Phase.Performing;
                _sessions.RemoveAt(i);
                FinishSession(s, depart);
            }
        }

        private void FinishSession(Session s, bool depart)
        {
            if (s == null)
                return;

            if (s.Board != null)
            {
                s.Board.ReleaseOccupy();
                MarkBoardCooldown(s.Board);
            }

            if (!depart || s.Luby == null)
            {
                s.Agent?.Brain?.InterruptAndReselect(boardInteractPriority);
                return;
            }

            BeginExitTransit(s.Luby);
        }

        private void BeginExitTransit(LubyInstanceComponent luby)
        {
            if (luby?.Data == null || DesktopPetServices.LubyWorld == null)
                return;

            EnsureCatalog();
            AdventureEventResolver.EnsureDayCounters(luby.Data);
            AdventureEventResolver.TripPick trip = AdventureEventResolver.PickTrip(eventCatalog, luby);
            AdventureEventDefinition evt = trip.Event;
            float duration = evt != null
                ? evt.ResolveDurationSeconds()
                : Mathf.Clamp(FallbackAdventureDurationSeconds, 5f, 600f);

            DesktopPetServices.EndAllLubyActivities(luby);
            luby.SyncPositionToData();

            float exitX = ResolveOffScreenX(luby.transform.position.x);
            luby.Data.adventureExitX = exitX;

            luby.Agent?.Brain?.InterruptAndReselect(boardInteractPriority);

            StartTransit(new TransitSession
            {
                Luby = luby,
                Kind = TransitKind.Exit,
                Region = trip.Region,
                Event = evt,
                BackgroundId = LubyWorld.ResolveActiveBackgroundId(),
                DurationSeconds = duration
            }, exitX);
        }

        private void StartTransit(TransitSession session, float targetX)
        {
            if (session?.Luby == null)
                return;

            session.TargetX = targetX;
            SetTransitLocked(session.Luby, locked: true);

            PetLocomotion loco = GetLocomotion(session.Luby);
            if (loco == null)
            {
                FinishTransit(session);
                return;
            }

            loco.SetAllowOutOfBounds(true);
            loco.SetMoveTarget(targetX, transitWalkSpeed);
            PlayTransitWalk(session.Luby);
            _transitSessions.Add(session);
        }

        private void TickTransitSessions()
        {
            for (int i = _transitSessions.Count - 1; i >= 0; i--)
            {
                TransitSession t = _transitSessions[i];
                if (t.Luby == null || !t.Luby.isActiveAndEnabled)
                {
                    _transitSessions.RemoveAt(i);
                    continue;
                }

                PetLocomotion loco = GetLocomotion(t.Luby);
                if (loco == null || !loco.HasReachedTarget(transitArriveThreshold))
                    continue;

                _transitSessions.RemoveAt(i);
                FinishTransit(t);
            }
        }

        private void FinishTransit(TransitSession t)
        {
            if (t == null)
                return;
            if (t.Kind == TransitKind.Exit)
                CompleteExitTransit(t.Luby, t.Region, t.Event, t.DurationSeconds, t.BackgroundId);
            else
                CompleteEnterTransit(t.Luby, t.Region, t.Event, t.SettleGold, t.SoftCapped);
        }

        private void CompleteExitTransit(
            LubyInstanceComponent luby,
            AdventureRegionDefinition region,
            AdventureEventDefinition evt,
            float durationSeconds,
            string backgroundId)
        {
            LubyWorld world = DesktopPetServices.LubyWorld;
            if (luby?.Data == null || world == null)
                return;

            if (string.IsNullOrEmpty(backgroundId))
                backgroundId = LubyWorld.ResolveActiveBackgroundId();

            luby.SyncPositionToData();
            luby.Data.adventureEventId = evt != null ? evt.eventId : string.Empty;
            luby.Data.adventureRegionId = region != null ? region.regionId : string.Empty;
            luby.Data.adventureBackgroundId = backgroundId;
            luby.Data.adventureEndsAtUtc =
                LubyInstanceData.UtcNowUnixSeconds() + durationSeconds;

            string regionName = AdventureEventResolver.ResolveRegionDisplayName(eventCatalog, luby.Data.adventureRegionId);
            LubyJournalService.RecordAdventureGo(luby.Data, regionName, persist: false);
            GetLocomotion(luby)?.Stop();

            if (!world.TryReturnDeskToWarehouse(luby))
            {
                ClearAdventureTripFields(luby.Data);
                RestoreTransitLocomotion(luby);
                return;
            }

            DesktopPetSaveMgr.PersistActive();
        }

        private void CompleteEnterTransit(
            LubyInstanceComponent luby,
            AdventureRegionDefinition region,
            AdventureEventDefinition evt,
            int gold,
            bool softCapped)
        {
            if (luby?.Data == null)
                return;

            RestoreTransitLocomotion(luby);
            luby.SyncPositionToData();
            luby.Data.adventureExitX = 0f;
            string regionName = region != null && !string.IsNullOrEmpty(region.displayName)
                ? region.displayName
                : (region != null ? region.regionId : string.Empty);
            LubyJournalService.RecordAdventureBack(luby.Data, evt, gold, softCapped, regionName, persist: false);
            DesktopPetSaveMgr.PersistActive();
        }

        private void TickAdventureReturns()
        {
            LubyWorld world = DesktopPetServices.LubyWorld;
            if (world == null || world.Warehouse == null)
                return;

            if (FindTransitKind(TransitKind.Enter) != null)
                return;

            EnsureCatalog();
            string activeBg = LubyWorld.ResolveActiveBackgroundId();
            IReadOnlyList<LubyInstanceData> warehouse = world.Warehouse;
            for (int i = warehouse.Count - 1; i >= 0; i--)
            {
                LubyInstanceData data = warehouse[i];
                if (data == null || data.adventureEndsAtUtc <= 0d || data.IsAwayOnAdventure)
                    continue;
                if (!data.IsOnAdventureTripForBackground(activeBg))
                    continue;
                if (_pendingEnterIds.Contains(data.instanceId))
                    continue;

                TryBeginEnterTransit(world, data);
                break;
            }
        }

        private void TryBeginEnterTransit(LubyWorld world, LubyInstanceData data)
        {
            if (world == null || data == null)
                return;

            world.EvictDeskLubiesToFitCapacity();
            if (world.Count >= world.DeskCapacity)
                return;

            string regionId = data.adventureRegionId;
            string instanceId = data.instanceId;
            _pendingEnterIds.Add(instanceId);

            if (!world.TryTakeFromWarehouse(instanceId, out LubyInstanceData taken) || taken == null)
            {
                _pendingEnterIds.Remove(instanceId);
                return;
            }

            float spawnX = taken.adventureExitX;
            if (Mathf.Approximately(spawnX, 0f))
                spawnX = ResolveOffScreenX(taken.x);

            float returnX = taken.x;
            LubyInstanceComponent back = world.Spawn(taken, spawnX, clampSpawnX: false);
            if (back == null)
            {
                world.AddToWarehouse(taken);
                _pendingEnterIds.Remove(instanceId);
                return;
            }

            AdventureEventResolver.Settle(
                eventCatalog, back.Data, out AdventureEventDefinition evt, out int gold, out bool softCap);
            ApplySettlementFields(back.Data, evt, gold, regionId);
            _pendingEnterIds.Remove(instanceId);

            AdventureRegionDefinition region = eventCatalog?.FindRegionById(regionId);
            StartTransit(new TransitSession
            {
                Luby = back,
                Kind = TransitKind.Enter,
                Region = region,
                Event = evt,
                SettleGold = gold,
                SoftCapped = softCap
            }, returnX);
            DesktopPetSaveMgr.PersistActive();
        }

        private static void ApplySettlementFields(
            LubyInstanceData data,
            AdventureEventDefinition evt,
            int gold,
            string regionId)
        {
            if (data == null)
                return;
            data.lastAdventureEventId = evt != null ? evt.eventId : data.adventureEventId;
            data.lastAdventureRegionId = !string.IsNullOrEmpty(regionId)
                ? regionId
                : data.adventureRegionId;
            data.lastAdventureTitle = evt != null ? evt.title : "探险";
            data.lastAdventureGold = gold;
            data.lastAdventureEndedAtUtc = LubyInstanceData.UtcNowUnixSeconds();
            data.adventureEventId = string.Empty;
            data.adventureRegionId = string.Empty;
            data.adventureBackgroundId = string.Empty;
            data.adventureEndsAtUtc = 0d;
        }

        private static void ClearAdventureTripFields(LubyInstanceData data)
        {
            if (data == null)
                return;
            data.adventureEndsAtUtc = 0d;
            data.adventureEventId = string.Empty;
            data.adventureRegionId = string.Empty;
            data.adventureBackgroundId = string.Empty;
            data.adventureExitX = 0f;
        }

        private static string ResolveBackgroundDisplayName(string backgroundId)
        {
            if (string.IsNullOrEmpty(backgroundId))
                return "未知场景";
            BackgroundCatalog catalog = BackgroundSystem.Instance != null
                ? BackgroundSystem.Instance.Catalog
                : BackgroundCatalog.LoadDefault();
            BackgroundDefinition def = catalog != null ? catalog.FindById(backgroundId) : null;
            if (def != null && !string.IsNullOrEmpty(def.displayName))
                return def.displayName;
            return backgroundId;
        }

        private void EnsureCatalog()
        {
            if (eventCatalog == null)
                eventCatalog = AdventureEventCatalog.LoadDefault();
        }

        private Session FindSession(LubyInstanceComponent luby)
        {
            if (luby == null)
                return null;
            for (int i = 0; i < _sessions.Count; i++)
            {
                if (_sessions[i].Luby == luby)
                    return _sessions[i];
            }

            return null;
        }

        private void EnsureDecorWorld()
        {
            if (_decorWorld == null)
                _decorWorld = DesktopPetServices.DecorWorld;
        }

        private void EndAll()
        {
            for (int i = _transitSessions.Count - 1; i >= 0; i--)
            {
                TransitSession t = _transitSessions[i];
                if (t.Luby != null)
                    RestoreTransitLocomotion(t.Luby);
            }

            _transitSessions.Clear();
            _pendingEnterIds.Clear();

            for (int i = _sessions.Count - 1; i >= 0; i--)
                FinishSession(_sessions[i], depart: false);
            _sessions.Clear();
        }

        private static PetLocomotion GetLocomotion(LubyInstanceComponent luby)
        {
            if (luby == null)
                return null;
            return luby.Agent != null ? luby.Agent.Locomotion : luby.GetComponent<PetLocomotion>();
        }

        private static void SetTransitLocked(LubyInstanceComponent luby, bool locked)
        {
            if (luby == null)
                return;

            PetAgent agent = luby.Agent;
            if (agent?.Brain != null)
                agent.Brain.enabled = !locked;

            PetInteraction interaction = luby.GetComponent<PetInteraction>();
            if (interaction != null)
                interaction.enabled = !locked;

            Collider2D col = luby.GetComponent<Collider2D>();
            if (col != null)
                col.enabled = !locked;
        }

        private static void RestoreTransitLocomotion(LubyInstanceComponent luby)
        {
            PetLocomotion loco = GetLocomotion(luby);
            if (loco == null)
                return;

            loco.SetAllowOutOfBounds(false);
            loco.Stop();
            SetTransitLocked(luby, locked: false);
        }

        private static void PlayTransitWalk(LubyInstanceComponent luby)
        {
            PetAnimatorDriver driver = luby?.Agent != null
                ? luby.Agent.AnimatorDriver
                : luby?.GetComponent<PetAnimatorDriver>();
            driver?.SetTrigger("Walk");
            driver?.SetFloat("Speed", 1f);
        }

        private float ResolveOffScreenX(float fromX)
        {
            DesktopPetPlayfieldBounds playfield = DesktopPetPlayfieldBounds.Instance;
            float min = playfield != null && playfield.IsValid ? playfield.MinX : fromX - 10f;
            float max = playfield != null && playfield.IsValid ? playfield.MaxX : fromX + 10f;
            bool exitLeft = fromX <= (min + max) * 0.5f;
            return exitLeft ? min - offScreenMargin : max + offScreenMargin;
        }

        private TransitSession FindTransit(LubyInstanceComponent luby)
        {
            if (luby == null)
                return null;
            for (int i = 0; i < _transitSessions.Count; i++)
            {
                if (_transitSessions[i].Luby == luby)
                    return _transitSessions[i];
            }

            return null;
        }

        private TransitSession FindTransitKind(TransitKind kind)
        {
            for (int i = 0; i < _transitSessions.Count; i++)
            {
                if (_transitSessions[i].Kind == kind)
                    return _transitSessions[i];
            }

            return null;
        }

        private void MarkBoardCooldown(DecorInteractable decor)
        {
            if (decor == null || decor.cooldownSeconds <= 0f)
                return;
            _boardCooldownUntil[decor.CooldownKey] = Time.time + decor.cooldownSeconds;
        }

        private bool IsOnBoardCooldown(DecorInteractable decor)
        {
            if (decor == null)
                return false;
            return _boardCooldownUntil.TryGetValue(decor.CooldownKey, out float until) && Time.time < until;
        }
    }
}
