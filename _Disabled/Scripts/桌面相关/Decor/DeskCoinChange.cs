using System.Collections.Generic;

namespace DesktopPet.Decor
{
    /// <summary>桌上掉落面额：金 50 / 银 10 / 铜 1。钱包仍是同一笔总价值。</summary>
    public static class DeskCoinChange
    {
        public const int GoldFace = 50;
        public const int SilverFace = 10;
        public const int CopperFace = 1;

        /// <summary>贪心拆分，例如 78 → 50 + 10 + 10 + 1×8。</summary>
        public static void Split(int amount, List<int> faces)
        {
            if (faces == null)
                return;
            faces.Clear();
            if (amount <= 0)
                return;

            int goldCount = amount / GoldFace;
            amount %= GoldFace;
            for (int i = 0; i < goldCount; i++)
                faces.Add(GoldFace);

            int silverCount = amount / SilverFace;
            amount %= SilverFace;
            for (int i = 0; i < silverCount; i++)
                faces.Add(SilverFace);

            for (int i = 0; i < amount; i++)
                faces.Add(CopperFace);
        }
    }
}
