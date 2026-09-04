using UnityEngine;

namespace UniverIdle.Game
{
  /// <summary>运行时游戏状态入口：玩家数据 + 动作挂机循环 + 本地存档。</summary>
  [DefaultExecutionOrder(-100)]
  public sealed class GameSession : MonoBehaviour
  {
    public const float DefaultAutoSaveIntervalSeconds = 10f;

    public PlayerState Player { get; private set; }
    public ActionRunner Runner { get; private set; }

    [SerializeField] private float autoSaveIntervalSeconds = DefaultAutoSaveIntervalSeconds;

    private bool _suppressSave;
    private bool _dirty;
    private float _autoSaveElapsed;

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
      TickAutoSave(Time.deltaTime);
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
      Player.TryAddItem("small_trap", 8);
      Player.TryAddItem("large_trap", 3);
    }

    private void WireSave()
    {
      Player.OnInventoryChanged += MarkDirty;
      Player.OnGoldChanged += MarkDirty;
      Player.OnWorkChanged += MarkDirtyIgnoreArgs;
      Player.OnActionMasteryChanged += MarkDirtyIgnoreArgs;
    }

    private void UnwireSave()
    {
      if (Player == null) return;
      Player.OnInventoryChanged -= MarkDirty;
      Player.OnGoldChanged -= MarkDirty;
      Player.OnWorkChanged -= MarkDirtyIgnoreArgs;
      Player.OnActionMasteryChanged -= MarkDirtyIgnoreArgs;
    }

    private void MarkDirtyIgnoreArgs(string _) => MarkDirty();

    private void MarkDirty()
    {
      if (_suppressSave) return;
      _dirty = true;
    }

    private void TickAutoSave(float deltaTime)
    {
      if (autoSaveIntervalSeconds <= 0f || !_dirty) return;
      _autoSaveElapsed += deltaTime;
      if (_autoSaveElapsed < autoSaveIntervalSeconds) return;
      _autoSaveElapsed = 0f;
      SaveNow();
    }

    private void SaveNow()
    {
      if (_suppressSave || Player == null) return;
      GameSave.Write(Player.ToSaveFile());
      _dirty = false;
      _autoSaveElapsed = 0f;
    }
  }
}
