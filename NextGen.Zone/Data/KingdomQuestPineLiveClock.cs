using System;

namespace NextGen.Zone.Data
{
    /// <summary>
    /// Zone mainthread 0x005ACAFB..0x005ACB7A accumulates unsigned
    /// timeGetTime deltas in 64 bits, then publishes elapsed milliseconds *
    /// 10 / 1000 at 0x14D41A70. All films see one sample per worker iteration.
    /// </summary>
    public sealed class KingdomQuestPineLiveClock : IKingdomQuestPineNativeTickSource
    {
        private uint previousMilliseconds;
        private ulong elapsedMilliseconds;
        private uint currentTick;

        public KingdomQuestPineLiveClock(uint initialMilliseconds)
        {
            previousMilliseconds = initialMilliseconds;
        }

        public void Advance(uint milliseconds)
        {
            elapsedMilliseconds = unchecked(elapsedMilliseconds +
                (uint)(milliseconds - previousMilliseconds));
            previousMilliseconds = milliseconds;
            currentTick = unchecked((uint)(elapsedMilliseconds * 10UL / 1000UL));
        }

        public bool TryGetCurrentTick(out uint tick)
        {
            tick = currentTick;
            return true;
        }
    }
}
