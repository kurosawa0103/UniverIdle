using System.Collections.Generic;
using DesktopPet.Luby;
using UnityEngine;

namespace DesktopPet.AI
{
    /// <summary>
    /// 行为选择与切换：加权随机、冷却、打断优先级；附带活动日志（当前 / 预选下一段 / 历史）。
    /// </summary>
    public sealed class PetBrain : MonoBehaviour
    {
        private const int HistoryCapacity = 48;

        [SerializeField]
        private PetAiGroup aiGroup;

        [SerializeField]
        private PetAgent agent;

        public PetAiGroup AiGroup => aiGroup;
        public PetBehaviorDefinition CurrentDefinition =>
            _currentRuntime != null ? _currentRuntime.Definition : null;
        public string CurrentBehaviorId =>
            CurrentDefinition != null ? CurrentDefinition.behaviorId : string.Empty;

        public float CurrentElapsed => _currentRuntime != null ? _currentRuntime.Elapsed : 0f;
        public float CurrentDuration => _currentRuntime != null ? _currentRuntime.Duration : 0f;
        public float CurrentRemaining =>
            _currentRuntime != null
                ? Mathf.Max(0f, _currentRuntime.Duration - _currentRuntime.Elapsed)
                : 0f;

        public PetBehaviorDefinition PlannedNextDefinition => _plannedNext;
        public string PlannedNextBehaviorId =>
            _plannedNext != null ? _plannedNext.behaviorId : string.Empty;

        private readonly Dictionary<string, float> _cooldownUntil = new Dictionary<string, float>();
        private readonly List<PetBehaviorDefinition> _candidates = new List<PetBehaviorDefinition>(16);
        private readonly List<float> _candidateWeights = new List<float>(16);
        private readonly List<LubyTraitDefinition> _traits = new List<LubyTraitDefinition>(2);
        private LubyPersonalityDefinition _personality;
        private readonly PetBehaviorLogEntry[] _history = new PetBehaviorLogEntry[HistoryCapacity];

        private PetBehaviorContext _context;
        private IPetBehaviorRuntime _currentRuntime;
        private PetBehaviorDefinition _plannedNext;
        private PetBehaviorLogEntry _openLog;
        private bool _hasOpenLog;
        private int _historyStart;
        private int _historyCount;
        private bool _started;
        private string _switchReason = "start";

        private void Awake()
        {
            if (agent == null)
                agent = GetComponent<PetAgent>();
        }

        private void Start()
        {
            EnsureContext();
            _started = true;
            if (aiGroup == null)
            {
                Debug.LogError("[PetBrain] 未指定 PetAiGroup。", this);
                enabled = false;
                return;
            }

            _switchReason = "start";
            EnterFallbackOrFirst();
        }

        private void Update()
        {
            if (!_started || agent == null || _context == null)
                return;

            _context.BeginFrame(Time.deltaTime, Time.time);

            if (_currentRuntime != null)
            {
                _currentRuntime.OnTick(_context);
                MaintainPlannedNext();
                if (ShouldExitBecauseConditionsLost())
                {
                    _switchReason = "condition_lost";
                    TransitionToNext();
                }
                else if (_currentRuntime.WantsExit)
                {
                    _switchReason = "complete";
                    TransitionToNext();
                }
            }
            else
            {
                _switchReason = "recover";
                TransitionToNext();
            }
        }

        /// <param name="personality">当前性格（Combo 匹配用）。</param>
        /// <param name="trait">第一特质：附加行为并池 + 对性格组同 id 权重相加。</param>
        /// <param name="trait2">第二特质（可空）；与第一特质行为/叠权合并。</param>
        public void SetAiGroup(
            PetAiGroup group,
            LubyPersonalityDefinition personality,
            LubyTraitDefinition trait,
            LubyTraitDefinition trait2 = null,
            bool restart = true)
        {
            aiGroup = group;
            _personality = personality;
            _traits.Clear();
            if (trait != null)
                _traits.Add(trait);
            if (trait2 != null && trait2 != trait)
                _traits.Add(trait2);
            if (restart && _started)
            {
                ExitCurrent();
                _plannedNext = null;
                _switchReason = "set_group";
                EnterFallbackOrFirst();
            }
        }

        /// <summary>组内 / requestOnly / 特质并池 / Combo 里是否存在该行为 id。</summary>
        /// <summary>组内 / requestOnly / 特质并池 / Combo 里是否存在该行为 id。</summary>
        public bool HasBehavior(string behaviorId) => FindBehaviorById(behaviorId) != null;

        /// <summary>
        /// 外部请求切入指定行为（点击、喂食、DLC 事件）。
        /// </summary>
        public bool RequestBehavior(string behaviorId, int requestPriority = 0, bool force = false)
        {
            if (string.IsNullOrEmpty(behaviorId))
                return false;

            EnsureContext();
            PetBehaviorDefinition def = FindBehaviorById(behaviorId);
            if (def == null)
            {
                Debug.LogWarning($"[PetBrain] 找不到行为 id={behaviorId}", this);
                return false;
            }

            if (!force && _currentRuntime != null)
            {
                PetBehaviorDefinition current = _currentRuntime.Definition;
                if (!current.canBeInterrupted && requestPriority <= current.interruptPriority)
                    return false;
            }

            if (!def.CanEnter(_context))
                return false;

            _plannedNext = null;
            _switchReason = "request";
            SwitchTo(def);
            return true;
        }

        /// <summary>
        /// 打断当前行为并按权重重选（点击默认路径）。
        /// </summary>
        public bool InterruptAndReselect(int requestPriority = 10)
        {
            if (_currentRuntime != null)
            {
                PetBehaviorDefinition current = _currentRuntime.Definition;
                if (!current.canBeInterrupted && requestPriority <= current.interruptPriority)
                    return false;
            }

            _plannedNext = null;
            _switchReason = "interrupt";
            TransitionToNext(forceIgnoreRepeatLock: false);
            return true;
        }

        /// <summary>复制已结束的行为历史（旧→新）。不含当前未结束段。</summary>
        public void CopyHistory(List<PetBehaviorLogEntry> dst)
        {
            if (dst == null)
                return;
            dst.Clear();
            for (int i = 0; i < _historyCount; i++)
            {
                int idx = (_historyStart + i) % HistoryCapacity;
                dst.Add(_history[idx]);
            }
        }

        public int HistoryCount => _historyCount;

        private void TransitionToNext(bool forceIgnoreRepeatLock = false)
        {
            EnsureContext();
            PetBehaviorDefinition next = _plannedNext;
            _plannedNext = null;

            if (next != null && !IsUsableNow(next))
                next = null;

            if (next == null)
                next = SelectNext(forceIgnoreRepeatLock);

            if (next == null)
                next = aiGroup != null ? aiGroup.fallbackBehavior : null;

            if (next == null)
            {
                ExitCurrent();
                return;
            }

            SwitchTo(next);
        }

        private void EnterFallbackOrFirst()
        {
            EnsureContext();
            PetBehaviorDefinition start = aiGroup.fallbackBehavior;
            if (start == null && aiGroup.behaviors != null && aiGroup.behaviors.Count > 0)
                start = aiGroup.behaviors[0];

            if (start != null)
                SwitchTo(start);
        }

        private void SwitchTo(PetBehaviorDefinition definition)
        {
            EnsureContext();
            string reason = _switchReason;
            ExitCurrent();

            _currentRuntime = definition.CreateRuntime();
            if (_currentRuntime == null)
            {
                Debug.LogError($"[PetBrain] CreateRuntime 返回 null: {definition.name}", this);
                _plannedNext = null;
                return;
            }

            _currentRuntime.OnEnter(_context);
            BeginOpenLog(definition, reason);
            RefreshPlannedNext();
            _switchReason = "complete";
        }

        private void RefreshPlannedNext()
        {
            // 只按当前约束预选；冷却未好时宁可空着，等 MaintainPlannedNext 再补，避免预锁成「再来一次当前」。
            _plannedNext = SelectNext(forceIgnoreRepeatLock: false);
        }

        private void MaintainPlannedNext()
        {
            if (_plannedNext != null)
            {
                bool sameAsCurrent = aiGroup != null &&
                                     aiGroup.avoidImmediateRepeat &&
                                     !string.IsNullOrEmpty(CurrentBehaviorId) &&
                                     _plannedNext.behaviorId == CurrentBehaviorId;
                if (sameAsCurrent || !IsUsableNow(_plannedNext))
                    _plannedNext = null;
            }

            if (_plannedNext == null)
                RefreshPlannedNext();
        }

        private void ExitCurrent()
        {
            if (_currentRuntime == null)
            {
                CloseOpenLog(Time.time);
                return;
            }

            PetBehaviorDefinition def = _currentRuntime.Definition;
            _currentRuntime.OnExit(_context);
            _currentRuntime = null;
            MarkCooldown(def);
            CloseOpenLog(Time.time);
        }

        private void BeginOpenLog(PetBehaviorDefinition definition, string reason)
        {
            float duration = _currentRuntime != null ? _currentRuntime.Duration : 0f;
            _openLog = new PetBehaviorLogEntry(
                definition != null ? definition.behaviorId : string.Empty,
                definition != null ? definition.name : string.Empty,
                Time.time,
                -1f,
                duration,
                reason);
            _hasOpenLog = true;
        }

        private void CloseOpenLog(float exitedAt)
        {
            if (!_hasOpenLog)
                return;

            PushHistory(_openLog.WithExit(exitedAt));
            _hasOpenLog = false;
        }

        private void PushHistory(PetBehaviorLogEntry entry)
        {
            if (_historyCount < HistoryCapacity)
            {
                int idx = (_historyStart + _historyCount) % HistoryCapacity;
                _history[idx] = entry;
                _historyCount++;
                return;
            }

            _history[_historyStart] = entry;
            _historyStart = (_historyStart + 1) % HistoryCapacity;
        }

        private void MarkCooldown(PetBehaviorDefinition definition)
        {
            if (definition == null || definition.cooldown <= 0f)
                return;

            _cooldownUntil[definition.behaviorId] = Time.time + definition.cooldown;
        }

        private bool IsOnCooldown(PetBehaviorDefinition definition)
        {
            if (definition == null || string.IsNullOrEmpty(definition.behaviorId))
                return false;

            if (!_cooldownUntil.TryGetValue(definition.behaviorId, out float until))
                return false;

            return Time.time < until;
        }

        /// <summary>
        /// 预选/切换前再验：冷却 + 进入条件。权重只在 Append*Candidates 里管（特质用条目 weight，不读资产 weight）。
        /// </summary>
        private bool IsUsableNow(PetBehaviorDefinition definition)
        {
            if (definition == null)
                return false;
            if (IsOnCooldown(definition))
                return false;
            return definition.CanEnter(_context);
        }

        private bool ShouldExitBecauseConditionsLost()
        {
            if (_currentRuntime == null)
                return false;

            PetBehaviorDefinition def = _currentRuntime.Definition;
            if (!def.maintainEnterConditionsWhileActive)
                return false;

            return !def.CanEnter(_context);
        }

        private PetBehaviorDefinition SelectNext(bool forceIgnoreRepeatLock)
        {
            _candidates.Clear();
            _candidateWeights.Clear();

            bool hasTraitPool = HasAnyTraitBehaviors();
            if (aiGroup == null && !hasTraitPool && !HasAnyMatchingComboBehaviors())
                return null;

            string currentId = CurrentBehaviorId;
            AppendGroupCandidates(currentId, forceIgnoreRepeatLock);
            AppendTraitCandidates(currentId, forceIgnoreRepeatLock);
            AppendComboCandidates(currentId, forceIgnoreRepeatLock);

            if (_candidates.Count == 0)
            {
                if (aiGroup != null && aiGroup.avoidImmediateRepeat && !forceIgnoreRepeatLock)
                    return SelectNext(forceIgnoreRepeatLock: true);
                return null;
            }

            float total = 0f;
            for (int i = 0; i < _candidateWeights.Count; i++)
            {
                float w = _candidateWeights[i];
                if (w > 0f)
                    total += w;
            }

            if (total <= 0f)
                return _candidates[0];

            float roll = Random.Range(0f, total);
            float acc = 0f;
            for (int i = 0; i < _candidates.Count; i++)
            {
                float w = _candidateWeights[i];
                if (w <= 0f)
                    continue;
                acc += w;
                if (roll <= acc)
                    return _candidates[i];
            }

            return _candidates[_candidates.Count - 1];
        }

        private void AppendGroupCandidates(string currentId, bool forceIgnoreRepeatLock)
        {
            IReadOnlyList<PetBehaviorDefinition> behaviors = aiGroup?.behaviors;
            if (behaviors == null)
                return;

            for (int i = 0; i < behaviors.Count; i++)
            {
                PetBehaviorDefinition def = behaviors[i];
                if (!IsSelectable(def, currentId, forceIgnoreRepeatLock))
                    continue;

                float w = def.weight + GetTraitGroupAdd(def.behaviorId);
                if (w <= 0f)
                    continue;

                _candidates.Add(def);
                _candidateWeights.Add(w);
            }
        }

        private void AppendTraitCandidates(string currentId, bool forceIgnoreRepeatLock)
        {
            for (int t = 0; t < _traits.Count; t++)
            {
                LubyTraitDefinition trait = _traits[t];
                IReadOnlyList<LubyWeightedBehaviorEntry> entries = trait?.behaviors;
                if (entries == null)
                    continue;

                for (int i = 0; i < entries.Count; i++)
                {
                    LubyWeightedBehaviorEntry e = entries[i];
                    if (e == null || e.behavior == null || e.weight <= 0f)
                        continue;
                    if (!IsSelectable(e.behavior, currentId, forceIgnoreRepeatLock))
                        continue;

                    int existing = IndexOfCandidate(e.behavior.behaviorId);
                    if (existing >= 0)
                        _candidateWeights[existing] += e.weight;
                    else
                    {
                        _candidates.Add(e.behavior);
                        _candidateWeights.Add(e.weight);
                    }
                }
            }
        }

        private void AppendComboCandidates(string currentId, bool forceIgnoreRepeatLock)
        {
            IReadOnlyList<LubyAiComboDefinition> combos = ResolveComboList();
            if (combos == null)
                return;

            string pid = _personality != null ? _personality.personalityId : null;
            for (int c = 0; c < combos.Count; c++)
            {
                LubyAiComboDefinition combo = combos[c];
                if (combo == null || !combo.Matches(pid, _traits))
                    continue;
                IReadOnlyList<LubyWeightedBehaviorEntry> entries = combo.behaviors;
                if (entries == null)
                    continue;

                for (int i = 0; i < entries.Count; i++)
                {
                    LubyWeightedBehaviorEntry e = entries[i];
                    if (e == null || e.behavior == null || e.weight <= 0f)
                        continue;
                    if (!IsSelectable(e.behavior, currentId, forceIgnoreRepeatLock))
                        continue;

                    int existing = IndexOfCandidate(e.behavior.behaviorId);
                    if (existing >= 0)
                        _candidateWeights[existing] += e.weight;
                    else
                    {
                        _candidates.Add(e.behavior);
                        _candidateWeights.Add(e.weight);
                    }
                }
            }
        }

        private int IndexOfCandidate(string behaviorId)
        {
            if (string.IsNullOrEmpty(behaviorId))
                return -1;
            for (int i = 0; i < _candidates.Count; i++)
            {
                PetBehaviorDefinition def = _candidates[i];
                if (def != null && def.behaviorId == behaviorId)
                    return i;
            }

            return -1;
        }

        private bool IsSelectable(
            PetBehaviorDefinition def,
            string currentId,
            bool forceIgnoreRepeatLock)
        {
            if (def == null)
                return false;
            if (IsOnCooldown(def))
                return false;
            if (!forceIgnoreRepeatLock &&
                aiGroup != null &&
                aiGroup.avoidImmediateRepeat &&
                !string.IsNullOrEmpty(currentId) &&
                def.behaviorId == currentId)
            {
                return false;
            }

            return def.CanEnter(_context);
        }

        private float GetTraitGroupAdd(string behaviorId)
        {
            if (_traits.Count == 0 || string.IsNullOrEmpty(behaviorId))
                return 0f;

            float add = 0f;
            for (int t = 0; t < _traits.Count; t++)
            {
                LubyTraitDefinition trait = _traits[t];
                if (trait?.groupWeightAdds == null)
                    continue;

                for (int i = 0; i < trait.groupWeightAdds.Length; i++)
                {
                    LubyBehaviorWeightAdd m = trait.groupWeightAdds[i];
                    PetBehaviorDefinition target = m.behavior;
                    if (target == null || string.IsNullOrEmpty(target.behaviorId))
                        continue;
                    if (target.behaviorId != behaviorId)
                        continue;
                    add += m.weight;
                }
            }

            return add;
        }

        private bool HasAnyTraitBehaviors()
        {
            for (int t = 0; t < _traits.Count; t++)
            {
                LubyTraitDefinition trait = _traits[t];
                if (trait?.behaviors != null && trait.behaviors.Count > 0)
                    return true;
            }

            return false;
        }

        private bool HasAnyMatchingComboBehaviors()
        {
            IReadOnlyList<LubyAiComboDefinition> combos = ResolveComboList();
            if (combos == null)
                return false;
            string pid = _personality != null ? _personality.personalityId : null;
            for (int c = 0; c < combos.Count; c++)
            {
                LubyAiComboDefinition combo = combos[c];
                if (combo == null || !combo.Matches(pid, _traits) || combo.behaviors == null)
                    continue;
                for (int i = 0; i < combo.behaviors.Count; i++)
                {
                    if (combo.behaviors[i]?.behavior != null)
                        return true;
                }
            }

            return false;
        }

        private static IReadOnlyList<LubyAiComboDefinition> ResolveComboList()
        {
            LubyTemplateCatalog catalog = DesktopPetServices.LubyWorld != null
                ? DesktopPetServices.LubyWorld.Catalog
                : LubyTemplateCatalog.LoadDefault();
            return catalog != null ? catalog.aiCombos : null;
        }

        private PetBehaviorDefinition FindBehaviorById(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            PetBehaviorDefinition def = aiGroup?.FindById(id);
            if (def != null)
                return def;

            for (int t = 0; t < _traits.Count; t++)
            {
                IReadOnlyList<LubyWeightedBehaviorEntry> entries = _traits[t]?.behaviors;
                if (entries == null)
                    continue;

                for (int i = 0; i < entries.Count; i++)
                {
                    LubyWeightedBehaviorEntry e = entries[i];
                    if (e?.behavior != null &&
                        string.Equals(e.behavior.behaviorId, id, System.StringComparison.Ordinal))
                    {
                        return e.behavior;
                    }
                }
            }

            IReadOnlyList<LubyAiComboDefinition> combos = ResolveComboList();
            if (combos != null)
            {
                string pid = _personality != null ? _personality.personalityId : null;
                for (int c = 0; c < combos.Count; c++)
                {
                    LubyAiComboDefinition combo = combos[c];
                    if (combo == null || !combo.Matches(pid, _traits) || combo.behaviors == null)
                        continue;
                    for (int i = 0; i < combo.behaviors.Count; i++)
                    {
                        LubyWeightedBehaviorEntry e = combo.behaviors[i];
                        if (e?.behavior != null &&
                            string.Equals(e.behavior.behaviorId, id, System.StringComparison.Ordinal))
                            return e.behavior;
                    }
                }
            }

            return null;
        }

        private void EnsureContext()
        {
            if (_context == null)
                _context = new PetBehaviorContext(agent != null ? agent : GetComponent<PetAgent>());
        }

        private void OnDisable()
        {
            ExitCurrent();
            _plannedNext = null;
        }
    }
}
