using UnityEngine;
using UnityEngine.UI;

namespace UniverIdle.UI
{
  /// <summary>按容器宽度自动计算 3 列动作卡单元格尺寸（对齐概念图 grid）。</summary>
  [RequireComponent(typeof(GridLayoutGroup))]
  public sealed class ActionCardGridSizer : MonoBehaviour
  {
    [SerializeField] private int columns = 3;
    [SerializeField] private float minCellHeight = 100f;

    private GridLayoutGroup _grid;
    private float _lastWidth = -1f;

    private void Awake() => _grid = GetComponent<GridLayoutGroup>();

    private void OnRectTransformDimensionsChange() => Refresh();

    public void Refresh()
    {
      if (_grid == null) _grid = GetComponent<GridLayoutGroup>();
      if (_grid == null) return;

      var width = ((RectTransform)transform).rect.width;
      if (width <= 1f || Mathf.Approximately(width, _lastWidth)) return;
      _lastWidth = width;

      var spacing = _grid.spacing.x;
      var cellWidth = (width - spacing * (columns - 1)) / columns;
      if (cellWidth < 80f) cellWidth = 80f;
      _grid.cellSize = new Vector2(cellWidth, minCellHeight);
    }
  }
}
