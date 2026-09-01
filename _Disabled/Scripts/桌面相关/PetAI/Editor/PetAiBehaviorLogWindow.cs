#if UNITY_EDITOR
using System.Collections.Generic;
using DesktopPet.Luby;
using UnityEditor;
using UnityEngine;

namespace DesktopPet.AI.Editor
{
    /// <summary>Play 时查看桌上 Luby / PetBrain：肖像选目标 + 行为色块日志（无行为预览图）。</summary>
    public sealed class PetAiBehaviorLogWindow : EditorWindow
    {
        private const float PortraitSize = 72f;

        private static readonly Color ColPanel = new Color(0.18f, 0.18f, 0.2f, 0.55f);
        private static readonly Color ColCard = new Color(0.16f, 0.16f, 0.18f, 0.65f);
        private static readonly Color ColRowA = new Color(1f, 1f, 1f, 0.04f);
        private static readonly Color ColRowB = new Color(0f, 0f, 0f, 0.12f);
        private static readonly Color ColSelect = new Color(0.28f, 0.5f, 0.85f, 0.45f);

        private readonly List<PetBrain> _brains = new List<PetBrain>(8);
        private readonly List<string> _brainLabels = new List<string>(8);
        private readonly List<PetBehaviorLogEntry> _historyScratch = new List<PetBehaviorLogEntry>(48);

        private int _selectedIndex;
        private Vector2 _pickScroll;
        private Vector2 _historyScroll;
        private bool _autoRepaint = true;
        private bool _stickHistoryToEnd = true;
        private int _lastHistoryCount = -1;
        private double _lastRepaint;
        private GUIStyle _titleStyle;
        private GUIStyle _sectionTitleStyle;
        private GUIStyle _idStyle;
        private GUIStyle _metaStyle;
        private GUIStyle _badgeStyle;

        private int _mainTab;

        public void SwitchToTheaterTab() => _mainTab = 1;

        [MenuItem("桌宠/AI 行为日志")]
        public static void Open()
        {
            var window = GetWindow<PetAiBehaviorLogWindow>();
            window.titleContent = new GUIContent("AI 行为日志");
            window.minSize = new Vector2(560, 520);
            window.Show();
        }

        public static void OpenTheaterTab()
        {
            Open();
            var window = GetWindow<PetAiBehaviorLogWindow>();
            window.Focus();
            window.SwitchToTheaterTab();
        }

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.update += OnEditorUpdate;
            RefreshBrainList();
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnPlayModeChanged(PlayModeStateChange _)
        {
            RefreshBrainList();
            _lastHistoryCount = -1;
            Repaint();
        }

        private void OnEditorUpdate()
        {
            if (!_autoRepaint || !EditorApplication.isPlaying)
                return;
            if (EditorApplication.timeSinceStartup - _lastRepaint < 0.2d)
                return;
            _lastRepaint = EditorApplication.timeSinceStartup;
            Repaint();
        }

        private void EnsureStyles()
        {
            if (_titleStyle != null)
                return;

            _titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 };
            _sectionTitleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 };
            _idStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 };
            _metaStyle = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };
            _badgeStyle = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 10,
                normal = { textColor = Color.white }
            };
        }

        private void OnGUI()
        {
            EnsureStyles();

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("桌宠调试", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(48)))
                    RefreshBrainList();
                _autoRepaint = GUILayout.Toggle(
                    _autoRepaint,
                    "自动刷新",
                    EditorStyles.toolbarButton,
                    GUILayout.Width(72));
                if (_mainTab == 0)
                {
                    GUILayout.Label(
                        EditorApplication.isPlaying ? $"{_brains.Count} 只" : "未 Play",
                        EditorStyles.miniLabel,
                        GUILayout.Width(48));
                }
            }

            _mainTab = GUILayout.Toolbar(_mainTab, new[] { "行为日志", "小剧场" });
            GUILayout.Space(4);

            if (_mainTab == 1)
            {
                PetAiTheaterDebugPanel.TickAutoRefresh();
                PetAiTheaterDebugPanel.Draw();
                return;
            }

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.Space(12);
                EditorGUILayout.HelpBox("进入 Play 后即可查看 Luby 行为日志。", MessageType.Warning);
                return;
            }

            if (_brains.Count == 0)
            {
                EditorGUILayout.Space(12);
                EditorGUILayout.HelpBox("场景里没有启用的 PetBrain。", MessageType.Info);
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawPickerColumn();
                GUILayout.Space(6);
                using (new EditorGUILayout.VerticalScope())
                {
                    DrawSelectedPortraitBanner();
                    GUILayout.Space(6);
                    DrawHistoryPanel();
                    GUILayout.Space(8);
                    DrawCurrentCard();
                    GUILayout.Space(6);
                    DrawPlannedCard();
                }
            }
        }

        private void DrawPickerColumn()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(160)))
            {
                DrawSectionHeader("目标", ColPanel, new Color(0.45f, 0.65f, 0.95f, 1f));
                _pickScroll = EditorGUILayout.BeginScrollView(_pickScroll, GUILayout.ExpandHeight(true));
                for (int i = 0; i < _brains.Count; i++)
                {
                    PetBrain brain = _brains[i];
                    if (brain == null)
                        continue;

                    Rect row = EditorGUILayout.GetControlRect(false, 56f);
                    EditorGUI.DrawRect(row, i == _selectedIndex ? ColSelect : (i % 2 == 0 ? ColRowA : ColRowB));
                    if (i == _selectedIndex)
                        EditorGUI.DrawRect(new Rect(row.x, row.y, 3f, row.height), new Color(0.45f, 0.65f, 0.95f, 1f));

                    Rect iconRect = new Rect(row.x + 8f, row.y + 6f, 44f, 44f);
                    DrawPortrait(iconRect, ResolvePortrait(brain));

                    Rect labelRect = new Rect(row.x + 58f, row.y + 10f, row.width - 64f, row.height - 14f);
                    GUI.Label(labelRect, _brainLabels[i], _metaStyle);

                    if (Event.current.type == EventType.MouseDown && row.Contains(Event.current.mousePosition))
                    {
                        _selectedIndex = i;
                        _lastHistoryCount = -1;
                        Event.current.Use();
                        Repaint();
                    }
                }

                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawSelectedPortraitBanner()
        {
            PetBrain brain = GetSelectedBrain();
            if (brain == null)
                return;

            Rect box = EditorGUILayout.BeginVertical();
            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(box, ColPanel);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(8);
                Rect r = GUILayoutUtility.GetRect(
                    PortraitSize,
                    PortraitSize,
                    GUILayout.Width(PortraitSize),
                    GUILayout.Height(PortraitSize));
                if (Event.current.type == EventType.Repaint)
                    DrawPortrait(r, ResolvePortrait(brain));

                using (new EditorGUILayout.VerticalScope())
                {
                    GUILayout.Space(6);
                    GUILayout.Label(brain.gameObject.name, _titleStyle);
                    LubyInstanceComponent luby = brain.GetComponent<LubyInstanceComponent>();
                    if (luby?.Data != null)
                    {
                        GUILayout.Label($"模板  {luby.Data.templateId}", _metaStyle);
                        GUILayout.Label($"外形  {luby.Data.appearanceKey}", _metaStyle);
                        string traits = LubyTraitDisplay.FormatIds(luby.Data.traitId, luby.Data.traitId2);
                        GUILayout.Label($"{luby.Data.personalityId}  ·  {traits}", _metaStyle);
                    }

                    PetAiGroup group = brain.AiGroup;
                    GUILayout.Label(
                        group != null ? $"AI 组  {group.displayName}" : "AI 组  （无）",
                        _metaStyle);
                    GUILayout.Space(6);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawHistoryPanel()
        {
            PetBrain brain = GetSelectedBrain();
            BeginCard("已经做了（旧 → 新，最新在下）", ColCard);

            if (brain == null)
            {
                GUILayout.Label("—", _metaStyle);
                EndCard();
                return;
            }

            brain.CopyHistory(_historyScratch);
            if (_historyScratch.Count == 0)
            {
                GUILayout.Label("（还没有结束过的段）", _metaStyle);
                EndCard();
                return;
            }

            if (_historyScratch.Count != _lastHistoryCount)
            {
                _lastHistoryCount = _historyScratch.Count;
                _stickHistoryToEnd = true;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label($"共 {_historyScratch.Count} 条", _sectionTitleStyle);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(_stickHistoryToEnd ? "跟随最新" : "已松开", EditorStyles.miniButton, GUILayout.Width(72)))
                    _stickHistoryToEnd = !_stickHistoryToEnd;
            }

            GUILayout.Space(4);
            float historyH = Mathf.Clamp(position.height - 300f, 200f, 480f);
            _historyScroll = EditorGUILayout.BeginScrollView(_historyScroll, GUILayout.Height(historyH));

            // 旧 → 新：从上往下，新的在下面（往上是更早的）
            for (int i = 0; i < _historyScratch.Count; i++)
            {
                PetBehaviorLogEntry e = _historyScratch[i];
                Color behaviorColor = BehaviorColor(e.BehaviorId);
                Rect row = EditorGUILayout.GetControlRect(false, 48f);
                EditorGUI.DrawRect(row, i % 2 == 0 ? ColRowA : ColRowB);
                EditorGUI.DrawRect(new Rect(row.x, row.y, 5f, row.height), behaviorColor);

                Rect swatch = new Rect(row.x + 10f, row.y + 10f, 28f, 28f);
                EditorGUI.DrawRect(swatch, behaviorColor);
                GUI.Label(swatch, e.BehaviorId.Length > 0 ? e.BehaviorId.Substring(0, 1).ToUpperInvariant() : "?", _badgeStyle);

                float textX = row.x + 48f;
                float textW = row.width - 48f - 78f;
                GUI.Label(
                    new Rect(textX, row.y + 4f, textW, 20f),
                    e.BehaviorId,
                    _idStyle);
                GUI.Label(
                    new Rect(textX, row.y + 24f, textW, 20f),
                    $"时长 {e.LivedSeconds:0.00}s（计划 {e.PlannedDuration:0.00}s）  ·  {e.EnteredAt:0.0}→{e.ExitedAt:0.0}s",
                    _metaStyle);

                DrawBadge(
                    new Rect(row.xMax - 72f, row.y + 14f, 64f, 20f),
                    ReasonLabel(e.Reason),
                    ReasonColor(e.Reason));
            }

            if (_stickHistoryToEnd && Event.current.type == EventType.Repaint)
            {
                float contentH = _historyScratch.Count * 50f;
                float viewH = historyH;
                if (contentH > viewH)
                    _historyScroll.y = contentH - viewH;
            }

            EditorGUILayout.EndScrollView();

            // 用户拖滚动条则松开跟随
            if (Event.current.type == EventType.ScrollWheel)
                _stickHistoryToEnd = false;

            EndCard();
        }

        private void DrawCurrentCard()
        {
            PetBrain brain = GetSelectedBrain();
            BeginCard("正在做", ColCard);
            if (brain == null || string.IsNullOrEmpty(brain.CurrentBehaviorId))
            {
                GUILayout.Label("（无）", _metaStyle);
                EndCard();
                return;
            }

            DrawBehaviorColorRow(brain.CurrentBehaviorId, brain.CurrentDefinition);
            GUILayout.Space(4);
            GUILayout.Label(
                $"{brain.CurrentElapsed:0.00}s / {brain.CurrentDuration:0.00}s    剩余 {brain.CurrentRemaining:0.00}s",
                _metaStyle);
            float denom = Mathf.Max(0.01f, brain.CurrentDuration);
            Rect bar = EditorGUILayout.GetControlRect(false, 18f);
            Rect fill = new Rect(bar.x, bar.y, bar.width * Mathf.Clamp01(brain.CurrentElapsed / denom), bar.height);
            EditorGUI.DrawRect(bar, new Color(0.1f, 0.1f, 0.12f, 0.9f));
            EditorGUI.DrawRect(fill, BehaviorColor(brain.CurrentBehaviorId));
            GUI.Label(bar, brain.CurrentBehaviorId, EditorStyles.centeredGreyMiniLabel);
            EndCard();
        }

        private void DrawPlannedCard()
        {
            PetBrain brain = GetSelectedBrain();
            BeginCard("即将做", ColCard);
            if (brain == null)
            {
                GUILayout.Label("—", _metaStyle);
                EndCard();
                return;
            }

            if (string.IsNullOrEmpty(brain.PlannedNextBehaviorId))
            {
                GUILayout.Label("（尚未预选 · 可能还在冷却）", _metaStyle);
                EndCard();
                return;
            }

            DrawBehaviorColorRow(brain.PlannedNextBehaviorId, brain.PlannedNextDefinition);
            EndCard();
        }

        private void DrawBehaviorColorRow(string behaviorId, PetBehaviorDefinition def)
        {
            Color c = BehaviorColor(behaviorId);
            using (new EditorGUILayout.HorizontalScope())
            {
                Rect swatch = GUILayoutUtility.GetRect(36f, 36f, GUILayout.Width(36f), GUILayout.Height(36f));
                if (Event.current.type == EventType.Repaint)
                {
                    EditorGUI.DrawRect(swatch, c);
                    GUI.Label(swatch, behaviorId.Length > 0 ? behaviorId.Substring(0, 1).ToUpperInvariant() : "?", _badgeStyle);
                }

                using (new EditorGUILayout.VerticalScope())
                {
                    GUILayout.Label(behaviorId, _idStyle);
                    GUILayout.Label(def != null ? def.name : "?", _metaStyle);
                }
            }
        }

        private void BeginCard(string title, Color bg)
        {
            Rect box = EditorGUILayout.BeginVertical();
            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(box, bg);

            GUILayout.Space(4);
            GUILayout.Label(title, _sectionTitleStyle);
            GUILayout.Space(4);
        }

        private static void EndCard()
        {
            GUILayout.Space(6);
            EditorGUILayout.EndVertical();
        }

        private void DrawSectionHeader(string title, Color bg, Color accent)
        {
            Rect row = EditorGUILayout.GetControlRect(false, 24f);
            EditorGUI.DrawRect(row, bg);
            EditorGUI.DrawRect(new Rect(row.x, row.y, 4f, row.height), accent);
            GUI.Label(new Rect(row.x + 10f, row.y, row.width - 12f, row.height), title, _sectionTitleStyle);
        }

        private void DrawBadge(Rect rect, string text, Color color)
        {
            EditorGUI.DrawRect(rect, color);
            GUI.Label(rect, text, _badgeStyle);
        }

        /// <summary>按 behaviorId 稳定映射到区分色（同 id 始终同色）。</summary>
        private static Color BehaviorColor(string behaviorId)
        {
            if (string.IsNullOrEmpty(behaviorId))
                return new Color(0.4f, 0.4f, 0.45f, 1f);

            switch (behaviorId)
            {
                case "stand": return new Color(0.45f, 0.72f, 0.95f, 1f);
                case "walk": return new Color(0.40f, 0.82f, 0.55f, 1f);
                case "run": return new Color(0.95f, 0.55f, 0.30f, 1f);
                case "sleep": return new Color(0.62f, 0.48f, 0.90f, 1f);

                case "sleepy_nap": return new Color(0.72f, 0.55f, 0.90f, 1f);
                case "hum_along": return new Color(0.65f, 0.55f, 0.95f, 1f);
                case "coin_peek": return new Color(0.95f, 0.82f, 0.25f, 1f);
                case "snack_munch": return new Color(0.92f, 0.58f, 0.32f, 1f);
                case "listen_radio": return new Color(0.85f, 0.45f, 0.70f, 1f);
            }

            unchecked
            {
                int hash = behaviorId.GetHashCode();
                float h = ((hash >> 8) & 0xFF) / 255f;
                float s = 0.45f + ((hash >> 16) & 0xFF) / 255f * 0.35f;
                float v = 0.75f + (hash & 0xFF) / 255f * 0.2f;
                Color c = Color.HSVToRGB(h, s, v);
                c.a = 1f;
                return c;
            }
        }

        private static string ReasonLabel(string reason)
        {
            switch (reason)
            {
                case "complete": return "完成";
                case "request": return "请求";
                case "interrupt": return "打断";
                case "start": return "启动";
                case "set_group": return "换组";
                case "recover": return "恢复";
                default: return string.IsNullOrEmpty(reason) ? "—" : reason;
            }
        }

        private static Color ReasonColor(string reason)
        {
            switch (reason)
            {
                case "complete": return new Color(0.25f, 0.55f, 0.35f, 0.95f);
                case "request": return new Color(0.35f, 0.45f, 0.75f, 0.95f);
                case "interrupt": return new Color(0.75f, 0.35f, 0.25f, 0.95f);
                case "start": return new Color(0.45f, 0.45f, 0.5f, 0.95f);
                default: return new Color(0.35f, 0.35f, 0.4f, 0.95f);
            }
        }

        private static void DrawPortrait(Rect rect, Sprite sprite)
        {
            EditorGUI.DrawRect(rect, new Color(0.08f, 0.08f, 0.1f, 0.85f));
            if (sprite == null || sprite.texture == null)
            {
                GUI.Label(rect, "—", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            Rect draw = new Rect(rect.x + 2f, rect.y + 2f, rect.width - 4f, rect.height - 4f);
            Texture2D tex = sprite.texture;
            Rect tr = sprite.textureRect;
            Rect uv = new Rect(
                tr.x / tex.width,
                tr.y / tex.height,
                tr.width / tex.width,
                tr.height / tex.height);

            float aspect = tr.width / Mathf.Max(1f, tr.height);
            Rect fitted = draw;
            if (aspect > 1f)
            {
                float h = draw.width / aspect;
                fitted = new Rect(draw.x, draw.y + (draw.height - h) * 0.5f, draw.width, h);
            }
            else if (aspect < 1f)
            {
                float w = draw.height * aspect;
                fitted = new Rect(draw.x + (draw.width - w) * 0.5f, draw.y, w, draw.height);
            }

            GUI.DrawTextureWithTexCoords(fitted, tex, uv, true);
        }

        private static Sprite ResolvePortrait(PetBrain brain)
        {
            if (brain == null)
                return null;

            SpriteRenderer[] renderers = brain.GetComponentsInChildren<SpriteRenderer>(true);
            Sprite best = null;
            float bestArea = 0f;
            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer sr = renderers[i];
                if (sr == null || !sr.enabled || sr.sprite == null)
                    continue;
                Rect r = sr.sprite.rect;
                float area = r.width * r.height;
                if (area > bestArea)
                {
                    bestArea = area;
                    best = sr.sprite;
                }
            }

            if (best != null)
                return best;

            LubyInstanceComponent luby = brain.GetComponent<LubyInstanceComponent>();
            if (luby?.Template != null && luby.Template.previewIcon != null)
                return luby.Template.previewIcon;

            return null;
        }

        private PetBrain GetSelectedBrain()
        {
            if (!EditorApplication.isPlaying || _brains.Count == 0)
                return null;
            _selectedIndex = Mathf.Clamp(_selectedIndex, 0, _brains.Count - 1);
            PetBrain brain = _brains[_selectedIndex];
            return brain;
        }

        private void RefreshBrainList()
        {
            _brains.Clear();
            _brainLabels.Clear();
            if (!EditorApplication.isPlaying)
            {
                _selectedIndex = 0;
                return;
            }

            PetBrain[] found = Object.FindObjectsOfType<PetBrain>();
            for (int i = 0; i < found.Length; i++)
            {
                PetBrain brain = found[i];
                if (brain == null || !brain.isActiveAndEnabled)
                    continue;
                _brains.Add(brain);
                _brainLabels.Add(BuildLabel(brain));
            }

            _selectedIndex = Mathf.Clamp(_selectedIndex, 0, Mathf.Max(0, _brains.Count - 1));
        }

        private static string BuildLabel(PetBrain brain)
        {
            LubyInstanceComponent luby = brain.GetComponent<LubyInstanceComponent>();
            if (luby != null && luby.Data != null)
            {
                string shortId = luby.InstanceId;
                if (!string.IsNullOrEmpty(shortId) && shortId.Length > 8)
                    shortId = shortId.Substring(0, 8);
                return $"{luby.Data.templateId}\n{shortId}";
            }

            return brain.gameObject.name;
        }
    }
}
#endif
