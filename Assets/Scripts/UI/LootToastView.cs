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

    private void Awake()
    {
      ResolveReferences();
      WireLines();
    }

    public void BindPrefabs(LootToastLineView line, TextMeshProUGUI floater)
    {
      if (line != null) linePrefab = line;
      if (floater != null) floaterPrefab = floater;
      if (_wired) return;
      WireLines();
    }

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
        RefreshItemLine(existing);
        SpawnGainFloater(existing, gained);
        return;
      }

      var line = TakeSlot();
      line.IsItem = true;
      line.IsGold = false;
      line.ItemId = itemId;
      line.Gained = gained;
      line.Total = totalOwned;
      line.Remaining = defaultDuration;
      line.Active = true;
      line.View.Root.SetActive(true);
      RefreshItemLine(line);
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
        RefreshGoldLine(existing);
        SpawnGainFloater(existing, gained);
        return;
      }

      var line = TakeSlot();
      line.IsItem = false;
      line.IsGold = true;
      line.ItemId = null;
      line.Gained = gained;
      line.Total = totalOwned;
      line.Remaining = defaultDuration;
      line.Active = true;
      line.View.Root.SetActive(true);
      RefreshGoldLine(line);
      SpawnGainFloater(line, gained);
    }

    public void PushText(string message)
    {
      if (string.IsNullOrWhiteSpace(message)) return;
      if (!_wired) WireLines();
      if (!_wired) return;

      var line = TakeSlot();
      line.IsItem = false;
      line.IsGold = false;
      line.ItemId = null;
      line.Gained = 0;
      line.Total = 0;
      line.Remaining = defaultDuration;
      line.Active = true;
      line.View.Root.SetActive(true);
      RefreshTextLine(line, message.Trim());
    }

    private void WireLines()
    {
      if (_wired) return;
      ResolveReferences();
      EnsureLineSlots();

      if (_lineSlots == null || _lineSlots.Length == 0)
      {
        Debug.LogWarning("[UniverIdle] LootToastView 未绑定 linePrefab。");
        return;
      }

      var count = Mathf.Min(MaxLines, _lineSlots.Length);
      for (var i = 0; i < count; i++)
      {
        _lineSlots[i]?.ResolveReferences();
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

    private void ResolveReferences()
    {
      if (lineRoot == null)
      {
        var lines = transform.Find("Lines");
        if (lines != null) lineRoot = lines as RectTransform;
      }

      if (floatLayer == null)
      {
        var layer = transform.Find("FloatLayer");
        if (layer != null) floatLayer = layer as RectTransform;
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
      line.ItemId = null;
      line.Gained = 0;
      line.Total = 0;
      line.Remaining = 0f;
      line.View.Root.SetActive(false);
    }

    private void RefreshItemLine(LineState line)
    {
      var view = line.View;
      if (view.Icon != null)
      {
        view.Icon.gameObject.SetActive(true);
        var item = GameContent.GetItem(line.ItemId);
        var sprite = ItemIconLoader.Get(item);
        if (sprite != null)
        {
          view.Icon.sprite = sprite;
          view.Icon.color = Color.white;
        }
        else
        {
          view.Icon.sprite = null;
          view.Icon.color = UITheme.Muted;
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
    }

    private void RefreshGoldLine(LineState line)
    {
      var view = line.View;
      if (view.Icon != null)
      {
        view.Icon.gameObject.SetActive(true);
        view.Icon.sprite = null;
        view.Icon.color = UITheme.Gold;
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
    }
  }
}
