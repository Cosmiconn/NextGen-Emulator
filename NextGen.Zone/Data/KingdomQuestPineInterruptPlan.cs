using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NextGen.Zone.Data
{
    public enum KingdomQuestPineInterruptKind : byte
    {
        PlayerEliminate = 1,
        TimeOut = 2,
        PlayerDead = 3,
        SecondInterval = 4,
        HPLow = 5,
        DeadIndex = 6,
        PickUpItemIndex = 7,
        DeadHandle = 8,
        NPCClickHandle = 9,
        MobEliminate = 10,
    }

    public sealed class KingdomQuestPineInterruptSetPlan
    {
        public KingdomQuestPineInterruptKind Kind { get; private set; }
        public string NativeTypeToken { get; private set; }
        public string EraseName { get; private set; }
        public byte[] NativeEraseName16 { get; private set; }
        public int RepeatCount { get; private set; }
        public IReadOnlyList<string> Arguments { get; private set; }
        public string ActionBlock { get; private set; }
        public uint? IntervalDurationTicks { get; private set; }
        public uint? InitialIntervalDeadlineTick { get; private set; }

        internal KingdomQuestPineInterruptSetPlan(
            KingdomQuestPineInterruptKind kind,
            string nativeTypeToken,
            string eraseName,
            int repeatCount,
            IEnumerable<string> arguments,
            string actionBlock,
            uint? intervalDurationTicks,
            uint? initialIntervalDeadlineTick)
        {
            Kind = kind;
            NativeTypeToken = nativeTypeToken ?? string.Empty;
            EraseName = eraseName ?? string.Empty;
            NativeEraseName16 = BuildName16(EraseName);
            RepeatCount = repeatCount;
            Arguments = (arguments ?? new string[0]).ToList().AsReadOnly();
            ActionBlock = actionBlock ?? string.Empty;
            IntervalDurationTicks = intervalDurationTicks;
            InitialIntervalDeadlineTick = initialIntervalDeadlineTick;
        }

        private static byte[] BuildName16(string value)
        {
            byte[] source = Encoding.ASCII.GetBytes(value ?? string.Empty);
            byte[] target = new byte[16];
            Buffer.BlockCopy(
                source, 0, target, 0, Math.Min(source.Length, target.Length));
            return target;
        }
    }

    /// <summary>
    /// Source-exact command projection for the interrupt forms actually used
    /// by the nine supplied Pine KQs.
    ///
    /// ShineInterruptSet::sa_Step (0x004EC1E0):
    ///   - lower-cases the interrupt type token;
    ///   - evaluates a 16-byte interrupt name and stores it with strncpy(16);
    ///   - evaluates a repeat count;
    ///   - evaluates up to three type-specific Pine operands;
    ///   - dispatches to ScriptInterruptManager/Theater helpers.
    ///
    /// ScriptInterruptManager::ScriptInterruptManager (0x0050D270) creates a
    /// fixed List<ScriptInterruptArgument> with exactly 0x14 = 20 entries.
    ///
    /// Supplied-source type distribution (225 total):
    ///   TimeOut 55, PlayerEliminate 50, Sec 45, HPLow 21,
    ///   PlayerDead 15, DeadIndex 12, PickUpItemIndex 10,
    ///   DeadHandle 9, NPCClickHandle 4, MobEliminate 4.
    /// </summary>
    public static class KingdomQuestPineInterruptPlan
    {
        public const int NativeManagerCapacity = 20;
        public const int NativeEraseNameBytes = 16;
        public const int UsedInterruptSetCount = 225;
        public const int UsedInterruptEraseCount = 14;
        public const int UsedInterruptClearCount = 65;
        public const int UsedWaitInterruptCount = 55;

        public static bool TryParseUsedSet(
            string commandText,
            uint currentTick,
            out KingdomQuestPineInterruptSetPlan plan)
        {
            plan = null;
            List<Operand> values;
            if (!TryLex(commandText, out values) ||
                values.Count < 5 ||
                !EqualsToken(values[0].Text, "interruptset") ||
                !values[2].Quoted ||
                values[2].Text.Length > 10 ||
                !int.TryParse(values[3].Text, out int repeatCount) ||
                repeatCount <= 0)
                return false;

            KingdomQuestPineInterruptKind kind;
            int expectedTail;
            if (!TryUsedKind(
                    values[1].Text, out kind, out expectedTail) ||
                values.Count != 4 + expectedTail)
                return false;

            Operand action = values[values.Count - 1];
            if (!action.Quoted || action.Text.Length == 0)
                return false;

            var arguments = values
                .Skip(4)
                .Take(expectedTail - 1)
                .Select(v => v.Text)
                .ToList();

            uint? intervalTicks = null;
            uint? intervalDeadline = null;
            if (kind == KingdomQuestPineInterruptKind.SecondInterval)
            {
                if (arguments.Count != 1)
                    return false;

                int seconds;
                if (!TryEvaluateUsedConstantExpression(
                        arguments[0], out seconds))
                    return false;

                uint duration =
                    KingdomQuestPineNativeTime.ConvertToNativeTicks(
                        KingdomQuestPineTimeUnit.Second, seconds);
                intervalTicks = duration;
                intervalDeadline =
                    KingdomQuestPineNativeTime.AddDuration(
                        currentTick, duration);
            }

            plan = new KingdomQuestPineInterruptSetPlan(
                kind,
                values[1].Text,
                values[2].Text,
                repeatCount,
                arguments,
                action.Text,
                intervalTicks,
                intervalDeadline);
            return true;
        }

        public static bool TryParseErase(
            string commandText,
            out byte[] nativeName16)
        {
            nativeName16 = null;
            List<Operand> values;
            if (!TryLex(commandText, out values) ||
                values.Count != 2 ||
                !EqualsToken(values[0].Text, "interrupterase") ||
                !values[1].Quoted)
                return false;

            byte[] source = Encoding.ASCII.GetBytes(values[1].Text);
            nativeName16 = new byte[NativeEraseNameBytes];
            Buffer.BlockCopy(
                source, 0, nativeName16, 0,
                Math.Min(source.Length, nativeName16.Length));
            return true;
        }

        public static bool IsInterruptClear(string commandText)
        {
            List<Operand> values;
            return TryLex(commandText, out values) &&
                values.Count == 1 &&
                EqualsToken(values[0].Text, "interruptclear");
        }

        public static bool TryParseWaitInterrupt(
            string commandText,
            out string blockVariable,
            out string argumentVariable)
        {
            blockVariable = null;
            argumentVariable = null;

            List<Operand> values;
            if (!TryLex(commandText, out values) ||
                values.Count != 3 ||
                !EqualsToken(values[0].Text, "waitinterrupt") ||
                values[1].Quoted ||
                !values[2].Quoted ||
                values[1].Text.Length == 0 ||
                values[2].Text.Length == 0)
                return false;

            blockVariable = values[1].Text;
            argumentVariable = values[2].Text;
            return true;
        }

        /// <summary>
        /// ScriptInterruptInterval::sib_BlastCheck (0x0050AB90):
        /// no fire while nextDeadline > currentTick. On fire it advances the
        /// next deadline by the original interval (anchored schedule), copies
        /// the action block, decrements RepeatCount and removes the entry when
        /// the count reaches zero.
        /// </summary>
        public static bool IsIntervalDue(
            uint nextDeadlineTick, uint currentTick)
        {
            return nextDeadlineTick <= currentTick;
        }

        public static uint AdvanceIntervalDeadline(
            uint previousDeadlineTick, uint durationTicks)
        {
            return KingdomQuestPineNativeTime.AddDuration(
                previousDeadlineTick, durationTicks);
        }

        /// <summary>
        /// ScriptInterruptTimeOut::sib_BlastCheck (0x0050AE40) calls
        /// Movie::TimeLimit::tl_LeftTick and fires when the returned signed
        /// tick difference is <= 0. Equality is therefore expired.
        /// </summary>
        public static bool IsTimeLimitExpired(
            uint deadlineTick, uint currentTick)
        {
            unchecked
            {
                return (int)(deadlineTick - currentTick) <= 0;
            }
        }

        private static bool TryUsedKind(
            string value,
            out KingdomQuestPineInterruptKind kind,
            out int tailOperandCount)
        {
            kind = 0;
            tailOperandCount = 0;

            if (EqualsToken(value, "PlayerEliminate"))
            {
                kind = KingdomQuestPineInterruptKind.PlayerEliminate;
                tailOperandCount = 1;
            }
            else if (EqualsToken(value, "TimeOut"))
            {
                kind = KingdomQuestPineInterruptKind.TimeOut;
                tailOperandCount = 1;
            }
            else if (EqualsToken(value, "PlayerDead"))
            {
                kind = KingdomQuestPineInterruptKind.PlayerDead;
                tailOperandCount = 1;
            }
            else if (EqualsToken(value, "Sec"))
            {
                kind = KingdomQuestPineInterruptKind.SecondInterval;
                tailOperandCount = 2;
            }
            else if (EqualsToken(value, "HPLow"))
            {
                kind = KingdomQuestPineInterruptKind.HPLow;
                tailOperandCount = 3;
            }
            else if (EqualsToken(value, "DeadIndex"))
            {
                kind = KingdomQuestPineInterruptKind.DeadIndex;
                tailOperandCount = 2;
            }
            else if (EqualsToken(value, "PickUpItemIndex"))
            {
                kind = KingdomQuestPineInterruptKind.PickUpItemIndex;
                tailOperandCount = 2;
            }
            else if (EqualsToken(value, "DeadHandle"))
            {
                kind = KingdomQuestPineInterruptKind.DeadHandle;
                tailOperandCount = 2;
            }
            else if (EqualsToken(value, "NPCClickHandle"))
            {
                kind = KingdomQuestPineInterruptKind.NPCClickHandle;
                tailOperandCount = 2;
            }
            else if (EqualsToken(value, "MobEliminate"))
            {
                kind = KingdomQuestPineInterruptKind.MobEliminate;
                tailOperandCount = 1;
            }
            else
            {
                return false;
            }

            return true;
        }

        private static bool TryEvaluateUsedConstantExpression(
            string value, out int result)
        {
            result = 0;
            if (int.TryParse(value, out result))
                return true;

            string text = value.Trim();
            if (text.Length < 5 ||
                text[0] != '(' ||
                text[text.Length - 1] != ')')
                return false;

            text = text.Substring(1, text.Length - 2).Trim();
            int minus = text.IndexOf('-');
            if (minus <= 0 || text.IndexOf('-', minus + 1) >= 0)
                return false;

            int left;
            int right;
            if (!int.TryParse(text.Substring(0, minus).Trim(), out left) ||
                !int.TryParse(text.Substring(minus + 1).Trim(), out right))
                return false;

            unchecked
            {
                result = left - right;
            }
            return true;
        }

        private sealed class Operand
        {
            public string Text;
            public bool Quoted;
        }

        private static bool TryLex(
            string commandText,
            out List<Operand> values)
        {
            values = new List<Operand>();
            if (string.IsNullOrWhiteSpace(commandText))
                return false;

            string text = commandText.Trim();
            if (text.EndsWith(".", StringComparison.Ordinal))
                text = text.Substring(0, text.Length - 1).TrimEnd();

            int offset = 0;
            while (offset < text.Length)
            {
                SkipWhiteSpace(text, ref offset);
                if (offset >= text.Length)
                    break;

                if (text[offset] == '"')
                {
                    int start = ++offset;
                    while (offset < text.Length && text[offset] != '"')
                        offset++;
                    if (offset >= text.Length)
                        return false;

                    values.Add(new Operand
                    {
                        Text = text.Substring(start, offset - start),
                        Quoted = true,
                    });
                    offset++;
                    continue;
                }

                if (text[offset] == '(')
                {
                    int start = offset;
                    int depth = 0;
                    while (offset < text.Length)
                    {
                        char ch = text[offset++];
                        if (ch == '(') depth++;
                        if (ch == ')')
                        {
                            depth--;
                            if (depth == 0)
                                break;
                        }
                    }
                    if (depth != 0)
                        return false;

                    values.Add(new Operand
                    {
                        Text = text.Substring(start, offset - start),
                        Quoted = false,
                    });
                    continue;
                }

                int tokenStart = offset;
                while (offset < text.Length &&
                    !char.IsWhiteSpace(text[offset]))
                    offset++;

                values.Add(new Operand
                {
                    Text = text.Substring(tokenStart, offset - tokenStart),
                    Quoted = false,
                });
            }

            return values.Count != 0;
        }

        private static void SkipWhiteSpace(string value, ref int offset)
        {
            while (offset < value.Length &&
                char.IsWhiteSpace(value[offset]))
                offset++;
        }

        private static bool EqualsToken(string left, string right)
        {
            return string.Equals(
                left, right, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Mutation-only bookkeeping equivalent of interruptset/erase/clear.
    /// It does not synthesize game events and does not choose BlastCheck
    /// ordering. Entries remain source plans until the matching native event
    /// predicate has been implemented.
    ///
    /// sim_InterruptErase (0x0050C260) compares all four DWORDs of the native
    /// 16-byte name and removes every matching active interrupt, not just the
    /// first. ShineInterruptClear::sa_Step (0x004F8B30) applies ListEraser to
    /// every active manager entry.
    /// </summary>
    public sealed class KingdomQuestPineInterruptRegistryState
    {
        private readonly List<KingdomQuestPineInterruptSetPlan> entries =
            new List<KingdomQuestPineInterruptSetPlan>();

        public IReadOnlyList<KingdomQuestPineInterruptSetPlan> Entries
        {
            get { return entries.AsReadOnly(); }
        }

        public bool TryRegister(KingdomQuestPineInterruptSetPlan plan)
        {
            if (plan == null ||
                entries.Count >=
                    KingdomQuestPineInterruptPlan.NativeManagerCapacity)
                return false;

            entries.Add(plan);
            return true;
        }

        public int Erase(byte[] nativeName16)
        {
            if (nativeName16 == null ||
                nativeName16.Length !=
                    KingdomQuestPineInterruptPlan.NativeEraseNameBytes)
                return 0;

            int removed = 0;
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                if (!SameName16(
                        entries[i].NativeEraseName16,
                        nativeName16))
                    continue;

                entries.RemoveAt(i);
                removed++;
            }

            return removed;
        }

        public void Clear()
        {
            entries.Clear();
        }

        private static bool SameName16(byte[] left, byte[] right)
        {
            if (left == null || right == null ||
                left.Length != 16 || right.Length != 16)
                return false;

            for (int i = 0; i < 16; i++)
            {
                if (left[i] != right[i])
                    return false;
            }
            return true;
        }
    }
}
