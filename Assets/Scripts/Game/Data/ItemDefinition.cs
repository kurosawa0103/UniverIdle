using UnityEngine;

namespace UniverIdle.Game
{
  public sealed class ItemDefinition
  {
    public string Id { get; }
    public string DisplayName { get; }
    public Color DisplayColor { get; }
    public string Description { get; }

    public ItemDefinition(string id, string displayName, Color displayColor, string description)
    {
      Id = id;
      DisplayName = displayName;
      DisplayColor = displayColor;
      Description = description;
    }
  }
}
