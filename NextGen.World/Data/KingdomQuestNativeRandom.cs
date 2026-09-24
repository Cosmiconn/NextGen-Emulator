using System;

namespace NextGen.World.Data
{
    /// <summary>
    /// Exact integer path used by the original RandomBox::rb_1000:
    /// MSVCRT rand() seeds 16 WELL512 words; WELL output is normalized by
    /// 2^-32, multiplied by 1e11, truncated, then reduced modulo 1000.
    ///
    /// The caller supplies the time32 seed. RandomBox's original constructor
    /// seeds CRT rand from _time32 at process initialization.
    /// </summary>
    public sealed class KingdomQuestNativeRandom
    {
        private readonly uint[] state = new uint[16];
        private int stateIndex;

        public KingdomQuestNativeRandom(int time32Seed)
        {
            uint crtState = unchecked((uint)time32Seed);
            for (int i = 0; i < state.Length; i++)
            {
                crtState = unchecked(crtState * 0x343fdu + 0x269ec3u);
                state[i] = (crtState >> 16) & 0x7fffu;
            }
            stateIndex = 0;
        }

        public ushort Next1000()
        {
            uint raw = NextUInt32();
            double unit = raw * 2.3283064365386963e-10;
            ulong scaled = (ulong)(unit * 100000000000.0);
            return (ushort)(scaled % 1000UL);
        }

        private uint NextUInt32()
        {
            int index = stateIndex;
            uint z0 = state[(index - 1) & 15];
            uint v0 = state[index];
            uint vm1 = state[(index - 3) & 15];

            uint z1 = unchecked(
                v0 ^ (v0 << 16) ^ vm1 ^ (vm1 << 15));
            uint vm2 = state[(index - 7) & 15];
            uint z2 = vm2 ^ (vm2 >> 11);
            uint newV1 = z1 ^ z2;
            state[index] = newV1;

            uint result = unchecked(
                ((z2 << 10) ^ z1) << 13);
            result ^= newV1 & 0xfed22169u;
            result = unchecked(result << 3);
            result ^= z0;
            result = unchecked(result << 2);
            result ^= z0;
            result ^= newV1;
            result ^= z1;

            stateIndex = (index - 1) & 15;
            state[stateIndex] = result;
            return result;
        }
    }
}
