using System.Collections.Generic;
using DesktopPet.AI;
using DesktopPet.Decor;
using DesktopPet.Hub;
using Sirenix.OdinInspector;
using UnityEngine;

namespace DesktopPet.Luby
{
    /// <summary>配置驱动小剧场：走位到道具相对点 → 各播一段 requestOnly → 结束回常规 AI。</summary>
    [DisallowMultipleComponent]
    public sealed class LubyTheaterDirector : MonoBehaviour, ILubyActivity
    {
        private enum Phase
        {
            Seeking,
            Rejecting,
            Approaching,
            Performing
        }

        private sealed class CastMember
        {
            public string roleKey;
            public LubyInstanceComponent luby;
            public PetAgent agent;
            public PetBrain brain;
            public PetLocomotion loco;
            public string behaviorId;
            public float stageOffsetX;
            public float stageX;
            public LubyTheaterStageFacing stageFacing;
            public bool arrived;
            public bool brainWasEnabled = true;
        }

        private sealed class Session
        {
            public int id;
            public LubyTheaterEventDefinition evt;
            public readonly List<CastMember> cast = new List<CastMember>(4);
            public Phase phase;
            public float approachDeadline;
            public float endAt;
            public PlacedDecor stageProp;
            public bool requiresStageProp;
            public bool usesPeerMidpoint;
            public float stageAnchorX0;
            public CastMember seeker;
            public CastMember seekTarget;
        }

        public readonly struct TheaterSessionSnapshot
        {
            public readonly int DisplayIndex;
            public readonly int SessionId;
            public readonly string EventId;
            public readonly string PhaseLabel;
            public readonly float RemainingSeconds;
            public readonly int CastCount;
            public readonly List<string> CastLines;

            public TheaterSessionSnapshot(
                int displayIndex,
                int sessionId,
                string eventId,
                string phaseLabel,
                float remainingSeconds,
                int castCount,
                List<string> castLines)
            {
                DisplayIndex = displayIndex;
                SessionId = sessionId;
                EventId = eventId;
                PhaseLabel = phaseLabel;
                RemainingSeconds = remainingSeconds;
                CastCount = castCount;
                CastLines = castLines;
            }
        }

        [Title("小剧场")]
        [LabelText("事件目录")]
        [SerializeField]
        private LubyTheaterCatalog catalog;

        [LabelText("表演优先级")]
        [SerializeField]
        private int performancePriority = 14;

        [LabelText("扫描间隔")]
        [MinValue(0.2f)]
        [SerializeField]
        private float scanInterval = 2f;

        [LabelText("最大同时场次")]
        [MinValue(1)]
        [MaxValue(8)]
        [SerializeField]
        private int maxConcurrentSessions = 3;

        [LabelText("舞台位移拆场阈值")]
        [Tooltip("舞台道具锚点相对开场位置水平偏移超过该值则整场结束（不进冷却）")]
        [MinValue(0.05f)]
        [SerializeField]
        private float stageMoveEndDistance = 0.75f;

        private readonly List<Session> _sessions = new List<Session>(4);
        private readonly Dictionary<string, float> _eventCooldownUntil = new Dictionary<string, float>();
        private readonly List<LubyInstanceComponent> _castScratch = new List<LubyInstanceComponent>(8);
        private readonly List<CastMember> _resolveScratch = new List<CastMember>(8);
        private readonly List<CastMember> _bestCastScratch = new List<CastMember>(8);
        private readonly List<SlotNeed> _slotNeedsScratch = new List<SlotNeed>(8);
        private readonly List<LubyInstanceComponent> _intentFirstScratch = new List<LubyInstanceComponent>(8);
        private readonly Dictionary<LubyInstanceComponent, float> _socialIntentUntil =
            new Dictionary<LubyInstanceComponent, float>(8);
        private float _nextScanAt;
        private int _nextSessionId = 1;

        /// <summary>当前绑定的事件目录（编辑器调试页读取）。</summary>
        public LubyTheaterCatalog Catalog => catalog;

        /// <summary>是否有任意场次在进行。</summary>
        public bool HasActiveSession => _sessions.Count > 0;

        public int ActiveSessionCount => _sessions.Count;

        public int MaxConcurrentSessions => Mathf.Max(1, maxConcurrentSessions);

        /// <summary>清掉所有事件冷却（仅调试用）。</summary>
        public void ClearAllEventCooldowns() => _eventCooldownUntil.Clear();

        /// <summary>日常 AI 抽到「想社交」时打点；holdSeconds 内可被社交剧场选中。</summary>
        public void SignalSocialIntent(LubyInstanceComponent luby, float holdSeconds = 14f)
        {
            if (luby == null)
                return;
            _socialIntentUntil[luby] = Time.time + Mathf.Max(0.5f, holdSeconds);
        }

        public bool HasSocialIntent(LubyInstanceComponent luby)
        {
            if (luby == null)
                return false;
            if (!_socialIntentUntil.TryGetValue(luby, out float until))
                return false;
            if (Time.time < until)
                return true;
            _socialIntentUntil.Remove(luby);
            return false;
        }

        public void ClearSocialIntent(LubyInstanceComponent luby)
        {
            if (luby != null)
                _socialIntentUntil.Remove(luby);
        }

        /// <summary>桌上是否有人仍带着有效社交意图。</summary>
        public bool HasAnySocialIntentOnDesk()
        {
            PruneExpiredSocialIntents();
            return _socialIntentUntil.Count > 0;
        }

        /// <summary>评估一场能否开演，返回可读报告（不修改状态）。</summary>
        public bool TryEvaluateEvent(
            LubyTheaterEventDefinition evt,
            bool ignoreGates,
            bool ignoreCooldown,
            out string report)
        {
            return TryEvaluateEventInternal(
                evt,
                ignoreGates,
                ignoreCooldown,
                leaveResolvedCast: false,
                out report,
                out _);
        }

        /// <summary>调试强制开演：跳过 scanChance；ignoreGates / ignoreCooldown 控制门闸与冷却。</summary>
        public bool TryDebugStartEvent(
            LubyTheaterEventDefinition evt,
            bool ignoreGates,
            bool ignoreCooldown,
            out string error)
        {
            error = null;
            if (!TryEvaluateEventInternal(
                    evt,
                    ignoreGates,
                    ignoreCooldown,
                    leaveResolvedCast: true,
                    out string report,
                    out PlacedDecor stageProp))
            {
                error = report;
                return false;
            }

            Begin(evt, _resolveScratch, stageProp);
            return true;
        }

        private bool TryEvaluateEventInternal(
            LubyTheaterEventDefinition evt,
            bool ignoreGates,
            bool ignoreCooldown,
            bool leaveResolvedCast,
            out string report,
            out PlacedDecor stageProp)
        {
            report = null;
            stageProp = null;
            if (evt == null)
            {
                report = "事件为空。";
                return false;
            }

            var lines = new List<string>(12);
            bool ok = true;

            if (_sessions.Count >= MaxConcurrentSessions)
            {
                lines.Add($"× 已达同时场次上限（{ActiveSessionCount}/{MaxConcurrentSessions}）。");
                ok = false;
            }

            if (!ignoreGates)
            {
                if (DesktopPetServices.IsAnyPlacementHolding())
                {
                    lines.Add("× 正手持放置装饰或 Luby。");
                    ok = false;
                }

                if (DesktopPetServices.IsHubOpen())
                {
                    lines.Add("× Hub 已打开（自动扫描会挡新开）。");
                    ok = false;
                }
            }

            if (!ignoreCooldown && IsEventOnCooldown(evt.eventId))
            {
                _eventCooldownUntil.TryGetValue(evt.eventId, out float until);
                lines.Add($"× 事件冷却中（剩余 {Mathf.Max(0f, until - Time.time):0.0}s）。");
                ok = false;
            }

            LubyWorld world = DesktopPetServices.LubyWorld;
            DecorWorld decor = DesktopPetServices.DecorWorld;
            if (world == null)
            {
                lines.Add("× 场景无 LubyWorld。");
                ok = false;
            }

            if (!PropsSatisfied(evt, decor))
            {
                lines.Add("× 桌上道具不满足 requiredProps。");
                ok = false;
            }
            else if (evt.requiredProps != null && evt.requiredProps.Count > 0)
            {
                lines.Add("✓ 道具要求已满足。");
            }

            if (evt.requiresSocialIntent && !ignoreGates)
            {
                if (!HasAnySocialIntentOnDesk())
                {
                    lines.Add("× 需要社交意图：尚无人抽到 want_social。");
                    ok = false;
                }
                else
                {
                    lines.Add("✓ 桌上有社交意图。");
                }
            }

            if (world != null)
            {
                _castScratch.Clear();
                _resolveScratch.Clear();
                if (!TryResolveCast(
                        evt, world, decor, _castScratch, _resolveScratch,
                        enforceSocialIntent: !ignoreGates,
                        out stageProp))
                {
                    lines.Add(evt.requiresSocialIntent && !ignoreGates
                        ? "× 演员槽无法凑齐（含：意图者须在场 / 性格 / 跨度）。"
                        : "× 演员槽无法凑齐（性格/特质/行为/忙碌/数量）。");
                    ok = false;
                }
                else
                {
                    lines.Add($"✓ 已匹配 {_resolveScratch.Count} 名演员。");
                    bool seekFirst = evt.seekPartnerFirst && string.IsNullOrEmpty(evt.ResolveStagePropItemId());
                    for (int i = 0; i < _resolveScratch.Count; i++)
                    {
                        CastMember m = _resolveScratch[i];
                        string name = m.luby != null ? m.luby.gameObject.name : "?";
                        string intentMark = HasSocialIntent(m.luby) ? " ·有意图" : "";
                        string xLabel = seekFirst ? "现X" : "X";
                        string face = LubyTheaterStaging.FormatStageFacingShort(m.stageFacing);
                        lines.Add($"  · {m.roleKey} → {name} · {m.behaviorId} · {xLabel}={m.stageX:0.00} · 朝向{face}{intentMark}");
                    }

                    if (seekFirst)
                        lines.Add($"✓ 寻访开场：有意图者先找人；走开概率 {evt.partnerRejectChance:0%}；中点站位留下后再算。");

                    string stageId = evt.ResolveStagePropItemId();
                    if (!string.IsNullOrEmpty(stageId))
                    {
                        if (stageProp != null)
                            lines.Add($"✓ 舞台道具 {stageId} @ X={LubyTheaterStaging.GetPropAnchorWorld(stageProp).x:0.00}");
                        else
                        {
                            lines.Add($"× 找不到舞台道具 {stageId}。");
                            ok = false;
                        }
                    }
                }
            }

            if (!leaveResolvedCast)
            {
                _castScratch.Clear();
                _resolveScratch.Clear();
            }

            if (ok)
                lines.Insert(0, "可以开演。");
            else
                lines.Insert(0, "暂不能开演：");

            report = string.Join("\n", lines);
            return ok;
        }

        /// <summary>复制当前所有场次摘要（调试 UI 用）。</summary>
        public void CopyActiveSessionSummaries(List<TheaterSessionSnapshot> dst)
        {
            dst.Clear();
            for (int i = 0; i < _sessions.Count; i++)
            {
                Session s = _sessions[i];
                var castLines = new List<string>(4);
                FillSessionSnapshot(s, out string eventId, out string phaseLabel, out float remaining, castLines);
                dst.Add(new TheaterSessionSnapshot(
                    i + 1,
                    s.id,
                    eventId,
                    phaseLabel,
                    remaining,
                    s.cast.Count,
                    castLines));
            }
        }

        /// <summary>调试：Luby →「场1·lead」标签。</summary>
        public void FillLubyTheaterLabels(Dictionary<LubyInstanceComponent, string> labels)
        {
            labels.Clear();
            for (int si = 0; si < _sessions.Count; si++)
            {
                Session s = _sessions[si];
                int display = si + 1;
                for (int ci = 0; ci < s.cast.Count; ci++)
                {
                    CastMember m = s.cast[ci];
                    if (m?.luby == null)
                        continue;
                    labels[m.luby] = $"场{display}·{m.roleKey}";
                }
            }
        }

        private static void FillSessionSnapshot(
            Session s,
            out string eventId,
            out string phaseLabel,
            out float remainingSeconds,
            List<string> castLines)
        {
            castLines.Clear();
            eventId = s.evt != null ? s.evt.eventId : "?";
            phaseLabel = PhaseLabel(s.phase);
            remainingSeconds = s.phase == Phase.Performing
                ? Mathf.Max(0f, s.endAt - Time.time)
                : Mathf.Max(0f, s.approachDeadline - Time.time);

            for (int i = 0; i < s.cast.Count; i++)
            {
                CastMember m = s.cast[i];
                if (m == null)
                    continue;
                string name = LubyTheaterStaging.ShortLubyName(m.luby);
                string arrive = m.arrived ? "已到" : "走向中";
                string face = LubyTheaterStaging.FormatStageFacingShort(m.stageFacing);
                castLines.Add($"{m.roleKey}|{name}|{arrive}|{m.stageX:0.0}|{face}");
            }
        }

        private void Awake()
        {
            DesktopPetServices.RegisterLubyTheater(this);
            DesktopPetServices.RegisterLubyActivity(this);
            if (catalog == null)
                catalog = Resources.Load<LubyTheaterCatalog>("GameData/Luby/Theater/DefaultTheaterCatalog");
        }

        private void OnDestroy()
        {
            EndCurrent(reselect: false, applyCooldown: false);
            DesktopPetServices.UnregisterLubyTheater(this);
            DesktopPetServices.UnregisterLubyActivity(this);
        }

        private void Update()
        {
            TickSessions();

            if (Time.time < _nextScanAt)
                return;

            _nextScanAt = Time.time + scanInterval;
            if (_sessions.Count < MaxConcurrentSessions)
                TryStartAny();
        }

        public bool IsLubyInTheater(LubyInstanceComponent luby) => FindSessionContaining(luby) != null;

        public bool IsLubyBusy(LubyInstanceComponent luby) => IsLubyInTheater(luby);

        public bool TryHandlePlayerClick(LubyInstanceComponent luby)
        {
            Session session = FindSessionContaining(luby);
            if (session?.evt == null)
                return false;

            if (session.evt.allowPlayerInterrupt)
            {
                EndSession(session, reselect: true, applyCooldown: false);
                return true;
            }

            return true;
        }

        public void EndAllForLuby(LubyInstanceComponent luby)
        {
            if (luby == null)
                return;

            for (int i = _sessions.Count - 1; i >= 0; i--)
            {
                if (SessionContainsLuby(_sessions[i], luby))
                    EndSessionAt(i, reselect: true, applyCooldown: false);
            }
        }

        /// <summary>结束全部场次。</summary>
        public void EndCurrent(bool reselect, bool applyCooldown = true)
        {
            for (int i = _sessions.Count - 1; i >= 0; i--)
                EndSessionAt(i, reselect, applyCooldown);
        }

        private void EndSession(Session s, bool reselect, bool applyCooldown)
        {
            int idx = _sessions.IndexOf(s);
            if (idx >= 0)
                EndSessionAt(idx, reselect, applyCooldown);
        }

        private void EndSessionAt(int index, bool reselect, bool applyCooldown)
        {
            if (index < 0 || index >= _sessions.Count)
                return;

            Session s = _sessions[index];
            _sessions.RemoveAt(index);

            bool naturalEnd = applyCooldown && s.phase == Phase.Performing;

            if (applyCooldown &&
                s.evt != null &&
                !string.IsNullOrEmpty(s.evt.eventId) &&
                s.evt.cooldownSeconds > 0f)
            {
                _eventCooldownUntil[s.evt.eventId] = Time.time + s.evt.cooldownSeconds;
            }

            if (naturalEnd)
                RecordTheaterJournal(s);

            for (int i = 0; i < s.cast.Count; i++)
            {
                CastMember m = s.cast[i];
                if (m == null)
                    continue;

                m.loco?.Stop();
                RestoreBrain(m);

                if (reselect && m.agent != null)
                    m.agent.Brain?.InterruptAndReselect(performancePriority);
            }
        }

        private static void RecordTheaterJournal(Session s)
        {
            if (s?.cast == null || s.cast.Count < 2)
                return;

            for (int i = 0; i < s.cast.Count; i++)
            {
                CastMember self = s.cast[i];
                if (self?.luby == null)
                    continue;

                CastMember peer = null;
                for (int j = 0; j < s.cast.Count; j++)
                {
                    if (j == i)
                        continue;
                    if (s.cast[j]?.luby != null)
                    {
                        peer = s.cast[j];
                        break;
                    }
                }

                if (peer != null)
                    LubyJournalService.RecordGreet(self.luby, peer.luby);
            }
        }

        private Session FindSessionContaining(LubyInstanceComponent luby)
        {
            if (luby == null)
                return null;

            for (int si = 0; si < _sessions.Count; si++)
            {
                if (SessionContainsLuby(_sessions[si], luby))
                    return _sessions[si];
            }

            return null;
        }

        private static bool SessionContainsLuby(Session s, LubyInstanceComponent luby)
        {
            if (s == null || luby == null)
                return false;

            for (int i = 0; i < s.cast.Count; i++)
            {
                if (s.cast[i].luby == luby)
                    return true;
            }

            return false;
        }

        private void TickSessions()
        {
            for (int i = _sessions.Count - 1; i >= 0; i--)
            {
                Session s = _sessions[i];
                if (ShouldForceEnd(s))
                {
                    EndSessionAt(i, reselect: true, applyCooldown: false);
                    continue;
                }

                switch (s.phase)
                {
                    case Phase.Seeking:
                        TickSeeking(s);
                        break;
                    case Phase.Rejecting:
                        TickRejecting(s);
                        break;
                    case Phase.Approaching:
                        TickApproach(s);
                        break;
                    default:
                        TickPerformance(s);
                        break;
                }
            }
        }

        private bool ShouldForceEnd(Session s)
        {
            if (IsStagePropLost(s) || IsRequiredPropsLost(s) || IsStagePropMovedTooFar(s))
                return true;

            for (int i = 0; i < s.cast.Count; i++)
            {
                LubyInstanceComponent luby = s.cast[i].luby;
                if (luby == null || !luby.isActiveAndEnabled)
                    return true;
            }

            return false;
        }

        private bool IsStagePropLost(Session s)
        {
            if (s == null || !s.requiresStageProp)
                return false;
            return !LubyTheaterStaging.IsStagePropAlive(s.stageProp);
        }

        private bool IsRequiredPropsLost(Session s)
        {
            if (s?.evt == null)
                return false;
            return !PropsSatisfied(s.evt, DesktopPetServices.DecorWorld);
        }

        private bool IsStagePropMovedTooFar(Session s)
        {
            if (s == null || !s.requiresStageProp)
                return false;
            if (!LubyTheaterStaging.IsStagePropAlive(s.stageProp))
                return false;

            float ax = LubyTheaterStaging.GetPropAnchorWorld(s.stageProp).x;
            return Mathf.Abs(ax - s.stageAnchorX0) > stageMoveEndDistance;
        }

        private void TickSeeking(Session s)
        {
            LubyTheaterEventDefinition evt = s.evt;
            CastMember seeker = s.seeker;
            CastMember target = s.seekTarget;
            if (evt == null || seeker?.luby == null || seeker.loco == null ||
                target?.luby == null || target.loco == null)
            {
                EndSession(s, reselect: true, applyCooldown: false);
                return;
            }

            if (Time.time >= s.approachDeadline)
            {
                EndSession(s, reselect: true, applyCooldown: false);
                return;
            }

            float seekArrive = Mathf.Max(0.1f, evt.seekArriveDistance);
            float speed = Mathf.Max(0.1f, evt.stageMoveSpeed);
            float seekerX = seeker.luby.transform.position.x;
            float targetX = target.luby.transform.position.x;
            float standBeside = LubyTheaterStaging.ComputeSeekStandBesideX(seekerX, targetX, seekArrive);
            seeker.stageX = standBeside;
            seeker.loco.SetMoveTarget(standBeside, speed);
            target.loco.Stop();

            if (Mathf.Abs(seekerX - targetX) > seekArrive)
                return;

            seeker.loco.Stop();
            ApplyCastFacing(s, seeker, target.luby.transform.position.x);
            float rejectChance = Mathf.Clamp01(evt.partnerRejectChance);
            if (rejectChance > 0f && Random.value < rejectChance)
            {
                BeginReject(s);
                return;
            }

            BeginStageApproachAfterSeek(s);
        }

        private void BeginReject(Session s)
        {
            LubyTheaterEventDefinition evt = s.evt;
            CastMember seeker = s.seeker;
            CastMember target = s.seekTarget;
            if (evt == null || seeker?.luby == null || target?.luby == null || target.loco == null)
            {
                EndSession(s, reselect: true, applyCooldown: false);
                return;
            }

            float flee = Mathf.Max(0.3f, evt.rejectFleeDistance);
            float speed = Mathf.Max(0.1f, evt.stageMoveSpeed);
            float seekerX = seeker.luby.transform.position.x;
            float targetX = target.luby.transform.position.x;
            float away = targetX >= seekerX ? 1f : -1f;
            target.stageX = targetX + away * flee;
            target.arrived = false;
            seeker.loco?.Stop();
            target.loco.SetMoveTarget(target.stageX, speed);

            s.phase = Phase.Rejecting;
            s.approachDeadline = Time.time + Mathf.Max(0.3f, evt.rejectFleeSeconds);
        }

        private void TickRejecting(Session s)
        {
            CastMember target = s.seekTarget;
            LubyTheaterEventDefinition evt = s.evt;
            float speed = evt != null ? Mathf.Max(0.1f, evt.stageMoveSpeed) : 1.2f;
            if (target?.loco != null && target.luby != null)
                target.loco.SetMoveTarget(target.stageX, speed);

            if (Time.time < s.approachDeadline)
                return;

            // 走开不算完整演出：不进事件冷却，方便过会儿再试。
            EndSession(s, reselect: true, applyCooldown: false);
        }

        private void BeginStageApproachAfterSeek(Session s)
        {
            if (s?.evt == null)
                return;

            ApplyPeerMidpointStaging(s.cast, out float midpointX);
            s.stageAnchorX0 = midpointX;
            s.usesPeerMidpoint = true;
            s.phase = Phase.Approaching;
            s.approachDeadline = Time.time + Mathf.Max(1f, s.evt.stageApproachTimeout);

            float speed = Mathf.Max(0.1f, s.evt.stageMoveSpeed);
            for (int i = 0; i < s.cast.Count; i++)
            {
                CastMember m = s.cast[i];
                if (m?.loco == null)
                    continue;
                m.arrived = false;
                m.loco.SetMoveTarget(m.stageX, speed);
            }
        }

        private void TickApproach(Session s)
        {
            LubyTheaterEventDefinition evt = s.evt;
            float speed = evt != null ? evt.stageMoveSpeed : 1.2f;
            float arrive = evt != null ? evt.stageArriveDistance : 0.2f;

            if (Time.time >= s.approachDeadline)
            {
                EndSession(s, reselect: true, applyCooldown: false);
                return;
            }

            RefreshApproachTargets(s);

            bool allArrived = true;
            for (int i = 0; i < s.cast.Count; i++)
            {
                CastMember m = s.cast[i];
                if (m?.loco == null || m.luby == null)
                {
                    EndSession(s, reselect: true, applyCooldown: false);
                    return;
                }

                if (m.arrived)
                    continue;

                m.loco.SetMoveTarget(m.stageX, speed);
                if (Mathf.Abs(m.luby.transform.position.x - m.stageX) <= arrive)
                {
                    m.loco.Stop();
                    m.arrived = true;
                    ApplyCastFacing(s, m);
                }
                else
                {
                    allArrived = false;
                }
            }

            if (allArrived)
            {
                BeginPerformance(s);
            }
        }

        /// <summary>走位中重算目标：道具场跟锚点；社交场跟开场中点。</summary>
        private void RefreshApproachTargets(Session s)
        {
            if (s == null)
                return;

            if (s.usesPeerMidpoint)
            {
                for (int i = 0; i < s.cast.Count; i++)
                {
                    CastMember m = s.cast[i];
                    if (m == null || m.arrived)
                        continue;
                    m.stageX = LubyTheaterStaging.GetPeerStageWorldX(s.stageAnchorX0, m.stageOffsetX);
                }

                return;
            }

            if (!s.requiresStageProp)
                return;
            if (!LubyTheaterStaging.IsStagePropAlive(s.stageProp))
                return;

            for (int i = 0; i < s.cast.Count; i++)
            {
                CastMember m = s.cast[i];
                if (m == null || m.arrived)
                    continue;
                m.stageX = LubyTheaterStaging.GetStageWorldX(s.stageProp, m.stageOffsetX);
            }
        }

        private void BeginPerformance(Session s)
        {
            if (s == null || s.evt == null)
                return;

            ApplyAllCastFacing(s);

            for (int i = 0; i < s.cast.Count; i++)
            {
                CastMember m = s.cast[i];
                RestoreBrain(m);
                if (m.agent?.Brain == null ||
                    !m.agent.Brain.RequestBehavior(m.behaviorId, performancePriority, force: true))
                {
                    EndSession(s, reselect: true, applyCooldown: false);
                    return;
                }
            }

            s.phase = Phase.Performing;
            s.endAt = Time.time + Mathf.Max(1f, s.evt.durationSeconds);
        }

        private void TickPerformance(Session s)
        {
            if (Time.time >= s.endAt)
            {
                EndSession(s, reselect: true, applyCooldown: true);
                return;
            }

            for (int i = 0; i < s.cast.Count; i++)
            {
                CastMember m = s.cast[i];
                PetBrain brain = m?.brain ?? m?.agent?.Brain;
                if (brain == null)
                {
                    EndSession(s, reselect: true, applyCooldown: false);
                    return;
                }

                if (brain.CurrentBehaviorId == m.behaviorId)
                    continue;

                if (!brain.RequestBehavior(m.behaviorId, performancePriority, force: true))
                {
                    EndSession(s, reselect: true, applyCooldown: false);
                    return;
                }
            }
        }

        private void TryStartAny()
        {
            if (_sessions.Count >= MaxConcurrentSessions)
                return;
            if (catalog == null || catalog.events == null || catalog.events.Count == 0)
                return;
            if (DesktopPetServices.IsAnyPlacementHolding() || DesktopPetServices.IsHubOpen())
                return;

            LubyWorld world = DesktopPetServices.LubyWorld;
            DecorWorld decor = DesktopPetServices.DecorWorld;
            if (world == null)
                return;

            LubyTheaterEventDefinition best = null;
            float bestWeight = 0f;
            PlacedDecor bestStageProp = null;

            for (int i = 0; i < catalog.events.Count; i++)
            {
                LubyTheaterEventDefinition evt = catalog.events[i];
                if (evt == null || string.IsNullOrEmpty(evt.eventId))
                    continue;
                if (IsEventOnCooldown(evt.eventId))
                    continue;
                if (evt.weight <= 0f)
                    continue;
                if (evt.requiresSocialIntent && !HasAnySocialIntentOnDesk())
                    continue;
                if (!PropsSatisfied(evt, decor))
                    continue;

                _castScratch.Clear();
                _resolveScratch.Clear();
                if (!TryResolveCast(
                        evt, world, decor, _castScratch, _resolveScratch,
                        enforceSocialIntent: true,
                        out PlacedDecor stageProp))
                    continue;

                if (evt.weight > bestWeight)
                {
                    bestWeight = evt.weight;
                    best = evt;
                    bestStageProp = stageProp;
                    _bestCastScratch.Clear();
                    _bestCastScratch.AddRange(_resolveScratch);
                }
            }

            if (best == null)
                return;

            if (best.scanChance < 1f && Random.value > best.scanChance)
                return;

            Begin(best, _bestCastScratch, bestStageProp);
        }

        private void Begin(LubyTheaterEventDefinition evt, List<CastMember> cast, PlacedDecor stageProp)
        {
            if (evt == null || cast == null || cast.Count == 0)
                return;
            if (_sessions.Count >= MaxConcurrentSessions)
                return;

            for (int i = 0; i < cast.Count; i++)
            {
                CastMember m = cast[i];
                if (m?.luby == null || m.agent == null || m.loco == null)
                    return;
                if (string.IsNullOrEmpty(m.behaviorId))
                    return;
                if (IsLubyInTheater(m.luby) || DesktopPetServices.IsLubyExternallyBusy(m.luby))
                    return;
            }

            bool requiresStageProp = !string.IsNullOrEmpty(evt.ResolveStagePropItemId());
            if (requiresStageProp && !LubyTheaterStaging.IsStagePropAlive(stageProp))
                return;

            bool usesPeerMidpoint = !requiresStageProp;
            bool seekFirst = evt.seekPartnerFirst && usesPeerMidpoint && cast.Count >= 2;
            CastMember seeker = seekFirst ? FindSeekerBeforeClearIntent(cast) : null;
            CastMember seekTarget = seekFirst ? FindSeekTarget(cast, seeker) : null;
            if (seekFirst && (seeker == null || seekTarget == null))
                seekFirst = false;

            float anchorX0;
            Phase startPhase;
            if (seekFirst)
            {
                anchorX0 = seekTarget.luby.transform.position.x;
                startPhase = Phase.Seeking;
            }
            else if (usesPeerMidpoint)
            {
                ApplyPeerMidpointStaging(cast, out anchorX0);
                startPhase = Phase.Approaching;
            }
            else
            {
                anchorX0 = stageProp != null
                    ? LubyTheaterStaging.GetPropAnchorWorld(stageProp).x
                    : 0f;
                startPhase = Phase.Approaching;
            }

            var session = new Session
            {
                id = _nextSessionId++,
                evt = evt,
                phase = startPhase,
                approachDeadline = Time.time + Mathf.Max(1f, evt.stageApproachTimeout),
                stageProp = stageProp,
                requiresStageProp = requiresStageProp,
                usesPeerMidpoint = usesPeerMidpoint,
                stageAnchorX0 = anchorX0,
                seeker = seeker,
                seekTarget = seekTarget
            };
            session.cast.AddRange(cast);
            _sessions.Add(session);

            for (int i = 0; i < session.cast.Count; i++)
                ClearSocialIntent(session.cast[i].luby);

            float speed = Mathf.Max(0.1f, evt.stageMoveSpeed);
            if (seekFirst)
            {
                for (int i = 0; i < session.cast.Count; i++)
                    SuspendBrain(session.cast[i]);

                seekTarget.loco.Stop();
                float seekerX = seeker.luby.transform.position.x;
                float targetX = seekTarget.luby.transform.position.x;
                float standBeside = LubyTheaterStaging.ComputeSeekStandBesideX(
                    seekerX, targetX, evt.seekArriveDistance);
                seeker.stageX = standBeside;
                seeker.loco.SetMoveTarget(standBeside, speed);
            }
            else
            {
                for (int i = 0; i < session.cast.Count; i++)
                {
                    CastMember m = session.cast[i];
                    SuspendBrain(m);
                    m.loco.SetMoveTarget(m.stageX, speed);
                }
            }
        }

        private CastMember FindSeekerBeforeClearIntent(List<CastMember> cast)
        {
            if (cast == null)
                return null;

            CastMember fallback = null;
            for (int i = 0; i < cast.Count; i++)
            {
                CastMember m = cast[i];
                if (m?.luby == null)
                    continue;
                if (fallback == null)
                    fallback = m;
                if (HasSocialIntent(m.luby))
                    return m;
            }

            return fallback;
        }

        private static CastMember FindSeekTarget(List<CastMember> cast, CastMember seeker)
        {
            if (cast == null || seeker == null)
                return null;

            for (int i = 0; i < cast.Count; i++)
            {
                CastMember m = cast[i];
                if (m != null && m != seeker && m.luby != null)
                    return m;
            }

            return null;
        }

        /// <summary>仅测演员能否凑齐（与开演试配一致，含回溯）。</summary>
        public bool TryProbeCast(LubyTheaterEventDefinition evt, out int matchedCount)
        {
            matchedCount = 0;
            if (evt == null)
                return false;

            LubyWorld world = DesktopPetServices.LubyWorld;
            DecorWorld decor = DesktopPetServices.DecorWorld;
            if (world == null)
                return false;

            _castScratch.Clear();
            _resolveScratch.Clear();
            if (!TryResolveCast(
                    evt, world, decor, _castScratch, _resolveScratch,
                    enforceSocialIntent: true,
                    out _))
                return false;

            matchedCount = _resolveScratch.Count;
            return true;
        }

        private static void ApplyPeerMidpointStaging(List<CastMember> cast, out float midpointX)
        {
            midpointX = 0f;
            if (cast == null || cast.Count == 0)
                return;

            float min = float.MaxValue;
            float max = float.MinValue;
            int n = 0;
            for (int i = 0; i < cast.Count; i++)
            {
                CastMember m = cast[i];
                if (m?.luby == null)
                    continue;
                float x = m.luby.transform.position.x;
                if (x < min)
                    min = x;
                if (x > max)
                    max = x;
                n++;
            }

            midpointX = n == 0 ? 0f : (min + max) * 0.5f;
            for (int i = 0; i < cast.Count; i++)
            {
                CastMember m = cast[i];
                if (m == null)
                    continue;
                m.stageX = LubyTheaterStaging.GetPeerStageWorldX(midpointX, m.stageOffsetX);
                m.arrived = false;
            }
        }

        private static void SuspendBrain(CastMember m)
        {
            if (m.brain == null)
                m.brain = m.agent != null ? m.agent.Brain : null;
            if (m.brain == null)
                return;

            m.brainWasEnabled = m.brain.enabled;
            m.brain.enabled = false;
        }

        private static void RestoreBrain(CastMember m)
        {
            if (m?.brain == null)
                return;
            m.brain.enabled = m.brainWasEnabled;
        }

        private sealed class SlotNeed
        {
            public LubyTheaterRoleSlot slot;
            public int slotIndex;
            public int slotCount;
        }

        private static void BuildSlotNeeds(IReadOnlyList<LubyTheaterRoleSlot> roles, List<SlotNeed> dst)
        {
            dst.Clear();
            if (roles == null)
                return;

            for (int ri = 0; ri < roles.Count; ri++)
            {
                LubyTheaterRoleSlot slot = roles[ri];
                if (slot == null || slot.count <= 0)
                    continue;

                for (int si = 0; si < slot.count; si++)
                {
                    dst.Add(new SlotNeed
                    {
                        slot = slot,
                        slotIndex = si,
                        slotCount = slot.count
                    });
                }
            }
        }

        private bool CanPickLubyForSlot(
            LubyInstanceComponent luby,
            LubyTheaterRoleSlot slot,
            List<LubyInstanceComponent> used)
        {
            if (luby == null || !luby.isActiveAndEnabled)
                return false;
            if (used.Contains(luby))
                return false;
            if (IsLubyInTheater(luby))
                return false;
            if (DesktopPetServices.IsLubyExternallyBusy(luby))
                return false;
            if (!slot.Matches(luby))
                return false;

            PetAgent agent = luby.Agent;
            PetLocomotion loco = agent != null ? agent.Locomotion : null;
            PetBrain brain = agent != null ? agent.Brain : null;
            if (brain == null || loco == null)
                return false;
            string performanceId = slot.ResolvePerformanceBehaviorId();
            return !string.IsNullOrEmpty(performanceId) && brain.HasBehavior(performanceId);
        }

        private static CastMember MakeCastMember(
            LubyTheaterRoleSlot slot,
            int slotIndex,
            int slotCount,
            LubyInstanceComponent luby,
            PlacedDecor stageProp)
        {
            PetAgent agent = luby.Agent;
            PetLocomotion loco = agent.Locomotion;
            PetBrain brain = agent.Brain;
            float offsetX = LubyTheaterStaging.ResolveRoleOffsetX(slot.stageOffsetX, slotIndex, slotCount);
            float stageX = luby.transform.position.x;
            if (stageProp != null)
                stageX = LubyTheaterStaging.GetStageWorldX(stageProp, offsetX);

            return new CastMember
            {
                roleKey = slot.roleKey,
                luby = luby,
                agent = agent,
                brain = brain,
                loco = loco,
                behaviorId = slot.ResolvePerformanceBehaviorId(),
                stageOffsetX = offsetX,
                stageX = stageX,
                stageFacing = slot.stageFacing,
                arrived = false
            };
        }

        private static float GetStageCenterX(Session s)
        {
            if (s == null)
                return 0f;
            if (s.usesPeerMidpoint)
                return s.stageAnchorX0;
            if (s.requiresStageProp && LubyTheaterStaging.IsStagePropAlive(s.stageProp))
                return LubyTheaterStaging.GetPropAnchorWorld(s.stageProp).x;
            return s.stageAnchorX0;
        }

        private static void ApplyCastFacing(Session s, CastMember m, float referenceCenterX = float.NaN)
        {
            if (m?.loco == null || m.luby == null || m.stageFacing == LubyTheaterStageFacing.Auto)
                return;

            float centerX = float.IsNaN(referenceCenterX) ? GetStageCenterX(s) : referenceCenterX;
            float sign = LubyTheaterStaging.ResolveStageFacingSign(
                m.stageFacing,
                m.luby.transform.position.x,
                centerX);
            if (Mathf.Abs(sign) > 0.01f)
                m.loco.SetFacingSign(sign);
        }

        private static void ApplyAllCastFacing(Session s)
        {
            if (s?.cast == null)
                return;

            float centerX = GetStageCenterX(s);
            for (int i = 0; i < s.cast.Count; i++)
                ApplyCastFacing(s, s.cast[i], centerX);
        }

        private bool TryResolveCastBacktrack(
            IReadOnlyList<SlotNeed> needs,
            int needIdx,
            IReadOnlyList<LubyInstanceComponent> instances,
            List<LubyInstanceComponent> used,
            PlacedDecor stageProp,
            float maxCastSpanX,
            bool requiresSocialIntent,
            List<CastMember> cast)
        {
            if (needIdx >= needs.Count)
            {
                if (!LubyTheaterStaging.IsCastSpanOk(used, maxCastSpanX))
                    return false;
                if (requiresSocialIntent && !CastHasSocialIntent(cast))
                    return false;
                return true;
            }

            SlotNeed need = needs[needIdx];
            LubyTheaterRoleSlot slot = need.slot;
            if (slot == null || string.IsNullOrEmpty(slot.ResolvePerformanceBehaviorId()))
                return false;

            for (int li = 0; li < instances.Count; li++)
            {
                LubyInstanceComponent luby = instances[li];
                if (!CanPickLubyForSlot(luby, slot, used))
                    continue;

                used.Add(luby);
                cast.Add(MakeCastMember(slot, need.slotIndex, need.slotCount, luby, stageProp));
                if (TryResolveCastBacktrack(
                        needs, needIdx + 1, instances, used, stageProp, maxCastSpanX, requiresSocialIntent, cast))
                    return true;

                cast.RemoveAt(cast.Count - 1);
                used.RemoveAt(used.Count - 1);
            }

            return false;
        }

        private void BuildIntentFirstOrder(
            IReadOnlyList<LubyInstanceComponent> instances,
            List<LubyInstanceComponent> dst)
        {
            dst.Clear();
            if (instances == null)
                return;

            for (int i = 0; i < instances.Count; i++)
            {
                LubyInstanceComponent luby = instances[i];
                if (luby != null && HasSocialIntent(luby))
                    dst.Add(luby);
            }

            for (int i = 0; i < instances.Count; i++)
            {
                LubyInstanceComponent luby = instances[i];
                if (luby == null || HasSocialIntent(luby))
                    continue;
                dst.Add(luby);
            }
        }

        private bool CastHasSocialIntent(List<CastMember> cast)
        {
            if (cast == null)
                return false;
            for (int i = 0; i < cast.Count; i++)
            {
                if (HasSocialIntent(cast[i]?.luby))
                    return true;
            }

            return false;
        }

        private readonly List<LubyInstanceComponent> _intentPruneScratch = new List<LubyInstanceComponent>(8);

        private void PruneExpiredSocialIntents()
        {
            if (_socialIntentUntil.Count == 0)
                return;

            _intentPruneScratch.Clear();
            foreach (var kv in _socialIntentUntil)
            {
                if (kv.Key == null || Time.time >= kv.Value)
                    _intentPruneScratch.Add(kv.Key);
            }

            for (int i = 0; i < _intentPruneScratch.Count; i++)
                _socialIntentUntil.Remove(_intentPruneScratch[i]);
        }

        private bool TryResolveCast(
            LubyTheaterEventDefinition evt,
            LubyWorld world,
            DecorWorld decor,
            List<LubyInstanceComponent> used,
            List<CastMember> outCast,
            bool enforceSocialIntent,
            out PlacedDecor stageProp)
        {
            outCast.Clear();
            stageProp = null;
            if (evt == null || evt.roles == null || evt.roles.Count == 0 || world == null)
                return false;

            for (int ri = 0; ri < evt.roles.Count; ri++)
            {
                LubyTheaterRoleSlot slot = evt.roles[ri];
                if (slot == null || slot.count <= 0)
                    continue;
                if (string.IsNullOrEmpty(slot.ResolvePerformanceBehaviorId()))
                    return false;
            }

            string stageItemId = evt.ResolveStagePropItemId();
            if (!string.IsNullOrEmpty(stageItemId))
            {
                if (!LubyTheaterStaging.TryFindStageProp(stageItemId, decor, out stageProp))
                    return false;
            }

            BuildSlotNeeds(evt.roles, _slotNeedsScratch);
            if (_slotNeedsScratch.Count == 0)
                return false;

            bool needIntent = enforceSocialIntent && evt.requiresSocialIntent;
            IReadOnlyList<LubyInstanceComponent> pickOrder = world.Instances;
            if (evt.seekPartnerFirst && needIntent)
            {
                BuildIntentFirstOrder(world.Instances, _intentFirstScratch);
                pickOrder = _intentFirstScratch;
            }

            if (!TryResolveCastBacktrack(
                    _slotNeedsScratch,
                    0,
                    pickOrder,
                    used,
                    stageProp,
                    evt.maxCastSpanX,
                    needIntent,
                    outCast))
            {
                return false;
            }

            bool seekFirst = evt.seekPartnerFirst && stageProp == null;
            if (stageProp == null && !seekFirst)
                ApplyPeerMidpointStaging(outCast, out _);

            return true;
        }

        private static bool PropsSatisfied(LubyTheaterEventDefinition evt, DecorWorld decor)
        {
            if (evt.requiredProps == null || evt.requiredProps.Count == 0)
                return true;
            if (decor == null)
                return false;

            IReadOnlyList<PlacedDecor> placed = decor.Placed;
            for (int i = 0; i < evt.requiredProps.Count; i++)
            {
                LubyTheaterPropRequirement req = evt.requiredProps[i];
                string itemId = req?.ResolveItemId();
                if (string.IsNullOrEmpty(itemId))
                    continue;

                int count = 0;
                for (int p = 0; p < placed.Count; p++)
                {
                    PlacedDecor d = placed[p];
                    if (d != null && d.ItemId == itemId)
                        count++;
                }

                if (count < Mathf.Max(1, req.minCount))
                    return false;
            }

            return true;
        }

        private bool IsEventOnCooldown(string eventId)
        {
            return _eventCooldownUntil.TryGetValue(eventId, out float until) && Time.time < until;
        }

        private static string PhaseLabel(Phase phase)
        {
            switch (phase)
            {
                case Phase.Seeking:
                    return "寻访中";
                case Phase.Rejecting:
                    return "对方走开";
                case Phase.Approaching:
                    return "走位中";
                default:
                    return "表演中";
            }
        }
    }
}
