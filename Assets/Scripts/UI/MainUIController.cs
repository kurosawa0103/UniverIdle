using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UniverIdle.UI
{
    /// <summary>主界面运行时：技能切换、动作选中（逻辑后续接挂机系统）。</summary>
    public class MainUIController : MonoBehaviour
    {
        [Header("技能导航")]
        [SerializeField] private List<SkillNavItemView> skillItems = new();

        [Header("中部")]
        [SerializeField] private TextMeshProUGUI locationTitleText;
        [SerializeField] private List<ActionCardView> actionCards = new();
        [SerializeField] private Image progressFill;
        [SerializeField] private TextMeshProUGUI progressLabelText;
        [SerializeField] private TextMeshProUGUI progressTimeText;

        [Header("右侧详情")]
        [SerializeField] private TextMeshProUGUI detailTitleText;
        [SerializeField] private TextMeshProUGUI detailBodyText;

        [Header("顶栏")]
        [SerializeField] private TextMeshProUGUI goldText;

        private int activeSkillIndex = 2; // 溪钓
        private int activeActionIndex;

        private void Start()
        {
            SelectSkill(activeSkillIndex);
            if (actionCards.Count > 0)
                SelectAction(0);
        }

        public void SelectSkill(int index)
        {
            if (index < 0 || index >= skillItems.Count) return;
            activeSkillIndex = index;

            for (var i = 0; i < skillItems.Count; i++)
                skillItems[i].SetSelected(i == index);

            var skill = skillItems[index];
            if (locationTitleText != null)
                locationTitleText.text = skill.LocationName;

            RefreshActionSelection();
        }

        public void SelectAction(int index)
        {
            if (index < 0 || index >= actionCards.Count) return;
            if (actionCards[index].IsLocked) return;

            activeActionIndex = index;
            for (var i = 0; i < actionCards.Count; i++)
                actionCards[i].SetSelected(i == index);

            var card = actionCards[index];
            if (detailTitleText != null)
                detailTitleText.text = card.DisplayName;
            if (detailBodyText != null)
                detailBodyText.text = card.Description;
            if (progressLabelText != null)
                progressLabelText.text = "进行中 · " + card.DisplayName;
            if (progressFill != null)
                progressFill.fillAmount = 0.62f;
            if (progressTimeText != null)
                progressTimeText.text = "00:06";
        }

        private void RefreshActionSelection()
        {
            var valid = activeActionIndex < actionCards.Count && !actionCards[activeActionIndex].IsLocked;
            SelectAction(valid ? activeActionIndex : FindFirstUnlockedAction());
        }

        private int FindFirstUnlockedAction()
        {
            for (var i = 0; i < actionCards.Count; i++)
                if (!actionCards[i].IsLocked)
                    return i;
            return 0;
        }

        public void BindSkillButton(int index, Button button)
        {
            var captured = index;
            button.onClick.AddListener(() => SelectSkill(captured));
        }

        public void BindActionCard(int index, Button button)
        {
            var captured = index;
            button.onClick.AddListener(() => SelectAction(captured));
        }

#if UNITY_EDITOR
        public void SetReferences(
            List<SkillNavItemView> skills,
            TextMeshProUGUI locationTitle,
            List<ActionCardView> actions,
            Image progress,
            TextMeshProUGUI progressLabel,
            TextMeshProUGUI progressTime,
            TextMeshProUGUI detailTitle,
            TextMeshProUGUI detailBody,
            TextMeshProUGUI gold)
        {
            skillItems = skills;
            locationTitleText = locationTitle;
            actionCards = actions;
            progressFill = progress;
            progressLabelText = progressLabel;
            progressTimeText = progressTime;
            detailTitleText = detailTitle;
            detailBodyText = detailBody;
            goldText = gold;
        }
#endif
    }
}
