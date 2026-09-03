using UnityEngine;

namespace UniverIdle.Game
{
  /// <summary>运行时游戏状态入口：玩家数据 + 动作挂机循环 + 本地存档。</summary>
  [DefaultExecutionOrder(-100)]
  public sealed class GameSession : MonoBehaviour
  {
    public PlayerState Player { get; private set; }
    public ActionRunner Runner { get; private set; }

    private bool _suppressSave;

    private void Awake()
    {
      Player = new PlayerState();
      Runner = new ActionRunner(Player);
      WireSave();

      if (GameSave.TryLoad(out var file))
        Player.LoadFrom(file);
      else
        BeginNewPlayer();
    }

    private void Update()
    {
      Runner?.Tick(Time.deltaTime);
    }

    private void OnApplicationPause(bool pause)
    {
      if (pause)
        SaveNow();
    }

    private void OnApplicationQuit() => SaveNow();

    private void OnDestroy()
    {
      UnwireSave();
      SaveNow();
    }

    /// <summary>删档并回到开局（GM / 运行中重置）。</summary>
    public void ResetToNewGame()
    {
      Runner?.Stop();
      _suppressSave = true;
      Player.ResetToNewPlayer();
      GiveStarterItems();
      _suppressSave = false;
      GameSave.Delete();
      SaveNow();
      Player.NotifyStateReplaced();
    }

    private void BeginNewPlayer()
    {
      _suppressSave = true;
      GiveStarterItems();
      _suppressSave = false;
      SaveNow();
    }

    private void GiveStarterItems()
    {
      Player.AddItem("small_trap", 8);
      Player.AddItem("large_trap", 3);
    }

    private void WireSave()
    {
      Player.OnInventoryChanged += SaveNow;
      Player.OnGoldChanged += SaveNow;
      Player.OnWorkChanged += OnWorkProgressSaved;
      Player.OnSceneProgressChanged += OnSceneProgressSaved;
    }

    private void UnwireSave()
    {
      if (Player == null) return;
      Player.OnInventoryChanged -= SaveNow;
      Player.OnGoldChanged -= SaveNow;
      Player.OnWorkChanged -= OnWorkProgressSaved;
      Player.OnSceneProgressChanged -= OnSceneProgressSaved;
    }

    private void OnWorkProgressSaved(string _) => SaveNow();

    private void OnSceneProgressSaved(string _, string __) => SaveNow();

    private void SaveNow()
    {
      if (_suppressSave || Player == null) return;
      GameSave.Write(Player.ToSaveFile());
    }
  }
}
