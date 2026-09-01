using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DesktopPet.Adventure
{
    /// <summary>
    /// 探险面板壳层（MainCanvas 预置）。
    /// 不在运行时创建 UI，只负责开关已有节点。
    /// </summary>
    public sealed class AdventureBoardUiController : MonoBehaviour
    {
        [Title("探险面板", "请在 MainCanvas 预挂节点")]
        [SerializeField]
        private GameObject root;

        [SerializeField]
        private Button closeButton;

        [SerializeField]
        private Button dimButton;

        [SerializeField]
        private TextMeshProUGUI titleText;

        [SerializeField]
        private TextMeshProUGUI bodyText;

        public bool IsVisible => root != null && root.activeSelf;

        private void Awake()
        {
            if (root == null)
                root = gameObject;

            DesktopPetServices.RegisterAdventureUi(this);

            if (closeButton != null)
                closeButton.onClick.AddListener(Hide);
            if (dimButton != null)
                dimButton.onClick.AddListener(Hide);

            Hide();
        }

        private void OnDestroy()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(Hide);
            if (dimButton != null)
                dimButton.onClick.RemoveListener(Hide);

            DesktopPetServices.UnregisterAdventureUi(this);
        }

        public void OpenFromBoard()
        {
            root ??= gameObject;

            if (titleText != null)
                titleText.text = "探险";

            if (bodyText != null)
            {
                LubyAdventureSystem sys = DesktopPetServices.LubyAdventure;
                bodyText.text = sys != null
                    ? sys.BuildBoardStatusText()
                    : "探险系统未就绪。\n请确认场景已挂 LubyAdventureSystem。";
            }

            root.SetActive(true);
            transform.SetAsLastSibling();
        }

        public void Hide()
        {
            root ??= gameObject;

            if (root != null)
                root.SetActive(false);
        }
    }
}
