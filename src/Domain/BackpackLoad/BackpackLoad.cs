using System;

namespace LethalDungeon.Domain
{
    public static class BackpackLoad
    {
        public static decimal SpeedMultiplier(int occupiedSlots)
        {
            if (occupiedSlots < 0 || occupiedSlots > 10)
                throw new ArgumentOutOfRangeException(nameof(occupiedSlots));

            return 1.2m - 0.03m * occupiedSlots;
        }
    }
}
