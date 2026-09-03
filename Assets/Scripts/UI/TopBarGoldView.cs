using TMPro;
using UniverIdle.Game;
using UnityEngine;
using UnityEngine.UI;

namespace UniverIdle.UI
{
  /// <summary>顶栏金币：图标 + 数量；订阅 <see cref="PlayerState.OnGoldChanged"/>。引用手配在 TopBar/Currency。</summary>
  public sealed class TopBarGoldView : MonoBehaviour
  {
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI amountText;

    private PlayerState _player;

    private void OnDestroy() => Unbind();

    public void Bind(PlayerState player)
    {
      Unbind();
      _player = player;
      if (_player != null)
        _player.OnGoldChanged += Refresh;
      Refresh();
    }

    public void Refresh()
    {
      if (amountText != null)
        amountText.text = _player != null ? _player.Gold.ToString() : "0";
    }

    private void Unbind()
    {
      if (_player == null) return;
      _player.OnGoldChanged -= Refresh;
      _player = null;
    }
  }
}
