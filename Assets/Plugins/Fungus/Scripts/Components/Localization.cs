// This code is part of the Fungus library (https://github.com/snozbot/fungus)
// It is released for free under the MIT open source license (https://github.com/snozbot/fungus/blob/master/LICENSE)

﻿using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;
using Ideafixxxer.CsvParser;

namespace Fungus
{
    /// <summary>
    /// Multi-language localization support.
    /// </summary>
    public class Localization : MonoBehaviour, ISubstitutionHandler
    {
        /// <summary>
        /// Temp storage for a single item of standard text and its localizations.
        /// </summary>
        protected class TextItem
        {
            public string description = "";
            public string standardText = "";
            public Dictionary<string, string> localizedStrings = new Dictionary<string, string>();
        }

        [Tooltip("Language to use at startup, usually defined by a two letter language code (e.g DE = German)")]
        [SerializeField] protected string activeLanguage = "";

        [Tooltip("CSV file containing localization data which can be easily edited in a spreadsheet tool")]
        [SerializeField] protected TextAsset localizationFile;

        protected Dictionary<string, ILocalizable> localizeableObjects = new Dictionary<string, ILocalizable>();

        protected string notificationText = "";

        protected bool initialized;

        protected static Dictionary<string, string> localizedStrings = new Dictionary<string, string>();

        #if UNITY_5_4_OR_NEWER
        #else
        public virtual void OnLevelWasLoaded(int level) 
        {
            LevelWasLoaded();
        }
        #endif

        protected virtual void LevelWasLoaded()
        {
            // Check if a language has been selected using the Set Language command in a previous scene.
            if (SetLanguage.mostRecentLanguage != "")
            {
                // This language will be used when Start() is called
                activeLanguage = SetLanguage.mostRecentLanguage;
            }
        }

        private void SceneManager_activeSceneChanged(UnityEngine.SceneManagement.Scene arg0, UnityEngine.SceneManagement.Scene arg1)
        {
            LevelWasLoaded();
        }

        protected virtual void OnEnable()
        {
            StringSubstituter.RegisterHandler(this);
            #if UNITY_5_4_OR_NEWER
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;
            #endif
        }

        protected virtual void OnDisable()
        {
            StringSubstituter.UnregisterHandler(this);
            #if UNITY_5_4_OR_NEWER
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= SceneManager_activeSceneChanged;
            #endif
        }

        protected virtual void Start()
        {
            Init();
        }

        /// <summary>
        /// String subsitution can happen during the Start of another component, so we
        /// may need to call Init() from other methods.
        /// </summary>
        protected virtual void Init()
        {
            if (initialized)
            {
                return;
            }

            CacheLocalizeableObjects();

            if (localizationFile != null &&
                localizationFile.text.Length > 0)
            {
                SetActiveLanguage(activeLanguage);
            }

            initialized = true;
        }

        // Build a cache of all the localizeable objects in the scene
        protected virtual void CacheLocalizeableObjects()
        {
            UnityEngine.Object[] objects = Resources.FindObjectsOfTypeAll(typeof(Component));
            for (int i = 0; i < objects.Length; i++)
            {
                var o = objects[i];
                ILocalizable localizable = o as ILocalizable;
                if (localizable != null)
                {
                    localizeableObjects[localizable.GetStringId()] = localizable;
                }
            }
        }

        /// <summary>
        /// Builds a dictionary of localizable text items in the scene.
        /// </summary>
        protected Dictionary<string, TextItem> FindTextItems()
        {
            Dictionary<string, TextItem> textItems = new Dictionary<string, TextItem>();

            // Add localizable commands in same order as command list to make it
            // easier to localise / edit standard text.
            var flowcharts = GameObject.FindObjectsOfType<Flowchart>();
            for (int i = 0; i < flowcharts.Length; i++)
            {
                var flowchart = flowcharts[i];
                var blocks = flowchart.GetComponents<Block>();

                for (int j = 0; j < blocks.Length; j++)
                {
                    var block = blocks[j];
                    var commandList = block.CommandList;
                    for (int k = 0; k < commandList.Count; k++)
                    {
                        var command = commandList[k];
                        ILocalizable localizable = command as ILocalizable;
                        if (localizable != null)
                        {
                            TextItem textItem = new TextItem();
                            textItem.standardText = localizable.GetStandardText();
                            textItem.description = localizable.GetDescription();
                            textItems[localizable.GetStringId()] = textItem;
                        }
                    }
                }
            }

            // Add everything else that's localizable (including inactive objects)
            UnityEngine.Object[] objects = Resources.FindObjectsOfTypeAll(typeof(Component));
            for (int i = 0; i < objects.Length; i++)
            {
                var o = objects[i];
                ILocalizable localizable = o as ILocalizable;
                if (localizable != null)
                {
                    string stringId = localizable.GetStringId();
                    if (textItems.ContainsKey(stringId))
                    {
                        // Already added
                        continue;
                    }
                    TextItem textItem = new TextItem();
                    textItem.standardText = localizable.GetStandardText();
                    textItem.description = localizable.GetDescription();
                    textItems[stringId] = textItem;
                }
            }

            return textItems;
        }

        /// <summary>
        /// Adds localized strings from CSV file data to a dictionary of text items in the scene.
        /// </summary>
        protected virtual void AddCSVDataItems(Dictionary<string, TextItem> textItems, string csvData)
        {
            CsvParser csvParser = new CsvParser();
            string[][] csvTable = csvParser.Parse(csvData);

            if (csvTable.Length <= 1)
            {
                // No data rows in file
                return;
            }

            // Parse header row
            string[] columnNames = csvTable[0];
            
            for (int i = 1; i < csvTable.Length; ++i)
            {
                string[] fields = csvTable[i];
                if (fields.Length < 3)
                {
                    // No standard text or localized string fields present
                    continue;
                }
                
                string stringId = fields[0];

                if (!textItems.ContainsKey(stringId))
                {
                    if (stringId.StartsWith("CHARACTER.") || 
                        stringId.StartsWith("SAY.") || 
                        stringId.StartsWith("MENU.") ||
                        stringId.StartsWith("WRITE.") ||
                        stringId.StartsWith("SETTEXT."))
                    {
                        // If it's a 'built-in' type this probably means that item has been deleted from its flowchart,
                        // so there's no need to add a text item for it.
                        continue;
                    }

                    // Key not found. Assume it's a custom string that we want to retain, so add a text item for it.
                    TextItem newTextItem = new TextItem();
                    newTextItem.description = CSVSupport.Unescape(fields[1]);
                    newTextItem.standardText = CSVSupport.Unescape(fields[2]);
                    textItems[stringId] = newTextItem;
                }

                TextItem textItem = textItems[stringId];

                for (int j = 3; j < fields.Length; ++j)
                {
                    if (j >= columnNames.Length)
                    {
                        continue;
                    }
                    string languageCode = columnNames[j];
                    string languageEntry = CSVSupport.Unescape(fields[j]);
                    
                    if (languageEntry.Length > 0)
                    {
                        textItem.localizedStrings[languageCode] = languageEntry;
                    }
                }
            }
        }

        #region Public members

        /// <summary>
        /// Looks up the specified string in the localized strings table.
        /// For this to work, a localization file and active language must have been set previously.
        /// Return null if the string is not found.            
        /// </summary>
        public static string GetLocalizedString(string stringId)
        {
            if (localizedStrings == null)
            {
                return null;
            }

            if (localizedStrings.ContainsKey(stringId))
            {
                return localizedStrings[stringId];
            }

            return null;
        }

        /// <summary>
        /// Language to use at startup, usually defined by a two letter language code (e.g DE = German).
        /// </summary>
        public virtual string ActiveLanguage { get { return activeLanguage; } }

        /// <summary>
        /// CSV file containing localization data which can be easily edited in a spreadsheet tool.
        /// </summary>
        public virtual TextAsset LocalizationFile { get { return localizationFile; } set { localizationFile = value; } }

        /// <summary>
        /// Stores any notification message from export / import methods.
        /// </summary>
        public virtual string NotificationText { get { return notificationText; } set { notificationText = value; } }

        /// <summary>
        /// Clears the cache of localizeable objects.
        /// </summary>
        public virtual void ClearLocalizeableCache()
        {
            localizeableObjects.Clear();
        }

        /// <summary>
        /// Convert all text items and localized strings to an easy to edit CSV format.
        /// </summary>
        public virtual string GetCSVData()
        {
            // 收集场景中的文本项
            Dictionary<string, TextItem> textItems = FindTextItems();

            // 载入现有 CSV（如果存在），以便保留旧翻译
            string[][] existingTable = null;
            string[] existingHeader = null;
            if (localizationFile != null && localizationFile.text.Length > 0)
            {
                CsvParser csvParser = new CsvParser();
                existingTable = csvParser.Parse(localizationFile.text);
                if (existingTable.Length > 0)
                {
                    existingHeader = existingTable[0];
                }
            }

            // 构建导出表头
            List<string> exportHeader = new List<string>() { "Key", "Description", "Standard", "Chinese" };

            // 如果有旧表头，就追加它的其它语言列（保留顺序）
            if (existingHeader != null)
            {
                foreach (var col in existingHeader)
                {
                    if (col == "Key" || col == "Description" || col == "Standard" || col == "Chinese")
                        continue;
                    if (!exportHeader.Contains(col))
                        exportHeader.Add(col);
                }
            }

            // 从旧 CSV 中构建一个查找表
            Dictionary<string, string[]> oldRows = new Dictionary<string, string[]>();
            if (existingTable != null && existingTable.Length > 1)
            {
                for (int i = 1; i < existingTable.Length; i++)
                {
                    string[] row = existingTable[i];
                    if (row.Length > 0 && !oldRows.ContainsKey(row[0]))
                        oldRows[row[0]] = row;
                }
            }

            // 拼接 CSV 文本
            StringBuilder csv = new StringBuilder();
            csv.AppendLine(string.Join(",", exportHeader));

            int rowCount = 0;
            foreach (var kv in textItems)
            {
                string key = kv.Key;
                TextItem item = kv.Value;

                List<string> row = new List<string>();
                row.Add(CSVSupport.Escape(key));
                row.Add(CSVSupport.Escape(item.description));
                row.Add(CSVSupport.Escape(item.standardText));

                // ✅ Chinese 总是与 Standard 同步
                row.Add(CSVSupport.Escape(item.standardText));

                // ✅ 保留旧列内容
                for (int i = 4; i < exportHeader.Count; i++)
                {
                    string col = exportHeader[i];
                    string val = "";
                    if (oldRows.ContainsKey(key))
                    {
                        var oldRow = oldRows[key];
                        int colIndex = System.Array.IndexOf(existingHeader, col);
                        if (colIndex >= 0 && colIndex < oldRow.Length)
                            val = oldRow[colIndex];
                    }
                    row.Add(CSVSupport.Escape(val));
                }

                csv.AppendLine(string.Join(",", row));
                rowCount++;
            }

            notificationText = $"Exported {rowCount} items (Standard + Chinese updated, other languages preserved).";
            return csv.ToString();
        }


        /// <summary>
        /// Scan a localization CSV file and copies the strings for the specified language code
        /// into the text properties of the appropriate scene objects.
        /// </summary>
        public virtual void SetActiveLanguage(string languageCode, bool forceUpdateSceneText = false)
        {
            if (!Application.isPlaying)
            {
                // This function should only ever be called when the game is playing (not in editor).
                return;
            }

            if (localizationFile == null)
            {
                // No localization file set
                return;
            }

            localizedStrings.Clear();

            CsvParser csvParser = new CsvParser();
            string[][] csvTable = csvParser.Parse(localizationFile.text);

            if (csvTable.Length <= 1)
            {
                // No data rows in file
                return;
            }

            // Parse header row
            string[] columnNames = csvTable[0];

            if (columnNames.Length < 3)
            {
                // No languages defined in CSV file
                return;
            }

            // First assume standard text column and then look for a matching language column
            int languageIndex = 2;
            for (int i = 3; i < columnNames.Length; ++i)
            {
                if (columnNames[i] == languageCode)
                {
                    languageIndex = i;
                    break;
                }
            }

            if (languageIndex == 2)
            {
                // Using standard text column
                // Add all strings to the localized strings dict, but don't replace standard text in the scene.
                // This allows string substitution to work for both standard and localized text strings.
                for (int i = 1; i < csvTable.Length; ++i)
                {
                    string[] fields = csvTable[i];
                    if (fields.Length < 3)
                    {
                        continue;
                    }

                    localizedStrings[fields[0]] = fields[languageIndex];
                }

                // Early out unless we've been told to force the scene text to update.
                // This happens when the Set Language command is used to reset back to the standard language.
                if (!forceUpdateSceneText)
                {
                    return;
                }
            }

            // Using a localized language text column
            // 1. Add all localized text to the localized strings dict
            // 2. Update all scene text properties with localized versions
            for (int i = 1; i < csvTable.Length; ++i)
            {
                string[] fields = csvTable[i];

                if (fields.Length < languageIndex + 1)
                {
                    continue;
                }

                string stringId = fields[0];
                string languageEntry = CSVSupport.Unescape(fields[languageIndex]);

                if (languageEntry.Length > 0)
                {
                    localizedStrings[stringId] = languageEntry;
                    PopulateTextProperty(stringId, languageEntry);
                }
            }
        }

        /// <summary>
        /// Populates the text property of a single scene object with a new text value.
        /// </summary>
        public virtual bool PopulateTextProperty(string stringId, string newText)
        {
            // Ensure that all localizeable objects have been cached
            if (localizeableObjects.Count == 0)
            {
                CacheLocalizeableObjects();
            }

            ILocalizable localizable = null;
            localizeableObjects.TryGetValue(stringId, out localizable);
            if (localizable != null)
            {
                localizable.SetStandardText(newText);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns all standard text for localizeable text in the scene using an
        /// easy to edit custom text format.
        /// </summary>
        public virtual string GetStandardText()
        {
            // Collect all the text items present in the scene
            Dictionary<string, TextItem> textItems = FindTextItems();

            string textData = "";
            int rowCount = 0;
            var keys = textItems.Keys;
            foreach (var stringId in keys)
            {
                TextItem languageItem = textItems[stringId];

                textData += "#" + stringId + "\n";
                textData += languageItem.standardText.Trim() + "\n\n";
                rowCount++;
            }

            notificationText = "Exported " + rowCount + " standard text items.";

            return textData;
        }

        /// <summary>
        /// Sets standard text on scene objects by parsing a text data file.
        /// </summary>
        public virtual void SetStandardText(string textData)
        {
            var lines = textData.Split('\n');

            int updatedCount = 0;

            string stringId = "";
            string buffer = "";
            for (int i = 0; i < lines.Length; i++)
            {
                // Check for string id line 
                var line = lines[i];
                if (line.StartsWith("#"))
                {
                    if (stringId.Length > 0)
                    {
                        // Write buffered text to the appropriate text property
                        if (PopulateTextProperty(stringId, buffer.Trim()))
                        {
                            updatedCount++;
                        }
                    }
                    // Set the string id for the follow text lines
                    stringId = line.Substring(1, line.Length - 1);
                    buffer = "";
                }
                else
                {
                    buffer += line + "\n";
                }
            }

            // Handle last buffered entry
            if (stringId.Length > 0)
            {
                if (PopulateTextProperty(stringId, buffer.Trim()))
                {
                    updatedCount++;
                }
            }

            notificationText = "Updated " + updatedCount + " standard text items.";
        }

        #endregion

        #region StringSubstituter.ISubstitutionHandler imlpementation

        public virtual bool SubstituteStrings(StringBuilder input)
        {
            // This method could be called from the Start method of another component, so we
            // may need to initilize the localization system.
            Init();

            // Instantiate the regular expression object.
            Regex r = new Regex(Flowchart.SubstituteVariableRegexString);

            bool modified = false;

            // Match the regular expression pattern against a text string.
            var results = r.Matches(input.ToString());
            for (int i = 0; i < results.Count; i++)
            {
                Match match = results[i];
                string key = match.Value.Substring(2, match.Value.Length - 3);
                // Next look for matching localized string
                string localizedString = Localization.GetLocalizedString(key);
                if (localizedString != null)
                {
                    input.Replace(match.Value, localizedString);
                    modified = true;
                }
            }

            return modified;
        }

        #endregion
    }
}