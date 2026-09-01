using System.Collections.Generic;
using DesktopPet.AI;
using DesktopPet.Decor;
using DesktopPet.Adventure;
using DesktopPet.Shop;
using Sirenix.OdinInspector;
using UnityEngine;

namespace DesktopPet.Luby
{
    /// <summary>Luby×装饰交互：AI 抽 seek_decor → 随机可玩装饰 → 走近 → 播商品表演。</summary>
    [DisallowMultipleComponent]
    public sealed class LubyDecorInteractionSystem : MonoBehaviour, ILubyActivity
    {
        private enum Phase
        {
            Approaching,
            Performing
        }

        private sealed class Session
        {
            public LubyInstanceComponent Luby;
            public PetAgent Agent;
            public DecorInteractable Decor;
            public DecorRadio Radio;
            public Phase Phase;
            public float EndAt;
        }

        [Title("Luby × 装饰交互")]
        [LabelText("表演优先级")]
        [SerializeField]
        private int performancePriority = 12;

        [LabelText("强制切入表演")]
        [SerializeField]
        private bool forcePerformance = true;

        [LabelText("走近超时（秒）")]
        [Tooltip("走近阶段权威超时；seek_decor 行为 maxDuration 仅作兜底，应 ≥ 本值")]
        [MinValue(1f)]
        [SerializeField]
        private float maxApproachSeconds = 10f;

        [LabelText("表演最长时长")]
        [MinValue(1f)]
        [SerializeField]
        private float maxInteractSeconds = 12f;

        private readonly Dictionary<string, float> _decorCooldownUntil = new Dictionary<string, float>();
        private readonly List<Session> _sessions = new List<Session>(4);
        private readonly List<DecorInteractable> _eligibleScratch = new List<DecorInteractable>(8);
        private DecorWorld _decorWorld;

        private void Awake()
        {
            DesktopPetServices.RegisterLubyDecorInteraction(this);
            DesktopPetServices.RegisterLubyActivity(this);
            EnsureDecorWorld();
        }

        private void OnDestroy()
        {
            EndAll();
            DesktopPetServices.UnregisterLubyDecorInteraction(this);
            DesktopPetServices.UnregisterLubyActivity(this);
        }

        private void Update()
        {
            TickSessions(DesktopPetServices.IsAnyPlacementHolding());
        }

        /// <summary>是否存在至少一件当前可 Claim 的装饰（供 seek_decor CanEnter）。</summary>
        public bool HasAnyEligible(LubyInstanceComponent luby)
        {
            if (!CanLubySeek(luby))
                return false;
            CollectEligible(luby, _eligibleScratch);
            return _eligibleScratch.Count > 0;
        }

        /// <summary>均匀随机挑一件合格装饰并 Occupy，进入 Approaching。</summary>
        public bool TryClaimRandom(LubyInstanceComponent luby, out DecorInteractable decor)
        {
            decor = null;
            if (!CanLubySeek(luby))
                return false;

            CollectEligible(luby, _eligibleScratch);
            if (_eligibleScratch.Count == 0)
                return false;

            DecorInteractable pick = _eligibleScratch[Random.Range(0, _eligibleScratch.Count)];
            if (pick == null || !pick.TryOccupy())
                return false;

            PetAgent agent = luby.Agent;
            _sessions.Add(new Session
            {
                Luby = luby,
                Agent = agent,
                Decor = pick,
                Radio = null,
                Phase = Phase.Approaching,
                EndAt = Time.time + maxApproachSeconds
            });
            decor = pick;
            return true;
        }

        /// <summary>走近中则返回已 Claim 的装饰；否则 false。</summary>
        public bool TryGetApproachingDecor(LubyInstanceComponent luby, out DecorInteractable decor)
        {
            decor = null;
            Session s = FindSession(luby);
            if (s == null || s.Phase != Phase.Approaching || s.Decor == null)
                return false;
            decor = s.Decor;
            return true;
        }

        /// <summary>到达锚点后切入商品表演；收音机顺带开机。</summary>
        public bool TryStartPerformance(LubyInstanceComponent luby)
        {
            Session s = FindSession(luby);
            if (s == null || s.Phase != Phase.Approaching || s.Decor == null)
                return false;

            PlacedDecor placed = s.Decor.GetComponent<PlacedDecor>();
            ShopItemDefinition def = placed != null ? placed.Definition : null;
            string personalityId = luby.ResolvePersonalityId();
            string performanceId = def != null
                ? def.ResolveLubyPerformanceBehaviorId(personalityId)
                : null;
            if (string.IsNullOrEmpty(performanceId))
            {
                FinishSession(s, reselect: true);
                _sessions.Remove(s);
                return false;
            }

            DecorRadio radio = s.Decor.GetComponent<DecorRadio>();
            radio?.SetOn(true);
            s.Radio = radio;
            s.Phase = Phase.Performing;
            s.EndAt = Time.time + maxInteractSeconds;

            PetAgent agent = s.Agent != null ? s.Agent : luby.Agent;
            s.Agent = agent;
            if (agent?.Brain != null)
            {
                agent.Brain.RequestBehavior(
                    performanceId,
                    performancePriority,
                    forcePerformance);
            }

            return true;
        }

        /// <summary>走近阶段被打断/失败时取消 Claim（表演中勿调）。</summary>
        public void CancelApproach(LubyInstanceComponent luby)
        {
            Session s = FindSession(luby);
            if (s == null || s.Phase != Phase.Approaching)
                return;
            FinishSession(s, reselect: false);
            _sessions.Remove(s);
        }

        public void EndAllForDecor(DecorInteractable decor)
        {
            if (decor == null)
                return;
            for (int i = _sessions.Count - 1; i >= 0; i--)
            {
                Session s = _sessions[i];
                if (s.Decor != decor)
                    continue;
                FinishSession(s, reselect: true);
                _sessions.RemoveAt(i);
            }
        }

        public void EndAllForLuby(LubyInstanceComponent luby)
        {
            LubyActivityEndUtility.EndAllForLuby(
                _sessions,
                luby,
                getSessionLuby: s => s.Luby,
                endAction: s => FinishSession(s, reselect: true));
        }

        public bool IsLubyBusy(LubyInstanceComponent luby)
        {
            return FindSession(luby) != null;
        }

        private bool CanLubySeek(LubyInstanceComponent luby)
        {
            if (luby == null || !luby.isActiveAndEnabled)
                return false;
            if (DesktopPetServices.IsAnyPlacementHolding() || DesktopPetServices.IsHubOpen())
                return false;
            if (DesktopPetServices.IsLubyBlockedForWorldActivity(luby))
                return false;
            return true;
        }

        private void CollectEligible(LubyInstanceComponent luby, List<DecorInteractable> dst)
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
                if (!IsDecorEligible(luby, d))
                    continue;
                dst.Add(d);
            }
        }

        private bool IsDecorEligible(LubyInstanceComponent luby, DecorInteractable decor)
        {
            if (luby == null || decor == null)
                return false;
            PlacedDecor placed = decor.GetComponent<PlacedDecor>();
            ShopItemDefinition def = placed != null ? placed.Definition : null;
            if (def == null || !def.HasLubyInteractGate)
                return false;
            if (def.itemId == LubyAdventureSystem.AdventureBoardItemId)
                return false;
            if (!def.MatchesLubyInteractGate(luby))
                return false;
            if (string.IsNullOrEmpty(def.ResolveLubyPerformanceBehaviorId(luby.ResolvePersonalityId())))
                return false;
            if (!decor.HasFreeSlot)
                return false;
            if (IsOnDecorCooldown(decor))
                return false;
            return true;
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

        private void TickSessions(bool holding)
        {
            for (int i = _sessions.Count - 1; i >= 0; i--)
            {
                Session s = _sessions[i];
                if (s.Luby == null || s.Decor == null)
                {
                    FinishSession(s, reselect: false);
                    _sessions.RemoveAt(i);
                    continue;
                }

                if (!s.Luby.isActiveAndEnabled || !s.Decor.isActiveAndEnabled)
                {
                    FinishSession(s, reselect: true);
                    _sessions.RemoveAt(i);
                    continue;
                }

                if (holding || DesktopPetServices.IsLubyBusyWithOtherActivities(s.Luby, this))
                {
                    FinishSession(s, reselect: true);
                    _sessions.RemoveAt(i);
                    continue;
                }

                if (Time.time >= s.EndAt)
                {
                    FinishSession(s, reselect: true);
                    _sessions.RemoveAt(i);
                    continue;
                }

                if (s.Phase == Phase.Performing)
                {
                    Vector2 foot = s.Luby.transform.position;
                    if (!s.Decor.IsWithinRadius(foot))
                    {
                        FinishSession(s, reselect: true);
                        _sessions.RemoveAt(i);
                    }
                }
            }
        }

        private void EnsureDecorWorld()
        {
            if (_decorWorld == null)
                _decorWorld = DesktopPetServices.DecorWorld;
        }

        private void FinishSession(Session s, bool reselect)
        {
            if (s == null)
                return;

            bool completedPerformance = s.Phase == Phase.Performing && s.Luby != null;
            DecorInteractable decorForJournal = completedPerformance ? s.Decor : null;

            if (s.Decor != null)
            {
                s.Decor.ReleaseOccupy();
                MarkDecorCooldown(s.Decor);
            }

            if (s.Radio != null)
                s.Radio.SetOn(false);

            if (reselect && s.Agent != null)
                s.Agent.Brain?.InterruptAndReselect(performancePriority);

            if (completedPerformance)
                LubyJournalService.RecordDecor(s.Luby, decorForJournal);
        }

        private void EndAll()
        {
            for (int i = _sessions.Count - 1; i >= 0; i--)
                FinishSession(_sessions[i], reselect: false);
            _sessions.Clear();
        }

        private void MarkDecorCooldown(DecorInteractable decor)
        {
            if (decor == null || decor.cooldownSeconds <= 0f)
                return;
            _decorCooldownUntil[decor.CooldownKey] = Time.time + decor.cooldownSeconds;
        }

        private bool IsOnDecorCooldown(DecorInteractable decor)
        {
            if (decor == null)
                return false;
            return _decorCooldownUntil.TryGetValue(decor.CooldownKey, out float until) && Time.time < until;
        }
    }
}
