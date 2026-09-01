#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace DesktopPet.Luby.Editor
{
    [CustomEditor(typeof(LubyTheaterEventDefinition))]
    public sealed class LubyTheaterEventDefinitionEditor : OdinEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space(8f);
            Sirenix.Utilities.Editor.SirenixEditorGUI.BeginBox("站位预览");
            EditorGUILayout.HelpBox(
                "独立窗口：拖圆点改 offsetX；可选 Scene 真锚点。",
                MessageType.None);
            if (GUILayout.Button("打开小剧场站位预览窗口", GUILayout.Height(28f)))
                LubyTheaterStagePreviewWindow.Open(target as LubyTheaterEventDefinition);
            Sirenix.Utilities.Editor.SirenixEditorGUI.EndBox();
        }
    }
}
#endif
