using System;

namespace NextGen.Zone.Data
{
    /// <summary>
    /// Exact used-corpus projection of
    /// PineEventScriptNode::SysFuncShineRemoveFisrt::sfb_Calculate
    /// (native class typo preserved by PDB) at Zone.exe 0x004E4B70.
    ///
    /// Native order:
    ///   1. calculate argument 1 and read its first byte as the delimiter;
    ///   2. calculate argument 0 and resolve that token as a VariableStack name;
    ///   3. copy bytes from that variable to the result until delimiter/NUL;
    ///   4. NUL-terminate the result;
    ///   5. skip every consecutive delimiter byte in the source;
    ///   6. shift the remainder to source offset zero, or clear the source when
    ///      no remainder exists.
    ///
    /// The seven calls in the supplied KQ Pine corpus all use two quoted source
    /// strings: variable-name plus a single-space delimiter. String::sa_Load
    /// removes those quotation marks before sfb_Calculate receives the tokens.
    /// </summary>
    public static class KingdomQuestPineRemoveFirst
    {
        public const int UsedCallCount = 7;

        public static KingdomQuestPineExpressionResolution TryCalculateUsed(
            string expression,
            KingdomQuestPineVariableStack variables,
            KingdomQuestPineTokenValue destination)
        {
            if (expression == null ||
                variables == null ||
                destination == null)
                return KingdomQuestPineExpressionResolution.Invalid;

            string variableName;
            string delimiterToken;
            if (!TryParseUsedExpression(
                    expression,
                    out variableName,
                    out delimiterToken))
                return KingdomQuestPineExpressionResolution.Unsupported;

            KingdomQuestPineTokenValue source;
            if (!variables.TryFind(variableName, out source))
                return KingdomQuestPineExpressionResolution.Invalid;

            byte[] sourceBytes = source.SnapshotNativeBytes();
            byte delimiter = delimiterToken.Length == 0
                ? (byte)0
                : (byte)delimiterToken[0];

            byte[] resultBytes =
                new byte[KingdomQuestPineTokenValue.NativeByteCapacity];

            int sourceIndex = 0;
            int resultIndex = 0;

            while (sourceIndex < sourceBytes.Length)
            {
                byte value = sourceBytes[sourceIndex];
                if (value == delimiter || value == 0)
                    break;

                if (resultIndex >= resultBytes.Length - 1)
                    return KingdomQuestPineExpressionResolution.Invalid;

                resultBytes[resultIndex++] = value;
                sourceIndex++;
            }
            resultBytes[resultIndex] = 0;

            while (sourceIndex < sourceBytes.Length &&
                sourceBytes[sourceIndex] == delimiter)
                sourceIndex++;

            byte[] remainder =
                new byte[KingdomQuestPineTokenValue.NativeByteCapacity];
            int remainderIndex = 0;
            while (sourceIndex < sourceBytes.Length &&
                sourceBytes[sourceIndex] != 0)
            {
                if (remainderIndex >= remainder.Length - 1)
                    return KingdomQuestPineExpressionResolution.Invalid;

                remainder[remainderIndex++] =
                    sourceBytes[sourceIndex++];
            }
            remainder[remainderIndex] = 0;

            if (!destination.TrySetAscii(
                    ReadAscii(resultBytes)) ||
                !source.TrySetAscii(
                    ReadAscii(remainder)))
                return KingdomQuestPineExpressionResolution.Invalid;

            return KingdomQuestPineExpressionResolution.Success;
        }

        public static bool TryParseUsedExpression(
            string expression,
            out string variableName,
            out string delimiter)
        {
            variableName = null;
            delimiter = null;
            if (string.IsNullOrWhiteSpace(expression))
                return false;

            string text = expression.Trim();
            const string prefix = "@RemoveFirst(";
            if (!text.StartsWith(
                    prefix, StringComparison.OrdinalIgnoreCase) ||
                !text.EndsWith(")", StringComparison.Ordinal))
                return false;

            int offset = prefix.Length;
            if (!TryReadQuoted(text, ref offset, out variableName) ||
                !TryReadQuoted(text, ref offset, out delimiter))
                return false;

            SkipWhiteSpace(text, ref offset);
            if (offset >= text.Length || text[offset] != ')')
                return false;
            offset++;
            SkipWhiteSpace(text, ref offset);

            return offset == text.Length &&
                variableName.Length != 0;
        }

        private static bool TryReadQuoted(
            string text,
            ref int offset,
            out string value)
        {
            value = null;
            SkipWhiteSpace(text, ref offset);
            if (offset >= text.Length || text[offset] != '"')
                return false;

            int start = ++offset;
            while (offset < text.Length && text[offset] != '"')
                offset++;

            if (offset >= text.Length)
                return false;

            value = text.Substring(start, offset - start);
            offset++;
            return true;
        }

        private static void SkipWhiteSpace(
            string text, ref int offset)
        {
            while (offset < text.Length &&
                char.IsWhiteSpace(text[offset]))
                offset++;
        }

        private static string ReadAscii(byte[] value)
        {
            int length = Array.IndexOf(value, (byte)0);
            if (length < 0)
                length = value.Length;

            return System.Text.Encoding.ASCII.GetString(
                value, 0, length);
        }
    }
}
