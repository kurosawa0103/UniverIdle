#if UNITY_EDITOR
using System.Collections.Generic;
using DesktopPet.Luby;
using UnityEditor;
using UnityEngine;

namespace DesktopPet.AI.Editor
{
    /// <summary>AI 行为日志 ·「小剧场」页签。</summary>
    internal static class PetAiTheaterDebugPanel
    {
        private static LubyTheaterDirector _director;
        private static LubyTheaterCatalog _catalog;
        private static readonly List<LubyTheaterEventDefinition> _events = new List<LubyTheaterEventDefinition>(8);
        private static int _eventIndex;
        private static bool _ignoreGates = true;
        private static bool _autoRefreshLog = true;
        private static bool _showSections;
        private static LubyTheaterDebugReport.Section _logSections = LubyTheaterDebugReport.Section.All;
        private static string _debugLog = string.Empty;
        private static string _lastMessage;
        private static MessageType _lastMessageType = MessageType.Info;
        private static Vector2 _logScroll;
        private static double _lastLogRefresh;
        private static GUIStyle _logStyle;

        public static void Draw()
        {
            RefreshCatalog();
            EnsureLogStyle();

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("进入 Play 后查看调试日志、强制开演。", MessageType.Warning);
                DrawQuickGuide();
                return;
            }

            if (_director == null)
            {
                EditorGUILayout.HelpBox("无 LubyTheaterDirector → 请在场景 LubySystem 上手挂该组件", MessageType.Error);
                DrawQuickGuide();
                return;
            }

            DrawToolbar();
            GUILayout.Space(4f);

            if (!string.IsNullOrEmpty(_lastMessage))
                EditorGUILayout.HelpBox(_lastMessage, _lastMessageType);

            DrawLogArea();
            DrawQuickGuide();
        }

        public static void TickAutoRefresh()
        {
            if (!EditorApplication.isPlaying || !_autoRefreshLog || _events.Count == 0)
                return;
            if (EditorApplication.timeSinceStartup - _lastLogRefresh < 0.35d)
                return;
            RefreshDebugLog(force: false);
        }

        private static void EnsureLogStyle()
        {
            if (_logStyle == null)
                _logStyle = new GUIStyle(EditorStyles.label);
            _logStyle.wordWrap = true;
            _logStyle.fontSize = 11;
            _logStyle.padding = new RectOffset(6, 6, 4, 4);
            _logStyle.richText = true;
        }

        private static string BuildSessionToolbarSummary(LubyTheaterDirector director)
        {
            int active = director.ActiveSessionCount;
            int max = director.MaxConcurrentSessions;
            if (active == 0)
                return "空闲";

            var snaps = new List<LubyTheaterDirector.TheaterSessionSnapshot>(4);
            director.CopyActiveSessionSummaries(snaps);
            var parts = new List<string>(snaps.Count);
            for (int i = 0; i < snaps.Count; i++)
            {
                LubyTheaterDirector.TheaterSessionSnapshot s = snaps[i];
                string phase = s.PhaseLabel == "走位中" ? "走" : "演";
                parts.Add($"场{s.DisplayIndex}{phase}");
            }

            return $"{active}/{max} [{string.Join(" ", parts)}]";
        }

        private static void DrawToolbar()
        {
            string session = BuildSessionToolbarSummary(_director);
            EditorGUILayout.LabelField(
                $"导演 {_director.gameObject.name} · {_catalog?.name ?? "?"} · {_events.Count} 事件 · {session}",
                EditorStyles.miniLabel);

            if (_events.Count == 0)
            {
                EditorGUILayout.HelpBox("目录无事件", MessageType.Warning);
                return;
            }

            EditorGUI.BeginChangeCheck();
            string[] labels = new string[_events.Count];
            for (int i = 0; i < _events.Count; i++)
                labels[i] = _events[i]?.eventId ?? "?";

            using (new EditorGUILayout.HorizontalScope())
            {
                _eventIndex = EditorGUILayout.Popup(_eventIndex, labels, GUILayout.MinWidth(120f));
                _ignoreGates = GUILayout.Toggle(_ignoreGates, "忽略 Hub/手持", EditorStyles.miniButton);
                _autoRefreshLog = GUILayout.Toggle(_autoRefreshLog, "自动刷新", EditorStyles.miniButton);
            }

            _showSections = EditorGUILayout.Foldout(_showSections, "输出段落", true);
            if (_showSections)
            {
                _logSections = (LubyTheaterDebugReport.Section)EditorGUILayout.EnumFlagsField(_logSections);
                if (GUILayout.Button("全选段落", EditorStyles.miniButton))
                    _logSections = LubyTheaterDebugReport.Section.All;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                int active = _director.ActiveSessionCount;
                int max = _director.MaxConcurrentSessions;
                bool canStartMore = active < max;
                GUI.enabled = canStartMore;
                if (GUILayout.Button("强制开演", GUILayout.Height(22f)))
                    TryStart(force: true);
                if (GUILayout.Button("按规则开演", GUILayout.Height(22f)))
                    TryStart(force: false);
                GUI.enabled = _director.HasActiveSession;
                if (GUILayout.Button("结束全部", GUILayout.Height(22f)))
                {
                    _director.EndCurrent(reselect: true, applyCooldown: false);
                    SetMessage("已结束全部场次。", MessageType.Info);
                    RefreshDebugLog(force: true);
                }
                GUI.enabled = true;
                if (GUILayout.Button("清冷却", GUILayout.Height(22f)))
                {
                    _director.ClearAllEventCooldowns();
                    RefreshDebugLog(force: true);
                }
                if (GUILayout.Button("刷新", GUILayout.Height(22f), GUILayout.Width(48f)))
                    RefreshDebugLog(force: true);
                if (GUILayout.Button("复制", GUILayout.Height(22f), GUILayout.Width(48f)))
                {
                    EditorGUIUtility.systemCopyBuffer = _debugLog ?? string.Empty;
                    SetMessage("已复制。", MessageType.Info);
                }
            }

            if (EditorGUI.EndChangeCheck())
                RefreshDebugLog(force: true);
        }

        private static void DrawLogArea()
        {
            string text = string.IsNullOrEmpty(_debugLog) ? "（点「刷新」）" : _debugLog;
            float viewW = Mathf.Max(100f, EditorGUIUtility.currentViewWidth - 28f);
            float contentH = Mathf.Max(48f, _logStyle.CalcHeight(new GUIContent(text), viewW));

            Rect scrollRect = GUILayoutUtility.GetRect(0f, 0f, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            if (scrollRect.height < 80f)
                scrollRect.height = 80f;

            _logScroll = GUI.BeginScrollView(scrollRect, _logScroll, new Rect(0f, 0f, viewW - 16f, contentH));
            GUI.Label(new Rect(0f, 0f, viewW - 16f, contentH), text, _logStyle);
            GUI.EndScrollView();
        }

        private static void RefreshCatalog()
        {
            if (!EditorApplication.isPlaying)
            {
                _director = null;
                _catalog = null;
                _events.Clear();
                _debugLog = string.Empty;
                return;
            }

            if (_director == null)
                _director = Object.FindObjectOfType<LubyTheaterDirector>();

            LubyTheaterCatalog cat = _director != null ? _director.Catalog : null;
            if (cat == _catalog && _events.Count > 0)
                return;

            _catalog = cat;
            _events.Clear();
            if (_catalog?.events != null)
            {
                for (int i = 0; i < _catalog.events.Count; i++)
                {
                    if (_catalog.events[i] != null)
                        _events.Add(_catalog.events[i]);
                }
            }

            _eventIndex = Mathf.Clamp(_eventIndex, 0, Mathf.Max(0, _events.Count - 1));
            RefreshDebugLog(force: true);
        }

        private static void RefreshDebugLog(bool force)
        {
            if (!EditorApplication.isPlaying || _events.Count == 0)
            {
                _debugLog = string.Empty;
                return;
            }

            if (!force && EditorApplication.timeSinceStartup - _lastLogRefresh < 0.05d)
                return;

            _lastLogRefresh = EditorApplication.timeSinceStartup;
            _debugLog = LubyTheaterDebugReport.Build(
                _events[_eventIndex],
                _logSections,
                _director,
                _ignoreGates,
                ignoreCooldown: _ignoreGates);
        }

        private static void TryStart(bool force)
        {
            if (_eventIndex < 0 || _eventIndex >= _events.Count)
                return;

            LubyTheaterEventDefinition evt = _events[_eventIndex];
            if (_director.TryDebugStartEvent(evt, force || _ignoreGates, force, out string error))
            {
                SetMessage($"已开演 {evt.eventId}", MessageType.Info);
                RefreshDebugLog(force: true);
                return;
            }

            _debugLog = LubyTheaterDebugReport.ColorizePlainReport(error);
            SetMessage("开演失败", MessageType.Warning);
        }

        private static void SetMessage(string text, MessageType type)
        {
            _lastMessage = text;
            _lastMessageType = type;
        }

        private static void DrawQuickGuide()
        {
            EditorGUILayout.LabelField(
                "social_greet：有意图者先找人 · 对方可走开 · 强制开演可忽略意图",
                EditorStyles.centeredGreyMiniLabel);
        }
    }
}
#endif
