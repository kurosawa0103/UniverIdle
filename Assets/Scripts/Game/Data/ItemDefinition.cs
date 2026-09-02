using System;
using UnityEngine;

namespace UniverIdle.Game
{
  public sealed class ItemDefinition
  {
    public string Id { get; }
    public string DisplayName { get; }
    public Color DisplayColor { get; }
    /// <summary>表内原始 icon 列；空表示按 item_{id} 约定。</summary>
    public string Icon { get; }
    public string Description { get; }

    /// <summary>Resources 加载路径（无扩展名）。null 表示不尝试贴图。</summary>
    public string IconResourcePath { get; }

    public ItemDefinition(string id, string displayName, Color displayColor, string icon, string description)
    {
      Id = id;
      DisplayName = displayName;
      DisplayColor = displayColor;
      Icon = icon ?? string.Empty;
      Description = description;
      IconResourcePath = ResolveIconResourcePath(id, Icon);
    }

    internal static string ResolveIconResourcePath(string id, string iconFromTable)
    {
      if (string.IsNullOrWhiteSpace(iconFromTable))
        return $"{GameDataPaths.ItemIconResourcesPrefix}/item_{id}";

      var value = iconFromTable.Trim();
      if (value == "-" || value.Equals("none", StringComparison.OrdinalIgnoreCase))
        return null;

      return value.Contains("/")
        ? value
        : $"{GameDataPaths.ItemIconResourcesPrefix}/{value}";
    }
  }
}
