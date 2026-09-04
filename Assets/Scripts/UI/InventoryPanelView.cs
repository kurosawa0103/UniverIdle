using System.Collections.Generic;
using TMPro;
using UniverIdle.Game;
using UnityEngine;
using UnityEngine.UI;

namespace UniverIdle.UI
{
  /// <summary>背包弹层：页签切页、金币解锁页与格子。</summary>
  public sealed class InventoryPanelView : MonoBehaviour
  {
    [SerializeField] private GameObject overlayRoot;
    [SerializeField] private InventoryGridView grid;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button backdropButton;
    [SerializeField] private Transform tabRoot;
    [SerializeField] private List<Button> pageTabs = new();
    [SerializeField] private TextMeshProUGUI pageLabelText;
    [SerializeField] private TextMeshProUGUI goldText;

    private bool _handlersWired;
    private int _pageIndex;
    private PlayerState _player;
    private readonly List<Image> _tabBackgrounds = new();
    private readonly List<TextMeshProUGUI> _tabLabels = new();
    private bool _goldWired;

    public bool IsOpen => overlayRoot != null && overlayRoot.activeSelf;

    private void Awake()
    {
      if (pageTabs == null)
        pageTabs = new List<Button>();
      CacheTabVisuals();
      WireHandlers();
      SetOpen(false);
    }

    private void OnDestroy() => UnbindGold();

    private void CacheTabVisuals()
    {
      _tabBackgrounds.Clear();
      _tabLabels.Clear();
      for (var i = 0; i < pageTabs.Count; i++)
      {
        var tab = pageTabs[i];
        if (tab == null)
        {
          _tabBackgrounds.Add(null);
          _tabLabels.Add(null);
          continue;
        }

        _tabBackgrounds.Add(tab.GetComponent<Image>());
        _tabLabels.Add(tab.GetComponentInChildren<TextMeshProUGUI>(true));
      }
    }

    private void WireHandlers()
    {
      if (_handlersWired) return;
      _handlersWired = true;

      if (closeButton != null)
      {
        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(() => SetOpen(false));
      }

      if (backdropButton != null)
      {
        backdropButton.onClick.RemoveAllListeners();
        backdropButton.onClick.AddListener(() => SetOpen(false));
      }

      for (var i = 0; i < pageTabs.Count; i++)
      {
        var tab = pageTabs[i];
        if (tab == null) continue;
        var page = i;
        tab.onClick.RemoveAllListeners();
        tab.onClick.AddListener(() => SelectPage(page));
      }

      grid?.SetSlotClickHandler(OnSlotClicked);
    }

    public void SetOpen(bool open, PlayerState player = null)
    {
      if (player != null)
        _player = player;
      if (overlayRoot != null)
        overlayRoot.SetActive(open);
      if (open)
        Refresh(_player);
    }

    public void Toggle(PlayerState player)
    {
      SetOpen(!IsOpen, player);
    }

    public void Refresh(PlayerState player)
    {
      BindPlayer(player);
      if (!IsOpen || _player == null) return;

      var bag = GameContent.Inventory;
      if (_pageIndex >= bag.PageCount)
        _pageIndex = bag.PageCount - 1;
      if (_pageIndex < 0)
        _pageIndex = 0;

      grid?.Refresh(_player, _pageIndex);
      RefreshChrome();
    }

    private void BindPlayer(PlayerState player)
    {
      if (player == null)
      {
        UnbindGold();
        _player = null;
        return;
      }

      if (_player == player && _goldWired) return;

      UnbindGold();
      _player = player;
      _player.OnGoldChanged += OnPlayerGoldChanged;
      _goldWired = true;
    }

    private void UnbindGold()
    {
      if (_player == null || !_goldWired) return;
      _player.OnGoldChanged -= OnPlayerGoldChanged;
      _goldWired = false;
    }

    private void OnPlayerGoldChanged()
    {
      if (!IsOpen || _player == null || goldText == null) return;
      goldText.text = $"金币 {_player.Gold}";
    }

    private void RefreshChrome()
    {
      var bag = GameContent.Inventory;
      if (pageLabelText != null)
        pageLabelText.text = $"{_player.UnlockedSlotCount}/{bag.SlotCapForPages(_player.UnlockedPageCount)}格";
      if (goldText != null)
        goldText.text = $"金币 {_player.Gold}";

      for (var i = 0; i < pageTabs.Count; i++)
      {
        var tab = pageTabs[i];
        if (tab == null) continue;
        var inRange = i < bag.PageCount;
        tab.gameObject.SetActive(inRange);
        if (!inRange) continue;

        tab.interactable = true;
        var selected = i == _pageIndex;
        var unlocked = _player.IsPageUnlocked(i);
        if (i < _tabBackgrounds.Count && _tabBackgrounds[i] != null)
          _tabBackgrounds[i].color = selected ? UITheme.CardHover : UITheme.PanelLight;
        if (i < _tabLabels.Count && _tabLabels[i] != null)
        {
          _tabLabels[i].color = selected ? UITheme.TealBright : UITheme.Text;
          _tabLabels[i].text = unlocked ? (i + 1).ToString() : (i + 1) + " 🔒";
        }
      }
    }

    private void SelectPage(int pageIndex)
    {
      var bag = GameContent.Inventory;
      if (pageIndex < 0 || pageIndex >= bag.PageCount) return;
      _pageIndex = pageIndex;
      Refresh(_player);
    }

    private void OnSlotClicked(int localIndex)
    {
      if (_player == null) return;
      var bag = GameContent.Inventory;
      if (!_player.IsPageUnlocked(_pageIndex))
      {
        if (_pageIndex == _player.UnlockedPageCount)
          _player.TryUnlockNextPage();
        return;
      }

      var global = _pageIndex * bag.SlotsPerPage + localIndex;
      if (global == _player.UnlockedSlotCount)
        _player.TryUnlockNextSlot();
    }
  }
}
