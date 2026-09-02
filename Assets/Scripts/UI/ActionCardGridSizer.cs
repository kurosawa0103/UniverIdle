using UnityEngine;
using UnityEngine.UI;

namespace UniverIdle.UI
{
  /// <summary>ActionCards GridLayoutGroup：固定 Cell Size 或由列宽自动均分（cellWidth=0）。</summary>
  [RequireComponent(typeof(GridLayoutGroup))]
  public sealed class ActionCardGridSizer : MonoBehaviour
  {
    [SerializeField] private int columns = 3;
    [SerializeField] private float cellWidth;
    [SerializeField] private float cellHeight = 250f;

    public void Configure(int columnCount, float width, float height)
    {
      columns = Mathf.Max(1, columnCount);
      cellWidth = width;
      cellHeight = Mathf.Max(40f, height);
      _lastWidth = -1f;
      Refresh();
    }

    private GridLayoutGroup _grid;
    private float _lastWidth = -1f;

    private void Awake() => _grid = GetComponent<GridLayoutGroup>();

    private void OnEnable() => Refresh();

    private void Start() => Refresh();

    private void OnRectTransformDimensionsChange() => Refresh();

    public void Refresh()
    {
      if (_grid == null) _grid = GetComponent<GridLayoutGroup>();
      if (_grid == null) return;

      if (cellWidth > 0f)
      {
        _grid.cellSize = new Vector2(cellWidth, cellHeight);
        return;
      }

      var width = ((RectTransform)transform).rect.width;
      if (width <= 1f || Mathf.Approximately(width, _lastWidth)) return;
      _lastWidth = width;

      var spacing = _grid.spacing.x;
      var autoWidth = (width - spacing * (columns - 1)) / columns;
      if (autoWidth < 80f) autoWidth = 80f;
      _grid.cellSize = new Vector2(autoWidth, cellHeight);
    }
  }
}
