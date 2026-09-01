// This code is part of the Fungus library (https://github.com/snozbot/fungus)
// It is released for free under the MIT open source license (https://github.com/snozbot/fungus/blob/master/LICENSE)

using UnityEngine;
using TMPro;
using System;
using System.Collections.Generic;

namespace Fungus
{
    /// <summary>
    /// 语言对应的字号配置
    /// </summary>
    /// <summary>
    /// 语言对应的字号配置
    /// </summary>
    [Serializable]
    public class LanguageFontSize
    {
        [Tooltip("语言代码（例如：EN, ZH, JP）")]
        public string language = "";
        
        [Tooltip("该语言对应的字号")]
        public float fontSize = 38f;
    }
    
    /// <summary>
    /// Writes text in a dialog box.
    /// </summary>
    [CommandInfo("Narrative", 
                 "Say", 
                 "Writes text in a dialog box.")]
    [AddComponentMenu("")]
    public class Say : Command, ILocalizable
    {
        // Removed this tooltip as users's reported it obscures the text box
        [TextArea(5,10)]
        [SerializeField] protected string storyText = "";

        [Tooltip("Notes about this story text for other authors, localization, etc.")]
        [SerializeField] protected string description = "";

        [Tooltip("Character that is speaking")]
        [SerializeField] protected Character character;

        [Tooltip("Portrait that represents speaking character")]
        [SerializeField] protected Sprite portrait;

        [Tooltip("Voiceover audio to play when writing the text")]
        [SerializeField] protected AudioClip voiceOverClip;

        [Tooltip("Always show this Say text when the command is executed multiple times")]
        [SerializeField] protected bool showAlways = true;

        [Tooltip("Number of times to show this Say text when the command is executed multiple times")]
        [SerializeField] protected int showCount = 1;

        [Tooltip("Type this text in the previous dialog box.")]
        [SerializeField] protected bool extendPrevious = false;

        [Tooltip("Fade out the dialog box when writing has finished and not waiting for input.")]
        [SerializeField] protected bool fadeWhenDone = true;

        [Tooltip("Wait for player to click before continuing.")]
        [SerializeField] protected bool waitForClick = true;

        [Tooltip("Stop playing voiceover when text finishes writing.")]
        [SerializeField] protected bool stopVoiceover = true;

        [Tooltip("Wait for the Voice Over to complete before continuing")]
        [SerializeField] protected bool waitForVO = false;

        //add wait for vo that overrides stopvo

        [Tooltip("Sets the active Say dialog with a reference to a Say Dialog object in the scene. All story text will now display using this Say Dialog.")]
        [SerializeField] protected SayDialog setSayDialog;

        [Header("自定义字号")]
        [Tooltip("勾选后启用自定义字号，仅对当前这句话生效")]
        public bool useCustomFontSize = false;
        
        [Tooltip("自定义字号大小（仅在勾选启用自定义字号时生效，如果配置了语言特定字号则优先使用）")]
        public float customFontSize = 38f;
        
        [Tooltip("按语言配置特定字号（可选，如果配置了则优先使用对应语言的字号）")]
        public List<LanguageFontSize> languageFontSizes = new List<LanguageFontSize>();
        
        [Tooltip("勾选后启用名字框自定义字号，仅对当前这句话生效")]
        public bool useCustomNameFontSize = false;
        
        [Tooltip("名字框自定义字号大小（仅在勾选启用名字框自定义字号时生效，如果配置了语言特定字号则优先使用）")]
        public float customNameFontSize = 38f;
        
        [Tooltip("名字框按语言配置特定字号（可选，如果配置了则优先使用对应语言的字号）")]
        public List<LanguageFontSize> languageNameFontSizes = new List<LanguageFontSize>();

        protected int executionCount;
        protected float savedOriginalFontSize = 0f; // 保存的原始字号
        protected bool hasFontSizeApplied = false; // 是否已应用自定义字号
        protected float savedOriginalNameFontSize = 0f; // 保存的名字框原始字号
        protected bool hasNameFontSizeApplied = false; // 是否已应用名字框自定义字号

        #region Public members

        /// <summary>
        /// Character that is speaking.
        /// </summary>
        public virtual Character _Character { get { return character; } }

        /// <summary>
        /// Portrait that represents speaking character.
        /// </summary>
        public virtual Sprite Portrait { get { return portrait; } set { portrait = value; } }

        /// <summary>
        /// Type this text in the previous dialog box.
        /// </summary>
        public virtual bool ExtendPrevious { get { return extendPrevious; } }

        public override void OnEnter()
        {
            if (!showAlways && executionCount >= showCount)
            {
                Continue();
                return;
            }

            executionCount++;

            // Override the active say dialog if needed
            if (character != null && character.SetSayDialog != null)
            {
                SayDialog.ActiveSayDialog = character.SetSayDialog;
            }

            if (setSayDialog != null)
            {
                SayDialog.ActiveSayDialog = setSayDialog;
            }

            var sayDialog = SayDialog.GetSayDialog();
            if (sayDialog == null)
            {
                Continue();
                return;
            }
    
            var flowchart = GetFlowchart();

            sayDialog.SetActive(true);

            // 在应用新字号之前，先恢复之前可能存在的自定义字号
            // 这样可以确保每个 Say 指令的字号是独立的
            RestoreAnyPreviousFontSize(sayDialog);

            sayDialog.SetCharacter(character);
            sayDialog.SetCharacterImage(portrait);

            string displayText = storyText;

            var activeCustomTags = CustomTag.activeCustomTags;
            for (int i = 0; i < activeCustomTags.Count; i++)
            {
                var ct = activeCustomTags[i];
                displayText = displayText.Replace(ct.TagStartSymbol, ct.ReplaceTagStartWith);
                if (ct.TagEndSymbol != "" && ct.ReplaceTagEndWith != "")
                {
                    displayText = displayText.Replace(ct.TagEndSymbol, ct.ReplaceTagEndWith);
                }
            }

            string subbedText = flowchart.SubstituteVariables(displayText);

            // 保存原始字号并应用自定义字号（如果启用）
            hasFontSizeApplied = false;
            savedOriginalFontSize = 0f;
            if (useCustomFontSize)
            {
                // 获取当前语言对应的字号，如果没有配置则使用默认字号
                float fontSizeToUse = GetFontSizeForCurrentLanguage(customFontSize, languageFontSizes);
                hasFontSizeApplied = ApplyCustomFontSize(sayDialog, fontSizeToUse, out savedOriginalFontSize);
                
                // 保存当前状态，供下一个 Say 指令恢复使用
                lastSayWithFontSize = this;
                lastSavedFontSize = savedOriginalFontSize;
                lastHadFontSize = true;
            }
            else
            {
                lastHadFontSize = false;
            }
            
            // 保存名字框原始字号并应用自定义字号（如果启用）
            hasNameFontSizeApplied = false;
            savedOriginalNameFontSize = 0f;
            if (useCustomNameFontSize)
            {
                // 获取当前语言对应的字号，如果没有配置则使用默认字号
                float fontSizeToUse = GetFontSizeForCurrentLanguage(customNameFontSize, languageNameFontSizes);
                hasNameFontSizeApplied = ApplyCustomNameFontSize(sayDialog, fontSizeToUse, out savedOriginalNameFontSize);
                
                // 保存当前状态，供下一个 Say 指令恢复使用
                lastSayWithFontSize = this;
                lastSavedNameFontSize = savedOriginalNameFontSize;
                lastHadNameFontSize = true;
            }
            else
            {
                lastHadNameFontSize = false;
            }

            var narrativeLog = FungusManager.Instance?.NarrativeLog;
            if (narrativeLog != null && !string.IsNullOrEmpty(subbedText))
            {
                if (extendPrevious && narrativeLog.Entries.Count > 0)
                {
                    narrativeLog.AppendToLastLine(subbedText);
                }
                else
                {
                    narrativeLog.AddLine(new NarrativeLogEntry
                    {
                        name = sayDialog.NameText,
                        text = subbedText
                    });
                }
            }

            sayDialog.Say(subbedText, !extendPrevious, waitForClick, fadeWhenDone, stopVoiceover, waitForVO, voiceOverClip, delegate {
                // 不在回调中恢复字号，让字号保持到下一条 Say 指令
                // 字号会在下一个 Say 指令的 OnEnter 中恢复（如果下一个 Say 没有自定义字号）
                // 或者在 OnStopExecuting 中恢复（如果流程被中断）
                Continue();
            });
        }

        public override string GetSummary()
        {
            string namePrefix = "";
            if (character != null) 
            {
                namePrefix = character.NameText + ": ";
            }
            if (extendPrevious)
            {
                namePrefix = "EXTEND" + ": ";
            }
            return namePrefix + "\"" + storyText + "\"";
        }

        public override Color GetButtonColor()
        {
            return new Color32(184, 210, 235, 255);
        }

        public override void OnReset()
        {
            executionCount = 0;
        }

        public override void OnStopExecuting()
        {
            var sayDialog = SayDialog.GetSayDialog();
            if (sayDialog == null)
            {
                return;
            }

            // 如果应用了自定义字号，在停止时恢复
            if (hasFontSizeApplied && savedOriginalFontSize > 0f)
            {
                RestoreOriginalFontSize(sayDialog, savedOriginalFontSize);
                hasFontSizeApplied = false;
            }
            
            // 如果应用了名字框自定义字号，在停止时恢复
            if (hasNameFontSizeApplied && savedOriginalNameFontSize > 0f)
            {
                RestoreOriginalNameFontSize(sayDialog, savedOriginalNameFontSize);
                hasNameFontSizeApplied = false;
            }

            sayDialog.Stop();
        }

        // 静态变量用于跟踪上一个 Say 指令的字号状态
        private static Say lastSayWithFontSize = null;
        private static float lastSavedFontSize = 0f;
        private static float lastSavedNameFontSize = 0f;
        private static bool lastHadFontSize = false;
        private static bool lastHadNameFontSize = false;
        
        /// <summary>
        /// 恢复之前可能存在的自定义字号
        /// </summary>
        protected virtual void RestoreAnyPreviousFontSize(SayDialog sayDialog)
        {
            if (sayDialog == null) return;
            
            // 如果上一个 Say 指令应用了自定义字号，现在恢复它
            if (lastSayWithFontSize != null && lastSayWithFontSize != this)
            {
                if (lastHadFontSize && lastSavedFontSize > 0f)
                {
                    RestoreOriginalFontSize(sayDialog, lastSavedFontSize);
                }
                
                if (lastHadNameFontSize && lastSavedNameFontSize > 0f)
                {
                    RestoreOriginalNameFontSize(sayDialog, lastSavedNameFontSize);
                }
                
                // 清除上一个指令的引用
                lastSayWithFontSize = null;
            }
        }
        
        /// <summary>
        /// 根据当前语言获取对应的字号
        /// </summary>
        protected virtual float GetFontSizeForCurrentLanguage(float defaultFontSize, List<LanguageFontSize> languageFontSizes)
        {
            if (languageFontSizes == null || languageFontSizes.Count == 0)
            {
                return defaultFontSize;
            }
            
            // 获取当前语言（通过反射访问 I2 LocalizationManager，避免直接依赖）
            string currentLanguage = "";
            try
            {
                // 尝试从所有已加载的程序集中查找 LocalizationManager
                System.Reflection.Assembly[] assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
                Type localizationManagerType = null;
                
                foreach (var assembly in assemblies)
                {
                    localizationManagerType = assembly.GetType("I2.Loc.LocalizationManager");
                    if (localizationManagerType != null)
                        break;
                }
                
                if (localizationManagerType != null)
                {
                    var currentLanguageProperty = localizationManagerType.GetProperty("CurrentLanguage");
                    if (currentLanguageProperty != null)
                    {
                        var value = currentLanguageProperty.GetValue(null);
                        if (value != null)
                        {
                            currentLanguage = value.ToString();
                        }
                    }
                }
            }
            catch
            {
                // 如果获取失败，使用默认字号
                return defaultFontSize;
            }
            
            if (string.IsNullOrEmpty(currentLanguage))
            {
                return defaultFontSize;
            }
            
            // 查找匹配的语言配置
            foreach (var langFontSize in languageFontSizes)
            {
                if (langFontSize != null && !string.IsNullOrEmpty(langFontSize.language))
                {
                    // 支持大小写不敏感匹配
                    if (string.Equals(langFontSize.language, currentLanguage, StringComparison.OrdinalIgnoreCase))
                    {
                        return langFontSize.fontSize;
                    }
                }
            }
            
            // 没有找到匹配的语言，返回默认字号
            return defaultFontSize;
        }
        
        /// <summary>
        /// 应用自定义字号到 SayDialog 的文本组件
        /// </summary>
        protected virtual bool ApplyCustomFontSize(SayDialog sayDialog, float fontSize, out float originalFontSize)
        {
            originalFontSize = 0f;
            
            if (sayDialog == null) return false;

            // 尝试从 SayDialog 获取 TextMeshProUGUI 组件
            TMPro.TextMeshProUGUI tmpText = null;
            
            // 方法1: 通过反射获取 storyText 字段
            var storyTextField = typeof(SayDialog).GetField("storyText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (storyTextField != null)
            {
                var storyTextValue = storyTextField.GetValue(sayDialog);
                if (storyTextValue is TMPro.TextMeshProUGUI)
                {
                    tmpText = storyTextValue as TMPro.TextMeshProUGUI;
                }
            }
            
            // 方法2: 通过 storyTextGO 获取
            if (tmpText == null)
            {
                var storyTextGOField = typeof(SayDialog).GetField("storyTextGO", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (storyTextGOField != null)
                {
                    var storyTextGO = storyTextGOField.GetValue(sayDialog) as GameObject;
                    if (storyTextGO != null)
                    {
                        tmpText = storyTextGO.GetComponent<TMPro.TextMeshProUGUI>();
                    }
                }
            }
            
            // 方法3: 直接在 SayDialog GameObject 及其子对象中查找
            if (tmpText == null && sayDialog.gameObject != null)
            {
                tmpText = sayDialog.gameObject.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            }
            
            if (tmpText != null)
            {
                originalFontSize = tmpText.fontSize;
                tmpText.fontSize = fontSize;
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// 恢复原始字号
        /// </summary>
        protected virtual void RestoreOriginalFontSize(SayDialog sayDialog, float originalFontSize)
        {
            if (sayDialog == null || originalFontSize <= 0f) return;

            // 尝试从 SayDialog 获取 TextMeshProUGUI 组件
            TMPro.TextMeshProUGUI tmpText = null;
            
            // 方法1: 通过反射获取 storyText 字段
            var storyTextField = typeof(SayDialog).GetField("storyText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (storyTextField != null)
            {
                var storyTextValue = storyTextField.GetValue(sayDialog);
                if (storyTextValue is TMPro.TextMeshProUGUI)
                {
                    tmpText = storyTextValue as TMPro.TextMeshProUGUI;
                }
            }
            
            // 方法2: 通过 storyTextGO 获取
            if (tmpText == null)
            {
                var storyTextGOField = typeof(SayDialog).GetField("storyTextGO", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (storyTextGOField != null)
                {
                    var storyTextGO = storyTextGOField.GetValue(sayDialog) as GameObject;
                    if (storyTextGO != null)
                    {
                        tmpText = storyTextGO.GetComponent<TMPro.TextMeshProUGUI>();
                    }
                }
            }
            
            // 方法3: 直接在 SayDialog GameObject 及其子对象中查找
            if (tmpText == null && sayDialog.gameObject != null)
            {
                tmpText = sayDialog.gameObject.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            }
            
            if (tmpText != null)
            {
                tmpText.fontSize = originalFontSize;
            }
        }
        
        /// <summary>
        /// 应用自定义字号到 SayDialog 的名字框文本组件
        /// </summary>
        protected virtual bool ApplyCustomNameFontSize(SayDialog sayDialog, float fontSize, out float originalFontSize)
        {
            originalFontSize = 0f;
            
            if (sayDialog == null) return false;

            // 尝试从 SayDialog 获取名字框的 TextMeshProUGUI 组件
            TMPro.TextMeshProUGUI tmpText = null;
            
            // 方法1: 通过反射获取 nameText 字段
            var nameTextField = typeof(SayDialog).GetField("nameText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (nameTextField != null)
            {
                var nameTextValue = nameTextField.GetValue(sayDialog);
                if (nameTextValue is TMPro.TextMeshProUGUI)
                {
                    tmpText = nameTextValue as TMPro.TextMeshProUGUI;
                }
            }
            
            // 方法2: 通过 nameTextGO 获取
            if (tmpText == null)
            {
                var nameTextGOField = typeof(SayDialog).GetField("nameTextGO", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (nameTextGOField != null)
                {
                    var nameTextGO = nameTextGOField.GetValue(sayDialog) as GameObject;
                    if (nameTextGO != null)
                    {
                        tmpText = nameTextGO.GetComponent<TMPro.TextMeshProUGUI>();
                    }
                }
            }
            
            if (tmpText != null)
            {
                originalFontSize = tmpText.fontSize;
                tmpText.fontSize = fontSize;
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// 恢复名字框原始字号
        /// </summary>
        protected virtual void RestoreOriginalNameFontSize(SayDialog sayDialog, float originalFontSize)
        {
            if (sayDialog == null || originalFontSize <= 0f) return;

            // 尝试从 SayDialog 获取名字框的 TextMeshProUGUI 组件
            TMPro.TextMeshProUGUI tmpText = null;
            
            // 方法1: 通过反射获取 nameText 字段
            var nameTextField = typeof(SayDialog).GetField("nameText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (nameTextField != null)
            {
                var nameTextValue = nameTextField.GetValue(sayDialog);
                if (nameTextValue is TMPro.TextMeshProUGUI)
                {
                    tmpText = nameTextValue as TMPro.TextMeshProUGUI;
                }
            }
            
            // 方法2: 通过 nameTextGO 获取
            if (tmpText == null)
            {
                var nameTextGOField = typeof(SayDialog).GetField("nameTextGO", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (nameTextGOField != null)
                {
                    var nameTextGO = nameTextGOField.GetValue(sayDialog) as GameObject;
                    if (nameTextGO != null)
                    {
                        tmpText = nameTextGO.GetComponent<TMPro.TextMeshProUGUI>();
                    }
                }
            }
            
            if (tmpText != null)
            {
                tmpText.fontSize = originalFontSize;
            }
        }

        #endregion

        #region ILocalizable implementation

        public virtual string GetStandardText()
        {
            return storyText;
        }

        public virtual void SetStandardText(string standardText)
        {
            storyText = standardText;
        }

        public virtual string GetDescription()
        {
            return description;
        }
        
        public virtual string GetStringId()
        {
            // String id for Say commands is SAY.<Localization Id>.<Command id>.[Character Name]
            string stringId = "SAY." + GetFlowchartLocalizationId() + "." + itemId + ".";
            if (character != null)
            {
                stringId += character.NameText;
            }

            return stringId;
        }

        #endregion
    }
}