using System;

namespace NextGen.FiestaLib.Data
{
    /// <summary>
    /// Exact state transition used by the MSVC CRT rand()/srand() implementation
    /// statically linked into the supplied NA2016 Zone.exe.
    ///
    /// Zone.exe rand() at 0x006594B2 obtains the per-process CRT state, applies
    /// state = state * 0x343FD + 0x269EC3 modulo 2^32, then returns
    /// (state >> 16) & 0x7FFF.
    ///
    /// This type owns only an explicitly supplied state. It deliberately does
    /// not choose a seed, call wall-clock time, or pretend to own Zone's shared
    /// CRT stream.
    /// </summary>
    public sealed class MsvcCrtRand
    {
        public uint State { get; private set; }

        public MsvcCrtRand(uint state)
        {
            State = state;
        }

        public ushort Next()
        {
            unchecked
            {
                State = State * 0x343fdu + 0x269ec3u;
            }

            return (ushort)((State >> 16) & 0x7fffu);
        }

        public void Consume(int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException("count");

            for (int i = 0; i < count; i++)
                Next();
        }
    }
}
