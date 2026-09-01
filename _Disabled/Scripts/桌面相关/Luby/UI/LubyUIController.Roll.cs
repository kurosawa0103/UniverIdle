using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DesktopPet.Luby
{
    public sealed partial class LubyUIController
    {
        private void DoRoll()
        {
            if (acquisition == null)
            {
                SetStatus("缺少抽取服务");
                return;
            }

            LubyRollResult result;
            bool ok = acquisition.TryRollTemplate(GetSelectedTemplate(), out result);

            if (ok)
            {
                string name = LubyDisplayNames.ResolvePetName(result.instance, null);
                string p = result.personality != null ? result.personality.displayName : "—";
                string t = LubyTraitDisplay.FormatNames(result.trait, result.trait2);
                string look = !string.IsNullOrEmpty(result.instance?.appearanceKey)
                    ? result.instance.appearanceKey
                    : "—";
                if (result.sentToWarehouse)
                    SetStatus($"桌上已满({DesktopPetServices.LubyWorld?.DeskCapacity ?? 3})，已进仓库｜{name}｜{look}｜{p}｜{t}｜-{result.pricePaid}");
                else
                    SetStatus($"获得 {name}｜{look}｜{p}｜{t}｜-{result.pricePaid}");
            }
            else
            {
                SetStatus(result.FailMessage);
            }

            DesktopPetServices.HubUi?.RefreshChrome();
        }

        private void WireLongPress(Button button)
        {
            if (button == null)
                return;

            EventTrigger trigger = button.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                Debug.LogError(
                    "[LubyUI] 抽取按钮缺少 EventTrigger。请在 MainCanvas.prefab 的 RollBtn 上预挂后再「应用主面板」。",
                    button);
                return;
            }

            trigger.triggers.Clear();
            AddTrigger(trigger, EventTriggerType.PointerDown, _ => BeginHold());
            AddTrigger(trigger, EventTriggerType.PointerUp, _ => ResetHold());
            AddTrigger(trigger, EventTriggerType.PointerExit, _ => ResetHold());
        }

        private void UnwireLongPress(Button button)
        {
            if (button == null)
                return;
            EventTrigger trigger = button.GetComponent<EventTrigger>();
            if (trigger != null)
                trigger.triggers.Clear();
        }

        private static void AddTrigger(EventTrigger trigger, EventTriggerType type, System.Action<BaseEventData> cb)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(e => cb(e));
            trigger.triggers.Add(entry);
        }

        private void BeginHold()
        {
            if (rollButton == null || !rollButton.interactable)
                return;
            _holding = true;
            _holdTime = 0f;
            _rollFiredThisHold = false;
            if (rollFillImage != null)
                rollFillImage.fillAmount = 0f;
        }

        private void ResetHold()
        {
            _holding = false;
            _holdTime = 0f;
            _rollFiredThisHold = false;
            if (rollFillImage != null)
                rollFillImage.fillAmount = 0f;
        }
    }
}
