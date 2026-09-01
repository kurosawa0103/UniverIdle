// This code is part of the Fungus library (https://github.com/snozbot/fungus)
// It is released for free under the MIT open source license (https://github.com/snozbot/fungus/blob/master/LICENSE)

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace Fungus.EditorUtils
{

    [CustomEditor (typeof(SetVariable))]
    public class SetVariableEditor : CommandEditor
    {
        protected SerializedProperty anyVarProp;
        protected SerializedProperty setOperatorProp;
        
        public override void OnEnable()
        {
            base.OnEnable();

            anyVarProp = serializedObject.FindProperty("anyVar");
            setOperatorProp = serializedObject.FindProperty("setOperator");
        }

        public override void DrawCommandGUI()
        {
            serializedObject.Update();

            SetVariable t = target as SetVariable;

            var flowchart = (Flowchart)t.GetFlowchart();
            if (flowchart == null)
            {
                return;
            }

            // Select Variable
            EditorGUILayout.PropertyField(anyVarProp, true);

            // Get selected variable
            Variable selectedVariable = anyVarProp.FindPropertyRelative("variable").objectReferenceValue as Variable;
            List<GUIContent> operatorsList = new List<GUIContent>();
            List<SetOperator> availableOperators = new List<SetOperator>();
            if (selectedVariable != null)
            {
                AddOperatorIfSupported(selectedVariable, SetOperator.Assign, operatorsList, availableOperators);
                AddOperatorIfSupported(selectedVariable, SetOperator.Negate, operatorsList, availableOperators);
                AddOperatorIfSupported(selectedVariable, SetOperator.Add, operatorsList, availableOperators);
                AddOperatorIfSupported(selectedVariable, SetOperator.Subtract, operatorsList, availableOperators);
                AddOperatorIfSupported(selectedVariable, SetOperator.Multiply, operatorsList, availableOperators);
                AddOperatorIfSupported(selectedVariable, SetOperator.Divide, operatorsList, availableOperators);
            }
            else
            {
                operatorsList.Add(VariableConditionEditor.None);
            }

            int popupIndex = 0;
            if (selectedVariable != null && availableOperators.Count > 0)
            {
                popupIndex = availableOperators.IndexOf(t._SetOperator);
                if (popupIndex < 0)
                    popupIndex = 0;
            }

            popupIndex = EditorGUILayout.Popup(
                new GUIContent("Operation", "Arithmetic operator to use"),
                popupIndex,
                operatorsList.ToArray());

            if (selectedVariable != null && availableOperators.Count > 0)
            {
                setOperatorProp.enumValueIndex = (int)availableOperators[popupIndex];
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static void AddOperatorIfSupported(
            Variable selectedVariable,
            SetOperator setOperator,
            List<GUIContent> operatorsList,
            List<SetOperator> availableOperators)
        {
            if (!selectedVariable.IsArithmeticSupported(setOperator))
                return;

            operatorsList.Add(new GUIContent(VariableUtil.GetSetOperatorDescription(setOperator)));
            availableOperators.Add(setOperator);
        }
    }
}
