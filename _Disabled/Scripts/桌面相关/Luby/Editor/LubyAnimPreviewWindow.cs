#if UNITY_EDITOR
using System.Collections.Generic;
using DesktopPet.AI;
using UnityEditor;
using UnityEngine;

namespace DesktopPet.Luby.Editor
{
    /// <summary>
    /// Luby 动画参数预览窗口。
    /// 左：Luby Prefab（点击 → 场景实例化）  中：AI 行为组 + 行为  右：动画参数详情 + Animator 驱动按钮
    /// </summary>
    public sealed class LubyAnimPreviewWindow : EditorWindow
    {
        // ── 布局 ─────────────────────────────────────────────────
        private const float LeftW  = 170f;
        private const float MidW   = 210f;

        // ── 数据 ─────────────────────────────────────────────────
        private List<GameObject>            _prefabs    = new();
        private List<PetAiGroup>            _aiGroups   = new();
        private List<PetBehaviorDefinition> _behaviors  = new();

        private int _prefabIdx   = -1;
        private int _aiGroupIdx  = -1;
        private int _behaviorIdx = -1;

        private Vector2 _leftScroll;
        private Vector2 _midScroll;
        private Vector2 _rightScroll;

        // 场景预览实例
        private GameObject _preview;

        // ── 菜单入口 ─────────────────────────────────────────────
        [MenuItem("桌宠/Luby 动画预览")]
        public static void Open()
        {
            var w = GetWindow<LubyAnimPreviewWindow>();
            w.titleContent = new GUIContent("Luby 动画预览");
            w.minSize = new Vector2(680, 440);
            w.Show();
        }

        // ── 生命周期 ──────────────────────────────────────────────
        private void OnEnable()  => Refresh();
        private void OnDisable() => CleanupPreview();

        // ── 数据刷新 ──────────────────────────────────────────────
        private void Refresh()
        {
            _prefabs.Clear();
            _aiGroups.Clear();
            _behaviors.Clear();
            _prefabIdx = _aiGroupIdx = _behaviorIdx = -1;

            string[] pg = AssetDatabase.FindAssets("t:Prefab",
                new[] { "Assets/Resources/Prefabs/Prefabs_Luby" });
            foreach (string g in pg)
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g));
                if (go != null) _prefabs.Add(go);
            }

            string[] ag = AssetDatabase.FindAssets("t:PetAiGroup",
                new[] { "Assets/Resources/GameData/Luby/AI" });
            foreach (string g in ag)
            {
                var grp = AssetDatabase.LoadAssetAtPath<PetAiGroup>(AssetDatabase.GUIDToAssetPath(g));
                if (grp != null) _aiGroups.Add(grp);
            }
        }

        private void SelectAiGroup(int idx)
        {
            _aiGroupIdx  = idx;
            _behaviorIdx = -1;
            _behaviors.Clear();
            if (idx < 0 || idx >= _aiGroups.Count) return;

            PetAiGroup grp = _aiGroups[idx];
            void Add(IEnumerable<PetBehaviorDefinition> list)
            {
                if (list == null) return;
                foreach (var b in list) if (b != null) _behaviors.Add(b);
            }
            Add(grp.behaviors);
            Add(grp.requestOnlyBehaviors);
            if (grp.fallbackBehavior != null) _behaviors.Add(grp.fallbackBehavior);
        }

        // ── 绘制 ──────────────────────────────────────────────────
        private void OnGUI()
        {
            DrawToolbar();
            EditorGUILayout.BeginHorizontal();
            DrawLeft();
            DrawSep();
            DrawMid();
            DrawSep();
            DrawRight();
            EditorGUILayout.EndHorizontal();
        }

        // ─ 工具栏 ─────────────────────────────────────────────────
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(44)))
                Refresh();

            GUILayout.Space(8);

            // 预览实例状态
            if (_preview != null)
            {
                GUI.color = new Color(0.6f, 1f, 0.6f);
                GUILayout.Label($"▶ 预览中：{_preview.name}", EditorStyles.miniLabel);
                GUI.color = Color.white;
                GUILayout.Space(4);
                if (GUILayout.Button("销毁", EditorStyles.toolbarButton, GUILayout.Width(40)))
                    CleanupPreview();
            }
            else
            {
                GUILayout.Label("（左栏点击 Prefab → 场景实例化）", EditorStyles.miniLabel);
            }

            GUILayout.FlexibleSpace();

            // 当前选中 Animator（场景中）
            Animator selAnim = GetSelectedSceneAnimator();
            if (selAnim != null)
            {
                GUI.color = new Color(1f, 0.9f, 0.5f);
                GUILayout.Label($"场景 Animator：{selAnim.gameObject.name}", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }

            EditorGUILayout.EndHorizontal();
        }

        // ─ 左栏：Prefab ───────────────────────────────────────────
        private void DrawLeft()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(LeftW));
            GUILayout.Label("外观 Prefab", EditorStyles.boldLabel);
            _leftScroll = EditorGUILayout.BeginScrollView(_leftScroll);

            for (int i = 0; i < _prefabs.Count; i++)
            {
                bool sel = i == _prefabIdx;
                DrawItem(_prefabs[i].name, sel, new Color(0.5f, 0.82f, 1f), () =>
                {
                    _prefabIdx = i;
                    SpawnPreview(_prefabs[i]);
                });
            }

            if (_prefabs.Count == 0)
                EditorGUILayout.HelpBox("未找到 Prefab", MessageType.None);

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        // ─ 中栏：AI 组 + 行为 ─────────────────────────────────────
        private void DrawMid()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(MidW));
            GUILayout.Label("AI 行为组 / 行为", EditorStyles.boldLabel);
            _midScroll = EditorGUILayout.BeginScrollView(_midScroll);

            // AI 组
            for (int i = 0; i < _aiGroups.Count; i++)
            {
                bool sel = i == _aiGroupIdx;
                DrawItem(_aiGroups[i].name, sel, new Color(0.5f, 1f, 0.72f), () => SelectAiGroup(i));
            }

            // 行为（分隔后显示）
            if (_behaviors.Count > 0)
            {
                GUILayout.Space(6);
                GUILayout.Label("── 行为 ──", EditorStyles.centeredGreyMiniLabel);
                GUILayout.Space(2);

                for (int i = 0; i < _behaviors.Count; i++)
                {
                    PetBehaviorDefinition b = _behaviors[i];
                    string icon = BehaviorIcon(b);
                    bool sel = i == _behaviorIdx;
                    DrawItem($"{icon} {b.behaviorId}", sel, new Color(1f, 0.85f, 0.35f),
                        () => { _behaviorIdx = i; });
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        // ─ 右栏：参数 + 播放 ──────────────────────────────────────
        private void DrawRight()
        {
            EditorGUILayout.BeginVertical();
            GUILayout.Label("动画参数", EditorStyles.boldLabel);
            _rightScroll = EditorGUILayout.BeginScrollView(_rightScroll);

            if (_behaviorIdx < 0 || _behaviorIdx >= _behaviors.Count)
            {
                EditorGUILayout.HelpBox("← 在中栏选择一个行为", MessageType.None);
                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
                return;
            }

            PetBehaviorDefinition def = _behaviors[_behaviorIdx];

            // 基础信息（只读展示）
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("行为 ID", def.behaviorId);
                EditorGUILayout.FloatField("权重", def.weight);
                EditorGUILayout.FloatField(
                    "时长", def.minDuration == def.maxDuration
                        ? def.minDuration
                        : def.minDuration);
                EditorGUILayout.LabelField("时长范围",
                    $"{def.minDuration:F1} ~ {def.maxDuration:F1} 秒");
                EditorGUILayout.FloatField("冷却(秒)", def.cooldown);
            }

            // 动画参数
            GUILayout.Space(10);
            bool hasTrigger = !string.IsNullOrEmpty(def.animTrigger);
            bool hasBool    = !string.IsNullOrEmpty(def.animBool);
            bool hasSpeed   = !string.IsNullOrEmpty(def.animSpeedParam);

            if (!hasTrigger && !hasBool && !hasSpeed)
            {
                EditorGUILayout.HelpBox("此行为未配置任何动画参数", MessageType.None);
            }
            else
            {
                GUILayout.Label("Animator 参数", EditorStyles.boldLabel);
                using (new EditorGUI.DisabledScope(true))
                {
                    if (hasTrigger)
                        EditorGUILayout.TextField("Trigger", def.animTrigger);
                    if (hasBool)
                    {
                        EditorGUILayout.TextField("Bool", def.animBool);
                        EditorGUILayout.Toggle("值", def.animBoolValue);
                    }
                    if (hasSpeed)
                    {
                        EditorGUILayout.TextField("Float", def.animSpeedParam);
                        EditorGUILayout.FloatField("值", def.animSpeedValue);
                    }
                }
            }

            // ── 播放控制 ──────────────────────────────────────────
            GUILayout.Space(12);
            GUILayout.Label("播放控制", EditorStyles.boldLabel);

            Animator anim = ResolveAnimator();

            if (anim == null)
            {
                EditorGUILayout.HelpBox(
                    "没有可用的 Animator。\n" +
                    "• 在左栏点击 Prefab → 自动 Spawn 到场景\n" +
                    "• 或在场景 Hierarchy 选中带 Animator 的对象",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox($"目标：{anim.gameObject.name}", MessageType.None);
                GUILayout.Space(4);

                if (hasTrigger)
                    DrawPlayButton($"▶  SetTrigger  \"{def.animTrigger}\"",
                        () => anim.SetTrigger(def.animTrigger));

                if (hasBool)
                {
                    DrawPlayButton($"▶  SetBool  \"{def.animBool}\"  =  {def.animBoolValue}",
                        () => anim.SetBool(def.animBool, def.animBoolValue));
                    DrawPlayButton($"⏹  SetBool  \"{def.animBool}\"  =  {!def.animBoolValue}  [还原]",
                        () => anim.SetBool(def.animBool, !def.animBoolValue));
                }

                if (hasSpeed && def.animSpeedValue != 0f)
                {
                    DrawPlayButton($"▶  SetFloat  \"{def.animSpeedParam}\"  =  {def.animSpeedValue}",
                        () => anim.SetFloat(def.animSpeedParam, def.animSpeedValue));
                    DrawPlayButton($"⏹  SetFloat  \"{def.animSpeedParam}\"  =  0  [归零]",
                        () => anim.SetFloat(def.animSpeedParam, 0f));
                }

                GUILayout.Space(4);
                if (GUILayout.Button("重置全部参数", GUILayout.Height(22)))
                {
                    if (hasTrigger)  anim.ResetTrigger(def.animTrigger);
                    if (hasBool)     anim.SetBool(def.animBool, !def.animBoolValue);
                    if (hasSpeed)    anim.SetFloat(def.animSpeedParam, 0f);
                }
            }

            // 快速定位
            GUILayout.Space(8);
            if (GUILayout.Button("在 Inspector 选中此行为", EditorStyles.miniButton))
                Selection.activeObject = def;

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        // ── 场景 Prefab Spawn ─────────────────────────────────────
        private void SpawnPreview(GameObject prefab)
        {
            CleanupPreview();
            _preview = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            _preview.name = $"[LubyPreview] {prefab.name}";
            _preview.transform.position = Vector3.zero;
            Selection.activeGameObject  = _preview;
            SceneView.FrameLastActiveSceneView();
        }

        private void CleanupPreview()
        {
            if (_preview != null)
                DestroyImmediate(_preview);
            _preview = null;
        }

        // ── Animator 解析优先级：预览实例 > 场景选中 ─────────────
        private Animator ResolveAnimator()
        {
            if (_preview != null)
            {
                Animator a = _preview.GetComponentInChildren<Animator>();
                if (a != null) return a;
            }
            return GetSelectedSceneAnimator();
        }

        private static Animator GetSelectedSceneAnimator()
        {
            if (Selection.activeGameObject == null) return null;
            return Selection.activeGameObject.GetComponentInChildren<Animator>();
        }

        // ── 辅助 UI ──────────────────────────────────────────────
        private static void DrawItem(string label, bool selected, Color selColor,
            System.Action onClick)
        {
            GUI.color = selected ? selColor : Color.white;
            GUIStyle style = selected ? EditorStyles.boldLabel : EditorStyles.label;
            Rect r = GUILayoutUtility.GetRect(
                new GUIContent(label), style,
                GUILayout.ExpandWidth(true));
            if (selected)
                EditorGUI.DrawRect(r, selColor * 0.25f);
            if (GUI.Button(r, label, style))
                onClick?.Invoke();
            GUI.color = Color.white;
        }

        private static void DrawPlayButton(string label, System.Action onClick)
        {
            if (GUILayout.Button(label, GUILayout.Height(26)))
                onClick?.Invoke();
        }

        private static string BehaviorIcon(PetBehaviorDefinition b)
        {
            if (!string.IsNullOrEmpty(b.animTrigger))  return "▶";
            if (!string.IsNullOrEmpty(b.animBool))     return "⏺";
            if (!string.IsNullOrEmpty(b.animSpeedParam) && b.animSpeedValue != 0) return "~";
            return "○";
        }

        private static void DrawSep()
        {
            GUILayout.Box(GUIContent.none, GUILayout.Width(3), GUILayout.ExpandHeight(true));
        }
    }
}
#endif
