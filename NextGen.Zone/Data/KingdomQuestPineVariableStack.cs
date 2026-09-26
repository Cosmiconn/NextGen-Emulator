using System;
using System.Collections.Generic;
using System.Text;

namespace NextGen.Zone.Data
{
    /// <summary>
    /// Native PineScriptToken storage boundary. Zone.exe stores identifier and
    /// value tokens in fixed 0x100-byte buffers inside VariableStack entries.
    ///
    /// This type intentionally preserves raw token text only. Numeric/string
    /// expression semantics belong to the recovered Pine expression evaluator,
    /// not to VariableStack.
    /// </summary>
    public sealed class KingdomQuestPineTokenValue
    {
        public const int NativeByteCapacity = 0x100;

        private readonly byte[] nativeBytes =
            new byte[NativeByteCapacity];

        public string Text
        {
            get
            {
                int length = Array.IndexOf(nativeBytes, (byte)0);
                if (length < 0)
                    length = nativeBytes.Length;
                return Encoding.ASCII.GetString(
                    nativeBytes, 0, length);
            }
        }

        public byte[] SnapshotNativeBytes()
        {
            return (byte[])nativeBytes.Clone();
        }

        public bool TrySetAscii(string value)
        {
            if (value == null)
                return false;

            byte[] source = Encoding.ASCII.GetBytes(value);
            if (source.Length >= NativeByteCapacity)
                return false;

            Array.Clear(nativeBytes, 0, nativeBytes.Length);
            Buffer.BlockCopy(
                source, 0, nativeBytes, 0, source.Length);
            return true;
        }

        public bool TryCopyFrom(KingdomQuestPineTokenValue source)
        {
            if (source == null)
                return false;

            Buffer.BlockCopy(
                source.nativeBytes, 0,
                nativeBytes, 0,
                NativeByteCapacity);
            return true;
        }

        internal static bool TryCreate(
            string value,
            out KingdomQuestPineTokenValue token)
        {
            token = new KingdomQuestPineTokenValue();
            if (!token.TrySetAscii(value))
            {
                token = null;
                return false;
            }

            return true;
        }
    }

    public sealed class KingdomQuestPineVariableStackEntry
    {
        public KingdomQuestPineTokenValue NameToken { get; private set; }
        public KingdomQuestPineTokenValue ValueToken { get; private set; }

        internal KingdomQuestPineVariableStackEntry(
            KingdomQuestPineTokenValue nameToken)
        {
            NameToken = nameToken;
            ValueToken = new KingdomQuestPineTokenValue();
        }
    }

    /// <summary>
    /// Executable projection of PineScriptStack::VariableStack.
    ///
    /// Zone.exe:
    ///   vs_FindVariable 0x004D69F0
    ///     - count is stored at +0x10000;
    ///     - entries have stride 0x200;
    ///     - search starts at count-1 and walks backwards;
    ///     - a hit returns entry +0x100 (the value PineScriptToken).
    ///
    ///   vs_Push 0x004D6A90
    ///     - accepts pushes only while count &lt; 0x7F;
    ///     - copies exactly 0x40 DWORDs = 0x100 bytes of the identifier token;
    ///     - increments count and returns the value token at entry +0x100.
    ///
    /// Duplicate identifiers are therefore legal and the newest pushed entry
    /// shadows older entries. This class does not calculate Pine expressions.
    /// </summary>
    public sealed class KingdomQuestPineVariableStack
    {
        public const int NativeTokenBytes = 0x100;
        public const int NativeEntryStrideBytes = 0x200;
        public const int NativeCountOffset = 0x10000;
        public const int NativeCapacity = 0x7F;

        private readonly List<KingdomQuestPineVariableStackEntry> entries =
            new List<KingdomQuestPineVariableStackEntry>(
                NativeCapacity);

        public int Count
        {
            get { return entries.Count; }
        }

        public IReadOnlyList<KingdomQuestPineVariableStackEntry> Entries
        {
            get { return entries.AsReadOnly(); }
        }

        public bool TryPush(
            string identifier,
            out KingdomQuestPineTokenValue valueToken)
        {
            valueToken = null;
            if (entries.Count >= NativeCapacity)
                return false;

            KingdomQuestPineTokenValue nameToken;
            if (!KingdomQuestPineTokenValue.TryCreate(
                    identifier, out nameToken))
                return false;

            var entry =
                new KingdomQuestPineVariableStackEntry(nameToken);
            entries.Add(entry);
            valueToken = entry.ValueToken;
            return true;
        }

        public bool TryFind(
            string identifier,
            out KingdomQuestPineTokenValue valueToken)
        {
            valueToken = null;
            if (identifier == null)
                return false;

            for (int i = entries.Count - 1; i >= 0; i--)
            {
                KingdomQuestPineVariableStackEntry entry =
                    entries[i];
                if (!string.Equals(
                        entry.NameToken.Text,
                        identifier,
                        StringComparison.Ordinal))
                    continue;

                valueToken = entry.ValueToken;
                return true;
            }

            return false;
        }

        public void Clear()
        {
            entries.Clear();
        }
    }
}
