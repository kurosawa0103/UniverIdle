namespace UniverIdle.Game
{
  /// <summary>背包分页与格子解锁；金币为系统金币。</summary>
  public sealed class InventoryBagDefinition
  {
    public int SlotsPerPage { get; }
    public int PageCount { get; }
    public int FreeSlotCount { get; }
    public int SlotUnlockGoldBase { get; }
    public int SlotUnlockGoldPer { get; }

    private readonly int[] _pageUnlockGold;

    public int MaxSlots => SlotsPerPage * PageCount;

    public InventoryBagDefinition(
      int slotsPerPage,
      int pageCount,
      int freeSlotCount,
      int[] pageUnlockGold,
      int slotUnlockGoldBase,
      int slotUnlockGoldPer)
    {
      SlotsPerPage = slotsPerPage < 1 ? 20 : slotsPerPage;
      PageCount = pageCount < 1 ? 1 : pageCount;
      var free = freeSlotCount < 1 ? 1 : freeSlotCount;
      if (free > SlotsPerPage) free = SlotsPerPage;
      FreeSlotCount = free;
      SlotUnlockGoldBase = slotUnlockGoldBase < 0 ? 0 : slotUnlockGoldBase;
      SlotUnlockGoldPer = slotUnlockGoldPer < 0 ? 0 : slotUnlockGoldPer;
      _pageUnlockGold = pageUnlockGold;
    }

    public static InventoryBagDefinition CreateDefault() =>
      new InventoryBagDefinition(20, 4, 10, new[] { 0, 50, 150, 400 }, 15, 8);

    public int PageUnlockCost(int pageIndex)
    {
      if (pageIndex <= 0) return 0;
      if (_pageUnlockGold != null && pageIndex < _pageUnlockGold.Length)
        return _pageUnlockGold[pageIndex] < 0 ? 0 : _pageUnlockGold[pageIndex];
      return 50 * pageIndex * pageIndex;
    }

    public int SlotUnlockCost(int slotIndex)
    {
      if (slotIndex < FreeSlotCount) return 0;
      return SlotUnlockGoldBase + (slotIndex - FreeSlotCount) * SlotUnlockGoldPer;
    }

    public int SlotCapForPages(int unlockedPageCount)
    {
      if (unlockedPageCount < 1) unlockedPageCount = 1;
      if (unlockedPageCount > PageCount) unlockedPageCount = PageCount;
      return unlockedPageCount * SlotsPerPage;
    }
  }
}
