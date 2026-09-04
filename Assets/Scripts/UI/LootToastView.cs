using System.Collections.Generic;
using TMPro;
using UniverIdle.Game;
using UnityEngine;

namespace UniverIdle.UI
{
  /// <summary>获得提示容器：运行时从行预制体实例化最多 3 行；飘字单独预制体。</summary>
  public sealed class LootToastView : MonoBehaviour
  {
    private const int MaxLines = 3;
    private const int MaxFloaters = 12;

    [SerializeField] private float defaultDuration = 3.5f;
    [SerializeField] private float flyDuration = 0.65f;
    [SerializeField] private float flyDistance = 32f;
    [SerializeField] private float lineStepY = 42f;
    [SerializeField] private RectTransform lineRoot;
    [SerializeField] private LootToastLineView linePrefab;
    [SerializeField] private RectTransform floatLayer;
    [SerializeField] private TextMeshProUGUI floaterPrefab;

    private sealed class LineState
    {
      public LootToastLineView View;
      public string ItemId;
      public int Gained;
      public long Total;
      public float Remaining;
      public bool Active;
      public bool IsItem;
      public bool IsGold;
      public bool IsXp;
    }

    private sealed class Floater
    {
      public GameObject Root;
      public RectTransform Rect;
      public TextMeshProUGUI Label;
      public Vector2 StartAnchored;
      public float Elapsed;
      public Color BaseColor;
    }

    private readonly LineState[] _lines = new LineState[MaxLines];
    private readonly List<Floater> _floaters = new();
    private readonly Stack<Floater> _floaterPool = new();
    private LootToastLineView[] _lineSlots;
    private bool _wired;
    /// <summary>本轮挂机累计经验；停机后清零。</summary>
    private long _sessionXpGained;

    private void Awake() => WireLines();

    private void Update()
    {
      if (!_wired) return;

      var dt = Time.deltaTime;
      for (var i = 0; i < _lines.Length; i++)
      {
        var line = _lines[i];
        if (!line.Active) continue;
        line.Remaining -= dt;
        if (line.Remaining <= 0f)
          Hide(line);
      }

      TickFloaters(dt);
    }

    public void PushItem(string itemId, int gained, long totalOwned)
    {
      if (string.IsNullOrEmpty(itemId) || gained <= 0) return;
      if (!_wired) WireLines();
      if (!_wired) return;

      var existing = FindActiveItemLine(itemId);
      if (existing != null)
      {
        existing.Gained += gained;
        existing.Total = totalOwned;
        existing.Remaining = defaultDuration;
        existing.View.Root.SetActive(true);
        RefreshGainLine(existing);
        SpawnGainFloater(existing, gained);
        return;
      }

      var line = TakeSlot();
      line.IsItem = true;
      line.IsGold = false;
      line.IsXp = false;
      line.ItemId = itemId;
      line.Gained = gained;
      line.Total = totalOwned;
      line.Remaining = defaultDuration;
      line.Active = true;
      line.View.Root.SetActive(true);
      RefreshGainLine(line);
      SpawnGainFloater(line, gained);
    }

    public void PushGold(int gained, long totalOwned)
    {
      if (gained <= 0) return;
      if (!_wired) WireLines();
      if (!_wired) return;

      var existing = FindActiveGoldLine();
      if (existing != null)
      {
        existing.Gained += gained;
        existing.Total = totalOwned;
        existing.Remaining = defaultDuration;
        existing.View.Root.SetActive(true);
        RefreshGainLine(existing);
        SpawnGainFloater(existing, gained);
        return;
      }

      var line = TakeSlot();
      line.IsItem = false;
      line.IsGold = true;
      line.IsXp = false;
      line.ItemId = null;
      line.Gained = gained;
      line.Total = totalOwned;
      line.Remaining = defaultDuration;
      line.Active = true;
      line.View.Root.SetActive(true);
      RefreshGainLine(line);
      SpawnGainFloater(line, gained);
    }

    /// <summary>经验行：左侧 +本次叠加入账；右侧为本轮挂机累计（停机清零）。</summary>
    public void PushXp(int gained)
    {
      if (gained <= 0) return;
      if (!_wired) WireLines();
      if (!_wired) return;

      _sessionXpGained += gained;

      var existing = FindActiveXpLine();
      if (existing != null)
      {
        existing.Gained += gained;
        existing.Total = _sessionXpGained;
        existing.Remaining = defaultDuration;
        existing.View.Root.SetActive(true);
        RefreshGainLine(existing);
        SpawnGainFloater(existing, gained);
        return;
      }

      var line = TakeSlot();
      line.IsItem = false;
      line.IsGold = false;
      line.IsXp = true;
      line.ItemId = null;
      line.Gained = gained;
      line.Total = _sessionXpGained;
      line.Remaining = defaultDuration;
      line.Active = true;
      line.View.Root.SetActive(true);
      RefreshGainLine(line);
      SpawnGainFloater(line, gained);
    }

    /// <summary>停机后重置本轮经验累计，下次开工右侧从 0 再累加。</summary>
    public void ResetSessionXp() => _sessionXpGained = 0;

    public void PushText(string message)
    {
      if (string.IsNullOrWhiteSpace(message)) return;
      if (!_wired) WireLines();
      if (!_wired) return;

      var line = TakeSlot();
      line.IsItem = false;
      line.IsGold = false;
      line.IsXp = false;
      line.ItemId = null;
      line.Gained = 0;
      line.Total = 0;
      line.Remaining = defaultDuration;
      line.Active = true;
      line.View.Root.SetActive(true);
      RefreshTextLine(line, message.Trim());
    }

    /// <summary>一次动作结算的全局获得提示（任意工作共用）。</summary>
    public void PushResult(ActionCompleteResult result, PlayerState player)
    {
      if (result?.Action == null) return;

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
        PushText(EmptyLootLine(result.Action.WorkId));
      else
      {
        if (hasLoot)
        {
          for (var i = 0; i < result.Loot.Count; i++)
          {
            var drop = result.Loot[i];
            if (LootRules.IsEmpty(drop.ItemId)) continue;
            var total = player != null ? player.GetItemCount(drop.ItemId) : drop.Amount;
            PushItem(drop.ItemId, drop.Amount, total);
          }
        }

        if (hasGold)
        {
          var goldTotal = player != null ? player.Gold : result.GoldGained;
          PushGold(result.GoldGained, goldTotal);
        }
      }

      if (result.XpGained > 0 && player != null)
      {
        var work = GameContent.GetWork(result.Action.WorkId);
        if (work != null && work.GrantWorkXp)
          PushXp(result.XpGained);
      }

      if (result.BagFull)
        PushText("背包已满，装不下新道具。");

      if (result.WorkLeveledUp)
      {
        var work = GameContent.GetWork(result.Action.WorkId);
        var workName = work != null ? work.DisplayName : "工作";
        PushText($"{workName}总等级升至 Lv.{result.WorkNewLevel}！");
      }

      if (result.ActionMasteryLeveledUp)
      {
        var scene = string.IsNullOrEmpty(result.SceneName) ? "本地点" : result.SceneName;
        var spot = result.Action.SpotName;
        var label = string.IsNullOrEmpty(spot) ? scene : spot;
        PushText($"{label}熟练度升至 Lv.{result.ActionMasteryNewLevel}！");
      }
    }

    private static string EmptyLootLine(string workId) =>
      workId switch
      {
        "woodcutting" => "这次没砍下原木。",
        "scavenge" => "这次什么也没捡到。",
        _ => "这次什么也没有。"
      };

    private void WireLines()
    {
      if (_wired) return;
      EnsureLineSlots();

      if (_lineSlots == null || _lineSlots.Length == 0)
      {
        Debug.LogWarning("[UniverIdle] LootToastView 未绑定 linePrefab。");
        return;
      }

      var count = Mathf.Min(MaxLines, _lineSlots.Length);
      for (var i = 0; i < count; i++)
      {
        _lines[i] = new LineState { View = _lineSlots[i] };
        _lines[i].View.Root.SetActive(false);
      }

      _wired = true;
    }

    private void EnsureLineSlots()
    {
      if (lineRoot == null || linePrefab == null) return;

      for (var i = lineRoot.childCount - 1; i >= 0; i--)
        Destroy(lineRoot.GetChild(i).gameObject);

      var templateRt = linePrefab.transform as RectTransform;
      _lineSlots = new LootToastLineView[MaxLines];
      for (var i = 0; i < MaxLines; i++)
      {
        var line = Instantiate(linePrefab, lineRoot);
        line.name = $"Toast_{i + 1}";
        if (templateRt != null)
        {
          var rt = (RectTransform)line.transform;
          rt.anchorMin = templateRt.anchorMin;
          rt.anchorMax = templateRt.anchorMax;
          rt.pivot = templateRt.pivot;
          rt.sizeDelta = templateRt.sizeDelta;
          rt.anchoredPosition = templateRt.anchoredPosition + new Vector2(0f, lineStepY * i);
        }
        line.gameObject.SetActive(false);
        _lineSlots[i] = line;
      }
    }

    private void TickFloaters(float dt)
    {
      for (var i = _floaters.Count - 1; i >= 0; i--)
      {
        var floater = _floaters[i];
        floater.Elapsed += dt;
        var t = Mathf.Clamp01(floater.Elapsed / flyDuration);
        var eased = 1f - (1f - t) * (1f - t);
        floater.Rect.anchoredPosition = floater.StartAnchored + new Vector2(0f, flyDistance * eased);
        floater.Label.color = new Color(
          floater.BaseColor.r,
          floater.BaseColor.g,
          floater.BaseColor.b,
          1f - eased);

        if (t < 1f) continue;
        ReturnFloater(floater);
        _floaters.RemoveAt(i);
      }
    }

    private void SpawnGainFloater(LineState line, int gained)
    {
      if (line?.View?.GainText == null || floatLayer == null) return;

      var floater = RentFloater();
      if (floater == null) return;
      floater.Elapsed = 0f;
      floater.Label.text = $"+{gained}";
      floater.BaseColor = UITheme.TealBright;
      floater.Label.color = floater.BaseColor;

      var gainRt = line.View.GainText.rectTransform;
      var world = gainRt.TransformPoint(gainRt.rect.center);
      floater.Rect.SetParent(floatLayer, false);
      floater.Rect.position = world;
      floater.Rect.localScale = Vector3.one;
      floater.StartAnchored = floater.Rect.anchoredPosition;
      floater.Root.SetActive(true);
      _floaters.Add(floater);
    }

    private Floater RentFloater()
    {
      if (_floaterPool.Count > 0)
        return _floaterPool.Pop();

      if (floaterPrefab == null)
      {
        Debug.LogWarning("[UniverIdle] LootToastView 未绑定 floaterPrefab。");
        return null;
      }

      var label = Instantiate(floaterPrefab, floatLayer);
      label.gameObject.name = "GainFloater";
      label.raycastTarget = false;
      return new Floater
      {
        Root = label.gameObject,
        Rect = label.rectTransform,
        Label = label,
      };
    }

    private void ReturnFloater(Floater floater)
    {
      if (floater?.Root == null) return;
      floater.Root.SetActive(false);
      if (_floaterPool.Count < MaxFloaters)
        _floaterPool.Push(floater);
      else
        Destroy(floater.Root);
    }

    private LineState FindActiveGoldLine()
    {
      for (var i = 0; i < _lines.Length; i++)
      {
        var line = _lines[i];
        if (line?.View == null) continue;
        if (line.Active && line.IsGold)
          return line;
      }
      return null;
    }

    private LineState FindActiveXpLine()
    {
      for (var i = 0; i < _lines.Length; i++)
      {
        var line = _lines[i];
        if (line?.View == null) continue;
        if (line.Active && line.IsXp)
          return line;
      }
      return null;
    }

    private LineState FindActiveItemLine(string itemId)
    {
      for (var i = 0; i < _lines.Length; i++)
      {
        var line = _lines[i];
        if (line?.View == null) continue;
        if (line.Active && line.IsItem && line.ItemId == itemId)
          return line;
      }
      return null;
    }

    private LineState TakeSlot()
    {
      for (var i = 0; i < _lines.Length; i++)
      {
        if (_lines[i]?.View == null) continue;
        if (!_lines[i].Active)
          return _lines[i];
      }

      var pick = 0;
      var min = _lines[0].Remaining;
      for (var i = 1; i < _lines.Length; i++)
      {
        if (_lines[i].Remaining >= min) continue;
        min = _lines[i].Remaining;
        pick = i;
      }
      return _lines[pick];
    }

    private void Hide(LineState line)
    {
      line.Active = false;
      line.IsItem = false;
      line.IsGold = false;
      line.IsXp = false;
      line.ItemId = null;
      line.Gained = 0;
      line.Total = 0;
      line.Remaining = 0f;
      line.View.Root.SetActive(false);
    }

    private void RefreshGainLine(LineState line)
    {
      var view = line.View;
      ResolveGainIcon(line, out var sprite, out var fallbackColor);
      if (view.Icon != null)
      {
        view.Icon.gameObject.SetActive(true);
        if (sprite != null)
        {
          view.Icon.sprite = sprite;
          view.Icon.color = Color.white;
        }
        else
        {
          view.Icon.sprite = null;
          view.Icon.color = fallbackColor;
        }
      }

      if (view.GainText != null)
      {
        view.GainText.gameObject.SetActive(true);
        view.GainText.text = $"+{line.Gained}";
      }

      if (view.TotalText != null)
      {
        view.TotalText.gameObject.SetActive(true);
        view.TotalText.text = line.Total.ToString();
      }

      if (view.MessageText != null)
        view.MessageText.gameObject.SetActive(false);

      view.RefreshLayout();
      view.PunchGainNumbers();
    }

    private static void ResolveGainIcon(LineState line, out Sprite sprite, out Color fallbackColor)
    {
      if (line.IsGold)
      {
        sprite = ItemIconLoader.GetGold();
        fallbackColor = UITheme.Gold;
        return;
      }

      if (line.IsXp)
      {
        sprite = ItemIconLoader.GetXp();
        fallbackColor = UITheme.TealBright;
        return;
      }

      var item = GameContent.GetItem(line.ItemId);
      sprite = ItemIconLoader.Get(item);
      fallbackColor = UITheme.Muted;
    }

    private void RefreshTextLine(LineState line, string message)
    {
      var view = line.View;
      if (view.Icon != null) view.Icon.gameObject.SetActive(false);
      if (view.GainText != null) view.GainText.gameObject.SetActive(false);
      if (view.TotalText != null) view.TotalText.gameObject.SetActive(false);
      if (view.MessageText != null)
      {
        view.MessageText.gameObject.SetActive(true);
        view.MessageText.text = message;
      }

      view.RefreshLayout();
    }
  }
}
