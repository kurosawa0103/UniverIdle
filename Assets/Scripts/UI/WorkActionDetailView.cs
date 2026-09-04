using System.Text;
using TMPro;
using UniverIdle.Game;
using UnityEngine;

namespace UniverIdle.UI
{
  /// <summary>
  /// 工作页右侧详情（通用）：标题、正文、掉落预览。
  /// 获得提示由主界面全局 <see cref="LootToastView"/> 处理。
  /// </summary>
  public class WorkActionDetailView : MonoBehaviour
  {
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private LootPreviewView lootPreview;

    public virtual void ShowAction(WorkActionDefinition action, PlayerState player, bool revealGuaranteedLoot = false)
    {
      if (action == null) return;
      var work = GameContent.GetWork(action.WorkId);
      if (titleText != null)
        titleText.text = action.DisplayName;
      if (bodyText != null)
        bodyText.text = BuildDetailBody(action, player, work);
      lootPreview?.Bind(action, revealGuaranteedLoot);
    }

    public virtual void ShowStopped(WorkActionDefinition action, PlayerState player)
    {
      if (action == null) return;
      var work = GameContent.GetWork(action.WorkId);
      if (bodyText != null)
        bodyText.text = BuildDetailBody(action, player, work) + "\n\n请补充道具后重新开始。";
    }

    public virtual void OnActionCompleted(ActionCompleteResult result, PlayerState player)
    {
      if (result?.Action == null) return;
      lootPreview?.RevealLoot(result);
    }

    protected static string BuildDetailBody(WorkActionDefinition action, PlayerState player, WorkDefinition work)
    {
      if (action == null) return string.Empty;

      var sb = new StringBuilder();
      if (!string.IsNullOrWhiteSpace(action.Description))
        sb.Append(action.Description.Trim());
      else
        sb.Append("暂无描述。");

      var workName = work != null ? work.DisplayName : "工作";
      if (player != null && !SceneProgressRules.IsRegionUnlocked(player, action))
      {
        sb.Append("\n\n").Append(SceneProgressRules.FormatUnlockHint(action, workName));
        return sb.ToString();
      }

      if (action.HasCost && player != null)
      {
        var costItem = GameContent.GetItem(action.CostItemId);
        var costName = costItem != null ? costItem.DisplayName : action.CostItemId;
        var owned = player.GetItemCount(action.CostItemId);
        sb.Append($"\n\n每次消耗：{costName} ×{action.CostAmount}（持有 {owned}）");
        if (!SceneProgressRules.CanAffordCost(player, action))
          sb.Append("\n").Append(SceneProgressRules.FormatCostHint(action));
      }

      AppendGuaranteedLoot(sb, action);
      return sb.ToString();
    }

    private static void AppendGuaranteedLoot(StringBuilder sb, WorkActionDefinition action)
    {
      if (action?.LootTable == null || action.LootTable.Count == 0) return;

      var first = true;
      for (var i = 0; i < action.LootTable.Count; i++)
      {
        var entry = action.LootTable[i];
        if (LootRules.IsEmpty(entry.ItemId) || !Mathf.Approximately(entry.Chance, 1f))
          continue;
        var item = GameContent.GetItem(entry.ItemId);
        var name = item != null ? item.DisplayName : entry.ItemId;
        var amount = entry.MinAmount == entry.MaxAmount
          ? $"×{entry.MinAmount}"
          : $"×{entry.MinAmount}-{entry.MaxAmount}";
        if (first)
        {
          sb.Append("\n\n必定获得：");
          first = false;
        }
        else
          sb.Append("、");
        sb.Append(name).Append(amount);
      }
    }
  }
}
