using DesktopPet.AI;
using DesktopPet;
using Sirenix.OdinInspector;
using UnityEngine;

namespace DesktopPet.Luby
{
    /// <summary>单只 Luby 实例：绑定模板/性格/特质，供 AI 修正与存档。</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PetAgent))]
    public sealed class LubyInstanceComponent : MonoBehaviour
    {
        [ShowInInspector, ReadOnly]
        private LubyInstanceData _data;

        [ShowInInspector, ReadOnly]
        private LubyTemplateDefinition _template;

        [ShowInInspector, ReadOnly]
        private LubyPersonalityDefinition _personality;

        [ShowInInspector, ReadOnly]
        private LubyTraitDefinition _trait;

        [ShowInInspector, ReadOnly]
        private LubyTraitDefinition _trait2;

        public LubyInstanceData Data => _data;
        public LubyTemplateDefinition Template => _template;
        public LubyPersonalityDefinition Personality => _personality;
        public LubyTraitDefinition Trait => _trait;
        public LubyTraitDefinition Trait2 => _trait2;
        public string InstanceId => _data != null ? _data.instanceId : string.Empty;
        public string PetName =>
            LubyDisplayNames.ResolvePetName(_data, DesktopPetServices.LubyWorld != null
                ? DesktopPetServices.LubyWorld.Catalog
                : null);

        /// <summary>性格 id：优先运行时 Personality，否则存档字段。</summary>
        public string ResolvePersonalityId()
        {
            if (_personality != null && !string.IsNullOrEmpty(_personality.personalityId))
                return _personality.personalityId;
            return _data != null ? _data.personalityId : null;
        }

        /// <summary>点选 / 放置重叠用的世界包围盒（本地尺寸缓存 + 随 transform 更新）。</summary>
        public Bounds PickBounds { get; private set; }

        private Vector3 _localPickCenter;
        private Vector3 _localPickSize;
        private bool _pickLocalValid;
        private Vector3 _lastPickPos;
        private Vector3 _lastPickScale;

        private PetAgent _agent;
        private LubyCoinCollector _coinCollector;

        public PetAgent Agent => _agent;
        public LubyCoinCollector CoinCollector => _coinCollector;

        private void Awake()
        {
            EnsureRuntimeRefs();
        }

        public void Initialize(
            LubyInstanceData data,
            LubyTemplateCatalog catalog,
            LubyTemplateDefinition templateOverride = null)
        {
            _data = data != null ? data.Clone() : null;
            if (_data == null || catalog == null)
                return;

            _template = templateOverride ?? catalog.FindTemplateById(_data.templateId);
            _personality = catalog.FindPersonalityById(_data.personalityId);
            _trait = catalog.FindTraitById(_data.traitId);
            _trait2 = catalog.FindTraitById(_data.traitId2);

            EnsureRuntimeRefs();
            ApplyTransform();
            ApplyAiGroup();
            RefreshPickBounds();
        }

        /// <summary>身上是否持有该特质（含第二特质）。</summary>
        public bool HasTrait(string traitId)
        {
            if (string.IsNullOrEmpty(traitId))
                return false;
            if (_trait != null && _trait.traitId == traitId)
                return true;
            if (_trait2 != null && _trait2.traitId == traitId)
                return true;
            if (_data != null)
            {
                if (_data.traitId == traitId || _data.traitId2 == traitId)
                    return true;
            }

            return false;
        }

        private void EnsureRuntimeRefs()
        {
            if (_agent == null)
                _agent = GetComponent<PetAgent>();
            if (_coinCollector == null)
                _coinCollector = GetComponent<LubyCoinCollector>();
        }

        /// <summary>外形/缩放变化后重算本地 pick 盒（昂贵，勿每帧全量扫）。</summary>
        public void RefreshPickBounds()
        {
            if (!DeskSpriteBounds.TryMeasureLocalBox(transform, out _localPickCenter, out _localPickSize))
            {
                _localPickCenter = Vector3.zero;
                _localPickSize = Vector3.one;
            }

            _pickLocalValid = true;
            ApplyWorldPickBounds();
            DeskSpriteBounds.InvalidateHead(transform);
        }

        /// <summary>移动后只按缓存本地盒更新世界 Bounds（pick / 重叠检测前调用）。</summary>
        public void EnsureWorldPickBounds()
        {
            Vector3 pos = transform.position;
            Vector3 scale = transform.localScale;
            if (!_pickLocalValid)
            {
                RefreshPickBounds();
                return;
            }

            if (pos == _lastPickPos && scale == _lastPickScale)
                return;

            ApplyWorldPickBounds();
        }

        private void ApplyWorldPickBounds()
        {
            Vector3 worldCenter = transform.TransformPoint(_localPickCenter);
            Vector3 lossy = transform.lossyScale;
            Vector3 worldSize = new Vector3(
                _localPickSize.x * Mathf.Abs(lossy.x),
                _localPickSize.y * Mathf.Abs(lossy.y),
                _localPickSize.z * Mathf.Abs(lossy.z));
            PickBounds = new Bounds(worldCenter, worldSize);
            _lastPickPos = transform.position;
            _lastPickScale = transform.localScale;
        }

        private void ApplyTransform()
        {
            if (_data == null)
                return;

            float s = 1f;
            if (_data.scale > 0.01f)
                s = _data.scale;
            else if (_template != null)
                s = _template.ResolveScale(gameObject);

            Vector3 pos = transform.position;
            pos.x = _data.x;
            transform.position = pos;
            transform.localScale = Vector3.one * s;

            PetLocomotion loco = GetComponent<PetLocomotion>();
            if (loco != null)
                loco.RefreshBaseScale();
        }

        private void ApplyAiGroup()
        {
            PetAiGroup group = _personality?.aiGroup;
            if (group == null)
            {
                Debug.LogError(
                    $"[Luby] 性格未配 aiGroup：personalityId={_data?.personalityId ?? "(null)"}",
                    this);
                return;
            }

            if (_agent == null || _agent.Brain == null)
                return;

            _agent.Brain.SetAiGroup(group, _personality, _trait, _trait2, restart: true);
        }

        public void SyncPositionToData()
        {
            if (_data == null)
                return;

            Vector3 p = transform.position;
            _data.x = p.x;
            _data.y = p.y;
            _data.scale = transform.localScale.x;
        }
    }
}
