#if UNITY_EDITOR
using UniverIdle.Game;
using UnityEditor;
using UnityEngine;

namespace UniverIdle.Editor
{
  public sealed class GmSaveWindow : EditorWindow
  {
    [MenuItem("UniverIdle/GM...", false, 20)]
    public static void ShowWindow()
    {
      var window = GetWindow<GmSaveWindow>("GM");
      window.minSize = new Vector2(360, 160);
      window.Show();
    }

    private void OnGUI()
    {
      EditorGUILayout.Space(8);
      EditorGUILayout.LabelField("存档", EditorStyles.boldLabel);
      EditorGUILayout.HelpBox(GameSave.FilePath, MessageType.None);
      EditorGUILayout.LabelField("状态", GameSave.Exists ? "已有存档" : "无存档");

      EditorGUILayout.Space(12);
      GUI.backgroundColor = new Color(0.85f, 0.35f, 0.3f);
      if (GUILayout.Button("重置存档", GUILayout.Height(32)))
        ResetSave();
      GUI.backgroundColor = Color.white;
    }

    private static void ResetSave()
    {
      var playing = Application.isPlaying;
      var message = playing
        ? "将删除本地存档，并把当前运行中的进度重置为新号。确定？"
        : "将删除本地存档。下次进入游戏会从新号开始。确定？";
      if (!EditorUtility.DisplayDialog("重置存档", message, "重置", "取消"))
        return;

      if (playing)
      {
        var session = Object.FindFirstObjectByType<GameSession>();
        if (session != null)
        {
          session.ResetToNewGame();
          Debug.Log("已重置运行中的存档。");
          return;
        }
      }

      if (GameSave.Delete())
        Debug.Log(playing ? "未找到 GameSession，已删除存档文件。" : "已删除存档。");
    }
  }
}
#endif
