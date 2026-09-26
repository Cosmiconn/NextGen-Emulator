using System;

namespace NextGen.Zone.Data
{
    public enum KingdomQuestPineQuestResultKind : byte
    {
        Success = 1,
        Fail = 2,
    }

    /// <summary>
    /// Mutation-free projection of PineEventScriptNode::ShineQuestResult::sa_Step.
    ///
    /// Zone.exe 0x004EF450 lower-cases the command's single argument and
    /// compares it with the PDB global Pine token "index_suc". A match builds
    /// AxialListKQEnd(header 0x16, type 0x12); every other valid token builds
    /// type 0x13. AxialListKQEnd::ali_Work then calls the player's KQ success
    /// or fail title hook and sends that Header-22 packet.
    ///
    /// PDB names 0x12/0x13 as NC_KQ_COMPLETE_CMD / NC_KQ_FAIL_CMD.
    /// CharacterTitleData source categories line up with the native hooks:
    /// 21 = KQ success, 22 = KQ fail.
    /// </summary>
    public sealed class KingdomQuestPineQuestResultPlan
    {
        public const byte NativeHeader = 0x16;
        public const byte NativeCompleteType = 0x12;
        public const byte NativeFailType = 0x13;
        public const uint NativeSuccessTitleCategory = 21;
        public const uint NativeFailTitleCategory = 22;

        public KingdomQuestPineQuestResultKind Kind { get; private set; }
        public byte PacketHeader { get; private set; }
        public byte PacketType { get; private set; }
        public uint TitleCategoryType { get; private set; }

        internal KingdomQuestPineQuestResultPlan(
            KingdomQuestPineQuestResultKind kind)
        {
            Kind = kind;
            PacketHeader = NativeHeader;
            if (kind == KingdomQuestPineQuestResultKind.Success)
            {
                PacketType = NativeCompleteType;
                TitleCategoryType = NativeSuccessTitleCategory;
            }
            else
            {
                PacketType = NativeFailType;
                TitleCategoryType = NativeFailTitleCategory;
            }
        }
    }

    /// <summary>
    /// Mutation-free projection of
    /// PineEventScriptNode::ShineEndOfKingdomQuest::sa_Step.
    ///
    /// Zone.exe 0x004F5FC0 obtains the current FieldMap KQ handle, requires it
    /// to differ from 0xFFFFFFFF, calls
    /// WorldManagerSession::wms_EndOfKQPacket(handle), then invokes
    /// FieldMap::fm_ClearObject(0xB0) before popping the Pine frame.
    ///
    /// AxialListObjectClear::ali_Work proves 0xB0 is an object-class bit mask:
    /// it computes (1 << objectType) and invokes the object's clear virtual
    /// only when that bit is set. The emulator does not reinterpret those bits
    /// here.
    /// </summary>
    public sealed class KingdomQuestPineEndPlan
    {
        public const uint NativeNoKingdomQuestHandle = 0xFFFFFFFFu;
        public const uint NativeClearObjectTypeMask = 0xB0u;

        public uint Handle { get; private set; }
        public uint ClearObjectTypeMask { get; private set; }

        internal KingdomQuestPineEndPlan(uint handle)
        {
            Handle = handle;
            ClearObjectTypeMask = NativeClearObjectTypeMask;
        }
    }

    /// <summary>
    /// Exact terminal KQ command projection for the source-modeled Pine
    /// control runtime. This layer only decodes commands into native actions;
    /// it sends no packets, clears no map objects and mutates no title state.
    /// </summary>
    public static class KingdomQuestPineKqTerminalPlan
    {
        public static bool TryParseQuestResult(
            string commandText,
            out KingdomQuestPineQuestResultPlan plan)
        {
            plan = null;

            string verb;
            string argument;
            if (!TryOneArgumentCommand(
                    commandText, out verb, out argument) ||
                !string.Equals(
                    verb, "questresult",
                    StringComparison.OrdinalIgnoreCase))
                return false;

            // Native sa_Step lower-cases the token and compares only against
            // index_suc. Any other syntactically valid single token follows
            // the NC_KQ_FAIL_CMD branch.
            KingdomQuestPineQuestResultKind kind =
                string.Equals(
                    argument, "suc",
                    StringComparison.OrdinalIgnoreCase)
                    ? KingdomQuestPineQuestResultKind.Success
                    : KingdomQuestPineQuestResultKind.Fail;

            plan = new KingdomQuestPineQuestResultPlan(kind);
            return true;
        }

        public static bool TryParseEndOfKq(
            string commandText,
            uint currentKqHandle,
            out KingdomQuestPineEndPlan plan)
        {
            plan = null;

            string normalized = NormalizeStatement(commandText);
            if (!string.Equals(
                    normalized, "endofkq",
                    StringComparison.OrdinalIgnoreCase) ||
                currentKqHandle ==
                    KingdomQuestPineEndPlan.NativeNoKingdomQuestHandle)
                return false;

            plan = new KingdomQuestPineEndPlan(currentKqHandle);
            return true;
        }

        private static bool TryOneArgumentCommand(
            string commandText,
            out string verb,
            out string argument)
        {
            verb = null;
            argument = null;

            string normalized = NormalizeStatement(commandText);
            if (normalized.Length == 0)
                return false;

            int firstSpace = IndexOfWhiteSpace(normalized, 0);
            if (firstSpace <= 0)
                return false;

            verb = normalized.Substring(0, firstSpace);
            int argumentStart = firstSpace;
            while (argumentStart < normalized.Length &&
                char.IsWhiteSpace(normalized[argumentStart]))
                argumentStart++;

            if (argumentStart >= normalized.Length)
                return false;

            argument = normalized.Substring(argumentStart).Trim();
            if (argument.Length == 0 ||
                IndexOfWhiteSpace(argument, 0) >= 0)
                return false;

            return true;
        }

        private static int IndexOfWhiteSpace(
            string value, int start)
        {
            for (int i = start; i < value.Length; i++)
            {
                if (char.IsWhiteSpace(value[i]))
                    return i;
            }
            return -1;
        }

        private static string NormalizeStatement(string commandText)
        {
            if (string.IsNullOrWhiteSpace(commandText))
                return string.Empty;

            string value = commandText.Trim();
            if (value.EndsWith(".", StringComparison.Ordinal))
                value = value.Substring(0, value.Length - 1).TrimEnd();
            return value;
        }
    }
}
