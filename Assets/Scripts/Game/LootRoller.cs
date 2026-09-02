using System;
using System.Collections.Generic;

namespace UniverIdle.Game
{
  public static class LootRoller
  {
    /// <summary>按权重随机 1 种道具；chance 列为相对权重（不必加和为 1）。</summary>
    public static List<LootResult> Roll(IReadOnlyList<LootEntry> table, Random rng)
    {
      var results = new List<LootResult>();
      if (table == null || table.Count == 0 || rng == null) return results;

      var totalWeight = 0f;
      for (var i = 0; i < table.Count; i++)
      {
        var weight = table[i].Chance;
        if (weight > 0f) totalWeight += weight;
      }

      if (totalWeight <= 0f) return results;

      var roll = (float)(rng.NextDouble() * totalWeight);
      var cumulative = 0f;
      for (var i = 0; i < table.Count; i++)
      {
        var entry = table[i];
        var weight = entry.Chance;
        if (weight <= 0f) continue;

        cumulative += weight;
        if (roll >= cumulative) continue;

        if (LootRules.IsEmpty(entry.ItemId))
        {
          results.Add(new LootResult(entry.ItemId, 0));
          break;
        }

        var amount = entry.MinAmount == entry.MaxAmount
          ? entry.MinAmount
          : rng.Next(entry.MinAmount, entry.MaxAmount + 1);

        if (amount > 0)
          results.Add(new LootResult(entry.ItemId, amount));
        break;
      }

      return results;
    }

    /// <summary>按概率独立掷骰系统金币；命中后在 min～max 间均匀随机。</summary>
    public static int RollGold(float chance, int min, int max, Random rng)
    {
      if (rng == null || chance <= 0f || max <= 0) return 0;
      if (rng.NextDouble() >= chance) return 0;

      if (min <= 0) min = 1;
      if (max < min) max = min;
      return min == max ? min : rng.Next(min, max + 1);
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
