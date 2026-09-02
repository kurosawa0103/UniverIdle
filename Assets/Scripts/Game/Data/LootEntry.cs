namespace UniverIdle.Game
{
  public readonly struct LootEntry
  {
    public string ItemId { get; }
    public float Chance { get; }
    /// <summary>相对权重；与表内其他行一起参与「随机 1 种」。</summary>
    public int MinAmount { get; }
    public int MaxAmount { get; }

    public bool IsEmpty => LootRules.IsEmpty(ItemId);

    public LootEntry(string itemId, float chance, int minAmount = 1, int maxAmount = 1)
    {
      ItemId = itemId;
      Chance = chance;
      MinAmount = minAmount;
      MaxAmount = maxAmount;
    }
  }
}
