#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UniverIdle.Editor
{
    /// <summary>
    /// 主界面布局调参：改参数实时预览，满意后保存到场景 / Asset。
    /// 也可在场景里直接拖 Inspector，再点「从场景读取」。
    /// </summary>
    public sealed class MainUILayoutTuneWindow : EditorWindow
    {
        private const string PrefLivePreview = "UniverIdle.LayoutTune.LivePreview";

        private MainUILayoutParams _params;
        private UnityEditor.Editor _inspector;
        private Vector2 _scroll;
        private bool _livePreview = true;
        private string _status;

        [MenuItem("UniverIdle/布局调参窗口")]
        public static void ShowWindow()
        {
            var window = GetWindow<MainUILayoutTuneWindow>(false, "主界面布局", true);
            window.minSize = new Vector2(380, 520);
            window.Show();
        }

        private void OnEnable()
        {
            _livePreview = EditorPrefs.GetBool(PrefLivePreview, true);
            ReloadAsset();
        }

        private void OnDisable()
        {
            if (_inspector != null)
                DestroyImmediate(_inspector);
        }

        private void ReloadAsset()
        {
            _params = MainUISetup.GetLayoutAsset();
            if (_inspector != null)
                DestroyImmediate(_inspector);
            if (_params != null)
                _inspector = UnityEditor.Editor.CreateEditor(_params);
        }

        private void OnGUI()
        {
            DrawToolbar();
            EditorGUILayout.Space(6);

            if (_params == null)
            {
                EditorGUILayout.HelpBox("找不到 MainUILayoutParams.asset。", MessageType.Error);
                if (GUILayout.Button("重新加载 Asset"))
                    ReloadAsset();
                return;
            }

            if (!MainUISetup.HasMainUiInScene())
            {
                EditorGUILayout.HelpBox(
                    "当前场景没有 UniverIdle_MainUI。\n请先执行「创建主界面」，或打开含主界面的场景。",
                    MessageType.Warning);
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUI.BeginChangeCheck();
            _inspector?.OnInspectorGUI();
            var changed = EditorGUI.EndChangeCheck();
            EditorGUILayout.EndScrollView();

            if (!string.IsNullOrEmpty(_status))
                EditorGUILayout.HelpBox(_status, MessageType.None);

            if (changed)
            {
                EditorUtility.SetDirty(_params);
                if (_livePreview)
                    PreviewToScene(silent: true);
            }
        }

        private void DrawToolbar()
        {
            EditorGUILayout.LabelField("布局调参", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            _livePreview = EditorGUILayout.ToggleLeft("实时预览（改参数立即套到场景）", _livePreview);
            if (EditorGUI.EndChangeCheck())
                EditorPrefs.SetBool(PrefLivePreview, _livePreview);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("从场景读取"))
                PullFromScene();
            if (GUILayout.Button("预览到场景"))
                PreviewToScene();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("保存到场景"))
                SaveToScene();
            if (GUILayout.Button("保存 Asset"))
                SaveAsset();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("选中主界面根节点"))
                SelectRoot();
            if (GUILayout.Button("打开背包层"))
                SelectInventory();
            EditorGUILayout.EndHorizontal();
        }

        private void PullFromScene()
        {
            if (!MainUISetup.HasMainUiInScene())
            {
                _status = "场景里没有 UniverIdle_MainUI。";
                return;
            }

            if (!MainUISetup.CaptureLayoutFromScene(_params))
            {
                _status = "读取失败。";
                return;
            }

            _status = "已从场景读取到 MainUILayoutParams（记得点「保存 Asset」固化）。";
            Repaint();
        }

        private void PreviewToScene(bool silent = false)
        {
            if (!MainUISetup.HasMainUiInScene())
            {
                if (!silent)
                    _status = "场景里没有主界面，无法预览。";
                return;
            }

            MainUISetup.ApplyLayoutToScene(_params);
            if (!silent)
                _status = "已预览到场景（未保存场景文件）。";
        }

        private void SaveToScene()
        {
            if (!MainUISetup.HasMainUiInScene())
            {
                _status = "场景里没有主界面。";
                return;
            }

            MainUISetup.ApplyLayoutToScene(_params);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            _status = "已应用并标记场景为已修改 · 请 Ctrl+S 保存场景。";
        }

        private void SaveAsset()
        {
            if (_params == null) return;
            EditorUtility.SetDirty(_params);
            AssetDatabase.SaveAssets();
            _status = "已保存 MainUILayoutParams.asset。";
        }

        private static void SelectRoot()
        {
            var root = GameObject.Find(MainUISetup.RootNameForEditor);
            if (root != null)
                Selection.activeGameObject = root;
        }

        private static void SelectInventory()
        {
            var root = GameObject.Find(MainUISetup.RootNameForEditor);
            var panel = root != null ? root.transform.Find("InventoryOverlay/Panel") : null;
            if (panel != null)
                Selection.activeGameObject = panel.gameObject;
        }
    }
}
#endif
