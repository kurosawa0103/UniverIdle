// This code is part of the Fungus library (https://github.com/snozbot/fungus)
// It is released for free under the MIT open source license (https://github.com/snozbot/fungus/blob/master/LICENSE)

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace Fungus.EditorUtils
{
    [CustomEditor (typeof(Say))]
    public class SayEditor : CommandEditor
    {
        public static bool showTagHelp;
        public Texture2D blackTex;
        
        public static void DrawTagHelpLabel()
        {
            string tagsText = TextTagParser.GetTagHelp();

            if (CustomTag.activeCustomTags.Count > 0)
            {
                tagsText += "\n\n\t-------- CUSTOM TAGS --------";
                List<Transform> activeCustomTagGroup = new List<Transform>();
                foreach (CustomTag ct in CustomTag.activeCustomTags)
                {
                    if(ct.transform.parent != null)
                    {
                        if (!activeCustomTagGroup.Contains(ct.transform.parent.transform))
                        {
                            activeCustomTagGroup.Add(ct.transform.parent.transform);
                        }
                    }
                    else
                    {
                        activeCustomTagGroup.Add(ct.transform);
                    }
                }
                foreach(Transform parent in activeCustomTagGroup)
                {
                    string tagName = parent.name;
                    string tagStartSymbol = "";
                    string tagEndSymbol = "";
                    CustomTag parentTag = parent.GetComponent<CustomTag>();
                    if (parentTag != null)
                    {
                        tagName = parentTag.name;
                        tagStartSymbol = parentTag.TagStartSymbol;
                        tagEndSymbol = parentTag.TagEndSymbol;
                    }
                    tagsText += "\n\n\t" + tagStartSymbol + " " + tagName + " " + tagEndSymbol;
                    foreach(Transform child in parent)
                    {
                        tagName = child.name;
                        tagStartSymbol = "";
                        tagEndSymbol = "";
                        CustomTag childTag = child.GetComponent<CustomTag>();
                        if (childTag != null)
                        {
                            tagName = childTag.name;
                            tagStartSymbol = childTag.TagStartSymbol;
                            tagEndSymbol = childTag.TagEndSymbol;
                        }
                            tagsText += "\n\t      " + tagStartSymbol + " " + tagName + " " + tagEndSymbol;
                    }
                }
            }
            tagsText += "\n";
            float pixelHeight = EditorStyles.miniLabel.CalcHeight(new GUIContent(tagsText), EditorGUIUtility.currentViewWidth);
            EditorGUILayout.SelectableLabel(tagsText, GUI.skin.GetStyle("HelpBox"), GUILayout.MinHeight(pixelHeight));
        }
        
        protected SerializedProperty characterProp;
        protected SerializedProperty portraitProp;
        protected SerializedProperty storyTextProp;
        protected SerializedProperty descriptionProp;
        protected SerializedProperty voiceOverClipProp;
        protected SerializedProperty showAlwaysProp;
        protected SerializedProperty showCountProp;
        protected SerializedProperty extendPreviousProp;
        protected SerializedProperty fadeWhenDoneProp;
        protected SerializedProperty waitForClickProp;
        protected SerializedProperty stopVoiceoverProp;
        protected SerializedProperty setSayDialogProp;
        protected SerializedProperty waitForVOProp;
        protected SerializedProperty useCustomFontSizeProp;
        protected SerializedProperty customFontSizeProp;
        protected SerializedProperty languageFontSizesProp;
        protected SerializedProperty useCustomNameFontSizeProp;
        protected SerializedProperty customNameFontSizeProp;
        protected SerializedProperty languageNameFontSizesProp;

        public override void OnEnable()
        {
            base.OnEnable();

            characterProp = serializedObject.FindProperty("character");
            portraitProp = serializedObject.FindProperty("portrait");
            storyTextProp = serializedObject.FindProperty("storyText");
            descriptionProp = serializedObject.FindProperty("description");
            voiceOverClipProp = serializedObject.FindProperty("voiceOverClip");
            showAlwaysProp = serializedObject.FindProperty("showAlways");
            showCountProp = serializedObject.FindProperty("showCount");
            extendPreviousProp = serializedObject.FindProperty("extendPrevious");
            fadeWhenDoneProp = serializedObject.FindProperty("fadeWhenDone");
            waitForClickProp = serializedObject.FindProperty("waitForClick");
            stopVoiceoverProp = serializedObject.FindProperty("stopVoiceover");
            setSayDialogProp = serializedObject.FindProperty("setSayDialog");
            waitForVOProp = serializedObject.FindProperty("waitForVO");
            useCustomFontSizeProp = serializedObject.FindProperty("useCustomFontSize");
            customFontSizeProp = serializedObject.FindProperty("customFontSize");
            languageFontSizesProp = serializedObject.FindProperty("languageFontSizes");
            useCustomNameFontSizeProp = serializedObject.FindProperty("useCustomNameFontSize");
            customNameFontSizeProp = serializedObject.FindProperty("customNameFontSize");
            languageNameFontSizesProp = serializedObject.FindProperty("languageNameFontSizes");

            if (blackTex == null)
            {
                blackTex = CustomGUI.CreateBlackTexture();
            }
        }
        
        protected virtual void OnDisable()
        {
            DestroyImmediate(blackTex);
        }

        public override void DrawCommandGUI() 
        {
            serializedObject.Update();

            bool showPortraits = false;
            CommandEditor.ObjectField<Character>(characterProp,
                                                new GUIContent("Character", "Character that is speaking"),
                                                new GUIContent("<None>"),
                                                Character.ActiveCharacters);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(" ");
            characterProp.objectReferenceValue = (Character) EditorGUILayout.ObjectField(characterProp.objectReferenceValue, typeof(Character), true);
            EditorGUILayout.EndHorizontal();

            Say t = target as Say;

            // Only show portrait selection if...
            if (t._Character != null &&              // Character is selected
                t._Character.Portraits != null &&    // Character has a portraits field
                t._Character.Portraits.Count > 0 )   // Selected Character has at least 1 portrait
            {
                showPortraits = true;    
            }

            if (showPortraits) 
            {
                CommandEditor.ObjectField<Sprite>(portraitProp, 
                                                  new GUIContent("Portrait", "Portrait representing speaking character"), 
                                                  new GUIContent("<None>"),
                                                  t._Character.Portraits);
            }
            else
            {
                if (!t.ExtendPrevious)
                {
                    t.Portrait = null;
                }
            }
            
            EditorGUILayout.PropertyField(storyTextProp);

            EditorGUILayout.PropertyField(descriptionProp);

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.PropertyField(extendPreviousProp);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button(new GUIContent("Tag Help", "View available tags"), new GUIStyle(EditorStyles.miniButton)))
            {
                showTagHelp = !showTagHelp;
            }
            EditorGUILayout.EndHorizontal();
            
            if (showTagHelp)
            {
                DrawTagHelpLabel();
            }
            
            EditorGUILayout.Separator();
            
            EditorGUILayout.PropertyField(voiceOverClipProp, 
                                          new GUIContent("Voice Over Clip", "Voice over audio to play when the text is displayed"));

            EditorGUILayout.PropertyField(showAlwaysProp);
            
            if (showAlwaysProp.boolValue == false)
            {
                EditorGUILayout.PropertyField(showCountProp);
            }

            GUIStyle centeredLabel = new GUIStyle(EditorStyles.label);
            centeredLabel.alignment = TextAnchor.MiddleCenter;
            GUIStyle leftButton = new GUIStyle(EditorStyles.miniButtonLeft);
            leftButton.fontSize = 10;
            leftButton.font = EditorStyles.toolbarButton.font;
            GUIStyle rightButton = new GUIStyle(EditorStyles.miniButtonRight);
            rightButton.fontSize = 10;
            rightButton.font = EditorStyles.toolbarButton.font;

            EditorGUILayout.PropertyField(fadeWhenDoneProp);
            EditorGUILayout.PropertyField(waitForClickProp);
            EditorGUILayout.PropertyField(stopVoiceoverProp);
            EditorGUILayout.PropertyField(setSayDialogProp);
            EditorGUILayout.PropertyField(waitForVOProp);
            
            // 自定义字号设置
            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("自定义字号", EditorStyles.boldLabel);
            
            // 对话文字字号
            EditorGUILayout.PropertyField(useCustomFontSizeProp, new GUIContent("启用对话文字自定义字号", "勾选后启用对话文字自定义字号，仅对当前这句话生效"));
            
            if (useCustomFontSizeProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(customFontSizeProp, new GUIContent("对话文字字号大小（默认）", "对话文字默认字号大小，如果配置了语言特定字号则优先使用对应语言的字号"));
                
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("按语言配置字号（可选）", EditorStyles.miniLabel);
                EditorGUILayout.PropertyField(languageFontSizesProp, new GUIContent("语言特定字号", "为不同语言配置不同的字号，如果配置了则优先使用对应语言的字号"), true);
                EditorGUI.indentLevel--;
            }
            
            // 名字框字号
            EditorGUILayout.PropertyField(useCustomNameFontSizeProp, new GUIContent("启用名字框自定义字号", "勾选后启用名字框自定义字号，仅对当前这句话生效"));
            
            if (useCustomNameFontSizeProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(customNameFontSizeProp, new GUIContent("名字框字号大小（默认）", "名字框默认字号大小，如果配置了语言特定字号则优先使用对应语言的字号"));
                
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("按语言配置字号（可选）", EditorStyles.miniLabel);
                EditorGUILayout.PropertyField(languageNameFontSizesProp, new GUIContent("名字框语言特定字号", "为不同语言配置不同的名字框字号，如果配置了则优先使用对应语言的字号"), true);
                EditorGUI.indentLevel--;
            }
            
            if (showPortraits && t.Portrait != null)
            {
                Texture2D characterTexture = t.Portrait.texture;
                float aspect = (float)characterTexture.width / (float)characterTexture.height;
                Rect previewRect = GUILayoutUtility.GetAspectRect(aspect, GUILayout.Width(100), GUILayout.ExpandWidth(true));
                if (characterTexture != null)
                {
                    GUI.DrawTexture(previewRect,characterTexture,ScaleMode.ScaleToFit,true,aspect);
                }
            }
            
            serializedObject.ApplyModifiedProperties();
        }
    }    
}