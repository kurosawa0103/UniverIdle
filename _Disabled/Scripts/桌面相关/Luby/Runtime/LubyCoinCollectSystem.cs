using System.Collections.Generic;
using DesktopPet.AI;
using DesktopPet.Decor;
using DesktopPet.Hub;
using DesktopPet.Save;
using Sirenix.OdinInspector;
using UnityEngine;

namespace DesktopPet.Luby
{
    /// <summary>Luby 主动捡桌上的金币：挑最近落地金币，走近后自动收取。</summary>
    [DisallowMultipleComponent]
    public sealed class LubyCoinCollectSystem : MonoBehaviour, ILubyActivity
    {
        private sealed class Session
        {
            public LubyInstanceComponent luby;
            public PetAgent agent;
            public DecorGoldCoin coin;
            public float endAt;
        }

        private const string CollectBehaviorId = "collect_coin";

        [Title("Luby 捡金币")]

        [LabelText("请求优先级")]
        [SerializeField]
        private int requestPriority = 13;

        [LabelText("扫描间隔")]
        [MinValue(0.1f)]
        [SerializeField]
        private float scanInterval = 1.5f;

        [LabelText("收取距离")]
        [MinValue(0.05f)]
        [SerializeField]
        private float collectDistance = 0.42f;

        [LabelText("单次超时")]
        [MinValue(1f)]
        [SerializeField]
        private float maxCollectSeconds = 8f;

        [Title("随机请求")]
        [LabelText("命中概率（0-1）")]
        [SerializeField]
        [Range(0f, 1f)]
        private float collectRequestChance = 0.35f;

        private readonly List<Session> _sessions = new List<Session>(8);
        private readonly List<DecorGoldCoin> _scratchCoins = new List<DecorGoldCoin>(32);
        private readonly Dictionary<string, float> _cooldownUntil = new Dictionary<string, float>();
        private float _nextScanAt;

        private void Awake()
        {
            DesktopPetServices.RegisterLubyActivity(this);
        }

        private void OnDestroy()
        {
            EndAll(reselect: false);
            DesktopPetServices.UnregisterLubyActivity(this);
        }

        private void Update()
        {
            TickSessions();

            if (Time.unscaledTime < _nextScanAt)
                return;

            _nextScanAt = Time.unscaledTime + scanInterval;
            TryAutoBegin();
        }

        public void EndAllForLuby(LubyInstanceComponent luby)
        {
            LubyActivityEndUtility.EndAllForLuby(
                _sessions,
                luby,
                getSessionLuby: s => s.luby,
                endAction: s => FinishSession(s, reselect: false));
        }

        public bool IsLubyBusy(LubyInstanceComponent luby)
        {
            for (int i = 0; i < _sessions.Count; i++)
            {
                if (_sessions[i].luby == luby)
                    return true;
            }

            return false;
        }

        private void TryAutoBegin()
        {
            if (DesktopPetServices.IsAnyPlacementHolding() || DesktopPetServices.IsHubOpen())
                return;

            LubyWorld world = DesktopPetServices.LubyWorld;
            if (world == null)
                return;

            DecorGoldCoin.CollectActive(_scratchCoins);
            if (_scratchCoins.Count == 0)
                return;

            IReadOnlyList<LubyInstanceComponent> lubies = world.Instances;
            for (int i = 0; i < lubies.Count; i++)
            {
                LubyInstanceComponent luby = lubies[i];
                if (!CanLubyStart(luby))
                    continue;

                DecorGoldCoin bestCoin = FindBestCoin(luby);
                if (bestCoin != null)
                    TryBegin(luby, bestCoin);
            }
        }

        private bool TryBegin(LubyInstanceComponent luby, DecorGoldCoin coin)
        {
            if (luby == null || coin == null || !coin.TryClaim(luby))
                return false;

            // 让“捡币”跟随 AI 随机氛围：不命中就放弃本次尝试。
            // 下一轮扫描/冷却过后还有机会。
            if (collectRequestChance <= 0f || UnityEngine.Random.value > collectRequestChance)
            {
                coin.ReleaseClaim(luby);
                return false;
            }

            LubyCoinCollector collector = luby.CoinCollector;
            PetAgent agent = luby.Agent;
            if (collector == null || agent?.Brain == null)
            {
                coin.ReleaseClaim(luby);
                return false;
            }

            collector.BeginCollect(coin);
            if (!agent.Brain.RequestBehavior(CollectBehaviorId, requestPriority, false))
            {
                collector.Clear();
                coin.ReleaseClaim(luby);
                return false;
            }

            _sessions.Add(new Session
            {
                luby = luby,
                agent = agent,
                coin = coin,
                endAt = Time.time + maxCollectSeconds
            });
            return true;
        }

        private void TickSessions()
        {
            bool holding = DesktopPetServices.IsAnyPlacementHolding();
            bool hubOpen = DesktopPetServices.IsHubOpen();

            for (int i = _sessions.Count - 1; i >= 0; i--)
            {
                Session session = _sessions[i];
                if (session == null || session.luby == null)
                {
                    FinishSession(session, reselect: false);
                    _sessions.RemoveAt(i);
                    continue;
                }

                if (!session.luby.isActiveAndEnabled ||
                    holding ||
                    hubOpen ||
                    DesktopPetServices.IsLubyBusyWithOtherActivities(session.luby, this))
                {
                    FinishSession(session, reselect: true);
                    _sessions.RemoveAt(i);
                    continue;
                }

                if (session.coin == null || !session.coin.isActiveAndEnabled)
                {
                    FinishSession(session, reselect: true);
                    _sessions.RemoveAt(i);
                    continue;
                }

                if (Time.time >= session.endAt)
                {
                    FinishSession(session, reselect: true);
                    _sessions.RemoveAt(i);
                    continue;
                }

                bool confirmed = session.coin.IsCollected;
                if (!confirmed
                    && session.coin.CanReach(session.luby, collectDistance)
                    && (session.coin.ClaimedByLuby == null || session.coin.ClaimedByLuby == session.luby))
                {
                    confirmed = session.coin.Collect();
                }

                if (confirmed)
                {
                    LubyJournalService.RecordCoin(session.luby, persist: false);
                    DesktopPetSaveMgr.PersistActive();
                    FinishSession(session, reselect: true);
                    _sessions.RemoveAt(i);
                }
            }
        }

        private DecorGoldCoin FindBestCoin(LubyInstanceComponent luby)
        {
            DecorGoldCoin best = null;
            float bestDist = float.MaxValue;
            float x = luby.transform.position.x;

            for (int i = 0; i < _scratchCoins.Count; i++)
            {
                DecorGoldCoin coin = _scratchCoins[i];
                if (coin == null || !coin.IsGrounded || coin.IsCollected)
                    continue;
                if (coin.ClaimedByLuby != null && coin.ClaimedByLuby != luby)
                    continue;

                float dist = Mathf.Abs(coin.PickupX - x);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = coin;
                }
            }

            return best;
        }

        private bool CanLubyStart(LubyInstanceComponent luby)
        {
            if (luby == null || !luby.isActiveAndEnabled)
                return false;
            if (DesktopPetServices.IsLubyBlockedForWorldActivity(luby))
                return false;
            return !IsOnCooldown(luby);
        }

        private void FinishSession(Session session, bool reselect)
        {
            if (session == null)
                return;

            if (session.coin != null && !session.coin.IsCollected)
                session.coin.ReleaseClaim(session.luby);

            session.luby?.CoinCollector?.Clear();

            if (session.luby != null)
            {
                MarkCooldown(session.luby);
                if (reselect)
                    session.agent?.Brain?.InterruptAndReselect(requestPriority);
            }
        }

        private void EndAll(bool reselect)
        {
            for (int i = _sessions.Count - 1; i >= 0; i--)
                FinishSession(_sessions[i], reselect);
            _sessions.Clear();
        }

        private void MarkCooldown(LubyInstanceComponent luby)
        {
            string id = luby != null ? luby.InstanceId : null;
            if (string.IsNullOrEmpty(id))
                return;
            _cooldownUntil[id] = Time.time + 5f;
        }

        private bool IsOnCooldown(LubyInstanceComponent luby)
        {
            string id = luby != null ? luby.InstanceId : null;
            if (string.IsNullOrEmpty(id))
                return false;
            return _cooldownUntil.TryGetValue(id, out float until) && Time.time < until;
        }

    }
}
