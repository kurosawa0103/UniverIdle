using UniverIdle.Game;
using UnityEngine;
using UnityEngine.UI;

namespace UniverIdle.UI
{
  /// <summary>背包弹层：顶栏按钮打开，点遮罩或关闭按钮收起。</summary>
  public sealed class InventoryPanelView : MonoBehaviour
  {
    [SerializeField] private GameObject overlayRoot;
    [SerializeField] private InventoryGridView grid;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button backdropButton;

    private bool _handlersWired;

    public bool IsOpen => overlayRoot != null && overlayRoot.activeSelf;

    public void Configure(GameObject root, InventoryGridView inventoryGrid, Button close, Button backdrop)
    {
      overlayRoot = root;
      grid = inventoryGrid;
      closeButton = close;
      backdropButton = backdrop;
    }

    private void Awake()
    {
      ResolveReferences();
      WireCloseHandlers();
      SetOpen(false);
    }

    private void ResolveReferences()
    {
      if (overlayRoot == null)
        overlayRoot = gameObject;

      if (grid == null)
        grid = GetComponentInChildren<InventoryGridView>(true);

      if (closeButton == null)
      {
        foreach (var btn in GetComponentsInChildren<Button>(true))
        {
          if (btn.gameObject.name == "Btn_Close")
          {
            closeButton = btn;
            break;
          }
        }
      }

      if (backdropButton == null)
      {
        var backdrop = transform.Find("Backdrop");
        if (backdrop != null)
          backdropButton = backdrop.GetComponent<Button>();
      }
    }

    private void WireCloseHandlers()
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
    }

    public void SetOpen(bool open, PlayerState player = null)
    {
      if (overlayRoot != null)
        overlayRoot.SetActive(open);
      if (open && grid != null && player != null)
        grid.Refresh(player);
    }

    public void Toggle(PlayerState player)
    {
      SetOpen(!IsOpen, player);
    }

    public void Refresh(PlayerState player)
    {
      if (IsOpen && grid != null)
        grid.Refresh(player);
    }
  }
}
