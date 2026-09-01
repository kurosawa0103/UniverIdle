using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace DesktopPet.Luby
{
    [CreateAssetMenu(menuName = "桌宠/Luby/小剧场目录", fileName = "TheaterCatalog")]
    public sealed class LubyTheaterCatalog : ScriptableObject
    {
        [Title("小剧场目录", "Director 按权重扫描此列表")]
        [LabelText("事件列表")]
        [ListDrawerSettings(
            ShowFoldout = true,
            DraggableItems = true,
            ListElementLabelName = "eventId")]
        public List<LubyTheaterEventDefinition> events = new List<LubyTheaterEventDefinition>();
    }
}
