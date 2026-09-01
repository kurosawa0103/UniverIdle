using UnityEngine;

namespace UniverIdle.Game
{
  /// <summary>运行时游戏状态入口：玩家数据 + 动作挂机循环。</summary>
  public sealed class GameSession : MonoBehaviour
  {
    public PlayerState Player { get; private set; }
    public ActionRunner Runner { get; private set; }

    private void Awake()
    {
      Player = new PlayerState();
      Runner = new ActionRunner(Player);
    }

    private void Update()
    {
      Runner?.Tick(Time.deltaTime);
    }
  }
}
