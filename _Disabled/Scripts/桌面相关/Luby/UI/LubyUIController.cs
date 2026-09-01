using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DesktopPet.Luby
{
    /// <summary>
    /// 领养页：左轮播选盲盒 · 右详情长按抽取；壳层由 DesktopHub 管理。
    /// </summary>
    public sealed partial class LubyUIController : MonoBehaviour
    {
        [Title("Luby UI", "轮播 + 详情；长按抽取所选盲盒")]
        [FoldoutGroup("引用", expanded: true)]
        [SerializeField] private LubyAcquisitionService acquisition;

        [FoldoutGroup("页面", expanded: true)]
        [SerializeField] private TextMeshProUGUI statusText;

        [FoldoutGroup("轮播", expanded: true)]
        [SerializeField] private Transform carouselRoot;
        [SerializeField] private LubyCarouselItem carouselItemPrefab;
        [SerializeField] private Button prevButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Sprite fallbackIcon;

        [FoldoutGroup("详情")]
        [SerializeField] private GameObject detailRoot;
        [SerializeField] private Image detailIcon;
        [SerializeField] private TextMeshProUGUI detailNameText;
        [SerializeField] private TextMeshProUGUI detailDescText;
        [SerializeField] private Button rollButton;
        [SerializeField] private TextMeshProUGUI rollPriceText;
        [SerializeField] private Image rollFillImage;
        [SerializeField] private TextMeshProUGUI longPressHintText;
        [SerializeField] private float longPressSeconds = 0.65f;

        private readonly List<LubyTemplateDefinition> _templates = new List<LubyTemplateDefinition>(8);
        private readonly List<LubyCarouselItem> _items = new List<LubyCarouselItem>(8);
        private LubyTemplateCatalog catalog;
        private int _selectedIndex;
        private bool _holding;
        private float _holdTime;
        private bool _rollFiredThisHold;

        private void Awake()
        {
            ResolveRefs();
            DesktopPetServices.RegisterLubyUi(this);

            if (rollFillImage != null)
            {
                rollFillImage.type = Image.Type.Filled;
                rollFillImage.fillMethod = Image.FillMethod.Horizontal;
                rollFillImage.fillAmount = 0f;
            }

            DesktopPetDetailLayout.Stabilize(
                detailIcon, detailNameText, detailDescText,
                previewHeight: 148f, descMinHeight: 48f, detailFlexibleWidth: 0.9f);
        }

        private void OnDestroy()
        {
            DesktopPetServices.UnregisterLubyUi(this);
        }

        private void OnEnable()
        {
            WireButton(prevButton, SelectPrev);
            WireButton(nextButton, SelectNext);
            WireLongPress(rollButton);
        }

        private void OnDisable()
        {
            ClearButton(prevButton);
            ClearButton(nextButton);
            ClearButton(rollButton);
            UnwireLongPress(rollButton);
            ResetHold();
        }

        private void Start()
        {
            if (longPressHintText != null)
                longPressHintText.text = "长按抽取";
        }

        private void Update()
        {
            if (!_holding || rollButton == null || !rollButton.interactable)
                return;

            _holdTime += Time.unscaledDeltaTime;
            float t = longPressSeconds > 0.01f ? Mathf.Clamp01(_holdTime / longPressSeconds) : 1f;
            if (rollFillImage != null)
                rollFillImage.fillAmount = t;

            if (!_rollFiredThisHold && t >= 1f)
            {
                _rollFiredThisHold = true;
                DoRoll();
                ResetHold();
            }
        }

        /// <summary>切走/关闭主面板时由 Hub 调用，取消长按抽取。</summary>
        public void OnPageHidden() => ResetHold();

        public void OnPageShown()
        {
            DesktopPetDetailLayout.Stabilize(
                detailIcon, detailNameText, detailDescText,
                previewHeight: 148f, descMinHeight: 48f, detailFlexibleWidth: 0.9f);
            RebuildTemplateList();
            RefreshAll();
            SetStatus("选择盲盒后长按抽取");
            DesktopPetServices.HubUi?.RefreshChrome();
        }

        private void ResolveRefs()
        {
            if (acquisition == null)
                acquisition = GetComponent<LubyAcquisitionService>() ?? DesktopPetServices.LubyAcquisition;

            LubyWorld world = GetComponent<LubyWorld>() ?? DesktopPetServices.LubyWorld;
            catalog = world != null ? world.Catalog : null;
        }

        private void SetStatus(string msg)
        {
            if (statusText != null)
                statusText.text = msg;
        }

        private static void WireButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
                return;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        private static void ClearButton(Button button)
        {
            if (button != null)
                button.onClick.RemoveAllListeners();
        }
    }
}
