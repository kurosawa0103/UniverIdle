#if UNITY_EDITOR
using DesktopPet.AI.Editor;
using DesktopPet.Decor;
using UnityEditor;
using UnityEngine;

namespace DesktopPet.Luby.Editor
{
    /// <summary>小剧场站位预览窗口：相对道具锚点/中点示意，可拖圆点改 offsetX、右键改朝向。</summary>
    public sealed class LubyTheaterStagePreviewWindow : EditorWindow
    {
        private static readonly Color[] RolePalette =
        {
            new Color(0.3f, 0.85f, 1f, 0.95f),
            new Color(1f, 0.45f, 0.7f, 0.95f),
            new Color(0.5f, 1f, 0.45f, 0.95f),
            new Color(1f, 0.7f, 0.3f, 0.95f)
        };

        private const float PreviewHeight = 240f;
        private const float MarkerRadius = 12f;
        private const float FacingArrowLen = 26f;
        private const float MinHalfSpan = 1.2f;

        private LubyTheaterEventDefinition _event;
        private SerializedObject _so;
        private Vector2 _scroll;
        private bool _followSelection = true;
        private bool _drawSceneGizmos = true;
        private int _dragRoleIndex = -1;

        [MenuItem("桌宠/小剧场站位预览")]
        public static void Open()
        {
            Open(Selection.activeObject as LubyTheaterEventDefinition);
        }

        public static void Open(LubyTheaterEventDefinition evt)
        {
            var window = GetWindow<LubyTheaterStagePreviewWindow>();
            window.titleContent = new GUIContent("小剧场站位");
            window.minSize = new Vector2(420f, 360f);
            if (evt != null)
                window.SetEvent(evt);
            window.Show();
            window.Focus();
        }

        private void OnEnable()
        {
            Selection.selectionChanged += OnSelectionChanged;
            SceneView.duringSceneGui += OnSceneGui;
            if (_followSelection)
                TryBindFromSelection();
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            SceneView.duringSceneGui -= OnSceneGui;
        }

        private void OnSelectionChanged()
        {
            if (_followSelection)
                TryBindFromSelection();
            Repaint();
        }

        private void TryBindFromSelection()
        {
            if (Selection.activeObject is LubyTheaterEventDefinition evt)
                SetEvent(evt);
        }

        private void SetEvent(LubyTheaterEventDefinition evt)
        {
            if (_event == evt && _so != null)
                return;
            _event = evt;
            _so = evt != null ? new SerializedObject(evt) : null;
            _dragRoleIndex = -1;
            Repaint();
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.LabelField("小剧场站位预览", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "示意相对舞台中心（道具锚点或伙伴中点）。左键拖圆点改 offsetX；右键圆点循环切换最终朝向；下方列表也可改。",
                MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                var next = (LubyTheaterEventDefinition)EditorGUILayout.ObjectField(
                    "事件", _event, typeof(LubyTheaterEventDefinition), false);
                if (EditorGUI.EndChangeCheck())
                    SetEvent(next);

                if (GUILayout.Button("用选中", GUILayout.Width(64f)))
                    TryBindFromSelection();
            }

            _followSelection = EditorGUILayout.ToggleLeft("跟随 Project 选中事件", _followSelection);
            _drawSceneGizmos = EditorGUILayout.ToggleLeft("Scene 画出真实站位与朝向", _drawSceneGizmos);

            EditorGUILayout.Space(6f);

            if (_event == null || _so == null)
            {
                EditorGUILayout.HelpBox("指定或选中一个 LubyTheaterEventDefinition。", MessageType.Warning);
                EditorGUILayout.EndScrollView();
                return;
            }

            _so.Update();

            string itemId = _event.ResolveStagePropItemId();
            bool peerMode = string.IsNullOrEmpty(itemId);
            EditorGUILayout.LabelField(peerMode
                ? "站位模式：无道具 · 示意中心 = 演员中点"
                : "舞台道具：" + itemId);

            DrawStageCanvas(peerMode);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("角色站位与朝向", EditorStyles.boldLabel);
            DrawRoleFields();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("聚焦 Scene"))
                    SceneView.RepaintAll();
                if (GUILayout.Button("在 Project 中选中事件"))
                    Selection.activeObject = _event;
            }

            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("调试", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "完整调试日志（演员槽/道具/门闸）请用：桌宠 → AI 行为日志 → 小剧场 页签。",
                MessageType.None);
            if (GUILayout.Button("打开 AI 行为日志（小剧场）"))
                PetAiBehaviorLogWindow.OpenTheaterTab();

            _so.ApplyModifiedProperties();
            EditorGUILayout.EndScrollView();
        }

        private void DrawStageCanvas(bool peerMode)
        {
            Rect rect = GUILayoutUtility.GetRect(10f, PreviewHeight, GUILayout.ExpandWidth(true));
            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(rect, new Color(0.12f, 0.12f, 0.14f, 1f));

            float halfSpan = ComputeHalfSpan(_event);
            float midY = rect.y + rect.height * 0.55f;
            float centerX = rect.x + rect.width * 0.5f;

            Handles.BeginGUI();
            Handles.color = new Color(1f, 1f, 1f, 0.2f);
            Handles.DrawLine(new Vector3(rect.x + 8f, midY), new Vector3(rect.xMax - 8f, midY));
            Handles.color = peerMode
                ? new Color(0.55f, 0.85f, 1f, 0.95f)
                : new Color(1f, 0.85f, 0.2f, 0.95f);
            Handles.DrawSolidDisc(new Vector3(centerX, midY), Vector3.forward, 8f);
            Handles.EndGUI();

            GUI.Label(
                new Rect(centerX - 28f, midY + 14f, 56f, 16f),
                peerMode ? "中点" : "道具",
                EditorStyles.centeredGreyMiniLabel);
            DrawTick(rect, centerX, midY, halfSpan, -1f);
            DrawTick(rect, centerX, midY, halfSpan, 1f);

            SerializedProperty rolesProp = _so.FindProperty("roles");
            if (rolesProp == null || !rolesProp.isArray)
                return;

            int colorIdx = 0;
            for (int ri = 0; ri < rolesProp.arraySize; ri++)
            {
                SerializedProperty slot = rolesProp.GetArrayElementAtIndex(ri);
                SerializedProperty oxProp = slot.FindPropertyRelative("stageOffsetX");
                SerializedProperty facingProp = slot.FindPropertyRelative("stageFacing");
                SerializedProperty keyProp = slot.FindPropertyRelative("roleKey");
                SerializedProperty countProp = slot.FindPropertyRelative("count");
                if (oxProp == null)
                    continue;

                int count = countProp != null ? Mathf.Max(1, countProp.intValue) : 1;
                Color col = RolePalette[colorIdx % RolePalette.Length];
                colorIdx++;
                int controlId = GUIUtility.GetControlID(FocusType.Passive);
                var facing = facingProp != null
                    ? (LubyTheaterStageFacing)facingProp.enumValueIndex
                    : LubyTheaterStageFacing.Auto;

                for (int c = 0; c < count; c++)
                {
                    float ox = LubyTheaterStaging.ResolveRoleOffsetX(
                        oxProp.floatValue, c, count);
                    float px = OffsetToPixelX(rect, centerX, halfSpan, ox);
                    Vector2 marker = new Vector2(px, midY);
                    float facingSign = ResolvePreviewFacingSign(facing, ox);

                    if (Event.current.type == EventType.Repaint)
                    {
                        Handles.BeginGUI();
                        Handles.color = new Color(col.r, col.g, col.b, 0.35f);
                        Handles.DrawLine(new Vector3(centerX, midY), new Vector3(px, midY));
                        Handles.color = col;
                        Handles.DrawSolidDisc(marker, Vector3.forward, MarkerRadius);
                        DrawFacingArrowGui(marker, facingSign, col, facing);
                        Handles.EndGUI();
                    }

                    string label = keyProp != null && !string.IsNullOrEmpty(keyProp.stringValue)
                        ? keyProp.stringValue
                        : "role";
                    if (count > 1)
                        label += "#" + (c + 1);
                    label += $"\n{ox:0.##}";
                    GUI.Label(new Rect(px - 40f, midY - 56f, 80f, 36f), label, EditorStyles.centeredGreyMiniLabel);
                    GUI.Label(
                        new Rect(px - 36f, midY + MarkerRadius + 10f, 72f, 14f),
                        FacingShortLabel(facing),
                        EditorStyles.centeredGreyMiniLabel);

                    if (c != 0)
                        continue;

                    Rect hit = new Rect(px - MarkerRadius - 2f, midY - MarkerRadius - 2f,
                        (MarkerRadius + 2f) * 2f, (MarkerRadius + 2f) * 2f);
                    EditorGUIUtility.AddCursorRect(hit, MouseCursor.SlideArrow);

                    Event e = Event.current;
                    switch (e.GetTypeForControl(controlId))
                    {
                        case EventType.MouseDown:
                            if (hit.Contains(e.mousePosition))
                            {
                                if (e.button == 1 && facingProp != null)
                                {
                                    facingProp.enumValueIndex = (facingProp.enumValueIndex + 1) % 4;
                                    e.Use();
                                    Repaint();
                                    SceneView.RepaintAll();
                                }
                                else if (e.button == 0)
                                {
                                    GUIUtility.hotControl = controlId;
                                    _dragRoleIndex = ri;
                                    e.Use();
                                }
                            }
                            break;
                        case EventType.MouseDrag:
                            if (GUIUtility.hotControl == controlId && _dragRoleIndex == ri)
                            {
                                float newOx = PixelXToOffset(rect, centerX, halfSpan, e.mousePosition.x);
                                oxProp.floatValue = Mathf.Round(newOx * 20f) / 20f;
                                e.Use();
                                Repaint();
                                SceneView.RepaintAll();
                            }
                            break;
                        case EventType.MouseUp:
                            if (GUIUtility.hotControl == controlId)
                            {
                                GUIUtility.hotControl = 0;
                                _dragRoleIndex = -1;
                                e.Use();
                            }
                            break;
                    }
                }
            }
        }

        private void DrawRoleFields()
        {
            SerializedProperty rolesProp = _so.FindProperty("roles");
            if (rolesProp == null || !rolesProp.isArray)
                return;

            for (int ri = 0; ri < rolesProp.arraySize; ri++)
            {
                SerializedProperty slot = rolesProp.GetArrayElementAtIndex(ri);
                SerializedProperty oxProp = slot.FindPropertyRelative("stageOffsetX");
                SerializedProperty facingProp = slot.FindPropertyRelative("stageFacing");
                SerializedProperty keyProp = slot.FindPropertyRelative("roleKey");
                if (oxProp == null)
                    continue;

                string key = keyProp != null && !string.IsNullOrEmpty(keyProp.stringValue)
                    ? keyProp.stringValue
                    : "role#" + ri;
                Color col = RolePalette[ri % RolePalette.Length];

                using (new EditorGUILayout.HorizontalScope())
                {
                    Rect swatch = GUILayoutUtility.GetRect(14f, 14f, GUILayout.Width(14f));
                    if (Event.current.type == EventType.Repaint)
                        EditorGUI.DrawRect(swatch, col);
                    EditorGUILayout.PropertyField(oxProp, new GUIContent(key + " · X"), GUILayout.MinWidth(120f));
                    if (facingProp != null)
                        EditorGUILayout.PropertyField(facingProp, GUIContent.none, GUILayout.Width(108f));
                }
            }
        }

        private void OnSceneGui(SceneView view)
        {
            if (!_drawSceneGizmos || _event == null || _event.roles == null)
                return;

            string itemId = _event.ResolveStagePropItemId();
            bool peerMode = string.IsNullOrEmpty(itemId);
            float anchorX;
            float groundY;
            Vector3 anchor;
            PlacedDecor stageProp = null;

            if (peerMode)
            {
                groundY = DesktopPetServices.Ground != null
                    ? DesktopPetServices.Ground.ResolveGroundY()
                    : 0f;
                anchorX = 0f;
                anchor = new Vector3(anchorX, groundY, 0f);
                Handles.color = new Color(0.55f, 0.85f, 1f, 0.9f);
                Handles.SphereHandleCap(0, anchor, Quaternion.identity, 0.12f, EventType.Repaint);
                Handles.Label(anchor + Vector3.up * 0.25f, "舞台·中点（示意）");
            }
            else
            {
                stageProp = LubyTheaterStagingEditor.FindPropInOpenScenes(itemId);
                if (stageProp == null)
                    return;

                anchor = LubyTheaterStaging.GetPropAnchorWorld(stageProp);
                anchorX = anchor.x;
                groundY = DesktopPetServices.Ground != null
                    ? DesktopPetServices.Ground.ResolveGroundY()
                    : anchor.y;
                Handles.color = new Color(1f, 0.85f, 0.2f, 0.9f);
                Handles.SphereHandleCap(0, anchor, Quaternion.identity, 0.12f, EventType.Repaint);
                Handles.Label(anchor + Vector3.up * 0.25f, "舞台·" + itemId);
            }

            int colorIdx = 0;
            for (int ri = 0; ri < _event.roles.Count; ri++)
            {
                LubyTheaterRoleSlot slot = _event.roles[ri];
                if (slot == null)
                    continue;

                int count = Mathf.Max(1, slot.count);
                for (int c = 0; c < count; c++)
                {
                    float offsetX = LubyTheaterStaging.ResolveRoleOffsetX(
                        slot.stageOffsetX, c, count);
                    float x = peerMode
                        ? LubyTheaterStaging.GetPeerStageWorldX(anchorX, offsetX)
                        : LubyTheaterStaging.GetStageWorldX(stageProp, offsetX);
                    Vector3 pos = new Vector3(x, groundY, anchor.z);

                    Handles.color = RolePalette[colorIdx % RolePalette.Length];
                    Handles.DrawSolidDisc(pos, Vector3.forward, 0.18f);
                    Handles.DrawLine(anchor, pos);
                    float sign = LubyTheaterStaging.ResolveStageFacingSign(slot.stageFacing, x, anchorX);
                    DrawSceneFacingArrow(pos, sign, slot.stageFacing);
                    string label = string.IsNullOrEmpty(slot.roleKey) ? "role" : slot.roleKey;
                    if (count > 1)
                        label += "#" + (c + 1);
                    label += $"  ox={offsetX:0.##} · {FacingShortLabel(slot.stageFacing)}";
                    Handles.Label(pos + Vector3.up * 0.35f, label);
                    colorIdx++;
                }
            }
        }

        private static float ComputeHalfSpan(LubyTheaterEventDefinition evt)
        {
            float maxAbs = MinHalfSpan;
            if (evt.roles == null)
                return maxAbs;

            for (int i = 0; i < evt.roles.Count; i++)
            {
                LubyTheaterRoleSlot slot = evt.roles[i];
                if (slot == null)
                    continue;
                int count = Mathf.Max(1, slot.count);
                for (int c = 0; c < count; c++)
                {
                    float ox = Mathf.Abs(LubyTheaterStaging.ResolveRoleOffsetX(
                        slot.stageOffsetX, c, count));
                    if (ox > maxAbs)
                        maxAbs = ox;
                }
            }

            return maxAbs + 0.4f;
        }

        private static float OffsetToPixelX(Rect rect, float centerX, float halfSpan, float offsetX)
        {
            float usable = rect.width - 24f;
            return centerX + (offsetX / halfSpan) * (usable * 0.5f);
        }

        private static float PixelXToOffset(Rect rect, float centerX, float halfSpan, float pixelX)
        {
            float usable = rect.width - 24f;
            if (usable < 1f)
                return 0f;
            return (pixelX - centerX) / (usable * 0.5f) * halfSpan;
        }

        private static float ResolvePreviewFacingSign(LubyTheaterStageFacing facing, float offsetX)
        {
            return LubyTheaterStaging.ResolveStageFacingSign(facing, offsetX, 0f);
        }

        private static string FacingShortLabel(LubyTheaterStageFacing facing)
        {
            switch (facing)
            {
                case LubyTheaterStageFacing.Left:
                    return "← 左";
                case LubyTheaterStageFacing.Right:
                    return "→ 右";
                case LubyTheaterStageFacing.FaceCenter:
                    return "◎ 向心";
                default:
                    return "Auto";
            }
        }

        private static void DrawFacingArrowGui(Vector2 origin, float sign, Color col, LubyTheaterStageFacing facing)
        {
            Handles.BeginGUI();
            if (facing == LubyTheaterStageFacing.Auto)
            {
                Handles.color = new Color(col.r, col.g, col.b, 0.45f);
                Handles.DrawLine(origin + Vector2.left * 8f, origin + Vector2.right * 8f);
                Handles.EndGUI();
                return;
            }

            if (Mathf.Abs(sign) < 0.01f)
            {
                Handles.EndGUI();
                return;
            }

            Vector2 end = origin + new Vector2(sign * FacingArrowLen, 0f);
            Handles.color = new Color(col.r * 0.85f, col.g * 0.85f, col.b * 0.85f, 1f);
            Handles.DrawLine(origin, end);
            Vector2 dir = (end - origin).normalized;
            Vector2 perp = new Vector2(-dir.y, dir.x);
            Handles.DrawLine(end, end - dir * 7f + perp * 5f);
            Handles.DrawLine(end, end - dir * 7f - perp * 5f);
            Handles.EndGUI();
        }

        private static void DrawSceneFacingArrow(Vector3 pos, float sign, LubyTheaterStageFacing facing)
        {
            if (facing == LubyTheaterStageFacing.Auto)
            {
                Handles.color = new Color(1f, 1f, 1f, 0.35f);
                Handles.DrawLine(pos + Vector3.left * 0.12f, pos + Vector3.right * 0.12f);
                return;
            }

            if (Mathf.Abs(sign) < 0.01f)
                return;

            Vector3 end = pos + Vector3.right * (sign * 0.42f);
            Handles.DrawLine(pos, end);
            Vector3 dir = (end - pos).normalized;
            Vector3 perp = Vector3.Cross(dir, Vector3.forward);
            Handles.DrawLine(end, end - dir * 0.1f + perp * 0.07f);
            Handles.DrawLine(end, end - dir * 0.1f - perp * 0.07f);
        }

        private static void DrawTick(Rect rect, float centerX, float midY, float halfSpan, float worldX)
        {
            float px = OffsetToPixelX(rect, centerX, halfSpan, worldX);
            Handles.BeginGUI();
            Handles.color = new Color(1f, 1f, 1f, 0.25f);
            Handles.DrawLine(new Vector3(px, midY - 6f), new Vector3(px, midY + 6f));
            Handles.EndGUI();
            GUI.Label(
                new Rect(px - 16f, midY + 8f, 32f, 14f),
                worldX.ToString("0"),
                EditorStyles.centeredGreyMiniLabel);
        }
    }
}
#endif
