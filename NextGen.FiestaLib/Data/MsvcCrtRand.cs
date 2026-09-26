using System;

namespace NextGen.FiestaLib.Data
{
    /// <summary>
    /// Exact state transition used by the MSVC CRT rand()/srand() implementation
    /// statically linked into the supplied NA2016 Zone.exe.
    ///
    /// Native anchors recovered from Zone.exe:
    ///   srand  0x006594A0
    ///   rand   0x006594B2
    ///   CRT thread-data resolver 0x006637A8
    ///
    /// Both functions obtain the current CRT thread-data block through
    /// 0x006637A8 and read/write the rand state DWORD at offset +0x14.
    /// The state is therefore native-thread-local, not process-global.
    ///
    /// rand applies state = state * 0x343FD + 0x269EC3 modulo 2^32, then
    /// returns (state >> 16) & 0x7FFF. srand writes the supplied seed directly
    /// to that same thread-local state slot.
    ///
    /// This type owns only an explicitly supplied thread-local state. It
    /// deliberately does not choose a seed, call wall-clock time, choose a
    /// native execution thread, or merge independent native thread streams.
    /// </summary>
    public sealed class MsvcCrtRand
    {
        public const uint NativeSrandAddress = 0x006594A0u;
        public const uint NativeRandAddress = 0x006594B2u;
        public const uint NativeThreadDataResolverAddress = 0x006637A8u;
        public const int NativeThreadStateOffset = 0x14;

        public uint State { get; private set; }

        public MsvcCrtRand(uint state)
        {
            Seed(state);
        }

        public void Seed(uint state)
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
