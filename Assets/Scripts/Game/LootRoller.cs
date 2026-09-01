using System;
using System.Collections.Generic;

namespace UniverIdle.Game
{
  public static class LootRoller
  {
    public static List<LootResult> Roll(IReadOnlyList<LootEntry> table, Random rng)
    {
      var results = new List<LootResult>();
      if (table == null || rng == null) return results;

      foreach (var entry in table)
      {
        if (rng.NextDouble() >= entry.Chance) continue;

        var amount = entry.MinAmount == entry.MaxAmount
          ? entry.MinAmount
          : rng.Next(entry.MinAmount, entry.MaxAmount + 1);

        if (amount > 0)
          results.Add(new LootResult(entry.ItemId, amount));
      }

      return results;
    }
  }

  public readonly struct LootResult
  {
    public string ItemId { get; }
    public int Amount { get; }

    public LootResult(string itemId, int amount)
    {
      ItemId = itemId;
      Amount = amount;
    }
  }
}
