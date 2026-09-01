namespace UniverIdle.Game
{
  public readonly struct LootEntry
  {
    public string ItemId { get; }
    public float Chance { get; }
    public int MinAmount { get; }
    public int MaxAmount { get; }

    public LootEntry(string itemId, float chance, int minAmount = 1, int maxAmount = 1)
    {
      ItemId = itemId;
      Chance = chance;
      MinAmount = minAmount;
      MaxAmount = maxAmount;
    }
  }
}
