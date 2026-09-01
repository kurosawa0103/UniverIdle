using DesktopPet.Save;
using Sirenix.OdinInspector;
using UnityEngine;

namespace DesktopPet.Background
{
    /// <summary>
    /// 背景切换运行时。
    /// 每个背景是一个预制体（底图 + 可选 PlaceSurface 等），切换时 Instantiate 新的、Destroy 旧的。
    /// 透明背景（transparent）不生成预制体，透出 Windows 桌面。
    /// </summary>
    public sealed class BackgroundSystem : MonoBehaviour
    {
        [Title("背景系统")]

        [BoxGroup("引用")]
        [LabelText("背景根节点")]
        [Tooltip("背景预制体实例化到此 Transform 下。场景预挂。")]
        [SerializeField]
        private Transform backgroundRoot;

        [BoxGroup("引用")]
        [LabelText("背景目录")]
        [SerializeField]
        private BackgroundCatalog catalog;

        [BoxGroup("运行时")]
        [ShowInInspector, ReadOnly, LabelText("当前背景 ID")]
        public string CurrentBackgroundId { get; private set; } = BackgroundDefinition.TransparentId;

        /// <summary>是否已执行过 ApplyBackground（Start 前 Instance 可能已在，但 ID 仍是默认值）。</summary>
        public bool HasAppliedBackground { get; private set; }

        public BackgroundCatalog Catalog => catalog;
        public static BackgroundSystem Instance { get; private set; }

        /// <summary>切换背景后触发（参数 = 新 backgroundId）。</summary>
        public event System.Action<string> BackgroundChanged;

        /// <summary>当前背景预制体实例（透明时为 null）。</summary>
        private GameObject _currentInstance;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (catalog == null)
                catalog = BackgroundCatalog.LoadDefault();

            if (backgroundRoot == null)
                Debug.LogError("[BackgroundSystem] 未绑定 backgroundRoot。请在场景预挂并拖到字段。", this);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Start()
        {
            string savedId = BackgroundCatalog.ResolveActiveId(
                DesktopPetSaveMgr.Current?.currentBackgroundId, catalog);
            ApplyBackground(savedId);
        }

        /// <summary>切换背景预制体（不写档，存档由 PersistActive 统一处理）。</summary>
        public void ApplyBackground(string backgroundId)
        {
            BackgroundDefinition def = null;
            if (backgroundId != BackgroundDefinition.TransparentId && catalog != null)
                def = catalog.FindById(backgroundId);

            CurrentBackgroundId = (def != null) ? def.backgroundId : BackgroundDefinition.TransparentId;
            HasAppliedBackground = true;

            if (_currentInstance != null)
            {
                Destroy(_currentInstance);
                _currentInstance = null;
            }

            if (def != null && def.backgroundPrefab != null)
            {
                Transform parent = backgroundRoot != null ? backgroundRoot : transform;
                _currentInstance = Instantiate(def.backgroundPrefab, parent);
                _currentInstance.name = $"Background_{def.backgroundId}";
            }

            BackgroundChanged?.Invoke(CurrentBackgroundId);
        }
    }
}
