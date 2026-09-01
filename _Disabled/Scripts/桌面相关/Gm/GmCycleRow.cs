using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DesktopPet.Gm
{
    /// <summary>GM 指定获得一行：◀ Value ▶。子节点名固定 Prev / Value / Next。</summary>
    public sealed class GmCycleRow : MonoBehaviour
    {
        [SerializeField] private Button prevButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private TextMeshProUGUI valueText;

        public Button PrevButton => prevButton;
        public Button NextButton => nextButton;
        public TextMeshProUGUI ValueText => valueText;

        private void Awake()
        {
            Resolve();
        }

        public void Resolve()
        {
            if (prevButton == null)
            {
                Transform t = transform.Find("Prev");
                if (t != null)
                    prevButton = t.GetComponent<Button>();
            }

            if (nextButton == null)
            {
                Transform t = transform.Find("Next");
                if (t != null)
                    nextButton = t.GetComponent<Button>();
            }

            if (valueText == null)
            {
                Transform t = transform.Find("Value");
                if (t != null)
                    valueText = t.GetComponent<TextMeshProUGUI>();
            }
        }

        public void Wire(UnityAction onPrev, UnityAction onNext)
        {
            Resolve();
            WireOne(prevButton, onPrev);
            WireOne(nextButton, onNext);
        }

        public void SetLabel(string text)
        {
            Resolve();
            if (valueText != null)
                valueText.text = text;
        }

        private static void WireOne(Button btn, UnityAction action)
        {
            if (btn == null || action == null)
                return;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(action);
        }
    }
}
