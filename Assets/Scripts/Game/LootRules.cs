namespace UniverIdle.Game
{
  /// <summary>掉落表约定：itemId 为 <see cref="EmptyItemId"/> 表示本次未获得道具。</summary>
  public static class LootRules
  {
    public const string EmptyItemId = "_empty";

    public static bool IsEmpty(string itemId) =>
      string.Equals(itemId, EmptyItemId, System.StringComparison.Ordinal);
  }
}
