using System.Text;
using TMPro;
using UniverIdle.Game;
using UnityEngine;

namespace UniverIdle.UI
{
  /// <summary>
  /// 工作页右侧详情（通用）：标题、正文、掉落预览、获得提示。
  /// 不含开始/停止按钮；砍树等动作列表工作用本组件。
  /// </summary>
  public class WorkActionDetailView : MonoBehaviour
  {
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private LootPreviewView lootPreview;
    [SerializeField] private LootToastView lootToast;
    [SerializeField] private LootToastLineView lootLinePrefab;
    [SerializeField] private TextMeshProUGUI lootFloaterPrefab;

    protected virtual void Awake()
    {
      if (lootPreview == null)
        lootPreview = GetComponentInChildren<LootPreviewView>(true);
      EnsureLootToast();
    }

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
      EnsureLootToast();
      PushLootToasts(result, player);
      lootPreview?.RevealLoot(result);
    }

    private void PushLootToasts(ActionCompleteResult result, PlayerState player)
    {
      if (lootToast == null) return;

      var hasLoot = false;
      if (result.Loot != null)
      {
        for (var i = 0; i < result.Loot.Count; i++)
        {
          if (LootRules.IsEmpty(result.Loot[i].ItemId)) continue;
          hasLoot = true;
          break;
        }
      }
      var hasGold = result.GoldGained > 0;
      if (!hasLoot && !hasGold)
        lootToast.PushText(EmptyLootLine(result.Action.WorkId));
      else
      {
        if (hasLoot)
        {
          for (var i = 0; i < result.Loot.Count; i++)
          {
            var drop = result.Loot[i];
            if (LootRules.IsEmpty(drop.ItemId)) continue;
            var total = player != null ? player.GetItemCount(drop.ItemId) : drop.Amount;
            lootToast.PushItem(drop.ItemId, drop.Amount, total);
          }
        }

        if (hasGold)
        {
          var goldTotal = player != null ? player.Gold : result.GoldGained;
          lootToast.PushGold(result.GoldGained, goldTotal);
        }
      }

      if (result.BagFull)
        lootToast.PushText("背包已满，装不下新道具。");

      if (result.WorkLeveledUp)
      {
        var work = GameContent.GetWork(result.Action.WorkId);
        var workName = work != null ? work.DisplayName : "工作";
        lootToast.PushText($"{workName}升至 Lv.{result.WorkNewLevel}！");
      }

      if (result.LeveledUp)
      {
        var scene = string.IsNullOrEmpty(result.SceneName) ? "本地区" : result.SceneName;
        lootToast.PushText($"{scene}熟练度升至 Lv.{result.NewLevel}！");
      }
    }

    protected void EnsureLootToast()
    {
      if (lootToast == null)
      {
        var host = transform.Find("获得提示区");
        if (host != null)
          lootToast = host.GetComponent<LootToastView>();
      }

      if (lootToast == null)
        lootToast = CreateLootToastHost();

      lootToast?.BindPrefabs(lootLinePrefab, lootFloaterPrefab);
    }

    private LootToastView CreateLootToastHost()
    {
      var go = new GameObject("获得提示区", typeof(RectTransform), typeof(LootToastView));
      go.transform.SetParent(transform, false);
      go.transform.SetAsLastSibling();

      var rt = (RectTransform)go.transform;
      rt.anchorMin = new Vector2(0f, 0f);
      rt.anchorMax = new Vector2(1f, 0f);
      rt.pivot = new Vector2(0.5f, 0f);
      rt.anchoredPosition = new Vector2(0f, 12f);
      rt.sizeDelta = new Vector2(-24f, 108f);

      var lines = new GameObject("Lines", typeof(RectTransform));
      lines.transform.SetParent(go.transform, false);
      var linesRt = (RectTransform)lines.transform;
      linesRt.anchorMin = Vector2.zero;
      linesRt.anchorMax = Vector2.one;
      linesRt.offsetMin = Vector2.zero;
      linesRt.offsetMax = Vector2.zero;

      var floatLayer = new GameObject("FloatLayer", typeof(RectTransform));
      floatLayer.transform.SetParent(go.transform, false);
      var floatRt = (RectTransform)floatLayer.transform;
      floatRt.anchorMin = Vector2.zero;
      floatRt.anchorMax = Vector2.one;
      floatRt.offsetMin = Vector2.zero;
      floatRt.offsetMax = Vector2.zero;

      return go.GetComponent<LootToastView>();
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

    private static string EmptyLootLine(string workId)
    {
      return workId switch
      {
        "woodcutting" => "这次没砍下原木。",
        "scavenge" => "这次什么也没捡到。",
        _ => "这次什么也没有。"
      };
    }
  }
}
