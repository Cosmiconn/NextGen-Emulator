namespace NextGen.World.Data
{
    /// <summary>
    /// Exact mask builder from Zone.exe
    /// ShinePlayer::sp_GetItemWhoEquip_ClassGroup.
    ///
    /// The native virtual class getter feeds a switch that recognizes only the
    /// six family roots 1, 6, 11, 16, 21 and 26. Each root expands through its
    /// family's final class bit; every other input returns bit 0 (value 1).
    /// </summary>
    public static class KingdomQuestRewardClassGroup
    {
        public static uint FromNativeClass(byte nativeClass)
        {
            byte lastClass;
            switch (nativeClass)
            {
                case 1:
                    lastClass = 5;
                    break;
                case 6:
                    lastClass = 10;
                    break;
                case 11:
                    lastClass = 15;
                    break;
                case 16:
                    lastClass = 20;
                    break;
                case 21:
                    lastClass = 25;
                    break;
                case 26:
                    lastClass = 27;
                    break;
                default:
                    return 1;
            }

            uint mask = 0;
            for (int current = nativeClass; current <= lastClass; current++)
                mask += 1u << current;
            return mask;
        }
    }
}
