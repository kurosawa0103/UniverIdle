using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "HierarchyIconConfig", menuName = "Config/Hierarchy Icon Config")]
public class HierarchyIconConfig : ScriptableObject
{
    [Serializable]
    public class IconEntry
    {
        public string scriptName;  // 脚本名（不带 .cs）
        public Texture2D icon;
    }

    public List<IconEntry> entries = new List<IconEntry>();
}
